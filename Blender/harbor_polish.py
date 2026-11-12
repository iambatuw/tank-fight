# Gives the Harbor scene of Arenas.blend a modelled, volumetric look: chamfers hard box edges and
# adds 3D dock clutter. Safe to run more than once (bevelled objects are tagged).
# Run inside Blender:  exec(open(r"<project>/Blender/harbor_polish.py", encoding="utf-8").read(), {"__name__": "harbor"})
import bpy, bmesh, math, random, os

DIR = r"C:\Users\batuh\My project\Blender"
exec(open(os.path.join(DIR, "arena_lib.py"), encoding="utf-8").read(), globals())
sc = bpy.data.scenes["Harbor"]
bpy.context.window.scene = sc
SCENE[0] = sc
M = {m.name.rsplit("_", 1)[0]: m for m in bpy.data.materials if m.name.startswith("HB_")}
rnd = random.Random(77)

# 1. chamfer every solid box (8 vertices) that is thick enough to show it; flat paint stays sharp
SKIP = ("Water", "Wave", "Road", "Dash", "CenterDash", "Zebra", "Sleeper", "Rail", "Stain", "Crack", "Manhole",
        "Spawn", "Chevron", "Supply", "Lane")
bevelled = 0
for o in list(bpy.data.collections["Harbor_Art"].objects):
    if o.type != 'MESH' or o.get("bevelled") or o.name.startswith(SKIP) or len(o.data.vertices) != 8:
        continue
    dims = sorted(o.dimensions)
    if dims[0] < 0.25:
        continue
    width = min(0.14, dims[0] * 0.16)
    if o.name in ("Pier", "PierSide"):
        width = 0.18
    bevel_object(o, width=width, segments=2 if dims[0] > 1.0 else 1)
    o["bevelled"] = True
    bevelled += 1

# 2. dock clutter (3D, closed models); anything inside the play area gets a collider
for o in [o for o in sc.objects if o.name.startswith(("TireStack", "Pallet", "Sack", "Coil", "COL_TireStack",
                                                         "COL_Pallet"))]:
    bpy.data.objects.remove(o, do_unlink=True)


def tire_stack(name, x, y, n):
    for k in range(n):
        t = cyl(f"{name}_{k}", x + rnd.uniform(-.08, .08), y + rnd.uniform(-.08, .08), .62, .32, M["HB_Tire"],
                z0=k * .33, seg=12)
        cyl(f"{name}_Hub{k}", t.location.x, t.location.y, .3, .34, M["HB_Black"], z0=k * .33 - .01, seg=10)
    col(name, x, y, 1.3, 1.3, n * .33)


