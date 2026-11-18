# Handoff — tái cấu trúc Dungeon Gathering (GĐ 0–11)

Cập nhật: 2026-11-16. Kiến trúc tổng quan: [`ARCHITECTURE.md`](ARCHITECTURE.md). File này dành cho người tiếp nhận: trạng
thái hiện tại, cách kiểm tra, **danh mục đầy đủ các ScriptableObject** (lớp, file code, asset, field, ai dùng, cách thêm
mới), số liệu đang cấu hình, và việc còn lại.

> Tra cứu **từng field** của mọi SO (kiểu, giá trị mặc định, ý nghĩa, đơn vị, nơi tham chiếu): [`SO_REFERENCE.md`](SO_REFERENCE.md)
> — sinh tự động bằng `Tools ▸ Dungeon Gathering ▸ Docs ▸ Generate SO Reference`.

---

## 1. Trạng thái

| Mục | Trạng thái |
|---|---|
| Giai đoạn 0–11 | Xong (xem bảng cuối `ARCHITECTURE.md`) |
| Compile | 0 lỗi |
| EditMode test (`Project.Tests.EditMode`) | **61/61 đạt** |
| `Tools ▸ Dungeon Gathering ▸ Validate All` | **0 lỗi · 0 cảnh báo** (đã kiểm 40 vật phẩm, 10 hero, 18 quái/boss, 35 skill, 3 level) |
| Chơi thử trong Play Mode | **Chưa** — Play Mode qua MCP bị treo khi Editor không có focus; cần người chạy tay |
| Git | **Chưa commit** (~750 file thay đổi trên `main`). Nên tách commit theo giai đoạn hoặc ít nhất một commit lớn sau khi chơi thử |
| Save | `SaveData.CurrentVersion = 2` → save cũ của người chơi bị reset tiến trình (giữ cài đặt) |

### Checklist chơi thử đề xuất

1. Menu → Lobby: danh sách 10 hero, mở khóa / trang bị đồ (kiểm tra không ghi save khi chỉ xem hero chưa có tiến trình).
2. Vào run qua `LobbyPortal` → Tầng 1: HUD hiện `TẦNG 1` / `FLOOR 1`, map có Shop + Treasure, 3–4 phòng combat.
3. Thử 1 kiếm (swing + thrust: giáo), 1 súng (rifle bắn loạt), 1 cung / trượng; kiếm có kiếm khí.
4. Skill gây choáng / làm chậm / đốt (vd `FrostField`, `ThunderJudgement`); quái bị choáng không đánh, hết choáng chạy lại.
5. Gặp quái Elite (to hơn, màu khác, rơi đồ tốt hơn); phòng Elite ở Tầng 2–3.
6. Boss từng tầng (`elite-warrior`, `elite-vampire`, `vampire-lord`): phase chuyển, `Atk_lord-summon` gọi `skeleton-spear`.
7. Portal → tầng tiếp theo → hết Tầng 3 → màn kết quả → về Lobby.
8. Bẫy lửa (`Trap_flame-hazard`): đốt + làm chậm khi đứng trong vùng.
9. Đổi ngôn ngữ EN / VI ở mọi màn — không còn chữ cứng.

---

## 2. Quy tắc khi sửa dữ liệu

- SO và prefab là nguồn dữ liệu duy nhất. Không còn seeder / builder / migration nào ghi đè (đã xóa `Editor/Legacy/`,
  `Editor/Migration/`). Hai menu `UI ▸ Build Design UI` trong `UIDesignBuilder.cs` đã bị comment, đừng bật lại — chúng dựng
  lại prefab UI từ đầu.
- Ghi đè dùng `Overridable<T>`: tick ô = dùng giá trị của entry, bỏ tick = dùng giá trị của SO gốc.
- Sau mỗi đợt sửa: `Tools ▸ Dungeon Gathering ▸ Validate All` (log Error nếu có lỗi, Warning nếu chỉ có cảnh báo).
- Thêm / đổi skill: luôn chạy `Skills ▸ Generate SkillId` sau khi sửa `SkillLibrary.asset`. Không sửa tay `SkillId.cs`.
- Thêm key đa ngôn ngữ: sửa `Design/localization/DG_Multilanguage.csv` → `Localization ▸ Import CSV`.
  **Không chạy `strings_src.py`** (bản cũ, ghi đè CSV).
- Thêm level / boss mới: chạy `Addressables ▸ Apply Module Rules` để đăng ký address.
- Runtime không bao giờ ghi vào SO; mọi giá trị đã resolve nằm ở `WeaponSpec`, `StatSheet`, `StatusInstance`.

---

## 3. Danh mục ScriptableObject

Đường dẫn code tính từ `Assets/Project/`; asset nằm dưới `Assets/Project/Data/` trừ khi ghi khác.
Tạo asset mới: chuột phải trong Project ▸ `Create ▸ Dungeon Gathering ▸ …`.

### 3.1. Gốc nối mọi thứ

