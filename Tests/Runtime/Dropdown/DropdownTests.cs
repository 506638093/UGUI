using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class DropdownTests : IPrebuildSetup
{
    GameObject m_PrefabRoot;

    const string kPrefabPath = "Assets/Resources/DropdownPrefab.prefab";

    public void Setup()
    {
#if UNITY_EDITOR
        var rootGO = new GameObject("rootGo");
        var canvasGO = new GameObject("Canvas", typeof(Canvas));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.transform.SetParent(rootGO.transform);

        var dropdownGO = new GameObject("Dropdown", typeof(Dropdown), typeof(RectTransform));
        var dropdownTransform = dropdownGO.GetComponent<RectTransform>();
        dropdownTransform.SetParent(canvas.transform);
        dropdownTransform.anchoredPosition = Vector2.zero;
        var dropdown = dropdownGO.GetComponent<Dropdown>();

        var templateGO = new GameObject("Template", typeof(RectTransform));
        templateGO.SetActive(false);
        var templateTransform = templateGO.GetComponent<RectTransform>();
        templateTransform.SetParent(dropdownTransform);

        var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        itemGo.transform.SetParent(templateTransform);

        dropdown.template = templateTransform;

        if (!Directory.Exists("Assets/Resources/"))
            Directory.CreateDirectory("Assets/Resources/");

        PrefabUtility.SaveAsPrefabAsset(rootGO, kPrefabPath);
        GameObject.DestroyImmediate(rootGO);
#endif
    }

    [SetUp]
    public void TestSetup()
    {
        m_PrefabRoot = Object.Instantiate(Resources.Load("DropdownPrefab")) as GameObject;
        new GameObject("Camera", typeof(Camera));
#if UNITY_EDITOR
        // add a custom sorting layer before test. It doesn't seem to be serialized so no need to remove it after test
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
        sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
        var arrayElement = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
        foreach (SerializedProperty a in arrayElement)
        {
            switch (a.name)
            {
                case "name":
                    a.stringValue = "test layer";
                    break;
                case "uniqueID":
                    a.intValue = 314159265;
                    break;
                case "locked":
                    a.boolValue = false;
                    break;
            }
        }
        tagManager.ApplyModifiedProperties();
#endif
    }

    // test for case 958281 - [UI] Dropdown list does not copy the parent canvas layer when the panel is opened
    [UnityTest]
    public IEnumerator Dropdown_Canvas()
    {
        var dropdown = m_PrefabRoot.GetComponentInChildren<Dropdown>();
        var rootCanvas = m_PrefabRoot.GetComponentInChildren<Canvas>();
        dropdown.Show();
        yield return null;
        var dropdownList = dropdown.transform.Find("Dropdown List");
        var dropdownListCanvas = dropdownList.GetComponentInChildren<Canvas>();
        Assert.AreEqual(rootCanvas.sortingLayerID, dropdownListCanvas.sortingLayerID);
        dropdown.Hide();
        yield return new WaitForSeconds(1f); // hide is not instantaneous
        rootCanvas.sortingLayerName = "test layer";
        dropdown.Show();
        yield return null;
        dropdownList = dropdown.transform.Find("Dropdown List");
        dropdownListCanvas = dropdownList.GetComponentInChildren<Canvas>();
        Assert.AreEqual(rootCanvas.sortingLayerID, dropdownListCanvas.sortingLayerID);
    }

    // test for case 935649 - open dropdown menus become unresponsive when disabled and reenabled
    [UnityTest]
    public IEnumerator Dropdown_Disable()
    {
        var dropdown = m_PrefabRoot.GetComponentInChildren<Dropdown>();
        dropdown.Show();
        dropdown.gameObject.SetActive(false);
        yield return null;
        var dropdownList = dropdown.transform.Find("Dropdown List");
        Assert.IsNull(dropdownList);
    }

    [UnityTest]
    public IEnumerator Dropdown_ResetAndClear()
    {
        var options = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
        var dropdown = m_PrefabRoot.GetComponentInChildren<Dropdown>();

        // generate a first dropdown
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.value = 3;
        yield return null;


        // clear it and generate a new one
        dropdown.ClearOptions();
        yield return null;

        // check is the value is 0
        Assert.IsTrue(dropdown.value == 0);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(m_PrefabRoot);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
#if UNITY_EDITOR
        AssetDatabase.DeleteAsset(kPrefabPath);
#endif
    }
}
