#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class CreateHighScoreWallSign
{
    private const string Root = "Assets/IT201_WallHighScore";
    private const string PrefabPath = Root + "/HighScore_WallSign.prefab";

    static CreateHighScoreWallSign()
    {
        // After the scripts finish importing, generate the ready-to-place prefab once.
        EditorApplication.delayCall += CreateIfMissing;
    }

    [MenuItem("Tools/IT 201/Create High Score Wall Sign Prefab")]
    public static void CreateFromMenu()
    {
        CreateIfMissing();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("High score wall sign ready: " + PrefabPath +
                      "  | Drag the prefab from the Project window onto your wall.");
        }
    }

    private static void CreateIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        EnsureFolder(Root);
        EnsureFolder(Root + "/Materials");

        Material gray = GetOrMakeMaterial(Root + "/Materials/WallSign_Gray.mat",
            new Color(0.58f, 0.58f, 0.58f, 1f));
        Material black = GetOrMakeMaterial(Root + "/Materials/WallSign_Black.mat",
            Color.black);

        if (gray == null || black == null)
        {
            Debug.LogError("Unable to create wall sign materials.");
            return;
        }

        GameObject root = new GameObject("HighScore_WallSign");
        try
        {
            // This object is an actual 3D sign, with thickness and collision.
            MakeCube("Gray_Border_3D", root.transform,
                Vector3.zero, new Vector3(3.8f, 0.85f, 0.09f), gray, true);

            // The front of the sign faces its local -Z direction.
            MakeCube("Black_Face_3D", root.transform,
                new Vector3(0f, 0f, -0.052f), new Vector3(3.66f, 0.71f, 0.025f),
                black, false);

            GameObject lettering = new GameObject("HighScore_Text_WhiteAndRed");
            lettering.transform.SetParent(root.transform, false);
            lettering.transform.localPosition = new Vector3(0f, -0.015f, -0.087f);

            TextMesh text = lettering.AddComponent<TextMesh>();
            text.text = "High score: <color=#FF3030>0</color>";
            text.richText = true;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 100;
            text.characterSize = 0.34f;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;

            // LegacyRuntime is Unity's built-in runtime font in Unity 6.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                text.font = font;
                MeshRenderer textRenderer = lettering.GetComponent<MeshRenderer>();
                if (textRenderer != null)
                {
                    textRenderer.sharedMaterial = font.material;
                    textRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    textRenderer.receiveShadows = false;
                }
            }
            else
            {
                Debug.LogWarning("No built-in Unity font was found. Assign any Font to the sign's TextMesh.");
            }

            HighScoreWallSign scoreSign = root.AddComponent<HighScoreWallSign>();
            scoreSign.SetDisplay(text);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created a real 3D high-score wall sign prefab at " + PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void MakeCube(string name, Transform parent, Vector3 position,
        Vector3 scale, Material material, bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = scale;

        MeshRenderer meshRenderer = cube.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        if (!keepCollider)
        {
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }
    }

    private static Material GetOrMakeMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        if (slash < 0) return;
        string parent = path.Substring(0, slash);
        string child = path.Substring(slash + 1);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
