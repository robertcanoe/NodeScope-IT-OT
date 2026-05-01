"""Interactive standalone HTML report for OPC UA / node-dataset ingests (Chart.js, filters, CSV export).

Used by ``main.py`` after normalization and optionally from the CLI wrapper ``generarInformeNodos.py``.
"""

from __future__ import annotations

import html
import json
import re
from datetime import datetime
from pathlib import Path
from typing import Any

import pandas as pd

# Embedded row cap keeps the page responsive; full data remains in normalized.json.
MAX_EMBEDDED_ROWS = 25_000

_BRAND_PRIMARY = "#004254"
_BRAND_PRIMARY_HOVER = "#003040"


def _find_node_column(frame: pd.DataFrame) -> str | None:
    pattern = re.compile(r"node|nodeid|node_id|opcnode", re.IGNORECASE)
    best: str | None = None
    for column_name in frame.columns:
        if pattern.search(str(column_name).lower()):
            best = column_name
            break
    return best


def _find_type_column(frame: pd.DataFrame, exclude: frozenset[str]) -> str | None:
    pattern = re.compile(
        r"tipo(\s|_|-|de)*dato|datatype|data_type|dtype|opc_type|valuetype|^type$",
        re.IGNORECASE,
    )
    for column_name in frame.columns:
        if column_name in exclude:
            continue
        if pattern.search(str(column_name).lower()):
            return column_name
    return None


def _find_name_column(frame: pd.DataFrame, exclude: frozenset[str]) -> str | None:
    patterns = (
        re.compile(r"nombre(\s|_|-|de)*variable|variable(\s|_|-|)*name|^variable$", re.IGNORECASE),
        re.compile(r"tag|browse|display|parameter|pathname|opcitem", re.IGNORECASE),
    )
    for column_name in frame.columns:
        if column_name in exclude:
            continue
        lowered = str(column_name).lower()
        for pattern in patterns:
            if pattern.search(lowered):
                return column_name
    for column_name in frame.columns:
        if column_name in exclude:
            continue
        return column_name
    return None


def resolve_opcua_display_columns(normalized: pd.DataFrame) -> tuple[str | None, str | None, str | None]:
    """Returns ``(nombre_col, nodeid_col, tipo_col)`` using normalized headers."""
    node_col = _find_node_column(normalized)
    excluded: set[str] = set()
    if node_col:
        excluded.add(node_col)
    type_col = _find_type_column(normalized, frozenset(excluded))
    if type_col:
        excluded.add(type_col)
    name_col = _find_name_column(normalized, frozenset(excluded))
    return name_col, node_col, type_col


def _rows_for_json(
    frame: pd.DataFrame,
    name_col: str | None,
    node_col: str | None,
    tipo_col: str | None,
) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for idx, (_, series) in enumerate(frame.iterrows()):
        nombre = ""
        if name_col and name_col in frame.columns:
            val = series.get(name_col)
            nombre = "" if pd.isna(val) else str(val)
        nodeid = ""
        if node_col and node_col in frame.columns:
            val = series.get(node_col)
            nodeid = "" if pd.isna(val) else str(val)
        else:
            nodeid = f"row-{idx}"
        tipo = ""
        if tipo_col and tipo_col in frame.columns:
            val = series.get(tipo_col)
            tipo = "" if pd.isna(val) else str(val)
        if not tipo:
            tipo = "(sin tipo)"
        rows.append({"nombre": nombre or nodeid, "nodeid": nodeid, "tipo": tipo})
    return rows


def _tipo_counts(series: pd.Series) -> dict[str, int]:
    cleaned = series.fillna("(vacío)").astype(str).str.strip()
    if cleaned.empty:
        return {}
    return cleaned.value_counts().to_dict()


def _namespace_counts(series: pd.Series) -> dict[str, int]:
    pattern = re.compile(r"ns=\d+", re.IGNORECASE)
    tokens = []
    for literal in series.dropna().astype(str):
        found = pattern.search(literal)
        tokens.append(found.group(0).lower() if found else "sin-ns")
    if not tokens:
        return {}
    return pd.Series(tokens).value_counts().to_dict()