| SO | Code | Asset | Field | Ghi chú |
|---|---|---|---|---|
| `ItemCatalog` | `Scripts/Catalog/ItemCatalog.cs` | `Catalog/ItemCatalog.asset` | `items` (WeaponData + EquipmentSO), `characters` (HeroDefinitionSO), `swordLibrary`, `rangedLibrary`, `projectileLibrary`, `skillLibrary`, `campaign` | Nạp qua Addressables; `Register()` đăng ký `WeaponLibrary` + `SkillRegistry`. `PickItem(bậc, rng, vũ khí?, trang bị?)` |
| `PlayPrefabSet` | `Scripts/Play/PlayPrefabSet.cs` | `PlayPrefabSet.asset` | prefab `player`, `damagePopup`, `defaultHitVfx`, `spawnMarker`, `goldCoin`, `manaOrb`, `healthPotion`, `itemPickup`, `door`, `chest`, `shopStand`, `portal`, **`monsterBase`** (`Prefabs/Enemies/Enemy_Base`), **`monsterTiers`**, **`lootConfig`**, `prewarmCoins`, `prewarmPopups` | Mọi prefab gameplay của module Play |

### 3.2. Chỉ số

| SO | Code | Asset | Field |
|---|---|---|---|
| `StatProfileSO` | `Scripts/Catalog/Stats/StatProfileSO.cs` | `Stats/Stats_Hero_{Warrior,Ranger,Mage,Assassin}`, `Stats/Stats_Monster_Default` (5) | `stats : BaseStats` (maxHp, maxArmor, maxMana, attackDamage, attackSpeed, moveSpeed, critChance, critDamage, cooldownReduction, manaRegen, knockbackResist), `armorRegenDelay`, `armorRegenRate` |
| `MonsterTierConfigSO` | `Scripts/Catalog/Stats/MonsterTierConfigSO.cs` | `Config/MonsterTierConfig` (1) | `tiers : List<Tier { tier, hpMultiplier, damageMultiplier, moveSpeedMultiplier, bonusKnockbackResist, scale, tint }>` |

Kiểu nhúng: `StatBlock { profile : StatProfileSO, overrides : List<StatOverride { stat : StatType, value }> }` — dùng trong
`HeroDefinitionSO.stats` và `MonsterDefinitionSO.stats`.

Giá trị hiện tại:

| Profile | HP | Giáp | Mana | ATK× | Tốc đánh | Tốc chạy | Crit | CDR | Hồi mana |
|---|---|---|---|---|---|---|---|---|---|
| Hero_Warrior | 8 | 3 | 100 | 1.15 | 1.1 | 5.0 | 8% / ×2.15 | 0 | 2 |
| Hero_Ranger | 5 | 4 | 160 | 0.85 | 1.1 | 5.5 | 15% / ×2.2 | 0 | 2 |
| Hero_Mage | 5 | 4 | 220 | 1.1 | 1.0 | 5.1 | 5% / ×2.0 | 10% | 3 |
| Hero_Assassin | 6 | 4 | 120 | 1.05 | 1.1 | 5.3 | 15% / ×2.25 | 5% | 2 |
| Monster_Default | 40 | 0 | 0 | 1.0 | 1.0 | 2.5 | 0 | 0 | 0 |

Hero hồi giáp sau 4s, 1 giáp/s. Tier quái: Normal ×1 · **Elite HP ×2.5, sát thương ×1.4, tốc ×1.1, kháng đẩy +0.3, to
×1.25** · Boss kháng đẩy +0.8 (HP boss lấy từ định nghĩa).

### 3.3. Hiệu ứng trạng thái

| SO | Code | Asset | Field riêng |
|---|---|---|---|
| `StatusEffectSO` (abstract) | `Scripts/Catalog/Status/StatusEffectSO.cs` | — | `id`, `nameKey`, `icon`, `defaultDuration`, `defaultMagnitude`, `stacking` (Refresh / Extend / Stack / KeepStrongest), `maxStacks`, `tags` (CrowdControl, DamageOverTime, Buff, Debuff), `tint`, `tintPriority`, `loopVfx` |
| `StunEffectSO` | `…/Status/StunEffectSO.cs` | `StatusEffects/Status_Stun` (1s), `Status_Freeze` (1.5s) | `freeze` (đóng băng: đổi màu, dừng anim) |
| `SlowEffectSO` | `…/Status/SlowEffectSO.cs` | `StatusEffects/Status_Slow` (2s, 30%) | magnitude = % giảm tốc chạy |
| `DamageOverTimeEffectSO` | `…/Status/DamageOverTimeEffectSO.cs` | `Status_Burn` (3s, 2 dps), `Status_Poison` (4s, 1.5 dps) | `tickInterval`; magnitude = dps |
| `StatModifierEffectSO` | `…/Status/StatModifierEffectSO.cs` | (chưa có asset) | `modifiers : List<StatModifier>` — buff / debuff bất kỳ chỉ số |

Kiểu nhúng: `StatusEffectRef { effect, duration : Overridable<float>, magnitude : Overridable<float>, chance, loopVfxOverride }`.
Được dùng ở: `WeaponBaseSO.onHitStatus`, entry vũ khí / đạn `statusEffects`, `EquipmentSO.onHitStatus`,
`StatusPayload.effects` (skill hero, `EnemyAttackSO.status`, `TrapDataSO.onHitStatus`), `TrapDataSO.zoneEffects`,
`OnHitStatusPassive`.

### 3.4. Vũ khí

