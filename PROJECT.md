# Project: Dungeon Gathering

## 1. Tổng quan kiến trúc
- **Engine**: Unity 6000.3.26f1 · 2D Pixel Art Roguelike · Universal Render Pipeline (URP 2D).
- **Target**: WebGL (phát hành trên **itch.io**, template `DungeonPixel` 16:9, cap DPR 2, `SaveSync.jslib` lưu cố định trong IndexedDB).
- **9 Assembly Definitions (asmdef)**:
  - `Project.Core` (`Assets/Project/Core/`): `Services`, `ModuleScope`/`AssetService`, `SceneService`, `DownloadService`, `UIService`/`UIRoot`, `PoolManager`/`IPoolable`, `EventBus<T>`, `SessionData`, `SaveSystem`, `GameManager`, `AudioManager`, `VirtualInput`, `LoadingOverlay`, `PixelSnapCanvasScaler`, `SafeAreaFitter`, `Systems` (Bootstrap), `Localization` (`GameLanguage`, `LocalizedTMPText`, `LocalizationFontConfig`, `LocalizationLookup`).
  - `Project.Shared` (`Assets/Project/Shared/`): `ItemTile`, `VirtualJoystick`, `VirtualButton`, `SpriteContentFit`, `SettingsPanel`, `SettingsPopup`.
  - `Project.Catalog` (`Assets/Project/Scripts/Catalog/`): `ItemData`, `WeaponData`, `EquipmentData`, `CharacterData`, `ActiveSkillSO`, `WeaponPassiveSO`, `EquipmentPassiveSO`, `StatSheet`, `ItemCatalog`, `LoadoutRules`, `Economy`, `LootTable`.
  - `Project.Combat` (`Assets/Project/Scripts/Combat/`): `WeaponBase`, `MeleeWeapon`, `RangedWeapon`, `WeaponHolder`, `Projectile`, `StatusEffectController`, `CombatFx`, `DamagePopup`, `SpriteAnimDriver`, `AutoDestroyEffect`, 4 lớp `ActiveSkillSO`, 10 lớp Passive.
  - `Project.Enemy` (`Assets/Project/Scripts/Enemy/`): `Enemy`, `EnemyHealth`, `EnemyMotor`, `EnemyBrain` (FSM), `EnemyAttackRunner`, `EnemyData`, `EnemyAttackSO` (Melee/Shoot/Charge/Summon), `EnemyNavigation` (NavMesh — AI Navigation), `BossPhaseController`.
  - `Project.Menu` (`Assets/Project/Scripts/Menu/`): `MenuEntry`, `MenuScreen`.
  - `Project.Lobby` (`Assets/Project/Scripts/Lobby/`): `LobbyEntry`, `CharacterSpot`, `LobbyPortal`, `MerchantNpc`, `LobbyHud`, `CharacterPanel`, `EquipmentScreen`, `WeaponScreen`.
  - `Project.Play` (`Assets/Project/Scripts/Play/`): `LevelDirector`, `PlayPrefabSet`, `CameraRig`, `PlayerController`/`PlayerStats`/`PlayerMovement`/`PlayerEquipment`/`PlayerSkillController`/`PlayerInteractor`, `DungeonGenerator`/`DungeonBuilder`/`DungeonNavMesh`/`RoomTemplate`/`RoomController`/`EnemySpawner`/`RoomDoor`/`SpikeTrap`/`DamageZone`, `LootDrop`/`ItemPickup`/`Chest`/`ShopStand`/`Portal`, `PlayHud`, `PauseScreen`, `ShopScreen`.
  - `Project.Results` (`Assets/Project/Scripts/Results/`): `RunResultScreen` (Victory & Defeat).

---

## 2. Trạng thái hoàn thiện các Milestone

| # | Milestone | Nội dung chính | Trạng thái |
|---|---|---|---|
| M1 | Foundations: Layers, Projectiles & Loot Core | 9 Physics2D layers, Sorting Layers, `ItemPickup`, `LootDrop`, 4 projectile prefabs | DONE |
| M2 | Weapon, Socket & Addressables Rules | `BaseWeapon_Melee`, `BaseWeapon_Ranged`, `WeaponHolder`, 101 VFX prefabs, `AddressablesModuleRulesBuilder` (15 group, 194 entry, `ModuleRegistry`) | DONE |
| M3 | Localization & Bootstrap/Menu | 300 key EN/VI (`DG_Multilanguage.csv`), font pixel `PixelifySans`/`Tiny5`, sửa 3 lỗi `LocalizedTMPText` & `GameLanguage`, `MenuScreen` + `SettingsPopup` | DONE |
| M4 | Lobby, Loadout & Shop | `Lobby.unity` (12 chỗ đứng nhân vật, 6 đã ra mắt phủ đủ 4 Role + 6 Coming Soon, `SelectedMarker`/`LockedMarker`), `CharacterPanel`, `EquipmentScreen`, `WeaponScreen`, `ShopScreen` | DONE |
| M5 | Play HUD, SafeArea & Dungeon/Boss Content | `PlayHud` (Attack, Swap, Character Skill Slot 0, 2 Item Active Skills, Interact button, Minimap động), `SafeAreaFitter` trên `UIRoot`, 5 biến thể phòng vẽ sẵn tile 20×14, 3 tile hành lang/cửa, 3 bẫy, `Enemy_Skull`, 3 Boss (`Boss_EliteWarrior`, `Boss_EliteVampire`, `Boss_VampireLord` 3 phase), 30 item (13 kiếm + 4 súng + 13 trang bị) đủ passive | DONE |
| M6 | WebGL Template & Linker | `Assets/WebGLTemplates/DungeonPixel/index.html`, `Assets/Project/Core/link.xml`, `SaveSync.jslib` | DONE |

---

## 3. Công cụ Editor (`Tools ▸ Dungeon Gathering`)
- `Build Entity Prefabs`: Tạo/chuẩn hóa toàn bộ tile, 5 biến thể phòng 20×14, 3 bẫy, `Player`, 5 quái thường, 3 Boss, đạn, loot và `PlayPrefabSet`.
- `Data ▸ Seed All (Overwrite values)` & `Data ▸ Validate`: Sinh 99 ScriptableObject dữ liệu trong `Assets/Project/Data/` và kiểm chứng (**30 item · 12 nhân vật · 8 quái · 0 lỗi · 0 cảnh báo**).
- `UI ▸ Build Design UI (...)`: Dựng lại toàn bộ prefab UI chuẩn thiết kế (`MenuScreen`, `LobbyHud`, `CharacterPanel`, `PlayHud`, `PauseScreen`, `EquipmentScreen`, `WeaponScreen`, `ShopScreen`, `SettingsPopup`, `VictoryScreen`, `DefeatScreen`, `UIRoot` kèm `SafeArea`).
- `Lobby ▸ Build Character Spots`: Dựng 12 vị trí nhân vật trong `Lobby.unity` kèm `SelectedMarker`, `LockedMarker`, `ComingSoonMarker`.
- `Addressables ▸ Apply Module Rules`: Tạo `ModuleRegistry.asset`, 15 group Addressables và đăng ký 194 entry theo `AddressKeys`.
- `Localization ▸ Import CSV` & `Build Font Assets`: Cập nhật bảng chuỗi `DG_Multilanguage` (300 key) và font pixel EN/VI.
