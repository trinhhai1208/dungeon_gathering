using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Localization
{
    /// <summary>
    /// Nút chọn ngôn ngữ trong Settings (icon cờ pixel). Nút đang chọn hiện khung sáng, các nút khác khung tối.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LanguageButton : MonoBehaviour
    {
        [SerializeField] string languageCode = GameLanguage.English;
        [SerializeField] Image frame;
        [SerializeField] Sprite selectedFrame;
        [SerializeField] Sprite normalFrame;
        [SerializeField] TMP_Text label;
        [SerializeField] Color selectedTextColor = new Color32(0x17, 0x11, 0x1e, 0xff);
        [SerializeField] Color normalTextColor = new Color32(0xef, 0xe6, 0xdc, 0xff);

        void Awake() => GetComponent<Button>().onClick.AddListener(() => GameLanguage.Apply(languageCode));

        void OnEnable()
        {
            GameLanguage.Changed += OnChanged;
            OnChanged(GameLanguage.Current);
        }

        void OnDisable() => GameLanguage.Changed -= OnChanged;

        void OnChanged(string code)
        {
            bool selected = GameLanguage.Normalize(code) == GameLanguage.Normalize(languageCode);
            if (frame != null)
                frame.sprite = selected ? selectedFrame : normalFrame;
            if (label != null)
                label.color = selected ? selectedTextColor : normalTextColor;
        }
    }
}
