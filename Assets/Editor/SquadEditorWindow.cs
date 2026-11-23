using MouseKeeper;
using UnityEditor;
using UnityEngine;

public sealed class SquadEditorWindow : EditorWindow
{
    AgentDefinition selected;
    Editor inspector;
    Vector2 scroll;

    [MenuItem("Window/MouseKeeper/Editor de escuadrón")]
    static void Open()
    {
        GetWindow<SquadEditorWindow>("Escuadrón");
    }

    void OnEnable()
    {
        OnSelectionChange();
    }

    void OnSelectionChange()
    {
        if (Selection.activeObject is AgentDefinition definition)
            Select(definition);
    }

    void OnDisable()
    {
        if (inspector != null) DestroyImmediate(inspector);
    }

    void Select(AgentDefinition definition)
    {
        if ((definition == null && inspector == null) ||
            (definition != null && selected == definition && inspector != null)) return;
        if (inspector != null) DestroyImmediate(inspector);
        selected = definition;
        inspector = selected != null ? Editor.CreateEditor(selected) : null;
        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Personajes del escuadrón", EditorStyles.boldLabel);
        if (GUILayout.Button("Crear agente"))
        {
            string path = EditorUtility.SaveFilePanelInProject("Crear agente", "Nuevo Agente", "asset",
                "Selecciona dónde guardar el personaje.");
            if (!string.IsNullOrEmpty(path))
            {
                var definition = CreateInstance<AgentDefinition>();
                AssetDatabase.CreateAsset(definition, path);
                Selection.activeObject = definition;
                Select(definition);
            }
        }
        Select((AgentDefinition)EditorGUILayout.ObjectField("Agente", selected, typeof(AgentDefinition), false));
        if (inspector == null) return;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        inspector.OnInspectorGUI();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.HelpBox("Añade los agentes al catálogo Assets/Resources/ExpeditionCatalog para usarlos en planificación.", MessageType.Info);
    }
}
