"""Golden step responses and ZOH matrices for the C# tests.

Run from the repo root:  reference/.venv/bin/python reference/generate.py
Delay is modelled by Pade(5) here; the C# core uses a pure DelayLine, so the two differ near t = L.
"""
import json
import sys
from pathlib import Path

import control
import numpy as np
import scipy
import scipy.signal

HERE = Path(__file__).parent
GOLDEN = HERE / "golden"


def realization(p):
    """Controllable canonical form, same formulas as PlantModel.cs: ((A, B, C, D), delay)."""
    def second_order(b1, b0, a1, a0):
        return np.array([[0, 1], [-a0, -a1]]), np.array([[0], [1]]), np.array([[b0, b1]]), np.array([[0.0]])

    def lags(k, t1, t2, beta=0.0):
        q = t1 * t2
        return second_order(-k * beta / q, k / q, (t1 + t2) / q, 1 / q)

    t = p["type"]
    if t == "Fopdt":
        return (np.array([[-1 / p["T"]]]), np.array([[p["K"] / p["T"]]]), np.array([[1.0]]), np.array([[0.0]])), p["L"]
    if t == "TwoTank":
        return lags(p["K"], p["T1"], p["T2"]), p["L"]
    if t == "DcMotor":
        return lags(p["K"], p["Tm"], p["Te"]), 0.0
    if t == "Integrating":
        return (np.array([[0.0]]), np.array([[1.0]]), np.array([[p["K"]]]), np.array([[0.0]])), p["L"]
    if t == "InverseResponse":
        return lags(p["K"], p["T1"], p["T2"], p["Beta"]), 0.0
    raise ValueError(t)


def transfer_function(p):
    """Independent route to y: transfer function straight from the textbook formula, times Pade(L)."""
    s = control.tf("s")
    t = p["type"]
    if t == "Fopdt":
        g = p["K"] / (p["T"] * s + 1)
    elif t == "TwoTank":
        g = p["K"] / ((p["T1"] * s + 1) * (p["T2"] * s + 1))
    elif t == "DcMotor":
        g = p["K"] / ((p["Tm"] * s + 1) * (p["Te"] * s + 1))
    elif t == "Integrating":
        g = p["K"] / s
    elif t == "InverseResponse":
        g = p["K"] * (1 - p["Beta"] * s) / ((p["T1"] * s + 1) * (p["T2"] * s + 1))
    else:
        raise ValueError(t)
    delay = p.get("L", 0)
    if delay > 0:
        num, den = control.pade(delay, 5)
        g = g * control.tf(num, den)
    return g


def fmt(v):
    return f"{v:.11e}"


def main():
    spec = json.loads((HERE / "cases.json").read_text())
    amp = spec["amplitude"]
    GOLDEN.mkdir(exist_ok=True)
    zoh, manifest_cases = {}, []

    for c in spec["cases"]:
        n = round(c["horizon"] / c["dt"])
        t = np.arange(n) * c["dt"]
        u = np.full(n, amp)
        y = np.asarray(control.forced_response(transfer_function(c["plant"]), t, u).outputs).ravel()
        lines = ["t,u,y"] + [f"{fmt(a)},{fmt(b)},{fmt(d)}" for a, b, d in zip(t, u, y)]
        (GOLDEN / f"{c['id']}.csv").write_text("\n".join(lines) + "\n", newline="\n")

        (a, b, cc, d), _ = realization(c["plant"])
        ad, bd, _, _, _ = scipy.signal.cont2discrete((a, b, cc, d), c["dt"], method="zoh")
        zoh[c["id"]] = {
            "dt": c["dt"],
            "A": a.tolist(), "B": b.ravel().tolist(), "C": cc.ravel().tolist(), "D": float(d[0, 0]),
            "Ad": ad.tolist(), "Bd": bd.ravel().tolist(),
        }
        manifest_cases.append({**c, "n": n})

    (GOLDEN / "zoh.json").write_text(json.dumps(zoh, indent=2) + "\n", newline="\n")
    manifest = {
        "amplitude": amp,
        "versions": {
            "python": ".".join(map(str, sys.version_info[:3])),
            "numpy": np.__version__, "scipy": scipy.__version__, "control": control.__version__,
        },
        "cases": manifest_cases,
    }
    (GOLDEN / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", newline="\n")


if __name__ == "__main__":
    main()
