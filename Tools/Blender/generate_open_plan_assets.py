"""Generate OPEN PLAN's low-poly FBX asset library in Blender 5.x background mode."""

import argparse
import json
import math
import shutil
import sys
from pathlib import Path

import bpy


PALETTE = {
    "walnut": (0.163, 0.063, 0.036, 1),
    "light_wood": (0.42, 0.22, 0.10, 1),
    "cream": (0.68, 0.57, 0.39, 1),
    "burgundy": (0.145, 0.035, 0.052, 1),
    "carpet": (0.025, 0.026, 0.030, 1),
    "metal": (0.10, 0.085, 0.075, 1),
    "dark": (0.012, 0.016, 0.018, 1),
    "cyan": (0.055, 0.48, 0.55, 1),
    "blue": (0.045, 0.24, 0.36, 1),
    "amber": (1.0, 0.32, 0.08, 1),
    "green": (0.16, 0.55, 0.30, 1),
    "paper": (0.88, 0.80, 0.63, 1),
    "cardboard": (0.48, 0.24, 0.08, 1),
    "leaf": (0.055, 0.24, 0.14, 1),
    "skin": (0.68, 0.34, 0.18, 1),
    "coral": (0.90, 0.18, 0.13, 1),
}


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--only", nargs="+", action="append", default=[])
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    return parser.parse_args(args)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def mat(name):
    material = bpy.data.materials.get("OP_" + name)
    if material is None:
        material = bpy.data.materials.new("OP_" + name)
        material.diffuse_color = PALETTE[name]
        material.roughness = 0.62
        if name in ("cyan", "blue", "amber", "green"):
            material.diffuse_color = PALETTE[name]
    return material


def root(name):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    return obj


def finish(obj, parent, material, bevel=0.05):
    obj.parent = parent
    if material:
        obj.data.materials.append(mat(material))
    if bevel > 0:
        modifier = obj.modifiers.new("Soft toy bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    obj.select_set(True)
    return obj


def box(name, loc, scale, parent, material="cream", bevel=0.05, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] / 2, scale[1] / 2, scale[2] / 2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, parent, material, min(bevel, min(scale) * 0.22))


def cylinder(name, loc, radius, depth, parent, material="metal", vertices=16, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, parent, material, min(0.035, radius * 0.2))


def sphere(name, loc, radius, parent, material="cream", scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, parent, material, 0)


def torus(name, loc, major, minor, parent, material="metal", rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=16, minor_segments=6, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, parent, material, 0)


def desk(name="Desk_A", variant=False):
    r = root(name)
    box("Top", (0, 0, 0.75), (1.65, 0.78, 0.12), r, "walnut", 0.06)
    for x in (-0.68, 0.68):
        for y in (-0.27, 0.27):
            box("Leg", (x, y, 0.36), (0.10, 0.10, 0.72), r, "metal", 0.025)
    if variant:
        box("Drawer", (-0.58, 0, 0.50), (0.36, 0.58, 0.45), r, "burgundy", 0.05)
    return r


def chair(name="OfficeChair"):
    r = root(name)
    cylinder("Post", (0, 0, 0.42), 0.07, 0.50, r)
    box("Seat", (0, 0, 0.68), (0.58, 0.55, 0.14), r, "burgundy", 0.07)
    box("Back", (0, 0.23, 1.02), (0.58, 0.14, 0.68), r, "burgundy", 0.08, (math.radians(-7), 0, 0))
    for a in range(5):
        angle = a * math.tau / 5
        box("WheelArm", (math.cos(angle) * 0.18, math.sin(angle) * 0.18, 0.18), (0.40, 0.055, 0.055), r, "metal", 0.018, (0, 0, angle))
        cylinder("Wheel", (math.cos(angle) * 0.34, math.sin(angle) * 0.34, 0.12), 0.055, 0.05, r, "dark", 12, (math.pi/2, 0, 0))
    return r


