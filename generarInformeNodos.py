#!/usr/bin/env python3
"""CLI: mismo informe HTML interactivo que genera el worker NodeScope (Chart.js + KPIs + tabla + CSV).

Ejemplos::

    ./generarInformeNodos.py data-prueba/Nodos_DBPruebasFormacion.xlsx
    ./generarInformeNodos.py libro.xlsx ./salida/informe.html --limite-filas 5000

La tabla y la exportación CSV interna respetan ``--limite-filas`` (por rendimiento del navegador);
el backend de ingesta también usa ese límite al escribir ``report.html`` junto al JSON completo.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parent
_PROC = _REPO / "python" / "processor"
sys.path.insert(0, str(_PROC))

import logging

logging.getLogger("nodescope.processor").setLevel(logging.WARNING)

from main import _analyze, _read_frame  # noqa: E402

from opc_report import MAX_EMBEDDED_ROWS, write_interactive_nodes_report  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="NodeScope — informe HTML de nodos (OPC UA / spreadsheet)")
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
        help=f"Máximo de filas embebidas en la tabla/export del HTML (default {MAX_EMBEDDED_ROWS}).",
    )
    args = parser.parse_args()
    entrada = args.entrada
    if not entrada.is_file():
        print(f"No existe el archivo: {entrada}", file=sys.stderr)
        return 1
    salida = args.salida
    limite = max(100, args.limite_filas)

    frame = _read_frame(entrada, "opcua-default")
    diagnostics = _analyze(frame)
    df = diagnostics["normalized"]
    if df is None or len(df) == 0:
        print("Dataset vacío; no se genera informe.", file=sys.stderr)
        return 1

    out = write_interactive_nodes_report(df, salida, entrada.name, embedded_row_limit=limite)
    nbytes = out.stat().st_size
    print(f"Informe: {out.resolve()} ({nbytes:,} bytes; {len(df):,} filas en origen)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
