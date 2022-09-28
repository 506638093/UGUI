using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(AspectRatioFitter), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the AspectRatioFitter component.
    ///   Extend this class to write a custom editor for a component derived from AspectRatioFitter.
    /// </summary>
    public class AspectRatioFitterEditor : SelfControllerEditor
    {
        SerializedProperty m_AspectMode;
        SerializedProperty m_AspectRatio;
        SerializedProperty nativeSize;
        SerializedProperty childrenList;
        SerializedProperty childrenPosList;
        SerializedProperty fitChildren;
        SerializedProperty linkToRt;

        protected virtual void OnEnable()
        {
            m_AspectMode = serializedObject.FindProperty("m_AspectMode");
            m_AspectRatio = serializedObject.FindProperty("m_AspectRatio");
            nativeSize = serializedObject.FindProperty("nativeSize");
            childrenList = serializedObject.FindProperty("childrenList");
            childrenPosList = serializedObject.FindProperty("childrenPosList");
            fitChildren = serializedObject.FindProperty("fitChildren");
            linkToRt = serializedObject.FindProperty("linkToRt");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_AspectMode);
            EditorGUILayout.Space(20f);
            EditorGUI.BeginDisabled(true);
            EditorGUILayout.PropertyField(m_AspectRatio);
            EditorGUILayout.PropertyField(nativeSize);
            EditorGUILayout.Space(20f);
            if (m_AspectMode.intValue == (int)AspectRatioFitter.AspectMode.CenterFit)
            {
                EditorGUI.EndDisabled();
                EditorGUILayout.PropertyField(linkToRt);
                EditorGUILayout.PropertyField(fitChildren);
                EditorGUI.BeginDisabled(true);
            }
            EditorGUILayout.LabelField("Children");
            for (int i = 0; i < childrenList.arraySize; i++)
            {
                var _child = (RectTransform)childrenList.GetArrayElementAtIndex(i).objectReferenceValue;
                var pos = childrenPosList.GetArrayElementAtIndex(i);
                if (_child)
                    EditorGUILayout.Vector2Field(_child.gameObject.name, pos.vector2Value);
            }
            EditorGUI.EndDisabled();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}