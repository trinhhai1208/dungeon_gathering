# Tham chiếu ScriptableObject — Dungeon Gathering

> **File sinh tự động** bởi `Tools ▸ Dungeon Gathering ▸ Docs ▸ Generate SO Reference`
> (`Assets/Project/Editor/Docs/SOReferenceGenerator.cs`). Đừng sửa tay — sửa chú thích `///` / `[Tooltip]` trong
> code rồi chạy lại. Tổng quan kiến trúc: [`ARCHITECTURE.md`](ARCHITECTURE.md); số liệu đang cấu hình và việc còn
> lại: [`HANDOFF.md`](HANDOFF.md).

**62 lớp SO** · **193 asset**. Cập nhật: 2026-11-17 14:42.

Cách đọc:
- **Mặc định** = giá trị khi tạo asset mới (`Create ▸ Dungeon Gathering ▸ …`), không phải giá trị trong asset đang có.
- `Overridable<T>`: checkbox + giá trị. Bỏ tick = dùng giá trị của SO gốc; tick = dùng giá trị này.
- *(abstract)*: lớp gốc, không tạo asset trực tiếp — field của nó có trong mọi lớp con.
- Runtime **không bao giờ ghi vào SO**; sau khi sửa dữ liệu chạy `Tools ▸ Dungeon Gathering ▸ Validate All`.

## Mục lục

