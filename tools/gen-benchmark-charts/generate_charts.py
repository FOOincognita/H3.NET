#!/usr/bin/env python3
"""Generate the H3.NET.Native benchmark charts (PNG + SVG) from a results snapshot.

Usage:
    python3 generate_charts.py <benchmark-data.json> <output-dir>

Regenerate after a fresh benchmark run:
    dotnet run --project tests/H3.NET.Native.Benchmarks -c Release -- --filter '*' --artifacts <dir>
then transcribe the *-report-github.md means/allocations into benchmark-data.json and re-run this.

Palette is the validated dataviz default (blue #2a78d6 = H3.NET.Native, orange #eb6834 =
pocketken.H3); colorblind separation verified with the skill's validate_palette.js (ΔE 96.7).
Charts are light-surface (render correctly on GitHub light/dark, nuget.org, and the DocFX site).
"""
import json
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.ticker import FuncFormatter

# --- validated palette (dataviz reference instance, light surface) --------------
NATIVE = "#2a78d6"   # categorical slot 1 (blue) -> H3.NET.Native (the hero series)
POCKET = "#eb6834"   # categorical slot 8 (orange) -> pocketken.H3
SURFACE = "#fcfcfb"
INK = "#0b0b0b"
INK2 = "#52514e"
MUTED = "#898781"
GRID = "#e1e0d9"
AXIS = "#c3c2b7"

plt.rcParams.update({
    "font.family": "sans-serif",
    "font.sans-serif": ["Helvetica Neue", "Helvetica", "Arial", "DejaVu Sans"],
    "figure.facecolor": SURFACE,
    "axes.facecolor": SURFACE,
    "savefig.facecolor": SURFACE,
    "text.color": INK,
    "axes.edgecolor": AXIS,
    "axes.labelcolor": INK2,
    "xtick.color": MUTED,
    "ytick.color": MUTED,
    "xtick.labelsize": 9,
    "ytick.labelsize": 9,
    "axes.linewidth": 0.8,
})

PROV = "Apple M3 Pro  ·  .NET 10  ·  BenchmarkDotNet 0.15.8  ·  Uber H3 v4.5.0"


def _bytes(b):
    if b == 0:
        return "0 B"
    if b < 1024:
        return f"{b} B"
    if b < 1024 * 1024:
        return f"{b / 1024:.1f} KB"
    return f"{b / (1024 * 1024):.1f} MB"


def _style(ax):
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.spines["left"].set_color(AXIS)
    ax.spines["bottom"].set_color(AXIS)
    ax.tick_params(length=0)


def _title(ax, title, subtitle):
    # Large pad lifts the bold title well clear of the subtitle, which sits just
    # above the axes; bbox_inches="tight" grows the crop to include both.
    ax.set_title(title, color=INK, fontsize=14, fontweight="bold", loc="left", pad=44)
    ax.text(0, 1.02, subtitle, transform=ax.transAxes, color=INK2, fontsize=10.5,
            va="bottom", ha="left")


def _footer(fig):
    fig.text(0.008, 0.008, PROV, color=MUTED, fontsize=7.5, va="bottom", ha="left")


def _save(fig, out_dir, name):
    fig.savefig(f"{out_dir}/{name}.png", dpi=200, bbox_inches="tight", pad_inches=0.28)
    fig.savefig(f"{out_dir}/{name}.svg", bbox_inches="tight", pad_inches=0.28)
    plt.close(fig)


def chart_crossover(data, out_dir):
    rows = data["polygon_sweep"]["rows"]
    cells = [r["cells"] for r in rows]
    native = [r["native_us"] for r in rows]
    pocket = [r["pocketken_us"] for r in rows]

    fig, ax = plt.subplots(figsize=(8.2, 5.0))
    ax.plot(cells, native, color=NATIVE, lw=2.2, marker="o", ms=6.5, mfc=NATIVE,
            mec=SURFACE, mew=1.4, label="H3.NET.Native", zorder=3)
    ax.plot(cells, pocket, color=POCKET, lw=2.2, marker="o", ms=6.5, mfc=POCKET,
            mec=SURFACE, mew=1.4, label="pocketken.H3", zorder=3)

    ax.set_xscale("log")
    ax.set_yscale("log")

    # crossover marker (log-log interpolation of the equal-time point ~650 cells)
    xc = 650
    ax.axvline(xc, color=AXIS, lw=1.0, zorder=1)
    ax.annotate("native pulls ahead\n(~650 cells)", xy=(xc, native[3]), xytext=(xc * 1.5, 12),
                color=INK2, fontsize=9.5, ha="left", va="center")

    ax.grid(axis="both", color=GRID, lw=0.8, zorder=0)
    ax.set_axisbelow(True)
    _style(ax)
    ax.set_xlabel("output cell count  (same polygon, increasing resolution)")
    ax.set_ylabel("time per fill  (microseconds, log)")
    ax.xaxis.set_major_formatter(FuncFormatter(lambda v, _: f"{int(v):,}" if v >= 1 else f"{v:g}"))
    ax.yaxis.set_major_formatter(FuncFormatter(lambda v, _: f"{v:g}"))

    leg = ax.legend(loc="upper left", frameon=False, fontsize=10.5, labelcolor=INK)
    _title(ax, "Filling a polygon: native wins at scale",
            "Native overtakes pocketken.H3 past ~650 output cells and leads 1.4-1.6x at scale.")
    _footer(fig)
    _save(fig, out_dir, "polygon-crossover")


