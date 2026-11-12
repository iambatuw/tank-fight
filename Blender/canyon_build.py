# Builds the "Canyon" scene of Arenas.blend from scratch.
# Run inside Blender:  exec(open(r"<project>/Blender/canyon_build.py", encoding="utf-8").read(), {"__name__": "canyon"})
# Coordinates: Blender X/Y = Unity X/Z, Blender Z = Unity Y (height). Floor top is at 0.
import bpy, bmesh, math, random, os
from mathutils import Vector

DIR = os.path.join(os.path.dirname(bpy.data.filepath) if bpy.data.filepath else r"C:\Users\batuh\My project\Blender")
for name in ("arena_lib.py", "canyon_terrain.py"):
    exec(open(os.path.join(DIR, name), encoding="utf-8").read(), globals())

sc = reset_scene("Canyon"); SCENE[0] = sc
rnd = random.Random(2026)

# ---------------- palette ----------------
SAND = mat("CY_Sand", "DCA66B"); SAND2 = mat("CY_SandDark", "C48C56"); RIVER = mat("CY_Riverbed", "BD8250"); MUD = mat("CY_Mud", "A86F43")
BASE = mat("CY_CliffBase", "7A3B26"); RUST = mat("CY_Rust", "A9532F"); ORANGE = mat("CY_Orange", "D2854A"); PALE = mat("CY_Pale", "EBC48D")
TOPM = mat("CY_Plateau", "D99A5E"); LEDGE = mat("CY_Ledge", "C9773F")
CACT = mat("CY_Cactus", "5F8F4E"); CACT2 = mat("CY_CactusLight", "86B06A"); BUSH = mat("CY_Brush", "9A8848"); DEAD = mat("CY_DeadWood", "6E5440")
WOOD = mat("CY_Wood", "865733"); WOODD = mat("CY_WoodDark", "5B3A22"); IRON = mat("CY_Iron", "4A4440"); DARK = mat("CY_MineDark", "2A1A14")
TILE = mat("CY_Tile", "C7955D"); TILE2 = mat("CY_TileLight", "D6A86E"); PADLINE = mat("CY_PadLine", "F7EAD2")
SUP = mat("CY_Supply", "6F9468"); WHITE = mat("CY_White", "F4F1E8")
SPAWNS = [(-25, -14), (25, 14), (-25, 14), (25, -14)]
PICKUPS = [(-20, 0), (20, 0), (0, -19), (0, 19)]


def clear(x, y, r):
    return all(math.hypot(x - a, y - b) > r for a, b in SPAWNS) and all(math.hypot(x - a, y - b) > r - 1.0 for a, b in PICKUPS)


def pair(fn):
    """Point symmetry keeps both starts equal: each feature is built at (x, y) and (-x, -y)."""
    fn(1); fn(-1)


# ---------------- canyon walls ----------------
# Nested strata rings around the floor. Each ring's wall stands on the ledge (top face) of the
# ring below and the top ring runs out to a far plateau, so the surroundings are one continuous
# surface: no gaps, no stacked coplanar faces, nothing for the camera to see "behind".
BASE_R = 1.0                      # corner radius of the innermost wall
CORE_X, CORE_Y = FX + 0.25 - BASE_R, FY + 0.25 - BASE_R


def outward(x, y):
    """True outward normal of the rounded rectangle, so offset rings stay nested at the corners."""
    cx = max(-CORE_X, min(CORE_X, x)); cy = max(-CORE_Y, min(CORE_Y, y))
    nx, ny = x - cx, y - cy
    L = math.hypot(nx, ny)
    if L < 1e-6:
        return (0.0, 1.0)
    return nx / L, ny / L


def erosion(x, y, k):
    a = math.atan2(y, x)
    return (abs(math.sin(a * 9 + k * 1.7)) ** 3 * 1.6 + abs(math.sin(a * 23 + k * 0.9)) ** 6 * 0.9
            + 0.5 * (0.5 + 0.5 * noise(x * 1.6, y * 1.6 + k * 5)))