- **Gốc nối dữ liệu**: [ItemCatalog](#itemcatalog), [PlayPrefabSet](#playprefabset)
- **Chỉ số**: [MonsterTierConfigSO](#monstertierconfigso), [StatProfileSO](#statprofileso)
- **Hiệu ứng trạng thái**: [StatusEffectSO](#statuseffectso), [DamageOverTimeEffectSO](#damageovertimeeffectso), [SlowEffectSO](#sloweffectso), [StatModifierEffectSO](#statmodifiereffectso), [StunEffectSO](#stuneffectso)
- **Vũ khí**: [ProjectileLibrarySO](#projectilelibraryso), [RangedLibrarySO](#rangedlibraryso), [SwordLibrarySO](#swordlibraryso), [WeaponBaseSO](#weaponbaseso), [RangedBaseSO](#rangedbaseso), [SwordBaseSO](#swordbaseso), [WeaponData](#weapondata)
- **Skill**: [SkillBaseSO](#skillbaseso), [SkillLibrarySO](#skilllibraryso), [ActiveSkillSO](#activeskillso), [EnemyAttackSO](#enemyattackso), [AreaBurstSkill](#areaburstskill), [AreaTelegraphAttackSO](#areatelegraphattackso), [BuffSkill](#buffskill), [ChargeAttackSO](#chargeattackso), [DashSkill](#dashskill), [MeleeAttackSO](#meleeattackso), [ProjectileVolleySkill](#projectilevolleyskill), [ShieldHealSkill](#shieldhealskill), [ShootAttackSO](#shootattackso), [SummonAttackSO](#summonattackso), [SummonCompanionSkill](#summoncompanionskill), [TeleportAttackSO](#teleportattackso)
- **Hero và quái**: [HeroDefinitionSO](#herodefinitionso), [MonsterDefinitionSO](#monsterdefinitionso)
- **Rơi đồ**: [LootConfigSO](#lootconfigso)
- **Level và map**: [CampaignSO](#campaignso), [LevelConfigSO](#levelconfigso), [RoomTypeConfigSO](#roomtypeconfigso)
- **Bẫy**: [TrapDataSO](#trapdataso)
- **Passive**: [ItemPassiveSO](#itempassiveso), [EquipmentPassiveSO](#equipmentpassiveso), [LowHpBerserkPassive](#lowhpberserkpassive), [WeaponPassiveSO](#weaponpassiveso), [ArmorBreakNovaPassive](#armorbreaknovapassive), [CritRewardPassive](#critrewardpassive), [EquipStatBonusPassive](#equipstatbonuspassive), [EveryNthHitPassive](#everynthhitpassive), [HookWeaponPassive](#hookweaponpassive), [StillnessEmpowerPassive](#stillnessempowerpassive), [WeaponStatBonusPassive](#weaponstatbonuspassive), [ChainLightningPassive](#chainlightningpassive), [LifestealPassive](#lifestealpassive), [ManaOnKillPassive](#manaonkillpassive), [OnHitStatusPassive](#onhitstatuspassive)
- **Vật phẩm, trang bị, buff**: [EquipmentSetSO](#equipmentsetso), [ItemData](#itemdata), [RunBuffSO](#runbuffso), [EquipmentSO](#equipmentso)
- **Hạ tầng (Core)**: [LocalizationFontConfig](#localizationfontconfig), [ModuleDefinition](#moduledefinition), [ModuleRegistry](#moduleregistry), [SfxLibrary](#sfxlibrary)

## Gốc nối dữ liệu

### ItemCatalog

`ItemCatalog` — [`Scripts/Catalog/ItemCatalog.cs`](../Assets/Project/Scripts/Catalog/ItemCatalog.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Item Catalog`

Danh mục id → ItemData/HeroDefinitionSO (address "data/catalog", group Catalog_Items). Item và catalog nằm chung một group (Pack Together) nên tham chiếu trực tiếp không tốn thêm tải; màn Lobby/Play tải catalog một lần qua `LoadAsync`.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `items` | List<[ItemData](#itemdata)> | [] | Mọi vật phẩm (vũ khí, trang bị) tra theo id cho kho, save, shop, rơi đồ. Trùng id → cảnh báo, giữ cái đầu |
| `characters` | List<[HeroDefinitionSO](#herodefinitionso)> | [] | Mọi hero tra theo characterId. Trùng id → cảnh báo, giữ cái đầu |
| `swordLibrary` | [SwordLibrarySO](#swordlibraryso) |  | *[Thư viện vũ khí (hệ mới)]* Kiếm / giáo / dao: entry cùng id chứa mọi dữ liệu (chỉ số, hình, tên, bậc, icon, giá); WeaponData chỉ là id |
| `rangedLibrary` | [RangedLibrarySO](#rangedlibraryso) |  | Súng / cung / trượng: entry cùng id chứa mọi dữ liệu (chỉ số, hình, tên, bậc, icon, giá); WeaponData chỉ là id |
| `projectileLibrary` | [ProjectileLibrarySO](#projectilelibraryso) |  | Các loại đạn (cả kiếm khí); vũ khí tham chiếu đạn theo id |
| `campaign` | [CampaignSO](#campaignso) |  | *[Campaign (thứ tự level)]* Thứ tự level: số tầng tối đa + address config từng tầng. Trống → address mặc định theo theme / tầng |
| `skillLibrary` | [SkillLibrarySO](#skilllibraryso) |  | *[Thư viện skill]* Thư viện skill, đăng ký vào SkillRegistry; hero / quái / boss chọn skill bằng enum SkillId |

**Asset (1)**: `Data/Catalog/` ItemCatalog

### PlayPrefabSet

`PlayPrefabSet` — [`Scripts/Play/PlayPrefabSet.cs`](../Assets/Project/Scripts/Play/PlayPrefabSet.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Play Prefab Set`

Prefab dùng chung của module Play (cùng group Mod_Play → tham chiếu trực tiếp an toàn). Gán vào LevelDirector trong Play.unity.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `player` | [PlayerController](#playercontroller) |  | *[Player]* Prefab người chơi sinh khi vào màn. Trống → lỗi, không sinh người chơi |
| `damagePopup` | [DamagePopup](#damagepopup) |  | *[Combat FX]* Số sát thương bay lên khi trúng đòn (lấy từ pool, tạo sẵn prewarmPopups cái) |
| `defaultHitVfx` | `GameObject` |  | VFX trúng dự phòng khi đạn / tia không có hitVfx riêng. Trống → không phát |
| `spawnMarker` | [SpawnWarningMarker](#spawnwarningmarker) |  | Dấu cảnh báo tại chỗ quái sắp sinh ra (pool, tạo sẵn 6). Trống → không hiện dấu |
| `goldCoin` | [LootDrop](#lootdrop) |  | *[Loot]* Prefab đồng vàng rơi ra; tổng vàng chia cho nhiều đồng. Trống → không rơi vàng |
| `manaOrb` | [LootDrop](#lootdrop) |  | Prefab cầu mana rơi ra, mỗi cầu hồi 4 mana |
| `healthPotion` | [LootDrop](#lootdrop) |  | Prefab bình máu rơi ra, mỗi bình hồi 2 HP |
| `itemPickup` | [ItemPickup](#itempickup) |  | Vật phẩm nằm trên đất (vũ khí / trang bị rơi ra). Trống → không rơi vật phẩm |
| `door` | [RoomDoor](#roomdoor) |  | *[Phòng]* Cửa đặt ở mỗi lối nối đang dùng giữa các phòng khi dựng dungeon |
| `chest` | [Chest](#chest) |  | Rương ở phòng Treasure và rương có thể rơi khi dọn xong phòng Combat / Elite. Trống → không có rương |
| `shopStand` | [ShopStand](#shopstand) |  | Quầy hàng đặt ở phòng Shop. Trống → phòng Shop không có quầy |
| `portal` | [Portal](#portal) |  | Cổng sang tầng kế tiếp, hiện khi hạ boss. Trống → hạ boss là kết thúc màn ngay |
| `monsterBase` | [Enemy](#enemy) |  | *[Quái]* Prefab nền dùng chung cho mọi quái thường — định nghĩa (MonsterDefinitionSO) áp hình / chỉ số lúc sinh |
| `monsterTiers` | [MonsterTierConfigSO](#monstertierconfigso) |  | *[Cấu hình chỉ số]* Hệ số theo bậc quái Normal / Elite / Boss |
| `lootConfig` | [LootConfigSO](#lootconfigso) |  | *[Rơi đồ]* Rơi đồ theo bậc quái + level, trọng số bậc D…S |
| `prewarmCoins` | `int` | 20 | *[Prewarm]* Số đồng vàng tạo sẵn trong pool lúc vào màn (tránh giật khi rơi nhiều) |
| `prewarmPopups` | `int` | 12 | Số popup sát thương tạo sẵn trong pool lúc vào màn |

**Asset (1)**: `Data/` PlayPrefabSet


## Chỉ số

### MonsterTierConfigSO

`MonsterTierConfigSO` — [`Scripts/Catalog/Stats/MonsterTierConfigSO.cs`](../Assets/Project/Scripts/Catalog/Stats/MonsterTierConfigSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Stats ▸ Monster Tier Config`

Hệ số theo bậc quái (Normal / Elite / Boss). Áp vào StatSheet của quái như một nguồn modifier riêng (phần trăm), nên không cần code riêng cho từng bậc. Hệ số rớt đồ dùng ở hệ loot.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `tiers` | List<[Tier](#tier)> | 3 phần tử | Hệ số cho từng bậc quái Normal / Elite / Boss (mỗi bậc một dòng) |

**Asset (1)**: `Data/Config/` MonsterTierConfig

### StatProfileSO

`StatProfileSO` — [`Scripts/Catalog/Stats/StatProfileSO.cs`](../Assets/Project/Scripts/Catalog/Stats/StatProfileSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Stats ▸ Stat Profile`

Bảng chỉ số gốc dùng chung cho hero và quái (vd "Stats_Hero_Warrior", "Stats_Monster_Melee"). Nơi dùng gắn qua `StatBlock` và chỉ ghi đè vài chỉ số cần khác.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `stats` | [BaseStats](#basestats) |  | Chỉ số gốc của profile; nơi dùng (StatBlock) chép ra rồi ghi đè vài chỉ số |
| `armorRegenDelay` | `float` | 3 | *[Hồi phục]* Giây không bị đánh trước khi giáp bắt đầu hồi. |
| `armorRegenRate` | `float` | 1 | Điểm giáp hồi mỗi giây sau khi hết delay. |

**Asset (5)**: `Data/Stats/` Stats_Hero_Assassin, Stats_Hero_Mage, Stats_Hero_Ranger, Stats_Hero_Warrior, Stats_Monster_Default


## Hiệu ứng trạng thái

### StatusEffectSO

`StatusEffectSO` *(abstract)* — [`Scripts/Catalog/Status/StatusEffectSO.cs`](../Assets/Project/Scripts/Catalog/Status/StatusEffectSO.cs)

Định nghĩa một hiệu ứng trạng thái (Stun, Slow, Burn…). Một asset = một hiệu ứng với tham số mặc định; nơi gây hiệu ứng tham chiếu asset qua `StatusEffectRef` và ghi đè thời gian / độ mạnh nếu cần.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `id` | `string` |  | Định danh duy nhất (vd "stun", "burn") |
| `nameKey` | `string` |  | Key localization tên hiệu ứng (hiện chưa UI nào đọc) |
| `icon` | `Sprite` |  | Icon hiệu ứng cho UI (hiện chưa UI nào đọc) |
| `defaultDuration` | `float` |  | *[Mặc định]*  (≥ 0) |
| `defaultMagnitude` | `float` |  | Ý nghĩa tùy loại: Slow = phần trăm (0.3 = 30%), DoT = sát thương mỗi giây, StatModifier = hệ số nhân |
| `stacking` | `StackPolicy` |  | *[Cộng dồn]* Khi gây lại lúc đang dính: Refresh / Extend / Stack (tối đa maxStacks) / KeepStrongest |
| `maxStacks` | `int` |  |  (≥ 1) |
| `tags` | `StatusTag` |  | *[Hiển thị]* Phân loại; có CrowdControl → mục tiêu không đi / không đánh / không dùng skill |
| `tint` | `Color` |  | Màu phủ lên sprite khi đang dính (alpha 0 = không phủ) |
| `tintPriority` | `int` |  | Nhiều hiệu ứng cùng có màu → lấy cái ưu tiên cao nhất |
| `loopVfx` | `GameObject` |  | VFX lặp gắn theo mục tiêu khi đang dính (lấy từ pool) |

### DamageOverTimeEffectSO

`DamageOverTimeEffectSO` : `StatusEffectSO` — [`Scripts/Catalog/Status/DamageOverTimeEffectSO.cs`](../Assets/Project/Scripts/Catalog/Status/DamageOverTimeEffectSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Status ▸ Damage Over Time`

Sát thương theo thời gian (cháy, độc, chảy máu): magnitude = sát thương mỗi giây.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `tickInterval` | `float` | 0.5 |  (≥ 0.05) |

*Cộng thêm field của: [StatusEffectSO](#statuseffectso).*

**Asset (2)**: `Data/StatusEffects/` Status_Burn, Status_Poison

### SlowEffectSO

`SlowEffectSO` : `StatusEffectSO` — [`Scripts/Catalog/Status/SlowEffectSO.cs`](../Assets/Project/Scripts/Catalog/Status/SlowEffectSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Status ▸ Slow`

Làm chậm: modifier MoveSpeed −magnitude (0.3 = −30%) trên StatSheet, tự gỡ khi hết.

*Không có field riêng (dùng field của lớp cha).*

**Asset (1)**: `Data/StatusEffects/` Status_Slow

### StatModifierEffectSO

`StatModifierEffectSO` : `StatusEffectSO` — [`Scripts/Catalog/Status/StatModifierEffectSO.cs`](../Assets/Project/Scripts/Catalog/Status/StatModifierEffectSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Status ▸ Stat Modifier`

Buff / debuff chỉ số trong thời gian ngắn. Giá trị modifier × magnitude (mặc định 1) × số tầng.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `modifiers` | List<[StatModifier](#statmodifier)> | [] | Modifier áp khi đang dính; Flat và % đều nhân magnitude × số tầng, tự gỡ khi hết |

*Cộng thêm field của: [StatusEffectSO](#statuseffectso).*

**Asset**: chưa có.

### StunEffectSO

`StunEffectSO` : `StatusEffectSO` — [`Scripts/Catalog/Status/StunEffectSO.cs`](../Assets/Project/Scripts/Catalog/Status/StunEffectSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Status ▸ Stun (Freeze)`

Choáng / đóng băng: mục tiêu không đi, không đánh, không dùng skill. Đóng băng = asset Stun có màu xanh.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `freeze` | `bool` | ✗ | Hiển thị là đóng băng (dừng animation, màu băng) |

*Cộng thêm field của: [StatusEffectSO](#statuseffectso).*

**Asset (2)**: `Data/StatusEffects/` Status_Freeze, Status_Stun


## Vũ khí

### ProjectileLibrarySO

`ProjectileLibrarySO` — [`Scripts/Catalog/Weapons/ProjectileLibrarySO.cs`](../Assets/Project/Scripts/Catalog/Weapons/ProjectileLibrarySO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Weapons ▸ Projectile Library`

Thư viện đạn dùng chung cho súng và kiếm khí.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `projectiles` | List<[ProjectileEntry](#projectileentry)> | [] | Các loại đạn; vũ khí chọn theo id (projectileId, waveProjectileId, defaultProjectileId) |

**Asset (1)**: `Data/WeaponLibrary/` ProjectileLibrary

### RangedLibrarySO

`RangedLibrarySO` — [`Scripts/Catalog/Weapons/RangedLibrarySO.cs`](../Assets/Project/Scripts/Catalog/Weapons/RangedLibrarySO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Weapons ▸ Ranged Library`

Thư viện vũ khí tầm xa: một SO gốc + danh sách súng / cung / trượng (sprite, animator, nòng, đạn, VFX).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `baseConfig` | [RangedBaseSO](#rangedbaseso) |  | SO gốc: giá trị mặc định cho mọi vũ khí tầm xa. Trống → mọi súng dùng thông số tối thiểu |
| `weapons` | List<[RangedEntry](#rangedentry)> | [] | Từng vũ khí tầm xa; id trùng id WeaponData, chỉ ghi đè phần khác SO gốc |

**Asset (1)**: `Data/WeaponLibrary/` RangedLibrary

### SwordLibrarySO

`SwordLibrarySO` — [`Scripts/Catalog/Weapons/SwordLibrarySO.cs`](../Assets/Project/Scripts/Catalog/Weapons/SwordLibrarySO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Weapons ▸ Sword Library`

Thư viện vũ khí cận chiến: một SO gốc + danh sách kiếm / giáo / dao (sprite, hitbox, kiếm khí, VFX).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `baseConfig` | [SwordBaseSO](#swordbaseso) |  | SO gốc: giá trị mặc định cho mọi vũ khí cận chiến. Trống → mọi kiếm dùng thông số tối thiểu |
| `swords` | List<[SwordEntry](#swordentry)> | [] | Từng vũ khí cận chiến; id trùng id WeaponData, chỉ ghi đè phần khác SO gốc |

**Asset (1)**: `Data/WeaponLibrary/` SwordLibrary

### WeaponBaseSO

`WeaponBaseSO` *(abstract)* — [`Scripts/Catalog/Weapons/WeaponBaseSO.cs`](../Assets/Project/Scripts/Catalog/Weapons/WeaponBaseSO.cs)

Gốc mọi vũ khí: chỉ số + hiệu ứng mặc định. Kiếm (`SwordBaseSO`) và vũ khí tầm xa (`RangedBaseSO`) kế thừa và thêm thông số riêng. Từng vũ khí cụ thể là một entry trong thư viện, chỉ ghi đè những gì khác với gốc.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `damage` | `float` |  | *[Chỉ số mặc định]* Sát thương mỗi đòn / mỗi viên, nhân với chỉ số AttackDamage của người cầm |
| `fireRate` | `float` |  | Số đòn / giây |
| `attackRange` | `float` |  | Bán kính chém hoặc tầm bắn |
| `knockback` | `float` |  | Lực đẩy lùi mục tiêu (giảm theo KnockbackResist); 0 = không đẩy, kiếm khí đẩy 60% |
| `critBonus` | `float` |  |  (khoảng 0–1) |
| `manaCost` | `int` |  | Mana tốn mỗi lần tấn công; 0 = miễn phí, thiếu mana thì không đánh được |
| `holdStyle` | `WeaponHoldStyle` |  | *[Cầm & hiển thị mặc định]* Kiểu cầm; OneHandedSide trên vũ khí tầm xa tự đổi thành Ranged |
| `visualScale` | `float` |  | Tỉ lệ hiển thị sprite vũ khí trên tay; ≤ 0 → 0.55 |
| `spriteForwardAngle` | `float` |  | Góc vẽ gốc của sprite (độ): kiếm 135° (mũi lên trên-trái), súng 0° |
| `hitImpactVfx` | `GameObject` |  | *[Hiệu ứng mặc định]* VFX khi đạn / tia / kiếm khí trúng; chém cận chiến không phát. Trống → defaultHitVfx |
| `attackSfx` | `AudioClip` |  | Âm khi tấn công. Trống → âm mặc định theo loại (vung kiếm / súng / phép / cung) |
| `onHitStatus` | List<[StatusEffectRef](#statuseffectref)> |  | Hiệu ứng gây khi trúng (mọi vũ khí thuộc loại này) |

### RangedBaseSO

`RangedBaseSO` : `WeaponBaseSO` — [`Scripts/Catalog/Weapons/RangedBaseSO.cs`](../Assets/Project/Scripts/Catalog/Weapons/RangedBaseSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Weapons ▸ Ranged Base`

Gốc vũ khí tầm xa (súng, cung, trượng): kiểu bắn, chùm, loạt, giật nòng, đạn mặc định.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `fireMode` | `RangedFireMode` | Projectile | *[Tầm xa]* Projectile = bắn đạn bay; Hitscan = tia trúng tức thì. Không có prefab đạn → luôn Hitscan |
| `spread` | `float` | 0 | Độ lệch đạn (độ) |
| `projectileCount` | `int` | 1 |  (≥ 1) |
| `burstCount` | `int` | 1 |  (≥ 1) |
| `burstInterval` | `float` | 0.08 |  (≥ 0.02) |
| `recoilDistance` | `float` | 0.12 | Độ giật lùi của vũ khí mỗi phát (world-unit); 0 = không giật |
| `recoilDuration` | `float` | 0.08 | Thời gian một lần giật, lùi + về (giây); ≤ 0 → giá trị của prefab vũ khí |
| `muzzleVfx` | `GameObject` |  | VFX đầu nòng mỗi phát. Trống → VFX mặc định của prefab vũ khí |
| `defaultProjectileId` | `string` |  | Id đạn trong ProjectileLibrary dùng khi entry không chọn đạn |

*Cộng thêm field của: [WeaponBaseSO](#weaponbaseso).*

**Asset (1)**: `Data/WeaponLibrary/` WeaponBase_Ranged

### SwordBaseSO

`SwordBaseSO` : `WeaponBaseSO` — [`Scripts/Catalog/Weapons/SwordBaseSO.cs`](../Assets/Project/Scripts/Catalog/Weapons/SwordBaseSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Weapons ▸ Sword Base`

Gốc vũ khí cận chiến (kiếm, giáo, dao): kiểu đánh, cung chém, hitbox, vệt chém, kiếm khí.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `meleeStyle` | `MeleeAttackStyle` | Swing | *[Cận chiến]* Swing = vung cung; Thrust = đâm thẳng (cung ≤ 55°); Spin360 = xoay tròn trúng mọi hướng |
| `swingArc` | `float` | 110 | Góc vung (độ) |
| `swingDuration` | `float` | 0.15 | Thời gian một lần vung (giây); hồi đòn ít nhất bằng giá trị này + 0.12s. ≤ 0 → giá trị prefab |
| `hitbox` | [MeleeHitbox](#meleehitbox) |  | Vùng trúng: Auto = khớp vệt chém, Arc = quạt, Box = hộp; offset/size theo hướng ngắm |
| `destroysProjectiles` | `bool` | ✓ | Chém tan đạn thường của quái |
| `slashVfx` | `GameObject` |  | VFX vệt chém mỗi lần vung (đòn chém chỉ phát VFX này). Trống → VFX mặc định của prefab |
| `swordWaveDamageRatio` | `float` | 0.6 | Tỉ lệ sát thương kiếm khí (khi entry có đạn) (khoảng 0.1–2) |

*Cộng thêm field của: [WeaponBaseSO](#weaponbaseso).*

**Asset (1)**: `Data/WeaponLibrary/` WeaponBase_Sword

### WeaponData

`WeaponData` : `ItemData` — [`Scripts/Catalog/WeaponData.cs`](../Assets/Project/Scripts/Catalog/WeaponData.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Weapon Item`

Vật phẩm vũ khí: chỉ là định danh trong kho / save / shop (id). Mọi thứ khác — chỉ số, hình, hitbox, đạn, VFX, tên, icon, bậc, giá, skill, passive — nằm ở entry cùng id trong Sword / Ranged Library (ItemCatalog đăng ký). Các field kế thừa từ ItemData (nameKey, icon, rarity, shopPrice…) chỉ là dự phòng khi thiếu entry.

*Không có field riêng (dùng field của lớp cha).*

**Asset (21)**: `Data/Weapons/` Weapon_bow-wood, Weapon_dagger-twin, Weapon_gun-launcher, Weapon_gun-pistol, Weapon_gun-rifle, Weapon_gun-shotgun, Weapon_spear-iron, Weapon_staff-apprentice, Weapon_sword-dark, Weapon_sword-fire, Weapon_sword-gold, Weapon_sword-holy, Weapon_sword-ice, Weapon_sword-iron, Weapon_sword-lava, Weapon_sword-old, Weapon_sword-purple, Weapon_sword-thorn, Weapon_sword-thunder, Weapon_sword-venom, Weapon_sword-void


## Skill

### SkillBaseSO

`SkillBaseSO` *(abstract)* — [`Scripts/Catalog/Skills/SkillBaseSO.cs`](../Assets/Project/Scripts/Catalog/Skills/SkillBaseSO.cs)

Gốc chung của mọi skill: skill chủ động của hero / vật phẩm (`ActiveSkillSO`) và đòn của quái / boss (EnemyAttackSO). Mỗi lớp con là một kiểu hành vi (lướt, nổ vùng, bắn chùm, lao…) kèm tham số mặc định. Skill được liệt kê trong `SkillLibrarySO` và gán cho hero / quái / boss bằng enum `SkillId`.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `skillId` | `string` |  | Key định danh (vd "shadow-step") — trùng key của entry trong SkillLibrary |
| `nameKey` | `string` |  | Key localization tên skill (Lobby / Loadout). Trống → hiện skillId |
| `descKey` | `string` |  | Key localization mô tả skill (hiện chưa UI nào đọc) |
| `icon` | `Sprite` |  | Icon skill trên HUD trận đánh. Trống → ẩn icon |
| `cooldown` | `float` |  |  (≥ 0) |

### SkillLibrarySO

`SkillLibrarySO` — [`Scripts/Catalog/Skills/SkillLibrarySO.cs`](../Assets/Project/Scripts/Catalog/Skills/SkillLibrarySO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Skill Library`

Thư viện skill của cả game. Thêm skill: thêm entry (key + SO hành vi) → menu Tools ▸ Dungeon Gathering ▸ Skills ▸ Generate SkillId → chọn enum trên hero / quái / phase boss.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `skills` | List<[SkillEntry](#skillentry)> | [] | Mọi skill của game; thêm entry xong chạy menu Generate SkillId |
| `highestIssuedValue` | `int` | 0 | Số enum SkillId lớn nhất từng cấp (kể cả skill đã xóa). Generate SkillId không cấp lại số ≤ giá trị này để hero / quái đang lưu số của skill đã xóa không âm thầm trỏ sang skill mới. Không sửa tay. |

**Asset (1)**: `Data/Skills/` SkillLibrary

### ActiveSkillSO

`ActiveSkillSO` : `SkillBaseSO` *(abstract)* — [`Scripts/Catalog/ActiveSkillSO.cs`](../Assets/Project/Scripts/Catalog/ActiveSkillSO.cs)

Kỹ năng chủ động (của nhân vật hoặc vật phẩm).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `duration` | `float` |  |  (≥ 0) |
| `manaCost` | `float` |  |  (≥ 0) |
| `activateSfx` | `SfxId` |  | None = dùng âm mặc định của loại kỹ năng. |

*Cộng thêm field của: [SkillBaseSO](#skillbaseso).*

### EnemyAttackSO

`EnemyAttackSO` : `SkillBaseSO` *(abstract)* — [`Scripts/Enemy/EnemyAttackSO.cs`](../Assets/Project/Scripts/Enemy/EnemyAttackSO.cs)

Đòn tấn công dạng module (ARCHITECTURE §6). Execute là async: windup bị hủy khi quái chết (DeadState) → không tung đòn sau khi chết.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `range` | `float` |  | Khoảng cách tối đa (world-unit) từ điểm ra đòn tới Player để đòn này được chọn. |
| `windupTime` | `float` |  | Thời gian gồng/cảnh báo trước khi ra đòn (giây); bị hủy nếu quái chết. |
| `weight` | `float` |  | Trọng số chọn khi nhiều đòn sẵn sàng (≥ 0) |
| `requireLineOfSight` | `bool` |  | Phải nhìn thấy Player (không bị tường chắn) |
| `stopWhileAttacking` | `bool` |  | Đứng yên khi ra đòn |
| `status` | [StatusPayload](#statuspayload) |  | Hiệu ứng bất lợi kèm theo khi trúng mục tiêu (tùy chọn). |

*Cộng thêm field của: [SkillBaseSO](#skillbaseso).*

### AreaBurstSkill

`AreaBurstSkill` : `ActiveSkillSO` — [`Scripts/Combat/Skills/AreaBurstSkill.cs`](../Assets/Project/Scripts/Combat/Skills/AreaBurstSkill.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Area Burst`

Đòn diện rộng: Hỏa Trận, Băng Trận, Lôi Giáng, Bổ Trảm... Tâm = bản thân hoặc quái gần nhất. Sát thương = damage × AttackDamage của Player.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `damage` | `float` | 30 | Hệ số sát thương mỗi nhịp, nhân với AttackDamage của Player (1 = 100%). |
| `radius` | `float` | 3 | Bán kính vùng nổ (world-unit) quanh tâm. |
| `knockback` | `float` | 6 | Lực đẩy lùi quái ra xa tâm nổ (0 = không đẩy). |
| `centerOnNearestEnemy` | `bool` | ✗ | Bật: đặt tâm vào quái gần nhất trong targetRange (dò lại mỗi nhịp); tắt hoặc không có quái: tâm quanh Player / hướng ngắm. |
| `followPlayer` | `bool` | ✗ | Bật cho skill vòng bảo vệ quanh người: các nhịp liên tiếp bám theo vị trí Player. |
| `targetRange` | `float` | 8 | Tầm dò quái gần nhất (world-unit), chỉ dùng khi bật centerOnNearestEnemy. |
| `pulses` | `int` | 1 | Số lần đánh (vd sàn lửa đánh nhiều nhịp) (≥ 1) |
| `pulseInterval` | `float` | 0.5 | Khoảng cách giữa các nhịp đánh (giây), chỉ có tác dụng khi pulses > 1. |
| `status` | [StatusPayload](#statuspayload) |  | Hiệu ứng trạng thái áp lên mỗi quái trúng đòn (mỗi nhịp); để trống = không áp. |
| `vfx` | `GameObject` |  | VFX phát tại tâm mỗi nhịp; null = không có hiệu ứng hình. |

*Cộng thêm field của: [ActiveSkillSO](#activeskillso), [SkillBaseSO](#skillbaseso).*

**Asset (10)**: `Data/Skills/` Skill_arcane-storm, Skill_arcane-storm-book, Skill_dragon-flame, Skill_frost-field, Skill_ice-spike, Skill_leaf-storm, Skill_mega-blast, Skill_shadow-curse, Skill_thunder-judgement, Skill_wind-whirlwind

### AreaTelegraphAttackSO

`AreaTelegraphAttackSO` : `EnemyAttackSO` — [`Scripts/Enemy/Attacks/AreaTelegraphAttackSO.cs`](../Assets/Project/Scripts/Enemy/Attacks/AreaTelegraphAttackSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Enemy Attacks ▸ Area Telegraph`

Hiện vòng cảnh báo dưới chân mục tiêu rồi phát nổ gây sát thương vùng sau windupTime.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `damage` | `float` | 2 | Sát thương mỗi vùng nổ (HP) × AttackDamage của quái (đã gồm hệ số bậc). |
| `radius` | `float` | 1.8 | Bán kính mỗi vùng nổ (world-unit); vòng cảnh báo được scale theo đường kính này. |
| `knockback` | `float` | 5 | Lực đẩy Player ra xa tâm vùng nổ (0 = không đẩy). |
| `zonesCount` | `int` | 1 |  (≥ 1) |
| `zoneScatterRadius` | `float` | 2.5 | Độ tản của các vùng phụ quanh Player (world-unit, ngẫu nhiên 0.8 đến giá trị này); chỉ dùng khi zonesCount > 1. |
| `warningPrefab` | `GameObject` |  | Prefab vòng cảnh báo hiện trong windupTime tại mỗi vùng; null = nổ không báo trước. |
| `explosionVfx` | `GameObject` |  | VFX phát tại mỗi vùng khi nổ; null = không có. |

*Cộng thêm field của: [EnemyAttackSO](#enemyattackso), [SkillBaseSO](#skillbaseso).*

**Asset (1)**: `Data/EnemyAttacks/` Atk_lord-bloodpool

### BuffSkill

`BuffSkill` : `ActiveSkillSO` — [`Scripts/Combat/Skills/BuffSkill.cs`](../Assets/Project/Scripts/Combat/Skills/BuffSkill.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Buff`

Buff chỉ số tạm thời (Cuồng nộ, tăng tốc...).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `modifiers` | List<[StatModifier](#statmodifier)> | [] | Các chỉ số cộng thêm tạm thời cho Player, kéo dài theo duration của skill; rỗng = không buff. |
| `vfx` | `GameObject` |  | VFX gắn dưới chân Player suốt thời gian buff; null = không có. |

*Cộng thêm field của: [ActiveSkillSO](#activeskillso), [SkillBaseSO](#skillbaseso).*

**Asset (2)**: `Data/Skills/` Skill_blood-frenzy, Skill_dual-wield

### ChargeAttackSO

`ChargeAttackSO` : `EnemyAttackSO` — [`Scripts/Enemy/Attacks/ChargeAttackSO.cs`](../Assets/Project/Scripts/Enemy/Attacks/ChargeAttackSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Enemy Attacks ▸ Charge`

Hiện đường cảnh báo đỏ rồi lao nhanh về phía Player, gây sát thương khi chạm.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `damage` | `float` | 1 | Sát thương khi lao trúng (HP) × AttackDamage của quái; chỉ gây 1 lần mỗi cú lao. |
| `distance` | `float` | 5 | Quãng đường lao tối đa (world-unit); cũng là độ dài vạch cảnh báo; dừng sớm nếu đụng tường. |
| `chargeSpeed` | `float` | 14 | Tốc độ lao (world-unit/giây, tối thiểu 0.1). |
| `hitRadius` | `float` | 0.6 | Bán kính va chạm quanh quái khi đang lao (world-unit). |
| `knockback` | `float` | 6 | Lực đẩy Player theo hướng lao (0 = không đẩy). |
| `warningPrefab` | `GameObject` |  | Prefab cảnh báo (sprite đường đỏ, pivot ở đầu trái, dài 1 đơn vị) — scale X theo distance |

*Cộng thêm field của: [EnemyAttackSO](#enemyattackso), [SkillBaseSO](#skillbaseso).*

**Asset (3)**: `Data/EnemyAttacks/` Atk_assassin-dash, Atk_lord-charge, Atk_warrior-charge

### DashSkill

`DashSkill` : `ActiveSkillSO` — [`Scripts/Combat/Skills/DashSkill.cs`](../Assets/Project/Scripts/Combat/Skills/DashSkill.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Dash`

Lướt (có thể bất tử trong lúc lướt): Bóng Đêm, Blink.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `distance` | `float` | 4 | Quãng đường lướt (world-unit) theo hướng di chuyển, không di chuyển thì theo hướng ngắm. |
| `dashDuration` | `float` | 0.18 | Thời gian lướt (giây). |
| `invulnerable` | `bool` | ✓ | Bất tử trong lúc lướt (dashDuration + 0.1 giây). |
| `vfx` | `GameObject` |  | VFX phát tại điểm xuất phát khi lướt; null = không có. |
| `slashDamage` | `float` | 0 | *[Tùy chọn: Chém khi lướt & Cường hóa đòn kế]* Hệ số sát thương chém khi lướt × AttackDamage của Player; 0 = không chém. |
| `slashRadius` | `float` | 1.1 | Bán kính vùng chém (world-unit), tâm ở giữa quãng lướt thực tế (dừng trước tường); quái sau tường không trúng. |
| `knockback` | `float` | 4 | Lực đẩy lùi quái trúng đòn chém (0 = không đẩy). |
| `status` | [StatusPayload](#statuspayload) |  | Hiệu ứng trạng thái áp lên quái trúng đòn chém; để trống = không áp. |
| `empowerNextMultiplier` | `float` | 1 | Hệ số sát thương cho đòn đánh thường kế tiếp sau khi lướt; ≤ 1 = không cường hóa. |
| `slashVfx` | `GameObject` |  | VFX vệt chém xoay theo hướng lướt, chỉ phát khi slashDamage > 0; null = không có. |

*Cộng thêm field của: [ActiveSkillSO](#activeskillso), [SkillBaseSO](#skillbaseso).*

**Asset (4)**: `Data/Skills/` Skill_dodge-roll, Skill_rift-cut, Skill_shadow-step, Skill_spear-charge

### MeleeAttackSO

`MeleeAttackSO` : `EnemyAttackSO` — [`Scripts/Enemy/Attacks/MeleeAttackSO.cs`](../Assets/Project/Scripts/Enemy/Attacks/MeleeAttackSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Enemy Attacks ▸ Melee`

Áp sát cắn/chém: windup rồi gây sát thương trong vòng tròn trước mặt.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `damage` | `float` | 1 | Sát thương mỗi đòn (HP) × AttackDamage của quái (đã gồm hệ số bậc). |
| `hitRadius` | `float` | 0.8 | Bán kính vùng chém (world-unit), tâm đặt trước mặt quái 0.8 × hitRadius. |
| `knockback` | `float` | 4 | Lực đẩy Player khi trúng đòn (0 = không đẩy). |
| `slashVfx` | `GameObject` |  | VFX vệt chém xoay theo hướng đánh, tự thu nhỏ theo hitRadius; null = không có. |

*Cộng thêm field của: [EnemyAttackSO](#enemyattackso), [SkillBaseSO](#skillbaseso).*

**Asset (5)**: `Data/EnemyAttacks/` Atk_bite, Atk_lord-claw, Atk_spear-thrust, Atk_sword-slash, Atk_venom-slash

### ProjectileVolleySkill

`ProjectileVolleySkill` : `ActiveSkillSO` — [`Scripts/Combat/Skills/ProjectileVolleySkill.cs`](../Assets/Project/Scripts/Combat/Skills/ProjectileVolleySkill.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Projectile Volley`

Kỹ năng chủ động phóng chùm đạn theo hình quạt (Ví dụ: Mưa Tên).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `projectilePrefab` | `GameObject` |  | Prefab đạn (cần component Projectile); null = skill không bắn gì. |
| `impactVfx` | `GameObject` |  | VFX khi đạn trúng mục tiêu; null = không có. |
| `damage` | `float` | 14 | Hệ số sát thương mỗi viên × AttackDamage của Player (có thể chí mạng). |
| `speed` | `float` | 13 | Tốc độ bay của đạn (world-unit/giây). |
| `knockback` | `float` | 2.5 | Lực đẩy lùi quái trúng đạn (0 = không đẩy). |
| `count` | `int` | 5 |  (≥ 1) |
| `spreadAngle` | `float` | 35 | Tổng góc tỏa của chùm đạn (độ), chia đều theo count; bỏ qua khi count = 1. |
| `pierce` | `int` | 1 | Số quái đạn xuyên qua thêm trước khi biến mất (0 = dừng ở mục tiêu đầu tiên). |
| `status` | [StatusPayload](#statuspayload) |  | Hiệu ứng trạng thái áp lên quái trúng đạn; để trống = không áp. |

*Cộng thêm field của: [ActiveSkillSO](#activeskillso), [SkillBaseSO](#skillbaseso).*

**Asset (1)**: `Data/Skills/` Skill_arrow-rain

### ShieldHealSkill

`ShieldHealSkill` : `ActiveSkillSO` — [`Scripts/Combat/Skills/ShieldHealSkill.cs`](../Assets/Project/Scripts/Combat/Skills/ShieldHealSkill.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Shield Heal`

Hồi máu/giáp + khiên bất tử ngắn: Thánh Quang, Giơ Khiên.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `heal` | `float` | 2 | Lượng máu hồi (đơn vị HP, Player ~6 HP); 0 = không hồi. |
| `armor` | `float` | 2 | Lượng giáp hồi (điểm giáp); 0 = không hồi. |
| `mana` | `float` | 0 | Lượng mana hồi; 0 = không hồi. |
| `invulnerableDuration` | `float` | 2 | Thời gian bất tử sau khi dùng (giây); 0 = không có khiên bất tử. |
| `moveSpeedBonus` | `float` | 0 | Tăng tốc chạy tạm thời (vd 0.2 = +20%); 0 = không tăng tốc. |
| `moveSpeedDuration` | `float` | 3 | Thời gian tăng tốc chạy (giây). |
| `vfx` | `GameObject` |  | VFX gắn theo Player, tồn tại ít nhất 1 giây hoặc bằng invulnerableDuration / buff; null = không có. |

*Cộng thêm field của: [ActiveSkillSO](#activeskillso), [SkillBaseSO](#skillbaseso).*

**Asset (4)**: `Data/Skills/` Skill_holy-light, Skill_holy-light-sword, Skill_raise-shield, Skill_rejuvenate

### ShootAttackSO

`ShootAttackSO` : `EnemyAttackSO` — [`Scripts/Enemy/Attacks/ShootAttackSO.cs`](../Assets/Project/Scripts/Enemy/Attacks/ShootAttackSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Enemy Attacks ▸ Shoot`

Bắn đạn: đơn, chùm (count + spreadAngle), liên thanh (bursts), tỏa tròn 360 (radial).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `projectilePrefab` | `GameObject` |  | Prefab đạn (cần component Projectile); null = đòn không bắn gì. |
| `impactVfx` | `GameObject` |  | VFX khi đạn trúng; null = không có. |
| `damage` | `float` | 1 | Sát thương mỗi viên (HP) × AttackDamage của quái (đã gồm hệ số bậc). |
| `speed` | `float` | 6 | Tốc độ bay của đạn (world-unit/giây). |
| `knockback` | `float` | 2 | Lực đẩy Player khi trúng đạn (0 = không đẩy). |
| `lifetime` | `float` | 4 | Thời gian sống của đạn (giây); ≤ 0 = dùng lifetime của prefab. |
| `count` | `int` | 1 |  (≥ 1) |
| `spreadAngle` | `float` | 30 | Tổng góc tỏa của chùm (độ) |
| `radial` | `bool` | ✗ | Bật: count viên tỏa đều 360 độ (bỏ qua spreadAngle); tắt: chùm quạt hướng về Player. |
| `bursts` | `int` | 1 |  (≥ 1) |
| `burstInterval` | `float` | 0.15 | Khoảng nghỉ giữa các loạt bắn (giây), chỉ dùng khi bursts > 1. |
| `radialSpin` | `float` | 10 | Xoay thêm mỗi loạt khi radial (tạo xoắn ốc) |

*Cộng thêm field của: [EnemyAttackSO](#enemyattackso), [SkillBaseSO](#skillbaseso).*

**Asset (8)**: `Data/EnemyAttacks/` Atk_blood-bolt, Atk_bolt-fan, Atk_frost-fan, Atk_frost-orb, Atk_lord-fan, Atk_lord-spiral, Atk_skull-radial, Atk_void-fan

### SummonAttackSO

`SummonAttackSO` : `EnemyAttackSO` — [`Scripts/Enemy/Attacks/SummonAttackSO.cs`](../Assets/Project/Scripts/Enemy/Attacks/SummonAttackSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Enemy Attacks ▸ Summon`

Triệu hồi quái con. Quái con đăng ký vào phòng qua Enemy.SpawnContext.Summon (vào tập alive của RoomController).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `minion` | [MonsterDefinitionSO](#monsterdefinitionso) |  | Định nghĩa quái con (sinh bằng prefab nền dùng chung) |
| `count` | `int` | 2 |  (≥ 1) |
| `radius` | `float` | 1.5 | Khoảng cách từ quái tới điểm sinh quái con (world-unit, tối thiểu 0.25); kẹt tường thì sinh tại chỗ. |
| `maxAlive` | `int` | 6 | Số quái con tối đa còn sống cùng lúc do con này gọi |

*Cộng thêm field của: [EnemyAttackSO](#enemyattackso), [SkillBaseSO](#skillbaseso).*

**Asset (1)**: `Data/EnemyAttacks/` Atk_lord-summon

### SummonCompanionSkill

`SummonCompanionSkill` : `ActiveSkillSO` — [`Scripts/Combat/Skills/SummonCompanionSkill.cs`](../Assets/Project/Scripts/Combat/Skills/SummonCompanionSkill.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Skills ▸ Summon Companion`

Kỹ năng chủ động triệu hồi đệ tử chiến đấu và thu hút quái.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `companionSprite` | `Sprite` |  | Sprite của đệ tử; null = mượn hình của hero (mờ, ánh tím) — nên gán sprite riêng. |
| `count` | `int` | 2 |  (≥ 1) |
| `companionHp` | `float` | 35 | Máu của mỗi đệ tử (HP tuyệt đối, tối thiểu 1). |
| `companionDamage` | `float` | 10 | Sát thương mỗi đòn của đệ tử × AttackDamage của Player (tối thiểu ×0.5), đánh mỗi 0.7 giây. |
| `moveSpeed` | `float` | 3.8 | Tốc độ di chuyển của đệ tử (world-unit/giây, tối thiểu 0.5). |
| `summonDuration` | `float` | 12 | Thời gian tồn tại của đệ tử (giây); ≤ 0 = dùng duration của skill, nếu cũng 0 thì 12 giây. |
| `tauntRadius` | `float` | 5 | Bán kính khiêu khích (world-unit): quái trong vùng chuyển mục tiêu sang đệ tử; 0 = không khiêu khích. |
| `summonVfx` | `GameObject` |  | VFX phát tại vị trí xuất hiện của mỗi đệ tử; null = không có. |

*Cộng thêm field của: [ActiveSkillSO](#activeskillso), [SkillBaseSO](#skillbaseso).*

**Asset (1)**: `Data/Skills/` Skill_summon-skeletons

### TeleportAttackSO

`TeleportAttackSO` : `EnemyAttackSO` — [`Scripts/Enemy/Attacks/TeleportAttackSO.cs`](../Assets/Project/Scripts/Enemy/Attacks/TeleportAttackSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Enemy Attacks ▸ Teleport`

Biến mất rồi dịch chuyển tới quanh mục tiêu; có thể nối tiếp bằng một đòn đánh.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `minAppearDistance` | `float` | 1.5 | Khoảng cách tối thiểu từ Player tới điểm xuất hiện (world-unit, tối thiểu 0.8). |
| `maxAppearDistance` | `float` | 3.5 | Khoảng cách tối đa từ Player tới điểm xuất hiện (world-unit, không nhỏ hơn min). |
| `vanishDuration` | `float` | 0.25 | Thời gian biến mất trước khi hiện lại (giây); ≤ 0 = dùng windupTime. |
| `teleportVfx` | `GameObject` |  | VFX phát tại điểm biến mất và điểm xuất hiện; null = không có. |
| `followUpAttack` | [EnemyAttackSO](#enemyattackso) |  | Đòn tung ngay sau khi xuất hiện, chỉ khi Player trong range (và tầm nhìn nếu đòn yêu cầu); bỏ qua cooldown. Không được là Teleport. Null = chỉ dịch chuyển. |

*Cộng thêm field của: [EnemyAttackSO](#enemyattackso), [SkillBaseSO](#skillbaseso).*

**Asset (1)**: `Data/EnemyAttacks/` Atk_lord-teleport


## Hero và quái

### HeroDefinitionSO

`HeroDefinitionSO` — [`Scripts/Catalog/HeroDefinitionSO.cs`](../Assets/Project/Scripts/Catalog/HeroDefinitionSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Hero Definition`

Định nghĩa hero: định danh, bậc, hình / animation, VFX đòn đánh, chỉ số (StatProfile + ghi đè), vũ khí khởi đầu, skill (SkillId), nội tại. Một Player.prefab cho mọi hero — khác biệt nằm hết ở đây.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `characterId` | `string` | "knight" | *[Định danh]* ID duy nhất của hero: khóa lưu save (mở khóa, loadout, tiến trình) và tra cứu trong catalog. |
| `nameKey` | `string` |  | Khóa localization tên hero (sảnh, loadout, pause). Trống = hiện tên dự phòng. |
| `descKey` | `string` |  | Khóa localization mô tả hero ở bảng nhân vật. Trống = không hiện mô tả. |
| `role` | `CharacterRole` | Warrior | Vai trò hero: hiện nhãn / icon ở sảnh và kích hoạt hiệu ứng cộng hưởng của trang bị theo vai trò. |
| `unlockPrice` | `int` | 0 |  (≥ 0) |
| `released` | `bool` | ✓ | false = Chưa ra mắt: vẫn hiện ở sảnh (dáng tối + nhãn), xem được giới thiệu nhưng không mở khóa / không vào run. |
| `rank` | `Rarity` | C | Bậc hero (S cao nhất). Dùng cho hiển thị và cân bằng giá mở khóa. |
| `portrait` | `Sprite` |  | *[Hình]* Ảnh hero cho UI (sảnh, loadout, pause); trong run làm sprite đầu và mốc canh cỡ / vị trí hình Player. |
| `animator` | `RuntimeAnimatorController` |  | AnimatorOverrideController (hoặc controller riêng) gắn lên Visual của Player.prefab |
| `attackVfx` | `GameObject` |  | *[VFX đòn đánh]* Phát tại vũ khí mỗi lần hero ra đòn (thêm vào VFX riêng của vũ khí). Trống = không có. |
| `hitVfx` | `GameObject` |  | Phát tại điểm trúng mỗi khi đòn của hero trúng mục tiêu. Trống = không có. |
| `stats` | [StatBlock](#statblock) |  | *[Chỉ số]* StatProfile + ghi đè (máu, giáp, mana, sát thương, tốc chạy, tốc đánh, chí mạng, hồi chiêu, hồi mana, hồi giáp) |
| `startingWeapon` | [WeaponData](#weapondata) |  | *[Khởi đầu]* Vũ khí tặng khi mở khóa hero, gắn sẵn vào ô vũ khí 1 của loadout. Trống = không có vũ khí khởi đầu. |
| `skill` | `SkillId` | None | Skill nhân vật (chọn trong SkillLibrary) |
| `innatePassive` | [ItemPassiveSO](#itempassiveso) |  | Nội tại bẩm sinh kích hoạt suốt run (tùy chọn). |

**Asset (10)**: `Data/Characters/` Char_berserker, Char_frost-knight, Char_knight, Char_lancer, Char_monk, Char_necromancer, Char_paladin, Char_ranger, Char_shadow-knight, Char_wizard

### MonsterDefinitionSO

`MonsterDefinitionSO` — [`Scripts/Enemy/MonsterDefinitionSO.cs`](../Assets/Project/Scripts/Enemy/MonsterDefinitionSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Monster Definition`

Định nghĩa quái / boss: định danh, bậc (Normal / Elite / Boss), hình + animation, VFX đòn đánh, chỉ số (StatProfile + ghi đè), di chuyển, skill (SkillId), phase boss, rơi đồ. Quái thường dùng chung prefab Enemy_Base; định nghĩa áp hình / animator / kích thước lúc sinh.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `enemyId` | `string` | "skeleton-spear" | ID quái (chỉ dùng để nhận diện trong log / kiểm tra dữ liệu Editor). |
| `nameKey` | `string` |  | Khóa localization tên quái; boss dùng để hiện tên trên thanh máu boss. |
| `animator` | `RuntimeAnimatorController` |  | *[Hình]* Animator controller của quái (trống = giữ của prefab) |
| `sprite` | `Sprite` |  | Sprite mặc định (trống = giữ của prefab) |
| `visualScale` | `float` | 1 | Kích thước (scale gốc của quái; bậc Elite nhân thêm) (≥ 0.1) |
| `attackVfx` | `GameObject` |  | *[VFX đòn đánh]* Phát tại điểm ra đòn mỗi lần quái tung skill / đòn |
| `hitVfx` | `GameObject` |  | Phát trên mục tiêu mỗi khi đòn của quái trúng (Player) |
| `contactDamage` | `float` | 1 | *[Chạm & di chuyển]* Sát thương khi chạm Player (nhân AttackDamage của chỉ số) |
| `contactCooldown` | `float` | 0.8 | Thời gian chờ (giây) giữa 2 lần gây sát thương chạm lên Player. |
| `isFlying` | `bool` | ✗ | Bật = bay: bỏ qua NavMesh, di chuyển thẳng tới mục tiêu (xuyên chướng ngại). |
| `preferredDistance` | `float` | 0.6 | Khoảng cách giữ với Player khi đuổi (quái bắn xa nên > 0) |
| `retreatDistance` | `float` | 0 | Nếu > 0 và khoảng cách tới mục tiêu nhỏ hơn mức này, quái sẽ lùi lại (dành cho quái bắn xa) |
| `aggroRange` | `float` | 12 | Tầm phát hiện Player (world-unit) để bắt đầu đuổi; bỏ qua khi phòng đã kích hoạt (luôn đuổi). |
| `stats` | [StatBlock](#statblock) |  | *[Chỉ số]* StatProfile + ghi đè (máu, tốc chạy, kháng đẩy lùi…). AttackDamage = hệ số nhân sát thương mọi đòn |
| `tier` | `MonsterTier` | Normal | Bậc quái: hệ số chỉ số theo MonsterTierConfig (Elite có thể do spawner đặt lúc sinh). |
| `spawnCost` | `int` | 1 | *[Sinh quái]*  (≥ 1) |
| `spawnDuration` | `float` | 0.4 | Thời gian (giây) trạng thái xuất hiện sau khi sinh: chưa hành động, chưa gây sát thương chạm. |
| `deathDuration` | `float` | 0.8 | Thời gian (giây) chờ animation chết trước khi thu hồi quái (tối thiểu 0.1). |
| `skills` | `List<SkillId>` | [] | *[Tấn công]* Skill / đòn (chọn trong SkillLibrary) |
| `phases` | List<[Phase](#phase)> | [] | *[Boss Phases (tùy chọn)]* Phase boss theo ngưỡng máu; có phần tử thì ghi đè phase trên BossPhaseController của prefab. Trống = dùng của prefab. |
| `customLoot` | `bool` | ✗ | *[Rơi đồ]* Tắt (mặc định): rơi đồ theo bậc quái trong LootConfig. Bật: dùng bảng riêng bên dưới. |
| `lootTable` | [LootTable](#loottable) |  | Bảng rơi riêng, chỉ dùng khi bật customLoot (hoặc khi chưa có LootConfig); tắt thì LootConfig theo bậc quyết định. |

**Asset (18)**: `Data/Enemies/` Enemy_bone-captain, Enemy_crimson-warlord, Enemy_dread-executioner, Enemy_elite-vampire, Enemy_elite-warrior, Enemy_frost-acolyte, Enemy_frost-cryomancer, Enemy_frost-lich-king, Enemy_inferno-king, Enemy_iron-harvester, Enemy_plague-assassin, Enemy_shadow-stalker, Enemy_skeleton-spear, Enemy_skeleton-warrior, Enemy_skull, Enemy_vampire, Enemy_vampire-lord, Enemy_void-wraith


## Rơi đồ

### LootConfigSO

`LootConfigSO` — [`Scripts/Catalog/Loot/LootConfigSO.cs`](../Assets/Project/Scripts/Catalog/Loot/LootConfigSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Loot ▸ Loot Config`

Cấu hình rơi đồ của cả game (một asset): trọng số bậc D…S theo level, rơi đồ theo bậc quái, rương. Bậc vật phẩm = trọng số của level (level vượt danh sách thì dời tiếp `rankShiftPerExtraLevel`) dời thêm theo bậc quái + lootLuck của level. Vàng nhân thêm `goldPerLevel` mỗi level.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `rankWeightsByLevel` | List<[RankWeights](#rankweights)> | 3 phần tử | *[Bậc vật phẩm theo level (index 0 = level 1)]* Trọng số bậc D…S theo level (phần tử 0 = level 1). Level vượt danh sách dùng phần tử cuối + dời bậc. Trống = luôn D. |
| `rankShiftPerExtraLevel` | `float` | 0.5 | Level vượt danh sách: mỗi level dời thêm bấy nhiêu bậc |
| `goldPerLevel` | `float` | 0.25 | Vàng tăng thêm mỗi level (0.25 = +25% / level) |
| `tiers` | List<[TierLoot](#tierloot)> | 3 phần tử | *[Theo bậc quái]* Rơi đồ theo bậc quái (vàng, vật phẩm, mana, bình máu, dời bậc). Mỗi bậc một dòng. |
| `chestRankShift` | `float` | 0.5 | *[Rương]* Rương phòng thường / rương thưởng: dời bậc vật phẩm |

**Asset (1)**: `Data/Config/` LootConfig


## Level và map

### CampaignSO

`CampaignSO` — [`Scripts/Catalog/Levels/CampaignSO.cs`](../Assets/Project/Scripts/Catalog/Levels/CampaignSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Campaign`

Thứ tự level của một run. Số level = số phần tử (thay maxFloor cố định 3). Lưu address (không tham chiếu trực tiếp) để catalog không kéo theo prefab phòng / quái của mọi level vào cùng bundle.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `campaignId` | `string` | "dungeon" | ID campaign (định danh; hiện chưa được code runtime đọc). |
| `themeId` | `string` | "dungeon" | Theme của campaign (hiện chưa được đọc; runtime dùng themeId của session khi không có address). |
| `levelAddresses` | `List<string>` | [] | Address của LevelConfig theo thứ tự (level 1, 2, …) |

**Asset (1)**: `Data/DungeonConfigs/` Campaign_dungeon

### LevelConfigSO

`LevelConfigSO` — [`Scripts/Play/Dungeon/LevelConfigSO.cs`](../Assets/Project/Scripts/Play/Dungeon/LevelConfigSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Level Config`

Cấu hình một level (map): số phòng có quái, số phòng đặc biệt theo loại, cấu hình từng loại phòng (RoomTypeConfigSO), bảng quái + tỉ lệ Elite, boss, lootLuck, tile theo theme. Thứ tự level trong run do CampaignSO quyết định. Address: theme/&lt;id&gt;/config/floor-&lt;n&gt;.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `themeId` | `string` | "dungeon" | *[Level]* Theme của level; chỉ Editor dùng để đăng ký address theme/<themeId>/config/floor-<floor>. |
| `floor` | `int` | 1 | Số thứ tự level (hiện "Tầng N" trên HUD) |
| `nameKey` | `string` |  | Khóa localization tên level (hiện chưa được code runtime đọc). |
| `combatRoomCount` | `Vector2Int` | (0, 0) | *[Số phòng]* Số phòng có quái (min, max) — không tính Start / Boss / phòng đặc biệt |
| `specialRooms` | List<[SpecialRoomCount](#specialroomcount)> | [] | Phòng đặc biệt: mỗi phòng nằm ở cuối một nhánh |
| `roomTypes` | List<[RoomTypeConfigSO](#roomtypeconfigso)> | [] | Cấu hình theo loại phòng (template, đợt quái, Elite, rương). Phòng Elite thiếu cấu hình thì dùng template Combat |
| `eliteChance` | `float` | 0 | *[Độ khó & thưởng]* Tỉ lệ một con quái thường sinh ra thành Elite (khoảng 0–1) |
| `lootLuck` | `float` | 0 | Dời bậc vật phẩm rơi lên (map càng cao càng lớn) |
| `gridSize` | `Vector2Int` | (7, 7) | *[Lưới map]* Kích thước lưới ô phòng (số phòng tối đa theo chiều ngang / dọc) |
| `cellSpacing` | `int` | 40 | Khoảng cách giữa tâm 2 ô lưới (tile) — lớn hơn MaxRoomSize |
| `corridorFloorTile` | `TileBase` |  | *[Tile theo theme]* Tile sàn hành lang nối các phòng. Trống = không vẽ hành lang (kể cả tường). |
| `corridorWallTile` | `TileBase` |  | Tile tường hai bên hành lang. Trống = hành lang không có tường. |
| `sealWallTile` | `TileBase` |  | Tile bịt Door Socket không dùng |
| `corridorWidth` | `int` | 3 |  (≥ 1) |
| `sealDepth` | `int` | 2 |  (≥ 1) |
| `enemyTable` | List<[EnemySpawnEntry](#enemyspawnentry)> | [] | *[Quái]* Bảng quái của level: chọn ngẫu nhiên theo weight trong số quái có spawnCost vừa ngân sách. Trống = không sinh quái. |
| `spawnBudget` | `int` | 6 | Ngân sách điểm spawnCost của đợt đầu |
| `budgetGrowth` | `int` | 2 | Ngân sách tăng thêm mỗi đợt |
| `wavesPerRoom` | `Vector2Int` | (2, 3) | Số đợt quái mỗi phòng (min, max, gồm cả max); RoomTypeConfig có waves > 0 sẽ ghi đè. |
| `enemyHealthMultiplier` | `float` | 1 | Hệ số nhân máu gốc mọi quái của level (nhân thêm với hệ số bậc Elite/Boss; tối thiểu 0.1). |
| `spawnWarningTime` | `float` | 1 | Thời gian (giây) hiện dấu báo trước khi quái xuất hiện. |
| `bossAddress` | `string` |  | *[Boss]* Address boss (tải nền qua Addressables, label boss-<id>) |
| `bossLoadTimeout` | `float` | 20 | Thời gian tối đa (giây) chờ boss tải nền khi vào phòng boss; quá hạn thì thử tải lại một lần. |
| `chestOnClearChance` | `float` | 0.25 | *[Thưởng]*  (khoảng 0–1) |

**Asset (3)**: `Data/DungeonConfigs/` DungeonConfig_dungeon_F1, DungeonConfig_dungeon_F2, DungeonConfig_dungeon_F3

### RoomTypeConfigSO

`RoomTypeConfigSO` — [`Scripts/Play/Dungeon/RoomTypeConfigSO.cs`](../Assets/Project/Scripts/Play/Dungeon/RoomTypeConfigSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Room Type Config`

Cấu hình một loại phòng: template (prefab phòng vẽ sẵn — kích thước nằm trên RoomTemplate.size), số đợt quái, ngân sách quái, tỉ lệ Elite, tỉ lệ rương khi dọn xong. Dùng lại được cho nhiều level.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `roomType` | `RoomType` | Combat | Loại phòng mà cấu hình này áp dụng; LevelConfig lấy cấu hình đầu tiên khớp loại trong roomTypes. |
| `templates` | List<[RoomTemplate](#roomtemplate)> | [] | Prefab phòng chọn ngẫu nhiên (cùng loại). Kích thước phòng = RoomTemplate.size của từng prefab |
| `waves` | `Vector2Int` | (0, 0) | *[Quái (phòng Combat / Elite / Boss)]* Số đợt quái (min, max). (0, 0) = dùng wavesPerRoom của level |
| `spawnBudget` | `int` | 0 | Ngân sách điểm spawnCost của đợt đầu. 0 = dùng của level |
| `budgetGrowth` | `int` | -1 | Ngân sách tăng mỗi đợt. -1 = dùng của level |
| `eliteChanceBonus` | `float` | 0 | Cộng thêm vào tỉ lệ Elite của level (phòng Elite: 1 = mọi con là Elite) (khoảng 0–1) |
| `chestChance` | `float` | -1 | *[Thưởng]* Tỉ lệ rương khi dọn xong (0..1). Âm = dùng chestOnClearChance của level |

**Asset (6)**: `Data/RoomTypes/` RoomType_Boss, RoomType_Combat, RoomType_Elite, RoomType_Shop, RoomType_Start, RoomType_Treasure


## Bẫy

### TrapDataSO

`TrapDataSO` — [`Scripts/Play/Dungeon/Traps/TrapDataSO.cs`](../Assets/Project/Scripts/Play/Dungeon/Traps/TrapDataSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Trap Data`

Cấu hình chỉ số bẫy dạng ScriptableObject (giảm lặp dữ liệu trên prefab bẫy).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `trapId` | `string` |  | ID bẫy (định danh; hiện chưa được code runtime đọc). |
| `damage` | `float` | 1 | Sát thương mỗi lần trúng (mỗi mục tiêu tối đa 1 lần / chu kỳ kích hoạt). |
| `knockback` | `float` | 2 | Lực đẩy lùi mục tiêu ra xa tâm bẫy khi trúng (0 = không đẩy). |
| `inactiveTime` | `float` | 1.5 | *[Chu kỳ thời gian (Timed / Trigger)]* Bẫy Timed: thời gian (giây) nghỉ trước khi báo hiệu lần tiếp theo. |
| `warningTime` | `float` | 0.5 | Thời gian (giây) báo hiệu trước khi bẫy đâm lên (Timed và Trigger). |
| `activeTime` | `float` | 0.6 | Thời gian (giây) bẫy ở trạng thái gây sát thương (Timed và Trigger). |
| `cooldown` | `float` | 1 | Bẫy Trigger: thời gian (giây) hồi sau khi đâm trước khi có thể kích hoạt lại. |
| `onHitStatus` | [StatusPayload](#statuspayload) |  | *[Hiệu ứng khi trúng]* Hiệu ứng trạng thái áp lên mục tiêu khi bẫy gây sát thương thành công. Trống = không có. |
| `zoneEffects` | List<[StatusEffectRef](#statuseffectref)> | [] | *[Sàn nguyên tố (Hazard)]* Sàn nguyên tố: hiệu ứng làm mới liên tục khi đứng trên (thời gian = thời gian vương lại sau khi rời sàn) |

**Asset (3)**: `Data/Traps/` Trap_flame-hazard, Trap_spike-timed, Trap_spike-trigger


## Passive

### ItemPassiveSO

`ItemPassiveSO` *(abstract)* — [`Scripts/Catalog/ItemPassiveSO.cs`](../Assets/Project/Scripts/Catalog/ItemPassiveSO.cs)

Base chung cho passive vũ khí và trang bị.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `description` | `string` |  | Mô tả dạng văn bản thô, hiện trong UI khi descKey rỗng (dự phòng). |
| `descKey` | `string` |  | Key mô tả trong DG_Multilanguage; có giá trị thì được dùng thay cho description. |

### EquipmentPassiveSO

`EquipmentPassiveSO` : `ItemPassiveSO` *(abstract)* — [`Scripts/Catalog/EquipmentPassiveSO.cs`](../Assets/Project/Scripts/Catalog/EquipmentPassiveSO.cs)

Passive trang bị: kích hoạt khi mặc và đúng Role.

*Không có field riêng (dùng field của lớp cha).*

### LowHpBerserkPassive

`LowHpBerserkPassive` : `ItemPassiveSO` — [`Scripts/Combat/Passives/LowHpBerserkPassive.cs`](../Assets/Project/Scripts/Combat/Passives/LowHpBerserkPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Low HP Berserk`

Nội tại Berserk: HP dưới ngưỡng thì tăng sát thương và tốc đánh.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `hpThreshold` | `float` | 0.5 |  (khoảng 0.1–0.9) |
| `bonusAttackDamagePct` | `float` | 0.35 | % sát thương cộng thêm (0.35 = +35%, vào ΣPercent) khi tỉ lệ HP ≤ hpThreshold; 0 = không cộng. |
| `bonusAttackSpeedPct` | `float` | 0.25 | % tốc đánh cộng thêm (0.25 = +25%, vào ΣPercent) khi tỉ lệ HP ≤ hpThreshold; 0 = không cộng. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (1)**: `Data/Passives/` IP_low-hp-berserk

### WeaponPassiveSO

`WeaponPassiveSO` : `ItemPassiveSO` *(abstract)* — [`Scripts/Catalog/WeaponPassiveSO.cs`](../Assets/Project/Scripts/Catalog/WeaponPassiveSO.cs)

Passive vũ khí: kích hoạt khi đang CẦM vũ khí đó.

*Không có field riêng (dùng field của lớp cha).*

### ArmorBreakNovaPassive

`ArmorBreakNovaPassive` : `EquipmentPassiveSO` — [`Scripts/Combat/Passives/ArmorBreakNovaPassive.cs`](../Assets/Project/Scripts/Combat/Passives/ArmorBreakNovaPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Equip Armor Break Nova`

Giáp Lửa Kỵ Sĩ (Warrior): vỡ giáp → nổ vòng lửa đẩy lùi quái xung quanh.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `damage` | `float` | 20 | Hệ số sát thương vụ nổ, nhân với AttackDamage của người chơi; nổ khi giáp về 0 sau đòn không chí tử. |
| `radius` | `float` | 2.5 | Bán kính vụ nổ (world unit) quanh người chơi; quái trong vòng bị trúng. |
| `knockback` | `float` | 10 | Lực đẩy lùi quái ra xa tâm vụ nổ; 0 = không đẩy. |
| `vfx` | `GameObject` |  | Prefab VFX phát tại người chơi khi nổ; null = không hiệu ứng (vẫn gây sát thương). |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (1)**: `Data/EquipmentPassives/` EP_fire-nova

### CritRewardPassive

`CritRewardPassive` : `EquipmentPassiveSO` — [`Scripts/Combat/Passives/CritRewardPassive.cs`](../Assets/Project/Scripts/Combat/Passives/CritRewardPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Equip Crit Reward`

Găng Tay Bóng Đêm (Assassin): chí mạng hồi mana + tăng tốc chạy ngắn. Buff tốc chạy KHÔNG cộng dồn: chí mạng khi đang có buff chỉ làm mới thời gian.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `mana` | `float` | 2 | Mana hồi mỗi lần đòn chí mạng trúng (kể cả khi buff tốc chạy đang chạy); 0 = không hồi. |
| `moveSpeedPercent` | `float` | 0.2 |  (khoảng 0–2) |
| `buffDuration` | `float` | 1.5 | Thời gian (giây) buff tốc chạy sau chí mạng; chí mạng tiếp chỉ làm mới thời gian, không cộng dồn. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (1)**: `Data/EquipmentPassives/` EP_night-crit

### EquipStatBonusPassive

`EquipStatBonusPassive` : `EquipmentPassiveSO` — [`Scripts/Combat/Passives/EquipStatBonusPassive.cs`](../Assets/Project/Scripts/Combat/Passives/EquipStatBonusPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Equip Stat Bonus`

Trang bị chỉ cộng chỉ số đặc biệt khi đúng Role (ngoài baseModifiers).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `modifiers` | List<[StatModifier](#statmodifier)> | [] | Chỉ số cộng thêm khi đúng Role (ngoài baseModifiers), theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (20)**: `Data/EquipmentPassives/` EP_arcane-pendant, EP_archmage-robe, EP_blood-necklace, EP_dragon-crown, EP_gwhite-bonus, EP_hat-bonus, EP_horn-bonus, EP_iron-amulet, EP_mask-bonus, EP_neck-bonus, EP_phantom-cloak, EP_shield-bonus · `Data/Passives/` IP_innate-assassin, IP_innate-mage, IP_innate-ranger, IP_innate-warrior, IP_set-assassin, IP_set-mage, IP_set-ranger, IP_set-warrior

### EveryNthHitPassive

`EveryNthHitPassive` : `EquipmentPassiveSO` — [`Scripts/Combat/Passives/EveryNthHitPassive.cs`](../Assets/Project/Scripts/Combat/Passives/EveryNthHitPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Equip Every Nth Hit`

Sách Cổ Ngữ (Mage): mỗi đòn trúng thứ N triệu hồi vòng xoáy phép tại mục tiêu.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `everyN` | `int` | 3 |  (≥ 1) |
| `damage` | `float` | 15 | Hệ số sát thương vòng xoáy, nhân với AttackDamage của người chơi; trúng mọi quái trong radius. |
| `radius` | `float` | 1.5 | Bán kính vòng xoáy (world unit) quanh mục tiêu của đòn trúng thứ N. |
| `vfx` | `GameObject` |  | Prefab VFX phát tại mục tiêu khi vòng xoáy kích hoạt; null = không hiệu ứng. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (1)**: `Data/EquipmentPassives/` EP_book-nth

### HookWeaponPassive

`HookWeaponPassive` : `WeaponPassiveSO` *(abstract)* — [`Scripts/Combat/Passives/HookWeaponPassive.cs`](../Assets/Project/Scripts/Combat/Passives/HookWeaponPassive.cs)

Base passive dựa trên hook chiến đấu. Handler + state riêng từng Player nằm trong `PassiveRuntime` (SO là asset dùng chung, không giữ state) để OnUnequip gỡ đúng và không rò state giữa các lần Play.

*Không có field riêng (dùng field của lớp cha).*

### StillnessEmpowerPassive

`StillnessEmpowerPassive` : `EquipmentPassiveSO` — [`Scripts/Combat/Passives/StillnessEmpowerPassive.cs`](../Assets/Project/Scripts/Combat/Passives/StillnessEmpowerPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Equip Stillness Empower`

Giày Phong Vũ (Ranger): đứng yên N giây → đòn kế tiếp xuyên thấu + tăng sát thương.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `stillTime` | `float` | 1 | Số giây đứng yên (không di chuyển, không bị choáng) để nạp đòn cường hóa; tấn công sẽ đếm lại. |
| `damageMultiplier` | `float` | 1.4 | Hệ số nhân sát thương cho đòn kế tiếp sau khi nạp (1.4 = +40%); 1 = không tăng. |
| `extraPierce` | `int` | 3 | Số mục tiêu xuyên thêm của đòn cường hóa kế tiếp; 0 = không xuyên thêm. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (1)**: `Data/EquipmentPassives/` EP_wind-still

### WeaponStatBonusPassive

`WeaponStatBonusPassive` : `WeaponPassiveSO` — [`Scripts/Combat/Passives/WeaponStatBonusPassive.cs`](../Assets/Project/Scripts/Combat/Passives/WeaponStatBonusPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Weapon Stat Bonus`

Cộng chỉ số khi đang cầm vũ khí (vd +20% knockback quy về AttackDamage...).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `modifiers` | List<[StatModifier](#statmodifier)> | [] | Chỉ số cộng khi đang trang bị vũ khí này, theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (3)**: `Data/WeaponPassives/` WP_rifle-crit, WP_thorn-bonus, WP_void-bonus

### ChainLightningPassive

`ChainLightningPassive` : `HookWeaponPassive` — [`Scripts/Combat/Passives/ChainLightningPassive.cs`](../Assets/Project/Scripts/Combat/Passives/ChainLightningPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Weapon Chain Lightning`

Sét lan: xác suất giật thêm N mục tiêu kế cận.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `chance` | `float` | 0.2 |  (khoảng 0–1) |
| `jumps` | `int` | 2 |  (≥ 1) |
| `radius` | `float` | 3 | Bán kính (world unit) tìm quái gần nhất cho mỗi bước sét lan; không còn quái trong vòng = dừng lan. |
| `damageFraction` | `float` | 0.5 |  (khoảng 0–2) |
| `vfx` | `GameObject` |  | Prefab VFX phát tại mỗi quái bị sét lan trúng; null = không hiệu ứng. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (1)**: `Data/WeaponPassives/` WP_thunder-chain

### LifestealPassive

`LifestealPassive` : `HookWeaponPassive` — [`Scripts/Combat/Passives/LifestealPassive.cs`](../Assets/Project/Scripts/Combat/Passives/LifestealPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Weapon Lifesteal`

Hút máu: tích lũy % sát thương, đủ 1 HP thì hồi. Phần tích lũy nằm trong PassiveState (theo Player).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `percent` | `float` | 0.04 |  (khoảng 0–1) |
| `damagePerHp` | `float` | 25 | HP của Player nhỏ (kiểu Soul Knight): quy đổi sát thương → HP |
| `vfx` | `GameObject` |  | Prefab VFX gắn theo người chơi, phát mỗi lần hút máu thực sự hồi HP; null = không hiệu ứng. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (2)**: `Data/Passives/` IP_buff-lifesteal · `Data/WeaponPassives/` WP_holy-lifesteal

### ManaOnKillPassive

`ManaOnKillPassive` : `HookWeaponPassive` — [`Scripts/Combat/Passives/ManaOnKillPassive.cs`](../Assets/Project/Scripts/Combat/Passives/ManaOnKillPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Weapon Mana On Kill`

Hạ quái hồi mana.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `mana` | `float` | 3 | Mana hồi mỗi khi một đòn trúng của người chơi hạ gục quái; 0 = không hồi. |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (2)**: `Data/WeaponPassives/` WP_dark-mana, WP_gold-mana

### OnHitStatusPassive

`OnHitStatusPassive` : `HookWeaponPassive` — [`Scripts/Combat/Passives/OnHitStatusPassive.cs`](../Assets/Project/Scripts/Combat/Passives/OnHitStatusPassive.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Passives ▸ Weapon On-Hit Status`

Trúng đòn có xác suất gây làm chậm / thiêu đốt / độc / xuất huyết / choáng.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `chance` | `float` | 1 |  (khoảng 0–1) |
| `status` | [StatusPayload](#statuspayload) |  | Trạng thái gây lên mục tiêu còn sống khi trúng đòn và qua xác suất chance; rỗng = không gây gì. |
| `hitVfx` | `GameObject` |  | Prefab VFX phát tại mục tiêu mỗi lần gây trạng thái; null = không (VFX lặp của trạng thái vẫn hiện). |

*Cộng thêm field của: [ItemPassiveSO](#itempassiveso).*

**Asset (4)**: `Data/WeaponPassives/` WP_fire-burn, WP_ice-slow, WP_lava-burn, WP_venom-poison


## Vật phẩm, trang bị, buff

### EquipmentSetSO

`EquipmentSetSO` — [`Scripts/Catalog/EquipmentSetSO.cs`](../Assets/Project/Scripts/Catalog/EquipmentSetSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Equipment Set`

Bộ trang bị: mặc đủ 2 món kích hoạt cộng chỉ số, đủ 4 món kích hoạt nội tại.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `setId` | `string` |  | Id ngắn định danh bộ set (kebab-case); hiện runtime không đọc field này. |
| `nameKey` | `string` |  | Key tên bộ set trong bảng DG_Multilanguage (hiện chưa có UI đọc). |
| `pieces` | List<[EquipmentSO](#equipmentso)> | [] | Danh sách món thuộc set (chỉ tham khảo); runtime đếm set qua field equipmentSet của từng EquipmentSO. |
| `bonus2Pieces` | List<[StatModifier](#statmodifier)> | [] | Chỉ số cộng khi mặc từ 2 món cùng set trở lên; rỗng = không có thưởng 2 món. |
| `bonus4Passive` | [ItemPassiveSO](#itempassiveso) |  | Nội tại bật khi mặc từ 4 món cùng set trở lên; null = không có thưởng 4 món. |

**Asset (4)**: `Data/EquipmentSets/` Set_archmage, Set_crimson-warrior, Set_shadow-assassin, Set_wind-ranger

### ItemData

`ItemData` *(abstract)* — [`Scripts/Catalog/ItemData.cs`](../Assets/Project/Scripts/Catalog/ItemData.cs)

Base của WeaponData / EquipmentSO. id ngắn là khóa chung (save, catalog, i18n).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `id` | `string` |  | *[Định danh]* "sword-ice" |
| `rarity` | `Rarity` |  | Bậc hiếm (S cao nhất → D thấp nhất): lọc rơi đồ/shop và màu ô. Vũ khí lấy bậc từ entry thư viện, field này chỉ dự phòng. |
| `icon` | `Sprite` |  | ô 16×16 hoặc kiếm 32×32 |
| `nameKey` | `string` |  | key DG_Multilanguage |
| `descKey` | `string` |  | Key mô tả trong bảng DG_Multilanguage; rỗng = không có mô tả. Vũ khí ưu tiên descKey của entry thư viện. |
| `activeSkill` | [ActiveSkillSO](#activeskillso) |  | Kỹ năng chủ động (tùy chọn) — tính vào giới hạn 2 kỹ năng kích hoạt. |
| `shopPrice` | `int` |  |  (≥ 0) |

### RunBuffSO

`RunBuffSO` — [`Scripts/Catalog/RunBuffSO.cs`](../Assets/Project/Scripts/Catalog/RunBuffSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Run Buff`

Buff trong run (chỉ số + nội tại tùy chọn) tồn tại hết các tầng của run.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `buffId` | `string` |  | Id ngắn định danh buff trong run (kebab-case như id vật phẩm). |
| `nameKey` | `string` |  | Key tên buff trong bảng DG_Multilanguage. |
| `descKey` | `string` |  | Key mô tả buff trong bảng DG_Multilanguage. |
| `icon` | `Sprite` |  | Icon hiển thị của buff; null = không có icon. |
| `rarity` | `Rarity` | B | Bậc hiếm của buff (S cao nhất → D thấp nhất), dữ liệu hiển thị; Apply không dùng. |
| `statModifiers` | List<[StatModifier](#statmodifier)> | [] | Chỉ số cộng khi nhận buff, giữ tới hết run, theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng. |
| `passive` | [ItemPassiveSO](#itempassiveso) |  | Nội tại tùy chọn bật khi nhận buff và tắt khi gỡ buff; null = buff chỉ có chỉ số. |

**Asset (4)**: `Data/Buffs/` Buff_arcane-surge, Buff_iron-will, Buff_swift-boots, Buff_vampiric-fangs

### EquipmentSO

`EquipmentSO` : `ItemData` — [`Scripts/Catalog/EquipmentSO.cs`](../Assets/Project/Scripts/Catalog/EquipmentSO.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Equipment`

Trang bị: ô, bậc, chỉ số (modifier) cho mọi Role, hiệu ứng khi trúng (StatusEffectRef), passive khi đúng Role, bộ set — cấu hình trọn trong SO.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `slot` | `EquipmentSlotType` | Helmet | Ô trang bị món này chiếm; mỗi ô chỉ mặc 1 món, mặc món mới cùng ô sẽ thay món cũ. |
| `baseModifiers` | List<[StatModifier](#statmodifier)> | [] | Chỉ số cộng cho mọi Role khi mặc, theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng chỉ số. |
| `synergyRole` | `CharacterRole` | Any | Role kích hoạt synergyPassive; Any = mọi nhân vật đều kích hoạt. |
| `synergyPassive` | [EquipmentPassiveSO](#equipmentpassiveso) |  | Nội tại chỉ bật khi nhân vật đúng synergyRole; null = trang bị không có nội tại. |
| `equipmentSet` | [EquipmentSetSO](#equipmentsetso) |  | Bộ set chứa món này; mặc đủ 2 / 4 món cùng set kích hoạt thưởng. null = không thuộc set nào. |
| `onHitStatus` | List<[StatusEffectRef](#statuseffectref)> | [] | Hiệu ứng gây lên mục tiêu mỗi khi đòn của người mặc trúng (vd Slow 10%, Burn) — mọi Role |

*Cộng thêm field của: [ItemData](#itemdata).*

**Asset (19)**: `Data/Equipment/` Equip_arcane-pendant, Equip_archmage-robe, Equip_blood-necklace, Equip_dragon-crown, Equip_fire-armor, Equip_gold-ring, Equip_holy-shield, Equip_horn, Equip_ice-mask, Equip_iron-amulet, Equip_mage-hat, Equip_night-gloves, Equip_phantom-cloak, Equip_red-boots, Equip_rune-book, Equip_silver-armor, Equip_silver-gloves, Equip_wind-boots, Equip_wind-necklace


## Hạ tầng (Core)

### LocalizationFontConfig

`LocalizationFontConfig` — [`Core/Scripts/Localization/LocalizationFontConfig.cs`](../Assets/Project/Core/Scripts/Localization/LocalizationFontConfig.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Localization Font Config`

Font TMP theo từng locale (giống LocalizationFontManager của Stickman). EN: Pixelta · VI: dearpix (font pixel có đủ dấu tiếng Việt). Runtime nạp qua `SetInstance` (Bootstrap / Addressables Core_Local); Editor fallback từ Core/Data.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `entries` | List<[LocaleFontEntry](#localefontentry)> | [] | Font TMP theo từng locale; locale không có entry (hoặc thiếu font) sẽ dùng fallbackFont. |
| `fallbackFont` | `TMP_FontAsset` |  | Font dùng khi locale không có entry hay entry thiếu font; null = giữ font gốc của label. |

**Asset (1)**: `Core/Data/` LocalizationFontConfig

### ModuleDefinition

`ModuleDefinition` — [`Core/Scripts/Modules/ModuleDefinition.cs`](../Assets/Project/Core/Scripts/Modules/ModuleDefinition.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Module Definition`

Một module tải được (MODULES §7.6).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `moduleId` | `string` |  | "lobby" |
| `sceneAddress` | `string` |  | "scene/lobby" (rỗng nếu module chỉ có prefab) |
| `requiredLabels` | string[] | [] | tải xong mới vào |
| `prefetchLabels` | string[] | [] | tải nền sau khi vào |
| `screenAddresses` | string[] | [] | tài liệu hóa |
| `showBackdrop` | `bool` | ✗ | menu = true, lobby/play = false (UI_SYSTEM §5.1) |
| `bgmAddress` | `string` |  | nhạc nền module (tùy chọn) |
| `gameState` | `GameState` | Menu | Trạng thái game (GameState) được đặt khi SceneService chuyển vào module này. |

**Asset (3)**: `Core/Data/Modules/` Module_lobby, Module_menu, Module_play

### ModuleRegistry

`ModuleRegistry` — [`Core/Scripts/Modules/ModuleRegistry.cs`](../Assets/Project/Core/Scripts/Modules/ModuleRegistry.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Module Registry`

Danh sách module; SceneService.Go tra bảng này theo sceneAddress.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `modules` | List<[ModuleDefinition](#moduledefinition)> | [] | Danh sách module; SceneService tra theo sceneAddress, không thấy thì tạo định nghĩa mặc định (fallback). |

**Asset (1)**: `Core/Data/` ModuleRegistry

### SfxLibrary

`SfxLibrary` — [`Core/Scripts/Audio/SfxLibrary.cs`](../Assets/Project/Core/Scripts/Audio/SfxLibrary.cs) · tạo: `Create ▸ Dungeon Gathering ▸ Audio ▸ SFX Library`

Bảng SfxId → clip + luật phát. Dựng tự động bằng menu `Dungeon Gathering/Audio/Rebuild SFX Library` (quét Audio/SFX), chỉnh tay được trong Inspector. Lưu tại `Assets/Project/Core/Data/SfxLibrary.asset` (group Core_Local, key `AddressKeys.CoreSfxLibrary`).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `entries` | List<[Entry](#entry)> | [] | Bảng SfxId → clip + luật phát; id không có entry thì không phát âm. |

**Asset (1)**: `Core/Data/` SfxLibrary


## Kiểu nhúng (class / struct dùng trong field)

Không phải asset riêng — nằm bên trong field của SO. Sửa trong Inspector của SO chứa nó.

### BaseStats

struct `BaseStats` — [`Scripts/Catalog/Stats.cs`](../Assets/Project/Scripts/Catalog/Stats.cs)

Chỉ số gốc của nhân vật (HeroDefinitionSO).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `maxHp` | `float` | 0 | Máu tối đa (điểm HP) |
| `maxArmor` | `float` | 0 | Giáp tối đa: đỡ sát thương trước HP, tự hồi sau khi không bị đánh một lúc |
| `maxMana` | `float` | 0 | Mana tối đa; vũ khí / skill tốn mana trừ vào đây |
| `attackDamage` | `float` | 0 | Hệ số sát thương (1 = 100%). |
| `attackSpeed` | `float` | 0 | Hệ số tốc đánh (1 = 100%). |
| `moveSpeed` | `float` | 0 | Tốc chạy (world-unit / giây) |
| `critChance` | `float` | 0 |  (khoảng 0–1) |
| `critDamage` | `float` | 0 | Hệ số sát thương chí mạng (2 = 200%). |
| `cooldownReduction` | `float` | 0 | Giảm hồi chiêu (0.1 = 10%, tối đa 50%). (khoảng 0–0.5) |
| `manaRegen` | `float` | 0 | Mana tự hồi mỗi giây. |
| `knockbackResist` | `float` | 0 | Giảm đẩy lùi nhận vào (0 = bị đẩy đủ, 1 = miễn). (khoảng 0–1) |

### EnemySpawnEntry

class `EnemySpawnEntry` — [`Scripts/Play/Dungeon/LevelConfigSO.cs`](../Assets/Project/Scripts/Play/Dungeon/LevelConfigSO.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `monster` | [MonsterDefinitionSO](#monsterdefinitionso) |  | Định nghĩa quái — sinh bằng prefab nền dùng chung PlayPrefabSet.monsterBase |
| `weight` | `int` | 10 |  (≥ 0) |
| `prewarm` | `int` | 4 | Số instance tạo trước khi vào tầng (≥ 0) |

### Entry

class `Entry` — [`Core/Scripts/Audio/SfxLibrary.cs`](../Assets/Project/Core/Scripts/Audio/SfxLibrary.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `id` | `SfxId` | None | SfxId mà entry này phục vụ; nếu trùng id thì chỉ entry đầu tiên được dùng. |
| `clips` | AudioClip[] | [] | Nhiều clip = biến thể, chọn ngẫu nhiên (không lặp liền). |
| `volume` | `float` | 1 |  (khoảng 0–1) |
| `pitchJitter` | `float` | 0.04 |  (khoảng 0–0.3) |
| `cooldown` | `float` | 0.05 | Khoảng tối thiểu (giây, thời gian thực) giữa 2 lần phát — chống spam khi nhiều sự kiện dồn 1 frame. (≥ 0) |
| `maxVoices` | `int` | 2 | Số voice tối đa cùng lúc cho id này; vượt → cướp voice cũ nhất của chính nó. (≥ 1) |
| `priority` | `int` | 50 | Cao = quan trọng. Khi hết voice, âm ưu tiên thấp hơn sẽ bị cướp. (khoảng 0–100) |
| `positional` | `bool` | ✓ | PlayAt: lệch trái/phải theo vị trí trên màn hình. |

### LocaleFontEntry

class `LocaleFontEntry` — [`Core/Scripts/Localization/LocalizationFontConfig.cs`](../Assets/Project/Core/Scripts/Localization/LocalizationFontConfig.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `localeCode` | `string` | "en" | Mã ngôn ngữ (en, vi), so khớp với ngôn ngữ hiện tại sau khi chuẩn hóa. |
| `fontAsset` | `TMP_FontAsset` |  | Font TMP cho locale này; null = dùng fallbackFont. |
| `sizeScale` | `float` | 1 | Scale cỡ chữ cho font này (font pixel khác nhau có chiều cao khác nhau). |

### LootTable

class `LootTable` — [`Scripts/Catalog/LootTable.cs`](../Assets/Project/Scripts/Catalog/LootTable.cs)

Bảng rơi đồ (quái, rương). Prefab hạt loot do LootSpawner (Play) giữ; bảng chỉ chứa số liệu.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `minGold` | `int` | 1 |  (≥ 0) |
| `maxGold` | `int` | 3 |  (≥ 0) |
| `manaOrbChance` | `float` | 0.3 |  (khoảng 0–1) |
| `manaOrbs` | `int` | 1 |  (≥ 0) |
| `healthPotionChance` | `float` | 0.02 |  (khoảng 0–1) |
| `itemChance` | `float` | 0 | Xác suất rơi vũ khí/trang bị (rarity theo tầng) (khoảng 0–1) |
| `weapons` | `bool` | ✓ | Cho phép vật phẩm rơi là vũ khí. |
| `equipment` | `bool` | ✓ | Cho phép vật phẩm rơi là trang bị. |

### MeleeHitbox

struct `MeleeHitbox` — [`Scripts/Catalog/Weapons/WeaponSpec.cs`](../Assets/Project/Scripts/Catalog/Weapons/WeaponSpec.cs)

Vùng trúng của đòn cận chiến. offset/size theo trục hướng ngắm (x = phía trước, y = ngang).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `shape` | `HitboxShape` | Auto | Auto = khớp vệt chém (Thrust → hộp hẹp); Arc = quạt bán kính size.x; Box = hộp size.x × size.y |
| `offset` | `Vector2` | (0, 0) | Dời gốc vùng trúng từ tay cầm (world-unit; x = phía trước, y = ngang). Arc / Box mới dùng |
| `size` | `Vector2` | (0, 0) | Arc: x = bán kính; Box: x = dài, y = rộng (world-unit). Auto không dùng |
| `arc` | `float` | 0 | Góc quạt (độ) — chỉ dùng với Arc |

### Phase

class `Phase` — [`Scripts/Enemy/BossPhaseController.cs`](../Assets/Project/Scripts/Enemy/BossPhaseController.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `hpThreshold` | `float` | 0.6 | Vào phase khi máu ≤ ngưỡng này (tỉ lệ) (khoảng 0–1) |
| `speedMultiplier` | `float` | 1.2 | Hệ số nhân tốc chạy của boss trong phase này (1 = giữ nguyên). |
| `skills` | `List<SkillId>` | [] | Skill / đòn của phase (chọn trong SkillLibrary) |
| `transitionVfx` | `GameObject` |  | VFX phát tại vị trí boss khi vào phase. Trống = không có. |
| `transitionInvulnerable` | `float` | 0.8 | Bất tử trong lúc chuyển phase |

### ProjectileEntry

class `ProjectileEntry` — [`Scripts/Catalog/Weapons/WeaponEntries.cs`](../Assets/Project/Scripts/Catalog/Weapons/WeaponEntries.cs)

Một loại đạn: prefab (có component Projectile) + thông số mặc định. Vũ khí chọn theo id.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `id` | `string` |  | Id đạn để vũ khí tham chiếu (projectileId / waveProjectileId / defaultProjectileId) |
| `prefab` | `GameObject` |  | Prefab đạn (cần component Projectile). Trống → súng bắn Hitscan, kiếm không ra kiếm khí |
| `speed` | `float` | 12 | Tốc độ đạn (world-unit / giây); entry vũ khí có thể ghi đè |
| `pierce` | `int` | 0 | Số mục tiêu xuyên thêm sau mục tiêu đầu; 0 = biến mất khi trúng |
| `explosionRadius` | `float` | 0 | Bán kính nổ (world-unit) khi trúng hoặc hết tầm; 0 = không nổ |
| `hitVfx` | `GameObject` |  | VFX khi đạn trúng; thắng hitImpactVfx của SO gốc, thua hitVfx của entry vũ khí. Trống → giữ VFX của SO gốc |
| `statusEffects` | List<[StatusEffectRef](#statuseffectref)> | [] | Hiệu ứng gây khi đạn trúng (cộng thêm vào hiệu ứng của SO gốc và entry vũ khí) |

### RangedEntry

class `RangedEntry` — [`Scripts/Catalog/Weapons/WeaponEntries.cs`](../Assets/Project/Scripts/Catalog/Weapons/WeaponEntries.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `animator` | `RuntimeAnimatorController` |  | *[Tầm xa]* Animator của súng (trigger "Fire" khi bắn) — tùy chọn |
| `muzzleOffset` | `Vector2` | (0.5, 0) | Vị trí đầu nòng so với tay cầm (world-unit, x = phía trước); đạn và VFX nòng xuất phát ở đây |
| `projectileId` | `string` |  | Id trong ProjectileLibrary; trống = đạn mặc định của SO gốc |
| `fireMode` | `Overridable<RangedFireMode>` | tắt | Kiểu bắn Projectile / Hitscan. Bỏ tick = SO gốc |
| `spread` | `Overridable<float>` | tắt | Độ lệch (độ): nhiều viên chia đều trong ±spread, 1 viên lệch ngẫu nhiên. Bỏ tick = SO gốc |
| `projectileCount` | `Overridable<int>` | tắt | Số viên mỗi phát (bắn chùm, tối thiểu 1). Bỏ tick = SO gốc |
| `burstCount` | `Overridable<int>` | tắt | Số phát liên tiếp mỗi lần bắn (tối thiểu 1). Bỏ tick = SO gốc |
| `burstInterval` | `Overridable<float>` | tắt | Giãn cách giữa các phát trong loạt (giây, tối thiểu 0.02). Bỏ tick = SO gốc |
| `projectileSpeed` | `Overridable<float>` | tắt | Tốc độ đạn (world-unit / giây). Bỏ tick = speed của đạn đã chọn |
| `pierce` | `Overridable<int>` | tắt | Số mục tiêu đạn xuyên thêm. Bỏ tick = theo đạn đã chọn |
| `explosionRadius` | `Overridable<float>` | tắt | Bán kính nổ (world-unit), 0 = không nổ. Bỏ tick = theo đạn đã chọn |
| `recoilDistance` | `Overridable<float>` | tắt | Độ giật lùi mỗi phát (world-unit). Bỏ tick = SO gốc |
| `muzzleVfx` | `GameObject` |  | VFX đầu nòng riêng. Trống = của SO gốc |

### RankWeights

struct `RankWeights` — [`Scripts/Catalog/Loot/LootConfigSO.cs`](../Assets/Project/Scripts/Catalog/Loot/LootConfigSO.cs)

Trọng số bậc vật phẩm: D, C, B, A, S = trọng số tương đối của từng bậc (D thấp nhất, S cao nhất; số thực ≥ 0, không cần cộng đủ 100 — xác suất bậc = trọng số / tổng; tổng 0 → luôn D). `Shift` dời trọng số lên bậc cao hơn: 1 = dời trọn một bậc (D→C, C→B…, S giữ ở S), 0.5 = dời một nửa.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `D` | `float` | 0 | Trọng số tương đối của bậc (D thấp → S cao); xác suất = trọng số / tổng 5 bậc. |
| `C` | `float` | 0 | Trọng số tương đối của bậc (D thấp → S cao); xác suất = trọng số / tổng 5 bậc. |
| `B` | `float` | 0 | Trọng số tương đối của bậc (D thấp → S cao); xác suất = trọng số / tổng 5 bậc. |
| `A` | `float` | 0 | Trọng số tương đối của bậc (D thấp → S cao); xác suất = trọng số / tổng 5 bậc. |
| `S` | `float` | 0 | Trọng số tương đối của bậc (D thấp → S cao); xác suất = trọng số / tổng 5 bậc. |

### SkillEntry

class `SkillEntry` — [`Scripts/Catalog/Skills/SkillLibrarySO.cs`](../Assets/Project/Scripts/Catalog/Skills/SkillLibrarySO.cs)

Một skill trong thư viện: key + số enum cố định + SO hành vi + ghi đè thời gian hồi / mana + VFX / âm thanh khi kích hoạt. Hero, quái, boss chỉ chọn `SkillId`.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `key` | `string` |  | Key định danh (chữ thường, gạch ngang). Tên enum SkillId sinh từ key này. |
| `value` | `int` | 0 | Số của enum SkillId — cố định, do menu Generate SkillId cấp. Không sửa tay. |
| `skill` | [SkillBaseSO](#skillbaseso) |  | SO hành vi (ActiveSkillSO cho hero / vật phẩm, EnemyAttackSO cho quái / boss) kèm tham số mặc định |
| `cooldown` | `Overridable<float>` | tắt | *[Ghi đè]* Thời gian hồi (giây). Bỏ tick = cooldown của SO hành vi |
| `manaCost` | `Overridable<float>` | tắt | Chỉ áp cho skill của hero / vật phẩm |
| `castVfx` | `GameObject` |  | *[Hiệu ứng khi kích hoạt]* VFX phát tại người dùng skill lúc kích hoạt (ngoài VFX riêng của SO hành vi) |
| `castSfx` | `AudioClip` |  | Âm phát khi dùng skill (hero và quái/boss), thêm vào âm riêng của SO hành vi. Trống → không phát |

### SpecialRoomCount

struct `SpecialRoomCount` — [`Scripts/Play/Dungeon/LevelConfigSO.cs`](../Assets/Project/Scripts/Play/Dungeon/LevelConfigSO.cs)

Số phòng đặc biệt của một loại trong level (Treasure, Shop, Elite…).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `type` | `RoomType` | Start | Loại phòng đặc biệt (Treasure, Shop, Elite…). Start / Boss / Combat bị bỏ qua. |
| `count` | `int` | 0 |  (≥ 0) |

### StatBlock

class `StatBlock` — [`Scripts/Catalog/Stats/StatProfileSO.cs`](../Assets/Project/Scripts/Catalog/Stats/StatProfileSO.cs)

"Profile + ghi đè": hero/quái chỉ chọn một `StatProfileSO` rồi ghi đè vài chỉ số. Thiếu profile → dùng chỉ số dự phòng do nơi gọi truyền vào (dữ liệu cũ).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `profile` | [StatProfileSO](#statprofileso) |  | Bảng chỉ số gốc. Trống → dùng chỉ số dự phòng do nơi gọi truyền vào (dữ liệu cũ) |
| `overrides` | List<[StatOverride](#statoverride)> | [] | Ghi đè vài chỉ số của profile (giá trị tuyệt đối, áp theo thứ tự) |

### StatModifier

struct `StatModifier` — [`Scripts/Catalog/Stats.cs`](../Assets/Project/Scripts/Catalog/Stats.cs)

Chỉ số cộng: final = (base + ΣFlat) × (1 + ΣPercent). PercentValue 0.1 = +10%.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `Stat` | `StatType` | MaxHp | Chỉ số bị tác động |
| `FlatValue` | `float` | 0 | Cộng thẳng vào chỉ số gốc trước khi nhân %: final = (base + ΣFlat) × (1 + ΣPct) |
| `PercentValue` | `float` | 0 | Phần trăm cộng dồn, áp sau Flat: 0.1 = +10%, -0.3 = -30% |

### StatOverride

struct `StatOverride` — [`Scripts/Catalog/Stats/StatProfileSO.cs`](../Assets/Project/Scripts/Catalog/Stats/StatProfileSO.cs)

Ghi đè một chỉ số gốc (giá trị tuyệt đối, không phải cộng thêm).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `stat` | `StatType` | MaxHp | Chỉ số cần ghi đè |
| `value` | `float` | 0 | Giá trị gốc mới (tuyệt đối), thay hẳn giá trị trong profile chứ không cộng thêm |

### StatusEffectRef

struct `StatusEffectRef` — [`Scripts/Catalog/Status/StatusEffectSO.cs`](../Assets/Project/Scripts/Catalog/Status/StatusEffectSO.cs)

Gây một hiệu ứng với tham số ghi đè tùy chọn. Ví dụ skill A: Stun + thời gian 1.5s; skill B: Stun + 3s.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `effect` | [StatusEffectSO](#statuseffectso) |  | Asset hiệu ứng cần gây. Trống → không gây gì |
| `duration` | `Overridable<float>` | tắt | Thời gian hiệu lực (giây). Bỏ tick = defaultDuration của hiệu ứng |
| `magnitude` | `Overridable<float>` | tắt | Độ mạnh tùy loại: Slow 0.3 = chậm 30%, DoT = sát thương / giây, Buff = hệ số nhân. Bỏ tick = defaultMagnitude |
| `chance` | `float` | 0 | Xác suất gây (1 = luôn) (khoảng 0–1) |
| `loopVfxOverride` | `GameObject` |  | Ghi đè VFX lặp của hiệu ứng |

### StatusPayload

struct `StatusPayload` — [`Scripts/Combat/Skills/StatusPayload.cs`](../Assets/Project/Scripts/Combat/Skills/StatusPayload.cs)

Hiệu ứng gây lên mục tiêu (dùng chung cho skill, passive, đòn quái, bẫy, đạn): danh sách StatusEffectRef — mỗi phần tử trỏ tới một StatusEffectSO + ghi đè thời gian / độ mạnh / xác suất.

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `effects` | List<[StatusEffectRef](#statuseffectref)> |  | Hiệu ứng gây ra (StatusEffectSO + ghi đè thời gian / độ mạnh / xác suất) |

### SwordEntry

class `SwordEntry` — [`Scripts/Catalog/Weapons/WeaponEntries.cs`](../Assets/Project/Scripts/Catalog/Weapons/WeaponEntries.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `meleeStyle` | `Overridable<MeleeAttackStyle>` | tắt | *[Cận chiến]* Kiểu đánh Swing / Thrust / Spin360. Bỏ tick = SO gốc |
| `swingArc` | `Overridable<float>` | tắt | Góc vung (độ); Thrust tối đa 55°, Spin360 luôn 360°. Bỏ tick = SO gốc |
| `swingDuration` | `Overridable<float>` | tắt | Thời gian một lần vung (giây). Bỏ tick = SO gốc |
| `hitbox` | Overridable<[MeleeHitbox](#meleehitbox)> | tắt | Vùng trúng: offset/size theo hướng ngắm (x = phía trước) |
| `destroysProjectiles` | `Overridable<bool>` | tắt | Chém tan đạn của quái trong vùng trúng. Bỏ tick = SO gốc |
| `slashVfx` | `GameObject` |  | VFX vệt chém riêng. Trống = của SO gốc |
| `waveProjectileId` | `string` |  | *[Kiếm khí (tùy chọn)]* Id trong ProjectileLibrary; trống = không bắn kiếm khí |
| `swordWaveDamageRatio` | `Overridable<float>` | tắt | Sát thương kiếm khí = sát thương đòn chém × tỉ lệ này. Bỏ tick = SO gốc |
| `waveSpeed` | `Overridable<float>` | tắt | Tốc độ kiếm khí (world-unit / giây). Bỏ tick = speed của đạn kiếm khí |
| `pierce` | `Overridable<int>` | tắt | Số mục tiêu kiếm khí xuyên thêm. Bỏ tick = theo đạn kiếm khí |
| `explosionRadius` | `Overridable<float>` | tắt | Bán kính nổ kiếm khí (world-unit), 0 = không nổ. Bỏ tick = theo đạn kiếm khí |

### Tier

class `Tier` — [`Scripts/Catalog/Stats/MonsterTierConfigSO.cs`](../Assets/Project/Scripts/Catalog/Stats/MonsterTierConfigSO.cs)

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `tier` | `MonsterTier` | Normal | Bậc quái áp dụng dòng này; bậc không có dòng → mọi hệ số = 1 |
| `hpMultiplier` | `float` | 1 | 1 = giữ nguyên, 2.5 = HP ×2.5 |
| `damageMultiplier` | `float` | 1 | Hệ số sát thương: 1 = giữ nguyên, 1.4 = +40% (cộng % vào AttackDamage) |
| `moveSpeedMultiplier` | `float` | 1 | Hệ số tốc chạy: 1 = giữ nguyên, 1.1 = +10% (cộng % vào MoveSpeed) |
| `bonusKnockbackResist` | `float` | 0 | Cộng thêm kháng đẩy lùi (0..1) (khoảng 0–1) |
| `scale` | `float` | 1 | Hệ số kích thước hiển thị |
| `tint` | `Color` | trong suốt | Màu viền/tint nhận diện (alpha 0 = không tint) |

### TierLoot

class `TierLoot` — [`Scripts/Catalog/Loot/LootConfigSO.cs`](../Assets/Project/Scripts/Catalog/Loot/LootConfigSO.cs)

Rơi đồ của một bậc quái (hoặc một loại rương).

| Field | Kiểu | Mặc định | Ý nghĩa |
|---|---|---|---|
| `tier` | `MonsterTier` | Normal | Bậc quái áp dụng dòng này (Normal / Elite / Boss). Không tìm thấy bậc → dùng dòng đầu tiên. |
| `gold` | `Vector2Int` | (1, 3) | Vàng (min, max) |
| `itemChance` | `float` | 0.03 | Xác suất rơi vũ khí / trang bị (khoảng 0–1) |
| `guaranteedItems` | `int` | 0 | Số món chắc chắn rơi (boss) (≥ 0) |
| `manaOrbChance` | `float` | 0.3 |  (khoảng 0–1) |
| `manaOrbs` | `int` | 1 |  (≥ 0) |
| `potionChance` | `float` | 0.02 |  (khoảng 0–1) |
| `rankShift` | `float` | 0 | Dời bậc vật phẩm lên (Elite / Boss rơi đồ tốt hơn). 1 = một bậc |


## Enum dùng trong field

- **CharacterRole**: `Any`, `Warrior`, `Ranger`, `Mage`, `Assassin` — Phân loại Role của nhân vật trong Dungeon Gathering. Quyết định hiệu ứng Role Synergy khi mặc trang bị EquipmentSO. Default = Any (0) để đảm bảo an toàn cho các trường chưa gán giá trị.
- **EquipmentSlotType**: `Helmet`, `Armor`, `Gloves`, `Boots`, `Necklace`, `Accessory` — 6 ô trang bị (ENTITIES §5.2). Thứ tự = chỉ số trong LoadoutData.equipment.
- **GameState**: `Boot`, `Menu`, `Lobby`, `Playing`, `Paused`, `Victory`, `GameOver`
- **HitboxShape**: `Auto`, `Arc`, `Box`
- **MeleeAttackStyle**: `Swing`, `Thrust`, `Spin360`
- **MonsterTier**: `Normal`, `Elite`, `Boss` — Bậc quái: quyết định hệ số chỉ số (MonsterTierConfigSO) và bảng rớt đồ.
- **RangedFireMode**: `Projectile`, `Hitscan`
- **Rarity**: `S`, `A`, `B`, `C`, `D` — Bậc vật phẩm / hero, cao nhất trước (S=0 … D=4). Số giữ nguyên khi đổi F → D nên asset cũ không lệch. Order matches the colour squares in Basic Buttons 3 Fv. Lưu ý: Enum giá trị int nhỏ hơn = Rarity cao hơn (S mạnh nhất, D phổ thông). Khuyến khích dùng RarityExtensions để so sánh thay vì toán tử .
- **RoomType**: `Start`, `Combat`, `Treasure`, `Shop`, `Boss`, `Elite` — Loại phòng. Elite = phòng quái mà mọi con sinh ra là Elite. Thêm mới luôn nối vào cuối.
- **SfxId**: `None`, `SwordSwing`, `SwordHit`, `CritHit`, `BowShot`, `GunShot`, `MagicShot`, `EnemyShot`, `ProjectileHit`, `Explosion`, `PlayerHurt`, `PlayerDeath`, `EnemyHurt`, `EnemyDeath`, `BossRoar`, `BossAppear`, `WeaponSwap`, `Dash`, `Shield`, `Heal`, `Buff`, `SkillBurst`, `CoinPickup`, `ManaPickup`, `PotionDrink`, `ItemPickup`, `RareItemPickup`, `ChestOpen`, `DoorOpen`, `DoorClose`, `PortalEnter`, `SpawnWarning`, `TrapTrigger`, `Footstep`, `RoomCleared`, `Victory`, `Defeat`, `LevelUp`, `UiClick`, `UiConfirm`, `UiBack`, `UiError`, `UiPurchase`, `UiPanelOpen`, `UiPanelClose` — Định danh hiệu ứng âm thanh. Giá trị số được serialize trong SfxLibrary / prefab / SO → chỉ THÊM mới ở cuối nhóm với số chưa dùng, không đổi số đã có. Tên enum khớp tên file: sfx_&lt;nhóm&gt;_&lt;snake_case&gt;[_NN].wav (VD: UiClick ↔ sfx_ui_ui_click.wav).
- **SkillId**: `None`, `ArcaneStorm`, `ArcaneStormBook`, `ArrowRain`, `BloodFrenzy`, `DodgeRoll`, `DragonFlame`, `DualWield`, `FrostField`, `HolyLight`, `HolyLightSword`, `RaiseShield`, `RiftCut`, `ShadowStep`, `SpearCharge`, `SummonSkeletons`, `ThunderJudgement`, `AtkAssassinDash`, `AtkBite`, `AtkBloodBolt`, `AtkBoltFan`, `AtkFrostFan`, `AtkFrostOrb`, `AtkLordBloodpool`, `AtkLordCharge`, `AtkLordClaw`, `AtkLordFan`, `AtkLordSpiral`, `AtkLordSummon`, `AtkLordTeleport`, `AtkSkullRadial`, `AtkSpearThrust`, `AtkSwordSlash`, `AtkVenomSlash`, `AtkVoidFan`, `AtkWarriorCharge`, `IceSpike`, `WindWhirlwind`, `ShadowCurse`, `Rejuvenate`, `MegaBlast`, `LeafStorm`
- **StackPolicy**: `Refresh`, `Extend`, `Stack`, `KeepStrongest` — Cách cộng dồn khi cùng một hiệu ứng được gây lại lên mục tiêu đang mang nó.
- **StatType**: `MaxHp`, `MaxArmor`, `MaxMana`, `AttackDamage`, `AttackSpeed`, `MoveSpeed`, `CritChance`, `CritDamage`, `CooldownReduction`, `ManaRegen`, `KnockbackResist`
- **StatusTag** (cờ, chọn nhiều): `None` (0), `CrowdControl` (1), `DamageOverTime` (2), `Buff` (4), `Debuff` (8)
- **WeaponHoldStyle**: `OneHandedSide`, `HeavyShoulder`, `Ranged`, `Spear`

## Được tham chiếu bởi

Field nào trỏ tới mỗi lớp SO (kể cả qua List / kiểu nhúng). Dùng khi đổi tên / xóa một lớp.

- **ActiveSkillSO** ← `ItemData.activeSkill`, `SkillEntry.skill (qua SkillBaseSO)`
- **AreaBurstSkill** ← `ItemData.activeSkill (qua ActiveSkillSO)`, `SkillEntry.skill (qua SkillBaseSO)`
- **AreaTelegraphAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack (qua EnemyAttackSO)`
- **ArmorBreakNovaPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `EquipmentSO.synergyPassive (qua EquipmentPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **BuffSkill** ← `ItemData.activeSkill (qua ActiveSkillSO)`, `SkillEntry.skill (qua SkillBaseSO)`
- **CampaignSO** ← `ItemCatalog.campaign`
- **ChainLightningPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **ChargeAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack (qua EnemyAttackSO)`
- **CritRewardPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `EquipmentSO.synergyPassive (qua EquipmentPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **DamageOverTimeEffectSO** ← `StatusEffectRef.effect (qua StatusEffectSO)`
- **DashSkill** ← `ItemData.activeSkill (qua ActiveSkillSO)`, `SkillEntry.skill (qua SkillBaseSO)`
- **EnemyAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack`
- **EquipmentPassiveSO** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `EquipmentSO.synergyPassive`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **EquipmentSetSO** ← `EquipmentSO.equipmentSet`
- **EquipmentSO** ← `EquipmentSetSO.pieces`, `ItemCatalog.items (qua ItemData)`
- **EquipStatBonusPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `EquipmentSO.synergyPassive (qua EquipmentPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **EveryNthHitPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `EquipmentSO.synergyPassive (qua EquipmentPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **HeroDefinitionSO** ← `ItemCatalog.characters`
- **HookWeaponPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **ItemData** ← `ItemCatalog.items`
- **ItemPassiveSO** ← `EquipmentSetSO.bonus4Passive`, `HeroDefinitionSO.innatePassive`, `RunBuffSO.passive`
- **LifestealPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **LootConfigSO** ← `PlayPrefabSet.lootConfig`
- **LowHpBerserkPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **ManaOnKillPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **MeleeAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack (qua EnemyAttackSO)`
- **ModuleDefinition** ← `ModuleRegistry.modules`
- **MonsterDefinitionSO** ← `EnemySpawnEntry.monster`, `SummonAttackSO.minion`
- **MonsterTierConfigSO** ← `PlayPrefabSet.monsterTiers`
- **OnHitStatusPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **ProjectileLibrarySO** ← `ItemCatalog.projectileLibrary`
- **ProjectileVolleySkill** ← `ItemData.activeSkill (qua ActiveSkillSO)`, `SkillEntry.skill (qua SkillBaseSO)`
- **RangedBaseSO** ← `RangedLibrarySO.baseConfig`
- **RangedLibrarySO** ← `ItemCatalog.rangedLibrary`
- **RoomTypeConfigSO** ← `LevelConfigSO.roomTypes`
- **ShieldHealSkill** ← `ItemData.activeSkill (qua ActiveSkillSO)`, `SkillEntry.skill (qua SkillBaseSO)`
- **ShootAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack (qua EnemyAttackSO)`
- **SkillBaseSO** ← `SkillEntry.skill`
- **SkillLibrarySO** ← `ItemCatalog.skillLibrary`
- **SlowEffectSO** ← `StatusEffectRef.effect (qua StatusEffectSO)`
- **StatModifierEffectSO** ← `StatusEffectRef.effect (qua StatusEffectSO)`
- **StatProfileSO** ← `StatBlock.profile`
- **StatusEffectSO** ← `StatusEffectRef.effect`
- **StillnessEmpowerPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `EquipmentSO.synergyPassive (qua EquipmentPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **StunEffectSO** ← `StatusEffectRef.effect (qua StatusEffectSO)`
- **SummonAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack (qua EnemyAttackSO)`
- **SummonCompanionSkill** ← `ItemData.activeSkill (qua ActiveSkillSO)`, `SkillEntry.skill (qua SkillBaseSO)`
- **SwordBaseSO** ← `SwordLibrarySO.baseConfig`
- **SwordLibrarySO** ← `ItemCatalog.swordLibrary`
- **TeleportAttackSO** ← `SkillEntry.skill (qua SkillBaseSO)`, `TeleportAttackSO.followUpAttack (qua EnemyAttackSO)`
- **WeaponData** ← `HeroDefinitionSO.startingWeapon`, `ItemCatalog.items (qua ItemData)`
- **WeaponPassiveSO** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
- **WeaponStatBonusPassive** ← `EquipmentSetSO.bonus4Passive (qua ItemPassiveSO)`, `HeroDefinitionSO.innatePassive (qua ItemPassiveSO)`, `RunBuffSO.passive (qua ItemPassiveSO)`
