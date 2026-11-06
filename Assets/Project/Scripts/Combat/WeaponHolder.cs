using DungeonGathering.Catalog;

using DungeonGathering.Core;

using UnityEngine;



namespace DungeonGathering.Combat

{

    /// <summary>

    /// Giữ đúng 2 vũ khí (WEAPONS §6.2). Auto-Aim vào quái gần nhất có đường nhìn, không có thì theo hướng di chuyển.

    /// Xoay WeaponPivot 360°, flipY khi ngắm sang trái. Passive vũ khí chỉ bật khi đang cầm.

    /// </summary>

    public sealed class WeaponHolder : MonoBehaviour

    {

        [SerializeField] Transform weaponPivot;

        [SerializeField] Transform weaponSocket;

        [SerializeField] Transform socketPrimary;

        [SerializeField] Transform socketSecondary;

        [SerializeField] float aimRange = 8f;

        [SerializeField] int weaponSortingOrder = 5;

        [Tooltip("Góc (độ) của hướng chuôi→mũi trong sprite kiếm gốc. Sheet hiện tại: mũi chĩa lên trên-trái = 135°.")]

        [SerializeField] float meleeSpriteForwardAngle = 135f;

        [Tooltip("Góc (độ) của hướng nòng trong sprite súng gốc. Sheet hiện tại: nòng sang phải = 0°.")]

        [SerializeField] float rangedSpriteForwardAngle = 0f;

        [Tooltip("Góc nâng lưỡi kiếm quanh bàn tay khi đứng yên (tay vẫn ở hông, mũi kiếm chếch lên phía trước).")]

        [SerializeField] float restRaiseAngle = 35f;



        [Tooltip("Khoảng cách lệch sang bên sườn nhân vật (Soul Knight style)")]

        [SerializeField] float sideOffset = 0.22f;



        SpriteRenderer bodyRenderer;


        float handBaseY;

        bool lastFacingRight = true;



        readonly WeaponData[] weapons = new WeaponData[2];

        readonly WeaponBase[] views = new WeaponBase[2];

        IPlayerContext owner;

        int activeSlot;

        Vector2 aimDir = Vector2.right;

        IDamageable aimTarget;

        float retargetTimer;

        bool autoAim = true;

        WeaponPassiveSO activePassive;



        public int ActiveSlot => activeSlot;

        public WeaponData Active => weapons[activeSlot];

        public WeaponData Get(int slot) => slot is >= 0 and < 2 ? weapons[slot] : null;

        public WeaponBase GetView(int slot) => slot is >= 0 and < 2 ? views[slot] : null;

        public Vector2 AimDirection => aimDir;

        public IDamageable AimTarget => aimTarget;

        public bool AutoAim { get => autoAim; set => autoAim = value; }

        public Transform WeaponPivot => weaponPivot;

        public Transform WeaponSocket => weaponSocket;

        public Transform SocketPrimary => socketPrimary;

        public Transform SocketSecondary => socketSecondary;

        public float AimRange => aimRange;

        public int WeaponSortingOrder => weaponSortingOrder;



        void Awake()

        {

            if (weaponPivot == null) weaponPivot = transform;

            if (weaponSocket == null) weaponSocket = weaponPivot;

            if (socketPrimary == null) socketPrimary = weaponSocket;

            if (socketSecondary == null) socketSecondary = weaponSocket;

        }



        Transform GetSocket(int slot)

        {

            // Cả 2 slot vũ khí đều dùng socketPrimary khi cầm tay để luôn ở cùng vị trí bàn tay nhân vật

            return socketPrimary != null ? socketPrimary : (weaponSocket != null ? weaponSocket : (weaponPivot != null ? weaponPivot : transform));

        }



        /// <summary>

        /// Đặt điểm xoay ngang bàn tay nhân vật: WeaponPivot ở độ cao tay, lệch hông theo Soul Knight style.

        /// </summary>

        public void SetHandAnchor(float pivotLocalY, float handReach, float lateralOffset = 0.22f)

        {

            handBaseY = pivotLocalY;

            sideOffset = lateralOffset;

            UpdatePivotPosition(lastFacingRight);

            foreach (var s in new[] { socketPrimary, socketSecondary, weaponSocket })

                if (s != null && s != weaponPivot) s.localPosition = new Vector3(handReach, 0f, 0f);

        }