| SO | Code | Asset | Field |
|---|---|---|---|
| `WeaponBaseSO` (abstract) | `Scripts/Catalog/Weapons/WeaponBaseSO.cs` | — | `damage`, `fireRate`, `attackRange`, `knockback`, `critBonus`, `manaCost`, `holdStyle`, `visualScale`, `spriteForwardAngle`, `hitImpactVfx`, `attackSfx`, `onHitStatus` |
| `SwordBaseSO` | `…/Weapons/SwordBaseSO.cs` | `WeaponLibrary/WeaponBase_Sword` | `meleeStyle` (Swing / Thrust / Spin), `swingArc`, `swingDuration`, `hitbox : MeleeHitbox`, `destroysProjectiles`, `slashVfx`, `swordWaveDamageRatio`, `waveSpeed` |
| `RangedBaseSO` | `…/Weapons/RangedBaseSO.cs` | `WeaponLibrary/WeaponBase_Ranged` | `fireMode`, `spread`, `projectileCount`, `burstCount`, `burstInterval`, `recoilDistance`, `recoilDuration`, `muzzleVfx`, `defaultProjectileId` |
| `SwordLibrarySO` | `…/Weapons/SwordLibrarySO.cs` | `WeaponLibrary/SwordLibrary` | `baseConfig : SwordBaseSO`, `swords : List<SwordEntry>` (15) |
| `RangedLibrarySO` | `…/Weapons/RangedLibrarySO.cs` | `WeaponLibrary/RangedLibrary` | `baseConfig : RangedBaseSO`, `weapons : List<RangedEntry>` (6) |
| `ProjectileLibrarySO` | `…/Weapons/ProjectileLibrarySO.cs` | `WeaponLibrary/ProjectileLibrary` | `projectiles : List<ProjectileEntry>` (4) |
| `WeaponData` | `Scripts/Catalog/WeaponData.cs` | `Weapons/Weapon_*` (21) | Chỉ còn định danh (`id`). Tên / bậc / icon / giá / chỉ số đọc từ entry thư viện cùng id |

Entry (`Scripts/Catalog/Weapons/WeaponEntries.cs`):
- **Chung (`WeaponEntryBase`)**: `id` (trùng `WeaponData.id`), `nameKey`, `descKey`, `rank`, `icon`, `shopPrice`, `sprite`,
  `holdOffset`, `restAngle`, `viewPrefab`, ghi đè `visualScale` / `spriteForwardAngle` / `holdStyle` / `damage` / `fireRate`
  / `attackRange` / `knockback` / `critBonus` / `manaCost`, `hitVfx`, `attackSfx`, `statusEffects`, `passive`, `activeSkill`.
- **`SwordEntry`**: ghi đè `meleeStyle`, `swingArc`, `swingDuration`, `hitbox` (`MeleeHitbox { shape Auto/Arc/Box, offset,
  size, arc }`), `destroysProjectiles`, `swordWaveDamageRatio`, `waveSpeed`, `pierce`, `explosionRadius`; `slashVfx`,
  `waveProjectileId` (kiếm bắn kiếm khí).
- **`RangedEntry`**: `animator` (anim súng), `muzzleOffset`, `projectileId`, ghi đè `fireMode`, `spread`, `projectileCount`,
  `burstCount`, `burstInterval`, `projectileSpeed`, `pierce`, `explosionRadius`, `recoilDistance`; `muzzleVfx`.
- **`ProjectileEntry`**: `id`, `prefab`, `speed`, `pierce`, `explosionRadius`, `hitVfx`, `statusEffects`.

Vũ khí hiện có — kiếm / cận chiến (15): `sword-iron`, `sword-old`, `sword-gold`, `sword-fire`, `sword-ice`, `sword-lava`,
`sword-holy`, `sword-dark`, `sword-purple`, `sword-thorn`, `sword-thunder`, `sword-venom`, `sword-void`, `spear-iron`,
`dagger-twin`. Tầm xa (6): `gun-pistol`, `gun-rifle`, `gun-shotgun`, `gun-launcher`, `bow-wood`, `staff-apprentice`.

**Thêm vũ khí**: tạo `WeaponData` (`Create ▸ Dungeon Gathering ▸ …`, đặt `id`) → thêm vào `ItemCatalog.items` → thêm entry
cùng `id` vào SwordLibrary hoặc RangedLibrary (xem trước hitbox ngay trong Inspector) → Validate All.

### 3.5. Skill

| SO | Code | Asset | Field |
|---|---|---|---|
| `SkillBaseSO` (abstract) | `Scripts/Catalog/Skills/SkillBaseSO.cs` | — | `skillId`, `nameKey`, `descKey`, `icon`, `cooldown` |
| `ActiveSkillSO` (abstract, hero) | `Scripts/Catalog/ActiveSkillSO.cs` | — | `duration`, `manaCost`, `activateSfx` |
| `DashSkill`, `AreaBurstSkill`, `ProjectileVolleySkill`, `BuffSkill`, `ShieldHealSkill`, `SummonCompanionSkill` | `Scripts/Combat/Skills/*.cs` | `Skills/Skill_*` (16) | tham số riêng từng loại + `StatusPayload` |
| `EnemyAttackSO` (abstract, quái) | `Scripts/Enemy/EnemyAttackSO.cs` | — | `range`, `windupTime`, `weight`, `requireLineOfSight`, `stopWhileAttacking`, `status : StatusPayload` |
| `MeleeAttackSO`, `ShootAttackSO`, `ChargeAttackSO`, `AreaTelegraphAttackSO`, `SummonAttackSO`, `TeleportAttackSO` | `Scripts/Enemy/Attacks/*.cs` | `EnemyAttacks/Atk_*` (19) | `SummonAttackSO.minion : MonsterDefinitionSO` |
| `SkillLibrarySO` | `Scripts/Catalog/Skills/SkillLibrarySO.cs` | `Skills/SkillLibrary` | `skills : List<SkillEntry { key, value, skill, cooldown : Overridable, manaCost : Overridable, castVfx, castSfx }>` |

