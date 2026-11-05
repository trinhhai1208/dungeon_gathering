using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Âm khi bấm Button. UIService tự gắn UiClick cho mọi Button trong screen khi mở;
    /// thêm component này vào một Button để đổi âm (VD: UiConfirm, UiBack) hoặc chọn None để im lặng.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSfx : MonoBehaviour
    {
        public SfxId sound = SfxId.UiClick;
        bool hooked;

        void Awake() => Hook();

        internal void Hook()
        {
            if (hooked) return;
            hooked = true;
            if (TryGetComponent<Button>(out var b)) b.onClick.AddListener(PlayClick);
        }

        void PlayClick() => Sfx.Play(sound);

        /// <summary>Gắn âm click cho mọi Button con chưa có ButtonSfx (gọi một lần khi screen được tạo).</summary>
        public static void AttachAll(GameObject root)
        {
            if (root == null) return;
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.TryGetComponent<ButtonSfx>(out var existing)) { existing.Hook(); continue; }
                b.gameObject.AddComponent<ButtonSfx>().Hook();
            }
        }
    }
}
