using EquationShift;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EquationGate)), CanEditMultipleObjects]
public sealed class EquationGateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (!EditorGUI.EndChangeCheck() || Application.isPlaying) return;
        foreach (Object targetObject in targets)
        {
            var gate = (EquationGate)targetObject;
            Undo.RecordObject(gate.GetComponent<SpriteRenderer>(), "Alterar estado do portao");
            foreach (Collider2D shape in gate.GetComponentsInChildren<Collider2D>(true))
                Undo.RecordObject(shape, "Alterar colisao do portao");
            gate.ApplyConfiguredState();
            EditorUtility.SetDirty(gate.GetComponent<SpriteRenderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(gate.GetComponent<SpriteRenderer>());
            foreach (Collider2D shape in gate.GetComponentsInChildren<Collider2D>(true))
            {
                EditorUtility.SetDirty(shape);
                PrefabUtility.RecordPrefabInstancePropertyModifications(shape);
            }
        }
    }
}