Enum `SkillId` (`Scripts/Catalog/Skills/SkillId.cs`, sinh tự động) — 35 giá trị:

| Số | Hero (ActiveSkillSO) | Số | Quái / boss (EnemyAttackSO) |
|---|---|---|---|
| 1 | ArcaneStorm (AreaBurst) | 17 | AtkAssassinDash (Charge) |
| 2 | ArcaneStormBook (AreaBurst) | 18 | AtkBite (Melee) |
| 3 | ArrowRain (ProjectileVolley) | 19 | AtkBloodBolt (Shoot) |
| 4 | BloodFrenzy (Buff) | 20 | AtkBoltFan (Shoot) |
| 5 | DodgeRoll (Dash) | 21 | AtkFrostFan (Shoot) |
| 6 | DragonFlame (AreaBurst) | 22 | AtkFrostOrb (Shoot) |
| 7 | DualWield (Buff) | 23 | AtkLordBloodpool (AreaTelegraph) |
| 8 | FrostField (AreaBurst) | 24 | AtkLordCharge (Charge) |
| 9 | HolyLight (ShieldHeal) | 25 | AtkLordClaw (Melee) |
| 10 | HolyLightSword (ShieldHeal) | 26 | AtkLordFan (Shoot) |
| 11 | RaiseShield (ShieldHeal) | 27 | AtkLordSpiral (Shoot) |
| 12 | RiftCut (Dash) | 28 | AtkLordSummon (Summon → skeleton-spear) |
| 13 | ShadowStep (Dash) | 29 | AtkLordTeleport (Teleport) |
| 14 | SpearCharge (Dash) | 30 | AtkSkullRadial (Shoot) |
| 15 | SummonSkeletons (SummonCompanion) | 31 | AtkSpearThrust (Melee) |
| 16 | ThunderJudgement (AreaBurst) | 32 | AtkSwordSlash (Melee) |
| | | 33 | AtkVenomSlash (Melee) |
| | | 34 | AtkVoidFan (Shoot) |
| | | 35 | AtkWarriorCharge (Charge) |

**Thêm skill**: tạo SO hành vi → thêm `SkillEntry` (key kebab-case, `value` để 0) → `Skills ▸ Generate SkillId` (gán số mới,
giữ số cũ) → chọn enum trên hero / quái / phase boss → Validate All.

### 3.6. Hero và quái

| SO | Code | Asset | Field |
|---|---|---|---|
| `HeroDefinitionSO` | `Scripts/Catalog/HeroDefinitionSO.cs` | `Characters/Char_*` (10) | `characterId`, `nameKey`, `descKey`, `role`, `unlockPrice`, `released`, `rank`, `portrait`, `animator`, **`attackVfx`**, **`hitVfx`**, `stats : StatBlock`, `startingWeapon`, **`skill : SkillId`**, `innatePassive` |
| `MonsterDefinitionSO` | `Scripts/Enemy/MonsterDefinitionSO.cs` | `Enemies/Enemy_*` (18) | `enemyId`, `nameKey`, `animator`, `sprite`, `visualScale`, **`attackVfx`**, **`hitVfx`**, `contactDamage`, `contactCooldown`, `isFlying`, `preferredDistance`, `retreatDistance`, `aggroRange`, `stats : StatBlock`, **`tier`**, `spawnCost`, `spawnDuration`, `deathDuration`, **`skills : List<SkillId>`**, `phases : List<Phase { hpThreshold, speedMultiplier, skills, transitionVfx, transitionInvulnerable }>`, `customLoot`, `lootTable` |

Hero hiện có:

| Hero | Role | Bậc | Giá | Skill | Profile (số ghi đè) | Vũ khí đầu |
|---|---|---|---|---|---|---|
| knight | Warrior | C | 0 | DualWield | Warrior (7) | sword-iron |
| ranger | Ranger | B | 1000 | DodgeRoll | Ranger (0) | bow-wood |
| wizard | Mage | A | 1200 | ArcaneStorm | Mage (4) | staff-apprentice |
| lancer | Warrior | A | 1500 | SpearCharge | Warrior (7) | spear-iron |
| monk | Mage | A | 1500 | HolyLight | Mage (4) | staff-apprentice |
| paladin | Warrior | A | 1500 | HolyLight | Warrior (7) | sword-holy |
| berserker | Warrior | S | 2000 | BloodFrenzy | Warrior (0) | sword-lava |
| frost-knight | Mage | S | 2000 | FrostField | Mage (0) | sword-ice |
| necromancer | Mage | S | 2000 | SummonSkeletons | Mage (5) | staff-apprentice |
| shadow-knight | Assassin | S | 2500 | RiftCut | Assassin (0) | sword-dark |

