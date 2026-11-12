
import bpy, bmesh, math, random
from mathutils import Vector, Matrix

def srgb(c):
    return c/12.92 if c <= 0.04045 else ((c+0.055)/1.055)**2.4

def mat(name, hexc, rough=0.85, emit=0.0):
    full = f"{name}_{hexc}"
    m = bpy.data.materials.get(full)
    if m: return m
    m = bpy.data.materials.new(full)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    rgb = [srgb(int(hexc[i:i+2],16)/255) for i in (0,2,4)]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    if emit:
        b.inputs["Emission Color"].default_value = (*rgb, 1)
        b.inputs["Emission Strength"].default_value = emit
    m.diffuse_color = (*rgb, 1)
    return m

SCENE = [None]
def coll(name):
    sc = SCENE[0] or bpy.context.scene
    full = sc.name + "_" + name
    c = bpy.data.collections.get(full)
    if not c:
        c = bpy.data.collections.new(full)
        sc.collection.children.link(c)
    return c

def _obj(name, bm, m, c="Art"):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me)
    coll(c).objects.link(o)
    if m: me.materials.append(m)
    for p in me.polygons: p.use_smooth = False
    return o

def box(name, x, y, w, d, h, m, z0=0.0, rz=0.0, c="Art"):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(w, d, h), verts=bm.verts)
    bmesh.ops.translate(bm, vec=(0, 0, h/2), verts=bm.verts)
    o = _obj(name, bm, m, c)
    o.location = (x, y, z0); o.rotation_euler = (0, 0, math.radians(rz))
    return o

def cyl(name, x, y, r, h, m, z0=0.0, seg=10, r2=None, c="Art", rx=0.0, rz=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=h)
    bmesh.ops.translate(bm, vec=(0, 0, h/2), verts=bm.verts)
    o = _obj(name, bm, m, c)
    o.location = (x, y, z0); o.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    return o

def rock(name, x, y, w, d, h, m, seed=1, z0=0.0, seg=8, taper=0.7, rz=0.0, jit=0.18, c="Art"):
    rnd = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=0.5, radius2=0.5*taper, depth=1.0)
    bmesh.ops.translate(bm, vec=(0, 0, 0.5), verts=bm.verts)
    for v in bm.verts:
        f = 1 + rnd.uniform(-jit, jit)
        v.co.x *= f; v.co.y *= f
        if v.co.z > 0.5: v.co.z += rnd.uniform(-jit, jit) * 0.5
    bmesh.ops.scale(bm, vec=(w, d, h), verts=bm.verts)
    o = _obj(name, bm, m, c)
    o.location = (x, y, z0); o.rotation_euler = (0, 0, math.radians(rz))
    return o

def strata(name, x, y, w, d, h, mats, seed=1, rz=0.0, layers=3, z0=0.0):
    """Layered canyon rock: stacked faceted slabs that shrink upward."""
    out = []; rnd = random.Random(seed); z = z0
    for i in range(layers):
        lh = h / layers * rnd.uniform(0.85, 1.15)
        s = 1 - i * 0.13
        out.append(rock(f"{name}_L{i}", x + rnd.uniform(-.2,.2), y + rnd.uniform(-.2,.2), w*s, d*s, lh,
                        mats[i % len(mats)], seed*7+i, z, seg=rnd.choice([7,8,9]), taper=0.88, rz=rz + rnd.uniform(-8,8)))
        z += lh * 0.97
    return out

def col(name, x, y, w, d, h, rz=0.0, z0=0.0):
    o = box("COL_" + name, x, y, w, d, h, None, z0, rz, c="Colliders")
    o.display_type = 'WIRE'; o.hide_render = True
    return o

def marker(name, x, y, z=0.0):
    o = box("MARK_" + name, x, y, 0.2, 0.2, 0.2, None, z, c="Markers")
    o.display_type = 'WIRE'; o.hide_render = True
    return o

def reset_scene(sname):
    sc = bpy.data.scenes.get(sname)
    if sc:
        for o in list(sc.objects): bpy.data.objects.remove(o, do_unlink=True)
    else:
        sc = bpy.data.scenes.new(sname)
    bpy.context.window.scene = sc
    SCENE[0] = sc
    for c in list(sc.collection.children): sc.collection.children.unlink(c)
    for c in list(bpy.data.collections):
        if c.users == 0: bpy.data.collections.remove(c)
    for me in list(bpy.data.meshes):
        if me.users == 0: bpy.data.meshes.remove(me)
    return sc

