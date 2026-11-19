using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MouseKeeper
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class Minimap : MonoBehaviour
    {
        readonly Dictionary<Vector2Int, Image> rooms = new Dictionary<Vector2Int, Image>();
        RectTransform panel;

        public void SetRoom(Vector2Int position, Color color)
        {
            if (!rooms.TryGetValue(position, out Image image))
            {
                var room = new GameObject("Room " + position, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                room.layer = gameObject.layer;
                room.transform.SetParent(transform, false);
                image = room.GetComponent<Image>();
                image.raycastTarget = false;
                rooms.Add(position, image);
            }
            image.color = color;
        }

        public void LayoutRooms()
        {
            if (rooms.Count == 0) return;
            if (panel == null) panel = GetComponent<RectTransform>();
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (Vector2Int position in rooms.Keys)
            {
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }
            float width = Mathf.Max(0f, panel.rect.width - 16f);
            float height = Mathf.Max(0f, panel.rect.height - 16f);
            float step = Mathf.Min(32f, width / (maxX - minX + 1), height / (maxY - minY + 1));
            Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            foreach (KeyValuePair<Vector2Int, Image> room in rooms)
            {
                RectTransform rect = room.Value.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.one * step * 0.85f;
                rect.anchoredPosition = ((Vector2)room.Key - center) * step;
            }
        }

        public void ClearRooms()
        {
            foreach (Image image in rooms.Values)
            {
                image.gameObject.SetActive(false);
                Destroy(image.gameObject);
            }
            rooms.Clear();
        }

        void OnRectTransformDimensionsChange()
        {
            LayoutRooms();
        }
    }
}