        void UpdatePivotPosition(bool facingRight)

        {

            if (weaponPivot != null)

            {

                float x = facingRight ? sideOffset : -sideOffset;

                weaponPivot.localPosition = new Vector3(x, handBaseY, 0f);

            }

        }



        public void Init(IPlayerContext ctx, WeaponData primary, WeaponData secondary, int startSlot = 0)

        {

            owner = ctx;

            var animDriver = ctx != null ? ctx.Transform.GetComponentInChildren<SpriteAnimDriver>(true) : null;

            bodyRenderer = animDriver != null ? animDriver.Renderer : null;


            for (int i = 0; i < 2; i++)

            {

                if (views[i] != null)

                {

#if UNITY_EDITOR

                    if (!Application.isPlaying) DestroyImmediate(views[i].gameObject);

                    else Destroy(views[i].gameObject);

#else

                    Destroy(views[i].gameObject);

#endif

                }

                views[i] = null;

            }

            weapons[0] = primary;

            weapons[1] = secondary;

            for (int i = 0; i < 2; i++)

                if (weapons[i] != null) views[i] = CreateView(weapons[i], i);



            activeSlot = weapons[startSlot] != null ? startSlot : (weapons[0] != null ? 0 : 1);

            ApplyActive();

        }



        WeaponBase CreateView(WeaponData data, int slot = 0)

        {

            Transform targetSocket = GetSocket(slot);

            GameObject go;

            if (data.weaponPrefab != null) go = Instantiate(data.weaponPrefab, targetSocket, false);

            else

            {

                go = new GameObject("Weapon_" + data.id);

                go.transform.SetParent(targetSocket, false);

                go.transform.localScale = Vector3.one * (data.visualScale > 0f ? data.visualScale : 0.55f);



                // Căn sprite để hướng chuôi→mũi trùng trục +X của socket (hướng ngắm):

                // sheet kiếm vẽ chéo, mũi lên trên-trái (135°); súng vẽ ngang, nòng sang phải (0°).

                var visual = new GameObject("Visual");

                visual.transform.SetParent(go.transform, false);

                visual.transform.localPosition = data.holdOffset;

                float spriteForward = data.GetForwardAngle(meleeSpriteForwardAngle, rangedSpriteForwardAngle);

                visual.transform.localRotation = Quaternion.Euler(0f, 0f, -spriteForward);



                var sr = visual.AddComponent<SpriteRenderer>();

                sr.sprite = data.weaponSprite;

                sr.sortingOrder = weaponSortingOrder;

            }

            var view = go.GetComponent<WeaponBase>();

            if (view == null)

                view = data.weaponType == WeaponType.Melee ? go.AddComponent<MeleeWeapon>() : go.AddComponent<RangedWeapon>();

            view.Bind(data, owner);

            go.SetActive(false);

            return view;

        }



        void ApplyActive()

        {

            for (int i = 0; i < 2; i++)

                if (views[i] != null)

                {

                    if (i != activeSlot) views[i].CancelAttack();

                    views[i].gameObject.SetActive(i == activeSlot);

                }



            if (activePassive != null && owner != null) activePassive.OnUnequip(owner);

            activePassive = Active != null ? Active.passive : null;

            if (activePassive != null && owner != null) activePassive.OnEquip(owner);

        }



        float swapCooldown;



        /// <summary>Đổi giữa vũ khí 1 ↔ 2 (nút ô vũ khí trên HUD).</summary>

        public void SwapWeapon()

        {

            if (Time.time < swapCooldown) return;

            int other = 1 - activeSlot;

            if (weapons[other] == null) return;

            swapCooldown = Time.time + 0.2f;

            views[activeSlot]?.CancelAttack(); // hủy lượt bắn/vung đang dở

            activeSlot = other;


            ApplyActive();

            EventBus<WeaponSwappedEvent>.Raise(new WeaponSwappedEvent { ActiveSlot = activeSlot });

        }



        public bool TryAttack()

        {

            var view = views[activeSlot];

            return view != null && view.TryAttack(aimDir);

        }



        /// <summary>Cập nhật hướng ngắm (gọi mỗi frame từ Player với hướng di chuyển hiện tại).</summary>

        public void UpdateAim(Vector2 moveDir)

