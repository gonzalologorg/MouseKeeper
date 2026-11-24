using MouseKeeper;
using UnityEditor;
using UnityEngine;

public sealed class RoomLayoutEditorWindow : EditorWindow
{
    static readonly string[] WidthOptions = { "14", "18", "22" };
    static readonly string[] DepthOptions = { "10", "14", "18" };
    static readonly float[] Widths = { 14f, 18f, 22f };
    static readonly float[] Depths = { 10f, 14f, 18f };
    int widthIndex = 1;
    int depthIndex = 1;
    RoomLayout.DoorSide doorSide = RoomLayout.DoorSide.Left;
    [MenuItem("Window/MouseKeeper/Editor de habitaciones")]
    static void Open() => GetWindow<RoomLayoutEditorWindow>("Habitaciones");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Plantillas de salas de combate", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Elige las dimensiones y el lado de la puerta. Abre el prefab para editar el interior; conserva la estructura de referencia y coloca tus objetos dentro de Contenido.", MessageType.Info);
        widthIndex = EditorGUILayout.Popup("Ancho (X)", widthIndex, WidthOptions);
        depthIndex = EditorGUILayout.Popup("Fondo (Z)", depthIndex, DepthOptions);
        doorSide = (RoomLayout.DoorSide)EditorGUILayout.EnumPopup("Lado de la puerta", doorSide);
        EditorGUILayout.LabelField("Proporción", Widths[widthIndex] + " : " + Depths[depthIndex]);
        if (GUILayout.Button("Crear prefab de habitación")) CreateRoom(Widths[widthIndex], Depths[depthIndex], doorSide);
        EditorGUILayout.HelpBox("El generador usa la plantilla cuando el tamaño y el lado de la puerta coinciden. Configura los enemigos y las recompensas en Room Layout; -1 conserva la cantidad normal de enemigos.", MessageType.None);
    }

    static void CreateRoom(float width, float depth, RoomLayout.DoorSide doorSide)
    {
        RoomArchitecture architecture = Resources.Load<RoomArchitecture>("RoomArchitecture");
        if (architecture == null || !architecture.IsValid())
        {
            EditorUtility.DisplayDialog("Arquitectura no disponible", "Configura Resources/RoomArchitecture antes de crear una habitación.", "Aceptar");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/RoomLayouts"))
            AssetDatabase.CreateFolder("Assets/Resources", "RoomLayouts");
        string path = EditorUtility.SaveFilePanelInProject("Crear habitación", "NuevaHabitacion", "prefab",
            "Guarda la plantilla en Resources/RoomLayouts para que aparezca en el generador.", "Assets/Resources/RoomLayouts");
        if (string.IsNullOrEmpty(path)) return;
        if (!path.StartsWith("Assets/Resources/RoomLayouts/", System.StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Ubicación de la plantilla", "Guarda el prefab dentro de Assets/Resources/RoomLayouts para que el generador pueda encontrarlo.", "Aceptar");
            return;
        }

        var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        try
        {
            RoomLayout layout = root.AddComponent<RoomLayout>();
            layout.minimumRoomSize = new Vector2(width, depth);
            layout.hasRoomShell = true;
            layout.doorSide = doorSide;
            layout.editorShell = CreateShell(root.transform, width, depth, doorSide, architecture);
            var contents = new GameObject("Contenido");
            contents.transform.SetParent(root.transform, false);
            var spawns = new GameObject("Apariciones");
            spawns.transform.SetParent(root.transform, false);
            layout.hideSpots = new Transform[4];
            for (int i = 0; i < layout.hideSpots.Length; i++)
            {
                var spot = new GameObject("Aparición " + (i + 1));
                spot.transform.SetParent(spawns.transform, false);
                spot.transform.localPosition = new Vector3(i % 2 == 0 ? -3f : 3f, 0f, i < 2 ? -3f : 3f);
                layout.hideSpots[i] = spot.transform;
            }
            var reward = new GameObject("Punto de recompensa");
            reward.transform.SetParent(root.transform, false);
            reward.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            layout.rewards = new[] { new RoomLayout.Reward { position = reward.transform, chance = 0f } };
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                AssetDatabase.OpenAsset(prefab);
            }
        }
        finally { DestroyImmediate(root); }
    }

    static GameObject CreateShell(Transform parent, float width, float depth, RoomLayout.DoorSide doorSide, RoomArchitecture architecture)
    {
        var shell = new GameObject("Estructura (referencia)");
        shell.transform.SetParent(parent, false);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Piso";
        floor.transform.SetParent(shell.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(width, 1f, depth);
        Material floorMaterial = Resources.Load<Material>("PrimitiveMaterial");
        if (floorMaterial != null) floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        for (int direction = 0; direction < 4; direction++)
        {
            bool horizontal = direction == 0 || direction == 2;
            float length = horizontal ? width : depth;
            float edge = (horizontal ? depth : width) * 0.5f * (direction < 2 ? 1f : -1f);
            var wall = new GameObject("Pared " + (RoomLayout.DoorSide)direction);
            wall.transform.SetParent(shell.transform, false);
            wall.transform.localPosition = horizontal ? new Vector3(0f, 0f, edge) : new Vector3(edge, 0f, 0f);
            wall.transform.localRotation = Quaternion.Euler(0f, horizontal ? 0f : 90f, 0f);
            if (direction != (int)doorSide)
                CreateWallSpan(wall.transform, -length * 0.5f, length, architecture.walls[0]);
            else
            {
                float section = (length - 3.9f) * 0.5f;
                CreateWallSpan(wall.transform, -length * 0.5f, section, architecture.walls[0]);
                CreateWallSpan(wall.transform, 1.95f, section, architecture.walls[0]);
                var doorway = new GameObject("Puerta");
                doorway.transform.SetParent(wall.transform, false);
                AddModel(architecture.arch, doorway.transform);
                GameObject left = AddModel(architecture.door, doorway.transform);
                left.transform.localPosition = new Vector3(-1.43f, 0f, 0f);
                left.transform.localScale = new Vector3(1.1f, 1f, 1f);
                GameObject right = AddModel(architecture.door, doorway.transform);
                right.transform.localPosition = new Vector3(1.43f, 0f, 0f);
                right.transform.localScale = new Vector3(-1.1f, 1f, 1f);
            }
        }
        return shell;
    }

    static void CreateWallSpan(Transform parent, float start, float length, GameObject prefab)
    {
        const float moduleWidth = 5.278f;
        int count = Mathf.Max(1, Mathf.CeilToInt(length / moduleWidth));
        float width = length / count;
        for (int i = 0; i < count; i++)
        {
            GameObject model = AddModel(prefab, parent);
            model.transform.localPosition = new Vector3(start + width * (i + 0.5f), 0f, 0f);
            model.transform.localScale = new Vector3(width / moduleWidth, 1f, 1f);
        }
    }

    static GameObject AddModel(GameObject prefab, Transform parent)
    {
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        return model;
    }
}
