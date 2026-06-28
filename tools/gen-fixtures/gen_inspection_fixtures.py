# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR1 inspection/string-conversion fixtures.

Emits two NDJSON oracle files consumed by the .NET interop conformance tests:

  - index_digits.ndjson: per-cell base cell number, isResClassIII, resolution, and
    the full stored digit vector (digits[1..res]) for the inspection differential.
  - icosahedron_faces.ndjson: per-cell SORTED icosahedron face list (the existing
    corpus does not cover faces).

Both reuse the existing deterministic sample_cells() corpus from gen_fixtures.py so
cells line up exactly with cell_to_latlng/cell_to_boundary. Ground truth is h3-py 4.5.0
(libh3 4.5.0). Cells are zero-padded 16-hex strings.

Run:
    .venv/bin/python gen_inspection_fixtures.py
"""

from __future__ import annotations

import json
import os

import h3

from gen_fixtures import DATA_DIR, cell_hex, sample_cells, write_ndjson


def gen_index_digits(cells: list[str]) -> int:
    records: list[dict] = []
    for hcell in cells:
        res = h3.get_resolution(hcell)
        digits = [h3.get_index_digit(hcell, r) for r in range(1, res + 1)]
        records.append(
            {
                "cell": hcell,
                "res": res,
                "base_cell": h3.get_base_cell_number(hcell),
                "is_class_iii": bool(h3.is_res_class_III(hcell)),
                "digits": digits,
            }
        )
    return write_ndjson("index_digits.ndjson", records)


def gen_icosahedron_faces(cells: list[str]) -> int:
    records: list[dict] = []
    for hcell in cells:
        faces = sorted(int(f) for f in h3.get_icosahedron_faces(hcell))
        records.append({"cell": hcell, "faces": faces})
    return write_ndjson("icosahedron_faces.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    cells = sample_cells()
    counts = {
        "index_digits": gen_index_digits(cells),
        "icosahedron_faces": gen_icosahedron_faces(cells),
    }
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