def chart_overhead(data, out_dir):
    rows = {(r["op"], r["impl"]): r for r in data["small_workloads"]["rows"]}
    ops = ["LatLngToCell", "GridDisk"]
    native = [rows[(o, "H3.NET.Native")]["ratio"] for o in ops]
    pocket = [rows[(o, "pocketken.H3")]["ratio"] for o in ops]

    x = range(len(ops))
    w = 0.34
    fig, ax = plt.subplots(figsize=(7.4, 4.8))
    b1 = ax.bar([i - w / 2 - 0.012 for i in x], native, w, color=NATIVE, label="H3.NET.Native", zorder=3)
    b2 = ax.bar([i + w / 2 + 0.012 for i in x], pocket, w, color=POCKET, label="pocketken.H3", zorder=3)

    ax.axhline(1.0, color=AXIS, lw=1.1, zorder=2)
    # Sit the reference label in the empty gap between the two bar groups.
    ax.text(0.5, 1.012, "raw libh3 (C floor)", color=INK2, fontsize=8.5, va="bottom", ha="center")

    for bars in (b1, b2):
        for rect in bars:
            ax.text(rect.get_x() + rect.get_width() / 2, rect.get_height() + 0.02,
                    f"{rect.get_height():.2f}x", ha="center", va="bottom", color=INK, fontsize=9.5)

    ax.set_xticks(list(x))
    ax.set_xticklabels(ops, color=INK2, fontsize=10.5)
    ax.set_ylim(0, max(pocket) * 1.18)
    ax.grid(axis="y", color=GRID, lw=0.8, zorder=0)
    ax.set_axisbelow(True)
    _style(ax)
    ax.set_ylabel("time relative to raw libh3  (lower is better)")
    ax.legend(loc="upper left", frameon=False, fontsize=10.5, labelcolor=INK)
    _title(ax, "Indexing & traversal run at the raw-C floor",
            "Within ~1-9% of direct P/Invoke; pocketken.H3 pays 25-67% more.")
    _footer(fig)
    _save(fig, out_dir, "overhead-vs-raw")


def chart_allocation(data, out_dir):
    rows = {(r["op"], r["impl"]): r for r in data["small_workloads"]["rows"]}
    ops = ["LatLngToCell", "GridDisk", "PolygonToCells"]
    native = [rows[(o, "H3.NET.Native")]["alloc_b"] for o in ops]
    pocket = [rows[(o, "pocketken.H3")]["alloc_b"] for o in ops]

    x = range(len(ops))
    w = 0.34
    fig, ax = plt.subplots(figsize=(7.8, 4.8))
    floor = 60  # log-y visual floor; true zero is annotated, not drawn
    b1 = ax.bar([i - w / 2 - 0.012 for i in x], [max(v, floor) for v in native], w,
                color=NATIVE, label="H3.NET.Native", zorder=3)
    b2 = ax.bar([i + w / 2 + 0.012 for i in x], [max(v, floor) for v in pocket], w,
                color=POCKET, label="pocketken.H3", zorder=3)

    ax.set_yscale("log")
    for bars, vals in ((b1, native), (b2, pocket)):
        for rect, v in zip(bars, vals):
            ax.text(rect.get_x() + rect.get_width() / 2, rect.get_height() * 1.08,
                    _bytes(v), ha="center", va="bottom", color=INK, fontsize=9.5)

    ax.set_xticks(list(x))
    ax.set_xticklabels(ops, color=INK2, fontsize=10.5)
    ax.set_ylim(floor, max(pocket) * 3)
    ax.grid(axis="y", color=GRID, lw=0.8, zorder=0)
    ax.set_axisbelow(True)
    _style(ax)
    ax.set_ylabel("bytes allocated per op  (log, lower is better)")
    ax.legend(loc="upper left", frameon=False, fontsize=10.5, labelcolor=INK)
    _title(ax, "Managed allocation: up to 46x less, often zero",
            "Indexing allocates nothing; the pooled polygon path drops to 608 B.")
    _footer(fig)
    _save(fig, out_dir, "allocation")


def main():
    data_path, out_dir = sys.argv[1], sys.argv[2]
    with open(data_path) as f:
        data = json.load(f)
    chart_crossover(data, out_dir)
    chart_overhead(data, out_dir)
    chart_allocation(data, out_dir)
    print(f"wrote polygon-crossover, overhead-vs-raw, allocation (.png + .svg) to {out_dir}")


if __name__ == "__main__":
    main()
