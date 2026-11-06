using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.UI;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Gốc Player.prefab (ENTITIES §2.1) — một prefab cho mọi nhân vật. Triển khai IPlayerContext cho kỹ năng/passive
    /// và ITargetable cho quái. LevelDirector: Instantiate → Init → SetInputEnabled(true).
    /// </summary>
    [RequireComponent(typeof(PlayerStats), typeof(PlayerMovement), typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IPlayerContext, ITargetable
    {
        const float TargetHeroHeightUnits = 1.15f;
        const float HandHeightFromCenter = 0.26f;
        const float HandReachFromCenter = 0.33f;

        [SerializeField] WeaponHolder weapons;
        [SerializeField] PlayerSkillController skills;
        [SerializeField] PlayerEquipment equipment;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] SpriteAnimDriver anim;

        PlayerStats stats;
        PlayerMovement movement;
        PlayerInputReader input;
        StatusEffectController status;
        CharacterData character;
        readonly PlayerCombatHooks hooks = new();
        bool inputEnabled;

        // cường hóa đòn kế tiếp
        bool empowered;
        float empowerMult = 1f;
        int empowerPierce;

        struct TimedMod { public object Key; public float Until; }
        readonly List<TimedMod> timed = new();

        // FSM (ENTITIES §3.5.1): Spawn → Active → Dash/Stunned/Sealed → Dead
        readonly IPlayerState sSpawn = new PlayerSpawnState();
        readonly IPlayerState sActive = new PlayerActiveState();
        readonly IPlayerState sDash = new PlayerDashState();
        readonly IPlayerState sStunned = new PlayerStunnedState();
        readonly IPlayerState sSealed = new PlayerSealedState();
        readonly IPlayerState sDead = new PlayerDeadState();
        IPlayerState state;

        /// <summary>State hiện tại (quan sát / HUD / anim). Spawn trước khi Tick lần đầu.</summary>
        public PlayerStateId StateId => state?.Id ?? PlayerStateId.Spawn;

        public PlayerStats Stats => stats;
        public WeaponHolder Weapons => weapons;
        public PlayerSkillController Skills => skills;
        public PlayerEquipment Equipment => equipment;
        public PlayerInventory Inventory => inventory;
        public PlayerInteractor Interactor => interactor;
        public SpriteAnimDriver Anim => anim;
        public bool InputEnabled => inputEnabled;

        // ---- IPlayerContext ----
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;
        public CharacterData Character => character;
        public PlayerCombatHooks Hooks => hooks;
        public Vector2 AimDirection => weapons != null ? weapons.AimDirection : Vector2.right;
        public Vector2 MoveInput => inputEnabled ? input.Move : Vector2.zero;
        public bool IsDead => stats.IsDead;
        public float CurrentHp => stats.Hp;
        public float CurrentArmor => stats.Armor;
        public float CurrentMana => stats.Mana;

        // ---- ITargetable ----
        public bool IsAlive => this != null && stats != null && !stats.IsDead;
        public IDamageable Damageable => stats;

        void Awake()
        {
            stats = GetComponent<PlayerStats>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            status = GetComponent<StatusEffectController>();
            if (weapons == null) weapons = GetComponentInChildren<WeaponHolder>(true);
            if (skills == null) skills = gameObject.GetOrAdd<PlayerSkillController>();
            if (equipment == null) equipment = gameObject.GetOrAdd<PlayerEquipment>();
            if (inventory == null) inventory = gameObject.GetOrAdd<PlayerInventory>();
            if (interactor == null) interactor = gameObject.GetOrAdd<PlayerInteractor>();
            if (anim == null) anim = GetComponentInChildren<SpriteAnimDriver>(true);
            stats.Died += OnDied;
        }

        /// <summary>Khởi tạo từ dữ liệu run. carried = chỉ số mang từ tầng trước (null/hasValues=false → đầy).</summary>
        public void Init(CharacterData data, WeaponData weapon1, WeaponData weapon2, IReadOnlyList<EquipmentData> equips, RunStats carried)
        {
            character = data;
            if (anim != null && data != null)
            {
                if (data.portrait != null && anim.Renderer != null) anim.Renderer.sprite = data.portrait;
                if (data.animator != null) anim.SetController(data.animator);
                float k = 1f;
                if (data.portrait != null)
                {
                    var content = SpriteContentFit.ContentPixels(data.portrait);
                    float contentUnits = content.height / data.portrait.pixelsPerUnit;
                    k = contentUnits > 0.01f ? TargetHeroHeightUnits / contentUnits : 1f;
                    Vector2 contentOffset = (content.center - data.portrait.pivot) / data.portrait.pixelsPerUnit;
                    anim.transform.localPosition = new Vector3(-contentOffset.x * k, -contentOffset.y * k, 0f);
                }
                else anim.transform.localPosition = Vector3.zero;
                anim.transform.localScale = new Vector3(k, k, 1f);
            }
            anim?.ResetState();

            bool newRun = carried == null || !carried.hasValues;
            stats.Init(data, carried);
            equipment.Apply(this, stats, equips);
            stats.FillIfNew(newRun);

            if (weapons != null)
            {
                if (anim != null)
                {
                    // Bàn tay nằm ~26% chiều cao dưới tâm thân, cách tâm ~33% chiều cao theo phương ngang
                    float pivotY = anim.transform.localPosition.y - TargetHeroHeightUnits * HandHeightFromCenter;
                    weapons.SetHandAnchor(pivotY, TargetHeroHeightUnits * HandReachFromCenter);
                }
                weapons.AutoAim = Services.Save == null || Services.Save.Data.settings.autoAim;
                weapons.Init(this, weapon1, weapon2, carried != null ? carried.activeWeaponSlot : 0);
            }

            var items = new List<ItemData>();
            if (weapon1 != null) items.Add(weapon1);
            if (weapon2 != null) items.Add(weapon2);
            if (equips != null) foreach (var e in equips) if (e != null) items.Add(e);
            skills.Bind(this, data != null ? data.activeSkill : null, items);
            inventory?.Init(this, stats, data, carried);

            CombatTargets.RegisterPlayer(this);
        }

        public void SetInputEnabled(bool on)
        {
            if (inputEnabled == on) return;
            inputEnabled = on;
            input.SetEnabled(on);
            if (on)
            {
                input.SwapPressed += OnSwap;
                input.SkillPressed += OnSkill;
                input.InteractPressed += OnInteract;
            }
            else
            {
                input.SwapPressed -= OnSwap;
                input.SkillPressed -= OnSkill;
                input.InteractPressed -= OnInteract;
                movement.SetMove(Vector2.zero);
                weapons?.SetEnabled(false);
            }
            if (on) weapons?.SetEnabled(combatEnabled);
        }

        bool combatEnabled = true;
        public bool CombatEnabled => combatEnabled;

        /// <summary>Tắt riêng tấn công/kỹ năng (run đã chốt nhưng vẫn cho đi lại tới Portal — R10).</summary>
        public void SetCombatEnabled(bool on)
        {
            combatEnabled = on;
            weapons?.SetEnabled(on && inputEnabled);
        }

        void OnSwap() { if (combatEnabled) weapons?.SwapWeapon(); }
        void OnSkill(int slot) { if (combatEnabled) skills.TryUse(slot); }
        void OnInteract() => interactor?.TryInteract();

        void Update()
        {
            UpdateTimedModifiers();
            TickState();
        }

        // ------------------------------------------------------------------ State machine (ENTITIES §3.5.1)

        void TickState()
        {
            var next = ResolveState();
            if (next != state)
            {
                state?.Exit(this);
                state = next;
                state.Enter(this);
            }
            state.Tick(this);
        }

        /// <summary>Suy ra state từ cờ + điều kiện (ưu tiên: Dead > Spawn/!input > Sealed/!combat > Stunned > Dash > Active).</summary>
        IPlayerState ResolveState()
        {
            if (stats.IsDead) return sDead;
            if (!inputEnabled) return sSpawn;
            if (!combatEnabled) return sSealed;
            if (status != null && status.IsStunned) return sStunned;
            if (movement.IsDashing) return sDash;
            return sActive;
        }

        /// <summary>
        /// Khung xử lý mỗi frame, tham số hóa theo state — giữ nguyên hành vi Update() cũ.
        /// Choáng tự chặn di chuyển + đánh ngay trong đây nên Active/Dash/Stunned dùng chung (true, true).
        /// </summary>
        internal void DriveFrame(bool readInput, bool allowAttack)
        {
            Vector2 move = readInput ? input.Move : Vector2.zero;
            bool stunned = status != null && status.IsStunned;
            movement.SetMove(stunned ? Vector2.zero : move);

            if (weapons != null)
            {
                weapons.UpdateAim(move);
                if (allowAttack && !stunned && input.AttackHeld) weapons.TryAttack();
            }

            if (anim != null)
            {
                Vector2 face = weapons != null && weapons.AimTarget != null ? weapons.AimDirection : move;
                anim.Face(face.x);
                anim.Locomotion(movement.Velocity);
            }
        }

        void OnDied()
        {
            SetInputEnabled(false);
            movement.Stop();
        }

        // ---- IPlayerContext: thao tác ----
        public float GetStat(StatType stat) => stats.Get(stat);
        public void AddModifier(object source, StatModifier modifier) => stats.Sheet.Add(source, modifier);
        public void RemoveModifiers(object source) => stats.Sheet.RemoveSource(source);

        public void AddTimedModifier(StatModifier modifier, float duration)
        {
            var key = new object();
            stats.Sheet.Add(key, modifier);
            timed.Add(new TimedMod { Key = key, Until = Time.time + duration });
        }

        void UpdateTimedModifiers()
        {
            for (int i = timed.Count - 1; i >= 0; i--)
            {
                if (Time.time < timed[i].Until) continue;
                stats.Sheet.RemoveSource(timed[i].Key);
                timed.RemoveAt(i);
            }
        }

        public void Heal(float amount) => stats.Heal(amount);
        public void RestoreArmor(float amount) => stats.RestoreArmor(amount);
        public void RestoreMana(float amount) => stats.RestoreMana(amount);
        public bool TrySpendMana(float amount) => stats.TrySpendMana(amount);
        public void SetInvulnerable(object source, bool on) => stats.SetInvulnerable(source, on);
        public void Dash(Vector2 direction, float distance, float duration) => movement.Dash(direction, distance, duration);

        public void QueueEmpoweredAttack(float damageMultiplier, int extraPierce)
        {
            empowered = true;
            empowerMult = damageMultiplier;
            empowerPierce = extraPierce;
        }

        public bool TryConsumeEmpowered(out float damageMultiplier, out int extraPierce)
        {
            damageMultiplier = empowerMult;
            extraPierce = empowerPierce;
            if (!empowered) { damageMultiplier = 1f; extraPierce = 0; return false; }
            empowered = false;
            return true;
        }

        void OnDestroy()
        {
            if (stats != null) stats.Died -= OnDied;
            CombatTargets.UnregisterPlayer(this);
            hooks.ClearAll();
        }
    }
}
