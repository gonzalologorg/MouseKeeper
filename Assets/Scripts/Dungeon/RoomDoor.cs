using UnityEngine;

namespace MouseKeeper
{
    public sealed class RoomDoor : MonoBehaviour
    {
        const float HalfOpening = 1.43f;
        const float AnimationSpeed = 180f;
        Transform left;
        Transform right;
        BoxCollider barrier;
        GameObject hint;
        float angle;
        bool open;
        public bool IsOpen => open;

        public void Initialize(GameObject archPrefab, GameObject doorPrefab)
        {
            Instantiate(archPrefab, transform);
            AddFrameCollider(new Vector3(-1.69f, 1.95f, 0f), new Vector3(0.52f, 3.9f, 0.52f));
            AddFrameCollider(new Vector3(1.69f, 1.95f, 0f), new Vector3(0.52f, 3.9f, 0.52f));
            AddFrameCollider(new Vector3(0f, 3.45f, 0f), new Vector3(2.86f, 0.9f, 0.52f));
            left = CreateLeaf(doorPrefab, -HalfOpening, 1.1f);
            right = CreateLeaf(doorPrefab, HalfOpening, -1.1f);
            barrier = gameObject.AddComponent<BoxCollider>();
            barrier.center = new Vector3(0f, 1.365f, 0f);
            barrier.size = new Vector3(HalfOpening * 2f, 2.73f, 0.25f);
            BoxCollider clickTarget = gameObject.AddComponent<BoxCollider>();
            clickTarget.isTrigger = true;
            clickTarget.center = new Vector3(0f, 1.4f, 0f);
            clickTarget.size = new Vector3(2.86f, 2.8f, 0.7f);
            hint = new GameObject("Available Door Glow");
            hint.transform.SetParent(transform, false);
            hint.transform.localPosition = Vector3.up;
            hint.AddComponent<PickupGlow>().Initialize(false);
            hint.GetComponentInChildren<Light>().color = Color.white;
            hint.SetActive(false);
            open = false;
            angle = 0f;
            ApplyAngle();
            barrier.enabled = true;
        }

        Transform CreateLeaf(GameObject prefab, float x, float scale)
        {
            var pivot = new GameObject("Door Hinge").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(x, 0f, 0f);
            GameObject leaf = Instantiate(prefab, pivot);
            leaf.transform.localScale = new Vector3(scale, 1f, 1f);
            return pivot;
        }

        void AddFrameCollider(Vector3 center, Vector3 size)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        public void SetOpen(bool value)
        {
            open = value;
            if (!open) barrier.enabled = true;
        }

        public void SetHint(bool value)
        {
            if (hint != null) hint.SetActive(value);
        }

        void Update()
        {
            angle = Mathf.MoveTowards(angle, open ? 90f : 0f, AnimationSpeed * Time.deltaTime);
            ApplyAngle();
            if (open && angle >= 90f) barrier.enabled = false;
        }

        void ApplyAngle()
        {
            left.localRotation = Quaternion.Euler(0f, angle, 0f);
            right.localRotation = Quaternion.Euler(0f, -angle, 0f);
        }
    }
}
