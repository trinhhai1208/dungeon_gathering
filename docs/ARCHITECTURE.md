# Kiến trúc Dungeon Gathering (v2)

Game roguelike pixel 2D, Unity 6000.3, URP 2D, phát hành WebGL trên itch.io. Tài liệu này mô tả kiến trúc sau đợt tái
cấu trúc (giai đoạn 0–11, đã xong). Danh mục chi tiết từng ScriptableObject, số liệu hiện tại và việc còn lại: xem
[`HANDOFF.md`](HANDOFF.md). Hạ tầng module / Addressables / quy tắc async: `MODULES_ADDRESSABLES_WEBGL.md`; UI:
`UI_SYSTEM.md`; art: `ART_PIPELINE.md`.

## Nguyên tắc

1. **Định nghĩa + ghi đè.** Mỗi hệ có một ScriptableObject gốc mang giá trị mặc định. Nơi dùng giữ tham chiếu tới SO đó
   cùng các tham số ghi đè tùy chọn (`Overridable<T>`: checkbox + giá trị; bỏ tick = dùng giá trị gốc).
2. **Runtime không ghi vào SO.** Giá trị đã resolve nằm trong struct/class runtime (`WeaponSpec`, `StatSheet`,
   `StatusInstance`…).
3. **Hero và quái dùng chung lõi**: chỉ số (`ActorVitals` + `StatSheet`), hiệu ứng trạng thái (`StatusEffectSet`), state
   machine (`StateMachine<T>`), đường sát thương (`DamageInfo`), skill (`SkillRegistry`).
4. **Logic thuần C# tách khỏi MonoBehaviour** để có EditMode test (`Assets/Project/Tests/EditMode`, asmdef
   `Project.Tests.EditMode`, 61 test).
5. **Luồng chơi đi qua sự kiện** (`EventBus<T>`, khai báo ở `Core/Scripts/EventBus/GameEvents.cs`).
6. **SO và prefab là nguồn dữ liệu chính, sửa tay trong Inspector.** Không còn builder / seeder / migration nào dựng lại
   dữ liệu (đã xóa ở giai đoạn 11). Sau khi sửa dữ liệu: chạy `Tools ▸ Dungeon Gathering ▸ Validate All`.

## Bản đồ hệ thống

| Hệ | SO cấu hình | Runtime | Code |
|---|---|---|---|
| Chỉ số | `StatProfileSO`, `StatBlock` (profile + ghi đè), `MonsterTierConfigSO` | `ActorVitals`, `StatSheet` | `Scripts/Catalog/Stats/`, `Scripts/Catalog/Stats.cs` |
| Hiệu ứng trạng thái | `StatusEffectSO` → `StunEffectSO`, `SlowEffectSO`, `DamageOverTimeEffectSO`, `StatModifierEffectSO`; `StatusEffectRef` | `StatusEffectSet`, `StatusEffectController` | `Scripts/Catalog/Status/`, `Scripts/Combat/StatusEffectController.cs` |
| State | — | `StateMachine<T>`, `StunnedState<T>`, `DeadState<T>` | `Scripts/Combat/States/`, `Play/Player/PlayerStateMachine.cs`, `Enemy/EnemyBrain.cs` |
| Vũ khí | `SwordBaseSO`, `RangedBaseSO`, `SwordLibrarySO`, `RangedLibrarySO`, `ProjectileLibrarySO`, `WeaponData` (định danh) | `WeaponLibrary`, `WeaponSpec`, `WeaponHolder` | `Scripts/Catalog/Weapons/`, `Scripts/Combat/{WeaponBase,MeleeWeapon,RangedWeapon,WeaponHolder}.cs` |
| Skill | `SkillBaseSO` → `ActiveSkillSO` (hero) / `EnemyAttackSO` (quái); `SkillLibrarySO` | `SkillRegistry`, enum `SkillId` | `Scripts/Catalog/Skills/` |
| Hero / quái | `HeroDefinitionSO`, `MonsterDefinitionSO` | `PlayerController`, `Enemy` (prefab nền `Enemy_Base`) | `Scripts/Catalog/HeroDefinitionSO.cs`, `Scripts/Enemy/` |
| Rơi đồ | `LootConfigSO` | `DropResolver`, `LootSpawner` | `Scripts/Catalog/Loot/`, `Scripts/Play/Interaction/LootSpawner.cs` |
| Level / map | `CampaignSO`, `LevelConfigSO`, `RoomTypeConfigSO` | `DungeonGenerator`, `EnemySpawner`, `LevelDirector` | `Scripts/Play/Dungeon/`, `Scripts/Catalog/Levels/` |
| Inventory | `ItemCatalog`, `IItemDefinition`, `EquipmentSO`, `EquipmentSetSO`, `RunBuffSO` | `InventoryModel`, `LoadoutRules` | `Scripts/Catalog/Inventory/`, `Scripts/Catalog/ItemData.cs` |
| Bẫy | `TrapDataSO` | `TrapBase` (`TimedTrap`, `TriggerTrap`), `HazardZone` | `Scripts/Play/Dungeon/Traps/` |

