using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Scale With Screen Size + Expand, làm tròn để artScale × scaleFactor luôn nguyên (UI_SYSTEM §3.2).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class PixelSnapCanvasScaler : CanvasScaler
    {
        [Tooltip("Số đơn vị UI cho 1 pixel gốc của sprite (bản thiết kế vẽ ×4).")]
        [SerializeField] int artScale = 4;
        [Tooltip("Dưới mức này (màn rất nhỏ) cho phép tỉ lệ lẻ để UI không quá to.")]
        [SerializeField] int minWholePixels = 2;

        protected override void HandleScaleWithScreenSize()
        {
            Vector2 screen = new(Screen.width, Screen.height);
            float s = Mathf.Min(screen.x / m_ReferenceResolution.x, screen.y / m_ReferenceResolution.y);
            float px = s * artScale;
            if (px >= minWholePixels) s = Mathf.Floor(px) / artScale;
            SetScaleFactor(s);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }
    }
}
