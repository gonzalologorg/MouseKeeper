using UnityEngine;

namespace MouseKeeper
{
    public sealed class WeaponVisual : MonoBehaviour
    {
        WeaponInstance selected;
        Transform pivot;
        GameObject model;
        float swingTime;
        float swingDuration;

        public static GameObject CreateModel(WeaponDefinition definition, Transform parent)
        {
            GameObject visual;
            if (definition.modelMesh != null || definition.modelPrefab != null)
            {
                visual = new GameObject("Weapon Model");
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = definition.modelPosition;
                visual.transform.localRotation = Quaternion.Euler(definition.modelRotation);
                visual.transform.localScale = definition.modelScale;
                if (definition.modelMesh != null)
                {
                    visual.AddComponent<MeshFilter>().sharedMesh = definition.modelMesh;
                    visual.AddComponent<MeshRenderer>().sharedMaterial = definition.modelMaterial != null
                        ? definition.modelMaterial : PrimitiveMaterial.Shared;
                }
                else
                    Instantiate(definition.modelPrefab, visual.transform);
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
                foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
                {
                    body.detectCollisions = false;
                    body.isKinematic = true;
                }
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(parent, false);
                visual.transform.localScale = new Vector3(0.2f, 0.2f, 0.65f);
                visual.GetComponent<Collider>().enabled = false;
                visual.GetComponent<Renderer>().sharedMaterial = PrimitiveMaterial.Shared;
                var color = new MaterialPropertyBlock();
                color.SetColor("_BaseColor", Color.yellow);
                visual.GetComponent<Renderer>().SetPropertyBlock(color);
            }
            visual.name = "Weapon Model";
            return visual;
        }

        public void SetWeapon(WeaponInstance weapon)
        {
            if (weapon == selected) return;
            selected = weapon;
            swingTime = 0f;
            if (pivot == null)
            {
                var mount = new GameObject("Weapon Mount");
                pivot = mount.transform;
                pivot.SetParent(transform, false);
                pivot.localPosition = new Vector3(0.45f, 0.85f, 0.4f);
            }
            pivot.localRotation = Quaternion.identity;
            if (model != null)
            {
                model.SetActive(false);
                if (Application.isPlaying) Destroy(model);
                else DestroyImmediate(model);
            }
            model = weapon != null ? CreateModel(weapon.Definition, pivot) : null;
        }

        public void Swing(WeaponInstance weapon)
        {
            if (weapon != selected || pivot == null) return;
            swingDuration = Mathf.Max(0.08f, weapon.PrimaryInterval);
            swingTime = swingDuration;
            pivot.localRotation = Quaternion.Euler(0f, -60f, 0f);
        }

        void Update()
        {
            if (swingTime <= 0f || pivot == null) return;
            swingTime = Mathf.Max(0f, swingTime - Time.deltaTime);
            float progress = 1f - swingTime / swingDuration;
            float angle = progress < 0.65f
                ? Mathf.Lerp(-60f, 60f, progress / 0.65f)
                : Mathf.Lerp(60f, 0f, (progress - 0.65f) / 0.35f);
            pivot.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}