def monitor(name="Monitor"):
    r = root(name)
    box("ScreenFrame", (0, 0, 0.72), (0.76, 0.11, 0.50), r, "dark", 0.055)
    box("ScreenGlow", (0, -0.062, 0.72), (0.65, 0.025, 0.39), r, "blue", 0.025)
    box("Stem", (0, 0, 0.35), (0.09, 0.09, 0.28), r, "metal", 0.02)
    box("Base", (0, 0, 0.19), (0.40, 0.28, 0.055), r, "metal", 0.025)
    return r


def cheap_crt_monitor(name="CheapCRTMonitor"):
    r = root(name)
    box("BulkyCase", (0, 0, 0.56), (0.82, 0.58, 0.68), r, "cream", 0.09)
    box("ScreenBezel", (0, -0.306, 0.61), (0.64, 0.045, 0.44), r, "dark", 0.045)
    box("GreenScreen", (0, -0.334, 0.62), (0.53, 0.018, 0.33), r, "green", 0.04)
    cylinder("PowerKnob", (0.30, -0.35, 0.36), 0.045, 0.035, r, "burgundy", 10, (math.pi/2, 0, 0))
    box("HeavyBase", (0, 0.03, 0.16), (0.58, 0.48, 0.12), r, "metal", 0.04)
    return r


def damaged_desk(name="DamagedDesk"):
    r = root(name)
    box("ScuffedTop", (0, 0, 0.73), (1.65, 0.78, 0.12), r, "light_wood", 0.035, (0, math.radians(1.2), 0))
    box("LeftLeg", (-0.68, -0.25, 0.35), (0.11, 0.11, 0.70), r, "metal", 0.02)
    box("BackLeg", (-0.68, 0.25, 0.34), (0.11, 0.11, 0.68), r, "metal", 0.02)
    box("ShortLeg", (0.68, -0.25, 0.31), (0.11, 0.11, 0.61), r, "metal", 0.02)
    box("BoxBrace", (0.68, 0.25, 0.22), (0.34, 0.32, 0.44), r, "cardboard", 0.035)
    box("TapePatch", (0.30, -0.402, 0.75), (0.34, 0.018, 0.08), r, "paper", 0.008, (0, 0, math.radians(-7)))
    return r


def ashtray(name="Ashtray"):
    r = root(name)
    cylinder("Pedestal", (0, 0, 0.42), 0.08, 0.78, r, "metal", 12)
    cylinder("Bowl", (0, 0, 0.86), 0.30, 0.14, r, "dark", 16)
    torus("Rim", (0, 0, 0.94), 0.27, 0.035, r, "metal")
    cylinder("Ash", (0, 0, 0.95), 0.18, 0.018, r, "cream", 12)
    return r


def cigarette(name="Cigarette"):
    r = root(name)
    cylinder("PaperTube", (0, 0, 0.16), 0.035, 0.42, r, "paper", 12, (0, math.pi/2, 0))
    cylinder("Filter", (0.21, 0, 0.16), 0.038, 0.11, r, "amber", 12, (0, math.pi/2, 0))
    cylinder("Ember", (-0.215, 0, 0.16), 0.038, 0.025, r, "coral", 12, (0, math.pi/2, 0))
    return r


def neighbor_sign(name="NeighborSign"):
    r = root(name)
    for x in (-0.86, 0.86):
        box("Post", (x, 0, 0.72), (0.10, 0.10, 1.44), r, "metal", 0.025)
    box("ModestSign", (0, 0, 1.15), (2.05, 0.13, 0.86), r, "cardboard", 0.06)
    box("Inset", (0, -0.075, 1.15), (1.76, 0.025, 0.60), r, "dark", 0.025)
    return r


def connecting_wall_trim(name="ConnectingWallTrim"):
    r = root(name)
    box("BoardedOpening", (0, 0, 0.74), (3.35, 0.20, 1.38), r, "cardboard", 0.035)
    for x in (-1.72, 1.72):
        box("TrimSide", (x, 0, 0.78), (0.18, 0.30, 1.56), r, "metal", 0.035)
    box("TrimTop", (0, 0, 1.52), (3.62, 0.30, 0.18), r, "metal", 0.035)
    box("WarningStripe", (0, -0.13, 1.17), (2.15, 0.025, 0.13), r, "amber", 0.012, (0, 0, math.radians(-4)))
    return r


