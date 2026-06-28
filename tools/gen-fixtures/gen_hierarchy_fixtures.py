# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR2 hierarchy/compact fixtures.

Emits three NDJSON oracle files consumed by the .NET interop conformance tests.
The existing committed corpus covers cell_to_latlng/boundary/grid_disk/
icosahedron_faces/index_digits/latlng_to_cell/pentagons/polygon_to_cells/
res0_cells but NOT the hierarchy (parent/children/center-child/child-pos) or
compact/uncompact surface, so these fixtures fill that gap.

  - hierarchy.ndjson: per sample cell, its chosen parent, center child, and the
    full sorted children set. Covers hexagons AND pentagons (sample_cells already
    includes pentagons across resolutions), so the pentagon "fewer than 7^delta
    children" strip is exercised.
  - child_pos.ndjson: per sample child cell, its cellToChildPos at a chosen
    coarser parent resolution, for the cellToChildPos <-> childPosToCell inverse.
  - compact.ndjson: per seed cell, the full children set at a chosen resolution
    and its compactCells result (which is exactly the seed cell). Includes at
    least one pentagon-containing region.

All reuse the deterministic sample_cells() corpus from gen_fixtures.py so cells
line up exactly with the rest of the corpus. Ground truth is h3-py 4.5.0
(libh3 4.5.0). Cells are zero-padded 16-hex strings.

Run:
    .venv/bin/python gen_hierarchy_fixtures.py
"""

from __future__ import annotations

import os

import h3

from gen_fixtures import DATA_DIR, cell_hex, sample_cells, write_ndjson

# Cap the children fan-out so a single sample cell never explodes the corpus:
# children at res+1 is at most 7 cells, which is small and cheap.
CHILDREN_DELTA = 1

# Parent/center-child deltas: coarsen/refine by 2 where the resolution permits.
PARENT_DELTA = 2
CENTER_CHILD_DELTA = 2


def gen_hierarchy(cells: list[str]) -> int:
    records: list[dict] = []
    for hcell in cells:
        res = h3.get_resolution(hcell)

        parent_res = max(0, res - PARENT_DELTA)
        center_child_res = min(15, res + CENTER_CHILD_DELTA)
        children_res = min(15, res + CHILDREN_DELTA)

        parent = cell_hex(h3.cell_to_parent(hcell, parent_res))
        center_child = cell_hex(h3.cell_to_center_child(hcell, center_child_res))
        children = sorted(cell_hex(c) for c in h3.cell_to_children(hcell, children_res))

        records.append(
            {
                "cell": hcell,
                "res": res,
                "parent_res": parent_res,
                "parent": parent,
                "center_child_res": center_child_res,
                "center_child": center_child,
                "children_res": children_res,
                "children": children,
            }
        )
    return write_ndjson("hierarchy.ndjson", records)


def gen_child_pos(cells: list[str]) -> int:
    """cellToChildPos for each sample child cell at a coarser parent resolution.

    Skip res-0 cells (no coarser parent resolution exists).
    """
    records: list[dict] = []
    for hcell in cells:
        res = h3.get_resolution(hcell)
        if res == 0:
            continue
        parent_res = max(0, res - PARENT_DELTA)
        pos = int(h3.cell_to_child_pos(hcell, parent_res))
        records.append(
            {
                "child": hcell,
                "parent_res": parent_res,
                "pos": pos,
            }
        )
    return write_ndjson("child_pos.ndjson", records)


def gen_compact(cells: list[str]) -> int:
    """compactCells over the complete children set of a handful of seed cells.

    For a seed cell at res r, the full set of its children at res r+1 compacts
    back to exactly [seed]. Seeds include res-0 hexagons and a res-0 pentagon so
    the pentagon path (fewer than 7 children) is covered.
    """
    seen: set[str] = set()
    seeds: list[str] = []

    def add_seed(token: str) -> None:
        h = cell_hex(token)
        if h not in seen:
            seen.add(h)
            seeds.append(h)

    # A few res-0 base cells.
    for c in list(h3.get_res0_cells())[:4]:
        add_seed(c)

    # At least one res-0 pentagon (its children set is the fewer-than-7 case).
    for p in h3.get_pentagons(0):
        add_seed(p)
        break

    # A couple of finer mid-resolution seeds.
    for lat, lng in [(37.7749, -122.4194), (-33.8688, 151.2093)]:
        add_seed(h3.latlng_to_cell(lat, lng, 4))

    records: list[dict] = []
    for seed in seeds:
        res = h3.get_resolution(seed)
        child_res = min(15, res + 1)
        full_children = list(h3.cell_to_children(seed, child_res))
        compacted = sorted(cell_hex(c) for c in h3.compact_cells(full_children))
        full_children = sorted(cell_hex(c) for c in full_children)
        records.append(
            {
                "res": child_res,
                "input": full_children,
                "compacted": compacted,
            }
        )
    return write_ndjson("compact.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    cells = sample_cells()
    counts = {
        "hierarchy": gen_hierarchy(cells),
        "child_pos": gen_child_pos(cells),
        "compact": gen_compact(cells),
    }
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
