# gen-fixtures

Generates the committed ground-truth test corpus for `H3.NET.Native` using
[`h3-py`](https://github.com/uber/h3-py) v4 (the same C library, `libh3 4.5.0`,
that the .NET binding loads).

The generated data lives in
`tests/H3.NET.Native.Tests/Fixtures/data/` and **is committed**. The `.venv`
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

### Supplementary generators (reuse the same `sample_cells()` corpus)

Run after `gen_fixtures.py` (they import its helpers and write into the same data dir):

```sh
.venv/bin/python gen_inspection_fixtures.py     # index_digits + icosahedron_faces
.venv/bin/python gen_hierarchy_fixtures.py      # hierarchy + child_pos + compact
.venv/bin/python gen_grid_traversal_fixtures.py # grid_ring + grid_path + grid_distance + local_ij + grid_disk_distances
```

| File | Contents |
| --- | --- |
| `grid_ring.ndjson` | `{cell, k, cells}` hollow ring at exactly distance `k` (unordered set). |
| `grid_path.ndjson` | `{start, end, path}` ORDERED, endpoint-inclusive line (`path[0]==start`, `path[-1]==end`). |
| `grid_distance.ndjson` | `{origin, other, distance}` over the same pairs as `grid_path`. |
| `local_ij.ndjson` | `{origin, target, i, j}` local IJ of `target` relative to `origin` (mode 0). |
| `grid_disk_distances.ndjson` | `{cell, k, cells, distances}` parallel arrays; `distances[i]` is the grid distance of `cells[i]`. Derived from `grid_disk` via concentric set arithmetic (h3-py 4.5.0 has no `grid_disk_distances`). |

## Conventions

- Angular values are **degrees** (matching the `H3.NET.Native` public surface).
- Cells are zero-padded 16-character lowercase hex (`%016x`) so they map
  directly to a .NET `ulong`.
- Doubles are emitted at full precision so they round-trip exactly.
