using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MouseKeeper
{
    public partial class GameRun
    {
        FloorGore floorGore;

        public void ShowBlood(Vector3 impact, Vector3 direction)
        {
            if (!Application.isPlaying) return;
            Room room = currentRoom;
            if (!ContainsFloorPoint(room, impact))
            {
                room = null;
                foreach (Room candidate in rooms)
                {
                    if (!ContainsFloorPoint(candidate, impact)) continue;
                    room = candidate;
                    break;
                }
            }

            floorGore ??= new FloorGore();
            Quaternion spray = floorGore.SpreadRotation(direction);
            float size = floorGore.SampleSize();
            floorGore.Emit(impact, spray, size, room?.root.transform ?? transform);
            if (room != null && room.floor != null)
                StartCoroutine(PaintBloodAfterSpray(room.floor, impact, spray * Vector3.forward, size));
        }

        static readonly WaitForSeconds BloodSettlingTime = new WaitForSeconds(0.2f);

        IEnumerator PaintBloodAfterSpray(Collider floor, Vector3 impact, Vector3 direction, float size)
        {
            yield return BloodSettlingTime;
            if (floor == null || !floor.gameObject.activeInHierarchy) yield break;
            float brightness = currentRoom?.floor == floor ? 1f : InactiveRoomBrightness;
            floorGore.Stamp(floor, impact, direction, size, brightness);
        }

        static bool ContainsFloorPoint(Room room, Vector3 point)
        {
            if (room?.floor == null) return false;
            Vector3 local = room.floor.transform.InverseTransformPoint(point);
            return Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.z) <= 0.5f;
        }

        internal static Vector3 BloodImpact(Bounds bounds, Vector3 source)
        {
            source.y = bounds.center.y;
            return bounds.ClosestPoint(source);
        }
    }

    sealed class FloorGore
    {
        const int Resolution = 1024;
        const int RetainedSurfaces = 4;

        static readonly int RoomBrightness = Shader.PropertyToID("_RoomBrightness");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        static readonly int StampColor = Shader.PropertyToID("_StampColor");
        static readonly int StampNormal = Shader.PropertyToID("_StampNormal");
        static readonly int Stamps = Shader.PropertyToID("_Stamps");
        static readonly int TrailDirection = Shader.PropertyToID("_TrailDirection");

        // Lateral offset, distance downrange, width and length in world units.
        static readonly Vector4[] Trail =
        {
            new Vector4(0f, 0.4f, 2.0f, 2.3f),
            new Vector4(-0.45f, 1.25f, 1.7f, 2.1f),
            new Vector4(0.5f, 1.8f, 1.8f, 2.1f),
            new Vector4(-0.18f, 2.7f, 1.3f, 1.8f),
            new Vector4(0.7f, 3.5f, 0.9f, 1.4f)
        };

        readonly System.Random spread = new System.Random();
        readonly Vector4[] stamps = new Vector4[Trail.Length];
        static readonly int NormalPass = Shader.PropertyToID("_NormalPass");

        sealed class Surface
        {
            public RenderTexture color;
            public RenderTexture normal;
            public Material material;
            public Mesh mesh;
            public GameObject overlay;
        }

        readonly Dictionary<Collider, Surface> surfaces = new Dictionary<Collider, Surface>();
        readonly Stack<Surface> pool = new Stack<Surface>();
        readonly List<Material> stains = new List<Material>();
        readonly GameObject bloodPrefab;
        readonly Material painter;
        readonly Material roomMaterial;

        public FloorGore()
        {
            bloodPrefab = Resources.Load<GameObject>("FX/bloodfx");
            roomMaterial = Resources.Load<Material>("FX/RoomBlood");
            for (int i = 1; i <= 6; i++)
            {
                Material stain = Resources.Load<Material>($"FX/stains/blood_{i}");
                if (stain != null && stain.GetTexture(BaseMap) != null && stain.GetTexture(BumpMap) != null)
                    stains.Add(stain);
            }

            Shader shader = Resources.Load<Shader>("FX/BloodStamp");
            if (shader != null) painter = new Material(shader);
        }

        public Quaternion SpreadRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            Quaternion aim = Quaternion.LookRotation(direction);
            return aim * Quaternion.Euler(SpreadAngle(12f), SpreadAngle(24f), SpreadAngle(35f));
        }

        float SpreadAngle(float limit) => ((float)spread.NextDouble() * 2f - 1f) * limit;

        public float SampleSize() => Mathf.Lerp(0.75f, 1.25f, (float)spread.NextDouble());

        public void Emit(Vector3 impact, Quaternion rotation, float size, Transform parent)
        {
            if (bloodPrefab == null) return;
            GameObject effect = Object.Instantiate(bloodPrefab, impact, rotation, parent);
            effect.transform.localScale = bloodPrefab.transform.localScale * size;
            foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        public void SetRoomBrightness(Collider floor, float brightness)
        {
            if (floor != null && surfaces.TryGetValue(floor, out Surface surface))
                surface.material.SetFloat(RoomBrightness, brightness);
        }

        public void Stamp(Collider floor, Vector3 impact, Vector3 direction, float scale, float brightness)
        {
            if (painter == null || roomMaterial == null || stains.Count == 0) return;
            Vector3 size = floor.transform.lossyScale;
            float width = Mathf.Abs(size.x);
            float depth = Mathf.Abs(size.z);
            float span = Mathf.Max(width, depth);
            if (width <= 0f || depth <= 0f) return;
            if (!surfaces.TryGetValue(floor, out Surface surface))
            {
                surface = Rent(floor.transform, width / span, depth / span);
                surface.material.SetFloat(RoomBrightness, brightness);
                surfaces.Add(floor, surface);
            }

            Vector3 local = floor.transform.InverseTransformPoint(impact);
            Vector2 center = new Vector2(0.5f + local.x * width / span, 0.5f + local.z * depth / span);
            int index = (int)(unchecked((uint)impact.GetHashCode()) % (uint)stains.Count);
            Material stain = stains[index];
            Texture color = stain.GetTexture(BaseMap);
            Vector3 localDirection = floor.transform.InverseTransformDirection(direction);
            Vector2 forward = new Vector2(localDirection.x, localDirection.z);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
            Vector2 right = new Vector2(forward.y, -forward.x);
            float textureSpan = Mathf.Max(color.width, color.height);
            for (int i = 0; i < Trail.Length; i++)
            {
                Vector4 stamp = Trail[i];
                Vector2 offset = (right * stamp.x + forward * stamp.y) * (scale / span);
                stamps[i] = new Vector4(center.x + offset.x, center.y + offset.y,
                    stamp.z * color.width / textureSpan * scale / span, stamp.w * color.height / textureSpan * scale / span);
            }
            painter.SetVectorArray(Stamps, stamps);
            painter.SetVector(TrailDirection, new Vector4(right.x, right.y, forward.x, forward.y));
            painter.SetTexture(StampColor, color);
            painter.SetTexture(StampNormal, stain.GetTexture(BumpMap));
            Paint(surface.color, false);
            Paint(surface.normal, true);
        }

        void Paint(RenderTexture target, bool normal)
        {
            painter.SetFloat(NormalPass, normal ? 1f : 0f);
            RenderTexture scratch = RenderTexture.GetTemporary(target.descriptor);
            RenderTexture previous = RenderTexture.active;
            bool previousSrgbWrite = GL.sRGBWrite;
            try
            {
                GL.sRGBWrite = target.sRGB && QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(target, scratch, painter);
                Graphics.CopyTexture(scratch, target);
            }
            finally
            {
                GL.sRGBWrite = previousSrgbWrite;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(scratch);
            }
        }

        Surface Rent(Transform floor, float uSpan, float vSpan)
        {
            Surface surface = pool.Count == 0
                ? new Surface { color = CreateTarget("Blood coverage", false),
                    normal = CreateTarget("Blood normal", true) }
                : pool.Pop();

            surface.material = new Material(roomMaterial) { name = "Room blood (Specular)" };
            surface.material.SetTexture(BaseMap, surface.color);
            surface.material.SetTexture(BumpMap, surface.normal);

            float uMin = (1f - uSpan) * 0.5f;
            float vMin = (1f - vSpan) * 0.5f;
            surface.mesh = new Mesh
            {
                name = "Room blood overlay",
                vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) },
                uv = new[] { new Vector2(uMin, vMin), new Vector2(uMin, 1f - vMin),
                    new Vector2(1f - uMin, 1f - vMin), new Vector2(1f - uMin, vMin) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            surface.mesh.RecalculateNormals();
            surface.mesh.RecalculateTangents();
            surface.overlay = new GameObject("Blood stains");
            surface.overlay.transform.SetParent(floor, false);
            surface.overlay.transform.localPosition = new Vector3(0f, 0.5f + 0.004f / Mathf.Abs(floor.lossyScale.y), 0f);
            surface.overlay.AddComponent<MeshFilter>().sharedMesh = surface.mesh;
            MeshRenderer renderer = surface.overlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = surface.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return surface;
        }

        static RenderTexture CreateTarget(string name, bool normal)
        {
            var target = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            target.Create();
            Clear(target, normal
                ? new Color(0.5f, 0.5f, 1f, 1f) : Color.clear);
            return target;
        }

        static void Clear(RenderTexture target, Color color)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(false, true, color);
            RenderTexture.active = previous;
        }

        public void ReleaseFloor()
        {
            foreach (Surface surface in surfaces.Values)
            {
                if (surface.overlay != null)
                {
                    surface.overlay.SetActive(false);
                    Object.Destroy(surface.overlay);
                }
                Object.Destroy(surface.material);
                Object.Destroy(surface.mesh);
                surface.overlay = null;
                surface.material = null;
                surface.mesh = null;
                if (pool.Count < RetainedSurfaces)
                {
                    Clear(surface.color, Color.clear);
                    Clear(surface.normal, new Color(0.5f, 0.5f, 1f, 1f));
                    pool.Push(surface);
                }
                else ReleaseTargets(surface);
            }
            surfaces.Clear();
        }

        public void Dispose()
        {
            ReleaseFloor();
            while (pool.Count > 0) ReleaseTargets(pool.Pop());
            if (painter != null) Object.Destroy(painter);
        }

        static void ReleaseTargets(Surface surface)
        {
            surface.color.Release();
            surface.normal.Release();
            Object.Destroy(surface.color);
            Object.Destroy(surface.normal);
        }
    }
}
