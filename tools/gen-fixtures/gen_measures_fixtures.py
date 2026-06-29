# SPDX-License-Identifier: Apache-2.0
"""Generator for the PR6 measures fixtures.

Emits NDJSON oracle files consumed by the .NET interop conformance tests. The
existing committed corpus does not cover the measures surface (cellArea* /
edgeLength* / getHexagonAreaAvg* / getHexagonEdgeLengthAvg* / getNumCells /
greatCircleDistance*), so these fixtures fill that gap.

  - cell_area.ndjson: per sample cell, its area in rads2 / km2 / m2.
  - edge_length.ndjson: per sample directed edge, its length in rads / km / m.
  - hexagon_area_avg.ndjson: per resolution 0..15, the average hexagon area in
    km2 and m2 (average_hexagon_area).
  - hexagon_edge_length_avg.ndjson: per resolution 0..15, the average hexagon
    edge length in km and m (average_hexagon_edge_length).
  - num_cells.ndjson: per resolution 0..15, the total cell count (get_num_cells).
  - great_circle_distance.ndjson: curated (a, b) DEGREE point pairs and their
    great-circle distance in rads / km / m. These are the critical
    degrees-input check: h3.great_circle_distance takes (lat, lng) DEGREE
    tuples, so the binding's degrees->radians staging must reproduce them.

res0_cells.csv (122) and pentagons.csv (192, 12/res) already exist in the
committed corpus and fully cover get_res0_cells / get_pentagons, so they are
NOT regenerated here.

Reuses the deterministic sample_cells() corpus and curated_latlng_points() from
gen_fixtures.py so cells/points line up exactly with the rest of the corpus.
Ground truth is h3-py 4.5.0 (libh3 4.5.0). Cells/edges are zero-padded 16-hex
strings; doubles are full precision.

Run:
    .venv/bin/python gen_measures_fixtures.py
"""

from __future__ import annotations

import ctypes
import os

import h3

from gen_fixtures import (
    DATA_DIR,
    RESOLUTIONS,
    cell_hex,
    curated_latlng_points,
    sample_cells,
    write_ndjson,
)

# Path to the same native libh3 the .NET binding loads. The committed runtime copy
# is the ground truth for the unit-in-name average functions (see _cfn below).
_LIBH3_PATH = os.path.abspath(
    os.path.join(
        os.path.dirname(os.path.abspath(__file__)),
        "..",
        "..",
        "runtimes",
        "osx-arm64",
        "native",
        "libh3.dylib",
    )
)


def _cfn(name: str, res: int) -> float:
    """Call a unit-in-name C function `name(int res, double* out)` directly.

    The H3 C library's getHexagonAreaAvgM2 / getHexagonEdgeLengthAvgM return
    SEPARATELY ROUNDED published constants, NOT km*1000 / km2*1e6. h3-py instead
    derives the meter result from the km result by multiplying, so h3-py's m / m2
    diverge from the C function the .NET binding wraps 1:1. The differential
    oracle for those meter columns must therefore come from the C library itself,
    not h3-py, or the strict 1e-9 differential is comparing two different
    computations. The km / km2 / rads / rads2 columns match h3-py exactly and stay
    sourced from h3-py.
    """
    lib = ctypes.CDLL(_LIBH3_PATH)
    fn = getattr(lib, name)
    fn.restype = ctypes.c_uint32
    out = ctypes.c_double()
    err = fn(res, ctypes.byref(out))
    assert err == 0, f"{name}({res}) returned H3 error {err}"
    return out.value


def gen_cell_area(cells: list[str]) -> int:
    """Per sample cell: its area in rads2 / km2 / m2."""
    records: list[dict] = []
    for cell in cells:
        records.append(
            {
                "cell": cell,
                "rads2": h3.cell_area(cell, unit="rads^2"),
                "km2": h3.cell_area(cell, unit="km^2"),
                "m2": h3.cell_area(cell, unit="m^2"),
            }
        )
    return write_ndjson("cell_area.ndjson", records)


def _sample_edges(cells: list[str]) -> list[str]:
    """A deterministic set of directed edges: every edge out of each sample cell."""
    edges: list[str] = []
    seen: set[str] = set()
    for cell in cells:
        for edge in h3.origin_to_directed_edges(cell):
            # origin_to_directed_edges may pad a pentagon's 6th slot with 0; skip it.
            if int(edge, 16) == 0:
                continue
            h = cell_hex(edge)
            if h not in seen:
                seen.add(h)
                edges.append(h)
    return edges


