using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Hiển thị và cập nhật bản đồ thu nhỏ (Minimap) trong trận đấu (SYS-04-COR).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapView : MonoBehaviour
    {
        [SerializeField] RectTransform container;

        readonly Dictionary<int, Image> rooms = new();

        public void Build(RectTransform frame, DungeonLayout layout)
        {
            rooms.Clear();
            if (frame == null) return;
            if (container != null) Destroy(container.gameObject);

            if (layout == null || layout.Rooms.Count == 0) return;

            var go = new GameObject("MapContent", typeof(RectTransform));
            go.layer = frame.gameObject.layer;
            container = (RectTransform)go.transform;
            container.SetParent(frame, false);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = frame.sizeDelta - new Vector2(24f, 24f);

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var r in layout.Rooms)
            {
                if (r.Cell.x < minX) minX = r.Cell.x;
                if (r.Cell.x > maxX) maxX = r.Cell.x;
                if (r.Cell.y < minY) minY = r.Cell.y;
                if (r.Cell.y > maxY) maxY = r.Cell.y;
            }
            float spanX = Mathf.Max(1, maxX - minX + 1);
            float spanY = Mathf.Max(1, maxY - minY + 1);
            Vector2 area = container.sizeDelta;
            float step = Mathf.Min(area.x / spanX, area.y / spanY);
            step = Mathf.Clamp(step, 12f, 26f);
            float roomW = step * 0.72f;
            float roomH = step * 0.62f;
            Vector2 centerCell = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            // Vẽ đường nối giữa các phòng
            foreach (var (a, b, _) in layout.Links())
            {
                Vector2 pa = ((Vector2)a.Cell - centerCell) * step;
                Vector2 pb = ((Vector2)b.Cell - centerCell) * step;
                var lgo = new GameObject($"Link_{a.Id}_{b.Id}", typeof(RectTransform), typeof(Image));
                lgo.layer = go.layer;
                var lrt = (RectTransform)lgo.transform;
                lrt.SetParent(container, false);
                lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.anchoredPosition = (pa + pb) * 0.5f;
                bool horiz = a.Cell.y == b.Cell.y;
                lrt.sizeDelta = horiz ? new Vector2(step, 3f) : new Vector2(3f, step);
                var limg = lgo.GetComponent<Image>();
                limg.color = new Color32(0x6e, 0x66, 0x7c, 0xc8);
                limg.raycastTarget = false;
            }

            // Vẽ từng phòng
            foreach (var r in layout.Rooms)
            {
                Vector2 pos = ((Vector2)r.Cell - centerCell) * step;
                var rgo = new GameObject($"Room_{r.Id}_{r.Type}", typeof(RectTransform), typeof(Image));
                rgo.layer = go.layer;
                var rrt = (RectTransform)rgo.transform;
                rrt.SetParent(container, false);
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
                rrt.anchoredPosition = pos;
                rrt.sizeDelta = new Vector2(roomW, roomH);
                var rimg = rgo.GetComponent<Image>();
                rimg.raycastTarget = false;
                rooms[r.Id] = rimg;
            }

            RefreshColors(LevelDirector.Current);
        }

        public void RefreshColors(LevelDirector dir)
        {
            if (dir == null || dir.Builder == null) return;
            foreach (var rc in dir.Builder.Rooms)
            {
                if (rc == null || !rooms.TryGetValue(rc.RoomId, out var img) || img == null) continue;
                bool isCurrent = dir.CurrentRoom == rc;
                if (isCurrent)
                {
                    img.color = new Color32(0xff, 0xd2, 0x7a, 0xff); // Vàng sáng: phòng hiện tại
                    img.rectTransform.localScale = new Vector3(1.15f, 1.15f, 1f);
                }
                else
                {
                    img.rectTransform.localScale = Vector3.one;
                    img.color = rc.Type switch
                    {
                        RoomType.Boss => new Color32(0xd6, 0x45, 0x50, 0xff),     // Đỏ: Boss
                        RoomType.Treasure => new Color32(0xe6, 0xa8, 0x32, 0xff), // Cam vàng: Kho báu
                        RoomType.Shop => new Color32(0x4e, 0xb8, 0x82, 0xff),     // Xanh ngọc: Shop
                        RoomType.Start => new Color32(0x7a, 0x9e, 0xc9, 0xff),    // Xanh dương: Start
                        _ => rc.IsCleared || rc.Visited
                            ? new Color32(0x9a, 0x94, 0xa8, 0xff)                 // Xám sáng: Đã dọn
                            : new Color32(0x4a, 0x42, 0x58, 0xcc)                 // Tím tối: Chưa vào
                    };
                }
            }
        }
    }
}
