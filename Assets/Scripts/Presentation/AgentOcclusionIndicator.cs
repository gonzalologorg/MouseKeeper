using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MouseKeeper
{
    public sealed class AgentOcclusionIndicator : MonoBehaviour
    {
        sealed class Copy
        {
            public Renderer source;
            public Renderer overlay;
        }

        readonly List<Copy> copies = new List<Copy>();

        public void Initialize()
        {
            Material material = Resources.Load<Material>("AgentOccluded");
            if (material == null)
            {
                Debug.LogError("The agent occlusion material is missing.", this);
                return;
            }
            foreach (Renderer source in GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh;
                if (source is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else
                {
                    MeshFilter filter = source.GetComponent<MeshFilter>();
                    mesh = filter != null ? filter.sharedMesh : null;
                }
                if (mesh == null) continue;

                var root = new GameObject("Occluded Agent Silhouette");
                root.transform.SetParent(source.transform, false);
                Renderer overlay;
                if (source is SkinnedMeshRenderer original)
                {
                    var skin = root.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh = original.sharedMesh;
                    skin.bones = original.bones;
                    skin.rootBone = original.rootBone;
                    skin.localBounds = original.localBounds;
                    skin.updateWhenOffscreen = true;
                    overlay = skin;
                }
                else
                {
                    root.AddComponent<MeshFilter>().sharedMesh = mesh;
                    overlay = root.AddComponent<MeshRenderer>();
                }
                var materials = new Material[mesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                overlay.sharedMaterials = materials;
                overlay.shadowCastingMode = ShadowCastingMode.Off;
                overlay.receiveShadows = false;
                overlay.enabled = source.enabled;
                copies.Add(new Copy { source = source, overlay = overlay });
            }
        }

        void LateUpdate()
        {
            foreach (Copy copy in copies)
            {
                if (copy.source == null || copy.overlay == null) continue;
                copy.overlay.enabled = copy.source.enabled;
                if (copy.source is SkinnedMeshRenderer source && copy.overlay is SkinnedMeshRenderer overlay &&
                    source.sharedMesh != null && source.sharedMesh == overlay.sharedMesh)
                    for (int i = 0; i < source.sharedMesh.blendShapeCount; i++)
                        overlay.SetBlendShapeWeight(i, source.GetBlendShapeWeight(i));
            }
        }
    }
}
