
using UnityEngine;
using UnityEditor;

public static class UpgradeVertexColoredMaterials
{
    [MenuItem("Tools/Rendering/Upgrade Vertex Colored Materials")]
    private static void UpgradeMaterials()
    {
        const string oldShaderName = "Vertex Colored Lit";
        const string newShaderName = "Custom/Vertex Colored Lit URP";

        Shader oldShader = Shader.Find(oldShaderName);
        Shader newShader = Shader.Find(newShaderName);

        if (newShader == null)
        {
            Debug.LogError(
                $"Cannot find shader: {newShaderName}");
            return;
        }

        string[] materialGuids =
            AssetDatabase.FindAssets("t:Material");

        int upgraded = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
                continue;

            if (oldShader != null &&
                material.shader != oldShader &&
                material.shader.name != oldShaderName)
                continue;

            if (material.shader.name != oldShaderName)
                continue;

            Undo.RecordObject(material, "Upgrade Vertex Shader");
            material.shader = newShader;

            EditorUtility.SetDirty(material);
            upgraded++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Upgraded {upgraded} vertex-colored materials.");
    }
}
