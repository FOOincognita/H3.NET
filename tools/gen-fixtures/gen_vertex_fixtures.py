# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR5 vertex fixtures.

Emits an NDJSON oracle file consumed by the .NET interop conformance tests. The
existing committed corpus does not cover the vertex surface (cellToVertex /
cellToVertexes / vertexToLatLng / isValidVertex), so these fixtures fill that
gap.

  - vertex.ndjson: per sample origin cell, the full topological-vertex oracle:
    is_pentagon, num_vertexes (cell_to_vertexes length: 6 for a hexagon, 5 for a
    pentagon), and for each vertex its vertex_num, the 16-hex vertex index, and
    its degrees lat/lng. cell_to_vertex(origin, n) is emitted for every valid n
    (0..5 hexagon / 0..4 pentagon); the 6th pentagon slot does not exist and is
    therefore absent (it is the H3_NULL hole the binding strips).

Reuses the deterministic sample_cells() corpus from gen_fixtures.py so cells
line up exactly with the rest of the corpus and include pentagons across
resolutions. Ground truth is h3-py 4.5.0 (libh3 4.5.0). Cells and vertexes are
zero-padded 16-hex strings.

Run:
    .venv/bin/python gen_vertex_fixtures.py
"""

from __future__ import annotations

import os

import h3

from gen_fixtures import DATA_DIR, cell_hex, sample_cells, write_ndjson


def _vertex_record(origin: str, vertex_num: int) -> dict:
    """Full per-vertex oracle: vertex_num, 16-hex vertex index, degrees lat/lng."""
    vertex = h3.cell_to_vertex(origin, vertex_num)
    lat, lng = h3.vertex_to_latlng(vertex)
    # is_valid_vertex must agree for every vertex the generator emits.
    assert h3.is_valid_vertex(vertex), f"oracle emitted an invalid vertex for {origin}#{vertex_num}"
    return {
        "vertex_num": vertex_num,
        "vertex": cell_hex(vertex),
        "lat": lat,
        "lng": lng,
    }


def gen_vertex(cells: list[str]) -> int:
    """Per origin cell: its full vertex set plus each vertex's full oracle.

    cell_to_vertexes yields 6 vertices for a hexagon and 5 for a pentagon; the
    per-vertex records are built from cell_to_vertex(origin, n) for n in
    range(num_vertexes), so the indexed and bulk APIs are pinned consistently.
    """
    records: list[dict] = []
    for origin in cells:
        is_pentagon = bool(h3.is_pentagon(origin))
        num_vertexes = len(h3.cell_to_vertexes(origin))
        records.append(
            {
                "origin": origin,
                "is_pentagon": is_pentagon,
                "num_vertexes": num_vertexes,
                "vertexes": [_vertex_record(origin, n) for n in range(num_vertexes)],
            }
        )
    return write_ndjson("vertex.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    cells = sample_cells()
    counts = {
        "vertex": gen_vertex(cells),
    }
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
