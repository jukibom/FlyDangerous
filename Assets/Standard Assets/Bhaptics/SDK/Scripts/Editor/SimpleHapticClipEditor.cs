using Bhaptics.Tact.Unity;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleHapticClip), true)]
public class SimpleHapticClipEditor : HapticClipEditor
{
    protected SimpleHapticClip m_targetScript
    {
        get
        {
            return targetScript as SimpleHapticClip;
        }
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        FeedbackTypeUI();

        PositionUI();

        var feedbackTypeProperty = serializedObject.FindProperty("Mode");

        switch (feedbackTypeProperty.enumNames[feedbackTypeProperty.enumValueIndex])
        {
            case "DotMode":
                DotPointUI();
                break;
            case "PathMode":
                PathPointUI();
                break;
        }

        TimeMillisUI();

        ResetUI();

        GUILayout.Space(20);
        PlayUI();

        serializedObject.ApplyModifiedProperties();
    }

    private void FeedbackTypeUI()
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Mode"));

        GUILayout.EndHorizontal();
    }

    private void TimeMillisUI()
    {
        GUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 100f;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TimeMillis"), new GUIContent("Time (ms)"));

        GUILayout.EndHorizontal();
    }

    private void PositionUI()
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Position"));

        GUILayout.EndHorizontal();
    }

    private void PathPointUI()
    {
        GUILayout.BeginHorizontal();
        var points = serializedObject.FindProperty("Points");
        EditorGUILayout.LabelField(points.name, EditorStyles.boldLabel);
        if (GUILayout.Button("  +  ", GUILayout.Width(50)))
        {
            int inserted = points.arraySize;
            points.InsertArrayElementAtIndex(inserted);
            points.GetArrayElementAtIndex(inserted).FindPropertyRelative("X").floatValue = 0.5f;
            points.GetArrayElementAtIndex(inserted).FindPropertyRelative("Y").floatValue = 0.5f;
            points.GetArrayElementAtIndex(inserted).FindPropertyRelative("Intensity").intValue = 100;
        }
        GUILayout.EndHorizontal();


        for (var index = 0; index < points.arraySize; index++)
        {
            GUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 15f;

            SerializedProperty property = points.GetArrayElementAtIndex(index);

            SerializedProperty x = property.FindPropertyRelative("X");
            SerializedProperty y = property.FindPropertyRelative("Y");
            SerializedProperty intensity = property.FindPropertyRelative("Intensity");

            x.floatValue = Mathf.Min(1, Mathf.Max(0, x.floatValue));
            y.floatValue = Mathf.Min(1, Mathf.Max(0, y.floatValue));
            intensity.intValue = Mathf.Min(100, Mathf.Max(0, intensity.intValue));

            EditorGUILayout.PropertyField(x, new GUIContent(x.name));
            EditorGUILayout.PropertyField(y, new GUIContent(y.name));
            GUILayout.Label(new GUIContent(intensity.name, "means the intensity of the motor from 0 to 100."), GUILayout.Width(55));
            EditorGUILayout.PropertyField(intensity, GUIContent.none);

            if (GUILayout.Button(new GUIContent("-", "delete"), GUILayout.Width(50)))
            {
                points.DeleteArrayElementAtIndex(index);
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DotPointUI()
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Dot Points", EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        var dotPoints = serializedObject.FindProperty("DotPoints");

        for (var index = 0; index < dotPoints.arraySize; index++)
        {

            if (index % 5 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            EditorGUIUtility.labelWidth = 20f;
            SerializedProperty property = dotPoints.GetArrayElementAtIndex(index);

            property.intValue = Mathf.Min(100, Mathf.Max(0, property.intValue));
            EditorGUILayout.PropertyField(property, new GUIContent("" + (index + 1)));
        }
        GUILayout.EndHorizontal();
    }

}