base = rect_points(FX + 0.25, FY + 0.25, BASE_R); N = len(base); NL = 4
offs = [[0.0] * N for _ in range(NL)]
for i, (x, y, tag) in enumerate(base):
    for k in range(NL):
        offs[k][i] = 0.1 * erosion(x, y, 0) if k == 0 else offs[k - 1][i] + 1.65 + 1.1 * erosion(x, y, k) + rnd.uniform(0, .2)
for k in range(NL):
    offs[k] = [(offs[k][i - 1] + 2 * offs[k][i] + offs[k][(i + 1) % N]) / 4 for i in range(N)]
    if k:
        offs[k] = [max(offs[k][i], offs[k - 1][i] + 1.0) for i in range(N)]
# Lower ledges get a little jitter; the top ledge is exact so the plateau beyond it stays level.
heights = [[side_height(x, y) * (k + 1) / NL + (rnd.uniform(-.12, .12) * (k + 1) if k < NL - 1 else 0.0)
            for (x, y, t) in base] for k in range(NL)]
outer = rect_points(112.0, 100.0, 6.0)
bm = bmesh.new(); foot = []
for k in range(NL):
    zb = [0.0] * N if k == 0 else heights[k - 1]
    bot, top = [], []
    for i, (x, y, tag) in enumerate(base):
        nx, ny = outward(x, y); bx, by = x + nx * offs[k][i], y + ny * offs[k][i]
        lean = 0.3 + rnd.uniform(0, .25)
        bot.append(bm.verts.new((bx, by, zb[i] - (0.03 if k else 0.0))))
        top.append(bm.verts.new((bx + nx * lean, by + ny * lean, heights[k][i])))
        if k == 0:
            foot.append((bx, by, nx, ny))
    far = [bm.verts.new((ox, oy, heights[k][i])) for i, (ox, oy, _) in enumerate(outer)]
    inner_edge = top
    if k == NL - 1:
        # A level ring 12 units past the rim keeps the visible plateau made of short, regular
        # faces; only the far, off-screen part stretches out to the outer rectangle.
        mid = []
        for i, (x, y, tag) in enumerate(base):
            nx, ny = outward(x, y)
            mx_, my_ = top[i].co.x + nx * 12, top[i].co.y + ny * 12
            mid.append(bm.verts.new((mx_, my_, heights[k][i])))
        for i in range(N):
            j = (i + 1) % N
            bm.faces.new((top[i], top[j], mid[j], mid[i])).material_index = 4
        inner_edge = mid
    for i in range(N):
        j = (i + 1) % N
        bm.faces.new((bot[i], bot[j], top[j], top[i])).material_index = k
        bm.faces.new((inner_edge[i], inner_edge[j], far[j], far[i])).material_index = 4 if k == NL - 1 else 5
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if f.material_index < 4])
me = bpy.data.meshes.new("CanyonWalls"); bm.to_mesh(me); bm.free()
for m in (BASE, RUST, ORANGE, PALE, TOPM, LEDGE):
    me.materials.append(m)
for p in me.polygons:
    p.use_smooth = False
walls = bpy.data.objects.new("CanyonWalls", me); coll("Art").objects.link(walls)
box("Floor", 0, 0, 224, 200, 0.4, SAND, z0=-0.4)
# Colliders: the boundary's inner faces sit at the visible cliff foot (x = +-33.2, y = +-27.2).
col("Ground", 0, 0, 72, 60, 0.5, z0=-0.5)
col("WallW", -34.2, 0, 2, 60, 4); col("WallE", 34.2, 0, 2, 60, 4)
col("WallN", 0, 28.2, 70, 2, 4); col("WallS", 0, -28.2, 70, 2, 4)
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()


def ground_z(x, y):
    hit, loc, *_ = sc.ray_cast(dg, Vector((x, y, 60)), Vector((0, 0, -1)))
    return loc.z if hit else 0.0


