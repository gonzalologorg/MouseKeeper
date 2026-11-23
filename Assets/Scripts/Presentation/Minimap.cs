using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MouseKeeper
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class Minimap : MonoBehaviour
    {
        sealed class RoomDisplay
        {
            public Image image;
            public Vector2 center;
            public Vector2 size;
        }

        readonly Dictionary<Vector2Int, RoomDisplay> rooms = new Dictionary<Vector2Int, RoomDisplay>();
        RectTransform panel;

        public void SetRoom(Vector2Int position, Vector2 center, Vector2 size, Color color)
        {
            if (!rooms.TryGetValue(position, out RoomDisplay display))
            {
                var room = new GameObject("Room " + position, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                room.layer = gameObject.layer;
                room.transform.SetParent(transform, false);
                display = new RoomDisplay { image = room.GetComponent<Image>() };
                display.image.raycastTarget = false;
                rooms.Add(position, display);
            }
            display.center = center;
            display.size = size;
            display.image.color = color;
        }

        public void LayoutRooms()
        {
            if (rooms.Count == 0) return;
            if (panel == null) panel = GetComponent<RectTransform>();
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (RoomDisplay room in rooms.Values)
            {
                min = Vector2.Min(min, room.center - room.size * 0.5f);
                max = Vector2.Max(max, room.center + room.size * 0.5f);
            }
            float width = Mathf.Max(0f, panel.rect.width - 16f);
            float height = Mathf.Max(0f, panel.rect.height - 16f);
            Vector2 extent = max - min;
            float scale = Mathf.Min(1.75f, width / extent.x, height / extent.y);
            Vector2 center = (min + max) * 0.5f;
            foreach (RoomDisplay room in rooms.Values)
            {
                RectTransform rect = room.image.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = room.size * (scale * 0.9f);
                rect.anchoredPosition = (room.center - center) * scale;
            }
        }

        public void ClearRooms()
        {
            foreach (RoomDisplay room in rooms.Values)
            {
                room.image.gameObject.SetActive(false);
                Destroy(room.image.gameObject);
            }
            rooms.Clear();
        }

        public bool ContainsScreenPoint(Vector2 point)
        {
            if (panel == null) panel = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(panel, point, camera);
        }

        public bool TryGetRoomAtScreenPoint(Vector2 point, out Vector2Int position)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            foreach (KeyValuePair<Vector2Int, RoomDisplay> pair in rooms)
                if (RectTransformUtility.RectangleContainsScreenPoint(pair.Value.image.rectTransform, point, camera))
                {
                    position = pair.Key;
                    return true;
                }
            position = default;
            return false;
        }

        void OnRectTransformDimensionsChange()
        {
            LayoutRooms();
        }
    }
}