def game_view(sc, ortho=34, target=(0,0,0)):
    """Approximate the in-game camera: orthographic, pitched 55 degrees, looking north (+Y)."""
    cam = bpy.data.objects.get("GameCam_" + sc.name)
    if not cam:
        cam = bpy.data.objects.new("GameCam_" + sc.name, bpy.data.cameras.new("GameCam_" + sc.name))
        sc.collection.objects.link(cam)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho * 2; cam.data.clip_end = 500
    pitch = math.radians(55)
    fwd = Vector((0, math.cos(pitch), -math.sin(pitch)))
    cam.location = Vector(target) - fwd * 150
    cam.rotation_euler = (math.radians(90 - 55), 0, 0)
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sun = bpy.data.objects.get("Sun_" + sc.name)
    if not sun:
        sun = bpy.data.objects.new("Sun_" + sc.name, bpy.data.lights.new("Sun_" + sc.name, 'SUN'))
        sc.collection.objects.link(sun)
    sun.data.energy = 3.2; sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(40), math.radians(-25), math.radians(-35))
    if not sc.world:
        sc.world = bpy.data.worlds.new("World_" + sc.name)
    sc.world.use_nodes = True
    bg = next(n for n in sc.world.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[1].default_value = 0.9
    return cam


# ---------------------------------------------------------------------------------------------
# Volumetric low-poly rocks (closed, faceted from every side), in the style of the original
# arenas. Faces are coloured in horizontal bands by world height so canyon rock reads as strata.
# ---------------------------------------------------------------------------------------------
def _vnoise(v, seed):
    s = seed * 0.37
    return (math.sin(v.x * 2.1 + s) * math.cos(v.y * 1.7 - s) + math.sin(v.z * 2.6 + v.x * 1.3 + s * 1.7)
            + 0.5 * math.sin((v.x + v.y - v.z) * 4.3 + s * 2.3)) / 2.5


def boulder(name, x, y, w, d, h, mats, seed=1, z0=0.0, subdiv=2, rough=0.22, flat_top=None, rz=0.0,
            band=1.3, band_offset=0.0, top_mat=None, c="Art", sink=0.12, cuts=4):
    """Irregular icosphere rock. mats: list cycled by world-height band; top_mat colours upward faces.
    flat_top (0..1) cuts the top flat like a mesa. The base is flattened and sunk slightly into the ground."""
    rr = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    off = Vector((rr.uniform(-9, 9), rr.uniform(-9, 9), rr.uniform(-9, 9)))
    for v in bm.verts:
        n = v.co.normalized()
        disp = rough * _vnoise(v.co * 1.3 + off, seed) + rr.uniform(-rough, rough) * 0.35
        v.co = n * (1.0 + disp)
    # Slice the blob with a few planes: big flat facets give the chunky, chiselled look of the
    # original arena rocks; the base and (for mesas) the top are cut flat and capped.
    planes = [((0, 0, -0.55), (0, 0, -1))]
    if flat_top is not None:
        planes.append(((0, 0, flat_top), (0, 0, 1)))
    for _ in range(cuts):
        a = rr.uniform(0, math.tau); el = rr.uniform(-0.25, 0.75)
        nrm = Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el))).normalized()
        planes.append((tuple(nrm * rr.uniform(0.62, 0.86)), tuple(nrm)))
    for co, no in planes:
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_outer=True)
        boundary = [e for e in bm.edges if e.is_boundary]
        if boundary:
            bmesh.ops.holes_fill(bm, edges=boundary, sides=0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    zmin = min(v.co.z for v in bm.verts); zmax = max(v.co.z for v in bm.verts)
    xs = [v.co.x for v in bm.verts]; ys = [v.co.y for v in bm.verts]
    sx = w / (max(xs) - min(xs)); sy = d / (max(ys) - min(ys)); sz = h / (zmax - zmin)
    cx = (max(xs) + min(xs)) / 2; cy = (max(ys) + min(ys)) / 2
    for v in bm.verts:
        v.co = Vector(((v.co.x - cx) * sx, (v.co.y - cy) * sy, (v.co.z - zmin) * sz - sink))
    bm.normal_update()
    if isinstance(mats, (list, tuple)):
        all_mats = list(mats) + ([top_mat] if top_mat else [])
        for f in bm.faces:
            zc = z0 + sum(v.co.z for v in f.verts) / len(f.verts)
            if top_mat and f.normal.z > 0.82:
                f.material_index = len(mats)
            else:
                f.material_index = int(max(0.0, zc + band_offset) / band) % len(mats)
    else:
        all_mats = [mats]
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in all_mats: me.materials.append(m)
    for p in me.polygons: p.use_smooth = False
    o = bpy.data.objects.new(name, me); coll(c).objects.link(o)
    o.location = (x, y, z0); o.rotation_euler = (0, 0, math.radians(rz))
    return o


def bevel_object(o, width=0.08, segments=1):
    """Chamfer every edge of a mesh object so hard boxes catch light like modelled props."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=width, offset_type='OFFSET', segments=segments,
                    profile=0.5, affect='EDGES', clamp_overlap=True)
    bm.to_mesh(o.data); bm.free()
    for p in o.data.polygons: p.use_smooth = False