# talus boulders resting at the cliff foot, outside the collision wall face
for i in range(0, N, 3):
    bx, by, nx, ny = foot[i]
    if rnd.random() < 0.55:
        r = rnd.uniform(0.7, 1.4); d = r + 0.35
        rock(f"Talus{i}", bx + nx * d, by + ny * d, r * 2, r * 1.7, r * 1.1, rnd.choice([RUST, ORANGE, BASE]), 3000 + i,
             seg=7, taper=.6, rz=rnd.uniform(0, 180))

# plateau decoration: buttes and scrub, kept low on the south side facing the camera
for i in range(46):
    x, y = rnd.uniform(-100, 100), rnd.uniform(-90, 90)
    if abs(x) < 47 and abs(y) < 40:
        continue
    if y < -30 and rnd.random() < 0.6:
        continue
    z = ground_z(x, y)
    tall = 1.0 if y > 0 else 0.45
    strata(f"Butte{i}", x, y, rnd.uniform(6, 14), rnd.uniform(5, 11), rnd.uniform(2.5, 7) * tall, [RUST, ORANGE, PALE],
           4000 + i, z0=z - 0.3, layers=3)
for i in range(120):
    x, y = rnd.uniform(-95, 95), rnd.uniform(-85, 85)
    if abs(x) < 44 and abs(y) < 37:
        continue
    z = ground_z(x, y)
    if rnd.random() < 0.3:
        cyl(f"PlateauCactus{i}", x, y, .35, rnd.uniform(1.6, 2.6), CACT, z0=z, seg=7, r2=.3)
    else:
        rock(f"PlateauRock{i}", x, y, rnd.uniform(.6, 1.8), rnd.uniform(.5, 1.5), rnd.uniform(.3, .9),
             rnd.choice([RUST, ORANGE, TOPM]), 5000 + i, z0=z - .1, seg=6)


# ---------------- floor detail ----------------
def ribbon(name, pts, width, m, z):
    b = bmesh.new(); left, right = [], []
    for i, (x, y) in enumerate(pts):
        a = pts[max(i - 1, 0)]; c = pts[min(i + 1, len(pts) - 1)]
        dx, dy = c[0] - a[0], c[1] - a[1]; l = math.hypot(dx, dy) or 1
        w = width * (0.85 + 0.15 * math.sin(i * 1.7))
        left.append(b.verts.new((x - dy / l * w / 2, y + dx / l * w / 2, z)))
        right.append(b.verts.new((x + dy / l * w / 2, y - dx / l * w / 2, z)))
    for i in range(len(pts) - 1):
        b.faces.new((left[i], right[i], right[i + 1], left[i + 1]))
    return _obj(name, b, m)


wash = [(-32.5 + t * 65 / 44, -6.5 * math.sin(math.pi * (-32.5 + t * 65 / 44) / 33)) for t in range(45)]
ribbon("Wash", wash, 6.5, RIVER, 0.015); ribbon("WashCore", wash, 2.6, MUD, 0.028)
for i, (x, y) in enumerate(wash):
    for k in range(3):
        px, py = x + rnd.uniform(-1.6, 1.6), y + rnd.uniform(-1.6, 1.6)
        if clear(px, py, 4.5) and abs(px) < 31:
            rock(f"Pebble{i}_{k}", px, py, rnd.uniform(.25, .6), rnd.uniform(.25, .5), rnd.uniform(.08, .18), SAND2,
                 6000 + i * 3 + k, seg=6, taper=.6)
for i in range(240):
    x, y = rnd.uniform(-31.5, 31.5), rnd.uniform(-25.5, 25.5)
    if clear(x, y, 4.6):
        rock(f"Fleck{i}", x, y, rnd.uniform(.2, .7), rnd.uniform(.15, .5), .035, SAND2 if i % 3 else PALE, 7000 + i,
             seg=5, taper=1, jit=.3)

