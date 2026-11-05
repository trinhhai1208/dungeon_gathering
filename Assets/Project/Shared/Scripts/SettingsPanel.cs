using DungeonGathering.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.UI
{
    /// <summary>
    /// Panel cài đặt dùng chung (thiết kế: phần CÀI ĐẶT của UI-Pause.dc.html): âm lượng nhạc/hiệu ứng 10 nấc,
    /// Auto-Aim, Rung. Nút ngôn ngữ là <see cref="Localization.LanguageButton"/> riêng, tự xử lý.
    /// Nhúng trong PauseScreen, hoặc mở popup <c>ui/popup/settings</c> từ Menu.
    /// Đọc/ghi <c>SaveData.settings</c>, áp âm lượng qua <c>AudioManager.ApplySettings</c>, lưu có debounce.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [System.Serializable]
        public sealed class VolumeRow
        {
            public Button decrease;
            public Button increase;
            public Image[] cells = new Image[10];
            public TMP_Text percent;
        }

        [System.Serializable]
        public sealed class ToggleRow
        {
            public Button button;
            public Image frame;
            public TMP_Text label;   // BẬT / TẮT (key common.on / common.off qua LocalizedTMPText)
        }

        [SerializeField] VolumeRow bgm = new();
        [SerializeField] VolumeRow sfx = new();
        [SerializeField] ToggleRow autoAim = new();
        [SerializeField] ToggleRow vibration = new();

        [Header("Sprites")]
        [SerializeField] Sprite cellOn;
        [SerializeField] Sprite cellOff;
        [SerializeField] Sprite toggleOnFrame;
        [SerializeField] Sprite toggleOffFrame;
        [SerializeField] Color toggleOnText = new Color32(0x1b, 0x14, 0x20, 0xff);
        [SerializeField] Color toggleOffText = new Color32(0xef, 0xe6, 0xdc, 0xff);
        [SerializeField] float saveDelay = 0.5f;

        float saveAt = -1f;

        SettingsData Settings => Services.Save != null ? Services.Save.Data.settings : null;

        void Awake()
        {
            bgm.decrease?.onClick.AddListener(() => Step(true, -1));
            bgm.increase?.onClick.AddListener(() => Step(true, +1));
            sfx.decrease?.onClick.AddListener(() => Step(false, -1));
            sfx.increase?.onClick.AddListener(() => Step(false, +1));
            autoAim.button?.onClick.AddListener(() => Flip(s => s.autoAim = !s.autoAim));
            vibration.button?.onClick.AddListener(() => Flip(s => s.vibration = !s.vibration));
        }

        void OnEnable() => Refresh();

        void OnDisable()
        {
            if (saveAt >= 0f) { saveAt = -1f; Services.Save?.RequestSave(); }
        }

        void Update()
        {
            if (saveAt >= 0f && Time.unscaledTime >= saveAt) { saveAt = -1f; Services.Save?.RequestSave(); }
        }

        void Step(bool music, int delta)
        {
            var s = Settings; if (s == null) return;
            if (music) s.bgmVolume = Mathf.Clamp(s.bgmVolume + delta, 0, 10);
            else s.sfxVolume = Mathf.Clamp(s.sfxVolume + delta, 0, 10);
            Changed(s);
        }

        void Flip(System.Action<SettingsData> change)
        {
            var s = Settings; if (s == null) return;
            change(s);
            Changed(s);
        }

        void Changed(SettingsData s)
        {
            Services.Audio?.ApplySettings(s);
            saveAt = Time.unscaledTime + saveDelay;
            Refresh();
        }

        public void Refresh()
        {
            var s = Settings ?? new SettingsData();
            Paint(bgm, s.bgmVolume);
            Paint(sfx, s.sfxVolume);
            Paint(autoAim, s.autoAim);
            Paint(vibration, s.vibration);
        }

        void Paint(VolumeRow row, int value)
        {
            for (int i = 0; i < row.cells.Length; i++)
                if (row.cells[i] != null) row.cells[i].sprite = i < value ? cellOn : cellOff;
            if (row.percent != null) row.percent.text = (value * 10) + "%";
        }

        void Paint(ToggleRow row, bool on)
        {
            if (row.frame != null) row.frame.sprite = on ? toggleOnFrame : toggleOffFrame;
            if (row.label == null) return;
            row.label.color = on ? toggleOnText : toggleOffText;
            var loc = row.label.GetComponent<Localization.LocalizedTMPText>();
            if (loc != null) loc.SetKey(on ? "common.on" : "common.off");
            else row.label.text = on ? "ON" : "OFF";
        }
    }
}
