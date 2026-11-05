using System;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Cầu nối input cảm ứng (VirtualJoystick, nút HUD) → PlayerInput, để UI không tham chiếu Play.
    /// HUD gọi các hàm Press*, PlayerInputReader đọc giá trị/sự kiện.
    /// </summary>
    public static class VirtualInput
    {
        public static Vector2 Move { get; set; }
        public static bool AttackHeld { get; set; }

        public static event Action SwapWeapon;
        public static event Action Interact;
        public static event Action<int> UseSkill;   // 0 = kỹ năng nhân vật, 1..2 = kích hoạt vật phẩm
        public static event Action Pause;

        public static void PressSwapWeapon() => SwapWeapon?.Invoke();
        public static void PressInteract() => Interact?.Invoke();
        public static void PressSkill(int slot) => UseSkill?.Invoke(slot);
        public static void PressPause() => Pause?.Invoke();

        public static void ResetState()
        {
            Move = Vector2.zero;
            AttackHeld = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            ResetState();
            SwapWeapon = null; Interact = null; UseSkill = null; Pause = null;
        }
    }

    /// <summary>Đối tượng tương tác được (ENTITIES §8).</summary>
    public interface IInteractable
    {
        string GetInteractPrompt();
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