def _build_css() -> str:
    return rf"""
*,*::before,*::after{{box-sizing:border-box;margin:0;padding:0}}
:root,[data-theme="light"]{{
  --bg:#f4f8fa;--surface:#ffffff;--surface-2:#f7fafb;--surface-offset:#eaf0f3;
  --border:rgba(0,0,0,0.08);--divider:#e3e9ec;
  --text:#172327;--text-muted:#5c6f76;--text-faint:#8a99a1;
  --primary:{_BRAND_PRIMARY};--primary-hover:{_BRAND_PRIMARY_HOVER};
  --shadow-sm:0 1px 3px rgba(0,0,0,.06);--shadow-md:0 4px 16px rgba(0,0,0,.08);
  --radius:.5rem;--radius-lg:.75rem;--radius-full:9999px;
  --font-body:'Plus Jakarta Sans','Inter',sans-serif;--font-mono:'JetBrains Mono',monospace;
  --c1:{_BRAND_PRIMARY};--c2:#c45c24;--c3:#0a6e8c;--c4:#6b4f9a;
}}
[data-theme="dark"]{{
  --bg:#0f1619;--surface:#141c20;--surface-2:#18252a;--surface-offset:#1e2e35;
  --border:rgba(255,255,255,0.07);--divider:#2a3840;
  --text:#e4ecef;--text-muted:#8fa7b0;--text-faint:#5f7782;
  --primary:#3d8fa3;--primary-hover:#5aa8ba;
  --shadow-sm:0 1px 3px rgba(0,0,0,.25);--shadow-md:0 4px 16px rgba(0,0,0,.35);
  --c1:#4a9fb4;--c2:#e08a52;--c3:#4db3cc;--c4:#9f7ad4;
}}
html{{font-family:var(--font-body);background:var(--bg);color:var(--text);font-size:15px;-webkit-font-smoothing:antialiased}}
body{{min-height:100dvh}}
nav{{position:sticky;top:0;z-index:100;background:var(--surface);border-bottom:1px solid var(--border);box-shadow:var(--shadow-sm);display:flex;align-items:center;justify-content:space-between;padding:0 clamp(1rem,4vw,2rem);height:56px}}
.nav-logo{{display:flex;align-items:center;gap:.625rem;text-decoration:none;color:var(--text)}}
.nav-title{{font-size:.875rem;font-weight:700;letter-spacing:-.01em}}
.nav-sub{{font-size:.7rem;color:var(--text-muted)}}
.nav-actions{{display:flex;gap:.5rem;align-items:center}}
.btn{{display:inline-flex;align-items:center;gap:.375rem;padding:.4rem .85rem;border-radius:var(--radius);font-size:.8125rem;font-weight:500;cursor:pointer;border:1px solid var(--border);background:var(--surface-2);color:var(--text);transition:all 140ms ease}}
.btn:hover{{background:var(--surface-offset)}}
.btn-primary{{background:var(--primary);color:#fff;border-color:transparent}}
.btn-primary:hover{{background:var(--primary-hover)}}
main{{max-width:1280px;margin-inline:auto;padding:clamp(1.25rem,3vw,2rem) clamp(1rem,4vw,2rem)}}
.page-header{{margin-bottom:1.75rem}}
.page-header h1{{font-size:clamp(1.4rem,2.5vw,1.85rem);font-weight:700;letter-spacing:-.02em;line-height:1.15}}
.page-header p{{margin-top:.35rem;color:var(--text-muted);font-size:.875rem}}
.meta{{display:inline-flex;align-items:center;gap:.3rem;font-size:.75rem;color:var(--text-faint);margin-top:.5rem}}
.kpi-grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(min(220px,100%),1fr));gap:1rem;margin-bottom:1.75rem}}
.kpi{{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-lg);padding:1.125rem 1.25rem;box-shadow:var(--shadow-sm)}}
.kpi-label{{font-size:.72rem;font-weight:600;letter-spacing:.04em;text-transform:uppercase;color:var(--text-muted);display:block;margin-bottom:.25rem}}
.kpi-value{{font-size:2rem;font-weight:700;letter-spacing:-.03em;line-height:1;color:var(--text);font-variant-numeric:tabular-nums;display:block}}
.kpi-sub{{font-size:.75rem;color:var(--text-faint);display:block;margin-top:.2rem}}
.accent{{color:var(--primary)}}
.charts-grid{{display:grid;grid-template-columns:1fr 1fr;gap:1rem;margin-bottom:1.75rem}}
@media(max-width:640px){{.charts-grid{{grid-template-columns:1fr}}}}
.chart-card{{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-lg);padding:1.25rem;box-shadow:var(--shadow-sm)}}
.chart-card h3{{font-size:.8125rem;font-weight:600;letter-spacing:.02em;text-transform:uppercase;color:var(--text-muted);margin-bottom:1rem}}
.chart-wrap{{position:relative;max-height:230px;display:flex;align-items:center;justify-content:center}}
.badge{{display:inline-flex;align-items:center;padding:.2rem .55rem;border-radius:var(--radius-full);font-size:.7rem;font-weight:600}}
.b-String{{background:color-mix(in oklch,var(--c1) 14%,var(--surface));color:var(--c1)}}
.b-Bool{{background:color-mix(in oklch,var(--c2) 14%,var(--surface));color:var(--c2)}}
.b-Int,.b-UInt16,.b-UInt32,.b-DInt,.b-LInt,.b-Byte,.b-Word,.b-DWord,.b-Real{{background:color-mix(in oklch,var(--c3) 14%,var(--surface));color:var(--c3)}}
.table-card{{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius-lg);box-shadow:var(--shadow-sm);overflow:hidden}}
.toolbar{{display:flex;align-items:center;gap:.75rem;padding:1rem 1.25rem;border-bottom:1px solid var(--divider);flex-wrap:wrap}}
.toolbar h3{{font-size:.875rem;font-weight:600;flex:1}}
.srch{{position:relative}}
.srch svg{{position:absolute;left:.65rem;top:50%;transform:translateY(-50%);color:var(--text-faint);pointer-events:none}}
#search{{padding:.4rem .75rem .4rem 2rem;border-radius:var(--radius);border:1px solid var(--border);background:var(--surface-2);color:var(--text);font-size:.8125rem;font-family:var(--font-body);width:220px;max-width:100%;outline:none;transition:border-color 140ms}}
#search:focus{{border-color:var(--primary)}}
#tipo-filter{{padding:.4rem .75rem;border-radius:var(--radius);border:1px solid var(--border);background:var(--surface-2);color:var(--text);font-size:.8125rem;font-family:var(--font-body);cursor:pointer;outline:none}}
#tipo-filter:focus{{border-color:var(--primary)}}
.chip{{font-size:.75rem;color:var(--text-muted);white-space:nowrap}}
.tbl-wrap{{overflow-x:auto}}
table{{width:100%;border-collapse:collapse;font-size:.82rem}}
thead tr{{background:var(--surface-offset)}}
th{{padding:.65rem 1rem;text-align:left;font-size:.7rem;font-weight:600;letter-spacing:.04em;text-transform:uppercase;color:var(--text-muted);white-space:nowrap;border-bottom:1px solid var(--divider);cursor:pointer;user-select:none}}
th:hover{{color:var(--text)}}
td{{padding:.6rem 1rem;border-bottom:1px solid var(--divider);vertical-align:middle}}
tr:last-child td{{border-bottom:none}}
tbody tr:hover{{background:var(--surface-offset)}}
.cn{{max-width:420px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:var(--text-muted);font-size:.78rem}}
.cn b{{color:var(--text);font-weight:500}}
.nid{{font-family:var(--font-mono);font-size:.72rem;color:var(--text-faint);max-width:260px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}}
.tbl-footer{{padding:.75rem 1.25rem;border-top:1px solid var(--divider);font-size:.75rem;color:var(--text-muted);display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:.5rem}}
.empty{{padding:3rem;text-align:center;color:var(--text-faint);font-size:.875rem;display:none}}
#theme-btn{{background:var(--surface-2);border:1px solid var(--border);color:var(--text);width:36px;height:36px;border-radius:var(--radius);cursor:pointer;display:flex;align-items:center;justify-content:center;transition:all 140ms}}
#theme-btn:hover{{background:var(--surface-offset)}}
[data-theme="dark"] .i-sun{{display:block}}[data-theme="dark"] .i-moon{{display:none}}
[data-theme="light"] .i-sun{{display:none}}[data-theme="light"] .i-moon{{display:block}}
"""