def pallet(name, x, y, rz, sacks):
    box(f"{name}_Base", x, y, 1.6, 1.3, .18, M["HB_Pallet"], rz=rz)
    for k in range(sacks):
        sx = (k % 2 - .5) * .7; sy = (k // 2 % 2 - .5) * .55; sz = .18 + (k // 4) * .38
        c_, s_ = math.cos(math.radians(rz)), math.sin(math.radians(rz))
        b = boulder(f"{name}_Sack{k}", x + c_ * sx - s_ * sy, y + s_ * sx + c_ * sy, .72, .55, .4,
                    mat("HB_Sack", "CDBB8E"), 500 + k + int(x * 7), z0=sz, subdiv=1, rough=.08, cuts=1, sink=0.0)
    col(name, x, y, 1.7, 1.4, .2 + .38 * (1 + (sacks - 1) // 4), rz=rz)


for s in (1, -1):
    tire_stack(f"TireStack{s}A", -29.5 * s, -8.5 * s, 3)
    tire_stack(f"TireStack{s}B", -28.2 * s, -9.6 * s, 2)
    pallet(f"Pallet{s}", 16.5 * s, -18.5 * s, 12, 6)
for x in range(-28, 30, 16):
    for y in (-26.6, 26.6):
        cyl(f"Coil{x}_{y}", x + .9, y, .42, .16, mat("HB_Rope", "C9A86A"), z0=0.45, seg=12)

marker_ok = all(bpy.data.objects.get(n) for n in ("MARK_Origin", "MARK_East20"))
print("bevelled", bevelled, "objects", len(sc.objects), "markers", marker_ok)

# 3. height and volume: container towers, a two-storey office, tall light masts, slab joints
for o in [o for o in sc.objects if o.name.startswith(("Tower", "COL_Tower", "Office2", "Stair", "RoofRail", "Mast",
                                                         "COL_Mast", "Joint"))]:
    bpy.data.objects.remove(o, do_unlink=True)
CONT = [M["HB_ContRed"], M["HB_ContBlue"], M["HB_ContGreen"], M["HB_ContOrange"]]


def tower(s):
    """Three containers stacked against the east/west quay edge (point-symmetric pair)."""
    x, y = -30.4 * s, 0.0
    for k in range(3):
        c = CONT[(k + (0 if s > 0 else 2)) % 4]
        b = box(f"Tower{s}_{k}", x + rnd.uniform(-.12, .12), y + rnd.uniform(-.2, .2), 2.8, 6.0, 2.8, c,
                z0=k * 2.82, rz=rnd.uniform(-2, 2))
        bevel_object(b, 0.12, 2); b["bevelled"] = True
        for side in (-1, 1):
            for r in range(-5, 6):
                box(f"Tower{s}_{k}_Rib{side}{r}", x + side * 1.43, y + r * .5, .08, .12, 2.5, c, z0=k * 2.82 + .15)
            box(f"Tower{s}_{k}_Door{side}", x, y + side * 3.03, 2.6, .06, 2.55, c, z0=k * 2.82 + .12)
            for dz in (-.5, .5):
                box(f"Tower{s}_{k}_Bar{side}{dz}", x + dz, y + side * 3.07, .07, .06, 2.4, M["HB_Line"], z0=k * 2.82 + .2)
    col(f"Tower{s}", x, y, 3.0, 6.2, 8.5)


tower(1); tower(-1)

# second storey on the harbour office, outside stairs and a roof rail
o2 = box("Office2", .5, .3, 4.6, 3.6, 2.6, M["HB_Building"], z0=3.95)
bevel_object(o2, .1, 2); o2["bevelled"] = True
box("Office2Roof", .5, .3, 5.0, 4.0, .3, M["HB_Roof"], z0=6.55)
for s_ in (-1, 1):
    box(f"Office2Win{s_}", .5, .3 + s_ * 1.82, 3.4, .06, 1.0, M["HB_Glass"], z0=5.0)
for o in [o for o in sc.objects if o.name.startswith("COL_Stair")]:
    bpy.data.objects.remove(o, do_unlink=True)
for k in range(8):                                   # outside stairs on the west wall
    box(f"Stair{k}", -3.75, -1.9 + k * .45, 1.0, .45, .2, M["HB_Steel"], z0=.3 + k * .46)
box("StairRail", -4.3, -.3, .08, 3.8, 1.0, M["HB_Safety"], z0=2.4)
col("Stairs", -3.8, -.3, 1.2, 4.0, 4.0)
# roof equipment moves up onto the second storey
for name, (x_, y_, z_) in {"ACUnit": (-0.9, .9, 6.85), "Antenna": (2.1, 1.3, 6.85), "AntennaTip": (2.1, 1.3, 9.85)}.items():
    o_ = bpy.data.objects.get(name)
    if o_:
        o_.location = (x_, y_, z_)
for x_ in (-1.7, 2.7):
    box(f"RoofRail{x_}", x_, .3, .08, 4.0, .6, M["HB_Safety"], z0=6.85)

# tall light masts with flood-light heads (replace the small road lamps' role at the corners)
for s_ in (1, -1):
    for (x, y) in [(-16 * s_, 20.5 * s_), (16 * s_, 20.5 * s_)]:
        cyl(f"Mast{x}_{y}", x, y, .22, 8.0, M["HB_Steel"], seg=8, r2=.14)
        box(f"MastHead{x}_{y}", x, y, 1.6, .5, .45, M["HB_Steel"], z0=7.9)
        box(f"MastLamp{x}_{y}", x, y - .27 * (1 if y > 0 else -1), 1.4, .06, .3, M["HB_Lamp"], z0=7.95)
        col(f"Mast{x}_{y}", x, y, .5, .5, 8)

# concrete slab joints (flat paint, kept off the road and the gameplay markings)
for x in range(-30, 31, 6):
    for (y0, y1) in ((-26, -4.6), (4.6, 26)):
        box(f"JointV{x}_{y0}", x, (y0 + y1) / 2, .07, y1 - y0, .012, M["HB_ConcreteDark"])
for y in list(range(-24, -4, 6)) + list(range(6, 26, 6)):
    box(f"JointH{y}", 0, y, 64, .07, .012, M["HB_ConcreteDark"])
print("volume pass done", len(sc.objects))
