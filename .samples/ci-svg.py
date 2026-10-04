#!/usr/bin/env python3
"""Draws the CI of a sample branch by day, as the samples page of the template's site draws it:

    ci-svg.py <reports/branch folder> <branch> <out.svg>

The README of a branch shows the picture: GitHub shows images there without running a script, so the grid
of the site cannot be the page itself. The same data (every run of every version in the folder), the same
rules and the same colours as site/samples.js and samples.css of DotNetSolutionKit:

- a day keeps the result of the branch's last run; a pass fades to ice and a failure rots to dark red over
  30 days without a run, the colours mixed in OKLab as the site's color-mix does;
- 52 weeks back, the weeks to come fading out, Monday at the top;
- light and dark follow the reader's colour scheme.

Days are UTC: the picture is drawn once, for every reader.
"""
import datetime as dt
import glob
import json
import os
import sys

FADE_DAYS = 30
PAST_WEEKS = 52
FUTURE_WEEKS = 8
CELL, GAP = 11, 3
LEFT, TOP = 34, 46

THEMES = {
    "dark": {"ok": "#3fb950", "bad": "#e08d79", "sleep": "#8ec5e0", "rot": "#4a1010", "empty": "#141d33",
             "ground": "#0e1526", "text": "#e6ecf5", "muted": "#93a2bc"},
    "light": {"ok": "#1f9d4c", "bad": "#cf3b2e", "sleep": "#74b3d6", "rot": "#5c1414", "empty": "#eef2fa",
              "ground": "#ffffff", "text": "#14203a", "muted": "#5a6a8a"},
}


# ---- colour: sRGB <-> OKLab, as CSS color-mix(in oklab, a p%, b) -------------------------------------
def _to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _to_srgb(c):
    c = max(0.0, min(1.0, c))
    return 12.92 * c if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def _oklab(hex_colour):
    r, g, b = (_to_linear(int(hex_colour[i:i + 2], 16) / 255) for i in (1, 3, 5))
    l = (0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b) ** (1 / 3)
    m = (0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b) ** (1 / 3)
    s = (0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b) ** (1 / 3)
    return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s)


def _hex(lab):
    L, a, b = lab
    l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3
    m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3
    s = (L - 0.0894841775 * a - 1.2914855480 * b) ** 3
    rgb = (4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
           -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
           -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s)
    return "#" + "".join(f"{round(_to_srgb(c) * 255):02x}" for c in rgb)


def mix(towards, share, base):
    """color-mix(in oklab, towards share, base)."""
    a, b = _oklab(towards), _oklab(base)
    return _hex(tuple(x * share + y * (1 - share) for x, y in zip(a, b)))


# ---- the days -------------------------------------------------------------------------------------------
def runs_of(folder):
    runs = []
    for path in glob.glob(os.path.join(folder, "*.json")):
        if os.path.basename(path) == "index.json":
            continue
        report = json.load(open(path, encoding="utf-8"))
        runs += [dict(run, version=report.get("version")) for run in report.get("runs", [])]
    return sorted((r for r in runs if r.get("started")), key=lambda r: r["started"])


def day_of(stamp):
    return dt.date.fromisoformat(stamp[:10])


