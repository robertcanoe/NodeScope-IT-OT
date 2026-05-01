#!/usr/bin/env python3
"""NodeScope ingestion worker: transforms technical spreadsheets into normalized artifacts + deterministic JSON payloads."""

from __future__ import annotations

import csv
import json
import logging
import re
import sys
from pathlib import Path
from typing import Any

import pandas as pd

_LOG = logging.getLogger("nodescope.processor")
logging.basicConfig(level=logging.INFO, format="%(levelname)s %(message)s")

MAX_SAMPLE_ROWS = 500


def _load_request(payload_path: Path) -> dict[str, Any]:
    return json.loads(payload_path.read_text(encoding="utf-8"))


def _read_frame(source: Path, profile: str) -> pd.DataFrame:
    extension = source.suffix.lower()
    match extension:
        case ".csv":
            return pd.read_csv(source)
        case ".xlsx" | ".xls":
            return pd.read_excel(source)
        case ".json":
            return pd.read_json(source)
        case _:
            raise ValueError(f"Unsupported ingestion extension `{extension}` for profile `{profile}`.")


def _normalize_columns(frame: pd.DataFrame) -> pd.DataFrame:
    renamed = []
    seen: dict[str, int] = {}
    for raw in frame.columns.astype(str):
        slug = re.sub(r"[^\w]+", "_", raw.strip().lower()).strip("_") or "column"
        occurrence = seen.get(slug, 0)
        seen[slug] = occurrence + 1
        candidate = slug if occurrence == 0 else f"{slug}_{occurrence}"
        renamed.append(candidate)
    duplicated = pd.DataFrame(frame.values, columns=renamed).copy()

    duplicated_mask = duplicated.columns.duplicated()
    if duplicated_mask.any():  # extremely defensive
        duplicated = duplicated.loc[:, ~duplicated_mask]
    return duplicated


def _find_node_column(frame: pd.DataFrame) -> str | None:
    pattern = re.compile(r"node|nodeid|node_id")
    best: str | None = None
    for column_name in frame.columns:
        lowered = column_name.lower()
        if pattern.search(lowered):
            best = column_name
            break
    return best


def _dominant(series: pd.Series) -> tuple[str | None, int]:
    cleaned = series.dropna().astype(str)
    if cleaned.empty:
        return None, 0
    counts = cleaned.value_counts(dropna=False)
    top_label = counts.index[0]
    return str(top_label), int(counts.iloc[0])


def _to_hashable_token(value: Any) -> Any:
    if isinstance(value, (dict, list)):
        return json.dumps(value, sort_keys=True, ensure_ascii=False)
    return value


def _analyze(frame: pd.DataFrame) -> dict[str, Any]:
    normalized = _normalize_columns(frame)

    profiling: list[dict[str, Any]] = []
    for column in normalized.columns:
        series = normalized[column]
        non_null_series = series.dropna()
        non_null_for_distinct = series.dropna()
        if non_null_for_distinct.size:
            hashable_values = non_null_for_distinct.map(_to_hashable_token)
            distinct_raw = pd.Series(pd.unique(hashable_values))
        else:
            distinct_raw = pd.Series([])
        distinct = int(distinct_raw.size)
        profiler_dtype = (
            pd.api.types.infer_dtype(non_null_series, skipna=True) if non_null_series.size else "empty"
        )
        profiling.append(
            {
                "name": column,
                "normalizedName": column,
                "dataTypeDetected": str(profiler_dtype),
                "distinctCount": distinct,
                "nullCount": int(series.isna().sum()),
            }
        )

    node_column = _find_node_column(normalized)
    issues: list[dict[str, Any]] = []

    duplicates = 0
    dominant_namespace_value: str | None = None
    if node_column:
        duplicated_mask = normalized[node_column].duplicated(keep=False)
        duplicated_rows = normalized[duplicated_mask]
        duplicates = int(duplicated_rows.shape[0])
        if duplicates > 0:
            issues.append(
                {
                    "severity": "Warning",
                    "code": "duplicate-node-id",
                    "message": f"Detected {duplicates} rows sharing duplicate `{node_column}` identifiers.",
                    "columnName": node_column,
                    "rowIndex": None,
                    "rawValue": None,
                }
            )

        namespace_series = normalized[node_column].dropna().astype(str)
        if not namespace_series.empty:
            namespace_tokens = []
            pattern = re.compile(r"ns=(?P<ns>\d+)", re.IGNORECASE)
            for literal in namespace_series:
                matcher = pattern.search(literal)
                namespace_tokens.append(f"ns={matcher.group('ns')}" if matcher else "unknown-namespace")
            dominant_namespace_label, dominant_namespace_frequency = _dominant(pd.Series(namespace_tokens))
            dominant_namespace_value = dominant_namespace_label
            _LOG.info(
                "Namespace heuristic derived from `%s`; dominant `%s` (count=%d)",
                node_column,
                dominant_namespace_label,
                dominant_namespace_frequency,
            )
    else:
        issues.append(
            {
                "severity": "Info",
                "code": "node-column-missing",
                "message": "No heuristic NodeId-oriented column detected; duplicate checks skipped.",
                "columnName": None,
                "rowIndex": None,
                "rawValue": None,
            }
        )

    dtype_series = normalized.dtypes.astype(str).str.lower().map(lambda literal: literal.split(".")[-1])
    dominant_type_label, dominant_type_frequency = _dominant(dtype_series.astype(str))

    sampled_rows = []
    truncated = normalized.head(MAX_SAMPLE_ROWS)
    for _, record in truncated.iterrows():
        serialized = {}
        for key, scalar in record.items():
            if pd.isna(scalar):
                serialized[str(key)] = None
            else:
                serialized[str(key)] = scalar.item() if hasattr(scalar, "item") else scalar
        sampled_rows.append(serialized)

    metrics = {
        "duplicates": duplicates,
        "nullNodeIds": int(normalized[node_column].isna().sum()) if node_column else 0,
        "dominantType": dominant_type_label,
        "dominantNamespace": dominant_namespace_value,
    }

    return {
        "totalRows": int(len(normalized)),
        "totalColumns": int(normalized.shape[1]),
        "metrics": metrics,
        "columns": profiling,
        "recordsSample": sampled_rows,
        "issues": issues,
        "normalized": normalized,
    }


