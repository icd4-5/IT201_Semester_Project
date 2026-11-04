# Whack-a-Mole board (Unity 6)

## Import — drag and drop
1. Unzip this folder.
2. In Unity, open the Project window and create a folder `Assets/WhackAMole`.
3. Drag ALL FOUR files from this folder (`WhackAMole_Board.obj`, `.mtl`, and both `.png`) into that Unity folder together.
4. Wait for Unity to import, then drag `WhackAMole_Board` from the Project window into your Scene or Hierarchy.
5. Set the new object's Transform Position to `(0, 0, 0)` and Scale to `(1, 1, 1)` to start. Move it wherever you want.

## Model details
- One combined, real 3D mesh: no cylinders need to be subtracted.
- 6 units × 6 units footprint, 1 unit thick.
- 9 ROUND, half-depth, closed-bottom wells arranged 3 × 3.
- Each well has diameter 1.4 units and is recessed 0.5 units below the top.
- Bottom of board: Y = -0.5; top: Y = +0.5; bottom of wells: Y = 0.0.
- Hole centers (x,z): (-2,2), (0,2), (2,2), (-2,0), (0,0), (2,0), (-2,-2), (0,-2), (2,-2).
- 64 segments per circular hole (smooth).
- Two materials: Board and HoleInterior, with different image textures.
- Approx. 3k triangles; good for a small game prop.

## Custom skins later
The board mesh uses two separate material slots / surface assignments:
- `Board` = top + outer walls + underside.
- `HoleInterior` = all nine inner cylinder walls and floors.

To change the look, make your own Unity Materials and assign them to the imported model's Mesh Renderer materials slots. You can replace the supplied textures or assign entirely different shaders. Your shape won't change.

If you see white/pink/default materials after import, create two Unity materials, assign the supplied textures as their Base Maps, and drag those materials into the two Mesh Renderer slots. Unity can also extract/import the OBJ's material definitions via the model Import Settings > Materials tab.

For Whack-a-Mole game objects, put the mole's initial position at a hole center with Y = 0.0. Script it to rise above Y = 0.5 and lower back down.

Optional: Add a Mesh Collider for precise hole collision. A Box Collider is faster if only the board's outer bounds matter. For a non-convex Mesh Collider, do not attach a dynamic Rigidbody to the same object.
