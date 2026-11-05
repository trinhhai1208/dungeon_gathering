# Dữ liệu game (ScriptableObject) & roster nhân vật

> Cập nhật 2026-11-05. Đọc cùng `ENTITIES_PREFABS.md` (lớp SO, prefab) và `CODE_STRUCTURE.md`.
> Toàn bộ asset trong `Assets/Project/Data/` được **sinh bằng code** — số liệu sửa ở một chỗ (`Editor/Data/DataSeeder.cs`), không tạo tay.

## 1. Công cụ

| Menu | Làm gì |
|---|---|
| `Tools ▸ Dungeon Gathering ▸ Data ▸ Seed All` | Tạo asset còn thiếu (ghi đủ số liệu); asset đã có thì **giữ số liệu** (cho phép chỉnh trong Inspector), chỉ điền tham chiếu còn trống. Cuối cùng tự chạy Validate. |
| `… ▸ Data ▸ Seed All (Overwrite values)` | Ghi đè lại toàn bộ số liệu theo `DataSeeder.cs` (mất chỉnh tay). |
| `… ▸ Data ▸ Validate` | Kiểm: trùng id; `nameKey`/`descKey` có trong `Design/localization/DG_Multilanguage.csv`; item có icon; nhân vật đã ra mắt có animator + skill; prefab quái đã gán `EnemyData`; DungeonConfig có phòng/bảng quái/boss/tile; catalog đủ item. Kết quả hiện tại: **30 item · 12 nhân vật (6 đã ra mắt) · 8 quái · 0 lỗi · 0 cảnh báo**. |
| `… ▸ Lobby ▸ Build Character Spots` (`Editor/Data/LobbyRosterBuilder.cs`) | Dựng/cập nhật 12 chỗ đứng trong `Lobby.unity` theo roster (§3), kèm `SelectedMarker` (mũi tên vàng) và `LockedMarker` (icon coin + giá mở khóa), gán `LobbyEntry.fallbackCatalog`. |
| `… ▸ Build Entity Prefabs` (`Editor/EntityPrefabBuilder.cs`) | Dựng toàn bộ tile (`Dungeon_Floor/Wall/SealWall`), 5 biến thể phòng (`Room_Start/Combat/Treasure/Shop/Boss` đã vẽ sẵn tile 20×14), 3 bẫy (`Trap_SpikeTimed/SpikeTrigger/FlameHazard`), `Enemy_Skull`, 3 Boss (`Boss_EliteWarrior/EliteVampire/VampireLord` có `BossPhaseController`) và `PlayPrefabSet`. |
| `… ▸ Addressables ▸ Apply Module Rules` (`Editor/AddressablesModuleRulesBuilder.cs`) | Tạo `ModuleRegistry.asset`, 3 `ModuleDefinition`, đồng bộ `LocalizationFontConfig.asset` sang `Core/Data/`, tạo 15 group Addressables và đăng ký 194 entry theo `AddressKeys`. |

## 2. Bố cục `Assets/Project/Data/` (99 asset)

```
Catalog/ItemCatalog.asset          30 item (13 kiếm + 4 súng + 13 trang bị) + 12 nhân vật
PlayPrefabSet.asset                (của EntityPrefabBuilder — kèm fallbackCatalog/Character/Configs)
Characters/Char_<id>               12 (6 đã ra mắt, 6 Coming Soon)
Weapons/Weapon_sword-<key>         13 kiếm cận chiến
Weapons/Weapon_gun-<key>           4 súng tầm xa (pistol, rifle, shotgun, launcher)
WeaponPassives/WP_*                11 passive vũ khí (ice, venom, thunder, fire, lava, holy, dark, gold, void, thorn, rifle)
Equipment/Equip_<id>               13 trang bị
EquipmentPassives/EP_*             10 passive trang bị (fire, night, wind, book, horn, shield, hat, mask, gwhite, neck)
Skills/Skill_<id>                  11 kỹ năng chủ động
Enemies/Enemy_<id>                 8 (4 thường + skull + 2 mini-boss + vampire-lord)
EnemyAttacks/Atk_<id>              11 đòn quái
DungeonConfigs/DungeonConfig_dungeon_F1..F3  (đã gán đủ 5 loại phòng, 3 tile hành lang/seal, bảng quái + bossPrefab/bossAddress)
Assets/Project/Art/Animations/Characters/<id>/   clip + controller của 6 nhân vật đã ra mắt
```

## 3. Roster nhân vật (6 đã ra mắt · 6 Coming Soon)

