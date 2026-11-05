# Cấu trúc code & hợp đồng component cho prefab

> Cập nhật 2026-11-05. Đọc cùng `MODULES_ADDRESSABLES_WEBGL.md` (service, R1–R14) và `ENTITIES_PREFABS.md` (cây prefab).

## 1. Assembly & thư mục

| asmdef | Thư mục | Nội dung chính |
|---|---|---|
| `Project.Core` | `Assets/Project/Core/` | `Services`, `ModuleScope`/`AssetService`, `SceneService`, `DownloadService`, `UIService`/`UIScreen`/`UIRoot`, `PoolManager`/`IPoolable`, `EventBus` + `GameEvents`, `SessionData`, `SaveSystem` (+ `Plugins/WebGL/SaveSync.jslib`), `GameManager`, `AudioManager`, `RunFlow`, `VirtualInput`, `IInteractable`, `LoadingOverlay`, `PixelSnapCanvasScaler`, `SafeAreaFitter`, `Systems` (Bootstrap), Localization (`GameLanguage`, `LocalizedTMPText`, `LocalizationFontConfig`, `LocalizationLookup`), `link.xml`, enum chung (`Rarity`, `CharacterRole`, `EquipmentSlotType`, `StatType`, `Team`) |
| `Project.Shared` | `Assets/Project/Shared/` | `ItemTile`, `VirtualJoystick`, `VirtualButton`, `SpriteContentFit`, `SettingsPanel`, `SettingsPopup` |
| `Project.Catalog` | `Scripts/Catalog/` | `ItemData`/`WeaponData`/`EquipmentData`/`CharacterData`, `ActiveSkillSO`, `WeaponPassiveSO`/`EquipmentPassiveSO`, `IPlayerContext`, `StatSheet`/`StatModifier`, `ItemCatalog`, `LoadoutRules`, `Economy` (mua/mở khóa/bắt đầu run — idempotent), `LootTable` |
| `Project.Combat` | `Scripts/Combat/` (+ `Skills/`, `Passives/`) | `IDamageable`/`DamageInfo`, `CombatLayers`, `CombatUtil`, `Projectile`, `WeaponHolder`/`MeleeWeapon`/`RangedWeapon`, `StatusEffectController`, `CombatFx`/`DamagePopup`, `SpriteAnimDriver`, `AutoDestroyEffect` (hỗ trợ pool), kỹ năng (`AreaBurstSkill`, `DashSkill`, `ShieldHealSkill`, `BuffSkill`), 10 lớp passive vũ khí & trang bị |
| `Project.Enemy` | `Scripts/Enemy/` (+ `Attacks/`) | `Enemy`, `EnemyHealth`, `EnemyMotor`, `EnemyBrain` (FSM), `EnemyAttackRunner`, `EnemyData`, `EnemyAttackSO` (+ Melee/Shoot/Charge/Summon), `EnemyNavigation` (NavMesh), `BossPhaseController`, `SpawnWarningMarker` |
| `Project.Play` | `Scripts/Play/` (+ `Player/`, `Dungeon/`, `Interaction/`, `UI/`) | `LevelDirector` (IModuleEntry), `PlayPrefabSet`, `CameraRig`, Player (`PlayerController`, `PlayerStats`, `PlayerMovement`, `PlayerInputReader`, `PlayerEquipment`, `PlayerSkillController`, `PlayerInteractor`), Dungeon (`DungeonGenerator`, `DungeonBuilder`, `DungeonConfig`, `RoomTemplate`, `RoomController`, `EnemySpawner`, `RoomDoor`, `RoomTriggerRelay`, `SpikeTrap`, `DamageZone`), Interaction (`LootDrop`, `ItemPickup`, `LootSpawner`, `Chest`, `ShopStand`, `Portal`), UI (`PlayHud`, `PauseScreen`, `ShopScreen`) |
| `Project.Menu` | `Scripts/Menu/` | `MenuEntry`, `MenuScreen` |
| `Project.Lobby` | `Scripts/Lobby/` (+ `Loadout/`) | `LobbyEntry`, `CharacterSpot`, `LobbyPortal`, `MerchantNpc`, `LobbyHud`, `CharacterPanel`, `LoadoutScreenBase`, `EquipmentScreen`, `WeaponScreen` |
| `Project.Results` | `Scripts/Results/` | `RunResultScreen` (dùng cho cả Victory và Defeat, cờ `isVictory`) |

Đường dẫn trong cột "Thư mục" tính từ `Assets/Project/`. Mỗi module code = 1 thư mục `Assets/Project/Scripts/<Module>/` chứa asmdef + script trực tiếp. Tên thư mục `Enemy` khớp asmdef `Project.Enemy` (namespace `DungeonGathering.Enemies`).

