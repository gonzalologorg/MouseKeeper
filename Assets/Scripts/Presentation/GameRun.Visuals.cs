using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            SetColor(block, color);
            return block;
        }

        void Visual(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.transform.SetParent(parent);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            SetColor(visual, color);
            Destroy(visual.GetComponent<Collider>());
        }

        void SetColor(GameObject target, Color color)
        {
            Material material = new Material(PrimitiveMaterial.Shared);
            material.color = color;
            target.GetComponent<Renderer>().sharedMaterial = material;
            if (currentRoom != null) levelAssets.Add(material);
            else generatedAssets.Add(material);
        }

        const float InactiveRoomBrightness = 0.2f;

        void RefreshRoomLighting()
        {
            var properties = new MaterialPropertyBlock();
            foreach (Room room in rooms)
            {
                if (room.root == null) continue;
                float brightness = room == currentRoom ? 1f : InactiveRoomBrightness;
                floorGore?.SetRoomBrightness(room.floor, brightness);
                for (int direction = 0; direction < 4; direction++)
                {
                    Room adjacent = null;
                    foreach (Room candidate in rooms)
                        if (candidate.position == room.position + Directions[direction]) { adjacent = candidate; break; }
                    bool visible = adjacent == null || room == currentRoom ||
                        (adjacent != currentRoom && direction < 2);
                    foreach (Renderer wallRenderer in room.walls[direction].GetComponentsInChildren<Renderer>(true))
                        if (room.wallRendererStates.TryGetValue(wallRenderer, out bool original))
                            wallRenderer.enabled = visible && original;
                }
                foreach (Renderer renderer in room.root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        if (material == null) continue;
                        renderer.GetPropertyBlock(properties, i);
                        if (material.HasProperty("_BaseColor"))
                        {
                            Color color = material.GetColor("_BaseColor");
                            properties.SetColor("_BaseColor", new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a));
                        }
                        if (material.HasProperty("_Color"))
                        {
                            Color color = material.GetColor("_Color");
                            properties.SetColor("_Color", new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a));
                        }
                        if (material.HasProperty("_EmissionColor"))
                            properties.SetColor("_EmissionColor", material.GetColor("_EmissionColor") * brightness);
                        renderer.SetPropertyBlock(properties, i);
                    }
                }
                foreach (Light light in room.root.GetComponentsInChildren<Light>(true))
                    light.enabled = room == currentRoom;
            }
        }

        void OnDestroy()
        {
            floorGore?.Dispose();
            foreach (Object asset in generatedAssets)
                if (asset != null) Destroy(asset);
            foreach (Object asset in levelAssets)
                if (asset != null) Destroy(asset);
        }

    }
}
