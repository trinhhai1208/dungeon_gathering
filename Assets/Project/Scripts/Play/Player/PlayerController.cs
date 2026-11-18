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
    public sealed class PlayerController : MonoBehaviour, IPlayerContext, ITargetable, IStateActor
    {
        const float TargetHeroHeightUnits = 1.15f;
        const float HandHeightFromCenter = 0.38f;
        const float HandSideOffsetUnits = 0.25f;
        const float HandReachFromCenter = 0f;

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
        HeroDefinitionSO character;
        readonly PlayerCombatHooks hooks = new();
        bool inputEnabled;

        // cường hóa đòn kế tiếp
        bool empowered;
        float empowerMult = 1f;
        int empowerPierce;

        struct TimedMod { public object Key; public float Until; }
        readonly List<TimedMod> timed = new();

        // FSM dùng chung (Combat/States/StateMachine.cs) — luật chuyển ở PlayerStates.Build
        StateMachine<PlayerController> fsm;
        internal StateMachine<PlayerController> States => fsm ??= PlayerStates.Build(this);

        /// <summary>State hiện tại (quan sát / HUD / anim).</summary>
        public ActorStateId StateId => States.CurrentId ?? ActorStateId.Spawn;

        // ---- IStateActor ----
        public bool IsCrowdControlled => status != null && status.IsStunned;
        public ActorStateId StateAfterStun => ActorStateId.Active;
        public void InterruptActions()
        {
            movement.SetMove(Vector2.zero);
            movement.CancelDash();
            weapons?.CancelAttack();
        }

        public PlayerStats Stats => stats;
        internal PlayerMovement Movement => movement;
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
        public HeroDefinitionSO Character => character;
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
            hooks.Attacked += OnHeroAttacked;
            hooks.HitDealt += OnHeroHit;
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
        public void Init(HeroDefinitionSO data, WeaponData weapon1, WeaponData weapon2, IReadOnlyList<EquipmentSO> equips, RunStats carried)
        {
            if (character != null && character.innatePassive != null)
                character.innatePassive.OnUnequip(this);

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
            stats.Init(data, carried, deferClamp: true);
            equipment.Apply(this, stats, equips);
            if (data != null && data.innatePassive != null) data.innatePassive.OnEquip(this);

            if (weapons != null)
            {
                if (anim != null)
                {
                    // Trục xoay vũ khí đặt ở bụng (cách tâm thân ~20% chiều cao), không vươn tay (reach = 0)
                    float pivotY = anim.transform.localPosition.y - TargetHeroHeightUnits * HandHeightFromCenter;
                    weapons.SetHandAnchor(pivotY, TargetHeroHeightUnits * HandReachFromCenter, HandSideOffsetUnits);
                }
                weapons.AutoAim = Services.Save == null || Services.Save.Data.settings.autoAim;
                weapons.Init(this, weapon1, weapon2, carried != null ? carried.activeWeaponSlot : 0);
            }

            var items = new List<ItemData>();
            if (weapon1 != null) items.Add(weapon1);
            if (weapon2 != null) items.Add(weapon2);
            if (equips != null) foreach (var e in equips) if (e != null) items.Add(e);
            skills.Bind(this, data != null ? data.ResolveSkill() : null, items);
            inventory?.Init(this, stats, data, carried);

            // Kẹp/làm đầy SAU khi mọi nguồn modifier (trang bị, set, innate, passive vũ khí) đã cộng xong.
            stats.FillIfNew(newRun);

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
        /// <summary>Skill chỉ dùng được khi điều khiển bình thường (không choáng, không pause, không Sealed/Spawn/Dead).</summary>
        void OnSkill(int slot)
        {
            if (!combatEnabled || IsCrowdControlled || (Services.Game != null && Services.Game.IsPaused)) return;
            if (!States.Is(ActorStateId.Active) && !States.Is(ActorStateId.Dash)) return;
            skills.TryUse(slot);
        }
        void OnInteract() => interactor?.TryInteract();

        /// <summary>VFX đòn đánh của hero (theo định nghĩa) — thêm vào VFX riêng của vũ khí.</summary>
        void OnHeroAttacked(WeaponData w)
        {
            if (character == null || character.attackVfx == null || weapons == null) return;
            var view = weapons.GetView(weapons.ActiveSlot);
            Vector3 at = view != null ? view.transform.position : transform.position;
            Vector2 aim = weapons.AimDirection;
            CombatFx.Play(character.attackVfx, at, Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg));
        }

        void OnHeroHit(HitReport r)
        {
            if (character != null && character.hitVfx != null) CombatFx.Play(character.hitVfx, r.Point, Quaternion.identity);
        }

        void Update()
        {
            UpdateTimedModifiers();
            if (States.Current == null) States.Reset(ActorStateId.Spawn);
            States.Tick();
        }

        /// <summary>
        /// Khung xử lý mỗi frame, tham số hóa theo state — giữ nguyên hành vi Update() cũ.
        /// Vẫn giữ kiểm tra <c>status.IsStunned</c> bên trong để phòng hộ trường hợp state Sealed
        /// (ưu tiên cao hơn Stunned sau khi thắng) đang chạy lúc player bị choáng.
        /// </summary>
        internal void DriveFrame(bool readInput, bool allowAttack)
        {
            Vector2 move = readInput ? input.Move : Vector2.zero;
            bool stunned = status != null && status.IsStunned;
            movement.SetMove(stunned ? Vector2.zero : move);

            if (weapons != null)
            {
                weapons.UpdateAim(move);
                // Pause: timeScale=0 nhưng Update vẫn chạy → không được tung đòn (trừ mana, đạn treo rồi bay ra khi Resume).
                bool paused = Services.Game != null && Services.Game.IsPaused;
                if (allowAttack && !stunned && !paused && input.AttackHeld) weapons.TryAttack();
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
            if (character != null && character.innatePassive != null)
                character.innatePassive.OnUnequip(this);
            if (stats != null) stats.Died -= OnDied;
            CombatTargets.UnregisterPlayer(this);
            hooks.ClearAll();
        }
    }
}
