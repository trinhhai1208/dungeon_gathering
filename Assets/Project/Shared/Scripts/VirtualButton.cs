using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonGathering.UI
{
    public enum VirtualAction { Attack, SwapWeapon, Interact, CharacterSkill, ItemSkill1, ItemSkill2, Pause }

    /// <summary>
    /// Nút HUD → VirtualInput. Attack là nút giữ (AttackHeld), các nút khác bắn khi nhấn xuống
    /// (phản hồi nhanh hơn onClick trên cảm ứng).
    /// </summary>
    public sealed class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] VirtualAction action;
        [SerializeField] RectTransform pressVisual;
        [SerializeField] float pressedOffset = -4f;

        bool held;
        Vector2 rest;

        public VirtualAction Action => action;

        void Awake() { if (pressVisual != null) rest = pressVisual.anchoredPosition; }

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            if (pressVisual != null) pressVisual.anchoredPosition = rest + new Vector2(0, pressedOffset);
            switch (action)
            {
                case VirtualAction.Attack: VirtualInput.AttackHeld = true; break;
                case VirtualAction.SwapWeapon: VirtualInput.PressSwapWeapon(); break;
                case VirtualAction.Interact: VirtualInput.PressInteract(); break;
                case VirtualAction.CharacterSkill: VirtualInput.PressSkill(0); break;
                case VirtualAction.ItemSkill1: VirtualInput.PressSkill(1); break;
                case VirtualAction.ItemSkill2: VirtualInput.PressSkill(2); break;
                case VirtualAction.Pause: VirtualInput.PressPause(); break;
            }
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) { if (action == VirtualAction.Attack && held) Release(); }

        void Release()
        {
            held = false;
            if (pressVisual != null) pressVisual.anchoredPosition = rest;
            if (action == VirtualAction.Attack) VirtualInput.AttackHeld = false;
        }

        void OnDisable() { if (held) Release(); }
    }
}
