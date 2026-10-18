// Scripts/Editor/PropAssigner.cs
// Menu: Coffee > Assign Pack Props And Render Icons
// Finds 3D prefabs from your Asset Store packs by name, assigns them to the
// matching IngredientSO (Prefab 3D field), then renders icons from them.
// Edit the Map table below to change which prop each ingredient uses.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PropAssigner
{
    // ingredient id -> prefab names to try, in order. Names must match exactly.
    static readonly (string id, string[] prefabs)[] Map =
    {
        ("espresso",      new[] { "Cup_With_Coffee", "Takeaway_Coffee_1" }),
        ("milk",          new[] { "Jar" }),
        // No matching props in the pack for these yet. Add names here when you find some:
        ("oat_milk",      new string[0]),
        ("vanilla",       new string[0]),
        ("caramel",       new string[0]),
        ("chocolate",     new string[0]),
        ("whipped_cream", new string[0]),
        ("cinnamon",      new string[0]),
    };

    [MenuItem("Coffee/Assign Pack Props And Render Icons")]
    static void Run()
    {
        var ingredients = AssetDatabase.FindAssets("t:IngredientSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<IngredientSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null)
            .ToList();

        var matched = new List<IngredientSO>();
        var missing = new List<string>();

        foreach (var (id, names) in Map)
        {
            var ing = ingredients.FirstOrDefault(i => i.id == id);
            if (ing == null) continue;

            GameObject prefab = null;
            foreach (var n in names)
            {
                prefab = FindPrefab(n);
                if (prefab != null) break;
            }

            if (prefab == null) { missing.Add(ing.displayName); continue; }

            ing.prefab3D = prefab;
            EditorUtility.SetDirty(ing);
            matched.Add(ing);
        }

        AssetDatabase.SaveAssets();

        if (matched.Count > 0)
        {
            Selection.objects = matched.ToArray();
            EditorApplication.ExecuteMenuItem("Coffee/Regenerate Icons For Selected Ingredients");
        }

        string msg = $"Assigned props and rendered icons for {matched.Count} ingredient(s).";
        if (missing.Count > 0)
            msg += "\n\nNo prop assigned (still using placeholder squares):\n- " + string.Join("\n- ", missing);
        EditorUtility.DisplayDialog("Coffee", msg, "OK");
    }

    static GameObject FindPrefab(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }
}
