using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Security.Policy;
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

    class PendingPropertyChanges
    {
        public List<(string, Vector4)> vectorProps;
        public List<(string, float)> floatProps;
        public List<(string, Color)> colorProps;
        public List<(string, Texture)> textureProps;
        public List<(string, bool)> keywordProps;

        public PendingPropertyChanges(int capacity)
        {
            vectorProps = new List<(string, Vector4)>(capacity);
            floatProps = new List<(string, float)>(capacity);
            colorProps = new List<(string, Color)>(capacity);
            textureProps = new List<(string, Texture)>(capacity);
            keywordProps = new List<(string, bool)>(capacity);
        }

        public int GetHash()
        {
            vectorProps.Sort();
            floatProps.Sort();
            colorProps.Sort();
            textureProps.Sort();
            keywordProps.Sort();

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
            }

            return propsHash;
        }

        public void ClearAll()
        {
            vectorProps.Clear();
            floatProps.Clear();
            colorProps.Clear();
            textureProps.Clear();
            keywordProps.Clear();
        }
    }

    Dictionary<int, Dictionary<int, Material>> m_MatTemplateIdToInstancesMap = new Dictionary<int, Dictionary<int, Material>>();
    Dictionary<Material, int> m_MatInstanceToTemplateIdMap = new Dictionary<Material, int>();
    Dictionary<Material, HashSet<Renderer>> m_MatInstanceToRenderersMap = new Dictionary<Material, HashSet<Renderer>>();

    int m_CurrentMatTemplateId = 0;
    Material m_CurrentMat = null;
    bool m_IsChangingProps = false;
    PendingPropertyChanges m_PendingPropChanges = new PendingPropertyChanges(4);
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
        m_PendingPropChanges.ClearAll();

        m_CurrentRenderer = renderer;
        m_CurrentMatIdx = matIdx;
        Material mat = renderer.sharedMaterials[matIdx];

        int matInstanceId = mat.GetInstanceID();
        bool isDynamicInstance = mat.name.EndsWith("(Instance)") || mat.name.EndsWith("(ClonedMat)");
        if (isDynamicInstance)
        {
            if (m_MatInstanceToTemplateIdMap.ContainsKey(mat))
            {
                // dynamic created material instance managed by MaterialInstanceManager
                m_CurrentMatTemplateId = m_MatInstanceToTemplateIdMap[mat];
                m_CurrentMat = mat;
            }
            else
            {
                // dynamic created material instance, but not managed by MaterialInstanceManager
                m_CurrentMatTemplateId = 0;
                m_CurrentMat = mat;
            }
        }
        else
        {
            // loaded material asset
            m_CurrentMatTemplateId = matInstanceId;
            m_CurrentMat = mat;
            if (!m_MatTemplateIdToInstancesMap.ContainsKey(matInstanceId))
            {
                m_MatTemplateIdToInstancesMap.Add(matInstanceId, new Dictionary<int, Material>());
            }
        }
    }

    void ApplyPendingPropertyChangesToMaterial(PendingPropertyChanges propChanges, Material mat)
    {
        foreach (var prop in propChanges.vectorProps)
        {
            mat.SetVector(prop.Item1, prop.Item2);
        }
        foreach (var prop in propChanges.floatProps)
        {
            mat.SetFloat(prop.Item1, prop.Item2);
        }
        foreach (var prop in propChanges.colorProps)
        {
            mat.SetColor(prop.Item1, prop.Item2);
        }
        foreach (var prop in propChanges.textureProps)
        {
            mat.SetTexture(prop.Item1, prop.Item2);
        }
        foreach (var prop in propChanges.keywordProps)
        {
            if (prop.Item2 == true)
                mat.EnableKeyword(prop.Item1);
            else
                mat.DisableKeyword(prop.Item1);
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

        // if current material instance not managed by MaterialInstanceManager, do not apply pending changes
        if (m_CurrentMatTemplateId != 0)
        {
            int propsHash = m_PendingPropChanges.GetHash();
            if (m_MatTemplateIdToInstancesMap[m_CurrentMatTemplateId].ContainsKey(propsHash))
            {
                Material foundMatInstance = m_MatTemplateIdToInstancesMap[m_CurrentMatTemplateId][propsHash];
                m_MatInstanceToRenderersMap[foundMatInstance].Add(m_CurrentRenderer);
                var sharedMaterials = m_CurrentRenderer.sharedMaterials;
                sharedMaterials[m_CurrentMatIdx] = foundMatInstance;
                m_CurrentRenderer.materials = sharedMaterials;
            }
            else
            {
                // props hash not found
                bool needToCreateNewInstance = false;
                if (m_MatInstanceToRenderersMap.ContainsKey(m_CurrentMat))
                {
                    // this is a material instance created by MaterialInstanceManager
                    HashSet<Renderer> curRenderers = m_MatInstanceToRenderersMap[m_CurrentMat];
                    if (curRenderers.Contains(m_CurrentRenderer))
                    {
                        if (curRenderers.Count == 1)
                        {
                            // if only current renderer using this material instance, we need to update current instance instead of creating new instance
                            // do not forget to remove old (propsHash, materialInstance) entry
                            Dictionary<int, Material> propsHashToMaterialsMap = m_MatTemplateIdToInstancesMap[m_CurrentMatTemplateId];
                            int propsHashToRemove = 0;
                            foreach (var kvp in propsHashToMaterialsMap)
                            {
                                if (kvp.Value == m_CurrentMat)
                                {
                                    propsHashToRemove = kvp.Key;
                                    break;
                                }
                            }
                            if (propsHashToRemove != 0)
                            {
                                propsHashToMaterialsMap.Remove(propsHashToRemove);
                                propsHashToMaterialsMap.Add(propsHash, m_CurrentMat);
                            }
                            else
                            {
                                Debug.LogError("Something went wrong, old hash props should exist.");
                            }

                            ApplyPendingPropertyChangesToMaterial(m_PendingPropChanges, m_CurrentMat);
                            // no need to set materials here, m_CurrentMat is referenced by current renderer
                            // update material's name
                            string oldMatName = m_CurrentMat.name;
                            // 11 == LengthOf(INT_MAX) + 1(Sign)
                            m_CurrentMat.name = oldMatName.Substring(0, oldMatName.Length - (11 + "ClonedMat".Length));
                            m_CurrentMat.name += string.Concat(propsHash.ToString("D11"), "(ClonedMat)");
                        }
                        else
                        {
                            // if multiple renderers using this material instance, we need to create new material instance and remove old reference
                            curRenderers.Remove(m_CurrentRenderer);
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
                    newMatInstance.name += string.Concat(propsHash.ToString("D11"), "(ClonedMat)");
                    ApplyPendingPropertyChangesToMaterial(m_PendingPropChanges, newMatInstance);
                    m_MatTemplateIdToInstancesMap[m_CurrentMatTemplateId].Add(propsHash, newMatInstance);
                    m_MatInstanceToTemplateIdMap.Add(newMatInstance, m_CurrentMatTemplateId);
                    m_MatInstanceToRenderersMap.Add(newMatInstance, new HashSet<Renderer>() { m_CurrentRenderer });

                    // NOTE: little trap here
                    // "m_CurrentRenderer.materials[m_CurrentMatIdx] = newMatInstance" will create new material instances when accessing .materials
                    var sharedMaterials = m_CurrentRenderer.sharedMaterials;
                    sharedMaterials[m_CurrentMatIdx] = newMatInstance;
                    m_CurrentRenderer.materials = sharedMaterials;
                }
            }
        }

        m_CurrentMatTemplateId = 0;
        m_CurrentMat = null;
        m_CurrentMatIdx = 0;
        m_CurrentRenderer = null;
    }

    public void SetVector(string name, Vector4 value)
    {
        if (m_CurrentMatTemplateId == 0)
        {
            // current material instance not managed by MaterialInstanceManager
            m_CurrentMat.SetColor(name, value);
        }
        else
        {
            m_PendingPropChanges.vectorProps.Add((name, value));
        }
    }

    public void SetFloat(string name, float value)
    {
        if (m_CurrentMatTemplateId == 0)
        {
            m_CurrentMat.SetFloat(name, value);
        }
        else
        {
            m_PendingPropChanges.floatProps.Add((name, value));
        }
    }

    public void SetColor(string name, Color value)
    {
        if (m_CurrentMatTemplateId == 0)
        {
            m_CurrentMat.SetColor(name, value);
        }
        else
        {
            m_PendingPropChanges.colorProps.Add((name, value));
        }
    }

    public void SetTexture(string name, Texture value)
    {
        if (m_CurrentMatTemplateId == 0)
        {
            m_CurrentMat.SetTexture(name, value);
        }
        else
        {
            m_PendingPropChanges.textureProps.Add((name, value));
        }
    }

    public void SetKeyword(string name, bool value)
    {
        if (m_CurrentMatTemplateId == 0)
        {
            if (value == true)
                m_CurrentMat.EnableKeyword(name);
            else
                m_CurrentMat.DisableKeyword(name);
        }
        else
        {
            m_PendingPropChanges.keywordProps.Add((name, value));
        }
    }

    public void DestroyMaterialInstances(Material mat)
    {
        int matInstanceId = mat.GetInstanceID();

        if (mat.name.EndsWith("(Instance)"))
        {
            Debug.LogWarningFormat("Material {0} is not a loaded asset or managed instance", mat.name);
        }
        else if (mat.name.EndsWith("(ClonedMat)"))
        {
            // this is a material instance created by MaterialInstanceManager
            int templateId = m_MatInstanceToTemplateIdMap[mat];
            Dictionary<int, Material> propsHashToMatInstanceMap = m_MatTemplateIdToInstancesMap[templateId];
            foreach (var matInst in propsHashToMatInstanceMap.Values)
            {
                CoreUtils.Destroy(matInst);
                m_MatInstanceToTemplateIdMap.Remove(matInst);
                m_MatInstanceToRenderersMap.Remove(matInst);
            }
            propsHashToMatInstanceMap.Clear();
            m_MatTemplateIdToInstancesMap.Remove(templateId);
        }
        else
        {
            if (m_MatTemplateIdToInstancesMap.ContainsKey(matInstanceId))
            {
                Dictionary<int, Material> propsHashToMatInstanceMap = m_MatTemplateIdToInstancesMap[matInstanceId];
                foreach (var matInst in propsHashToMatInstanceMap.Values)
                {
                    CoreUtils.Destroy(matInst);
                    m_MatInstanceToTemplateIdMap.Remove(matInst);
                    m_MatInstanceToRenderersMap.Remove(matInst);
                }
                propsHashToMatInstanceMap.Clear();
                m_MatTemplateIdToInstancesMap.Remove(matInstanceId);
            }
            else
            {
                Debug.LogWarningFormat("Material {0} is a loaded asset, but not managed by MaterialInstanceManager", mat.name);
            }

        }
    }

    public void DestroyAllMaterialInstances()
    {
        foreach (var matInst in m_MatInstanceToTemplateIdMap.Keys)
        {
            CoreUtils.Destroy(matInst);
        }
        m_MatInstanceToTemplateIdMap.Clear();
        m_MatTemplateIdToInstancesMap.Clear();
        m_MatInstanceToRenderersMap.Clear();
    }

    // for test
    public Dictionary<int, Dictionary<int, Material>> GetMatTemplateIdToInstancesMap()
    {
        return m_MatTemplateIdToInstancesMap;
    }

    public Dictionary<Material, int> GetMatInstanceToTemplateIdMap()
    {
        return m_MatInstanceToTemplateIdMap;
    }

    public Dictionary<Material, HashSet<Renderer>> GetMatInstanceToRenderersMap()
    {
        return m_MatInstanceToRenderersMap;
    }
}
