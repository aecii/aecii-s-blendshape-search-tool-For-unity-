// aecii's Blendshape Search tool
// Place this file in: Assets/aecii_3d/Editor/BlendShapeSearchWindow.cs
// Open via: Window > aecii's Blendshape Search tool
//
// by aecii | https://x.com/aecii_3d
// 2026-05-16---2235


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BlendShapeSearchWindow : EditorWindow
{
    private SkinnedMeshRenderer targetRenderer;
    private string searchQuery = "";
    private Vector2 scrollPosition;
    private Dictionary<int, float> blendShapeValues = new Dictionary<int, float>();
    private bool showOnlyNonZero = false;

    [MenuItem("Window/aecii's Blendshape Search tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<BlendShapeSearchWindow>("aecii's Blendshape Search tool");
        window.minSize = new Vector2(350, 400);
    }

    private void OnEnable()
    {
        // Auto-select if a SkinnedMeshRenderer is already selected
        RefreshFromSelection();
    }

    private void OnSelectionChange()
    {
        RefreshFromSelection();
        Repaint();
    }

    private void RefreshFromSelection()
    {
        if (Selection.activeGameObject != null)
        {
            var smr = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr != targetRenderer)
            {
                SetTarget(smr);
            }
        }
    }

    private void SetTarget(SkinnedMeshRenderer smr)
    {
        targetRenderer = smr;
        blendShapeValues.Clear();

        if (smr != null && smr.sharedMesh != null)
        {
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                blendShapeValues[i] = smr.GetBlendShapeWeight(i);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        // --- Target selector ---
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        var newTarget = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
            "Skinned Mesh Renderer", targetRenderer, typeof(SkinnedMeshRenderer), true);

        if (newTarget != targetRenderer)
            SetTarget(newTarget);

        if (targetRenderer == null || targetRenderer.sharedMesh == null)
        {
            EditorGUILayout.HelpBox(
                "Select a GameObject with a Skinned Mesh Renderer, or assign one above.",
                MessageType.Info);
            return;
        }

        var mesh = targetRenderer.sharedMesh;
        int totalCount = mesh.blendShapeCount;

        EditorGUILayout.Space(4);

        // --- Search bar ---
        EditorGUILayout.LabelField("Search", EditorStyles.boldLabel);
        GUI.SetNextControlName("SearchField");
        searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);

        // --- Filter options row ---
        EditorGUILayout.BeginHorizontal();
        showOnlyNonZero = EditorGUILayout.ToggleLeft("Show non-zero only", showOnlyNonZero, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear Search", GUILayout.Width(100)))
        {
            searchQuery = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // --- Build filtered list ---
        var filtered = new List<int>();
        string lowerQuery = searchQuery.ToLower();
        for (int i = 0; i < totalCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i);
            float weight = targetRenderer.GetBlendShapeWeight(i);
            bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                                 shapeName.ToLower().Contains(lowerQuery);
            bool matchesFilter = !showOnlyNonZero || weight != 0f;
            if (matchesSearch && matchesFilter)
                filtered.Add(i);
        }

        // --- Result count ---
        EditorGUILayout.LabelField(
            $"Showing {filtered.Count} of {totalCount} blendshapes",
            EditorStyles.miniLabel);

        EditorGUILayout.Space(2);

        // --- Scrollable blendshape list ---
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        bool anyChanged = false;

        foreach (int i in filtered)
        {
            string shapeName = mesh.GetBlendShapeName(i);
            float currentWeight = targetRenderer.GetBlendShapeWeight(i);

            EditorGUILayout.BeginHorizontal();

            // Highlight non-zero values
            if (currentWeight != 0f)
            {
                var originalColor = GUI.color;
                GUI.color = new Color(0.6f, 1f, 0.6f); // light green tint
                EditorGUILayout.LabelField(shapeName, GUILayout.Width(180));
                GUI.color = originalColor;
            }
            else
            {
                EditorGUILayout.LabelField(shapeName, GUILayout.Width(180));
            }

            float newWeight = EditorGUILayout.Slider(currentWeight, 0f, 100f);

            if (!Mathf.Approximately(newWeight, currentWeight))
            {
                Undo.RecordObject(targetRenderer, $"Set BlendShape {shapeName}");
                targetRenderer.SetBlendShapeWeight(i, newWeight);
                blendShapeValues[i] = newWeight;
                anyChanged = true;
                EditorUtility.SetDirty(targetRenderer);
            }

            // Reset button for non-zero shapes
            if (currentWeight != 0f)
            {
                if (GUILayout.Button("0", GUILayout.Width(24)))
                {
                    Undo.RecordObject(targetRenderer, $"Reset BlendShape {shapeName}");
                    targetRenderer.SetBlendShapeWeight(i, 0f);
                    blendShapeValues[i] = 0f;
                    anyChanged = true;
                    EditorUtility.SetDirty(targetRenderer);
                }
            }
            else
            {
                GUILayout.Space(28); // keep layout consistent
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        // --- Bottom action buttons ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset ALL to 0"))
        {
            if (EditorUtility.DisplayDialog("Reset All BlendShapes",
                "Set all blendshape weights to 0?", "Yes", "Cancel"))
            {
                Undo.RecordObject(targetRenderer, "Reset All BlendShapes");
                for (int i = 0; i < totalCount; i++)
                {
                    targetRenderer.SetBlendShapeWeight(i, 0f);
                    blendShapeValues[i] = 0f;
                }
                EditorUtility.SetDirty(targetRenderer);
            }
        }
        EditorGUILayout.EndHorizontal();

        // Force repaint while values are changing so sliders stay live
        if (anyChanged) Repaint();
    }
}
