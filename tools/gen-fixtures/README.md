# gen-fixtures

Generates the committed ground-truth test corpus for `H3NET.Native` using
[`h3-py`](https://github.com/uber/h3-py) v4 (the same C library, `libh3 4.5.0`,
that the .NET binding loads).

The generated data lives in
`tests/H3NET.Native.Tests/Fixtures/data/` and **is committed**. The `.venv`
created below is gitignored.

## Regenerate

```sh
python -m venv .venv
.venv/bin/pip install -r requirements.txt
.venv/bin/python gen_fixtures.py
```

The generator is deterministic (fixed seed in `gen_fixtures.py`). Re-running it
with the same pinned `h3` version reproduces byte-identical output.

## Output

| File | Contents |
| --- | --- |
| `latlng_to_cell.ndjson` | `{lat, lng, res, cell}` across all 16 resolutions plus curated antimeridian/pole/prime-meridian cases and a seeded random sweep. |
| `cell_to_latlng.ndjson` | `{cell, lat, lng}`. |
| `cell_to_boundary.ndjson` | `{cell, verts: [[lat,lng],...]}` including pentagons (5-vertex case) and Class III cells. |
| `grid_disk.ndjson` | `{cell, k, cells}` for `k` in 0..3 over a mix including pentagons. |
| `polygon_to_cells.ndjson` | `{polygon:{exterior,holes}, res, cells}` for a bbox, a triangle, and a polygon with a hole. |
| `res0_cells.csv` | All 122 resolution-0 cells, one hex per line. |
| `pentagons.csv` | `pentagon,res` header; pentagons across all resolutions. |
| `manifest.json` | `h3_py_version`, `libh3_version`, `seed`, `generated_counts`, `note`. |

## Conventions

- Angular values are **degrees** (matching the `H3NET.Native` public surface).
- Cells are zero-padded 16-character lowercase hex (`%016x`) so they map
  directly to a .NET `ulong`.
- Doubles are emitted at full precision so they round-trip exactly.
