# SPDX-License-Identifier: Apache-2.0
"""Deterministic fixture generator for the H3.NET.Native test corpus.

Emits ground-truth fixtures produced by h3-py v4 into the committed data
directory consumed by the .NET interop conformance tests.

All angular values at the public surface are DEGREES (matching the H3.NET.Native
public API). H3 cell indexes are emitted as zero-padded 16-character lowercase
hex strings ("%016x") so they round-trip exactly to a ulong on the .NET side.

Floating-point values are serialized at full precision (Python repr / json
defaults) so doubles round-trip bit-for-bit.

Run:
    python -m venv .venv
    .venv/bin/pip install -r requirements.txt
    .venv/bin/python gen_fixtures.py
"""

from __future__ import annotations

import csv
import json
import os
import random

import h3

# Deterministic seed for the random sweeps. Changing this changes the corpus.
SEED = 20260627

# Pinned reference C library version (h3-py 4.5.0 wraps libh3 4.5.0).
LIBH3_VERSION = "4.5.0"

# All 16 H3 resolutions.
RESOLUTIONS = list(range(16))

# Output directory (committed corpus). Resolved relative to this file so the
# generator works regardless of the current working directory.
_THIS_DIR = os.path.dirname(os.path.abspath(__file__))
_REPO_ROOT = os.path.abspath(os.path.join(_THIS_DIR, "..", ".."))
DATA_DIR = os.path.join(
    _REPO_ROOT,
    "tests",
    "H3.NET.Native.Tests",
    "Fixtures",
    "data",
)


def cell_hex(cell: str) -> str:
    """Normalize an h3-py cell token to a zero-padded 16-hex string."""
    return "%016x" % int(cell, 16)


def write_ndjson(name: str, records: list[dict]) -> int:
    """Write one JSON object per line; return the record count."""
    path = os.path.join(DATA_DIR, name)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        for rec in records:
            f.write(json.dumps(rec, ensure_ascii=False, sort_keys=True))
            f.write("\n")
    return len(records)


def curated_latlng_points() -> list[tuple[float, float]]:
    """Edge-case lat/lng points (degrees) that exercise boundary handling."""
    return [
        # Prime meridian / equator.
        (0.0, 0.0),
        # Antimeridian, both signs.
        (0.0, 180.0),
        (0.0, -180.0),
        (45.0, 180.0),
        (-45.0, -180.0),
        # Poles.
        (90.0, 0.0),
        (-90.0, 0.0),
        (90.0, 180.0),
        (-90.0, -180.0),
        # Prime meridian off-equator.
        (51.4779, 0.0),
        # A scattering of well-known locations.
        (37.7749, -122.4194),
        (-33.8688, 151.2093),
        (35.6895, 139.6917),
        (-1.2921, 36.8219),
        (64.1466, -21.9426),
    ]


def gen_latlng_to_cell() -> int:
    rng = random.Random(SEED)
    records: list[dict] = []

    # Curated edge cases across all resolutions.
    for lat, lng in curated_latlng_points():
        for res in RESOLUTIONS:
            cell = h3.latlng_to_cell(lat, lng, res)
            records.append(
                {"lat": lat, "lng": lng, "res": res, "cell": cell_hex(cell)}
            )

    # Seeded random sweep covering all resolutions.
    samples_per_res = 12
    for res in RESOLUTIONS:
        for _ in range(samples_per_res):
            lat = rng.uniform(-90.0, 90.0)
            lng = rng.uniform(-180.0, 180.0)
            cell = h3.latlng_to_cell(lat, lng, res)
            records.append(
                {"lat": lat, "lng": lng, "res": res, "cell": cell_hex(cell)}
            )

    return write_ndjson("latlng_to_cell.ndjson", records)


def sample_cells() -> list[str]:
    """A deterministic, varied set of cells (hex strings) for inspection.

    Includes res-0 base cells, pentagons across several resolutions,
    Class III and Class II cells, and children derived from base cells.
    """
    rng = random.Random(SEED + 1)
    cells: list[str] = []
    seen: set[str] = set()

    def add(token: str) -> None:
        h = cell_hex(token)
        if h not in seen:
            seen.add(h)
            cells.append(h)

    # All res-0 cells.
    for c in h3.get_res0_cells():
        add(c)

    # Pentagons across resolutions (covers the 5-vertex boundary case and
    # the pentagon grid-disk behaviour).
    for res in range(0, 16, 3):
        for p in h3.get_pentagons(res):
            add(p)

    # Cells produced from curated lat/lng across resolutions (covers Class III
    # odd resolutions and Class II even resolutions).
    for lat, lng in curated_latlng_points()[10:]:
        for res in RESOLUTIONS:
            add(h3.latlng_to_cell(lat, lng, res))

    # A seeded random handful at mid resolutions.
    for _ in range(40):
        res = rng.randint(0, 15)
        lat = rng.uniform(-90.0, 90.0)
        lng = rng.uniform(-180.0, 180.0)
        add(h3.latlng_to_cell(lat, lng, res))

    return cells