Editor script (`Assets/Project/Editor/`) nằm ở `Assembly-CSharp-Editor`: `EntityPrefabBuilder`, `DataSeeder`, `LobbyRosterBuilder`, `UIDesignBuilder`, `AddressablesModuleRulesBuilder`, `LocalizationCsvImporter`, `LocalizationFontBuilder`, `PlayCameraRigSetup`, `UIPreviewRenderer`, `VFXProcessor`.

## 2. Layer & ma trận va chạm 2D

`Player`(8) `Enemy`(9) `PlayerHurtbox`(10) `EnemyHurtbox`(11) `PlayerProjectile`(12) `EnemyProjectile`(13) `Wall`(14) `Pickup`(15) `Trap`(16).
Bật: Player↔Enemy/Wall/Pickup/Trap/EnemyProjectile · Enemy↔Enemy/Wall/Trap/PlayerProjectile · PlayerHurtbox↔EnemyProjectile/Trap · EnemyHurtbox↔PlayerProjectile · Projectile↔Wall. Các cặp khác giữa 9 layer này tắt.
Tilemap tường ở layer `Wall` (Auto-Aim, đạn dùng nó; bị trừ khỏi NavMesh của quái).

## 3. Component bắt buộc theo prefab

| Prefab | Root | Ghi chú |
|---|---|---|
| `Systems` (Bootstrap) | `Systems` (+ con `UIRoot`, `LoadingOverlay`, `AudioManager`) | Đã gán `ModuleRegistry` (`Core/Data/ModuleRegistry.asset`). Bootstrap là scene duy nhất trong Build Settings. |
| `UIRoot` | `Canvas` overlay + `PixelSnapCanvasScaler` + `UIRoot` | Có `SafeArea` (`SafeAreaFitter`) bọc `Hud`, `Screen`, `Panel`, `Popup`, `Toast`; `Backdrop`, `Overlay`, `InputBlocker` nằm ngoài `SafeArea`. |
| Mọi screen | component kế thừa `UIScreen` | Đặt `layer` + `releaseOnClose` trong Inspector. HUD (`PlayHud`, `LobbyHud`): layer **Hud**, `closeOnBack = false`. |
| `Player` | `Rigidbody2D` + `CapsuleCollider2D` (layer Player) + `PlayerController` (kéo theo `PlayerStats`, `PlayerMovement`, `PlayerInputReader`) + `StatusEffectController` | Con `WeaponPivot` có `WeaponHolder`. `Visual`: `Animator` + `SpriteRenderer` + `SpriteAnimDriver`, tự chuẩn hóa scale theo chiều cao nhân vật (`1.15` đơn vị). |
| Quái `Enemy_<Name>` & `Boss_<Name>` | `Rigidbody2D` + collider (layer Enemy) + `Enemy` (kéo theo Health/Motor/Brain/AttackRunner) + `StatusEffectController` | Đã gán `EnemyData`. Boss (`Boss_EliteWarrior`, `Boss_EliteVampire`, `Boss_VampireLord`) có `BossPhaseController` và `isBoss = true`. |
| Đạn | `Projectile` + `Collider2D` trigger + `Rigidbody2D` Kinematic | Layer tự đặt theo phe khi bắn. |
| VFX one-shot | `AutoDestroyEffect` (trên 101 prefab) | Tự trả về `PoolManager`. |
| Phòng (`Room_Start/Combat/Treasure/Shop/Boss`) | `RoomTemplate` (gốc = tâm phòng) | Đã vẽ sẵn Tilemap `Floor` + `Wall` (20×14), 4 door socket + `RoomDoor`, điểm spawn quái, bẫy, `Chest`, `ShopStand`, `Portal`. |
| Bẫy (`Trap_SpikeTimed/SpikeTrigger/FlameHazard`) | `SpikeTrap` / `DamageZone` + `BoxCollider2D` trigger (layer Trap) | Đặt sẵn trong phòng hoặc sinh động. |
| `PlayHud` | `PlayHud` | Gồm cụm máu/giáp/mana, coin, tầng, Minimap động theo phòng, nút Attack, Swap Weapon, Character Skill (Slot 0), 2 Item Active Skill (Slot 1–2), nút Interact ngữ cảnh và `VirtualJoystick`. |

## 4. Trạng thái hoàn thiện

- Đã dọn toàn bộ file dư thừa (`Prefabs/UI/HUD.prefab`, `Prefabs/UI/PauseMenu.prefab`, `Scripts/Play/UI/HudScreen.cs`, `Scripts/Play/UI/VirtualJoystick.cs`, `Project.Tests.csproj`).
- Đã hoàn thiện `AddressablesModuleRulesBuilder` (15 group, 194 entry, `ModuleRegistry`), sửa 3 lỗi `LocalizedTMPText` + `GameLanguage` (M3), thêm WebGL template `DungeonPixel` và `link.xml` (M9).
