using DG.Tweening;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    public enum LootKind
    {
        Gold = 0,
        Mana = 2,
        Health = 3,
        GoldCoin = 0,
        ManaOrb = 2,
        HealthPotion = 3
    }

    public enum LootType
    {
        GoldCoin = 0,
        ManaOrb = 2,
        HealthPotion = 3
    }

    /// <summary>
    /// Hạt loot rơi thế giới (tiền vàng, coin xanh, ngọc mana, bình máu).
    /// - Nảy ra xung quanh bằng DOTween DOJump.
    /// - Tự động hút (magnet) về phía Player khi vào pickupRadius hoặc khi RoomClearedEvent kích hoạt.
    /// - Khử trùng lặp va chạm bằng cờ đồng bộ 'collected' (R11).
    /// - Quản lý vòng đời qua IPoolable và PoolManager (R14).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class LootDrop : MonoBehaviour, IPoolable
    {
        [Header("Loot Configuration")]
        [SerializeField] LootKind kind = LootKind.Gold;
        [SerializeField, Min(1)] int amount = 1;

        [Header("Magnet & Pickup Settings")]
        [Tooltip("Cho phép tự động hút về phía Player hay không (thường tắt cho bình máu để tránh nhặt nhầm)")]
        [SerializeField] bool allowMagnet = true;
        [SerializeField] float pickupRadius = 2.2f;
        [SerializeField] float magnetSpeed = 4f;
        [SerializeField] float magnetAccel = 30f;
        [SerializeField] float maxMagnetSpeed = 25f;
        [SerializeField] float collectDistance = 0.35f;

        [Header("Feedback")]
        [SerializeField] GameObject pickupVfxPrefab;
        [SerializeField] AudioClip collectSfx;

        bool collected;
        bool settled;
        bool magnet;
        float speed;
        RoomController room;

        public LootKind Kind => kind;
        public LootType Type => (LootType)kind;
        public int Amount => amount;

        public void SetAmount(int value) => amount = Mathf.Max(1, value);

        public void Configure(LootKind lootKind, int value)
        {
            kind = lootKind;
            amount = Mathf.Max(1, value);
        }

        public void Configure(LootType lootType, int value)
        {
            kind = (LootKind)lootType;
            amount = Mathf.Max(1, value);
        }

        public void Scatter(Vector2 target)
        {
            settled = false;
            room = RoomController.FindAt(target);
            transform.DOJump(target, 0.6f, 1, 0.45f).SetEase(Ease.OutQuad).OnComplete(OnSettled);
        }

        void OnSettled()
        {
            settled = true;
            // Kiểm tra phòng đã sạch chưa ngay lúc nảy xong (chống sót loot khi quái cuối chết cùng frame)
            if (allowMagnet && (room == null || room.IsCleared))
            {
                magnet = true;
            }
        }

        void OnEnable()
        {
            EventBus<RoomClearedEvent>.Subscribe(OnRoomCleared);
        }

        void OnDisable()
        {
            EventBus<RoomClearedEvent>.Unsubscribe(OnRoomCleared);
        }

        void OnRoomCleared(RoomClearedEvent e)
        {
            if (!allowMagnet) return;
            if (room != null && room.RoomId != e.RoomId) return;
            if (settled) magnet = true;
        }

        void Update()
        {
            if (collected || !settled) return;

            var player = LevelDirector.Current != null ? LevelDirector.Current.Player : null;
            if (player == null || player.IsDead) return;

            Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
            float sqrDist = to.sqrMagnitude;

            if (allowMagnet && !magnet && sqrDist <= pickupRadius * pickupRadius)
            {
                magnet = true;
            }

            if (magnet)
            {
                speed = Mathf.Min(speed + magnetAccel * Time.deltaTime, maxMagnetSpeed);
                transform.position += (Vector3)(to.normalized * Mathf.Max(magnetSpeed, speed) * Time.deltaTime);

                if (sqrDist <= collectDistance * collectDistance)
                {
                    Collect(player);
                }
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryHandleTrigger(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryHandleTrigger(other);
        }

        void TryHandleTrigger(Collider2D other)
        {
            if (collected) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;

            // Nếu đang nảy dở và loại loot có bật magnet thì chờ nảy xong
            if (allowMagnet && !settled) return;

            Collect(player);
        }

        void Collect(PlayerController player)
        {
            if (collected) return;
            collected = true; // R11: Đặt cờ đồng bộ trước khi thực thi side-effects

            switch (kind)
            {
                case LootKind.Gold:
                    Services.Session?.AddRunCoins(amount);
                    break;

                case LootKind.Mana:
                    player.RestoreMana(amount);
                    break;

                case LootKind.Health:
                    player.Heal(amount);
                    break;
            }

            if (pickupVfxPrefab != null)
            {
                CombatFx.Play(pickupVfxPrefab, transform.position, Quaternion.identity);
            }

            Sfx.PlayOverride(collectSfx, kind switch
            {
                LootKind.Mana => SfxId.ManaPickup,
                LootKind.Health => SfxId.PotionDrink,
                _ => SfxId.CoinPickup,
            });

            if (Services.Pool != null)
                Services.Pool.Return(gameObject);
            else
                Destroy(gameObject);
        }

        public void OnSpawn()
        {
            collected = false;
            settled = false;
            magnet = false;
            speed = 0f;
            room = null;
        }

        public void OnDespawn()
        {
            DOTween.Kill(transform);
        }
    }
}