def build_interactive_html(
    normalized: pd.DataFrame,
    source_label: str,
    *,
    embedded_row_limit: int = MAX_EMBEDDED_ROWS,
) -> str:
    """Return full HTML document string."""
    name_col, node_col, tipo_col = resolve_opcua_display_columns(normalized)
    total = int(len(normalized))

    embed_len = min(total, embedded_row_limit)
    embedded = normalized.iloc[:embed_len]
    truncated = total > embed_len

    tipo_source = (
        normalized[tipo_col]
        if tipo_col and tipo_col in normalized.columns
        else pd.Series(["(sin columna tipo)"] * total, index=normalized.index)
    )
    tipo_counts = _tipo_counts(tipo_source)
    node_source = (
        normalized[node_col]
        if node_col and node_col in normalized.columns
        else pd.Series([], dtype=object)
    )
    namespace_counts = _namespace_counts(node_source) if not node_source.empty else {}

    if not namespace_counts:
        namespace_counts = {"n/s": total}

    rows_payload = _rows_for_json(embedded, name_col, node_col, tipo_col)
    rows_json = json.dumps(rows_payload, ensure_ascii=False)
    tipo_labels = json.dumps(list(tipo_counts.keys()), ensure_ascii=False)
    tipo_data = json.dumps(list(tipo_counts.values()), ensure_ascii=False)
    ns_labels = json.dumps(list(namespace_counts.keys()), ensure_ascii=False)
    ns_data = json.dumps(list(namespace_counts.values()), ensure_ascii=False)

    fecha_gen = datetime.now().strftime("%d/%m/%Y %H:%M")
    tipo_options = "".join(
        f'<option value="{html.escape(str(t))}">{html.escape(str(t))}</option>'
        for t in tipo_counts
    )
    tipo_summary = " · ".join(tipo_counts.keys()) if tipo_counts else "—"
    ns_summary = " · ".join(namespace_counts.keys()) if namespace_counts else "—"

    tc_items = list(tipo_counts.items())
    top_tipo, top_count = tc_items[0] if tc_items else ("—", 0)
    top_pct = round(top_count / total * 100) if total else 0

    col_hint = []
    if name_col:
        col_hint.append(str(name_col))
    if node_col:
        col_hint.append(str(node_col))
    if tipo_col:
        col_hint.append(str(tipo_col))
    heuristic_line = ", ".join(col_hint) if col_hint else "heurística genérica"

    truncation_html = ""
    if truncated:
        truncation_html = (
            f'<div class="meta" style="margin-top:0.5rem;color:#b45309">'
            f"Tabla y export CSV: primeras <strong>{embed_len}</strong> filas "
            f"de <strong>{total}</strong>; dataset completo en <code>normalized.json</code>."
            f"</div>"
        )

    css = _build_css()

    html_nav = (
        '<nav><a class="nav-logo" href="#"><svg width="28" height="28" viewBox="0 0 28 28" fill="none">'
        '<rect width="28" height="28" rx="7" fill="var(--primary)" opacity=".15"/>'
        '<rect x="5" y="5" width="8" height="8" rx="2" fill="var(--primary)"/>'
        '<rect x="15" y="5" width="8" height="8" rx="2" fill="var(--primary)" opacity=".5"/>'
        '<rect x="5" y="15" width="8" height="8" rx="2" fill="var(--primary)" opacity=".5"/>'
        '<rect x="15" y="15" width="8" height="8" rx="2" fill="var(--primary)" opacity=".3"/></svg>'
        '<div><div class="nav-title">NodeScope</div><div class="nav-sub">IT · OT ingestion</div></div></a>'
        '<div class="nav-actions"><button id="export-btn" class="btn btn-primary"><svg width="13" height="13" '
        'viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"><path d="M21 15v4a2 2 0 0 '
        '1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>'
        '</svg>Exportar CSV</button><button type="button" id="theme-btn" aria-label="Tema claro u oscuro">'
        '<svg class="i-sun" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" '
        'stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36'
        'l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>'
        '<svg class="i-moon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" '
        'stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg></button></div></nav>'
    )

    parts = [
        "<!DOCTYPE html>",
        '<html lang="es" data-theme="dark">',
        "<head>",
        '<meta charset="UTF-8"/>',
        '<meta name="viewport" content="width=device-width,initial-scale=1.0"/>',
        "<title>NodeScope — Informe de nodos</title>",
        '<link rel="preconnect" href="https://fonts.googleapis.com">',
        '<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>',
        '<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300..700&family=JetBrains+Mono:wght@400;600'
        '&family=Plus+Jakarta+Sans:wght@400..700&display=swap" rel="stylesheet">',
        '<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>',
        f"<style>{css}</style>",
        "</head><body>",
        html_nav,
        "<main>",
        '<div class="page-header"><h1>Informe de nodos</h1>',
        "<p>Vista interactiva del dataset ingerido: tipos, namespaces (ns=) y tabla filtrable.</p>",
        '<div class="meta">Generado el '
        + html.escape(fecha_gen)
        + " · Origen: "
        + html.escape(source_label)
        + " · Columnas: "
        + html.escape(heuristic_line)
        + "</div>",
        truncation_html,
        "</div>",
        '<div class="kpi-grid">',
        f'<div class="kpi"><span class="kpi-label">Total filas</span><span class="kpi-value accent">{total}</span>'
        f'<span class="kpi-sub">Registros en el archivo</span></div>',
        f'<div class="kpi"><span class="kpi-label">Tipos de dato</span><span class="kpi-value">{len(tipo_counts)}</span>'
        f'<span class="kpi-sub">{tipo_summary}</span></div>',
        f'<div class="kpi"><span class="kpi-label">Namespaces</span><span class="kpi-value">{len(namespace_counts)}</span>'
        f'<span class="kpi-sub">{ns_summary}</span></div>',
        f'<div class="kpi"><span class="kpi-label">Tipo dominante</span><span class="kpi-value" style="font-size:1.4rem">'
        f"{top_tipo}</span><span class=\"kpi-sub\">{top_count} filas ({top_pct}% del total)</span></div>",
        "</div>",
        '<div class="charts-grid"><div class="chart-card"><h3>Distribución por tipo de dato</h3>'
        '<div class="chart-wrap"><canvas id="cTipo"></canvas></div></div>'
        '<div class="chart-card"><h3>Distribución por namespace</h3>'
        '<div class="chart-wrap"><canvas id="cNS"></canvas></div></div></div>',
        '<div class="table-card"><div class="toolbar"><h3>Listado de nodos</h3><div class="srch">'
        '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">'
        '<circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>'
        '<input type="text" id="search" placeholder="Buscar nombre o NodeId…" autocomplete="off"/></div>',
        '<select id="tipo-filter"><option value="">Todos los tipos</option>' + tipo_options + "</select>",
        f'<span class="chip" id="row-count">{embed_len} filas en vista</span></div>',
        '<div class="tbl-wrap"><table><thead><tr>'
        '<th data-col="idx">#</th><th data-col="nombre">Nombre / variable</th>'
        '<th data-col="nodeid">NodeId</th><th data-col="tipo">Tipo de dato</th>'
        "</tr></thead><tbody id=\"tbody\"></tbody></table>"
        '<div class="empty" id="empty">Sin resultados para esta búsqueda.</div></div>',
        '<div class="tbl-footer"><span id="fc"></span>'
        '<span style="color:var(--text-faint)">NodeScope · informe embebido</span></div></div>',
        "</main>",
    ]

    js_template = """
const DATA = __ROWS_JSON__;
const TOTAL = __TOTAL__;
const EMBED_LEN = __EMBED_LEN__;
function badge(t) {
  const m = { String: 'b-String', Bool: 'b-Bool', Int: 'b-Int', DInt: 'b-DInt', UInt16: 'b-UInt16', UInt32: 'b-UInt32', LInt: 'b-LInt', Byte: 'b-Byte', Word: 'b-Word', DWord: 'b-DWord', Real: 'b-Real' };
  const cls = m[t] || '';
  return '<span class="badge ' + cls + '">' + esc(t) + '</span>';
}
function esc(s) {
  return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}
function shortN(n) {
  const p = String(n).split('.');
  const last = p[p.length - 1];
  const pre = p.slice(0, -1).join('.') + '.';
  return '<span class="cn"><b>' + esc(last) + '</b> <span style="font-size:.7rem;color:var(--text-faint)">' + (p.length > 1 ? esc(pre) : '') + '</span></span>';
}
let fil = [...DATA];
let sortCol = 'idx';
let sortAsc = true;
const tbody = document.getElementById('tbody');
const rc = document.getElementById('row-count');
const fc = document.getElementById('fc');
const empty = document.getElementById('empty');
function render() {
  if (!fil.length) {
    tbody.innerHTML = '';
    empty.style.display = 'block';
  } else {
    empty.style.display = 'none';
    tbody.innerHTML = fil.map((r, i) =>
      '<tr><td style="color:var(--text-faint);font-variant-numeric:tabular-nums">' + (i + 1) +
      '</td><td>' + shortN(r.nombre) + '</td><td><span class="nid">' + esc(r.nodeid) + '</span></td><td>' + badge(r.tipo) + '</td></tr>'
    ).join('');
  }
  rc.textContent = fil.length + ' fila' + (fil.length !== 1 ? 's' : '') + ' en vista';
  fc.textContent = 'Mostrando ' + fil.length + ' de ' + EMBED_LEN + ' embebidas · Total dataset ' + TOTAL;
}
render();
const search = document.getElementById('search');
const tf = document.getElementById('tipo-filter');
function applyFilter() {
  const q = search.value.toLowerCase();
  const t = tf.value;
  fil = DATA.filter(function (r) {
    return (!q || r.nombre.toLowerCase().includes(q) || r.nodeid.toLowerCase().includes(q)) && (!t || r.tipo === t);
  });
  render();
}
search.addEventListener('input', applyFilter);
tf.addEventListener('change', applyFilter);
document.querySelectorAll('th[data-col]').forEach(function (th) {
  th.addEventListener('click', function () {
    const c = th.dataset.col;
    sortAsc = sortCol === c ? !sortAsc : true;
    sortCol = c;
    const ix = function (row) {
      return DATA.indexOf(row);
    };
    fil.sort(function (a, b) {
      var va = c === 'idx' ? ix(a) : String(a[c] || '');
      var vb = c === 'idx' ? ix(b) : String(b[c] || '');
      if (sortAsc) return va > vb ? 1 : va < vb ? -1 : 0;
      return va < vb ? 1 : va > vb ? -1 : 0;
    });
    render();
  });
});
function cv(v) {
  return getComputedStyle(document.documentElement).getPropertyValue(v).trim();
}
var TIPO_LABELS = __TIPO_LABELS__;
var TIPO_DATA = __TIPO_DATA__;
var NS_LABELS = __NS_LABELS__;
var NS_DATA = __NS_DATA__;
var chartColorsTipo = TIPO_DATA.map(function (_, i) {
  return cv(['--c1', '--c2', '--c3', '--c4'][i % 4]);
});
var nsBarColors = NS_DATA.map(function (_, i) {
  return cv(i % 2 === 0 ? '--c1' : '--c3');
});
new Chart(document.getElementById('cTipo'), {
  type: 'doughnut',
  data: { labels: TIPO_LABELS, datasets: [{ data: TIPO_DATA, backgroundColor: chartColorsTipo, borderWidth: 0, hoverOffset: 6 }] },
  options: { cutout: '65%', animation: { duration: 500 }, plugins: { legend: { labels: { color: cv('--text-muted'), font: { family: 'Plus Jakarta Sans', size: 12 } } }, tooltip: { callbacks: { label: function (c) { return ' ' + c.label + ': ' + c.raw; } } } } }
});
new Chart(document.getElementById('cNS'), {
  type: 'bar',
  data: {
    labels: NS_LABELS,
    datasets: [{ data: NS_DATA, backgroundColor: nsBarColors, borderRadius: 6, borderSkipped: false }],
  },
  options: {
    indexAxis: 'y',
    animation: { duration: 500 },
    scales: {
      x: { grid: { color: cv('--border') }, ticks: { color: cv('--text-muted'), font: { family: 'Plus Jakarta Sans', size: 11 } } },
      y: { grid: { display: false }, ticks: { color: cv('--text-muted'), font: { family: 'Plus Jakarta Sans', size: 12 } } }
    },
    plugins: { legend: { display: false }, tooltip: { callbacks: { label: function (c) { return ' ' + c.raw + ' filas'; } } } }
  }
});
document.getElementById('theme-btn').addEventListener('click', function () {
  var h = document.documentElement;
  h.dataset.theme = h.dataset.theme === 'dark' ? 'light' : 'dark';
});
document.getElementById('export-btn').addEventListener('click', function () {
  var head = '\\uFEFFNombre de variable,NodeId,Tipo de dato\\n';
  var rows = fil.map(function (r) { return '\"' + r.nombre.replace(/\"/g,'\"\"') + '\",\"' + r.nodeid.replace(/\"/g,'\"\"') + '\",\"' + r.tipo.replace(/\"/g,'\"\"') + '\"'; }).join('\\n');
  var b = new Blob([head + rows], { type: 'text/csv;charset=utf-8;' });
  var a = document.createElement('a');
  a.href = URL.createObjectURL(b);
  a.download = 'nodescope_nodos_filtrados.csv';
  a.click();
});
"""

    js_body = js_template.replace("__ROWS_JSON__", rows_json)
    js_body = js_body.replace("__TOTAL__", str(total))
    js_body = js_body.replace("__EMBED_LEN__", str(embed_len))
    js_body = js_body.replace("__TIPO_LABELS__", tipo_labels)
    js_body = js_body.replace("__TIPO_DATA__", tipo_data)
    js_body = js_body.replace("__NS_LABELS__", ns_labels)
    js_body = js_body.replace("__NS_DATA__", ns_data)

    return "\n".join(parts) + "\n<script>\n" + js_body + "\n</script>\n</body></html>\n"


