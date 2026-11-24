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
            public bool corridor;
            public int corridorMask;
            public readonly Image[] arms = new Image[4];
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

        public void SetCorridor(Vector2Int position, Vector2 center, Vector2 size, int mask, Color color)
        {
            SetRoom(position, center, size, color);
            RoomDisplay display = rooms[position];
            display.corridor = true;
            display.corridorMask = mask;
            for (int i = 0; i < 4; i++)
            {
                if (display.arms[i] == null)
                {
                    var part = new GameObject("Corridor arm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    part.layer = gameObject.layer;
                    part.transform.SetParent(transform, false);
                    display.arms[i] = part.GetComponent<Image>();
                    display.arms[i].raycastTarget = false;
                }
                display.arms[i].color = color;
                display.arms[i].gameObject.SetActive((mask & (1 << i)) != 0);
            }
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
                rect.sizeDelta = room.corridor ? Vector2.one * (GameRun.CorridorWidth * scale) : room.size * (scale * 0.9f);
                rect.anchoredPosition = (room.center - center) * scale;
                if (!room.corridor) continue;
                for (int i = 0; i < 4; i++)
                {
                    if ((room.corridorMask & (1 << i)) == 0) continue;
                    bool horizontal = i % 2 == 1;
                    float sign = i < 2 ? 1f : -1f;
                    float length = ((horizontal ? room.size.x : room.size.y) - GameRun.CorridorWidth) * 0.5f;
                    RectTransform arm = room.arms[i].rectTransform;
                    arm.anchorMin = arm.anchorMax = arm.pivot = new Vector2(0.5f, 0.5f);
                    arm.sizeDelta = (horizontal ? new Vector2(length, GameRun.CorridorWidth) : new Vector2(GameRun.CorridorWidth, length)) * scale;
                    float half = GameRun.CorridorWidth * 0.5f;
                    Vector2 offset = horizontal ? new Vector2(sign * (half + length * 0.5f), 0f) : new Vector2(0f, sign * (half + length * 0.5f));
                    arm.anchoredPosition = (room.center + offset - center) * scale;
                }
            }
        }

        public void ClearRooms()
        {
            foreach (RoomDisplay room in rooms.Values)
            {
                room.image.gameObject.SetActive(false);
                Destroy(room.image.gameObject);
                foreach (Image arm in room.arms)
                    if (arm != null) { arm.gameObject.SetActive(false); Destroy(arm.gameObject); }
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
            {
                bool hit = RectTransformUtility.RectangleContainsScreenPoint(pair.Value.image.rectTransform, point, camera);
                if (!hit && pair.Value.corridor)
                    foreach (Image arm in pair.Value.arms)
                        if (arm != null && arm.gameObject.activeSelf && RectangleContains(arm.rectTransform, point, camera)) { hit = true; break; }
                if (hit)
                {
                    position = pair.Key;
                    return true;
                }
            }
            position = default;
            return false;
        }

        static bool RectangleContains(RectTransform rect, Vector2 point, Camera camera) =>
            RectTransformUtility.RectangleContainsScreenPoint(rect, point, camera);

        void OnRectTransformDimensionsChange()
        {
            LayoutRooms();
        }
    }
}