# spawn platforms and supply pads
for i, (x, y) in enumerate(SPAWNS):
    box(f"SpawnPad{i}", x, y, 6.4, 6.4, .05, SAND2)
    for tx in range(3):
        for ty in range(3):
            box(f"SpawnTile{i}_{tx}{ty}", x - 2 + tx * 2, y - 2 + ty * 2, 1.84, 1.84, .1 + rnd.uniform(0, .03),
                TILE if (tx + ty) % 2 else TILE2, rz=rnd.uniform(-2, 2))
    box(f"SpawnEdge{i}", x, y - 3.0, 5.2, .25, .13, PADLINE)
    for cx in (-1, 1):
        for cy in (-1, 1):
            rock(f"SpawnStone{i}_{cx}{cy}", x + cx * 3.5, y + cy * 3.5, .8, .8, .55, RUST, 700 + i * 4 + cx + cy * 2, seg=6)
for i, (x, y) in enumerate(PICKUPS):
    cyl(f"SupplyPad{i}", x, y, 1.9, .07, SUP, seg=8)
    box(f"SupplyCrossA{i}", x, y, 1.7, .35, .1, WHITE); box(f"SupplyCrossB{i}", x, y, .35, 1.7, .1, WHITE)


# ---------------- cover ----------------
def mesa_half(s):  # 1. split mesa; the dry wash runs through the slot
    x, y = 3.4 * s, 2.9 * s
    strata(f"Mesa{s}", x, y, 6.6, 4.6, 4.4, [BASE, RUST, ORANGE, PALE], 10 + s, rz=40, layers=4)
    col(f"Mesa{s}", x, y, 5.6, 3.8, 4.4, rz=40)


def hoodoos(s):  # 2. hoodoo clusters
    for k, (ox, oy, h) in enumerate([(0, 0, 4.4), (1.9, -1.0, 3.3), (-1.4, -1.6, 2.6)]):
        x, y = (-13 + ox) * s, (11 + oy) * s
        strata(f"Hoodoo{s}_{k}", x, y, 1.9, 1.9, h, [RUST, ORANGE, RUST], 20 + k + s * 5, layers=3)
        rock(f"HoodooCap{s}_{k}", x, y, 2.4, 2.2, .55, PALE, 30 + k + s * 5, z0=h - .1, seg=7, taper=.8)
        col(f"Hoodoo{s}_{k}", x, y, 1.6, 1.6, h)


def ledge(s):  # 3. low L-shaped rock ledges
    pts = [(-11, -12), (-8.6, -12.4), (-6.2, -12.2), (-5.4, -10.2)]
    for k, (x, y) in enumerate(pts):
        rock(f"Ledge{s}_{k}", x * s, y * s, 2.7, 2.0, 1.5, ORANGE if k % 2 else RUST, 40 + k + s * 7, seg=7, taper=.75,
             rz=(k * 37) % 90)
    col(f"LedgeA{s}", -8.6 * s, -12.2 * s, 7.0, 1.7, 1.5)
    col(f"LedgeB{s}", -5.4 * s, -10.6 * s, 1.8, 2.6, 1.5)


def side_mesa(s):  # 4. small mesas guarding the side supply pads
    x, y = -20 * s, 6.5 * s
    strata(f"SideMesa{s}", x, y, 4.6, 3.2, 2.6, [RUST, ORANGE, PALE], 50 + s, layers=3, rz=8)
    col(f"SideMesa{s}", x, y, 4.0, 2.6, 2.6, rz=8)


