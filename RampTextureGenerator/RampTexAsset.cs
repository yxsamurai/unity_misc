using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

public class RampTexAsset : ScriptableObject
{
    public enum RampSize
    {
        _16 = 16,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512
    }

    public Gradient gradient = new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.black, 0),
            new GradientColorKey(Color.white, 1f),
        }
    };

    public Texture2D texture;
    public RampSize size = RampSize._32;
    [Range(-1, 1)] public float hueShift = 0f;
    [Range(-1, 1)] public float saturateShift = 0f;
    [Range(-1, 1)] public float valueShift = 0f;

    public void CreateTexture()
    {
        int width = (int)size;
        texture = new Texture2D(width, 1, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.name = this.name;
        for (int i = 0; i < width; ++i)
        {
            float time = (float)i / (width - 1);
            Color color = gradient.Evaluate(time);
            texture.SetPixel(i, 0, color);
        }
        texture.Apply();
    }

    public void ApplyGradient()
    {
        if (texture == null)
            return;

        for (int i = 0; i < texture.width; ++i)
        {
            float time = (float)i / (texture.width - 1);
            Color color = gradient.Evaluate(time);
            float alpha = color.a;
            Color.RGBToHSV(color, out float h, out float s, out float v);
            h = (h + hueShift + 1f) % 1f;
            s = Mathf.Clamp01(s + saturateShift);
            v = Mathf.Clamp01(v + valueShift);
            color = Color.HSVToRGB(h, s, v);
            color.a = alpha;
            texture.SetPixel(i, 0, color);
        }
        texture.Apply();
    }

#if UNITY_EDITOR
    public void SaveAsPNG(string path)
    {
        if (texture == null)
        {
            Debug.LogError("Texture Not Created!");
            return;
        }
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
    }

    public class CreateRampTexAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var instance = CreateInstance<RampTexAsset>();
            AssetDatabase.CreateAsset(instance, pathName);
            instance.CreateTexture();
            AssetDatabase.AddObjectToAsset(instance.texture, instance);
            AssetDatabase.SaveAssetIfDirty(instance);
            Selection.activeObject = instance;
        }
    }

    [MenuItem("Assets/Create/Debug/RampTexAsset")]
    static void CreateRampTexData()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateRampTexAsset>(), "RampTex.asset", null, null);
    }

#endif
}
