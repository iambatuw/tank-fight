# Close-up cameras that mimic the in-game follow camera (orthographic size 15, pitched 55 degrees,
# player slightly below centre) at the arena edges. Usage inside Blender:
#   exec(open(r"<project>/Blender/check_views.py").read(), g); g["use"]("SW")
import bpy, math
from mathutils import Vector

SPOTS = {"SW": (-29, -23), "NE": (29, 23), "S": (0, -24), "E": (30, 0), "W": (-30, 4), "N": (8, 24),
         "NW": (-29, 23), "SE": (29, -23)}


def use(key, size=15.0):
    sc = bpy.context.scene
    x, y = SPOTS[key]
    name = "CHECK_View"
    cam = bpy.data.objects.get(name) or bpy.data.objects.new(name, bpy.data.cameras.new(name))
    if cam.name not in sc.collection.objects:
        sc.collection.objects.link(cam)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2 * size * 16 / 9; cam.data.clip_end = 500
    pitch = math.radians(55)
    fwd = Vector((0, math.cos(pitch), -math.sin(pitch)))
    up = Vector((0, math.sin(pitch), math.cos(pitch)))
    cam.location = Vector((x, y, 0)) - fwd * 100 + up * (size * (1 - 2 * 0.43))
    cam.rotation_euler = (math.radians(35), 0, 0)
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    return name


def restore():
    sc = bpy.context.scene
    game = bpy.data.objects.get("GameCam_" + sc.name)
    if game:
        sc.camera = game
    cam = bpy.data.objects.get("CHECK_View")
    if cam:
        bpy.data.objects.remove(cam, do_unlink=True)