def cheap_vending_machine(name="CheapVendingMachine"):
    r = root(name)
    box("DentedBody", (0, 0, .91), (.92, .66, 1.82), r, "metal", .07, (0, 0, math.radians(.8)))
    box("FadedWindow", (-.10, -.344, 1.20), (.55, .035, .72), r, "blue", .025)
    for row in range(3):
        for col in range(3):
            box("Snack", (-.27 + col * .18, -.37, .98 + row * .20), (.12, .025, .11), r,
                ("amber", "paper", "burgundy")[(row + col) % 3], .012)
    box("CoinSlot", (.31, -.37, 1.15), (.12, .04, .30), r, "dark", .018)
    box("StuckTray", (0, -.39, .36), (.50, .14, .22), r, "dark", .03, (math.radians(-5), 0, 0))
    box("RepairTape", (.38, -.36, .66), (.12, .025, .34), r, "paper", .008, (0, 0, math.radians(7)))
    return r


def keyboard(name="Keyboard"):
    r = root(name)
    box("KeyboardBody", (0, 0, 0.045), (0.62, 0.23, 0.09), r, "dark", 0.035, (math.radians(5), 0, 0))
    for row in range(3):
        for col in range(9):
            box("Key", (-0.25 + col * 0.062, -0.065 + row * 0.062, 0.093), (0.048, 0.045, 0.018), r, "cream", 0.006)
    return r


def mouse(name="Mouse"):
    r = root(name)
    sphere("MouseBody", (0, 0, 0.045), 0.13, r, "dark", (0.7, 1.0, 0.36))
    return r


def lamp(name="DeskLamp"):
    r = root(name)
    cylinder("Base", (0, 0, 0.05), 0.21, 0.10, r, "metal")
    cylinder("Arm", (0, 0, 0.44), 0.035, 0.72, r, "metal", 12, (0, math.radians(-20), 0))
    cylinder("Shade", (0.13, 0, 0.80), 0.22, 0.25, r, "amber", 16, (0, math.pi/2, 0))
    return r


def mug(name="Mug"):
    r = root(name)
    cylinder("Cup", (0, 0, 0.12), 0.12, 0.24, r, "cream", 16)
    torus("Handle", (0.12, 0, 0.13), 0.085, 0.025, r, "cream", (math.pi/2, 0, 0))
    return r


def paper_stack(name="PaperStack"):
    r = root(name)
    for i in range(4):
        box("Paper", (0.006 * (i % 2), 0, 0.012 + i * 0.018), (0.34, 0.26, 0.018), r, "paper", 0.008, (0, 0, math.radians((i % 2) * 2 - 1)))
    return r


def file_tray(name="FileTray"):
    r = root(name)
    box("Base", (0, 0, 0.035), (0.40, 0.30, 0.07), r, "metal", 0.025)
    box("Back", (0, 0.135, 0.15), (0.40, 0.04, 0.28), r, "metal", 0.02)
    box("Paper", (0, 0, 0.10), (0.34, 0.24, 0.035), r, "paper", 0.01)
    return r


def desk_plant(name="DeskPlant"):
    r = root(name)
    cylinder("Pot", (0, 0, 0.12), 0.15, 0.24, r, "burgundy", 12)
    for i, angle in enumerate((-0.8, -0.3, 0.3, 0.8)):
        sphere("Leaf", (math.sin(angle) * 0.09, 0, 0.32 + i * 0.025), 0.14, r, "leaf", (0.45, 0.25, 1.2))
    return r


def cubicle(name="CubicleDivider"):
    r = root(name)
    box("Panel", (0, 0, 0.55), (1.75, 0.10, 1.10), r, "burgundy", 0.045)
    box("Cap", (0, 0, 1.12), (1.82, 0.14, 0.06), r, "metal", 0.025)
    return r