Quái thường (dùng prefab nền `Enemy_Base`, tất cả profile `Monster_Default` + ghi đè):

| Quái | HP | Chi phí | Skill | Bay |
|---|---|---|---|---|
| skeleton-spear | 40 | 1 | AtkSpearThrust | |
| skull | 30 | 2 | AtkSkullRadial | ✓ |
| vampire | 45 | 2 | AtkBloodBolt | |
| frost-acolyte | 50 | 2 | AtkFrostOrb | |
| shadow-stalker | 55 | 2 | AtkVenomSlash | |
| skeleton-warrior | 60 | 2 | AtkSwordSlash, AtkWarriorCharge | |
| void-wraith | 120 | 4 | AtkVoidFan, AtkSkullRadial | ✓ |
| plague-assassin | 150 | 4 | AtkVenomSlash, AtkAssassinDash | |
| frost-cryomancer | 155 | 4 | AtkFrostFan, AtkFrostOrb | |
| iron-harvester | 160 | 4 | AtkSpearThrust, AtkWarriorCharge | |
| bone-captain | 170 | 4 | AtkSwordSlash, AtkWarriorCharge | |

Boss (tier Boss, prefab riêng `Prefabs/Enemies/Boss_*`, nạp qua Addressables):

| Boss | HP | Skill nền | Phase | Dùng ở |
|---|---|---|---|---|
| elite-vampire | 320 | AtkBoltFan, AtkBloodBolt | 0 | Tầng 2 |
| elite-warrior | 500 | AtkSwordSlash, AtkWarriorCharge | 2 | Tầng 1 |
| crimson-warlord | 650 | AtkSwordSlash, AtkLordCharge | 2 | (chưa dùng) |
| inferno-king | 680 | AtkLordFan, AtkSkullRadial | 2 | (chưa dùng, bay) |
| dread-executioner | 700 | AtkVenomSlash, AtkLordCharge | 2 | (chưa dùng) |
| frost-lich-king | 760 | AtkFrostFan, AtkLordFan | 2 | (chưa dùng) |
| vampire-lord | 900 | AtkLordFan, AtkLordCharge | 2 | Tầng 3 |

**Thêm quái thường**: tạo `MonsterDefinitionSO` (animator, sprite, profile + ghi đè, skills) → thêm vào `enemyTable` của level
→ Validate All. Không cần prefab. **Thêm boss**: tạo định nghĩa tier Boss + prefab `Boss_<id>` (có `BossPhaseController`)
→ `Addressables ▸ Apply Module Rules` → đặt `bossAddress` của level.

### 3.7. Rơi đồ

| SO | Code | Asset | Field |
|---|---|---|---|
| `LootConfigSO` | `Scripts/Catalog/Loot/LootConfigSO.cs` | `Config/LootConfig` | `rankWeightsByLevel : List<RankWeights {D,C,B,A,S}>`, `rankShiftPerExtraLevel`, `goldPerLevel`, `tiers : List<TierLoot { tier, gold, itemChance, guaranteedItems, manaOrbChance, manaOrbs, potionChance, rankShift }>`, `chestRankShift` |
| `LootTable` (class nhúng, không phải SO) | `Scripts/Catalog/LootTable.cs` | trong `MonsterDefinitionSO.lootTable` | Chỉ dùng khi `customLoot = true` (hiện không quái nào bật) |

Giá trị hiện tại: Level 1 D60 C35 B5 · Level 2 C50 B35 A15 · Level 3 B40 A45 S15; level sau +0.5 bậc / level; vàng +25% /
level. Normal: vàng 1–3, 3% vật phẩm, 30% mana, 2% bình máu. Elite: vàng 4–8, 20% vật phẩm, +1 bậc. Boss: vàng 20–30,
chắc chắn 1 món, +1.5 bậc. Rương: +0.5 bậc.

### 3.8. Level / map

| SO | Code | Asset | Field |
|---|---|---|---|
| `CampaignSO` | `Scripts/Catalog/Levels/CampaignSO.cs` | `DungeonConfigs/Campaign_dungeon` | `campaignId`, `themeId`, `levelAddresses` (`theme/dungeon/config/floor-1..3`) |
| `LevelConfigSO` | `Scripts/Play/Dungeon/LevelConfigSO.cs` | `DungeonConfigs/DungeonConfig_dungeon_F{1,2,3}` | `themeId`, `floor`, `nameKey`, `combatRoomCount`, `specialRooms : List<{type, count}>`, `roomTypes`, `eliteChance`, `lootLuck`, `gridSize`, `cellSpacing`, tile hành lang (`corridorFloorTile`, `corridorWallTile`, `sealWallTile`, `corridorWidth`, `sealDepth`), `enemyTable : List<EnemySpawnEntry { monster, weight, prewarm }>`, `spawnBudget`, `budgetGrowth`, `wavesPerRoom`, `enemyHealthMultiplier`, `spawnWarningTime`, `bossAddress`, `bossLoadTimeout`, `chestOnClearChance` |
| `RoomTypeConfigSO` | `Scripts/Play/Dungeon/RoomTypeConfigSO.cs` | `RoomTypes/RoomType_{Start,Combat,Treasure,Shop,Boss,Elite}` | `roomType`, `templates : List<RoomTemplate>`, `waves` (0 = theo level), `spawnBudget`, `budgetGrowth`, `eliteChanceBonus`, `chestChance` (-1 = theo level) |

