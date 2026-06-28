# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR4 directed-edge fixtures.

Emits NDJSON oracle files consumed by the .NET interop conformance tests. The
existing committed corpus does not cover the directed-edge surface
(areNeighborCells / cellsToDirectedEdge / getDirectedEdgeOrigin|Destination /
directedEdgeToCells / originToDirectedEdges / directedEdgeToBoundary /
reverseDirectedEdge), so these fixtures fill that gap.

  - directed_edge.ndjson: per sample origin cell, every directed edge originating
    at it (origin_to_directed_edges -> 6 for a hexagon, 5 for a pentagon), and for
    each edge its origin, destination, reverse, the (origin, destination) cell
    pair, and the full boundary vertex list (degrees, [lat, lng] pairs). The
    boundary count is usually 2 but exceeds 2 on icosahedron-face crossings, which
    pentagon-adjacent edges exercise.
  - neighbor.ndjson: ordered (origin, candidate, are_neighbors) triples covering
    both true neighbors (each origin's own grid-disk-1 ring) and non-neighbors
    (the origin paired with a far cell), so the are_neighbor_cells differential
    sees both branches.

Both reuse the deterministic sample_cells() corpus from gen_fixtures.py so cells
line up exactly with the rest of the corpus. Ground truth is h3-py 4.5.0
(libh3 4.5.0). Cells/edges are zero-padded 16-hex strings.

Run:
    .venv/bin/python gen_directed_edge_fixtures.py
"""

from __future__ import annotations

import os

import h3

from gen_fixtures import DATA_DIR, cell_hex, sample_cells, write_ndjson


def _edge_record(edge: str) -> dict:
    """Full per-edge oracle: origin, destination, reverse, cell pair, boundary."""
    origin = cell_hex(h3.get_directed_edge_origin(edge))
    destination = cell_hex(h3.get_directed_edge_destination(edge))
    cells = [cell_hex(c) for c in h3.directed_edge_to_cells(edge)]
    # h3-py 4.5.0 does not wrap reverseDirectedEdge; the reverse of origin->dest
    # is the directed edge dest->origin, which cells_to_directed_edge computes.
    reverse = cell_hex(h3.cells_to_directed_edge(destination, origin))
    boundary = [[lat, lng] for (lat, lng) in h3.directed_edge_to_boundary(edge)]
    return {
        "edge": cell_hex(edge),
        "origin": origin,
        "destination": destination,
        "cells": cells,
        "reverse": reverse,
        "boundary": boundary,
    }


def gen_directed_edge(cells: list[str]) -> int:
    """Per origin cell: its directed edges plus each edge's full oracle.

    Caps the number of origin cells so the corpus stays modest while still
    covering hexagons and pentagons (whose origin_to_directed_edges yields 5).
    """
    records: list[dict] = []
    for origin in cells:
        edges = [cell_hex(e) for e in h3.origin_to_directed_edges(origin)]
        records.append(
            {
                "origin": origin,
                "is_pentagon": bool(h3.is_pentagon(origin)),
                "edges": [_edge_record(e) for e in edges],
            }
        )
    return write_ndjson("directed_edge.ndjson", records)


def gen_neighbor(cells: list[str]) -> int:
    """Ordered (origin, candidate, are_neighbors) triples, both branches.

    True neighbors come from each origin's distance-1 ring; non-neighbors pair
    the origin with a deterministically chosen far cell from the corpus.
    """
    records: list[dict] = []
    for idx, origin in enumerate(cells):
        ring = [cell_hex(c) for c in h3.grid_disk(origin, 1) if cell_hex(c) != origin]

        # True-neighbor cases (same resolution as origin by construction).
        for candidate in ring:
            records.append(
                {
                    "origin": origin,
                    "candidate": candidate,
                    "are_neighbors": True,
                }
            )

        # A non-neighbor at the SAME resolution: shift far across the same ring
        # set is still adjacent, so pick a corpus cell of equal resolution that is
        # not in the ring. are_neighbor_cells requires matching resolution, so we
        # only assert False for an equal-res, non-adjacent pair.
        res = h3.get_resolution(origin)
        for other in cells[(idx + 7) % len(cells):] + cells[: (idx + 7) % len(cells)]:
            if (
                other != origin
                and other not in ring
                and h3.get_resolution(other) == res
            ):
                records.append(
                    {
                        "origin": origin,
                        "candidate": other,
                        "are_neighbors": bool(h3.are_neighbor_cells(origin, other)),
                    }
                )
                break

    return write_ndjson("neighbor.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    cells = sample_cells()
    counts = {
        "directed_edge": gen_directed_edge(cells),
        "neighbor": gen_neighbor(cells),
    }
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
