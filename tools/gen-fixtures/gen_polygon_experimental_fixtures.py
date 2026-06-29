# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR8 experimental polygon-fill fixtures.

Emits one NDJSON oracle file consumed by the .NET interop conformance tests:

  - polygon_to_cells_experimental.ndjson: per (polygon, resolution, containment
    mode) the SORTED set of covering H3 cells produced by h3-py 4.5.0's
    h3shape_to_cells_experimental(shape, res, contain=...). The mode is emitted
    as the numeric ContainmentMode ordinal (0=center, 1=full, 2=overlap,
    3=bbox_overlap) so it maps 1:1 to the .NET ContainmentMode enum, alongside
    the h3-py contain= token for readability.

The result is a SET of integer H3 cell ids; the .NET differential asserts
unordered-set equality (exact, no floating point), so polygons are chosen with
clear interior / exterior margins (not grazing cell boundaries) to keep
containment unambiguous across platforms.

Ground truth is h3-py 4.5.0 (libh3 4.5.0). Cells are zero-padded 16-hex strings.

Run:
    .venv/bin/python gen_polygon_experimental_fixtures.py
"""

from __future__ import annotations

import os

import h3

from gen_fixtures import DATA_DIR, cell_hex, write_ndjson

# ContainmentMode ordinal -> h3-py contain= token. Ordinals match the .NET
# ContainmentMode enum (Center=0, Full=1, Overlapping=2, OverlappingBBox=3) and
# the C ContainmentMode constants (minus CONTAINMENT_INVALID).
MODES: list[tuple[int, str]] = [
    (0, "center"),
    (1, "full"),
    (2, "overlap"),
    (3, "bbox_overlap"),
]


def gen_polygon_to_cells_experimental() -> int:
    """h3shape_to_cells_experimental over simple polygons x all four modes.

    Polygons are expressed in DEGREES as {exterior, holes} of [lat, lng] rings.
    Each polygon is filled at one resolution that yields a clear, unambiguous
    interior so the four containment modes produce well-separated, deterministic
    sets across platforms.
    """
    records: list[dict] = []

    # Small bounding box near San Francisco (clear interior margin).
    bbox_ext = [
        (37.70, -122.50),
        (37.70, -122.35),
        (37.82, -122.35),
        (37.82, -122.50),
    ]

    # A triangle.
    triangle_ext = [
        (40.00, -75.00),
        (40.00, -74.50),
        (40.40, -74.75),
    ]

    # A box with a rectangular hole punched out of the middle.
    holed_ext = [
        (10.00, 10.00),
        (10.00, 10.80),
        (10.80, 10.80),
        (10.80, 10.00),
    ]
    holed_hole = [
        (10.30, 10.30),
        (10.30, 10.50),
        (10.50, 10.50),
        (10.50, 10.30),
    ]

    # (name, exterior, holes, resolution). One resolution per polygon keeps the
    # four-mode sets modest while still exercising full / overlap / bbox_overlap
    # growth beyond the center set.
    cases = [
        ("bbox", bbox_ext, [], 8),
        ("triangle", triangle_ext, [], 7),
        ("holed", holed_ext, [holed_hole], 6),
    ]

    for _name, ext, holes, res in cases:
        poly = h3.LatLngPoly(ext, *holes)
        for mode_value, contain in MODES:
            cells = [
                cell_hex(c)
                for c in h3.h3shape_to_cells_experimental(poly, res, contain=contain)
            ]
            records.append(
                {
                    "polygon": {
                        "exterior": [[lat, lng] for (lat, lng) in ext],
                        "holes": [
                            [[lat, lng] for (lat, lng) in hole]
                            for hole in holes
                        ],
                    },
                    "res": res,
                    "mode": mode_value,
                    "contain": contain,
                    "cells": cells,
                }
            )

    return write_ndjson("polygon_to_cells_experimental.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    counts = {
        "polygon_to_cells_experimental": gen_polygon_to_cells_experimental(),
    }
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