        {

            if (owner == null) return;

            Vector2 origin = owner.Transform.position;



            if (autoAim)

            {

                retargetTimer -= Time.deltaTime;

                if (retargetTimer <= 0f || aimTarget == null || aimTarget.IsDead || !aimTarget.transform.gameObject.activeInHierarchy)

                {

                    retargetTimer = 0.15f;

                    float range = Active != null ? Mathf.Max(aimRange, Active.attackRange) : aimRange;

                    aimTarget = CombatUtil.FindNearest(origin, range, CombatLayers.EnemyTargets, CombatLayers.Walls);

                }

            }

            else aimTarget = null;



            bool hasAimTarget = false;

            if (aimTarget != null && !aimTarget.IsDead)

            {

                Vector2 d = (Vector2)aimTarget.transform.position - origin;

                if (d.sqrMagnitude > 0.0001f) { aimDir = d.normalized; hasAimTarget = true; }

            }

            else if (moveDir.sqrMagnitude > 0.01f)

            {

                aimDir = moveDir.normalized;

                hasAimTarget = true;

            }



            var view = views[activeSlot];

            bool busy = view != null && view.IsBusy;



            if (hasAimTarget || busy)

            {

                float rawAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

                if (rawAngle is > -85f and < 85f) lastFacingRight = true;

                else if (rawAngle is > 95f or < -95f) lastFacingRight = false;

            }

            else if (Mathf.Abs(moveDir.x) > 0.01f)

            {

                lastFacingRight = moveDir.x > 0f;

            }

            else if (Mathf.Abs(aimDir.x) > 0.01f)

            {

                lastFacingRight = aimDir.x > 0f;

            }

            UpdatePivotPosition(lastFacingRight);



            float pivotAngle;

            if (!hasAimTarget && !busy)

            {

                // Tư thế nghỉ: tay ở hông/vai theo loại vũ khí (Soul Knight style)

                pivotAngle = lastFacingRight ? 0f : 180f;

                view?.SetFacingLeft(!lastFacingRight);

                if (view != null)

                {

                    float raise = GetRestAngle(Active);

                    view.transform.localRotation = Quaternion.Euler(0f, 0f, lastFacingRight ? raise : -raise);

                }

            }

            else

            {

                pivotAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

                view?.SetFacingLeft(Mathf.Abs(pivotAngle) > 90f);

                if (view != null && !busy) view.transform.localRotation = Quaternion.identity;

            }

            weaponPivot.localRotation = Quaternion.Euler(0f, 0f, pivotAngle);

            UpdateSorting(view, pivotAngle);

        }



        float GetRestAngle(WeaponData data)

        {

            if (data == null) return restRaiseAngle;

            if (data.customRestAngle != 0f) return data.customRestAngle;

            return data.EffectiveHoldStyle switch

            {

                WeaponHoldStyle.OneHandedSide => restRaiseAngle,

                WeaponHoldStyle.HeavyShoulder => 55f,

                WeaponHoldStyle.Ranged => 0f,

                WeaponHoldStyle.Spear => 15f,

                _ => data.weaponType == WeaponType.Melee ? restRaiseAngle : 0f

            };

        }



        /// <summary>Trong Soul Knight & top-down pixel art, vũ khí luôn vẽ ở phía trước thân (+1) để không bị mũ/đầu che.</summary>

        void UpdateSorting(WeaponBase view, float pivotAngle)

        {

            if (view == null || view.Sprite == null) return;

            int targetLayerId = bodyRenderer != null ? bodyRenderer.sortingLayerID : view.Sprite.sortingLayerID;

            int targetOrder = (bodyRenderer != null ? bodyRenderer.sortingOrder : weaponSortingOrder) + 1;



            if (view.Sprite.sortingLayerID == targetLayerId && view.Sprite.sortingOrder == targetOrder) return;



            foreach (var v in views)

            {

                if (v == null || v.Sprite == null) continue;

                v.Sprite.sortingLayerID = targetLayerId;

                v.Sprite.sortingOrder = targetOrder;

            }

        }



        public void SetEnabled(bool on)

        {

            enabled = on;

            if (!on) foreach (var v in views) v?.CancelAttack();

        }



        void OnDisable()

        {

            foreach (var v in views) if (v != null) v.CancelAttack();

        }



        void OnDestroy()

        {

            if (activePassive != null && owner != null) activePassive.OnUnequip(owner);

            activePassive = null;

        }

    }

}

