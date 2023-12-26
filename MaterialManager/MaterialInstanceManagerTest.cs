using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Rendering;

public class MaterialInstanceManagerTest
{
    GameObject m_TestGO1;
    GameObject m_TestGO2;
    GameObject m_TestGO3;
    Material m_TestMat;

    [SetUp]
    public void Setup()
    {
        m_TestGO1 = new GameObject("TestGO1");
        m_TestGO2 = new GameObject("TestGO2");
        m_TestGO3 = new GameObject("TestGO3");
        m_TestMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        m_TestGO1.AddComponent<MeshRenderer>();
        m_TestGO2.AddComponent<MeshRenderer>();
        m_TestGO3.AddComponent<MeshRenderer>();
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(m_TestGO1);
        Object.DestroyImmediate(m_TestGO2);
        Object.DestroyImmediate(m_TestGO3);
        Object.DestroyImmediate(m_TestMat);
    }

    void Reset()
    {
        m_TestGO1.GetComponent<MeshRenderer>().material = m_TestMat;
        m_TestGO2.GetComponent<MeshRenderer>().material = m_TestMat;
        m_TestGO3.GetComponent<MeshRenderer>().material = m_TestMat;
        MaterialInstanceManager.Instance.DestroyAllMaterialInstances();
    }

    [Test]
    public void ChangeColorCreateOneInstance()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        var mr2 = m_TestGO2.GetComponent<MeshRenderer>();
        var mr3 = m_TestGO3.GetComponent<MeshRenderer>();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr3);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.AreSame(mr1.sharedMaterial, mr2.sharedMaterial);
        Assert.AreSame(mr2.sharedMaterial, mr3.sharedMaterial);
        Assert.IsTrue(mr1.sharedMaterial.GetColor("_BaseColor") == Color.red);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Count == 3);
    }

    [Test]
    public void ChangeColorCreateTwoInstances()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        var mr2 = m_TestGO2.GetComponent<MeshRenderer>();
        var mr3 = m_TestGO3.GetComponent<MeshRenderer>();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.green);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr3);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.AreSame(mr1.sharedMaterial, mr3.sharedMaterial);
        Assert.AreNotSame(mr2.sharedMaterial, mr3.sharedMaterial);
        Assert.IsTrue(mr1.sharedMaterial.GetColor("_BaseColor") == Color.red);
        Assert.IsTrue(mr2.sharedMaterial.GetColor("_BaseColor") == Color.green);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 2);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 2
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Count == 2
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr2.sharedMaterial].Count == 1);
    }

    [Test]
    public void ChangeMaterialMultipleTimesCreateCorrectInstanceCount()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        var mr2 = m_TestGO2.GetComponent<MeshRenderer>();
        var mr3 = m_TestGO3.GetComponent<MeshRenderer>();

        // 1, change to same color
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr3);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.AreSame(mr1.sharedMaterial, mr2.sharedMaterial);
        Assert.AreSame(mr2.sharedMaterial, mr3.sharedMaterial);
        Assert.IsTrue(mr1.sharedMaterial.GetColor("_BaseColor") == Color.red);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Count == 3);

        // 2, change mr1's color
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.green);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.AreSame(mr2.sharedMaterial, mr3.sharedMaterial);
        Assert.IsTrue(mr1.sharedMaterial.GetColor("_BaseColor") == Color.green);
        Assert.IsTrue(mr2.sharedMaterial.GetColor("_BaseColor") == Color.red);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 2);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 2
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr2.sharedMaterial].Count == 2);

        // 3, change mr2's color
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.yellow);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.AreNotSame(mr1.sharedMaterial, mr2.sharedMaterial);
        Assert.AreNotSame(mr2.sharedMaterial, mr3.sharedMaterial);
        Assert.IsTrue(mr1.sharedMaterial.GetColor("_BaseColor") == Color.green);
        Assert.IsTrue(mr2.sharedMaterial.GetColor("_BaseColor") == Color.yellow);
        Assert.IsTrue(mr3.sharedMaterial.GetColor("_BaseColor") == Color.red);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 3);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1
            && MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap()[m_TestMat.GetInstanceID()].Count == 3);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 3
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr2.sharedMaterial].Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr3.sharedMaterial].Count == 1);
    }

    [Test]
    public void ChangeMaterialUsedByOneRendererDoNotCreateMultipleInstances()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        for (int i = 0; i < 5; ++i)
        {
            MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
            MaterialInstanceManager.Instance.SetColor("_BaseColor", new Color(i / 5f, 0, 0, 1));
            MaterialInstanceManager.Instance.SetFloat("_Cutoff", i / 5f);
            MaterialInstanceManager.Instance.EndMaterialChanges();
        }

        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Contains(mr1));

    }

    [Test]
    public void ChangeMaterialPropsWithDiffOrderOnlyCreateOneInstance()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        var mr2 = m_TestGO2.GetComponent<MeshRenderer>();
        var mr3 = m_TestGO3.GetComponent<MeshRenderer>();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.SetTexture("_BaseMap", Texture2D.blackTexture);
        MaterialInstanceManager.Instance.SetFloat("_Smoothness", 0.5f);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetTexture("_BaseMap", Texture2D.blackTexture);
        MaterialInstanceManager.Instance.SetFloat("_Smoothness", 0.5f);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr3);
        MaterialInstanceManager.Instance.SetTexture("_BaseMap", Texture2D.blackTexture);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.SetFloat("_Smoothness", 0.5f);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.AreSame(mr1.sharedMaterial, mr2.sharedMaterial);
        Assert.AreSame(mr2.sharedMaterial, mr3.sharedMaterial);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 1
            && MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap()[mr1.sharedMaterial].Count == 3);
    }

    [Test]
    public void AlreadyCreatedInstanceApplyChangesByManager()
    {
        Reset();

        // create mat instance, renderer.material throw error in editor
        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        mr1.material = new Material(m_TestMat);
        mr1.sharedMaterial.name += "(Instance)";
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 0);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 0);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 0);
        Assert.IsTrue(mr1.sharedMaterial.GetColor("_BaseColor") == Color.red);
    }

    [Test]
    public void DestroyInstancesByMatTemplate()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        var mr2 = m_TestGO2.GetComponent<MeshRenderer>();
        var mr3 = m_TestGO3.GetComponent<MeshRenderer>();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.green);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr3);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.yellow);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 3);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 3);

        MaterialInstanceManager.Instance.DestroyMaterialInstances(m_TestMat);

        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 0);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 0);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 0);
    }

    [Test]
    public void DestroyInstancesByMatInstance()
    {
        Reset();

        var mr1 = m_TestGO1.GetComponent<MeshRenderer>();
        var mr2 = m_TestGO2.GetComponent<MeshRenderer>();
        var mr3 = m_TestGO3.GetComponent<MeshRenderer>();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr1);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.red);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr2);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.green);
        MaterialInstanceManager.Instance.EndMaterialChanges();
        MaterialInstanceManager.Instance.BeginMaterialChanges(mr3);
        MaterialInstanceManager.Instance.SetColor("_BaseColor", Color.yellow);
        MaterialInstanceManager.Instance.EndMaterialChanges();

        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 3);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 1);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 3);

        MaterialInstanceManager.Instance.DestroyMaterialInstances(mr1.sharedMaterial);

        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToTemplateIdMap().Count == 0);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatTemplateIdToInstancesMap().Count == 0);
        Assert.IsTrue(MaterialInstanceManager.Instance.GetMatInstanceToRenderersMap().Count == 0);
    }
}