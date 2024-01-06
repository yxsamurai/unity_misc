using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Security.Policy;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class MaterialInstanceManager
{
    static MaterialInstanceManager _instance;
    public static MaterialInstanceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MaterialInstanceManager();
            }
            return _instance;
        }
    }

    public class PendingPropertyChanges
    {
        internal List<(int, Vector4)> vectorProps;
        internal List<(int, float)> floatProps;
        internal List<(int, Color)> colorProps;
        internal List<(int, Texture)> textureProps;
        internal List<(string, bool)> keywordProps;
        internal List<(string, bool)> shaderPassProps;
        internal int renderQueueProp;

        bool isDirty = false;
        int cachedHashCode = 0;

        public PendingPropertyChanges(int capacity)
        {
            vectorProps = new List<(int, Vector4)>(capacity);
            floatProps = new List<(int, float)>(capacity);
            colorProps = new List<(int, Color)>(capacity);
            textureProps = new List<(int, Texture)>(capacity);
            // keywords and shaderpasses should not be changed frequently
            keywordProps = new List<(string, bool)>(1);
            shaderPassProps = new List<(string, bool)>(1);
            renderQueueProp = 0;
        }

        static Dictionary<int, Vector4> s_VectorPropsTempDic = new Dictionary<int, Vector4>(6);
        static Dictionary<int, float> s_FloatPropsTempDic = new Dictionary<int, float>(6);
        static Dictionary<int, Color> s_ColorPropsTempDic = new Dictionary<int, Color>(6);
        static Dictionary<int, Texture> s_TexturePropsTempDic = new Dictionary<int, Texture>(6);
        static Dictionary<string, bool> s_KeywordShaderPassPropsTempDic = new Dictionary<string, bool>(6);

        public void CombineWithPreviousChanges(PendingPropertyChanges prevChanges)
        {
            s_VectorPropsTempDic.Clear();
            foreach (var kvp in prevChanges.vectorProps)
            {
                s_VectorPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            foreach (var kvp in vectorProps)
            {
                s_VectorPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            vectorProps.Clear();
            foreach (var kvp in s_VectorPropsTempDic)
            {
                vectorProps.Add((kvp.Key, kvp.Value));
            }

            s_FloatPropsTempDic.Clear();
            foreach (var kvp in prevChanges.floatProps)
            {
                s_FloatPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            foreach (var kvp in floatProps)
            {
                s_FloatPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            floatProps.Clear();
            foreach (var kvp in s_FloatPropsTempDic)
            {
                floatProps.Add((kvp.Key, kvp.Value));
            }

            s_ColorPropsTempDic.Clear();
            foreach (var kvp in prevChanges.colorProps)
            {
                s_ColorPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            foreach (var kvp in colorProps)
            {
                s_ColorPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            colorProps.Clear();
            foreach (var kvp in s_ColorPropsTempDic)
            {
                colorProps.Add((kvp.Key, kvp.Value));
            }

            s_TexturePropsTempDic.Clear();
            foreach (var kvp in prevChanges.textureProps)
            {
                s_TexturePropsTempDic[kvp.Item1] = kvp.Item2;
            }
            foreach (var kvp in textureProps)
            {
                s_TexturePropsTempDic[kvp.Item1] = kvp.Item2;
            }
            textureProps.Clear();
            foreach (var kvp in s_TexturePropsTempDic)
            {
                textureProps.Add((kvp.Key, kvp.Value));
            }

            s_KeywordShaderPassPropsTempDic.Clear();
            foreach (var kvp in prevChanges.keywordProps)
            {
                s_KeywordShaderPassPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            foreach (var kvp in keywordProps)
            {
                s_KeywordShaderPassPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            keywordProps.Clear();
            foreach (var kvp in s_KeywordShaderPassPropsTempDic)
            {
                keywordProps.Add((kvp.Key, kvp.Value));
            }

            s_KeywordShaderPassPropsTempDic.Clear();
            foreach (var kvp in prevChanges.shaderPassProps)
            {
                s_KeywordShaderPassPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            foreach (var kvp in shaderPassProps)
            {
                s_KeywordShaderPassPropsTempDic[kvp.Item1] = kvp.Item2;
            }
            shaderPassProps.Clear();
            foreach (var kvp in s_KeywordShaderPassPropsTempDic)
            {
                shaderPassProps.Add((kvp.Key, kvp.Value));
            }
            // refresh renderQueueProp to avoid generating redundancy variants
            if (renderQueueProp == 0 && prevChanges.renderQueueProp != 0)
            {
                renderQueueProp = prevChanges.renderQueueProp;
            }

            isDirty = true;
        }

        public void AddVectorProp(int nameId, Vector4 value)
        {
            vectorProps.Add((nameId, value));
            isDirty = true;
        }

        public void AddFloatProp(int nameId, float value)
        {
            floatProps.Add((nameId, value));
            isDirty = true;
        }

        public void AddColorProp(int nameId, Color value)
        {
            colorProps.Add((nameId, value));
            isDirty = true;
        }

        public void AddTextureProp(int nameId, Texture value)
        {
            textureProps.Add((nameId, value));
            isDirty = true;
        }

        public void AddKeywordProp(string name, bool value)
        {
            keywordProps.Add((name, value));
            isDirty = true;
        }

        public void AddShaderPassProp(string name, bool value)
        {
            shaderPassProps.Add((name, value));
            isDirty = true;
        }

        public void SetRenderQueueProp(int value)
        {
            renderQueueProp = value;
            isDirty = true;
        }

        public void ApplyToMaterial(Material mat)
        {
            foreach (var prop in vectorProps)
            {
                mat.SetVector(prop.Item1, prop.Item2);
            }
            foreach (var prop in floatProps)
            {
                mat.SetFloat(prop.Item1, prop.Item2);
            }
            foreach (var prop in colorProps)
            {
                mat.SetColor(prop.Item1, prop.Item2);
            }
            foreach (var prop in textureProps)
            {
                mat.SetTexture(prop.Item1, prop.Item2);
            }
            foreach (var prop in keywordProps)
            {
                if (prop.Item2 == true)
                    mat.EnableKeyword(prop.Item1);
                else
                    mat.DisableKeyword(prop.Item1);
            }
            foreach (var prop in shaderPassProps)
            {
                mat.SetShaderPassEnabled(prop.Item1, prop.Item2);
            }
            if (renderQueueProp != 0)
            {
                mat.renderQueue = renderQueueProp;
            }
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            if (isDirty)
            {
                cachedHashCode = CalculateHashCode();
                isDirty = false;
            }
            return cachedHashCode;
        }

        public int CalculateHashCode()
        {
            vectorProps.Sort();
            floatProps.Sort();
            colorProps.Sort();
            textureProps.Sort();
            keywordProps.Sort();
            shaderPassProps.Sort();

            // TODO: need a better hash algorithm?
            int propsHash = 17;
            unchecked
            {
                foreach (var prop in vectorProps)
                {
                    propsHash = propsHash * 23 + prop.GetHashCode();
                }
                foreach (var prop in floatProps)
                {
                    propsHash = propsHash * 23 + prop.GetHashCode();
                }
                foreach (var prop in colorProps)
                {
                    propsHash = propsHash * 23 + prop.GetHashCode();
                }
                foreach (var prop in textureProps)
                {
                    propsHash = propsHash * 23 + prop.GetHashCode();
                }
                foreach (var prop in keywordProps)
                {
                    propsHash = propsHash * 23 + prop.GetHashCode();
                }
                foreach (var prop in shaderPassProps)
                {
                    propsHash = propsHash * 23 + prop.GetHashCode();
                }
                if (renderQueueProp != 0)
                {
                    propsHash = propsHash * 23 + renderQueueProp;
                }
            }

            return propsHash;
        }

        public bool HasPropChanges()
        {
            return vectorProps.Count > 0 || floatProps.Count > 0 || colorProps.Count > 0
                || textureProps.Count > 0 || keywordProps.Count > 0 || shaderPassProps.Count > 0
                || renderQueueProp != 0;
        }

        public void Reset()
        {
            vectorProps.Clear();
            floatProps.Clear();
            colorProps.Clear();
            textureProps.Clear();
            keywordProps.Clear();
            shaderPassProps.Clear();
            renderQueueProp = 0;
            cachedHashCode = 0;
            isDirty = false;
        }
    }

    class PropertyChangesPool
    {
        Stack<PendingPropertyChanges> pool;

        public PropertyChangesPool(int capacity = 30)
        {
            pool = new Stack<PendingPropertyChanges>(capacity);
        }

        public PendingPropertyChanges Get()
        {
            var ret = pool.Count == 0 ? new PendingPropertyChanges(4) : pool.Pop();
            return ret;
        }

        public void Release(PendingPropertyChanges propertyChanges)
        {
            propertyChanges.Reset();
            pool.Push(propertyChanges);
        }

        public void Clear()
        {
            pool.Clear();
        }
    }

    Dictionary<Material, Dictionary<PendingPropertyChanges, Material>> m_MatTemplateToInstancesMap = new Dictionary<Material, Dictionary<PendingPropertyChanges, Material>>();
    Dictionary<Material, Material> m_MatInstanceToTemplateMap = new Dictionary<Material, Material>();
    Dictionary<Material, HashSet<int>> m_MatInstanceToRendererIdsMap = new Dictionary<Material, HashSet<int>>();

    Material m_CurrentMatTemplate = null;
    Material m_CurrentMat = null;
    bool m_IsChangingProps = false;
    PendingPropertyChanges m_PendingPropChanges = null;
    PropertyChangesPool m_PropChangesPool = new PropertyChangesPool();
    Renderer m_CurrentRenderer = null;
    uint m_CurrentMatIdx = 0;

    public void BeginMaterialChanges(Renderer renderer, uint matIdx = 0)
    {
        if (m_IsChangingProps)
        {
            Debug.LogError("Cannot change multiple materials at the same time.");
            return;
        }

        if (renderer.sharedMaterials.Length <= matIdx)
        {
            Debug.LogError("Material index overflow.");
            return;
        }

        m_IsChangingProps = true;
        m_PendingPropChanges = m_PropChangesPool.Get();

        m_CurrentRenderer = renderer;
        m_CurrentMatIdx = matIdx;
        Material mat = renderer.sharedMaterials[matIdx];

        bool isDynamicInstance = mat.name.EndsWith("(Instance)") || mat.name.EndsWith("(ClonedMat)");
        if (isDynamicInstance)
        {
            if (m_MatInstanceToTemplateMap.ContainsKey(mat))
            {
                // dynamic created material instance managed by MaterialInstanceManager
                m_CurrentMatTemplate = m_MatInstanceToTemplateMap[mat];
                m_CurrentMat = mat;
            }
            else
            {
                // dynamic created material instance, but not managed by MaterialInstanceManager
                m_CurrentMatTemplate = null;
                m_CurrentMat = mat;
            }
        }
        else
        {
            // loaded material asset
            m_CurrentMatTemplate = mat;
            m_CurrentMat = mat;
        }
    }

    public void EndMaterialChanges()
    {
        if (m_IsChangingProps == false)
        {
            Debug.LogError("Cannot apply current material changes");
            return;
        }
        m_IsChangingProps = false;

        if (!m_PendingPropChanges.HasPropChanges())
            return;

        if (m_CurrentMatTemplate == m_CurrentMat && m_CurrentMat != null)
        {
            // new encountered material template
            if (!m_MatTemplateToInstancesMap.ContainsKey(m_CurrentMatTemplate))
            {
                m_MatTemplateToInstancesMap.Add(m_CurrentMatTemplate, new Dictionary<PendingPropertyChanges, Material>());
            }
        }

        // if current material instance not managed by MaterialInstanceManager, do not apply pending changes
        if (m_CurrentMatTemplate != null)
        {
            Dictionary<PendingPropertyChanges, Material> propChangesToMaterialsMap = m_MatTemplateToInstancesMap[m_CurrentMatTemplate];
            if (m_CurrentMat.name.EndsWith("(ClonedMat)"))
            {
                PendingPropertyChanges prevChanges = null;
                foreach (var kvp in propChangesToMaterialsMap)
                {
                    if (kvp.Value == m_CurrentMat)
                    {
                        prevChanges = kvp.Key;
                        break;
                    }
                }
                if (prevChanges != null)
                    m_PendingPropChanges.CombineWithPreviousChanges(prevChanges);
            }

            if (propChangesToMaterialsMap.ContainsKey(m_PendingPropChanges))
            {
                Material foundMatInstance = propChangesToMaterialsMap[m_PendingPropChanges];
                m_MatInstanceToRendererIdsMap[foundMatInstance].Add(m_CurrentRenderer.GetInstanceID());
                if (foundMatInstance != m_CurrentMat && m_MatInstanceToRendererIdsMap.ContainsKey(m_CurrentMat))
                {
                    m_MatInstanceToRendererIdsMap[m_CurrentMat].Remove(m_CurrentRenderer.GetInstanceID());
                    // if no other renderers use this material, destroy it
                    if (m_MatInstanceToRendererIdsMap[m_CurrentMat].Count == 0)
                    {
                        m_MatInstanceToRendererIdsMap.Remove(m_CurrentMat);
                        m_MatInstanceToTemplateMap.Remove(m_CurrentMat);
                        PendingPropertyChanges toRemove = null;
                        foreach (var kvp in propChangesToMaterialsMap)
                        {
                            if (kvp.Value == m_CurrentMat)
                            {
                                toRemove = kvp.Key;
                                break;
                            }
                        }
                        if (toRemove != null)
                        {
                            propChangesToMaterialsMap.Remove(toRemove);
                        }
                        CoreUtils.Destroy(m_CurrentMat);
                    }
                }
                var sharedMaterials = m_CurrentRenderer.sharedMaterials;
                sharedMaterials[m_CurrentMatIdx] = foundMatInstance;
                m_CurrentRenderer.materials = sharedMaterials;
                m_PropChangesPool.Release(m_PendingPropChanges);
            }
            else
            {
                // propChanges not found
                bool needToCreateNewInstance = false;
                if (m_MatInstanceToRendererIdsMap.ContainsKey(m_CurrentMat))
                {
                    // this is a material instance created by MaterialInstanceManager
                    HashSet<int> curRendererIds = m_MatInstanceToRendererIdsMap[m_CurrentMat];
                    if (curRendererIds.Contains(m_CurrentRenderer.GetInstanceID()))
                    {
                        if (curRendererIds.Count == 1)
                        {
                            // if only current renderer using this material instance, we need to update current instance instead of creating new instance
                            // do not forget to remove old (propsHash, materialInstance) entry
                            PendingPropertyChanges propChangesToRemove = null;
                            foreach (var kvp in propChangesToMaterialsMap)
                            {
                                if (kvp.Value == m_CurrentMat)
                                {
                                    propChangesToRemove = kvp.Key;
                                    break;
                                }
                            }
                            if (propChangesToRemove != null)
                            {
                                propChangesToMaterialsMap.Remove(propChangesToRemove);
                                m_PropChangesPool.Release(propChangesToRemove);
                                propChangesToMaterialsMap.Add(m_PendingPropChanges, m_CurrentMat);
                            }
                            else
                            {
                                Debug.LogError("Something went wrong, old hash props should exist.");
                            }

                            m_PendingPropChanges.ApplyToMaterial(m_CurrentMat);
                            // no need to set materials here, m_CurrentMat is referenced by current renderer
                            // update material's name
                            m_CurrentMat.name = m_CurrentMatTemplate.name + string.Concat(m_PendingPropChanges.GetHashCode().ToString("D11"), "(ClonedMat)");
                        }
                        else
                        {
                            // if multiple renderers using this material instance, we need to create new material instance and remove old reference
                            curRendererIds.Remove(m_CurrentRenderer.GetInstanceID());
                            needToCreateNewInstance = true;
                        }
                    }
                }
                else
                {
                    // this is a loaded material asset
                    needToCreateNewInstance = true;
                }

                if (needToCreateNewInstance)
                {
                    var newMatInstance = new Material(m_CurrentMat);
                    newMatInstance.name = m_CurrentMatTemplate.name + string.Concat(m_PendingPropChanges.GetHashCode().ToString("D11"), "(ClonedMat)");
                    m_PendingPropChanges.ApplyToMaterial(newMatInstance);
                    m_MatTemplateToInstancesMap[m_CurrentMatTemplate].Add(m_PendingPropChanges, newMatInstance);
                    m_MatInstanceToTemplateMap.Add(newMatInstance, m_CurrentMatTemplate);
                    m_MatInstanceToRendererIdsMap.Add(newMatInstance, new HashSet<int>() { m_CurrentRenderer.GetInstanceID() });

                    // NOTE: little trap here
                    // "m_CurrentRenderer.materials[m_CurrentMatIdx] = newMatInstance" will create new material instances when accessing .materials
                    var sharedMaterials = m_CurrentRenderer.sharedMaterials;
                    sharedMaterials[m_CurrentMatIdx] = newMatInstance;
                    m_CurrentRenderer.materials = sharedMaterials;
                }
            }
        }

        m_CurrentMatTemplate = null;
        m_CurrentMat = null;
        m_CurrentMatIdx = 0;
        m_CurrentRenderer = null;
    }

    public void SetVector(int nameId, Vector4 value)
    {
        if (!m_CurrentMat.HasVector(nameId))
            return;

        if (m_CurrentMatTemplate == null)
        {
            // current material instance not managed by MaterialInstanceManager
            m_CurrentMat.SetColor(nameId, value);
        }
        else
        {
            m_PendingPropChanges.AddVectorProp(nameId, value);
        }
    }

    public void SetFloat(int nameId, float value)
    {
        if (!m_CurrentMat.HasFloat(nameId))
            return;

        if (m_CurrentMatTemplate == null)
        {
            m_CurrentMat.SetFloat(nameId, value);
        }
        else
        {
            m_PendingPropChanges.AddFloatProp(nameId, value);
        }
    }

    public void SetColor(int nameId, Color value)
    {
        if (!m_CurrentMat.HasColor(nameId))
            return;

        if (m_CurrentMatTemplate == null)
        {
            m_CurrentMat.SetColor(nameId, value);
        }
        else
        {
            m_PendingPropChanges.AddColorProp(nameId, value);
        }
    }

    public void SetTexture(int nameId, Texture value)
    {
        if (!m_CurrentMat.HasTexture(nameId))
            return;

        if (m_CurrentMatTemplate == null)
        {
            m_CurrentMat.SetTexture(nameId, value);
        }
        else
        {
            m_PendingPropChanges.AddTextureProp(nameId, value);
        }
    }

    [LuaToStringOptimize]
    public void SetKeyword(string name, bool value)
    {
        if (m_CurrentMatTemplate == null)
        {
            if (value == true)
                m_CurrentMat.EnableKeyword(name);
            else
                m_CurrentMat.DisableKeyword(name);
        }
        else
        {
            m_PendingPropChanges.AddKeywordProp(name, value);
        }
    }

    [LuaToStringOptimize]
    public void SetShaderPassEnabled(string name, bool value)
    {
        if (m_CurrentMatTemplate == null)
        {
            m_CurrentMat.SetShaderPassEnabled(name, value);
        }
        else
        {
            m_PendingPropChanges.AddShaderPassProp(name, value);
        }
    }

    public void SetRenderQueue(int queue)
    {
        if (m_CurrentMatTemplate == null)
        {
            m_CurrentMat.renderQueue = queue;
        }
        else
        {
            m_PendingPropChanges.SetRenderQueueProp(queue);
        }
    }

    public void ReleaseMaterialInstances(Material mat, bool forceDestroy = false)
    {
        int matInstanceId = mat.GetInstanceID();

        if (mat.name.EndsWith("(Instance)"))
        {
            Debug.LogWarningFormat("Material {0} is not a loaded asset or managed instance", mat.name);
        }
        else if (mat.name.EndsWith("(ClonedMat)"))
        {
            // this is a material instance created by MaterialInstanceManager
            Material matTemplate = m_MatInstanceToTemplateMap[mat];
            Dictionary<PendingPropertyChanges, Material> propChangesToMatInstanceMap = m_MatTemplateToInstancesMap[matTemplate];
            foreach (var matInst in propChangesToMatInstanceMap.Values)
            {
                if (forceDestroy)
                {
                    CoreUtils.Destroy(matInst);
                }
                m_MatInstanceToTemplateMap.Remove(matInst);
                m_MatInstanceToRendererIdsMap.Remove(matInst);
            }
            foreach (var propChanges in propChangesToMatInstanceMap.Keys)
            {
                m_PropChangesPool.Release(propChanges);
            }
            propChangesToMatInstanceMap.Clear();
            m_MatTemplateToInstancesMap.Remove(matTemplate);
        }
        else
        {
            if (m_MatTemplateToInstancesMap.ContainsKey(mat))
            {
                Dictionary<PendingPropertyChanges, Material> propChangesToMatInstanceMap = m_MatTemplateToInstancesMap[mat];
                foreach (var matInst in propChangesToMatInstanceMap.Values)
                {
                    if (forceDestroy)
                    {
                        CoreUtils.Destroy(matInst);
                    }
                    m_MatInstanceToTemplateMap.Remove(matInst);
                    m_MatInstanceToRendererIdsMap.Remove(matInst);
                }
                foreach (var propChanges in propChangesToMatInstanceMap.Keys)
                {
                    m_PropChangesPool.Release(propChanges);
                }
                propChangesToMatInstanceMap.Clear();
                m_MatTemplateToInstancesMap.Remove(mat);
            }
            else
            {
                Debug.LogWarningFormat("Material {0} is a loaded asset, but not managed by MaterialInstanceManager", mat.name);
            }

        }
    }

    public void ReleaseAllMaterialInstances(bool forceDestroy = false)
    {
        if (forceDestroy)
        {
            foreach (var matInst in m_MatInstanceToTemplateMap.Keys)
            {
                CoreUtils.Destroy(matInst);
            }
        }
        m_MatInstanceToTemplateMap.Clear();
        m_MatTemplateToInstancesMap.Clear();
        m_MatInstanceToRendererIdsMap.Clear();
        m_PropChangesPool.Clear();
    }

    // for test
    public Dictionary<Material, Dictionary<PendingPropertyChanges, Material>> GetMatTemplateToInstancesMap()
    {
        return m_MatTemplateToInstancesMap;
    }

    public Dictionary<Material, Material> GetMatInstanceToTemplateMap()
    {
        return m_MatInstanceToTemplateMap;
    }

    public Dictionary<Material, HashSet<int>> GetMatInstanceToRendererIdsMap()
    {
        return m_MatInstanceToRendererIdsMap;
    }
}
