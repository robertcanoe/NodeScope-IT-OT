#!/usr/bin/env python3
"""CLI para generar el informe HTML interactivo de nodos.

Este comando reutiliza el mismo pipeline de normalizacion y reporte que usa
el worker de ingestion (`main.py` + `opc_report.py`).
"""

from __future__ import annotations

import argparse
import logging
import sys
from pathlib import Path

from main import _analyze, _read_frame
from opc_report import MAX_EMBEDDED_ROWS, write_interactive_nodes_report

logging.getLogger("nodescope.processor").setLevel(logging.WARNING)


def main() -> int:
    parser = argparse.ArgumentParser(description="NodeScope - informe HTML de nodos (OPC UA / spreadsheet)")
    parser.add_argument("entrada", type=Path, help="Archivo .xlsx, .xls, .csv o .json")
    parser.add_argument(
        "salida",
        nargs="?",
        type=Path,
        default=Path("nodescope-nodes-report.html"),
        help="Ruta del HTML generado (default: nodescope-nodes-report.html)",
    )
    parser.add_argument(
        "--limite-filas",
        type=int,
        default=MAX_EMBEDDED_ROWS,
        metavar="N",
        help=f"Maximo de filas embebidas en la tabla/export del HTML (default {MAX_EMBEDDED_ROWS}).",
    )
    args = parser.parse_args()

    if not args.entrada.is_file():
        print(f"No existe el archivo: {args.entrada}", file=sys.stderr)
        return 1

    limite = max(100, args.limite_filas)
    frame = _read_frame(args.entrada, "opcua-default")
    diagnostics = _analyze(frame)
    normalized = diagnostics["normalized"]

    if normalized is None or len(normalized) == 0:
        print("Dataset vacio; no se genera informe.", file=sys.stderr)
        return 1

    out = write_interactive_nodes_report(normalized, args.salida, args.entrada.name, embedded_row_limit=limite)
    print(f"Informe: {out.resolve()} ({out.stat().st_size:,} bytes; {len(normalized):,} filas)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