def nameplate(name="Nameplate"):
    r = root(name)
    box("Stand", (0, 0, 0.07), (0.42, 0.10, 0.14), r, "warm" if "warm" in PALETTE else "metal", 0.025, (math.radians(-12), 0, 0))
    return r


def water_cooler(name="WaterCooler"):
    r = root(name)
    box("Body", (0, 0, 0.72), (0.62, 0.55, 1.18), r, "cream", 0.10)
    cylinder("Bottle", (0, 0, 1.53), 0.26, 0.70, r, "cyan", 16)
    box("Dispense", (0, -0.29, 0.85), (0.34, 0.12, 0.30), r, "dark", 0.035)
    cylinder("Tap", (-0.09, -0.38, 0.92), 0.025, 0.15, r, "blue", 12, (math.pi/2, 0, 0))
    return r


def appliance(name, size, material, screen=True):
    r = root(name)
    box("Body", (0, 0, size[2] / 2), size, r, material, 0.10)
    if screen:
        box("Glow", (0, -size[1] / 2 - 0.012, size[2] * 0.68), (size[0] * 0.55, 0.025, size[2] * 0.16), r, "amber" if material != "blue" else "cyan", 0.02)
    box("Tray", (0, -size[1] / 2 - 0.05, size[2] * 0.30), (size[0] * 0.48, 0.12, size[2] * 0.18), r, "dark", 0.035)
    return r


def filing_cabinet(name="FilingCabinet"):
    r = root(name)
    box("Cabinet", (0, 0, 0.72), (0.72, 0.64, 1.44), r, "metal", 0.07)
    for i in range(4):
        box("Drawer", (0, -0.332, 0.25 + i * 0.32), (0.62, 0.04, 0.26), r, "cream", 0.018)
        box("Handle", (0, -0.37, 0.25 + i * 0.32), (0.18, 0.05, 0.04), r, "dark", 0.012)
    return r


def bin_asset(name="TrashBin", recycling=False):
    r = root(name)
    cylinder("Bin", (0, 0, 0.31), 0.27, 0.62, r, "blue" if recycling else "metal", 12)
    torus("Rim", (0, 0, 0.62), 0.24, 0.035, r, "dark")
    return r


def sign_asset(name, size, material):
    r = root(name)
    box("Sign", (0, 0, size[2] / 2), size, r, material, 0.055)
    box("Inset", (0, -size[1] / 2 - 0.012, size[2] / 2), (size[0] * 0.78, 0.022, size[2] * 0.55), r, "paper" if material != "green" else "cream", 0.02)
    return r


def clock_asset(name="Clock"):
    r = root(name)
    cylinder("Face", (0, 0, 0.34), 0.34, 0.08, r, "cream", 24, (math.pi/2, 0, 0))
    cylinder("Hour", (-0.04, -0.055, 0.38), 0.025, 0.20, r, "dark", 8, (math.pi/2, 0, math.radians(25)))
    cylinder("Minute", (0.08, -0.06, 0.38), 0.018, 0.26, r, "burgundy", 8, (math.pi/2, 0, math.radians(-55)))
    return r


def conference_table(name="ConferenceTable"):
    r = root(name)
    box("Top", (0, 0, 0.76), (3.3, 1.25, 0.14), r, "walnut", 0.16)
    for x in (-1.2, 1.2):
        box("Pedestal", (x, 0, 0.38), (0.22, 0.72, 0.70), r, "metal", 0.06)
    return r


def reception(name="ReceptionDesk"):
    r = root(name)
    box("Main", (0, 0, 0.60), (2.6, 0.78, 1.20), r, "walnut", 0.14)
    box("CreamFront", (0, -0.405, 0.58), (2.18, 0.06, 0.72), r, "cream", 0.04)
    box("RaisedShelf", (0, -0.12, 1.25), (2.75, 0.34, 0.12), r, "light_wood", 0.06)
    return r


def bench(name="WaitingBench"):
    r = root(name)
    box("Seat", (0, 0, 0.50), (1.7, 0.56, 0.16), r, "burgundy", 0.10)
    box("Back", (0, 0.24, 0.88), (1.7, 0.16, 0.65), r, "burgundy", 0.10)
    for x in (-0.65, 0.65):
        box("Leg", (x, 0, 0.24), (0.10, 0.42, 0.45), r, "metal", 0.025)
    return r


