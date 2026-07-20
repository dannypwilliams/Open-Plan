"""Validate OPEN PLAN Blender sources and FBX exports without importing .blend into Unity."""

import argparse
import json
import sys
from pathlib import Path


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    return parser.parse_args(args)


def main():
    project = Path(parse_args().project_root).resolve()
    manifest_path = project / "Tools" / "Blender" / "asset_manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    failures = []
    for record in manifest["assets"]:
        for key in ("fbx", "unity", "source"):
            path = Path(record[key])
            if not path.exists() or path.stat().st_size < 256:
                failures.append(f"{record['name']}: missing/empty {key} {path}")
        if record["objects"] < 2:
            failures.append(f"{record['name']}: expected a root and authored mesh")
    required = {
        "Worker", "Desk_A", "OfficeChair", "WaterCooler", "CoffeeMachine", "Elevator", "CardboardBox", "FloorSlab",
        "DamagedDesk", "CheapCRTMonitor", "CheapVendingMachine", "Ashtray", "Cigarette", "NeighborSign", "ConnectingWallTrim",
    }
    found = {record["name"] for record in manifest["assets"]}
    failures.extend(f"required asset absent: {name}" for name in sorted(required - found))
    report = {
        "status": "PASS" if not failures else "FAIL",
        "asset_count": manifest["asset_count"],
        "failures": failures,
    }
    report_path = project / "Tools" / "Blender" / "Logs" / "validation.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    raise SystemExit(0 if not failures else 1)


if __name__ == "__main__":
    main()