def write_interactive_nodes_report(
    normalized: pd.DataFrame,
    output_path: Path,
    source_label: str,
    *,
    embedded_row_limit: int = MAX_EMBEDDED_ROWS,
) -> Path:
    """Write HTML report to ``output_path``; returns the path."""
    html = build_interactive_html(normalized, source_label, embedded_row_limit=embedded_row_limit)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(html, encoding="utf-8")
    return output_path


def render_minimal_fallback_report(
    diagnostics: dict[str, Any],
    output_path: Path,
) -> Path:
    """Smaller HTML when the dataset is empty or generation is skipped."""
    issues = diagnostics.get("issues") or []
    snippets = "".join(
        f"<li><code>{html.escape(str(issue.get('code', '')))}</code>: "
        f"{html.escape(str(issue.get('message', '')))}</li>"
        for issue in issues
    )
    table_html = diagnostics["normalized"].head(200).to_html(classes="grid", border=0, index=False)
    path = output_path
    path.write_text(
        f"""<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <title>NodeScope — informe</title>
  <style>
    body {{ margin: 0; padding: 24px; font-family: "Plus Jakarta Sans", Roboto, sans-serif; background: #f4f8fa; color: #172327; }}
    .card {{ background: white; border-radius: 8px; padding: 20px; box-shadow: 0 15px 30px rgb(0 66 84 / 0.06); margin-bottom: 20px; border: 1px solid #e3e9ec; }}
    ul {{ padding-left: 20px; }}
    table.grid {{ border-collapse: collapse; width: 100%; font-size: 13px; }}
    table.grid th, table.grid td {{ border-bottom: 1px solid #e3e9ec; padding: 8px 10px; text-align: left; }}
    h1 {{ color: {_BRAND_PRIMARY}; }}
  </style>
</head>
<body>
  <div class="card">
    <h1>Informe técnico</h1>
    <p>Filas: <strong>{diagnostics["totalRows"]}</strong> — Columnas: <strong>{diagnostics["totalColumns"]}</strong></p>
    <p>Tipo dominante (pandas): <code>{diagnostics["metrics"]["dominantType"]}</code></p>
  </div>
  <div class="card">
    <h2>Hallazgos</h2>
    <ul>{snippets or "<li>Sin incidencias estructurales detectadas.</li>"}</ul>
  </div>
  <div class="card">
    <h2>Vista previa normalizada</h2>
    {table_html}
  </div>
</body>
</html>""",
        encoding="utf-8",
    )
    return path


__all__ = [
    "MAX_EMBEDDED_ROWS",
    "build_interactive_html",
    "resolve_opcua_display_columns",
    "write_interactive_nodes_report",
    "render_minimal_fallback_report",
]
