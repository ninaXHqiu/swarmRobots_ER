#!/usr/bin/env python3
"""Plot a recording made with the Physio panel (Download CSV): heart rate and skin conductance over time,
with the routine and its sections marked.

    python3 tools/plot_physio.py physio_2026-11-14T10-30-00.csv            # writes physio_..._plot.png next to it
    python3 tools/plot_physio.py recording.csv --out result.png --show       # also opens a window

Needs numpy and matplotlib. The CSV columns are described in SHIMMER.md.
"""
import argparse, csv, sys
import numpy as np
import matplotlib
import matplotlib.pyplot as plt


def load(path):
    samples, markers = [], []
    with open(path, newline="") as f:
        for r in csv.DictReader(f):
            t = float(r["t_s"]) if r["t_s"] != "" else None
            if t is None:
                continue
            if r["type"] == "sample":
                num = lambda k: float(r[k]) if r[k] not in ("", None) else np.nan
                samples.append((t, num("ppg_raw"), num("eda_us"), num("hr_bpm"), int(r["beat"] or 0)))
            elif r["type"] == "marker":
                markers.append((t, r["marker"], r["routine"]))
    return np.array(samples, float), markers


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("csv"); ap.add_argument("--out"); ap.add_argument("--show", action="store_true")
    a = ap.parse_args()
    if not a.show:
        matplotlib.use("Agg")
    s, markers = load(a.csv)
    if len(s) == 0:
        sys.exit("No sample rows in " + a.csv)
    t, ppg, eda, hr = s[:, 0], s[:, 1], s[:, 2], s[:, 3]
    fig, ax = plt.subplots(3, 1, figsize=(12, 8), sharex=True, gridspec_kw={"height_ratios": [2, 2, 1]})
    ax[0].plot(t, hr, color="#dc2626", lw=1.4); ax[0].set_ylabel("heart rate (beats/min)"); ax[0].set_ylim(40, 160)
    ax[1].plot(t, eda, color="#2563eb", lw=1.2); ax[1].set_ylabel("skin conductance (uS)")
    ax[2].plot(t, ppg, color="#444", lw=0.5); ax[2].set_ylabel("pulse signal (raw)"); ax[2].set_xlabel("seconds from the start of the recording")
    # routine bands and section lines
    start = None; name = ""
    colors = ["#c2410c", "#2563eb"]; n = 0
    for mt, text, routine in markers:
        if text.startswith("routine start"):
            start, name = mt, text.replace("routine start: ", "")
        elif text == "routine end" and start is not None:
            for x in ax:
                x.axvspan(start, mt, color=colors[n % 2], alpha=0.08)
            ax[0].text(start, 158, " " + name, va="top", fontsize=8); start = None; n += 1
        elif text.startswith("section: "):
            for x in ax:
                x.axvline(mt, color="#c2410c", lw=0.7, alpha=0.7)
            ax[0].text(mt, 44, text[9:], rotation=90, va="bottom", fontsize=7, color="#c2410c")
    if start is not None:
        for x in ax:
            x.axvspan(start, t[-1], color=colors[n % 2], alpha=0.08)
    ax[0].set_title(a.csv)
    fig.tight_layout()
    out = a.out or a.csv.rsplit(".", 1)[0] + "_plot.png"
    fig.savefig(out, dpi=130)
    print("wrote", out)
    if a.show:
        plt.show()


if __name__ == "__main__":
    main()