def elevator(name="Elevator"):
    r = root(name)
    box("FrameTop", (0, 0, 2.35), (2.75, 0.28, 0.30), r, "metal", 0.06)
    for x in (-1.25, 1.25):
        box("FrameSide", (x, 0, 1.22), (0.26, 0.28, 2.45), r, "metal", 0.06)
    box("DoorLeft", (-0.61, 0.03, 1.16), (1.18, 0.18, 2.25), r, "cream", 0.025)
    box("DoorRight", (0.61, 0.03, 1.16), (1.18, 0.18, 2.25), r, "cream", 0.025)
    box("Indicator", (0, -0.18, 2.48), (0.48, 0.08, 0.20), r, "amber", 0.025)
    return r


def floor_slab(name="FloorSlab"):
    r = root(name)
    box("Slab", (0, 0, -0.38), (30, 22, 0.75), r, "walnut", 0.18)
    box("Carpet", (0, 0, 0.025), (29.5, 21.5, 0.09), r, "carpet", 0.08)
    return r


def wall(name="PartialWall", glass=False):
    r = root(name)
    box("Wall", (0, 0, 0.72), (4.0, 0.18, 1.44), r, "blue" if glass else "cream", 0.055)
    box("Cap", (0, 0, 1.46), (4.08, 0.24, 0.08), r, "metal", 0.03)
    return r


def window_module(name="WindowModule"):
    r = root(name)
    for x in (-1.45, 0, 1.45):
        box("Mullion", (x, 0, 1.25), (0.12, 0.18, 2.50), r, "metal", 0.025)
    box("Sill", (0, 0, 0.08), (3.0, 0.24, 0.16), r, "cream", 0.04)
    box("Glass", (0, 0.04, 1.25), (2.8, 0.04, 2.30), r, "cyan", 0.015)
    return r


def column(name="StructuralColumn"):
    r = root(name)
    box("Column", (0, 0, 1.45), (0.55, 0.55, 2.90), r, "cream", 0.075)
    box("Foot", (0, 0, 0.10), (0.72, 0.72, 0.20), r, "metal", 0.06)
    return r


def bookshelf(name="Bookshelf"):
    r = root(name)
    box("Shell", (0, 0, 1.10), (1.35, 0.42, 2.20), r, "walnut", 0.08)
    for row in range(4):
        box("Shelf", (0, -0.22, 0.30 + row * 0.48), (1.18, 0.42, 0.08), r, "light_wood", 0.025)
        for col in range(4):
            material = ("burgundy", "cream", "blue", "amber")[(row + col) % 4]
            box("Book", (-0.42 + col * 0.27, -0.45, 0.46 + row * 0.48), (0.17, 0.18, 0.30 + 0.04 * (col % 2)), r, material, 0.016)
    return r


def counter(name="Counter"):
    r = root(name)
    box("Cabinet", (0, 0, 0.48), (2.6, 0.72, 0.96), r, "cream", 0.08)
    box("Top", (0, 0, 1.01), (2.78, 0.82, 0.12), r, "walnut", 0.06)
    for x in (-0.62, 0.62):
        box("Door", (x, -0.375, 0.48), (1.10, 0.05, 0.78), r, "light_wood", 0.025)
    return r


def plant(name="PottedPlant", tall=False):
    r = root(name)
    cylinder("Pot", (0, 0, 0.30), 0.38 if tall else 0.28, 0.60 if tall else 0.44, r, "burgundy", 12)
    height = 1.35 if tall else 0.85
    cylinder("Stem", (0, 0, height * 0.55), 0.055, height, r, "leaf", 10)
    for i in range(7 if tall else 5):
        angle = i * 2.4
        sphere("Leaf", (math.cos(angle) * 0.28, math.sin(angle) * 0.20, 0.58 + i * height * 0.09), 0.24, r, "leaf", (0.52, 0.28, 1.2))
    return r


