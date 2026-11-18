using DungeonGathering.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Banner cố định trên UILayer.Toast khi SaveSystem chuyển sang chỉ đọc (CanWrite=false):
    /// tab khác giữ khóa ghi (WebGL) hoặc save của bản game mới hơn. Không chặn input.
    /// Dựng bằng code để không phụ thuộc prefab; chữ đi qua LocalizedTMPText nên đổi theo ngôn ngữ.
    /// </summary>
    public sealed class SaveReadOnlyBanner : MonoBehaviour
    {
        public const string KeyOtherTab = "save.readonly_other_tab";
        public const string KeyNewerVersion = "save.readonly_newer";

        static SaveReadOnlyBanner current;
        LocalizedTMPText label;

        public static void Show(UIRoot root, bool newerVersion)
        {
            if (root == null) return;
            if (current == null) current = Create(root.GetLayer(UILayer.Toast));
            current.label.SetKey(newerVersion ? KeyNewerVersion : KeyOtherTab);
            current.gameObject.SetActive(true);
        }

        static SaveReadOnlyBanner Create(RectTransform parent)
        {
            var go = new GameObject("SaveReadOnlyBanner", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 44f);
            rt.anchoredPosition = Vector2.zero;
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.45f, 0.08f, 0.08f, 0.9f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            var trt = (RectTransform)textGo.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 4f);
            trt.offsetMax = new Vector2(-12f, -4f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = 20f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var banner = go.AddComponent<SaveReadOnlyBanner>();
            banner.label = textGo.AddComponent<LocalizedTMPText>();
            return banner;
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