| Level | Phòng combat | Phòng đặc biệt | Elite | lootLuck | Lưới | HP quái × | Ngân sách | Đợt | Boss |
|---|---|---|---|---|---|---|---|---|---|
| F1 | 3–4 | Treasure, Shop | 5% | 0 | 7×7 | 1.0 | 4 +1 | 2 | elite-warrior |
| F2 | 4–6 | Treasure, Shop, Elite | 10% | 0.25 | 8×8 | 1.25 | 6 +2 | 2–3 | elite-vampire |
| F3 | 5–7 | Treasure, Shop, Elite | 15% | 0.5 | 9×9 | 1.5 | 8 +2 | 2–3 | vampire-lord |

Bảng quái: F1 có 5 loại (skeleton-spear, skeleton-warrior, shadow-stalker, frost-acolyte, bone-captain); F2 và F3 có cả 11
loại quái thường với trọng số khác nhau. `RoomType_Elite`: mọi con là Elite, 2 đợt, rương 75%. Mỗi loại phòng hiện có 1
template.

**Thêm level**: nhân bản `DungeonConfig_dungeon_F3`, đổi `floor` + số liệu → thêm address vào `Campaign_dungeon` →
`Addressables ▸ Apply Module Rules` → Validate All (sinh thử 50 map mỗi level).

### 3.9. Vật phẩm, trang bị, buff, passive

| SO | Code | Asset | Field |
|---|---|---|---|
| `ItemData` (gốc, cài `IItemDefinition`) | `Scripts/Catalog/ItemData.cs` | — | `id`, `rarity`, `icon`, `nameKey`, `descKey`, `activeSkill`, `shopPrice` |
| `EquipmentSO` | `Scripts/Catalog/EquipmentSO.cs` | `Equipment/Equip_*` (19) | `slot`, `baseModifiers`, `synergyRole`, `synergyPassive`, `equipmentSet`, **`onHitStatus`** |
| `EquipmentSetSO` | `Scripts/Catalog/EquipmentSetSO.cs` | `EquipmentSets/Set_*` (4: archmage, crimson-warrior, shadow-assassin, wind-ranger) | `setId`, `nameKey`, `pieces`, `bonus2Pieces`, `bonus4Passive` |
| `RunBuffSO` | `Scripts/Catalog/RunBuffSO.cs` | `Buffs/Buff_*` (4: arcane-surge, iron-will, swift-boots, vampiric-fangs) | `buffId`, `nameKey`, `descKey`, `icon`, `rarity`, `statModifiers`, `passive` |
| `ItemPassiveSO` / `WeaponPassiveSO` / `EquipmentPassiveSO` (gốc) | `Scripts/Catalog/*PassiveSO.cs` | — | Hook vào sự kiện đánh / trúng / giết |
| Passive cụ thể | `Scripts/Combat/Passives/*.cs` | `Passives/IP_*` (10), `WeaponPassives/WP_*` (11), `EquipmentPassives/EP_*` (16) | `EquipStatBonusPassive` (20), `OnHitStatusPassive` (4), `WeaponStatBonusPassive` (3), `LifestealPassive` (2), `ManaOnKillPassive` (2), `ArmorBreakNovaPassive`, `ChainLightningPassive`, `CritRewardPassive`, `EveryNthHitPassive`, `LowHpBerserkPassive`, `StillnessEmpowerPassive` |

### 3.10. Bẫy

| SO | Code | Asset | Field |
|---|---|---|---|
| `TrapDataSO` | `Scripts/Play/Dungeon/Traps/TrapDataSO.cs` | `Traps/Trap_{flame-hazard, spike-timed, spike-trigger}` | `trapId`, `damage`, `knockback`, `inactiveTime`, `warningTime`, `activeTime`, `cooldown`, `onHitStatus : StatusPayload`, **`zoneEffects : List<StatusEffectRef>`** (flame-hazard: Burn 1.5 dps 1.2s + Slow 25% 1.2s, áp mỗi 0.25s) |

### 3.11. Hạ tầng (Core, không thuộc đợt tái cấu trúc)

| SO | Asset |
|---|---|
| `ModuleRegistry`, `ModuleDefinition` | `Core/Data/ModuleRegistry.asset`, `Core/Data/Modules/Module_{menu,lobby,play}.asset` |
| `SfxLibrary` | `Core/Data/SfxLibrary.asset` |
| `LocalizationFontConfig` | `Core/Data/LocalizationFontConfig.asset` |

### 3.12. VFX — phát ở đâu và đang gán gì

Toàn bộ VFX là prefab `Prefabs/VFX/<Nhóm>/VFX_*.prefab` (102 cái, group Addressables `VFX_Combat`), phát qua
`CombatFx.Play` (pool). 78 VFX tự trả về pool khi chạy xong (`AutoDestroyEffect`); 24 VFX lặp (aura, mây, lửa nền…) không tự
trả — chỉ dùng cho hiệu ứng có thời hạn (aura buff, VFX lặp của trạng thái), **không** gán vào ô phát một lần.

