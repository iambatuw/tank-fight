import math

# Playable floor half-extents (game units). Canyon walls start just outside these.
FX, FY = 33.0, 27.0
SIDE_N = {"S": 66, "E": 54, "N": 66, "W": 54}
CORNER_N = 6


def rect_points(hx, hy, r):
    """Rounded rectangle with a fixed sample count per side so nested contours line up."""
    pts = []

    def side(p0, p1, n, tag):
        for i in range(n):
            t = i / n
            pts.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t, tag))

    def corner(cx, cy, a0, tag):
        for i in range(CORNER_N):
            a = math.radians(a0 + 90 * i / CORNER_N)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a), tag))

    side((-hx + r, -hy), (hx - r, -hy), SIDE_N["S"], "S"); corner(hx - r, -hy + r, -90, "SE")
    side((hx, -hy + r), (hx, hy - r), SIDE_N["E"], "E"); corner(hx - r, hy - r, 0, "NE")
    side((hx - r, hy), (-hx + r, hy), SIDE_N["N"], "N"); corner(-hx + r, hy - r, 90, "NW")
    side((-hx, hy - r), (-hx, -hy + r), SIDE_N["W"], "W"); corner(-hx + r, -hy + r, 180, "SW")
    return pts


def noise(x, y, s=1.0):
    return (math.sin(x * 0.21 * s + 1.3) * math.cos(y * 0.17 * s - 0.7) + 0.6 * math.sin((x + y) * 0.33 * s + 2.1)
            + 0.35 * math.sin(x * 0.71 * s - y * 0.53 * s)) / 1.95


def side_height(x, y):
    """Wall height: tall behind (north), low toward the camera (south) so tanks stay visible."""
    # Depends on y only, so the rim rises smoothly from the south to the north with no step at
    # the corners where two sides meet.
    t = max(0.0, min(1.0, (y + FY) / (2 * FY)))
    t = t * t * (3 - 2 * t)
    return 3.0 + (9.5 - 3.0) * t