def _write_report(output_dir: Path, diagnostics: dict[str, Any]) -> Path:
    path = output_dir / "report.html"
    dataframe: pd.DataFrame = diagnostics["normalized"]
    table_html = dataframe.head(200).to_html(classes="grid", border=0, index=False)

    snippets = "".join(f"<li><code>{issue['code']}</code>: {issue['message']}</li>" for issue in diagnostics["issues"])

    path.write_text(
        f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>NodeScope report</title>
  <style>
    body {{
      margin: 0;
      padding: 24px;
      font-family: Roboto, "Segoe UI", sans-serif;
      background: #f4f7fb;
      color: #0f172a;
    }}
    .card {{
      background: white;
      border-radius: 8px;
      padding: 20px;
      box-shadow: 0 15px 30px rgb(15 23 42 / 0.05);
      margin-bottom: 20px;
    }}
    ul {{ padding-left: 20px; }}
    table.grid {{ border-collapse: collapse; width: 100%; font-size: 13px; }}
    table.grid th, table.grid td {{ border-bottom: 1px solid #e2e8f0; padding: 8px 10px; text-align: left; }}
  </style>
</head>
<body>
  <div class="card">
    <h1>Technical dataset ingest</h1>
    <p>Rows analysed: <strong>{diagnostics['totalRows']}</strong> —
       Columns recognised: <strong>{diagnostics['totalColumns']}</strong></p>
    <p>Dominant dtype family: <code>{diagnostics["metrics"]["dominantType"]}</code></p>
  </div>
  <div class="card">
    <h2>Structured findings</h2>
    <ul>{snippets or "<li>No structural warnings detected.</li>"}</ul>
  </div>
  <div class="card">
    <h2>Normalized preview</h2>
    {table_html}
  </div>
</body>
</html>""",
        encoding="utf-8",
    )
    return path


def run_job(request_path: Path) -> Path:
    request = _load_request(request_path)
    import_id = request["importId"]
    intake_path = Path(request["inputPath"])
    artifact_dir = Path(request["outputDir"])
    artifact_dir.mkdir(parents=True, exist_ok=True)

    frame = _read_frame(intake_path, request.get("profile", "opcua-default"))
    diagnostics = _analyze(frame)
    report_physical = _write_report(artifact_dir, diagnostics)
    dataframe = diagnostics.pop("normalized")

    dataframe.to_json(
        artifact_dir / "normalized.json",
        orient="records",
        lines=False,
        force_ascii=False,
    )

    issues_csv_path = artifact_dir / "issues.csv"
    issue_rows = diagnostics["issues"]
    with issues_csv_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["severity", "code", "message", "columnName", "rowIndex", "rawValue"])
        for issue_payload in issue_rows:
            writer.writerow(
                [
                    issue_payload.get("severity"),
                    issue_payload.get("code"),
                    issue_payload.get("message"),
                    issue_payload.get("columnName"),
                    issue_payload.get("rowIndex"),
                    issue_payload.get("rawValue"),
                ]
            )

    report_relative = report_physical.name
    normalized_relative = "normalized.json"
    issues_relative = issues_csv_path.name if issues_csv_path.exists() else None

    payload_out = {
        "success": True,
        "totalRows": diagnostics["totalRows"],
        "totalColumns": diagnostics["totalColumns"],
        "reportHtmlPath": report_relative,
        "normalizedJsonPath": normalized_relative,
        "issuesCsvPath": issues_relative,
        "metrics": diagnostics["metrics"],
        "columns": diagnostics["columns"],
        "recordsSample": diagnostics["recordsSample"],
        "issues": diagnostics["issues"],
    }

    artifacts_result = artifact_dir / "pipeline-result.json"
    artifacts_result.write_text(json.dumps(payload_out, indent=2), encoding="utf-8")

    _LOG.info(
        "Pipeline finished import=%s pendingPayload=%s totalRows=%d",
        import_id,
        intake_path.as_posix(),
        diagnostics["totalRows"],
    )
    return artifacts_result


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python main.py /path/to/pipeline-request.json")

    artifacts = run_job(Path(sys.argv[1]))
    sys.stdout.write(artifacts.as_posix())


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:  # noqa: BLE001 - translation boundary emits structured payloads for C# watchers
        _LOG.exception("Fatal pipeline failure.")
        artifact_dir_candidate = Path(sys.argv[1]).parent if len(sys.argv) > 1 else Path.cwd()
        failure_payload = {"success": False, "detail": repr(exc)}
        trace_path = artifact_dir_candidate / "pipeline-result.json"
        trace_path.parent.mkdir(parents=True, exist_ok=True)
        trace_path.write_text(json.dumps(failure_payload), encoding="utf-8")
        raise
