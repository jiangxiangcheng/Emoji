using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UI;

[CustomEditor(typeof(TextEx))]
//[CanEditMultipleObjects]
public class TextExEditor : UnityEditor.UI.TextEditor
{
    // TextEx m_textEx;    //TextEx对象

    SerializedProperty m_supportEmoji;

    protected override void OnEnable()
    {
        base.OnEnable();
        // m_textEx = (TextEx)target;

        m_supportEmoji = serializedObject.FindProperty("m_supportEmoji");

    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("拓展：");
        EditorGUILayout.PropertyField(m_supportEmoji, new GUIContent("IsSupportEmoji"));

        EditorGUILayout.LabelField("------------------------");

        if (GUI.changed)
            EditorUtility.SetDirty(target);
        serializedObject.ApplyModifiedProperties();

        base.OnInspectorGUI();
    }
}