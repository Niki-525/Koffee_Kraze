// Editor/IconGenerator.cs
// Place this file in a folder named "Editor" (e.g. Assets/Scripts/Editor/).
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders a 2D icon sprite from an IngredientSO's 3D prefab and assigns it
/// to the IngredientSO's icon field. Select one or more IngredientSO assets
/// in the Project window, then use the Coffee menu.
///
/// Note: the preview background is set to transparent; if your Unity version
/// renders it opaque, tweak backgroundColor / clear flags below.
/// </summary>
public static class IconGenerator
{
    const int IconSize = 256;
    const string OutputFolder = "Assets/Art/GeneratedIcons";

    [MenuItem("Coffee/Generate Missing Icons For Selected Ingredients")]
    static void GenerateMissing() => Run(overwrite: false);

    [MenuItem("Coffee/Regenerate Icons For Selected Ingredients")]
    static void Regenerate() => Run(overwrite: true);

    static void Run(bool overwrite)
    {
        var selected = Selection.GetFiltered<IngredientSO>(SelectionMode.Assets);
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Icon Generator", "Select one or more IngredientSO assets first.", "OK");
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        int made = 0, skipped = 0;

        foreach (var ing in selected)
        {
            if (ing.prefab3D == null || (!overwrite && ing.icon != null)) { skipped++; continue; }

            Texture2D tex = Render(ing.prefab3D);
            if (tex == null) { skipped++; continue; }

            string fileName = string.IsNullOrEmpty(ing.id) ? ing.name : ing.id;
            string path = $"{OutputFolder}/{fileName}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            ing.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorUtility.SetDirty(ing);
            made++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[IconGenerator] Generated {made} icon(s), skipped {skipped}.");
    }

    // Solid background (matches the tray tiles) so the icon never depends on alpha support.
    static readonly Color Background = new Color(0.42f, 0.28f, 0.20f, 1f);

    static Texture2D Render(GameObject prefab)
    {
        var root = new GameObject("__IconRig") { hideFlags = HideFlags.HideAndDontSave };
        root.transform.position = new Vector3(5000f, 5000f, 5000f);   // far from anything in the open scene
        RenderTexture rt = null;
        Texture2D tex = null;
        var previous = RenderTexture.active;

        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[IconGenerator] {prefab.name} has no renderers.");
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            var camGo = new GameObject("__IconCam");
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            cam.fieldOfView = 30f;

            float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            Vector3 dir = new Vector3(0.4f, 0.5f, -1f).normalized;   // slightly above and to the side
            cam.transform.position = bounds.center + dir * dist;
            cam.transform.LookAt(bounds.center);

            var lightGo = new GameObject("__IconLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            rt = RenderTexture.GetTemporary(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();

            // Sanity check: warn if nothing but background was drawn.
            var px = tex.GetPixels32();
            Color32 bg = Background;
            int same = 0;
            foreach (var p in px)
                if (Mathf.Abs(p.r - bg.r) < 4 && Mathf.Abs(p.g - bg.g) < 4 && Mathf.Abs(p.b - bg.b) < 4) same++;
            if (same > px.Length * 0.995f)
                Debug.LogWarning($"[IconGenerator] {prefab.name} rendered blank. Its materials may need upgrading to your render pipeline (Edit > Rendering > Materials).");

            return tex;
        }
        finally
        {
            RenderTexture.active = previous;
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(root);
        }
    }
}
#endif
