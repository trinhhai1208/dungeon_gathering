using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.UI
{
    /// <summary>
    /// Renders a segmented pixel bar (HP, Armor, Mana, Boss, or Progress) divided into discrete cells
    /// matching the stat count (e.g. 7 HP = 7 cells, 120 Mana at 20/cell = 6 cells).
    /// </summary>
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public sealed class SegmentedBar : MonoBehaviour
    {
        [SerializeField] Sprite filledSprite;
        [SerializeField] Sprite emptySprite;
        [SerializeField] int maxSegmentsCap = 24;
        [SerializeField] float pixelsPerUnitMultiplier = 1f;
        [SerializeField] List<Image> cells = new();

        int lastCount = -1;
        int lastFilled = -1;

        public void SetValue(int current, int max, int unitsPerCell = 1)
        {
            if (max <= 0)
            {
                Apply(0, 0);
                return;
            }
            int step = Mathf.Max(1, unitsPerCell);
            int count = Mathf.Clamp(Mathf.CeilToInt((float)max / step), 1, maxSegmentsCap);
            int filled;
            if (current <= 0)
                filled = 0;
            else if (max > maxSegmentsCap * step)
                filled = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01((float)current / max) * count), 1, count);
            else
                filled = Mathf.Clamp(Mathf.CeilToInt((float)current / step), 1, count);

            Apply(filled, count);
        }

        public void SetNormalized(float fraction, int fixedCount = 10)
        {
            int count = Mathf.Clamp(fixedCount, 1, maxSegmentsCap);
            int filled = fraction <= 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(fraction) * count), 1, count);
            Apply(filled, count);
        }

        void Apply(int filled, int count)
        {
            if (count == lastCount && filled == lastFilled) return;
            lastCount = count;
            lastFilled = filled;

            EnsureCells(count);
            for (int i = 0; i < cells.Count; i++)
            {
                var img = cells[i];
                if (img == null) continue;
                bool active = i < count;
                if (img.gameObject.activeSelf != active) img.gameObject.SetActive(active);
                if (!active) continue;
                img.sprite = i < filled ? filledSprite : emptySprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            }
        }

        void EnsureCells(int needed)
        {
            while (cells.Count < needed)
            {
                var go = new GameObject($"Seg_{cells.Count}", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
                cells.Add(img);
            }
        }
    }
}
