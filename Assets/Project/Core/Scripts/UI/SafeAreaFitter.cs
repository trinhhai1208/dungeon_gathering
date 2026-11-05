using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>Đặt trên node SafeArea của UIRoot (UI_SYSTEM §3.3).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect last;
        Vector2Int lastScreen;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != last || lastScreen != new Vector2Int(Screen.width, Screen.height)) Apply();
        }

        void Apply()
        {
            last = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            Vector2 min = last.position, max = last.position + last.size;
            min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