## Chỉ số

- `StatProfileSO`: bảng `BaseStats` gốc (maxHp, maxArmor, maxMana, attackDamage, attackSpeed, moveSpeed, critChance,
  critDamage, cooldownReduction, manaRegen, knockbackResist) + hồi giáp.
- `StatBlock` = profile + danh sách `StatOverride { StatType, value }`; `Resolve(fallback)` không ghi vào SO. Không chọn
  profile → hero dùng `BaseStats.Default`, quái dùng `MonsterDefinitionSO.MonsterDefault`.
- `ActorVitals` (thuần C#): HP / giáp / mana + `StatSheet` (công thức `(base + ΣFlat) × (1 + ΣPct)`), `BeginBatch/EndBatch`
  khi cộng hàng loạt modifier. Player (`PlayerStats`) và quái (`EnemyHealth`) cùng bọc lớp này.
- `MonsterTierConfigSO`: hệ số Normal / Elite / Boss (HP, sát thương, tốc chạy, kháng đẩy lùi, scale, màu) cộng vào
  `StatSheet` như một nguồn modifier. Kích thước quái = `definition.visualScale × tier.scale`.
- Quái: tốc chạy = `Stats.Get(MoveSpeed)`, mọi đòn nhân `Enemy.ScaleDamage()` (AttackDamage là hệ số, mặc định 1).

## Hiệu ứng trạng thái

- Lớp gốc `StatusEffectSO` (id, nameKey, icon, thời gian / độ mạnh mặc định, `StackPolicy` Refresh / Extend / Stack /
  KeepStrongest, tags, tint, loopVfx). Lớp con: Stun (bật `freeze` = đóng băng), Slow (magnitude = % giảm tốc), DoT
  (magnitude = dps, `tickInterval`), StatModifier (buff / debuff bất kỳ chỉ số).
- **`StatusEffectRef`** dùng ở mọi nơi gây hiệu ứng: `effect` + ghi đè `duration` / `magnitude` + `chance` + VFX lặp
  riêng. Ví dụ skill A gán `Status_Stun` ghi đè 1.5s, skill B gán `Status_Stun` ghi đè 3s.
- `StatusEffectController` (component trên hero / quái) chỉ có `Apply(StatusEffectRef, nguồn)`, `Apply(effect, dur, mag,
  nguồn)`, `Remove`, `ClearAll`, `IsStunned`, `IsFrozen`, `SpeedMultiplier`.
- `StatusPayload` (dùng trong skill, đòn quái, bẫy) chỉ còn `List<StatusEffectRef> effects`.
- Sát thương DoT mang cờ `DamageFlags.DoT`: bỏ qua i-frame, không đặt lại i-frame, không giật / đẩy lùi.

## State machine

- `StateMachine<TOwner>`: đăng ký trạng thái theo `ActorStateId`, chuyển tường minh, luật toàn cục theo thứ tự ưu tiên
  (`AddGlobalRule(điều kiện, đích, nguồn)`), Dead là trạng thái cuối (chỉ `Reset` thoát).
- Player: Spawn · Active · Dash · Sealed · Stunned · Dead. Skill chỉ dùng được ở Active / Dash; pause chặn đánh và skill.
- Quái: Spawn · Idle · Chase · Attack · Stunned · Dead. Bị tường che tầm nhìn thì tiến lại gần thay vì đứng im.

## Vũ khí

```
WeaponBaseSO (abstract: damage, fireRate, range, knockback, crit, mana, cầm, VFX trúng, SFX, onHitStatus)
 ├─ SwordBaseSO  : kiểu đánh (Swing / Thrust / Spin), cung, thời gian vung, hitbox, vệt chém, kiếm khí, chặn đạn
 └─ RangedBaseSO : kiểu bắn, độ tỏa, số viên, loạt, giật nòng, muzzle VFX, đạn mặc định
SwordLibrarySO  = SwordBaseSO + List<SwordEntry>
RangedLibrarySO = RangedBaseSO + List<RangedEntry>
ProjectileLibrarySO = List<ProjectileEntry>
```
- **Entry là nguồn duy nhất** của một vũ khí: tên, mô tả, bậc, icon, giá, sprite, holdOffset, hitbox (Auto / Arc / Box,
  offset, size, arc), đạn theo id, VFX, hiệu ứng khi trúng, passive, skill kích hoạt; mọi chỉ số là `Overridable<>`.
- `WeaponData` chỉ còn là asset định danh (`id`) để catalog, save, shop, loot tham chiếu. `WeaponData.Spec` = SO gốc →
  đạn → entry (cache trong `WeaponLibrary`). Không có entry trùng id → cảnh báo + spec tối thiểu.
- Inspector Sword / Ranged Library có ô xem trước sprite + hitbox + thông số đã resolve.
- **VFX vũ khí**: một đòn chỉ có VFX của chính vũ khí. Kiếm chỉ có vệt chém (không VFX trúng); súng có lửa nòng + đạn + VFX
  khi đạn trúng. Màu nguyên tố do VFX lặp của hiệu ứng trạng thái trên mục tiêu, hiệu ứng lớn do skill / passive khi kích
  hoạt.

## Skill

```
SkillBaseSO (skillId, nameKey, descKey, icon, cooldown)
 ├─ ActiveSkillSO → DashSkill, AreaBurstSkill, ProjectileVolleySkill, BuffSkill, ShieldHealSkill, SummonCompanionSkill
 └─ EnemyAttackSO → MeleeAttackSO, ShootAttackSO, ChargeAttackSO, AreaTelegraphAttackSO, SummonAttackSO, TeleportAttackSO
SkillLibrarySO: List<SkillEntry { key, value (số enum cố định), skill (SO hành vi), ghi đè cooldown / mana, castVfx, castSfx }>
```
- Hero (`HeroDefinitionSO.skill`), quái (`MonsterDefinitionSO.skills`), phase boss (`Phase.skills`) **chỉ chọn enum
  `SkillId`**. Runtime tra qua `SkillRegistry.Get<T>(id)`, `Cooldown(skill)`, `ManaCost(skill)`.
- Thêm skill: tạo SO hành vi → thêm entry vào `Data/Skills/SkillLibrary.asset` → `Tools ▸ Dungeon Gathering ▸ Skills ▸
  Generate SkillId` → chọn enum. Số của skill cũ không đổi. Không sửa tay `Scripts/Catalog/Skills/SkillId.cs`.
- `SummonAttackSO.minion` là một `MonsterDefinitionSO`; quái con sinh từ prefab nền như quái thường.

## Hero / quái

- `HeroDefinitionSO`: định danh, role, bậc D–S (hiển thị), giá mở khóa, portrait, animator, `attackVfx` / `hitVfx`
  (tùy chọn, chồng lên VFX vũ khí mỗi đòn — hiện để trống ở mọi hero), `StatBlock`, vũ khí khởi đầu, `skill : SkillId`, nội tại.
- `MonsterDefinitionSO`: định danh, `tier`, animator, sprite, visualScale, `attackVfx`, `hitVfx` (phát trên Player khi đòn
  trúng, qua `IHitVfxSource`), sát thương chạm, di chuyển, `StatBlock`, chi phí sinh, `skills : SkillId[]`, `phases`
  (boss), `customLoot` + `lootTable` (bảng rơi riêng, tùy chọn).
- **Quái thường không có prefab riêng**: `PlayPrefabSet.monsterBase` = `Prefabs/Enemies/Enemy_Base.prefab`; spawner lấy
  prefab nền từ pool rồi `Enemy.Init(ctx, định nghĩa)` áp hình / chỉ số / skill. Boss có prefab riêng `Boss_*`
  (có `BossPhaseController`), nạp qua Addressables theo `LevelConfigSO.bossAddress`.
- Elite = cùng định nghĩa + tier Elite (spawner tung theo `eliteChance` của level + `eliteChanceBonus` của loại phòng).

## Rơi đồ

- Bậc vật phẩm và hero: `Rarity { S, A, B, C, D }`. Bậc quái: `MonsterTier { Normal, Elite, Boss }`.
- `LootConfigSO`: trọng số bậc D–S theo level (vượt danh sách thì dời thêm `rankShiftPerExtraLevel`), vàng
  `+goldPerLevel`, theo tier quái (`TierLoot`: vàng, % vật phẩm, số món chắc chắn, mana, bình máu, `rankShift`), rương
  (`chestRankShift`).
- Bậc một món = trọng số level, dời theo `tier.rankShift + level.lootLuck`. `ItemCatalog.PickItem(bậc)` chọn món đúng
  bậc, thiếu thì lùi bậc thấp rồi mới lên bậc cao.
- `LootSpawner` chỉ sinh object từ pool: `DropFromMonster`, `DropChestItem`, `RollShopItem`.

## Level / map

- `CampaignSO`: danh sách address của level theo thứ tự; số tầng của run = số level.
- `LevelConfigSO`: `combatRoomCount` (min, max), `specialRooms` (loại + số lượng), `roomTypes`, `eliteChance`,
  `lootLuck`, lưới, tile hành lang, bảng quái (`EnemySpawnEntry { monster, weight, prewarm }`), ngân sách sinh, số đợt,
  hệ số HP quái, boss.
- `RoomTypeConfigSO`: template phòng, số đợt / ngân sách riêng (0 = theo level), cộng tỉ lệ Elite, tỉ lệ rương (-1 = theo
  level). `RoomType { Start, Combat, Treasure, Shop, Boss, Elite }`.
- `DungeonGenerator`: Start → phòng Combat → Boss trên đường chính, mỗi phòng đặc biệt ở cuối một nhánh. Luôn đúng số
  phòng; hết 64 lần thử thì dựng bố cục dự phòng (vẫn đúng số phòng).
- Portal: `Portal` chỉ phát `LevelExitRequestedEvent { NextLevel | Lobby }`, `LevelDirector` nghe và chuyển. `LobbyPortal`
  phát `RunStartRequestedEvent`, `LobbyEntry` nghe và vào run. Chỉ collider thân Player kích hoạt.
- HUD: `hud.floor` = `FLOOR {0}` / `TẦNG {0}`.

## Inventory và save

- `IItemDefinition` (`Id`, `NameKey`, `DescKey`, `Rank`, `Icon`, `Price`, `Kind`, `ActiveSkill`): mọi UI / shop / loot /
  kho đọc qua interface. Vũ khí lấy từ entry thư viện, trang bị từ `EquipmentSO`.
- `EquipmentSO`: slot, modifier, synergy theo role, set, `onHitStatus` (hiệu ứng gây lên mục tiêu khi đòn trúng).
- `InventoryModel`: chỉ xem không ghi save; thay đổi qua `LoadoutRules`; chặn hero chưa mở khóa và đồ chưa sở hữu.
- Save v2 (`SaveData.CurrentVersion = 2`): save cũ bị reset tiến trình nhưng giữ cài đặt; save bản mới hơn chỉ đọc.

## Đa ngôn ngữ

- Nguồn bảng: `Design/localization/DG_Multilanguage.csv` (cột `Key,en,vi`) → `Tools ▸ Dungeon Gathering ▸ Localization ▸
  Import CSV`.
- **Không chạy `Design/localization/strings_src.py`**: bản cũ, thiếu ~60 key, sẽ ghi đè CSV.
- `Localization ▸ Validate Keys` đối chiếu key trong prefab, scene, data SO và code; `Validate All` gọi lại bước này.
- Text tạo trong code: `LocalizationLookup.Get(key, fallback, args)`; tên role: `LocalizationLookup.Role(role)`.

## Công cụ Editor

| Menu | Việc |
|---|---|
| `Tools ▸ Dungeon Gathering ▸ Validate All` | Kiểm tra toàn bộ dữ liệu (key, id trùng, vũ khí thiếu entry, đạn thiếu, skill thiếu enum / SO, hero / quái thiếu profile / skill / animator, level thiếu template / sinh sai số phòng, PlayPrefabSet). Code: `Editor/Validation/ValidateAll.cs` |
| `Tools ▸ Dungeon Gathering ▸ Skills ▸ Generate SkillId` | Sinh enum `SkillId` từ SkillLibrary |
| `Tools ▸ Dungeon Gathering ▸ Debug ▸ Mở full hero + vũ khí (Editor)` | Bật / tắt chế độ test: mọi hero mở, mọi vũ khí / trang bị sở hữu, 999.999 coin. Chỉ Editor, dùng save riêng `save_editor_unlockall.json` (save thật không bị đụng); `Debug ▸ Xóa save mở full` để làm lại từ đầu. Code: `Core/Scripts/Save/EditorUnlockAll.cs` |
| `Tools ▸ Dungeon Gathering ▸ Docs ▸ Generate SO Reference` | Sinh [`SO_REFERENCE.md`](SO_REFERENCE.md): mọi lớp SO, từng field (kiểu, mặc định, ý nghĩa từ `[Tooltip]` / `///`), kiểu nhúng, enum, asset, nơi tham chiếu. Chạy lại sau khi sửa SO. Code: `Editor/Docs/SOReferenceGenerator.cs` |
| `Tools ▸ Dungeon Gathering ▸ Localization ▸ Import CSV` / `Validate Keys` | Bảng đa ngôn ngữ |
| `Addressables ▸ Apply Module Rules` | Gán group / label Addressables (level đăng ký theo LevelConfigSO, boss theo `Boss_*`) |

## Lịch sử giai đoạn

| GĐ | Nội dung | Trạng thái |
|---|---|---|
| 0 | Dọn docs cũ, tắt builder ghi đè, sửa hiển thị tầng, bổ sung key đa ngôn ngữ | Xong |
| 1 | Hệ chỉ số dùng chung | Xong |
| 2 | Hiệu ứng trạng thái | Xong |
| 3 | State machine dùng chung | Xong |
| 4 | Vũ khí base → sword / ranged + thư viện | Xong |
| 5 | Skill: thư viện + enum `SkillId` | Xong |
| 6 | Định nghĩa hero / quái, prefab quái nền | Xong |
| 7 | Rơi đồ + bậc D–S | Xong |
| 8 | Level config, room type, campaign, Portal theo sự kiện | Xong |
| 9 | Inventory, Save v2 | Xong |
| 10 | Hạ tầng UI / scene / pool / save / input | Xong |
| 11 | Gỡ hệ cũ: xóa `Editor/Legacy`, `Editor/Migration`, 11 prefab quái thường, field / API cũ; thêm `Validate All` | Xong |
