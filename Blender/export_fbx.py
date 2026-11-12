# Exports one arena scene of Arenas.blend to Assets/ArenaArt/Blender/<Scene>.fbx for Unity.
# Visual meshes are merged into one object per material (ART_<material>); COL_* colliders and
# MARK_* axis markers are exported as-is. Usage inside Blender:
#   g = {}; exec(open(r"<project>/Blender/export_fbx.py").read(), g); g["export"]("Canyon")
import bpy, bmesh, os

PROJECT = r"C:\Users\batuh\My project"


def export(scene_name):
    sc = bpy.data.scenes[scene_name]
    bpy.context.window.scene = sc
    tmp = bpy.data.collections.new(scene_name + "_Export"); sc.collection.children.link(tmp)
    dg = bpy.context.evaluated_depsgraph_get()
    groups = {}
    for o in bpy.data.collections[scene_name + "_Art"].objects:
        if o.type != 'MESH':
            continue
        src = o.evaluated_get(dg).to_mesh()
        b = bmesh.new(); b.from_mesh(src); b.transform(o.matrix_world)
        slots = [m.name if m else "None" for m in o.data.materials] or ["None"]
        for idx, key in enumerate(slots):
            faces = [f for f in b.faces if f.material_index == idx]
            if not faces:
                continue
            part = b.copy()
            bmesh.ops.delete(part, geom=[f for f in part.faces if f.material_index != idx], context='FACES')
            for f in part.faces:
                f.material_index = 0
            tme = bpy.data.meshes.new("tmp"); part.to_mesh(tme); part.free()
            groups.setdefault(key, (bmesh.new(), o.data.materials[idx] if o.data.materials else None))[0].from_mesh(tme)
            bpy.data.meshes.remove(tme)
        b.free(); o.evaluated_get(dg).to_mesh_clear()
    made = []
    for key, (b, m) in groups.items():
        me = bpy.data.meshes.new("ART_" + key); b.to_mesh(me); b.free()
        me.materials.append(m)
        ob = bpy.data.objects.new("ART_" + key, me); tmp.objects.link(ob); made.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in made:
        ob.select_set(True)
    for cname in (scene_name + "_Colliders", scene_name + "_Markers"):
        for ob in bpy.data.collections[cname].objects:
            ob.hide_set(False); ob.select_set(True)
    path = os.path.join(PROJECT, "Assets", "ArenaArt", "Blender", scene_name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True,
                             axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    tris = sum(sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in made)
    for ob in made:
        me = ob.data; bpy.data.objects.remove(ob); bpy.data.meshes.remove(me)
    bpy.data.collections.remove(tmp)
    return {"materials": len(made), "tris": tris, "bytes": os.path.getsize(path)}
