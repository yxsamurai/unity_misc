using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RampTexAsset))]
public class RampTexAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var obj = target as RampTexAsset;
        if (obj == null)
            return;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            obj.ApplyGradient();
            EditorUtility.SetDirty(obj);
        }

        if (GUILayout.Button("Reset Ramp Texture"))
        {
            AssetDatabase.RemoveObjectFromAsset(obj.texture);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
            obj.CreateTexture();
            AssetDatabase.AddObjectToAsset(obj.texture, obj);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
        }

        if (GUILayout.Button("Save As PNG"))
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            int offset = "Assets".Length;
            int len = assetPath.Length - offset - ".asset".Length;
            string path = Application.dataPath + assetPath.Substring(offset, len) + ".png";
            if (obj.texture == null)
            {
                Debug.LogError("Texture Not Created!");
                return;
            }
            byte[] bytes = obj.texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string pngPath = assetPath.Substring(0, assetPath.Length - ".asset".Length);
            pngPath += ".png";
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Default;
            importer.textureFormat = TextureImporterFormat.ARGB32;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    public override bool HasPreviewGUI()
    {
        return true;
    }

    public override void OnPreviewGUI(Rect r, GUIStyle style)
    {
        var obj = target as RampTexAsset;
        if (obj == null || obj.texture == null)
            return;

        GUI.DrawTexture(r, obj.texture);
    }


}
