using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using UnityEngine;

namespace DungeonGathering.Play
{
    public enum RunState { Loading, Playing, Finished }

    /// <summary>
    /// IModuleEntry của scene Play (ENTITIES §2.3, ARCHITECTURE §2): đọc SessionData → tải catalog/config → sinh dungeon
    /// → spawn Player → mở HUD → bật input. Chủ duy nhất của RunFinishedEvent; chốt kết quả run đúng MỘT lần (R10).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelDirector : MonoBehaviour, IModuleEntry
    {
        [SerializeField] PlayPrefabSet prefabs;
        [SerializeField] DungeonBuilder builder;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] float resultDelay = 1.2f;

        public static LevelDirector Current { get; private set; }

        public RunState State { get; private set; } = RunState.Loading;
        public bool IsPlaying => State == RunState.Playing;
        public PlayerController Player { get; private set; }
        public DungeonConfig Config { get; private set; }
        public ItemCatalog Catalog { get; private set; }
        public CharacterData Character { get; private set; }
        public ModuleScope Scope { get; private set; }
        public System.Random Rng { get; private set; }
        public LootSpawner Loot { get; private set; }
        public PlayPrefabSet Prefabs => prefabs;
        public DungeonBuilder Builder => builder;
        public DungeonLayout Layout { get; private set; }
        public RoomController CurrentRoom { get; private set; }
        public int Floor { get; private set; } = 1;
        public float EnemyHealthMultiplier => Config != null ? Config.enemyHealthMultiplier : 1f;

        public event Action<RoomController> RoomEntered;

        BossEncounterDirector bossEncounter;
        public BossEncounterDirector BossEncounter => bossEncounter ??= new BossEncounterDirector(this);

        void Awake() => bossEncounter = new BossEncounterDirector(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Current = null;

        void OnEnable()
        {
            EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied);
            EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            VirtualInput.Pause += OnPauseRequested;
        }

        void OnDisable()
        {
            EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
            EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            VirtualInput.Pause -= OnPauseRequested;
        }

        // ------------------------------------------------------------------ Begin

        public async Awaitable Begin(ModuleContext ctx)
        {
            Current = this;
            Scope = ctx.Scope;
            State = RunState.Loading;
            var token = destroyCancellationToken;
            var session = Services.Session;

            if (!session.runActive) EditorQuickStart(session);
            Floor = session.floor;
            Rng = new System.Random(session.seed);

            Catalog = await LoadCatalog();
            if (token.IsCancellationRequested) return;
            Config = await LoadConfig(session.themeId, Floor);
            if (token.IsCancellationRequested) return;
            if (Config == null)
            {
                Debug.LogError($"[Play] Không có DungeonConfig cho tầng {Floor}.");
                return;
            }

            Character = Catalog != null ? Catalog.GetCharacter(session.characterId) : null;
            if (Character == null) Character = prefabs != null ? prefabs.fallbackCharacter : null;

            CombatFx.Configure(prefabs != null ? prefabs.damagePopup : null, prefabs != null ? prefabs.defaultHitVfx : null);
            Loot = new LootSpawner(prefabs, Scope, Catalog, Rng, Floor);

            Layout = DungeonGenerator.Generate(session.seed, Config.ToGeneratorSettings());
            builder.Build(Layout, Config, this, prefabs);
            Prewarm();

            SpawnPlayer(session);
            if (Player == null) return;

            State = RunState.Playing;
            if (builder.StartRoom != null) OnRoomEntered(builder.StartRoom);

            if (AddressablesUtil.KeyExists(AddressKeys.UiHudPlay))
            {
                await Services.UI.Open<UIScreen>(AddressKeys.UiHudPlay);
                if (token.IsCancellationRequested) return;
            }
            else Debug.LogWarning("[Play] Chưa có address ui/hud/play — chạy không HUD.");

            Player.SetInputEnabled(true);
            bossEncounter.Preload(Config, Scope);
        }

