using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.UI
{
    /// <summary>
    /// Hiển thị sprite pixel art theo PHẦN CÓ HÌNH (bỏ viền trong suốt) thay vì cả ô sprite — để các sheet khác cỡ ô
    /// (FantasyKnights 48px, thân ~30px; Dungeon_Character 16px kín ô) trông cùng cỡ trong một khung.
    /// Vùng có hình lấy từ mesh Tight (sprite.vertices), không cần texture Read/Write. Hệ số phóng là số nguyên
    /// (pixel đều, không méo) khi có thể.
    /// </summary>
    public static class SpriteContentFit
    {
        /// <summary>Hình chữ nhật phần có hình, tính bằng pixel, gốc = góc dưới-trái của sprite.rect.</summary>
        public static Rect ContentPixels(Sprite s)
        {
            var full = new Rect(0f, 0f, s.rect.width, s.rect.height);
            var v = s.vertices;
            if (v == null || v.Length == 0) return full;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in v)
            {
                if (p.x < x0) x0 = p.x; if (p.y < y0) y0 = p.y;
                if (p.x > x1) x1 = p.x; if (p.y > y1) y1 = p.y;
            }
            float ppu = s.pixelsPerUnit;
            var r = Rect.MinMaxRect(x0 * ppu + s.pivot.x, y0 * ppu + s.pivot.y, x1 * ppu + s.pivot.x, y1 * ppu + s.pivot.y);
            return r.width < 1f || r.height < 1f ? full : r;
        }

        /// <summary>Hệ số phóng để phần có hình vừa khung (nguyên nếu ≥ 1).</summary>
        public static float Scale(Sprite s, Vector2 box)
        {
            var c = ContentPixels(s);
            float k = Mathf.Min(box.x / c.width, box.y / c.height);
            return k >= 1f ? Mathf.Floor(k) : k;
        }

        /// <summary>
        /// Gán sprite cho Image (neo giữa) sao cho phần có hình vừa khung <paramref name="box"/> và nằm giữa <paramref name="center"/>.
        /// </summary>
        public static void Fit(Image image, Sprite s, Vector2 box, Vector2 center)
        {
            if (image == null) return;
            image.sprite = s;
            image.enabled = s != null;
            if (s == null) return;
            image.preserveAspect = false;
            float k = Scale(s, box);
            var c = ContentPixels(s);
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(s.rect.width, s.rect.height) * k;
            var offset = (c.center - new Vector2(s.rect.width, s.rect.height) * 0.5f) * k; // lệch của phần có hình so với tâm ô
            rt.anchoredPosition = center - offset;
        }
    }
}
