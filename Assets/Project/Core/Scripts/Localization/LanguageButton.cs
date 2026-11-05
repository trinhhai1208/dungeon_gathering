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

        void Awake() => GetComponent<Button>().onClick.AddListener(() => GameLanguage.Apply(languageCode));

        void OnEnable()
        {
            GameLanguage.Changed += OnChanged;
            OnChanged(GameLanguage.Current);
        }

        void OnDisable() => GameLanguage.Changed -= OnChanged;

        void OnChanged(string code)
        {
            if (frame == null) return;
            frame.sprite = GameLanguage.Normalize(code) == GameLanguage.Normalize(languageCode) ? selectedFrame : normalFrame;
        }
    }
}