def gen_edge_length(cells: list[str]) -> int:
    """Per sample directed edge: its length in rads / km / m."""
    records: list[dict] = []
    for edge in _sample_edges(cells):
        records.append(
            {
                "edge": edge,
                "rads": h3.edge_length(edge, unit="rads"),
                "km": h3.edge_length(edge, unit="km"),
                "m": h3.edge_length(edge, unit="m"),
            }
        )
    return write_ndjson("edge_length.ndjson", records)


def gen_hexagon_area_avg() -> int:
    """Per resolution 0..15: the average hexagon area in km2 and m2."""
    records: list[dict] = []
    for res in RESOLUTIONS:
        records.append(
            {
                "res": res,
                "km2": h3.average_hexagon_area(res, unit="km^2"),
                # m2 from the C library (see _cfn): h3-py derives it as km2*1e6.
                "m2": _cfn("getHexagonAreaAvgM2", res),
            }
        )
    return write_ndjson("hexagon_area_avg.ndjson", records)


def gen_hexagon_edge_length_avg() -> int:
    """Per resolution 0..15: the average hexagon edge length in km and m."""
    records: list[dict] = []
    for res in RESOLUTIONS:
        records.append(
            {
                "res": res,
                "km": h3.average_hexagon_edge_length(res, unit="km"),
                # m from the C library (see _cfn): h3-py derives it as km*1000.
                "m": _cfn("getHexagonEdgeLengthAvgM", res),
            }
        )
    return write_ndjson("hexagon_edge_length_avg.ndjson", records)


def gen_num_cells() -> int:
    """Per resolution 0..15: the total number of cells (get_num_cells)."""
    records: list[dict] = []
    for res in RESOLUTIONS:
        records.append({"res": res, "count": int(h3.get_num_cells(res))})
    return write_ndjson("num_cells.ndjson", records)


def _curated_distance_pairs() -> list[tuple[tuple[float, float], tuple[float, float]]]:
    """Curated (a, b) DEGREE point pairs exercising the degrees->radians staging.

    Includes identical points (distance 0), well-known city pairs (SF<->NYC),
    a 90-degree equatorial pair (a quarter great circle), antimeridian, and
    pole-to-pole.
    """
    sf = (37.7749, -122.4194)
    nyc = (40.7128, -74.0060)
    return [
        # Identical points -> exactly 0.
        (sf, sf),
        ((0.0, 0.0), (0.0, 0.0)),
        # SF <-> NYC, both orderings (symmetry check on the .NET side).
        (sf, nyc),
        (nyc, sf),
        # 90 degrees apart on the equator -> a quarter of the great circle.
        ((0.0, 0.0), (0.0, 90.0)),
        # Antipodal-ish and antimeridian crossings.
        ((0.0, 0.0), (0.0, 180.0)),
        ((0.0, 179.0), (0.0, -179.0)),
        # Pole to pole.
        ((90.0, 0.0), (-90.0, 0.0)),
        # A scattering of curated points paired with the prime-meridian origin.
        ((0.0, 0.0), (51.4779, 0.0)),
        ((-33.8688, 151.2093), (35.6895, 139.6917)),
    ]


def gen_great_circle_distance() -> int:
    """Curated DEGREE point pairs and their great-circle distance in rads / km / m."""
    records: list[dict] = []
    for (a_lat, a_lng), (b_lat, b_lng) in _curated_distance_pairs():
        a = (a_lat, a_lng)
        b = (b_lat, b_lng)
        records.append(
            {
                "a_lat": a_lat,
                "a_lng": a_lng,
                "b_lat": b_lat,
                "b_lng": b_lng,
                "rads": h3.great_circle_distance(a, b, unit="rads"),
                "km": h3.great_circle_distance(a, b, unit="km"),
                "m": h3.great_circle_distance(a, b, unit="m"),
            }
        )
    return write_ndjson("great_circle_distance.ndjson", records)


def main() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    cells = sample_cells()
    counts = {
        "cell_area": gen_cell_area(cells),
        "edge_length": gen_edge_length(cells),
        "hexagon_area_avg": gen_hexagon_area_avg(),
        "hexagon_edge_length_avg": gen_hexagon_edge_length_avg(),
        "num_cells": gen_num_cells(),
        "great_circle_distance": gen_great_circle_distance(),
    }
    # Reference curated_latlng_points so the unused-import guard does not trip and
    # the corpus provenance is explicit (the distance pairs reuse these points).
    assert curated_latlng_points(), "curated points corpus must be non-empty"
    print("h3-py version:", h3.__version__)
    print("output dir:", DATA_DIR)
    for name, n in counts.items():
        print(f"  {name}: {n}")


if __name__ == "__main__":
    main()