def supply_cover(s):  # 5. boulder and dead tree beside the north/south supply pads
    rock(f"PadBoulder{s}", -5.5 * s, 19.5 * s, 2.4, 2.0, 1.5, RUST, 60 + s, seg=7, taper=.6)
    col(f"PadBoulder{s}", -5.5 * s, 19.5 * s, 2.0, 1.6, 1.5)
    x, y = 5.5 * s, 18.5 * s
    cyl(f"DeadTree{s}", x, y, .32, 2.8, DEAD, seg=6, r2=.18)
    for k, (a, h, l) in enumerate([(30, 1.6, 1.4), (160, 2.2, 1.1), (260, 1.2, 1.0)]):
        br = cyl(f"DeadBranch{s}_{k}", x, y, .12, l, DEAD, z0=h, seg=5, r2=.05)
        br.rotation_euler = (math.radians(55), 0, math.radians(a))
    col(f"DeadTree{s}", x, y, .7, .7, 2.8)


def ore_cart(s):  # 6. ore carts (the mine entrance itself is north-wall decoration)
    x, y = -12 * s, 22.3 * s
    box(f"Cart{s}", x, y, 1.9, 2.4, 1.1, IRON, z0=.35)
    box(f"CartOre{s}", x, y, 1.6, 2.0, .35, BASE, z0=1.4)
    for wx in (-1, 1):
        for wy in (-.7, .7):
            cyl(f"CartWheel{s}{wx}{wy}", x + wx * 1.0, y + wy, .35, .15, IRON, seg=8, rx=90, rz=90)
    col(f"Cart{s}", x, y, 2.1, 2.6, 1.8)


for fn in (mesa_half, hoodoos, ledge, side_mesa, supply_cover, ore_cart):
    pair(fn)

mx, my = -12.0, 28.6
box("MineHole", mx, my + 0.6, 3.2, 1.0, 3.0, DARK)
for side in (-1, 1):
    box(f"MinePost{side}", mx + side * 1.8, my, .45, .45, 3.4, WOOD)
box("MineBeam", mx, my, 4.3, .55, .5, WOOD, z0=3.3)
box("MineSign", mx + .9, my - .32, 1.6, .08, .6, WOODD, z0=3.9)
for k in range(9):
    box(f"MineSleeper{k}", mx, 27.6 - k * 0.9, 2.2, .3, .06, WOODD)
for r in (-.55, .55):
    box(f"MineRail{r}", mx + r, 24.0, .12, 7.4, .12, IRON)


def cactus(name, x, y, h, seedc):
    rr = random.Random(seedc)
    cyl(name + "_Trunk", x, y, .42, h, CACT, seg=8, r2=.36)
    cyl(name + "_Cap", x, y, .36, .3, CACT2, z0=h, seg=8, r2=.15)
    for side in (-1, 1):
        if rr.random() < .8:
            ah = rr.uniform(.9, 1.6); z = rr.uniform(h * .35, h * .6)
            box(name + f"_ArmOut{side}", x + side * .55, y, .7, .45, .4, CACT, z0=z)
            cyl(name + f"_ArmUp{side}", x + side * .85, y, .26, ah, CACT, z0=z, seg=7, r2=.22)
    col(name, x, y, .9, .9, h)


for i, (x, y, h) in enumerate([(-15, -3, 3.0), (15, 3, 3.0), (-29, -21.5, 2.4), (29, 21.5, 2.4),
                               (8, 7, 2.2), (-8, -7, 2.2), (-28, 6, 2.0), (28, -6, 2.0)]):
    cactus(f"Cactus{i}", x, y, h, 900 + i)
for i in range(40):
    x, y = rnd.uniform(-31, 31), rnd.uniform(-25, 25)
    if clear(x, y, 5) and math.hypot(x, y) > 8:
        for k in range(4):
            box(f"Brush{i}_{k}", x + rnd.uniform(-.3, .3), y + rnd.uniform(-.3, .3), .08, .08, rnd.uniform(.35, .7), BUSH,
                rz=rnd.uniform(0, 180)).rotation_euler.x = math.radians(rnd.uniform(-35, 35))

marker("Origin", 0, 0); marker("East20", 20, 0); marker("North20", 0, 20); marker("Up5", 0, 0, 5)
cam = game_view(sc, 34); cam.data.ortho_scale = 84
print("canyon objects", len(sc.objects))
