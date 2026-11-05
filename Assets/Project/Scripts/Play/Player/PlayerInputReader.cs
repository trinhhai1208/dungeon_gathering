using System;
using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Input của Player: bàn phím/gamepad (InputAction tạo trong code — không phụ thuộc asset) + VirtualInput (joystick/nút HUD).
    /// Phím: WASD/mũi tên di chuyển · J/Space/chuột trái tấn công · Q đổi vũ khí · E kỹ năng nhân vật · 1/2 kích hoạt · F tương tác · P tạm dừng.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        InputAction move, attack, swap, skill, item1, item2, interact, pause;
        bool inputEnabled;
        bool mouseStartedOnUI;

        public event Action SwapPressed;
        public event Action InteractPressed;
        public event Action<int> SkillPressed;   // 0 nhân vật, 1..2 vật phẩm

        public Vector2 Move { get; private set; }
        public bool AttackHeld { get; private set; }

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            attack = new InputAction("Attack", InputActionType.Button);
            attack.AddBinding("<Keyboard>/j");
            attack.AddBinding("<Keyboard>/space");
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/rightTrigger");
            attack.AddBinding("<Gamepad>/buttonSouth");

            swap = Button("Swap", "<Keyboard>/q", "<Gamepad>/buttonNorth");
            skill = Button("Skill", "<Keyboard>/e", "<Gamepad>/rightShoulder");
            item1 = Button("Item1", "<Keyboard>/1", "<Gamepad>/leftShoulder");
            item2 = Button("Item2", "<Keyboard>/2", "<Gamepad>/leftTrigger");
            interact = Button("Interact", "<Keyboard>/f", "<Gamepad>/buttonWest");
            pause = Button("Pause", "<Keyboard>/p", "<Gamepad>/start");
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            return a;
        }

        public void SetEnabled(bool on)
        {
            if (inputEnabled == on) return;
            inputEnabled = on;
            if (on)
            {
                foreach (var a in All()) a.Enable();
                swap.performed += OnSwap;
                skill.performed += OnSkill0;
                item1.performed += OnSkill1;
                item2.performed += OnSkill2;
                interact.performed += OnInteract;
                pause.performed += OnPause;
                VirtualInput.SwapWeapon += RaiseSwap;
                VirtualInput.Interact += RaiseInteract;
                VirtualInput.UseSkill += RaiseSkill;
            }
            else
            {
                swap.performed -= OnSwap;
                skill.performed -= OnSkill0;
                item1.performed -= OnSkill1;
                item2.performed -= OnSkill2;
                interact.performed -= OnInteract;
                pause.performed -= OnPause;
                VirtualInput.SwapWeapon -= RaiseSwap;
                VirtualInput.Interact -= RaiseInteract;
                VirtualInput.UseSkill -= RaiseSkill;
                foreach (var a in All()) a.Disable();
                Move = Vector2.zero;
                AttackHeld = false;
            }
        }

        InputAction[] All() => new[] { move, attack, swap, skill, item1, item2, interact, pause };

        void OnSwap(InputAction.CallbackContext _) => RaiseSwap();
        void OnSkill0(InputAction.CallbackContext _) => RaiseSkill(0);
        void OnSkill1(InputAction.CallbackContext _) => RaiseSkill(1);
        void OnSkill2(InputAction.CallbackContext _) => RaiseSkill(2);
        void OnInteract(InputAction.CallbackContext _) => RaiseInteract();
        void OnPause(InputAction.CallbackContext _) => VirtualInput.PressPause();

        void RaiseSwap() { if (Ok()) SwapPressed?.Invoke(); }
        void RaiseInteract() { if (Ok()) InteractPressed?.Invoke(); }
        void RaiseSkill(int i) { if (Ok()) SkillPressed?.Invoke(i); }
        bool Ok() => inputEnabled && (Services.Game == null || !Services.Game.IsPaused);

        void Update()
        {
            if (!inputEnabled) return;
            Vector2 m = move.ReadValue<Vector2>();
            if (VirtualInput.Move.sqrMagnitude > m.sqrMagnitude) m = VirtualInput.Move;
            Move = Vector2.ClampMagnitude(m, 1f);

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    mouseStartedOnUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                       UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                }
                else if (!Mouse.current.leftButton.isPressed)
                {
                    mouseStartedOnUI = false;
                }
            }

            AttackHeld = IsAttackActive();
        }

        bool IsAttackActive()
        {
            if (VirtualInput.AttackHeld) return true;
            if (mouseStartedOnUI) return false;
            if (VirtualInput.Move.sqrMagnitude > 0.001f && Mouse.current != null && Mouse.current.leftButton.isPressed)
                return false;

            if (!attack.IsPressed()) return false;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return false;
            }
            return true;
        }

        void OnDisable() => SetEnabled(false);

        void OnDestroy()
        {
            foreach (var a in All()) a?.Dispose();
        }
    }
}