**Nguyên tắc: một đòn vũ khí chỉ có VFX của chính vũ khí.** Kiếm chỉ có vệt chém (không VFX trúng). Màu nguyên tố (đốt,
độc, băng, choáng) do **VFX lặp của hiệu ứng trạng thái** bám trên mục tiêu thể hiện; hiệu ứng lớn do **skill** hoặc passive
khi kích hoạt (hồi máu, sét lan). Hero không có VFX riêng chồng lên vũ khí.

| Ô | Phát khi | Ưu tiên / ghi chú |
|---|---|---|
| Kiếm `slashVfx` | Mỗi lần vung, xoay theo hướng chém — **VFX duy nhất của đòn chém** | entry → `WeaponBase_Sword` (`Melee_SwordSlash`) → prefab `BaseWeapon_Melee`. Hiện chỉ `spear-iron` có riêng (`Melee_SpearThrust`) |
| Vũ khí `hitVfx` | Khi **đạn** trúng (súng, cung, trượng, kiếm khí). Chém cận chiến không dùng | entry → đạn (`ProjectileEntry.hitVfx`) → SO gốc → `PlayPrefabSet.defaultHitVfx` (`Impacts_HitSpark`) |
| Súng `muzzleVfx` | Tại nòng mỗi phát | entry → `WeaponBase_Ranged` (`Impacts_PunchHit`) |
| Trạng thái `loopVfx` | Bám trên mục tiêu suốt thời gian hiệu ứng | `StatusEffectRef.loopVfxOverride` → `StatusEffectSO.loopVfx` |
| Passive `vfx` / `hitVfx` | Khi passive kích hoạt | `WP_holy-lifesteal` (`Holy_HolyHeal` khi hồi máu), `WP_thunder-chain` (`Lightning_ChainLightning`, 25%) |
| Hero `attackVfx` / `hitVfx` | Mỗi đòn / mỗi lần trúng, chồng lên VFX vũ khí | **Để trống** ở mọi hero (field giữ lại, code bỏ qua khi trống) |
| Quái `attackVfx` | Tại điểm ra đòn khi quái bắt đầu một đòn (báo hiệu) | — |
| Quái `hitVfx` | Trên Player khi đòn của quái đó trúng (trừ DoT) | — |
| Skill `castVfx` (SkillLibrary) | Tại người dùng mỗi lần dùng skill | Thêm vào VFX riêng của SO hành vi (`vfx`, `slashVfx`, `impactVfx`, `projectilePrefab`…) |

VFX lặp của hiệu ứng trạng thái (tên bỏ tiền tố `VFX_`): Burn → Status_BurningAura · Poison → Status_Poisoned · Freeze →
Status_FrozenChill · Stun → Status_StunStars · Slow → không (chỉ đổi màu; dùng chung cho bẫy). Kiếm băng (`WP_ice-slow`) ghi
đè VFX của Slow thành Status_FrozenChill. Kiếm lửa / dung nham / độc / băng thể hiện nguyên tố qua các VFX này.

| Quái / boss | attackVfx | hitVfx |
|---|---|---|
| skeleton-spear, skeleton-warrior | Smoke_DustPuff | Impacts_PunchHit |
| iron-harvester | Smoke_DustPuff | Impacts_RockShatter |
| bone-captain | Impacts_ShockwaveRing | Impacts_CritHit |
| shadow-stalker | Shadow_ShadowTendrils | Melee_ShadowClaw |
| plague-assassin | Smoke_SmokeBomb | Nature_SporeBurst |
| vampire, elite-vampire | Arcane_MagicCircle | Shadow_SoulDrain |
| frost-acolyte, frost-cryomancer | Arcane_MagicCircle | Ice_SnowballImpact |
| skull | Explosions_FireworkBurst | Explosions_SmallExplosion |
| void-wraith | Shadow_VoidImplosion | Shadow_ShadowTendrils |
| elite-warrior | Impacts_GroundSlam | Impacts_CritHit |
| vampire-lord | Shadow_ShadowTendrils | Impacts_BloodSplat |
| crimson-warlord | Impacts_GroundSlam | Impacts_BloodSplat |
| dread-executioner | Smoke_SmokeBomb | Nature_AcidSplash |
| frost-lich-king | Arcane_MagicCircle | Ice_FreezeEncase |
| inferno-king | Fire_FirePillar | Fire_FireballImpact |

Skill `castVfx` (21/35):
- Hero: ArcaneStorm, ArcaneStormBook, FrostField, ThunderJudgement → Arcane_MagicCircle · DragonFlame → Fire_LavaBurst ·
  ArrowRain, DodgeRoll → Wind_WindGust · BloodFrenzy, SpearCharge → Impacts_ShockwaveRing · DualWield → Impacts_Parry ·
  HolyLight, HolyLightSword → Holy_DivineBeam · RaiseShield → Holy_BarrierBlock · RiftCut, ShadowStep → Arcane_BlinkOut ·
  SummonSkeletons → Shadow_ShadowTendrils.
- Quái: AtkAssassinDash, AtkWarriorCharge → Smoke_DustPuff · AtkLordCharge → Impacts_ShockwaveRing · AtkLordSummon →
  Arcane_PortalSummon · AtkLordSpiral → Explosions_Implosion.
- **Cố ý để trống** 14 đòn thường còn lại của quái: `attackVfx` của quái đã báo hiệu mỗi đòn, thêm `castVfx` sẽ thành hai
  báo hiệu chồng nhau. `castSfx` để trống ở mọi skill — âm thanh skill hero đã có `ActiveSkillSO.activateSfx` (SfxId).

