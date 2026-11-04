IT 201 — WALL-MOUNTED HIGH SCORE SIGN (UNITY 6000.3)
==================================================

THIS IS A 3D OBJECT, NOT AN ON-SCREEN HUD.

INSTALL:
1. Stop Play mode in Unity.
2. Unzip IT201_WallHighScore.zip.
3. Drag the entire IT201_WallHighScore folder into the ASSETS folder
   of your Unity Project window (not into the Hierarchy).
4. Wait for scripts to compile. The editor automatically creates:
   Assets/IT201_WallHighScore/HighScore_WallSign.prefab
5. Drag HighScore_WallSign.prefab from Assets into your Scene.
6. Move/rotate it onto your wall. The FRONT of the sign faces local -Z.
   From the Scene view, rotate Y by 180 degrees if it faces the wall.
7. Scale the root uniformly if you want a larger or smaller sign.
8. Press Play. It will display the saved high score, or red 0 if none.

If the prefab is not generated automatically, use:
Tools > IT 201 > Create High Score Wall Sign Prefab

The wall sign has a real thickness (gray frame + black face), a Box
Collider, and 3D white/red text. It remains attached to the wall as
part of the world when you move around with the first-person camera.

The high score key is:
IT201_WhackAMole_HighScore
This is the same key as in your WhackAMoleSession.cs script.
The game session saves the score after a round. The wall sign reads
it automatically (about four times per second while playing).

IMPORTANT: If you still have the earlier HighScorePanel prefab from
our previous ZIP in the Hierarchy, DELETE/DISABLE that old object.
The old one used OnGUI and was locked to the screen.

The code does not change any of your existing scripts or board.
Prefab creation requires the Unity Editor, so the .prefab is generated
in your own project after importing these files.