def cells(runs, today):
    start = today - dt.timedelta(days=7 * PAST_WEEKS + today.weekday())
    end = today + dt.timedelta(days=7 * FUTURE_WEEKS + (6 - today.weekday()))
    last, i, n, out = None, 0, 0, []
    d = start
    while d <= end:
        cell = {"week": n // 7, "weekday": n % 7, "day": d}
        if d > today:
            cell["future"] = (d - today).days
        else:
            while i < len(runs) and day_of(runs[i]["started"]) <= d:
                last, i = runs[i], i + 1
            if last:
                cell["ok"] = last.get("conclusion") == "success"
                cell["age"] = (d - day_of(last["started"])).days
        out.append(cell)
        d += dt.timedelta(days=1)
        n += 1
    return out


def fill(cell, theme):
    t = THEMES[theme]
    if "future" in cell:
        return t["empty"], max(0.15, 1 - cell["future"] / (7 * FUTURE_WEEKS))
    if "ok" not in cell:
        return t["empty"], 1.0
    fade = min(cell["age"] / FADE_DAYS, 1)
    return (mix(t["sleep"], fade, t["ok"]) if cell["ok"] else mix(t["rot"], fade, t["bad"])), 1.0


# ---- the picture ----------------------------------------------------------------------------------------
def svg(branch, runs, today):
    grid = cells(runs, today)
    weeks = grid[-1]["week"] + 1
    width = LEFT + weeks * (CELL + GAP) + 12
    legend_y = TOP + 7 * (CELL + GAP) + 18
    height = legend_y + 26

    last = runs[-1] if runs else None
    if last:
        tests = last.get("tests") or {}
        cov = last.get("coverage") or {}
        result = "passed" if last.get("conclusion") == "success" else "failed"
        facts = (f"last run {last['started'][:10]}: {result}, tests {tests.get('passed', 0)} / {tests.get('total', 0)}"
                 + (f", coverage {cov.get('line')}% lines, {cov.get('branch')}% branches" if cov else ""))
    else:
        facts = "no run yet"

    css = ["text{font:11px system-ui,-apple-system,'Segoe UI',sans-serif}",
           ".t{font-size:13px;font-weight:600}"]
    for theme in ("light", "dark"):
        t = THEMES[theme]
        rules = [f".g{{fill:{t['ground']}}}", f".x{{fill:{t['text']}}}", f".m{{fill:{t['muted']}}}"]
        block = "\n".join(rules)
        css.append(block if theme == "light" else "@media (prefers-color-scheme: dark){" + block + "}")

    # one class per colour of a theme: a cell names its light colour's class and its dark one's
    light = {c: k for k, c in enumerate(sorted({fill(c, "light")[0] for c in grid}))}
    dark = {c: k for k, c in enumerate(sorted({fill(c, "dark")[0] for c in grid}))}
    css_dark_alias = []
    body = [f'<rect class="g" width="{width}" height="{height}" rx="6"/>',
            f'<text class="t x" x="{LEFT}" y="20">CI of {branch} by day</text>',
            f'<text class="m" x="{LEFT}" y="35">{facts}</text>']
    for label, row in (("Mon", 0), ("Wed", 2), ("Fri", 4)):
        body.append(f'<text class="m" x="4" y="{TOP + row * (CELL + GAP) + CELL - 2}">{label}</text>')
    shown = set()
    for cell in grid:
        x = LEFT + cell["week"] * (CELL + GAP)
        y = TOP + cell["weekday"] * (CELL + GAP)
        lc, op = fill(cell, "light")
        dc, _ = fill(cell, "dark")
        cls = f"l{light[lc]}d{dark[dc]}"
        if cls not in shown:
            shown.add(cls)
            css_dark_alias.append((cls, light[lc], dark[dc]))
        title = cell["day"].isoformat()
        if "ok" in cell:
            title += (" passed" if cell["ok"] else " failed") + (f", last run {cell['age']} days before" if cell["age"] else "")
        body.append(f'<rect class="{cls}" x="{x}" y="{y}" width="{CELL}" height="{CELL}" rx="2"'
                    + (f' opacity="{op:.2f}"' if op < 1 else "") + f'><title>{title}</title></rect>')

    # the legend: what a pass and a failure become without a run
    lx = LEFT
    for label, colours in (("passed", ("ok", "sleep")), ("failed", ("bad", "rot"))):
        body.append(f'<text class="m" x="{lx}" y="{legend_y + 9}">{label}</text>')
        lx += 44
        for k in range(5):
            share = k / 4
            body.append(f'<rect class="L{label}{k}" x="{lx}" y="{legend_y}" width="{CELL}" height="{CELL}" rx="2"/>')
            for theme in ("light", "dark"):
                t = THEMES[theme]
                colour = mix(t[colours[1]], share, t[colours[0]])
                rule = f".L{label}{k}{{fill:{colour}}}"
                css.append(rule if theme == "light" else "@media (prefers-color-scheme: dark){" + rule + "}")
            lx += CELL + GAP
        body.append(f'<text class="m" x="{lx + 4}" y="{legend_y + 9}">over {FADE_DAYS} days without a run</text>')
        lx += 190

    for cls, lk, dk in css_dark_alias:
        lfill = sorted({fill(c, "light")[0] for c in grid})[lk]
        dfill = sorted({fill(c, "dark")[0] for c in grid})[dk]
        css.append(f".{cls}{{fill:{lfill}}}")
        css.append("@media (prefers-color-scheme: dark){." + cls + "{fill:" + dfill + "}}")

    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" '
            f'role="img" aria-label="CI of {branch} by day: {facts}">\n'
            f'<style>{chr(10).join(css)}</style>\n' + "\n".join(body) + "\n</svg>\n")


def main():
    if len(sys.argv) != 4:
        raise SystemExit(__doc__)
    folder, branch, out = sys.argv[1:]
    today = dt.datetime.now(dt.timezone.utc).date()
    open(out, "w", encoding="utf-8", newline="\n").write(svg(branch, runs_of(folder), today))


if __name__ == "__main__":
    main()
