# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR3 grid-traversal / localIJ fixtures.

Emits five NDJSON oracle files consumed by the .NET interop conformance tests.
The existing committed corpus covers the inspection / hierarchy / grid_disk
surface but NOT the rest of the grid-traversal domain (gridRing, gridPathCells,
gridDistance, cellToLocalIj / localIjToCell, gridDiskDistances), so these
fixtures fill that gap.

  - grid_ring.ndjson: per origin + k, the hollow ring at exactly distance k.
    Origins where a pentagon distorts the ring (h3-py raises) are skipped so the
    corpus is pure happy-path; the pentagon-error path is asserted in Unit tests.
  - grid_path.ndjson: per (start, end) pair, the ORDERED, endpoint-inclusive line
    of cells (out[0]==start, out[-1]==end). Exercises the exact-size no-strip path.
  - grid_distance.ndjson: per (origin, other) pair, the grid distance. Pairs are
    drawn from the path fixtures so symmetry/reflexivity is checkable.
  - local_ij.ndjson: per (origin, target) pair, the local IJ of target relative to
    origin (mode 0). Round-trip back to target is asserted on the .NET side.
  - grid_disk_distances.ndjson: per origin + k, the parallel (cell, distance)
    pairs. Exercises the dual-buffer lockstep compaction including pentagon holes.

All reuse the deterministic sample_cells() corpus from gen_fixtures.py so cells
line up exactly with the rest of the corpus. Ground truth is h3-py 4.5.0
(libh3 4.5.0). Cells are zero-padded 16-hex strings.

Run:
    .venv/bin/python gen_grid_traversal_fixtures.py
"""

from __future__ import annotations

import os
import random

import h3

from gen_fixtures import (
    DATA_DIR,
    SEED,
    cell_hex,
    curated_latlng_points,
    sample_cells,
    write_ndjson,
)


def _traversal_origins() -> list[str]:
    """A deterministic mix of base cells, pentagons, and mid-res ordinary cells."""
    seen: set[str] = set()
    origins: list[str] = []

    def add(token: str) -> None:
        h = cell_hex(token)
        if h not in seen:
            seen.add(h)
            origins.append(h)

    # A few base cells.
    for c in list(h3.get_res0_cells())[:6]:
        add(c)

    # Pentagons at several resolutions (their rings/disks carry the H3_NULL slot).
    for res in (0, 2, 5):
        for p in h3.get_pentagons(res):
            add(p)

    # Ordinary cells at mid resolutions.
    for lat, lng in [(37.7749, -122.4194), (-33.8688, 151.2093), (0.0, 0.0)]:
        for res in (5, 9):
            add(h3.latlng_to_cell(lat, lng, res))

    return origins


def gen_grid_ring(origins: list[str]) -> int:
    """gridRing for small k. Skip origins where a pentagon distorts the ring.

    h3-py raises H3FailedError (the native E_PENTAGON / E_FAILED) when the ring
    cannot be produced cleanly; those are excluded so the corpus is happy-path.
    The pentagon-error contract is asserted separately in the Unit tests.
    """
    records: list[dict] = []
    for origin in origins:
        for k in range(0, 4):
            try:
                ring = sorted(cell_hex(c) for c in h3.grid_ring(origin, k))
            except Exception:  # noqa: BLE001 - pentagon distortion, skip this case.
                continue
            records.append({"cell": origin, "k": k, "cells": ring})
    return write_ndjson("grid_ring.ndjson", records)


def _path_pairs() -> list[tuple[str, str]]:
    """Deterministic (start, end) same-resolution pairs that admit a clean path."""
    rng = random.Random(SEED + 7)
    pairs: list[tuple[str, str]] = []
    seen: set[tuple[str, str]] = set()

    def add(start: str, end: str) -> None:
        key = (cell_hex(start), cell_hex(end))
        if key not in seen:
            seen.add(key)
            pairs.append(key)

    # Reflexive pairs (distance 0, single-cell path) at a few resolutions.
    for lat, lng in [(37.7749, -122.4194), (-33.8688, 151.2093)]:
        for res in (5, 7, 9):
            c = h3.latlng_to_cell(lat, lng, res)
            add(c, c)

    # Short paths: start at a curated point, end at a k-step neighbour.
    for lat, lng in curated_latlng_points()[10:]:
        for res in (5, 7, 9):
            start = h3.latlng_to_cell(lat, lng, res)
            for k in (1, 2, 4):
                ring = list(h3.grid_disk(start, k))
                # Pick a deterministic far-ish endpoint from the disk.
                end = ring[rng.randrange(len(ring))]
                try:
                    h3.grid_path_cells(start, end)  # validate it admits a path.
                except Exception:  # noqa: BLE001
                    continue
                add(start, end)

    return pairs


def gen_grid_path(pairs: list[tuple[str, str]]) -> int:
    """gridPathCells, ORDERED and endpoint-inclusive (out[0]==start, out[-1]==end)."""
    records: list[dict] = []
    for start, end in pairs:
        path = [cell_hex(c) for c in h3.grid_path_cells(start, end)]
        records.append({"start": start, "end": end, "path": path})
    return write_ndjson("grid_path.ndjson", records)


def gen_grid_distance(pairs: list[tuple[str, str]]) -> int:
    """gridDistance over the same (start, end) pairs used for paths."""
    records: list[dict] = []
    for start, end in pairs:
        dist = int(h3.grid_distance(start, end))
        records.append({"origin": start, "other": end, "distance": dist})
    return write_ndjson("grid_distance.ndjson", records)


def gen_local_ij(pairs: list[tuple[str, str]]) -> int:
    """cellToLocalIj of target relative to origin (mode 0)."""
    records: list[dict] = []
    for origin, target in pairs:
        try:
            i, j = h3.cell_to_local_ij(origin, target)
        except Exception:  # noqa: BLE001 - too far / pentagon distortion, skip.
            continue
        records.append({"origin": origin, "target": target, "i": int(i), "j": int(j)})
    return write_ndjson("local_ij.ndjson", records)


def gen_grid_disk_distances(origins: list[str]) -> int:
    """gridDiskDistances: parallel (cell, distance) pairs for small k.

    h3-py 4.5.0 does not expose gridDiskDistances directly, so the oracle is
    derived purely from grid_disk via concentric set arithmetic: a cell is at
    distance d iff it is in grid_disk(origin, d) but not grid_disk(origin, d-1).
    This is independent of the binding under test (it only consumes grid_disk).
    """
    records: list[dict] = []
    for origin in origins:
        for k in range(0, 4):
            cells: list[str] = []
            distances: list[int] = []
            prev: set[str] = set()
            for d in range(0, k + 1):
                disk = {cell_hex(c) for c in h3.grid_disk(origin, d)}
                shell = sorted(disk - prev)
                for c in shell:
                    cells.append(c)
                    distances.append(d)
                prev = disk
            records.append(
                {
                    "cell": origin,
                    "k": k,
                    "cells": cells,
                    "distances": distances,
                }
            )
    return write_ndjson("grid_disk_distances.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    origins = _traversal_origins()
    pairs = _path_pairs()
    counts = {
        "grid_ring": gen_grid_ring(origins),
        "grid_path": gen_grid_path(pairs),
        "grid_distance": gen_grid_distance(pairs),
        "local_ij": gen_local_ij(pairs),
        "grid_disk_distances": gen_grid_disk_distances(origins),
    }
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
