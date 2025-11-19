using UnityEditor;
using UnityEngine;
using Rope;

public class UnderwaterRopeSetup : EditorWindow
{
    private Vector2 scrollPosition;

    // Rope Structure
    private int segmentCount = 20;
    private float segmentLength = 0.5f;
    private float ropeWidth = 0.05f;

    // Physics Settings
    private float segmentMass = 0.1f;
    private float drag = 2f;
    private float angularDrag = 1f;
    private float jointSpring = 100f;
    private float jointDamper = 10f;

    // Rendering
    private Material ropeMaterial;
    private int splineSmoothing = 4;

    // Attachment References
    private Rigidbody startObject;
    private Rigidbody endObject;

    [MenuItem("Deep/Setup/Underwater Rope")]
    public static void ShowWindow()
    {
        GetWindow<UnderwaterRopeSetup>("Underwater Rope Setup");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Underwater Rope Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Rope Structure Section
        EditorGUILayout.LabelField("Rope Structure", EditorStyles.boldLabel);
        segmentCount = EditorGUILayout.IntSlider("Segment Count", segmentCount, 5, 100);
        segmentLength = EditorGUILayout.Slider("Segment Length", segmentLength, 0.1f, 2f);
        ropeWidth = EditorGUILayout.Slider("Rope Width", ropeWidth, 0.01f, 0.5f);

        float totalLength = segmentCount * segmentLength;
        EditorGUILayout.HelpBox($"Total Rope Length: {totalLength:F1}m", MessageType.None);

        EditorGUILayout.Space(10);

        // Physics Settings Section
        EditorGUILayout.LabelField("Physics Settings (Zero Gravity Underwater)", EditorStyles.boldLabel);
        segmentMass = EditorGUILayout.Slider("Segment Mass", segmentMass, 0.01f, 1f);
        drag = EditorGUILayout.Slider("Water Drag", drag, 0f, 10f);
        angularDrag = EditorGUILayout.Slider("Angular Drag", angularDrag, 0f, 5f);
        jointSpring = EditorGUILayout.Slider("Joint Spring", jointSpring, 0f, 500f);
        jointDamper = EditorGUILayout.Slider("Joint Damper", jointDamper, 0f, 50f);

        EditorGUILayout.Space(10);

        // Rendering Section
        EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
        ropeMaterial = (Material)EditorGUILayout.ObjectField("Rope Material", ropeMaterial, typeof(Material), false);
        splineSmoothing = EditorGUILayout.IntSlider("Spline Smoothing", splineSmoothing, 1, 8);

        EditorGUILayout.Space(10);

        // Attachment References Section
        EditorGUILayout.LabelField("Attachment Points (Both with Rigidbody)", EditorStyles.boldLabel);
        startObject = (Rigidbody)EditorGUILayout.ObjectField("Start Object", startObject, typeof(Rigidbody), true);
        endObject = (Rigidbody)EditorGUILayout.ObjectField("End Object", endObject, typeof(Rigidbody), true);

        EditorGUILayout.Space(10);

        // Action Buttons
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        GUI.enabled = startObject != null && endObject != null;
        if (GUILayout.Button("Create Rope", GUILayout.Height(30)))
        {
            CreateRopeFromSettings();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Create Rope Between Selected Objects"))
        {
            CreateRopeBetweenSelection();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Quick Setup: Select two objects with Rigidbodies in the scene,\n" +
            "then click 'Create Rope Between Selected Objects'.\n\n" +
            "Both objects will move freely with zero gravity underwater physics.",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void CreateRopeFromSettings()
    {
        if (startObject == null || endObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign both Start Object and End Object.", "OK");
            return;
        }

        CreateRope(startObject, endObject);
    }

    private void CreateRopeBetweenSelection()
    {
        GameObject[] selection = Selection.gameObjects;

        if (selection.Length != 2)
        {
            EditorUtility.DisplayDialog("Selection Error",
                "Please select exactly 2 objects with Rigidbody components.",
                "OK");
            return;
        }

        Rigidbody rb1 = selection[0].GetComponent<Rigidbody>();
        Rigidbody rb2 = selection[1].GetComponent<Rigidbody>();

        if (rb1 == null || rb2 == null)
        {
            EditorUtility.DisplayDialog("Selection Error",
                "Both selected objects must have Rigidbody components.\n\n" +
                "Add Rigidbodies to the objects first.",
                "OK");
            return;
        }

        CreateRope(rb1, rb2);
    }

    private void CreateRope(Rigidbody start, Rigidbody end)
    {
        // Create rope parent object
        GameObject ropeObject = new GameObject("UnderwaterRope");
        Undo.RegisterCreatedObjectUndo(ropeObject, "Create Underwater Rope");

        // Position between the two objects
        ropeObject.transform.position = (start.position + end.position) / 2f;

        // Add the controller
        UnderwaterRopeController controller = ropeObject.AddComponent<UnderwaterRopeController>();

        // Configure via serialized properties
        SerializedObject serializedController = new SerializedObject(controller);

        serializedController.FindProperty("segmentCount").intValue = segmentCount;
        serializedController.FindProperty("segmentLength").floatValue = segmentLength;
        serializedController.FindProperty("ropeWidth").floatValue = ropeWidth;
        serializedController.FindProperty("segmentMass").floatValue = segmentMass;
        serializedController.FindProperty("drag").floatValue = drag;
        serializedController.FindProperty("angularDrag").floatValue = angularDrag;
        serializedController.FindProperty("jointSpring").floatValue = jointSpring;
        serializedController.FindProperty("jointDamper").floatValue = jointDamper;
        serializedController.FindProperty("startObject").objectReferenceValue = start;
        serializedController.FindProperty("endObject").objectReferenceValue = end;
        serializedController.FindProperty("splineSmoothing").intValue = splineSmoothing;

        if (ropeMaterial != null)
        {
            serializedController.FindProperty("ropeMaterial").objectReferenceValue = ropeMaterial;
        }

        serializedController.ApplyModifiedProperties();

        // Configure both objects for underwater physics
        ConfigureForUnderwater(start);
        ConfigureForUnderwater(end);

        // Select the new rope
        Selection.activeGameObject = ropeObject;

        float totalLength = segmentCount * segmentLength;
        Debug.Log($"Created underwater rope with {segmentCount} segments " +
                  $"(total length: {totalLength}m) between '{start.name}' and '{end.name}'");
    }

    private void ConfigureForUnderwater(Rigidbody rb)
    {
        Undo.RecordObject(rb, "Configure for Underwater");

        // Set up for zero gravity underwater environment
        rb.useGravity = false;
        rb.linearDamping = drag;  // Use the configured water drag
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        EditorUtility.SetDirty(rb);
    }
}
