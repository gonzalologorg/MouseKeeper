using UnityEngine;
using UnityEngine.Rendering;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        Material floorShadowMaterial;

        void BuildFloorShadows(Room room)
        {
            if (viewCamera != null) viewCamera.cullingMask |= 1 << 2;
            if (floorShadowMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("FX/FloorShadow");
                if (shader == null) { Debug.LogError("Falta el shader de sombra del piso.", this); return; }
                floorShadowMaterial = new Material(shader);
                generatedAssets.Add(floorShadowMaterial);
            }
            const float softness = 1.6f;
            int count = room.root.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform floor = room.root.transform.GetChild(i);
                if (floor.name != "Floor" && floor.name != "Corridor floor") continue;
                if (floor.GetComponent<Collider>() == null) continue;
                Vector3 size = floor.localScale;
                GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shadow.name = "Floor edge shadow";
                shadow.layer = 2;
                shadow.transform.SetParent(room.root.transform, false);
                shadow.transform.localPosition = new Vector3(floor.localPosition.x, -0.04f, floor.localPosition.z);
                shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shadow.transform.localScale = new Vector3(size.x + softness * 2f, size.z + softness * 2f, 1f);
                Collider collider = shadow.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                MeshRenderer renderer = shadow.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = floorShadowMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var properties = new MaterialPropertyBlock();
                properties.SetVector("_FloorSize", new Vector4(size.x, size.z, 0f, 0f));
                properties.SetFloat("_Softness", softness);
                renderer.SetPropertyBlock(properties);
            }
        }
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
                bool visibleRoom = room != bossRoom || BossRevealed();
                if (room.root.activeSelf != visibleRoom) room.root.SetActive(visibleRoom);
                if (!visibleRoom) continue;
                bool sameHallway = currentRoom != null && currentRoom.kind == RoomKind.Corridor && room.kind == RoomKind.Corridor;
                float brightness = room == currentRoom || sameHallway ? 1f : InactiveRoomBrightness;
                floorGore?.SetRoomBrightness(room.floor, brightness);
                for (int direction = 0; direction < 4; direction++)
                {
                    foreach (Renderer wallRenderer in room.walls[direction].GetComponentsInChildren<Renderer>(true))
                        if (room.wallRendererStates.TryGetValue(wallRenderer, out bool original))
                            wallRenderer.enabled = original;
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
            FinishBalanceTelemetry("interrupted");
            RemoveFloorNavigation();
            floorGore?.Dispose();
            foreach (Object asset in generatedAssets)
                if (asset != null) Destroy(asset);
            foreach (Object asset in levelAssets)
                if (asset != null) Destroy(asset);
        }

    }
}