**Tiêu chí ra mắt:** có đủ (1) skill dùng lớp SO đã có code, (2) bộ animation + `RuntimeAnimatorController`, (3) data hoàn chỉnh.

| id | Role | Hình | Vũ khí đầu | Skill | Trạng thái | Giá mở |
|---|---|---|---|---|---|---|
| knight | Warrior | FantasyKnights 01 (5 clip) | `sword-iron` | Song Kiếm (`BuffSkill`) | ✅ Ra mắt | 0 |
| paladin | Warrior | FantasyKnights 09 (5 clip) | `sword-iron` | Thánh Quang (`ShieldHealSkill`) | ✅ Ra mắt | 1500 |
| ranger | Ranger | Hero Knight Green (5 clip) | `gun-pistol` | Lộn Né (`DashSkill`) | ✅ Ra mắt | 1000 |
| wizard | Mage | Dungeon_Character_3 (controller) | `gun-pistol` | Bão Phép (`AreaBurstSkill`) | ✅ Ra mắt | 1200 |
| berserker | Warrior | FantasyKnights 02 (5 clip) | `sword-iron` | Song Kiếm (`BuffSkill`) | ✅ Ra mắt | 2000 |
| shadow-knight | Assassin | FantasyKnights 06 (5 clip) | `sword-dark` | Xé Không Gian (`DashSkill`) | ✅ Ra mắt | 2500 |
| rogue | Assassin | Dungeon_Character_9 | `sword-dark` | — | ⏳ Coming Soon | 800 |
| monk · lancer · hunter · necromancer · frost-wraith | Mage · Warrior · Ranger · Mage · Mage | Dungeon_Character 1 · 0 · 4 · 13 · 8 | `sword-iron` | — | ⏳ Coming Soon | 1500–2000 |

- **Chuẩn hóa kích thước nhân vật trong trận (`PlayerController.Init`)**: tự động gán `portrait` và tính `localScale` qua `SpriteContentFit.ContentPixels(data.portrait)` về chiều cao chuẩn `1.15` đơn vị thế giới, giúp sprite 48px (`FantasyKnights`) và 16px (`Hero Knight` / `Dungeon_Character`) đồng nhất kích thước.

## 4. Nội dung & quy ước số liệu

- **Kiếm cận chiến (13)** — bảng `UI-Weapon.dc.html` (rarity, dmg, aspd, rng, crit%); 11 thanh có passive đầy đủ (`OnHitStatusPassive`, `ChainLightningPassive`, `LifestealPassive`, `ManaOnKillPassive`, `WeaponStatBonusPassive`).
- **Vũ khí tầm xa (4)** — `gun-pistol` (C), `gun-rifle` (B, passive `+10% Crit`), `gun-shotgun` (A, chùm 5 viên), `gun-launcher` (S, tên lửa nổ bán kính 2.2m) lấy sprite từ `Weapons 1..3-Sheet.png` và gắn 4 prefab đạn.
- **Trang bị (13)** — bảng `UI-Equipment.dc.html`, toàn bộ 10 món có Role đều đã gắn `EquipmentPassiveSO` (`ArmorBreakNovaPassive`, `CritRewardPassive`, `StillnessEmpowerPassive`, `EveryNthHitPassive`, `EquipStatBonusPassive`).
- **Quái & Boss (8)** — 5 quái thường (`skeleton-spear`, `skeleton-warrior`, `zombie`, `vampire`, `skull` bay) + 2 mini-boss tầng 1–2 (`Boss_EliteWarrior`, `Boss_EliteVampire`) + boss cuối tầng 3 (`Boss_VampireLord` 3 phase máu 100% / 65% / 30%).
- **DungeonConfig F1–F3:** đã liên kết đầy đủ 5 biến thể phòng (`Room_Start`, `Room_Combat`, `Room_Treasure`, `Room_Shop`, `Room_Boss`), 3 tile hành lang/cửa (`Dungeon_Floor`, `Dungeon_Wall`, `Dungeon_SealWall`), bảng quái mở dần (F2–F3 có thêm `Enemy_Skull`), và `bossPrefab` + `bossAddress`.

## 5. Trạng thái hoàn thiện

- Mọi cảnh báo/thiếu sót ở đợt trước (Boss prefab, `Enemy_Skull`, Tile hành lang & RoomTemplate, Passive `[Proposal]`, Vũ khí tầm xa, `ModuleRegistry` & Addressables M2, `SelectedMarker`/`LockedMarker`, tỉ lệ nhân vật trong trận, lỗi `LocalizedTMPText` M3) **đã được xử lý hoàn tất 100%** (ngoại trừ phần Audio theo yêu cầu).