Vũ khí tầm xa: muzzle mặc định Impacts_PunchHit (pistol, rifle); shotgun, launcher → Smoke_DustPuff; bow-wood →
Wind_WindGust; staff-apprentice → Ui_StarBurstPickup. Đạn (`hitVfx` dự phòng, chỉ dùng khi entry vũ khí không có):
plasma-blue → Water_BubbleBurst, plasma-red → Impacts_HitSpark, rocket → Explosions_SmallExplosion, sparks-blast →
Lightning_StaticBurst.

Còn trống: `ChargeAttackSO.warningPrefab` (3 đòn lao) — đây là vạch cảnh báo bị kéo dài theo quãng lao (`scale.x =
distance`), cần một sprite vạch riêng chứ không dùng được VFX có sẵn; hiện đòn lao báo hiệu bằng `castVfx` + nháy sáng windup.

---

## 4. Đã xóa ở giai đoạn 11 (đừng tìm lại)

- Thư mục `Editor/Legacy/` (DataSeeder, EntityPrefabBuilder*, ItemTilePrefabBuilder, LobbyRosterBuilder,
  PlayHudPrefabBuilder, UIPrefabBuilder) và `Editor/Migration/` (mọi menu `Migrate ▸ …`, đều đã chạy xong).
- 11 prefab quái thường `Prefabs/Enemies/Enemy_*.prefab` (trừ `Enemy_Base`) + entry Addressables của chúng.
- Field cũ: chỉ số / hình / VFX trên `WeaponData`; `baseStats`, `activeSkill`, `armorRegen*`, `manaRegen` trên hero;
  `maxHp`, `moveSpeed`, `knockbackResist`, `attacks` trên quái; `minionPrefab` của `SummonAttackSO`; các field slow / stun /
  dot cũ của `StatusPayload`; `dotId`, `dotDps`, `lingerDuration`, `slowPercent` của `TrapDataSO`; `dropRatesByFloor`,
  `RollRarity`, `RollItem` của `ItemCatalog`; `lootMultiplier` của tier; bố cục cũ (`mainPathLength`, `branchCount`, list
  phòng) của `LevelConfigSO`.
- API cũ: `StatusEffectController.ApplySlow / ApplyStun / ApplyDot`, `WeaponSpec.FromLegacy`, `RankWeights.From`,
  `CharacterInventory` (thay bằng `InventoryModel`).
- Đổi tên lớp (giữ GUID, asset không đổi tên file): `CharacterData → HeroDefinitionSO`, `EnemyData → MonsterDefinitionSO`,
  `DungeonConfig → LevelConfigSO`, `EquipmentData → EquipmentSO`. `Rarity.F → Rarity.D`.

---

## 5. Việc còn lại / gợi ý tiếp theo

**Cần làm trước khi phát hành**
1. **Chơi thử** theo checklist mục 1 — chưa ai chạy game sau tái cấu trúc.
2. **Commit** (~750 file, chưa commit gì).
3. **Xem lại VFX khi chơi thử** (mục 3.12). VFX trong thư viện vẽ ở 16 PPU nên khá to so với hero (1 ô).
   Kiếm không còn VFX trúng nào — nếu đòn chém thiếu cảm giác "trúng", thứ nên chỉnh là nháy sáng / giật lùi của quái,
   không phải thêm VFX vào vũ khí.
4. Build Addressables + build WebGL thử sau khi chạy `Addressables ▸ Apply Module Rules`.

**Chưa làm so với kế hoạch ban đầu (có thể làm sau)**
- `RankConfigSO`: bậc D–S của hero hiện chỉ để hiển thị / định giá, chưa nhân chỉ số.
- `KnockbackEffectSO`: đẩy lùi vẫn là số trên vũ khí / đòn, chưa thành hiệu ứng trạng thái.
- `SkillEntry` mới ghi đè được cooldown / mana; sát thương, tầm, hiệu ứng vẫn sửa trên SO hành vi.
- Address level vẫn dạng `theme/dungeon/config/floor-N`; boss nạp bằng chuỗi `bossAddress` (chưa dùng AssetReference).
- `LevelConfigSO.enemyHealthMultiplier` vẫn còn (chồng với tier) — có thể gộp vào tier / profile.
- 4 boss (`crimson-warlord`, `inferno-king`, `dread-executioner`, `frost-lich-king`) đã có định nghĩa nhưng chưa có level nào
  dùng — sẵn cho campaign thứ hai hoặc tầng 4+.
- Mỗi loại phòng mới có 1 template; thêm template vào `RoomType_*` để map đa dạng hơn.
- `StatModifierEffectSO` chưa có asset nào (sẵn cho buff / debuff skill).
- `Design/localization/strings_src.py` đã lỗi thời — nên xóa hoặc cập nhật để khỏi bị chạy nhầm.

---

## 6. Test

`Assets/Project/Tests/EditMode/` (asmdef `Project.Tests.EditMode`): `StatsTests` (10), `StatusEffectTests` (11),
`StateMachineTests` (6), `WeaponLibraryTests` (7), `SkillLibraryTests` (5), `DefinitionTests` (4), `LootTests` (7),
`LevelGeneratorTests` (5), `InventoryTests` (6) — tổng 61. Chạy: Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All.