        /// <summary>Editor: bấm Play thẳng ở Play.unity — tạo run tạm từ save.</summary>
        void EditorQuickStart(SessionData session)
        {
            var save = Services.Save?.Data;
            string id = save != null && !string.IsNullOrEmpty(save.selectedCharacterId) ? save.selectedCharacterId
                      : prefabs != null && prefabs.fallbackCharacter != null ? prefabs.fallbackCharacter.characterId : "knight";
            var loadout = save != null ? save.GetCharacter(id).loadout : new LoadoutData();
            session.StartNewRun(id, loadout, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            Debug.Log($"[Play] Quick start: nhân vật '{id}', seed {session.seed}.");
        }

        async Awaitable<ItemCatalog> LoadCatalog()
        {
            if (AddressablesUtil.KeyExists(AddressKeys.DataCatalog))
            {
                try { return await ItemCatalog.LoadAsync(Scope); }
                catch (Exception e) when (e is not OperationCanceledException) { Debug.LogException(e); }
            }
            var fb = prefabs != null ? prefabs.fallbackCatalog : null;
            if (fb != null) fb.Register();
            return fb;
        }

        async Awaitable<DungeonConfig> LoadConfig(string theme, int floor)
        {
            string address = AddressKeys.DungeonConfig(theme, floor);
            if (AddressablesUtil.KeyExists(address))
            {
                try { return await Scope.Load<DungeonConfig>(address); }
                catch (Exception e) when (e is not OperationCanceledException) { Debug.LogException(e); }
            }
            if (prefabs != null && prefabs.fallbackConfigs != null && prefabs.fallbackConfigs.Length > 0)
                return prefabs.fallbackConfigs[Mathf.Clamp(floor - 1, 0, prefabs.fallbackConfigs.Length - 1)];
            return null;
        }

        void Prewarm()
        {
            var pool = Services.Pool;
            if (prefabs != null)
            {
                if (prefabs.goldCoin != null) pool.Prewarm(prefabs.goldCoin.gameObject, Scope, prefabs.prewarmCoins);
                if (prefabs.damagePopup != null) pool.Prewarm(prefabs.damagePopup.gameObject, Scope, prefabs.prewarmPopups);
                if (prefabs.spawnMarker != null) pool.Prewarm(prefabs.spawnMarker.gameObject, Scope, 6);
            }
            foreach (var e in Config.enemyTable)
                if (e.prefab != null && e.prewarm > 0) pool.Prewarm(e.prefab.gameObject, Scope, e.prewarm);
        }

        void SpawnPlayer(SessionData session)
        {
            if (prefabs == null || prefabs.player == null)
            {
                Debug.LogError("[Play] PlayPrefabSet thiếu Player prefab.");
                return;
            }
            var start = builder.StartRoom;
            Vector3 pos = start != null ? (start.Template.playerSpawn != null ? start.Template.playerSpawn.position : start.transform.position) : Vector3.zero;
            Player = Instantiate(prefabs.player, pos, Quaternion.identity);
            Player.name = "Player";

            WeaponData w1 = null, w2 = null;
            var equips = new List<EquipmentData>();
            if (Catalog != null)
            {
                w1 = Catalog.Get<WeaponData>(session.loadout.GetWeapon(0));
                w2 = Catalog.Get<WeaponData>(session.loadout.GetWeapon(1));
                foreach (var id in session.loadout.equipment)
                    if (Catalog.Get<EquipmentData>(id) is { } eq) equips.Add(eq);
            }
            Player.Init(Character, w1, w2, equips, session.runStats);
            if (cameraRig != null) cameraRig.Follow(Player.transform);
        }

        // ------------------------------------------------------------------ Phòng

        public void OnRoomEntered(RoomController room)
        {
            if (room == null || room == CurrentRoom) return;
            CurrentRoom = room;
            if (cameraRig != null) cameraRig.SetBounds(room.CameraBounds);
            RoomEntered?.Invoke(room);
        }

        public void OnRoomCleared(RoomController room)
        {
            if (!IsPlaying || room.Type != RoomType.Combat || prefabs == null || prefabs.chest == null) return;
            if (Rng.NextDouble() >= Config.chestOnClearChance) return;
            var at = room.Template.chestPoint != null ? room.Template.chestPoint.position : room.transform.position;
            var chest = Instantiate(prefabs.chest, at, Quaternion.identity, room.transform);
            chest.Setup(this, ChestKind.Item);
        }

        // ------------------------------------------------------------------ Boss

        /// <summary>Phòng boss kích hoạt: ủy quyền cho BossEncounterDirector điều phối.</summary>
        public void SpawnBoss(RoomController room) => bossEncounter.SpawnBoss(room);

        // ------------------------------------------------------------------ Sự kiện

        void OnEnemyDied(EnemyDiedEvent e)
        {
            if (Services.Session != null) Services.Session.kills++;
            if (e.Data != null) Loot?.Drop(e.Data.lootTable, e.Position);
            bossEncounter.NotifyEnemyDied(e);
        }

        void OnBossDefeated(BossDefeatedEvent e) => bossEncounter.NotifyBossDefeated();

        void OnPlayerDied(PlayerDiedEvent e) => Finish(false);

        void Update()
        {
            bossEncounter.Tick();
            if (IsPlaying && Services.Session != null && (Services.Game == null || !Services.Game.IsPaused))
                Services.Session.runTime += Time.deltaTime;
        }

        // ------------------------------------------------------------------ Kết thúc run

        /// <summary>Chốt kết quả run. Lời gọi đầu tiên thắng; các lần sau bị bỏ qua (R10).</summary>
        public void Finish(bool victory)
        {
            if (State != RunState.Playing) return;
            State = RunState.Finished;

            var session = Services.Session;
            session.lastRunVictory = victory;
            if (victory) SnapshotRunStats(); // chụp tạm; chụp lại khi vào Portal/mở màn kết quả (ShowResults)
            if (Player != null)
            {
                Player.SetInvulnerable(this, true);
                Player.SetCombatEnabled(false);
                if (!victory) Player.SetInputEnabled(false);
            }
            Projectile.DespawnAll(Team.Enemy);
            TrapBase.DisableAll();
            foreach (var r in builder.Rooms) if (r != null) r.Abort();

            EventBus<RunFinishedEvent>.Raise(new RunFinishedEvent { Victory = victory, Floor = Floor });
            Services.Game?.SetState(victory ? GameState.Victory : GameState.GameOver);

            // SYS-04: Thua trận hoặc thắng tầng cuối cùng thì Settle() ngay để không mất thưởng.
            // Thắng tầng giữa (floor < maxFloor) thì chưa Settle() để cho phép bấm Tiếp tục tầng kế.
            if (!victory || (session != null && session.floor >= session.maxFloor))
            {
                RunFlow.Settle();
            }

            if (victory && prefabs != null && prefabs.portal != null && builder.BossRoom != null)
            {
                var t = builder.BossRoom.Template;
                Vector3 at = t.portalPoint != null ? t.portalPoint.position : t.transform.position;
                var portal = Instantiate(prefabs.portal, at, Quaternion.identity, builder.BossRoom.transform);
                portal.Setup(() => ShowResults(true, 0f));
            }
            else ShowResults(victory, resultDelay);
        }

        /// <summary>Chỉ số mang sang tầng kế (HP/giáp/mana/vũ khí đang cầm) — chụp ở thời điểm rời tầng.</summary>
        void SnapshotRunStats()
        {
            var session = Services.Session;
            if (session == null || Player == null || Player.IsDead) return;
            session.runStats = Player.Stats.Snapshot(Player.Weapons != null ? Player.Weapons.ActiveSlot : 0);
        }

        /// <summary>Pause → "Bỏ run" (về sảnh ngay, tính như thua). Trả false nếu không bỏ được lúc này (run đã chốt / đang chuyển scene).</summary>
        public bool AbandonRun()
        {
            if (State != RunState.Playing) return false;
            if (Services.Scenes == null || Services.Scenes.IsTransitioning) return false;

            Services.Game?.Resume();
            State = RunState.Finished;
            if (Services.Session != null) Services.Session.lastRunVictory = false; // không dùng kết quả của tầng trước
            if (Player != null)
            {
                Player.SetInvulnerable(this, true);
                Player.SetCombatEnabled(false);
                Player.SetInputEnabled(false);
            }
            Projectile.DespawnAll(Team.Enemy);
            TrapBase.DisableAll();
            foreach (var r in builder.Rooms) if (r != null) r.Abort();
            if (RunFlow.AbandonToLobby()) return true;

            // Không chuyển scene được (hiếm: race với transition) → trả lại quyền điều khiển, không để Player đứng im.
            Debug.LogWarning("[Play] Bỏ run thất bại — tiếp tục run.");
            State = RunState.Playing;
            if (Player != null)
            {
                Player.SetInvulnerable(this, false);
                Player.SetCombatEnabled(true);
                Player.SetInputEnabled(true);
            }
            return false;
        }

        async void ShowResults(bool victory, float delay)
        {
            var token = destroyCancellationToken;
            try { if (delay > 0f) await Awaitable.WaitForSecondsAsync(delay, token); }
            catch (OperationCanceledException) { return; }
            if (victory) SnapshotRunStats(); // chụp lại lúc vào Portal: tính cả máu/mana nhặt sau khi hạ boss
            if (Player != null) Player.SetInputEnabled(false);
            string address = victory ? AddressKeys.UiScreenVictory : AddressKeys.UiScreenDefeat;
            if (!AddressablesUtil.KeyExists(address))
            {
                Debug.LogWarning($"[Play] Chưa có {address} — về sảnh.");
                RunFlow.ReturnToLobby();
                return;
            }
            await Services.UI.Open<UIScreen>(address, victory);
        }

        async void OnPauseRequested()
        {
            if (!IsPlaying || Services.Scenes.IsTransitioning) return;
            if (Services.UI.IsOpen(AddressKeys.UiScreenPause)) return;
            if (!AddressablesUtil.KeyExists(AddressKeys.UiScreenPause))
            {
                if (Services.Game.IsPaused) Services.Game.Resume(); else Services.Game.Pause();
                return;
            }
            var screen = await Services.UI.Open<UIScreen>(AddressKeys.UiScreenPause);
            // Run có thể đã chốt (Player chết / thắng) trong lúc tải prefab Pause → đóng ngay.
            if (screen != null && (this == null || !IsPlaying)) Services.UI.Close(screen);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            VirtualInput.ResetState();
            if (Services.Game != null && Services.Game.IsPaused) Services.Game.Resume();
        }
    }
}