def box_asset(name="CardboardBox"):
    r = root(name)
    box("Carton", (0, 0, 0.26), (0.72, 0.52, 0.52), r, "cardboard", 0.045)
    box("FlapL", (-0.22, 0, 0.59), (0.34, 0.48, 0.08), r, "cardboard", 0.025, (0, math.radians(-18), 0))
    box("FlapR", (0.22, 0, 0.59), (0.34, 0.48, 0.08), r, "cardboard", 0.025, (0, math.radians(18), 0))
    return r


def worker(name="Worker"):
    r = root(name)
    body = sphere("Body", (0, 0, 0.92), 0.38, r, "coral", (0.85, 0.62, 1.18))
    body.name = "Body"
    head = sphere("Head", (0, 0, 1.46), 0.32, r, "skin", (1.0, 0.92, 1.05))
    head.name = "Head"
    sphere("Hair", (0, 0.015, 1.69), 0.29, r, "dark", (1.03, 0.92, 0.40)).name = "Hair"
    for x in (-0.16, 0.16):
        sphere("Eye", (x * 0.42, -0.292, 1.50), 0.026, r, "dark", (1, 0.45, 1))
    for side in (-1, 1):
        cylinder("Arm_L" if side < 0 else "Arm_R", (side * 0.36, -0.02, 0.92), 0.075, 0.58, r, "coral", 10, (0, math.radians(8 * side), 0))
        cylinder("Leg_L" if side < 0 else "Leg_R", (side * 0.16, 0, 0.38), 0.09, 0.62, r, "dark", 10)
        sphere("Shoe", (side * 0.16, -0.08, 0.07), 0.13, r, "dark", (0.75, 1.2, 0.5))
    box("Badge", (0.18, -0.34, 1.02), (0.17, 0.035, 0.20), r, "paper", 0.018)
    return r


BUILDERS = [
    ("Desk_A", lambda: desk("Desk_A")), ("Desk_B", lambda: desk("Desk_B", True)),
    ("DamagedDesk", damaged_desk), ("OfficeChair", chair), ("Monitor", monitor), ("CheapCRTMonitor", cheap_crt_monitor),
    ("Keyboard", keyboard), ("Mouse", mouse),
    ("DeskLamp", lamp), ("Mug", mug), ("PaperStack", paper_stack), ("FileTray", file_tray),
    ("DeskPlant", desk_plant), ("CubicleDivider", cubicle), ("Nameplate", nameplate),
    ("WaterCooler", water_cooler), ("CoffeeMachine", lambda: appliance("CoffeeMachine", (0.78, 0.62, 1.42), "metal")),
    ("VendingMachine", lambda: appliance("VendingMachine", (1.05, 0.72, 1.95), "blue")),
    ("CheapVendingMachine", cheap_vending_machine),
    ("Printer", lambda: appliance("Printer", (0.84, 0.66, 0.62), "cream", False)),
    ("Copier", lambda: appliance("Copier", (0.92, 0.72, 1.18), "cream", False)),
    ("FilingCabinet", filing_cabinet), ("TrashBin", bin_asset), ("RecyclingBin", lambda: bin_asset("RecyclingBin", True)),
    ("NoticeBoard", lambda: sign_asset("NoticeBoard", (1.6, 0.10, 1.0), "burgundy")), ("Clock", clock_asset),
    ("ExitSign", lambda: sign_asset("ExitSign", (0.90, 0.10, 0.34), "green")),
    ("ConferenceTable", conference_table), ("MeetingChair", lambda: chair("MeetingChair")),
    ("Whiteboard", lambda: sign_asset("Whiteboard", (2.25, 0.10, 1.25), "cream")),
    ("DisplayScreen", lambda: sign_asset("DisplayScreen", (2.10, 0.12, 1.18), "dark")),
    ("ReceptionDesk", reception), ("WaitingBench", bench), ("CompanySign", lambda: sign_asset("CompanySign", (2.2, 0.12, 0.75), "burgundy")),
    ("VisitorChair", lambda: chair("VisitorChair")), ("Elevator", elevator), ("FloorSlab", floor_slab),
    ("PartialWall", wall), ("GlassWall", lambda: wall("GlassWall", True)), ("WindowModule", window_module),
    ("StructuralColumn", column), ("Door", lambda: sign_asset("Door", (1.05, 0.16, 2.15), "walnut")),
    ("Bookshelf", bookshelf), ("Counter", counter), ("Cabinet", filing_cabinet),
    ("PottedPlant", plant), ("TallPlant", lambda: plant("TallPlant", True)),
    ("CardboardBox", box_asset), ("Ashtray", ashtray), ("Cigarette", cigarette),
    ("NeighborSign", neighbor_sign), ("ConnectingWallTrim", connecting_wall_trim),
    ("ElevatorIndicator", lambda: sign_asset("ElevatorIndicator", (0.52, 0.10, 0.24), "amber")),
    ("Worker", worker),
]