def gen_cell_to_latlng(cells: list[str]) -> int:
    records: list[dict] = []
    for h in cells:
        lat, lng = h3.cell_to_latlng(h)
        records.append({"cell": h, "lat": lat, "lng": lng})
    return write_ndjson("cell_to_latlng.ndjson", records)


def gen_cell_to_boundary(cells: list[str]) -> int:
    records: list[dict] = []
    for h in cells:
        boundary = h3.cell_to_boundary(h)
        verts = [[lat, lng] for (lat, lng) in boundary]
        records.append({"cell": h, "verts": verts})
    return write_ndjson("cell_to_boundary.ndjson", records)


def gen_grid_disk() -> int:
    """grid_disk for small k over a mix of cells including pentagons."""
    records: list[dict] = []
    seen: set[str] = set()

    origins: list[str] = []

    def add_origin(token: str) -> None:
        h = cell_hex(token)
        if h not in seen:
            seen.add(h)
            origins.append(h)

    # A few base cells.
    for c in list(h3.get_res0_cells())[:6]:
        add_origin(c)

    # Pentagons at several resolutions (their disks have the H3_NULL slot).
    for res in (0, 2, 5):
        for p in h3.get_pentagons(res):
            add_origin(p)

    # A few ordinary cells at mid resolutions.
    for lat, lng in [(37.7749, -122.4194), (-33.8688, 151.2093), (0.0, 0.0)]:
        for res in (5, 9):
            add_origin(h3.latlng_to_cell(lat, lng, res))

    for origin in origins:
        for k in range(0, 4):
            disk = [cell_hex(c) for c in h3.grid_disk(origin, k)]
            records.append({"cell": origin, "k": k, "cells": disk})

    return write_ndjson("grid_disk.ndjson", records)


def gen_polygon_to_cells() -> int:
    """polygon_to_cells over a few simple polygons (bbox, triangle, hole).

    Polygons are expressed in DEGREES as {exterior, holes} of [lat, lng] rings.
    """
    records: list[dict] = []

    # Small bounding box near San Francisco.
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

    cases = [
        ("bbox", bbox_ext, [], [8, 9]),
        ("triangle", triangle_ext, [], [7, 8]),
        ("holed", holed_ext, [holed_hole], [6, 7]),
    ]

    for _name, ext, holes, resolutions in cases:
        poly = h3.LatLngPoly(ext, *holes)
        for res in resolutions:
            cells = [cell_hex(c) for c in h3.h3shape_to_cells(poly, res)]
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
                    "cells": cells,
                }
            )

    return write_ndjson("polygon_to_cells.ndjson", records)


def gen_res0_cells() -> int:
    cells = [cell_hex(c) for c in h3.get_res0_cells()]
    path = os.path.join(DATA_DIR, "res0_cells.csv")
    with open(path, "w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f, lineterminator="\n")
        for h in cells:
            writer.writerow([h])
    return len(cells)


def gen_pentagons() -> int:
    path = os.path.join(DATA_DIR, "pentagons.csv")
    count = 0
    with open(path, "w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f, lineterminator="\n")
        writer.writerow(["pentagon", "res"])
        for res in RESOLUTIONS:
            for p in h3.get_pentagons(res):
                writer.writerow([cell_hex(p), res])
                count += 1
    return count


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)

    cells = sample_cells()

    counts = {
        "latlng_to_cell": gen_latlng_to_cell(),
        "cell_to_latlng": gen_cell_to_latlng(cells),
        "cell_to_boundary": gen_cell_to_boundary(cells),
        "grid_disk": gen_grid_disk(),
        "polygon_to_cells": gen_polygon_to_cells(),
        "res0_cells": gen_res0_cells(),
        "pentagons": gen_pentagons(),
    }

    manifest = {
        "h3_py_version": h3.__version__,
        "libh3_version": LIBH3_VERSION,
        "seed": SEED,
        "generated_counts": counts,
        "note": (
            "Committed, curated ground-truth corpus for H3.NET.Native interop "
            "conformance tests. Angular values are DEGREES; cells are "
            "zero-padded 16-hex strings (ulong). Doubles are full precision. "
            "Regenerate with tools/gen-fixtures/gen_fixtures.py. The nightly "
            "CI sweep covers the large/exhaustive cases; this corpus is "
            "intentionally modest."
        ),
    }

    with open(
        os.path.join(DATA_DIR, "manifest.json"), "w", encoding="utf-8",
        newline="\n",
    ) as f:
        json.dump(manifest, f, indent=2, sort_keys=True)
        f.write("\n")

    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
