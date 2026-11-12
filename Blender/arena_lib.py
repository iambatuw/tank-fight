
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