def export_asset(name, builder, export_dir, unity_dir, source_dir):
    reset_scene()
    asset_root = builder()
    bpy.context.view_layer.objects.active = asset_root
    bpy.ops.object.select_all(action="SELECT")
    for obj in bpy.context.selected_objects:
        if obj.type == "MESH":
            bpy.context.view_layer.objects.active = obj
            try:
                bpy.ops.object.shade_smooth_by_angle()
            except Exception:
                pass
    fbx_path = export_dir / f"OP_{name}.fbx"
    if not hasattr(bpy.ops.export_scene, "fbx"):
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        use_mesh_modifiers=True,
        bake_space_transform=False,
        object_types={'EMPTY', 'MESH'},
        axis_forward='-Z',
        axis_up='Y',
        global_scale=1.0,
    )
    shutil.copy2(fbx_path, unity_dir / fbx_path.name)
    blend_path = source_dir / f"OP_{name}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)
    triangles = sum(len(obj.data.loop_triangles) if obj.type == "MESH" else 0 for obj in bpy.context.scene.objects)
    return {
        "name": name,
        "fbx": str(fbx_path),
        "unity": str(unity_dir / fbx_path.name),
        "source": str(blend_path),
        "objects": len(bpy.context.scene.objects),
        "triangles": triangles,
    }


def main():
    args = parse_args()
    project = Path(args.project_root).resolve()
    export_dir = project / "Tools" / "Blender" / "Exports"
    source_dir = project / "Tools" / "Blender" / "Source"
    unity_dir = project / "Assets" / "OpenPlan" / "Art" / "Models"
    log_dir = project / "Tools" / "Blender" / "Logs"
    for directory in (export_dir, source_dir, unity_dir, log_dir):
        directory.mkdir(parents=True, exist_ok=True)
    selected = BUILDERS
    if args.only:
        requested = {name for group in args.only for name in group}
        known = {name for name, _ in BUILDERS}
        unknown = sorted(requested - known)
        if unknown:
            raise ValueError("Unknown asset names: " + ", ".join(unknown))
        selected = [(name, builder) for name, builder in BUILDERS if name in requested]

    generated = []
    for name, builder in selected:
        print(f"[OPEN PLAN] Generating {name}")
        generated.append(export_asset(name, builder, export_dir, unity_dir, source_dir))

    records_by_name = {}
    manifest_path = project / "Tools" / "Blender" / "asset_manifest.json"
    if args.only and manifest_path.exists():
        existing = json.loads(manifest_path.read_text(encoding="utf-8"))
        records_by_name.update({record["name"]: record for record in existing["assets"]})
    records_by_name.update({record["name"]: record for record in generated})
    records = [records_by_name[name] for name, _ in BUILDERS if name in records_by_name]
    manifest = {
        "generator": "generate_open_plan_assets.py",
        "blender": bpy.app.version_string,
        "unit": "meter",
        "asset_count": len(records),
        "assets": records,
    }
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    (project / "Docs" / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"[OPEN PLAN] Complete: {len(records)} assets; manifest {manifest_path}")


if __name__ == "__main__":
    main()
