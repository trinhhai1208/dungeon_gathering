using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using DungeonGathering.Inventory;
using DungeonGathering.Play;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Tạo toàn bộ dữ liệu ScriptableObject của game (docs/DATA.md). Số liệu lấy theo bản thiết kế
    /// Design/canvas/project/UI-Weapon, UI-Equipment, UI-Lobby và key chữ trong Design/localization/strings_src.py.
    ///
    /// Idempotent: asset chưa có → tạo + ghi toàn bộ giá trị; asset đã có → GIỮ giá trị (người thiết kế có thể chỉnh trong
    /// Inspector), chỉ điền các tham chiếu còn trống. "Seed All (Overwrite values)" mới ghi đè số liệu.
    /// Prefab/tile chưa có → để trống + báo trong console; chạy lại sau khi có prefab là tự điền.
    /// </summary>
    public static class DataSeeder
    {
        const string Root = "Assets/Project/Data/";
        const string Prefabs = "Assets/Project/Prefabs/";
        const string Art = "Assets/Project/Art/Sprites/";
        const string TilesDir = "Assets/Project/Art/Tiles/";
        const string AnimDir = "Assets/Project/Art/Animations/Characters/";
        const string SwordSheet = Art + "Items/Weapon/Sword/File.png";
        const string GunSheet1 = Art + "Items/Weapon/Weapons 1-Sheet.png";
        const string GunSheet2 = Art + "Items/Weapon/Weapons 2-Sheet.png";
        const string GunSheet3 = Art + "Items/Weapon/Weapons 3-Sheet.png";
        const string ItemSheet = Art + "Items/spritesheet_16x16.png";
        const string DcSheet = Art + "Characters/Dungeon_Character.png";
        const string SkillIconDir = "Assets/Project/Art/Sprites/UI/Generated/Skills/";
        const string FkDir = Art + "Characters/FantasyKnights/";
        const string PlayPrefabSetPath = Root + "PlayPrefabSet.asset"; // do EntityPrefabBuilder tạo — chỉ điền trường fallback

        static bool overwrite;
        static int created, kept;
        static readonly List<string> missing = new();

        [MenuItem("Tools/Dungeon Gathering/Data/Seed All")]
        public static void SeedAll() => Run(false);

        [MenuItem("Tools/Dungeon Gathering/Data/Seed All (Overwrite values)")]
        public static void SeedOverwrite() => Run(true);

        static void Run(bool force)
        {
            overwrite = force;
            created = kept = 0;
            missing.Clear();
            try
            {
                var skills = SeedSkills();
                var wp = SeedWeaponPassives();
                var ep = SeedEquipmentPassives();
                var weapons = SeedWeapons(skills, wp);
                var equips = SeedEquipment(skills, ep);
                var anims = SeedCharacterAnimations();
                var chars = SeedCharacters(skills, weapons, anims);
                var attacks = SeedEnemyAttacks();
                var enemies = SeedEnemies(attacks);
                LinkEnemyPrefabs(enemies);
                var configs = SeedDungeonConfigs();
                var catalog = SeedCatalog(weapons.Values.Cast<ItemData>().Concat(equips.Values), chars);
                LinkPlayPrefabSet(catalog, chars["knight"], configs);
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log($"[Data] Seed xong: {created} asset mới, {kept} asset giữ nguyên số liệu{(overwrite ? " (đã ghi đè)" : "")}.");
            foreach (var m in missing.Distinct()) Debug.LogWarning("[Data] Thiếu tham chiếu: " + m);
            Validate();
        }

        // =====================================================================================================
        // Kỹ năng chủ động
        // =====================================================================================================

        static Dictionary<string, ActiveSkillSO> SeedSkills()
        {
            var d = new Dictionary<string, ActiveSkillSO>();

            var dual = Skill<BuffSkill>(d, "dual-wield", "skill.dual_wield", "char.knight.skill", 12f, 5f, Sword(18), out bool w);
            if (w) dual.modifiers = new() { Pct(StatType.AttackSpeed, 0.6f), Pct(StatType.AttackDamage, 0.15f) };
            Link(ref dual.vfx, Vfx("Status", "PowerUpAura"), "VFX_Status_PowerUpAura");

            var holy = Skill<ShieldHealSkill>(d, "holy-light", "skill.holy_light", "wp.holy.active", 25f, 0f, Item16(4, 18), out w);
            if (w) { holy.heal = 2f; holy.armor = 2f; holy.invulnerableDuration = 1f; }
            Link(ref holy.vfx, Vfx("Holy", "HolyHeal"), "VFX_Holy_HolyHeal");

            var holySword = Skill<ShieldHealSkill>(d, "holy-light-sword", "skill.holy_light", "wp.holy.active", 25f, 0f, Sword(19), out w);
            if (w) { holySword.heal = 2f; holySword.armor = 0f; holySword.invulnerableDuration = 0f; }
            Link(ref holySword.vfx, Vfx("Holy", "HolyHeal"), "VFX_Holy_HolyHeal");

            var roll = Skill<DashSkill>(d, "dodge-roll", "skill.dodge_roll", "char.ranger.skill", 4f, 0f, Item16(1, 4), out w);
            if (w) { roll.distance = 3.5f; roll.dashDuration = 0.2f; roll.invulnerable = true; }
            Link(ref roll.vfx, Vfx("Smoke", "DashDust"), "VFX_Smoke_DashDust");

            var storm = Skill<AreaBurstSkill>(d, "arcane-storm", "skill.arcane_storm", "char.wizard.skill", 15f, 3f, Item16(13, 1), out w);
            if (w) { storm.damage = 12f; storm.radius = 3f; storm.knockback = 2f; storm.pulses = 6; storm.pulseInterval = 0.5f; }
            Link(ref storm.vfx, Vfx("Explosions", "ArcaneBurst"), "VFX_Explosions_ArcaneBurst");

            var bookStorm = Skill<AreaBurstSkill>(d, "arcane-storm-book", "skill.arcane_storm", "eq.book.active", 20f, 3f, Item16(13, 1), out w);
            if (w) { bookStorm.damage = 12f; bookStorm.radius = 3f; bookStorm.knockback = 2f; bookStorm.pulses = 6; bookStorm.pulseInterval = 0.5f; }
            Link(ref bookStorm.vfx, Vfx("Explosions", "ArcaneBurst"), "VFX_Explosions_ArcaneBurst");

            var shield = Skill<ShieldHealSkill>(d, "raise-shield", "skill.raise_shield", "eq.shield.active", 18f, 2f, Item16(4, 18), out w);
            if (w) { shield.heal = 0f; shield.armor = 0f; shield.invulnerableDuration = 2f; }
            Link(ref shield.vfx, Vfx("Holy", "ShieldBubble"), "VFX_Holy_ShieldBubble");

            var flame = Skill<AreaBurstSkill>(d, "dragon-flame", "skill.dragon_flame", "wp.fire.active", 20f, 0f, Sword(4), out w);
            if (w)
            {
                flame.damage = 60f; flame.radius = 3f; flame.knockback = 8f; flame.centerOnNearestEnemy = true; flame.targetRange = 6f;
                flame.status = new StatusPayload { dotDps = 5f, dotDuration = 2f, dotId = "burn" };
            }
            Link(ref flame.vfx, Vfx("Explosions", "FireNova"), "VFX_Explosions_FireNova");
            var burnVfx = Vfx("Status", "BurningAura");
            if (burnVfx != null && (flame.status.dotLoopVfx == null || overwrite)) flame.status.dotLoopVfx = burnVfx;

            var thunder = Skill<AreaBurstSkill>(d, "thunder-judgement", "skill.thunder_judgement", "wp.thunder.active", 18f, 0f, Sword(12), out w);
            if (w)
            {
                thunder.damage = 25f; thunder.radius = 1.5f; thunder.knockback = 2f; thunder.centerOnNearestEnemy = true; thunder.targetRange = 8f;
                thunder.pulses = 5; thunder.pulseInterval = 0.25f; thunder.status = new StatusPayload { stunDuration = 0.3f };
            }
            Link(ref thunder.vfx, Vfx("Lightning", "LightningStrike"), "VFX_Lightning_LightningStrike");

            var rift = Skill<DashSkill>(d, "rift-cut", "skill.rift_cut", "wp.void.active", 15f, 0f, Sword(5), out w);
            if (w) { rift.distance = 5f; rift.dashDuration = 0.15f; rift.invulnerable = true; }
            Link(ref rift.vfx, Vfx("Arcane", "BlinkDash"), "VFX_Arcane_BlinkDash");

            var frost = Skill<AreaBurstSkill>(d, "frost-field", "skill.frost_field", "wp.ice.active", 16f, 0f, Sword(1), out w);
            if (w)
            {
                frost.damage = 10f; frost.radius = 3.5f; frost.knockback = 0f;
                frost.status = new StatusPayload { stunDuration = 2f, freeze = true };
            }
            Link(ref frost.vfx, Vfx("Ice", "FrostNova"), "VFX_Ice_FrostNova");

            return d;
        }

        static T Skill<T>(Dictionary<string, ActiveSkillSO> d, string id, string nameKey, string descKey, float cooldown, float duration,
            Sprite icon, out bool write) where T : ActiveSkillSO
        {
            var s = Asset<T>("Skills/Skill_" + id, out write);
            if (write)
            {
                s.skillId = id; s.nameKey = nameKey; s.descKey = descKey; s.cooldown = cooldown; s.duration = duration; s.manaCost = 0f;
            }
            // Ưu tiên icon chiêu thức vẽ riêng (Design/tools/skill_icons.py); "-book"/"-sword" dùng chung icon gốc
            string baseId = id.Replace("-book", "").Replace("-sword", "");
            var custom = AssetDatabase.LoadAssetAtPath<Sprite>(SkillIconDir + "skill_" + baseId + ".png");
            if (custom != null) s.icon = custom;
            else Link(ref s.icon, icon, $"icon kỹ năng {id}");
            d[id] = s;
            return s;
        }

        // =====================================================================================================
        // Passive vũ khí & trang bị
        // =====================================================================================================

        static Dictionary<string, WeaponPassiveSO> SeedWeaponPassives()
        {
            var d = new Dictionary<string, WeaponPassiveSO>();
            var burnVfx = Vfx("Status", "BurningAura");

            var ice = Asset<OnHitStatusPassive>("WeaponPassives/WP_ice-slow", out bool w);
            if (w) { ice.descKey = "wp.ice.passive"; ice.chance = 1f; ice.status = new StatusPayload { slowPercent = 0.3f, slowDuration = 1f }; }
            Link(ref ice.hitVfx, Vfx("Ice", "SnowballImpact"), "VFX_Ice_SnowballImpact");
            d["ice"] = ice;

            var venom = Asset<OnHitStatusPassive>("WeaponPassives/WP_venom-poison", out w);
            if (w) { venom.descKey = "wp.venom.passive"; venom.chance = 1f; venom.status = new StatusPayload { dotDps = 4f, dotDuration = 3f, dotId = "venom" }; }
            var poisonVfx = Vfx("Status", "Poisoned");
            if (poisonVfx != null && (venom.status.dotLoopVfx == null || overwrite)) venom.status.dotLoopVfx = poisonVfx;
            d["venom"] = venom;

            var chain = Asset<ChainLightningPassive>("WeaponPassives/WP_thunder-chain", out w);
            if (w) { chain.descKey = "wp.thunder.passive"; chain.chance = 0.25f; chain.jumps = 3; chain.radius = 3f; chain.damageFraction = 0.5f; }
            Link(ref chain.vfx, Vfx("Lightning", "ChainLightning"), "VFX_Lightning_ChainLightning");
            d["thunder"] = chain;

            var fire = Asset<OnHitStatusPassive>("WeaponPassives/WP_fire-burn", out w);
            if (w) { fire.descKey = "wp.fire.passive"; fire.chance = 1f; fire.status = new StatusPayload { dotDps = 6f, dotDuration = 2f, dotId = "burn" }; }
            if (burnVfx != null && (fire.status.dotLoopVfx == null || overwrite)) fire.status.dotLoopVfx = burnVfx;
            Link(ref fire.hitVfx, Vfx("Fire", "FireballImpact"), "VFX_Fire_FireballImpact");
            d["fire"] = fire;

            var lava = Asset<OnHitStatusPassive>("WeaponPassives/WP_lava-burn", out w);
            if (w) { lava.descKey = "wp.lava.passive"; lava.chance = 1f; lava.status = new StatusPayload { dotDps = 5f, dotDuration = 2f, dotId = "lava_burn" }; }
            if (burnVfx != null && (lava.status.dotLoopVfx == null || overwrite)) lava.status.dotLoopVfx = burnVfx;
            Link(ref lava.hitVfx, Vfx("Fire", "FireballImpact"), "VFX_Fire_FireballImpact");
            d["lava"] = lava;

            var holy = Asset<LifestealPassive>("WeaponPassives/WP_holy-lifesteal", out w);
            if (w) { holy.descKey = "wp.holy.passive"; holy.percent = 0.08f; holy.damagePerHp = 20f; }
            Link(ref holy.vfx, Vfx("Holy", "HolyHeal"), "VFX_Holy_HolyHeal");
            d["holy"] = holy;

            var dark = Asset<ManaOnKillPassive>("WeaponPassives/WP_dark-mana", out w);
            if (w) { dark.descKey = "wp.dark.passive"; dark.mana = 5f; }
            d["dark"] = dark;

            var gold = Asset<ManaOnKillPassive>("WeaponPassives/WP_gold-mana", out w);
            if (w) { gold.descKey = "wp.gold.passive"; gold.mana = 4f; }
            d["gold"] = gold;

            var voidP = Asset<WeaponStatBonusPassive>("WeaponPassives/WP_void-bonus", out w);
            if (w) { voidP.descKey = "wp.void.passive"; voidP.modifiers = new() { Pct(StatType.AttackDamage, 0.15f), Pct(StatType.AttackSpeed, 0.10f) }; }
            d["void"] = voidP;

            var thorn = Asset<WeaponStatBonusPassive>("WeaponPassives/WP_thorn-bonus", out w);
            if (w) { thorn.descKey = "wp.thorn.passive"; thorn.modifiers = new() { Flat(StatType.CritChance, 0.08f), Pct(StatType.AttackDamage, 0.10f) }; }
            d["thorn"] = thorn;

            var rifle = Asset<WeaponStatBonusPassive>("WeaponPassives/WP_rifle-crit", out w);
            if (w) { rifle.descKey = "wp.rifle.passive"; rifle.modifiers = new() { Flat(StatType.CritChance, 0.10f) }; }
            d["rifle"] = rifle;

            return d;
        }

        static Dictionary<string, EquipmentPassiveSO> SeedEquipmentPassives()
        {
            var d = new Dictionary<string, EquipmentPassiveSO>();

            var nova = Asset<ArmorBreakNovaPassive>("EquipmentPassives/EP_fire-nova", out bool w);
            if (w) { nova.descKey = "eq.fire.passive"; nova.damage = 20f; nova.radius = 2.5f; nova.knockback = 10f; }
            Link(ref nova.vfx, Vfx("Explosions", "FireNova"), "VFX_Explosions_FireNova");
            d["fire"] = nova;

            var crit = Asset<CritRewardPassive>("EquipmentPassives/EP_night-crit", out w);
            if (w) { crit.descKey = "eq.night.passive"; crit.mana = 2f; crit.moveSpeedPercent = 0.2f; crit.buffDuration = 1.5f; }
            d["night"] = crit;

            var still = Asset<StillnessEmpowerPassive>("EquipmentPassives/EP_wind-still", out w);
            if (w) { still.descKey = "eq.wind.passive"; still.stillTime = 1f; still.damageMultiplier = 1.4f; still.extraPierce = 3; }
            d["wind"] = still;

            var nth = Asset<EveryNthHitPassive>("EquipmentPassives/EP_book-nth", out w);
            if (w) { nth.descKey = "eq.book.passive"; nth.everyN = 3; nth.damage = 15f; nth.radius = 1.5f; }
            Link(ref nth.vfx, Vfx("Explosions", "ArcaneBurst"), "VFX_Explosions_ArcaneBurst");
            d["book"] = nth;

            var horn = Asset<EquipStatBonusPassive>("EquipmentPassives/EP_horn-bonus", out w);
            if (w) { horn.descKey = "eq.horn.passive"; horn.modifiers = new() { Pct(StatType.AttackDamage, 0.10f) }; }
            d["horn"] = horn;

            var shield = Asset<EquipStatBonusPassive>("EquipmentPassives/EP_shield-bonus", out w);
            if (w) { shield.descKey = "eq.shield.passive"; shield.modifiers = new() { Flat(StatType.MaxArmor, 2f), Flat(StatType.MaxHp, 1f) }; }
            d["shield"] = shield;

            var hat = Asset<EquipStatBonusPassive>("EquipmentPassives/EP_hat-bonus", out w);
            if (w) { hat.descKey = "eq.hat.passive"; hat.modifiers = new() { Flat(StatType.MaxMana, 20f), Pct(StatType.AttackSpeed, 0.10f) }; }
            d["hat"] = hat;

            var mask = Asset<EquipStatBonusPassive>("EquipmentPassives/EP_mask-bonus", out w);
            if (w) { mask.descKey = "eq.mask.passive"; mask.modifiers = new() { Flat(StatType.CritChance, 0.12f), Pct(StatType.AttackDamage, 0.15f) }; }
            d["mask"] = mask;

            var gwhite = Asset<EquipStatBonusPassive>("EquipmentPassives/EP_gwhite-bonus", out w);
            if (w) { gwhite.descKey = "eq.gwhite.passive"; gwhite.modifiers = new() { Flat(StatType.MaxArmor, 1f), Pct(StatType.AttackSpeed, 0.10f) }; }
            d["gwhite"] = gwhite;

            var neck = Asset<EquipStatBonusPassive>("EquipmentPassives/EP_neck-bonus", out w);
            if (w) { neck.descKey = "eq.neck.passive"; neck.modifiers = new() { Flat(StatType.CritChance, 0.10f), Pct(StatType.MoveSpeed, 0.10f) }; }
            d["neck"] = neck;

            return d;
        }

        // =====================================================================================================
        // Vũ khí — 13 kiếm cận chiến + 4 súng tầm xa
        // =====================================================================================================

        static readonly (string key, Rarity rar, int sprite, float dmg, float aspd, float rng, int crit, string passive, string active, string hitCat, string hitName)[] WeaponTable =
        {
            ("old",     Rarity.F, 17, 10, 1.3f, 1.0f, 5,  null,      null,                "Impacts",   "HitSpark"),
            ("iron",    Rarity.F, 18, 14, 1.0f, 1.2f, 5,  null,      null,                "Impacts",   "HitSpark"),
            ("venom",   Rarity.C, 20, 13, 1.1f, 1.2f, 5,  "venom",   null,                "Nature",    "AcidSplash"),
            ("dark",    Rarity.C, 23, 16, 0.9f, 1.2f, 5,  "dark",    null,                "Shadow",    "Curse"),
            ("ice",     Rarity.B, 1,  18, 0.9f, 1.4f, 5,  "ice",     "frost-field",       "Ice",       "SnowballImpact"),
            ("thorn",   Rarity.B, 2,  17, 1.0f, 1.3f, 5,  "thorn",   null,                "Nature",    "EarthSpike"),
            ("holy",    Rarity.B, 19, 15, 1.1f, 1.3f, 5,  "holy",    "holy-light-sword",  "Holy",      "HolySmite"),
            ("void",    Rarity.A, 5,  22, 0.9f, 1.5f, 8,  "void",    "rift-cut",          "Shadow",    "VoidImplosion"),
            ("gold",    Rarity.A, 22, 20, 1.0f, 1.4f, 6,  "gold",    null,                "Impacts",   "CritHit"),
            ("lava",    Rarity.A, 0,  24, 0.8f, 1.4f, 5,  "lava",    null,                "Fire",      "FireballImpact"),
            ("purple",  Rarity.A, 15, 21, 1.0f, 1.4f, 7,  null,      null,                "Arcane",    "ShieldShatter"),
            ("fire",    Rarity.S, 4,  28, 1.0f, 1.6f, 10, "fire",    "dragon-flame",      "Fire",      "FireballImpact"),
            ("thunder", Rarity.S, 12, 25, 1.2f, 1.5f, 12, "thunder", "thunder-judgement", "Lightning", "StaticBurst"),
        };

        static Dictionary<string, WeaponData> SeedWeapons(Dictionary<string, ActiveSkillSO> skills, Dictionary<string, WeaponPassiveSO> passives)
        {
            var d = new Dictionary<string, WeaponData>();
            var slash = Vfx("Melee", "SwordSlash") ?? Vfx("Melee", "SwordCombo");
            foreach (var t in WeaponTable)
            {
                string id = "sword-" + t.key;
                var wpn = Asset<WeaponData>("Weapons/Weapon_" + id, out bool w);
                if (w)
                {
                    wpn.id = id; wpn.rarity = t.rar; wpn.nameKey = "sword." + t.key;
                    wpn.descKey = t.key is "old" or "purple" ? "common.none" : "wp." + t.key + ".passive";
                    wpn.shopPrice = Price(t.rar);
                    wpn.weaponType = WeaponType.Melee; wpn.fireMode = RangedFireMode.Projectile;
                    wpn.damage = t.dmg; wpn.fireRate = 2f * t.aspd; wpn.attackRange = t.rng; wpn.critBonus = t.crit / 100f;
                    wpn.knockback = 5f; wpn.spread = 0f; wpn.manaCost = 0; wpn.projectileCount = 1; wpn.pierce = 0; wpn.explosionRadius = 0f;
                    wpn.swingArc = 110f; wpn.swingDuration = 0.15f; wpn.destroysProjectiles = true;
                }
                var sprite = Sword(t.sprite);
                Link(ref wpn.icon, sprite, $"sprite kiếm File_{t.sprite}");
                Link(ref wpn.weaponSprite, sprite, $"sprite kiếm File_{t.sprite}");
                Link(ref wpn.slashVfxPrefab, slash, "VFX_Melee_SwordSlash");
                Link(ref wpn.hitImpactVfxPrefab, Vfx(t.hitCat, t.hitName), $"VFX_{t.hitCat}_{t.hitName}");
                if (t.passive != null) Link(ref wpn.passive, passives[t.passive], $"passive {t.passive}");
                if (t.active != null) Link(ref wpn.activeSkill, skills[t.active], $"skill {t.active}");
                d[t.key] = wpn;
            }

            // Vũ khí tầm xa (Ranged)
            var muzzle = Vfx("Impacts", "HitSpark");
            void Gun(string key, Rarity rar, string sheetPath, string sliceName, string projPrefab, float dmg, float fireRate, float spd,
                int mana, int count, float spread, int pierce, float explosionRadius, string hitCat, string hitName, string passive = null)
            {
                string id = "gun-" + key;
                var wpn = Asset<WeaponData>("Weapons/Weapon_" + id, out bool w);
                if (w)
                {
                    wpn.id = id; wpn.rarity = rar; wpn.nameKey = "gun." + key; wpn.descKey = $"wp.{key}.passive";
                    wpn.shopPrice = Price(rar);
                    wpn.weaponType = WeaponType.Ranged; wpn.fireMode = RangedFireMode.Projectile;
                    wpn.damage = dmg; wpn.fireRate = fireRate; wpn.attackRange = 9f; wpn.critBonus = 0.08f;
                    wpn.knockback = 2.5f; wpn.spread = spread; wpn.manaCost = mana;
                    wpn.projectileSpeed = spd; wpn.projectileCount = count; wpn.pierce = pierce; wpn.explosionRadius = explosionRadius;
                }
                var sprite = SpriteNamed(sheetPath, sliceName);
                Link(ref wpn.icon, sprite, $"sprite súng {sliceName}");
                Link(ref wpn.weaponSprite, sprite, $"sprite súng {sliceName}");
                Link(ref wpn.projectilePrefab, Go(Prefabs + "Projectiles/" + projPrefab + ".prefab"), projPrefab);
                Link(ref wpn.muzzleVfxPrefab, muzzle, "VFX_Impacts_HitSpark");
                Link(ref wpn.hitImpactVfxPrefab, Vfx(hitCat, hitName), $"VFX_{hitCat}_{hitName}");
                if (passive != null) Link(ref wpn.passive, passives[passive], $"passive {passive}");
                d[key] = wpn;
            }

            Gun("pistol",   Rarity.C, GunSheet1, "W1_R00_0", "Projectile_Plasma_Blue",  12f, 3.2f, 14f, 1, 1, 3f,  0, 0f,   "Impacts",    "HitSpark");
            Gun("rifle",    Rarity.B, GunSheet2, "W2_R00_0", "Projectile_Plasma_Red",   15f, 4.5f, 16f, 2, 1, 4f,  1, 0f,   "Impacts",    "CritHit",      "rifle");
            Gun("shotgun",  Rarity.A, GunSheet1, "W1_R04_0", "Projectile_Sparks_Blast", 8f,  1.6f, 13f, 4, 5, 24f, 0, 0f,   "Lightning",  "StaticBurst");
            Gun("launcher", Rarity.S, GunSheet3, "W3_R00_0", "Projectile_Rocket",       36f, 1.0f, 11f, 6, 1, 0f,  0, 2.2f, "Explosions", "SmallExplosion");

            return d;
        }

        // =====================================================================================================
        // Trang bị — bảng UI-Equipment.dc.html (m: armor/hp/mana/atk/crit/aspd/spd, role, rarity)
        // =====================================================================================================

        static Dictionary<string, EquipmentData> SeedEquipment(Dictionary<string, ActiveSkillSO> skills, Dictionary<string, EquipmentPassiveSO> passives)
        {
            var d = new Dictionary<string, EquipmentData>();
            void E(string id, string key, string descKey, EquipmentSlotType slot, Rarity rar, CharacterRole role, (int c, int r) icon,
                StatModifier[] mods, string passive = null, string active = null)
            {
                var e = Asset<EquipmentData>("Equipment/Equip_" + id, out bool w);
                if (w)
                {
                    e.id = id; e.nameKey = "item." + key; e.descKey = descKey; e.slot = slot; e.rarity = rar; e.synergyRole = role;
                    e.shopPrice = Price(rar); e.baseModifiers = mods.ToList();
                }
                Link(ref e.icon, Item16(icon.c, icon.r), $"icon {id} ({icon.c},{icon.r})");
                if (passive != null) Link(ref e.synergyPassive, passives[passive], $"passive {passive}");
                if (active != null) Link(ref e.activeSkill, skills[active], $"skill {active}");
                d[id] = e;
            }

            E("horn", "horn", "eq.horn.passive", EquipmentSlotType.Helmet, Rarity.C, CharacterRole.Warrior, (11, 11),
                new[] { Flat(StatType.MaxArmor, 1) }, "horn");
            E("fire-armor", "fire_armor", "eq.fire.passive", EquipmentSlotType.Armor, Rarity.A, CharacterRole.Warrior, (4, 0),
                new[] { Flat(StatType.MaxArmor, 2), Flat(StatType.MaxHp, 1) }, "fire");
            E("night-gloves", "night_gloves", "eq.night.passive", EquipmentSlotType.Gloves, Rarity.A, CharacterRole.Assassin, (12, 10),
                new[] { Flat(StatType.CritChance, 0.08f), Pct(StatType.AttackSpeed, 0.10f) }, "night");
            E("wind-boots", "wind_boots", "eq.wind.passive", EquipmentSlotType.Boots, Rarity.B, CharacterRole.Ranger, (1, 4),
                new[] { Pct(StatType.MoveSpeed, 0.15f) }, "wind");
            E("rune-book", "rune_book", "eq.book.passive", EquipmentSlotType.Accessory, Rarity.B, CharacterRole.Mage, (13, 1),
                new[] { Flat(StatType.MaxMana, 30), Pct(StatType.AttackDamage, 0.10f) }, "book", "arcane-storm-book");
            E("holy-shield", "holy_shield", "eq.shield.passive", EquipmentSlotType.Accessory, Rarity.C, CharacterRole.Warrior, (4, 18),
                new[] { Flat(StatType.MaxArmor, 2) }, "shield", "raise-shield");
            E("silver-armor", "silver_armor", "eq.no_role", EquipmentSlotType.Armor, Rarity.C, CharacterRole.Any, (1, 0),
                new[] { Flat(StatType.MaxArmor, 1), Flat(StatType.MaxHp, 1) });
            E("mage-hat", "mage_hat", "eq.hat.passive", EquipmentSlotType.Helmet, Rarity.B, CharacterRole.Mage, (2, 11),
                new[] { Flat(StatType.MaxMana, 20), Pct(StatType.AttackDamage, 0.05f) }, "hat");
            E("ice-mask", "ice_mask", "eq.mask.passive", EquipmentSlotType.Helmet, Rarity.A, CharacterRole.Assassin, (8, 11),
                new[] { Flat(StatType.MaxArmor, 1), Flat(StatType.CritChance, 0.06f) }, "mask");
            E("silver-gloves", "silver_gloves", "eq.gwhite.passive", EquipmentSlotType.Gloves, Rarity.C, CharacterRole.Warrior, (11, 10),
                new[] { Flat(StatType.MaxArmor, 1), Pct(StatType.AttackSpeed, 0.05f) }, "gwhite");
            E("wind-necklace", "wind_necklace", "eq.neck.passive", EquipmentSlotType.Necklace, Rarity.B, CharacterRole.Ranger, (2, 13),
                new[] { Pct(StatType.MoveSpeed, 0.10f), Flat(StatType.CritChance, 0.05f) }, "neck");
            E("red-boots", "red_boots", "eq.no_role", EquipmentSlotType.Boots, Rarity.F, CharacterRole.Any, (3, 4), new[] { Pct(StatType.MoveSpeed, 0.05f) });
            E("gold-ring", "gold_ring", "eq.no_role", EquipmentSlotType.Accessory, Rarity.F, CharacterRole.Any, (0, 16),
                new[] { Flat(StatType.MaxHp, 1), Flat(StatType.MaxMana, 10) });
            return d;
        }

        // =====================================================================================================
        // Animation nhân vật đã ra mắt — tách sheet FantasyKnights (8 cột × 6 hàng, 48px) + Ranger/Wizard
        // Tên clip chứa từ khóa SpriteAnimDriver: idle / run / attack / hurt / death. State = tên clip.
        // =====================================================================================================

        static readonly (string id, string sheet)[] AnimatedHeroes =
        {
            ("knight", "01Knight"),
            ("paladin", "09Knight"),
            ("berserker", "02Knight"),
            ("shadow-knight", "06Knight"),
        };

        static Dictionary<string, RuntimeAnimatorController> SeedCharacterAnimations()
        {
            var d = new Dictionary<string, RuntimeAnimatorController>();
            foreach (var (id, sheet) in AnimatedHeroes)
            {
                string sheetPath = FkDir + sheet + ".png";
                var frames = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToDictionary(s => s.name);
                Sprite[] Range(params int[] idx) => idx.Select(i => frames.TryGetValue($"{sheet}_{i}", out var s) ? s : null).Where(s => s != null).ToArray();
                if (frames.Count < 48) { missing.Add($"sheet {sheetPath} (cần 48 sprite)"); continue; }

                string dir = AnimDir + id + "/";
                EnsureFolder(dir.TrimEnd('/'));
                var clips = new[]
                {
                    MakeClip(dir, id + "_idle", Range(1, 2, 3, 4, 5, 6, 7), 8f, true),
                    MakeClip(dir, id + "_run", Range(16, 17, 18, 19, 20, 21, 22, 23), 12f, true),
                    MakeClip(dir, id + "_attack", Range(32, 33, 34, 35, 36, 37), 14f, false),
                    MakeClip(dir, id + "_hurt", Range(40, 41), 10f, false),
                    MakeClip(dir, id + "_death", Range(42, 43, 44, 45, 46, 47), 8f, false),
                };

                d[id] = EnsureAnimatorController(dir, id, clips);
            }

            // Ranger: Green Knight idle 16x16 + run 16x17
            string gkIdlePath = Art + "Characters/Knight/Green Knight idle Sprite-sheet 16x16.png";
            string gkRunPath = Art + "Characters/Knight/Green Knight run Sprite-sheet 16x17.png";
            var gkIdle = AssetDatabase.LoadAllAssetsAtPath(gkIdlePath).OfType<Sprite>().ToDictionary(s => s.name);
            var gkRun = AssetDatabase.LoadAllAssetsAtPath(gkRunPath).OfType<Sprite>().ToDictionary(s => s.name);
            if (gkIdle.Count >= 8 && gkRun.Count >= 8)
            {
                Sprite[] IdleRange(params int[] idx) => idx.Select(i => gkIdle.TryGetValue($"Green Knight idle Sprite-sheet 16x16_{i}", out var s) ? s : null).Where(s => s != null).ToArray();
                Sprite[] RunRange(params int[] idx) => idx.Select(i => gkRun.TryGetValue($"Green Knight run Sprite-sheet 16x17_{i}", out var s) ? s : null).Where(s => s != null).ToArray();
                string rDir = AnimDir + "ranger/";
                EnsureFolder(rDir.TrimEnd('/'));
                var rClips = new[]
                {
                    MakeClip(rDir, "ranger_idle", IdleRange(0, 1, 2, 3, 4, 5, 6, 7), 8f, true),
                    MakeClip(rDir, "ranger_run", RunRange(0, 1, 2, 3, 4, 5, 6, 7), 12f, true),
                    MakeClip(rDir, "ranger_attack", RunRange(0, 1, 2, 3), 14f, false),
                    MakeClip(rDir, "ranger_hurt", IdleRange(0, 1), 10f, false),
                    MakeClip(rDir, "ranger_death", IdleRange(24, 25, 26, 27), 8f, false),
                };
                d["ranger"] = EnsureAnimatorController(rDir, "ranger", rClips);
            }

            // Wizard: Dungeon_Character_3..7
            var wizSprite = Dc(3);
            if (wizSprite != null)
            {
                string wDir = AnimDir + "wizard/";
                EnsureFolder(wDir.TrimEnd('/'));
                var wFrames = new[] { wizSprite };
                var wClips = new[]
                {
                    MakeClip(wDir, "wizard_idle", wFrames, 6f, true),
                    MakeClip(wDir, "wizard_run", wFrames, 10f, true),
                    MakeClip(wDir, "wizard_attack", wFrames, 12f, false),
                    MakeClip(wDir, "wizard_hurt", wFrames, 10f, false),
                    MakeClip(wDir, "wizard_death", wFrames, 8f, false),
                };
                d["wizard"] = EnsureAnimatorController(wDir, "wizard", wClips);
            }

            return d;
        }

        static RuntimeAnimatorController EnsureAnimatorController(string dir, string id, AnimationClip[] clips)
        {
            string ctrlPath = dir + id + ".controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                var sm = ctrl.layers[0].stateMachine;
                foreach (var c in clips)
                {
                    var st = sm.AddState(c.name);
                    st.motion = c;
                    if (c.name.EndsWith("_idle")) sm.defaultState = st;
                }
                created++;
            }
            else if (overwrite)
            {
                var sm = ctrl.layers[0].stateMachine;
                sm.states = System.Array.Empty<ChildAnimatorState>();
                foreach (var c in clips)
                {
                    var st = sm.AddState(c.name);
                    st.motion = c;
                    if (c.name.EndsWith("_idle")) sm.defaultState = st;
                }
                EditorUtility.SetDirty(ctrl);
                created++;
            }
            else kept++;
            return ctrl;
        }

        static AnimationClip MakeClip(string dir, string name, Sprite[] frames, float fps, bool loop)
        {
            string path = dir + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (!isNew && !overwrite) return clip;
            if (isNew) clip = new AnimationClip { name = name };
            clip.frameRate = fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < frames.Length; i++) keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = frames[i] });
            if (frames.Length > 0) keys.Add(new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[^1] }); // giữ frame cuối 1 nhịp
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
            return clip;
        }

        // =====================================================================================================
        // Nhân vật — 12 nhân vật; 6 nhân vật đã ra mắt (đủ 4 Role: Warrior, Ranger, Mage, Assassin) + 6 Coming Soon
        // =====================================================================================================

        static Dictionary<string, CharacterData> SeedCharacters(Dictionary<string, ActiveSkillSO> skills, Dictionary<string, WeaponData> weapons,
            Dictionary<string, RuntimeAnimatorController> anims)
        {
            var d = new Dictionary<string, CharacterData>();
            void C(string id, CharacterRole role, bool released, int price, Sprite portrait, float hp, float armor, float mana, float atk,
                float aspd, float spd, float crit, string weapon, string skill)
            {
                var c = Asset<CharacterData>("Characters/Char_" + id, out bool w);
                string key = id.Replace('-', '_');
                if (w)
                {
                    c.characterId = id; c.nameKey = $"char.{key}.name"; c.descKey = $"char.{key}.desc"; c.role = role;
                    c.unlockPrice = price; c.released = released;
                    c.baseStats = new BaseStats { maxHp = hp, maxArmor = armor, maxMana = mana, attackDamage = atk, attackSpeed = aspd, moveSpeed = spd, critChance = crit };
                    c.armorRegenDelay = 4f; c.armorRegenRate = 1f; c.manaRegen = 2f;
                }
                Link(ref c.portrait, portrait, $"portrait {id}");
                if (anims.TryGetValue(id, out var ctrl)) Link(ref c.animator, ctrl, $"animator {id}");
                Link(ref c.startingWeapon, weapons[weapon], $"vũ khí khởi đầu {id}");
                if (skill != null) Link(ref c.activeSkill, skills[skill], $"skill {id}");
                d[id] = c;
            }

            var greenKnight = Art + "Characters/Knight/Green Knight idle Sprite-sheet 16x16.png";
            // Đã ra mắt (6 nhân vật — phủ đủ 4 Role: Warrior, Ranger, Mage, Assassin)
            C("knight",        CharacterRole.Warrior,  true,  0,    Fk("01Knight"),          7, 6, 120, 14f / 13f, 1f,    5f,   0.05f, "iron",   "dual-wield");
            C("paladin",       CharacterRole.Warrior,  true,  1500, Fk("09Knight"),          8, 8, 100, 12f / 13f, 0.95f, 4.4f, 0.05f, "iron",   "holy-light");
            C("ranger",        CharacterRole.Ranger,   true,  1000, FirstSprite(greenKnight),5, 4, 160, 11f / 13f, 1.1f,  5.5f, 0.15f, "pistol", "dodge-roll");
            C("wizard",        CharacterRole.Mage,     true,  1200, Dc(3),                   4, 4, 220, 16f / 13f, 1f,    5f,   0.05f, "pistol", "arcane-storm");
            C("berserker",     CharacterRole.Warrior,  true,  2000, Fk("02Knight"),          8, 3, 100, 1.15f,     1.1f,  5f,   0.08f, "iron",   "dual-wield");
            C("shadow-knight", CharacterRole.Assassin, true,  2500, Fk("06Knight"),          6, 4, 120, 1.05f,     1.1f,  5.3f, 0.15f, "dark",   "rift-cut");
            // Chưa ra mắt (Coming Soon)
            C("rogue",         CharacterRole.Assassin, false, 800,  Dc(9),                   5, 3, 140, 1f,        1.2f,  5.5f, 0.2f,  "dark",   null);
            C("monk",          CharacterRole.Mage,     false, 1500, Dc(1),                   5, 3, 180, 0.9f,      1f,    5f,   0.05f, "iron",   null);
            C("lancer",        CharacterRole.Warrior,  false, 1500, Dc(0),                   7, 6, 100, 1f,        0.9f,  4.8f, 0.05f, "iron",   null);
            C("hunter",        CharacterRole.Ranger,   false, 1500, Dc(4),                   5, 3, 150, 1f,        1.1f,  5.5f, 0.12f, "iron",   null);
            C("necromancer",   CharacterRole.Mage,     false, 2000, Dc(13),                  4, 2, 240, 1.1f,      1f,    4.8f, 0.05f, "iron",   null);
            C("frost-wraith",  CharacterRole.Mage,     false, 2000, Dc(8),                   4, 3, 220, 1f,        1f,    5.2f, 0.05f, "iron",   null);
            return d;
        }

        // =====================================================================================================
        // Đòn quái
        // =====================================================================================================

        static Dictionary<string, EnemyAttackSO> SeedEnemyAttacks()
        {
            var d = new Dictionary<string, EnemyAttackSO>();
            var bolt = Go(Prefabs + "Projectiles/Projectile_Plasma_Red.prefab");
            var spark = Go(Prefabs + "Projectiles/Projectile_Sparks_Blast.prefab");
            var hit = Vfx("Impacts", "HitSpark");

            void Base(EnemyAttackSO a, float range, float cd, float windup, float weight = 1f, bool los = true, bool stop = true)
            { a.range = range; a.cooldown = cd; a.windupTime = windup; a.weight = weight; a.requireLineOfSight = los; a.stopWhileAttacking = stop; }

            MeleeAttackSO Melee(string id, float range, float cd, float windup, float dmg, float radius, float kb, GameObject vfx, string vfxName)
            {
                var a = Asset<MeleeAttackSO>("EnemyAttacks/Atk_" + id, out bool w);
                if (w) { Base(a, range, cd, windup); a.damage = dmg; a.hitRadius = radius; a.knockback = kb; }
                Link(ref a.slashVfx, vfx, vfxName);
                d[id] = a; return a;
            }
            ShootAttackSO Shoot(string id, float range, float cd, float windup, GameObject proj, float dmg, float speed, int count, float spread,
                bool radial = false, int bursts = 1, float interval = 0.15f, float spin = 10f)
            {
                var a = Asset<ShootAttackSO>("EnemyAttacks/Atk_" + id, out bool w);
                if (w)
                {
                    Base(a, range, cd, windup);
                    a.damage = dmg; a.speed = speed; a.knockback = 2f; a.lifetime = 4f; a.count = count; a.spreadAngle = spread;
                    a.radial = radial; a.bursts = bursts; a.burstInterval = interval; a.radialSpin = spin;
                }
                Link(ref a.projectilePrefab, proj, "Projectile cho " + id);
                Link(ref a.impactVfx, hit, "VFX_Impacts_HitSpark");
                d[id] = a; return a;
            }
            ChargeAttackSO Charge(string id, float range, float cd, float windup, float dmg, float distance, float speed)
            {
                var a = Asset<ChargeAttackSO>("EnemyAttacks/Atk_" + id, out bool w);
                if (w) { Base(a, range, cd, windup); a.damage = dmg; a.distance = distance; a.chargeSpeed = speed; a.hitRadius = 0.6f; a.knockback = 6f; }
                d[id] = a; return a;
            }

            Melee("spear-thrust", 1.4f, 1.6f, 0.45f, 1f, 0.7f, 4f, Vfx("Melee", "SpearThrust"), "VFX_Melee_SpearThrust");
            Melee("sword-slash", 1.1f, 1.4f, 0.35f, 1f, 0.8f, 4f, Vfx("Melee", "AxeSmear"), "VFX_Melee_AxeSmear");
            Melee("bite", 0.9f, 1.8f, 0.5f, 1f, 0.7f, 3f, Vfx("Impacts", "PunchHit"), "VFX_Impacts_PunchHit");
            Charge("warrior-charge", 5f, 5f, 0.7f, 1f, 5f, 14f);
            Shoot("blood-bolt", 7f, 2.2f, 0.4f, bolt, 1f, 6f, 1, 0f);
            Shoot("bolt-fan", 7f, 3.5f, 0.5f, bolt, 1f, 6f, 3, 30f);
            Shoot("skull-radial", 6f, 3f, 0.5f, spark, 1f, 4.5f, 8, 0f, radial: true);
            Shoot("lord-spiral", 9f, 4f, 0.6f, spark, 1f, 5f, 10, 0f, radial: true, bursts: 4, interval: 0.2f, spin: 12f);
            Shoot("lord-fan", 8f, 3f, 0.5f, bolt, 1f, 6.5f, 5, 50f, bursts: 3, interval: 0.25f);
            Charge("lord-charge", 7f, 6f, 0.8f, 2f, 6f, 16f);

            var summon = Asset<SummonAttackSO>("EnemyAttacks/Atk_lord-summon", out bool sw);
            if (sw) { Base(summon, 12f, 9f, 0.8f, 1f, false, true); summon.count = 2; summon.radius = 1.5f; summon.maxAlive = 4; }
            Link(ref summon.minionPrefab, EnemyPrefab("Enemy_Skeleton_Spear"), "Enemy_Skeleton_Spear (minion)");
            d["lord-summon"] = summon;
            return d;
        }

        // =====================================================================================================
        // Quái (+ mini-boss tầng 1–2, boss Vampire Lord tầng 3)
        // =====================================================================================================

        static Dictionary<string, EnemyData> SeedEnemies(Dictionary<string, EnemyAttackSO> atk)
        {
            var d = new Dictionary<string, EnemyData>();
            void N(string id, string nameKey, float hp, float spd, float pref, int cost, float kbRes, bool flying, string[] attacks, LootTable loot,
                float spawnDur = 0.4f, float deathDur = 0.8f)
            {
                var e = Asset<EnemyData>("Enemies/Enemy_" + id, out bool w);
                if (w)
                {
                    e.enemyId = id; e.nameKey = nameKey; e.maxHp = hp; e.moveSpeed = spd; e.preferredDistance = pref; e.spawnCost = cost;
                    e.knockbackResist = kbRes; e.isFlying = flying; e.contactDamage = 1f; e.contactCooldown = 0.8f; e.aggroRange = 12f;
                    e.spawnDuration = spawnDur; e.deathDuration = deathDur; e.lootTable = loot;
                    e.attacks = attacks.Select(a => atk[a]).ToList();
                }
                d[id] = e;
            }
            LootTable L(int min, int max, float mana, float potion, float item) =>
                new() { minGold = min, maxGold = max, manaOrbChance = mana, manaOrbs = 1, healthPotionChance = potion, itemChance = item, weapons = true, equipment = true };

            N("skeleton-spear", "enemy.skeleton_spear", 40, 2.4f, 1.0f, 1, 0f, false, new[] { "spear-thrust" }, L(1, 3, 0.3f, 0.02f, 0.02f));
            N("skeleton-warrior", "enemy.skeleton_warrior", 60, 2.2f, 0.6f, 2, 0.2f, false, new[] { "sword-slash", "warrior-charge" }, L(2, 4, 0.3f, 0.03f, 0.03f));
            N("zombie", "enemy.zombie", 80, 1.4f, 0.4f, 2, 0.4f, false, new[] { "bite" }, L(1, 4, 0.3f, 0.03f, 0.02f));
            N("vampire", "enemy.vampire", 45, 2.8f, 4f, 2, 0f, false, new[] { "blood-bolt" }, L(2, 4, 0.4f, 0.03f, 0.03f));
            N("skull", "enemy.skull", 30, 2.6f, 3.5f, 2, 0f, true, new[] { "skull-radial" }, L(1, 3, 0.4f, 0.02f, 0.02f));
            N("elite-warrior", "enemy.elite_warrior", 280, 2.4f, 0.6f, 6, 0.6f, false, new[] { "sword-slash", "warrior-charge" }, L(15, 25, 1f, 0.5f, 1f), 0.8f, 1.2f);
            N("elite-vampire", "enemy.elite_vampire", 260, 2.8f, 4f, 6, 0.5f, false, new[] { "bolt-fan", "blood-bolt" }, L(20, 30, 1f, 0.5f, 1f), 0.8f, 1.2f);
            N("vampire-lord", "boss.vampire_lord", 900, 2.6f, 3f, 10, 0.9f, false, new[] { "lord-fan", "lord-charge" }, L(40, 60, 1f, 1f, 1f), 1f, 2f);
            return d;
        }

        static readonly (string prefab, string enemy)[] EnemyPrefabMap =
        {
            ("Enemy_Skeleton_Spear", "skeleton-spear"), ("Enemy_Skeleton_Warrior", "skeleton-warrior"),
            ("Enemy_Zombie", "zombie"), ("Enemy_Vampire", "vampire"), ("Enemy_Skull", "skull"),
            ("Boss_EliteWarrior", "elite-warrior"), ("Boss_EliteVampire", "elite-vampire"), ("Boss_VampireLord", "vampire-lord"),
        };

        static void LinkEnemyPrefabs(Dictionary<string, EnemyData> enemies)
        {
            foreach (var (prefabName, enemyId) in EnemyPrefabMap)
            {
                string path = Prefabs + "Enemies/" + prefabName + ".prefab";
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) { missing.Add($"prefab {path} (cho EnemyData {enemyId})"); continue; }
                var enemy = root.GetComponent<Enemy>();
                if (enemy == null) { missing.Add($"component Enemy trên {path}"); continue; }
                var so = new SerializedObject(enemy);
                var prop = so.FindProperty("data");
                if (prop.objectReferenceValue != null && !overwrite) continue;
                prop.objectReferenceValue = enemies[enemyId];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SavePrefabAsset(root);
                Debug.Log($"[Data] Gán EnemyData '{enemyId}' vào {prefabName}.");
            }
        }

        // =====================================================================================================
        // DungeonConfig tầng 1–3 (theme Dungeon)
        // =====================================================================================================

        static DungeonConfig[] SeedDungeonConfigs()
        {
            var baseRoom = Comp<RoomTemplate>(Prefabs + "Rooms/RoomTemplate.prefab");
            var startRoom = Comp<RoomTemplate>(Prefabs + "Rooms/Room_Start.prefab") ?? baseRoom;
            var combatRoom = Comp<RoomTemplate>(Prefabs + "Rooms/Room_Combat.prefab") ?? baseRoom;
            var treasureRoom = Comp<RoomTemplate>(Prefabs + "Rooms/Room_Treasure.prefab") ?? baseRoom;
            var shopRoom = Comp<RoomTemplate>(Prefabs + "Rooms/Room_Shop.prefab") ?? baseRoom;
            var bossRoom = Comp<RoomTemplate>(Prefabs + "Rooms/Room_Boss.prefab") ?? baseRoom;

            var floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(TilesDir + "Dungeon_Floor.asset");
            var wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(TilesDir + "Dungeon_Wall.asset");
            var sealTile = AssetDatabase.LoadAssetAtPath<TileBase>(TilesDir + "Dungeon_SealWall.asset") ?? wallTile;

            var spear = EnemyPrefab("Enemy_Skeleton_Spear");
            var warrior = EnemyPrefab("Enemy_Skeleton_Warrior");
            var zombie = EnemyPrefab("Enemy_Zombie");
            var vampire = EnemyPrefab("Enemy_Vampire");
            var skull = EnemyPrefab("Enemy_Skull");
            var bosses = new[] { EnemyPrefab("Boss_EliteWarrior"), EnemyPrefab("Boss_EliteVampire"), EnemyPrefab("Boss_VampireLord") };
            var bossAddresses = new[] { AddressKeys.Boss("elite-warrior"), AddressKeys.Boss("elite-vampire"), AddressKeys.Boss("vampire-lord") };

            var list = new DungeonConfig[3];
            for (int f = 1; f <= 3; f++)
            {
                var c = Asset<DungeonConfig>($"DungeonConfigs/DungeonConfig_dungeon_F{f}", out bool w);
                if (w)
                {
                    c.themeId = "dungeon"; c.floor = f;
                    c.mainPathLength = 4 + f; c.branchCount = f == 1 ? 1 : 2; c.branchLength = new Vector2Int(1, 2);
                    c.gridSize = new Vector2Int(6 + f, 6 + f); c.cellSpacing = 26;
                    c.includeTreasure = true; c.includeShop = true;
                    c.corridorWidth = 3; c.sealDepth = 2;
                    c.spawnBudget = 2 + 2 * f; c.budgetGrowth = f == 1 ? 1 : 2; c.wavesPerRoom = f == 1 ? new Vector2Int(2, 2) : new Vector2Int(2, 3);
                    c.enemyHealthMultiplier = 1f + 0.25f * (f - 1); c.spawnWarningTime = 1f;
                    c.bossAddress = bossAddresses[f - 1];
                    c.bossLoadTimeout = 20f; c.chestOnClearChance = 0.2f + 0.05f * f;
                    c.enemyTable = new List<EnemySpawnEntry>();
                }
                if (startRoom != null && (c.startRooms.Count == 0 || overwrite)) c.startRooms = new List<RoomTemplate> { startRoom };
                if (combatRoom != null && (c.combatRooms.Count == 0 || overwrite)) c.combatRooms = new List<RoomTemplate> { combatRoom };
                if (treasureRoom != null && (c.treasureRooms.Count == 0 || overwrite)) c.treasureRooms = new List<RoomTemplate> { treasureRoom };
                if (shopRoom != null && (c.shopRooms.Count == 0 || overwrite)) c.shopRooms = new List<RoomTemplate> { shopRoom };
                if (bossRoom != null && (c.bossRooms.Count == 0 || overwrite)) c.bossRooms = new List<RoomTemplate> { bossRoom };
                if (combatRoom == null) missing.Add("RoomTemplate.prefab cho DungeonConfig");

                Link(ref c.corridorFloorTile, floorTile, $"Dungeon_Floor.asset cho F{f}");
                Link(ref c.corridorWallTile, wallTile, $"Dungeon_Wall.asset cho F{f}");
                Link(ref c.sealWallTile, sealTile, $"Dungeon_SealWall.asset cho F{f}");

                if (c.enemyTable.Count == 0 || overwrite)
                {
                    c.enemyTable = new List<EnemySpawnEntry>();
                    void Add(Enemy p, int weight, int prewarm) { if (p != null) c.enemyTable.Add(new EnemySpawnEntry { prefab = p, weight = weight, prewarm = prewarm }); }
                    if (f == 1) { Add(spear, 10, 4); Add(zombie, 6, 3); }
                    if (f == 2) { Add(spear, 8, 4); Add(zombie, 5, 3); Add(warrior, 8, 3); Add(vampire, 6, 3); Add(skull, 4, 2); }
                    if (f == 3) { Add(spear, 6, 3); Add(zombie, 5, 3); Add(warrior, 8, 4); Add(vampire, 8, 4); Add(skull, 6, 3); }
                }
                Link(ref c.bossPrefab, bosses[f - 1], $"boss tầng {f} (Prefabs/Enemies/{EnemyPrefabMap[4 + f].prefab}.prefab)");
                EditorUtility.SetDirty(c);
                list[f - 1] = c;
            }
            return list;
        }

        // =====================================================================================================
        // Catalog + PlayPrefabSet (fallback khi chưa có Addressables)
        // =====================================================================================================

        static ItemCatalog SeedCatalog(IEnumerable<ItemData> items, Dictionary<string, CharacterData> chars)
        {
            var cat = Asset<ItemCatalog>("Catalog/ItemCatalog", out _);
            var so = new SerializedObject(cat);
            var itemsProp = so.FindProperty("items");
            var charsProp = so.FindProperty("characters");
            AppendMissing(itemsProp, items.Cast<Object>());
            AppendMissing(charsProp, chars.Values.Cast<Object>());
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cat);
            return cat;
        }

        static void AppendMissing(SerializedProperty list, IEnumerable<Object> values)
        {
            var have = new HashSet<Object>();
            for (int i = 0; i < list.arraySize; i++) have.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var v in values)
            {
                if (v == null || have.Contains(v)) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = v;
            }
        }

        static void LinkPlayPrefabSet(ItemCatalog catalog, CharacterData knight, DungeonConfig[] configs)
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayPrefabSet>(PlayPrefabSetPath);
            if (set == null) { missing.Add($"{PlayPrefabSetPath} (chạy Tools ▸ Dungeon Gathering ▸ Build Entity Prefabs trước)"); return; }
            Link(ref set.fallbackCatalog, catalog, "fallbackCatalog");
            Link(ref set.fallbackCharacter, knight, "fallbackCharacter");
            if (set.fallbackConfigs == null || set.fallbackConfigs.Length == 0 || set.fallbackConfigs.Any(c => c == null) || overwrite)
                set.fallbackConfigs = configs;
            EditorUtility.SetDirty(set);
        }

        // =====================================================================================================
        // Validate
        // =====================================================================================================

        [MenuItem("Tools/Dungeon Gathering/Data/Validate")]
        public static void Validate()
        {
            var errors = new List<string>();
            var warns = new List<string>();
            var keys = LoadLocalizationKeys();
            void Key(string k, Object o, string field)
            {
                if (string.IsNullOrEmpty(k)) errors.Add($"{o.name}: {field} trống");
                else if (keys != null && !keys.Contains(k)) errors.Add($"{o.name}: {field} '{k}' không có trong DG_Multilanguage.csv");
            }

            var items = FindAll<ItemData>();
            foreach (var g in items.GroupBy(i => i.id).Where(g => g.Count() > 1)) errors.Add($"Trùng id item '{g.Key}'");
            foreach (var it in items)
            {
                Key(it.nameKey, it, "nameKey"); Key(it.descKey, it, "descKey");
                if (it.icon == null) errors.Add($"{it.name}: thiếu icon");
                if (it is WeaponData w && w.weaponSprite == null) errors.Add($"{it.name}: thiếu weaponSprite");
            }

            var chars = FindAll<CharacterData>();
            foreach (var g in chars.GroupBy(c => c.characterId).Where(g => g.Count() > 1)) errors.Add($"Trùng id nhân vật '{g.Key}'");
            foreach (var c in chars)
            {
                Key(c.nameKey, c, "nameKey"); Key(c.descKey, c, "descKey");
                if (c.portrait == null) errors.Add($"{c.name}: thiếu portrait");
                if (c.startingWeapon == null) errors.Add($"{c.name}: thiếu startingWeapon");
                if (c.released)
                {
                    if (c.animator == null) errors.Add($"{c.name}: đã ra mắt nhưng thiếu animator");
                    if (c.activeSkill == null) errors.Add($"{c.name}: đã ra mắt nhưng thiếu activeSkill");
                }
            }
            if (!chars.Any(c => c.released)) errors.Add("Không có nhân vật nào đã ra mắt");

            foreach (var s in FindAll<ActiveSkillSO>()) { Key(s.nameKey, s, "nameKey"); Key(s.descKey, s, "descKey"); }

            var enemies = FindAll<EnemyData>();
            foreach (var g in enemies.GroupBy(e => e.enemyId).Where(g => g.Count() > 1)) errors.Add($"Trùng id quái '{g.Key}'");
            foreach (var e in enemies)
            {
                Key(e.nameKey, e, "nameKey");
                if (e.attacks == null || e.attacks.Count == 0 || e.attacks.Any(a => a == null)) errors.Add($"{e.name}: danh sách đòn rỗng / có phần tử null");
            }
            foreach (var (prefabName, _) in EnemyPrefabMap)
            {
                var p = EnemyPrefab(prefabName);
                if (p == null) warns.Add($"Chưa có prefab Enemies/{prefabName}");
                else if (p.Data == null) errors.Add($"Prefab {prefabName} chưa gán EnemyData");
            }

            foreach (var c in FindAll<DungeonConfig>())
            {
                if (c.combatRooms.Count == 0 || c.combatRooms.Any(r => r == null)) errors.Add($"{c.name}: chưa có RoomTemplate");
                if (c.enemyTable.Count == 0) errors.Add($"{c.name}: bảng quái rỗng");
                foreach (var e in c.enemyTable) if (e.prefab == null || e.prefab.Data == null) errors.Add($"{c.name}: quái trong bảng thiếu prefab/EnemyData");
                if (c.bossPrefab == null) warns.Add($"{c.name}: chưa có boss prefab — phòng Boss sẽ tự mở, run không kết thúc được ở tầng này");
                if (c.corridorFloorTile == null) warns.Add($"{c.name}: chưa có tile hành lang");
            }

            var cat = AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root + "Catalog/ItemCatalog.asset");
            if (cat == null) errors.Add("Thiếu Data/Catalog/ItemCatalog.asset");
            else
            {
                if (cat.Items.Count != items.Count) errors.Add($"ItemCatalog có {cat.Items.Count}/{items.Count} item");
                if (cat.Characters.Count != chars.Count) errors.Add($"ItemCatalog có {cat.Characters.Count}/{chars.Count} nhân vật");
            }

            foreach (var w in warns) Debug.LogWarning("[Data][Validate] " + w);
            foreach (var e in errors) Debug.LogError("[Data][Validate] " + e);
            Debug.Log($"[Data][Validate] {items.Count} item · {chars.Count} nhân vật ({chars.Count(c => c.released)} đã ra mắt) · {enemies.Count} quái · " +
                      $"{errors.Count} lỗi · {warns.Count} cảnh báo.");
        }

        static HashSet<string> LoadLocalizationKeys()
        {
            string csv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Design", "localization", "DG_Multilanguage.csv"));
            if (!File.Exists(csv)) return null;
            var set = new HashSet<string>();
            foreach (var line in File.ReadLines(csv).Skip(1))
            {
                int comma = line.IndexOf(',');
                if (comma > 0) set.Add(line.Substring(0, comma));
            }
            return set;
        }

        static List<T> FindAll<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root.TrimEnd('/') })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null).ToList();

        // =====================================================================================================
        // Tiện ích
        // =====================================================================================================

        static T Asset<T>(string rel, out bool write) where T : ScriptableObject
        {
            string path = Root + rel + ".asset";
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            bool isNew = a == null;
            if (isNew)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
                created++;
            }
            else if (!overwrite) kept++;
            write = isNew || overwrite;
            EditorUtility.SetDirty(a);
            return a;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Điền tham chiếu khi còn trống (hoặc khi ghi đè). Không có giá trị → ghi vào danh sách thiếu.</summary>
        static void Link<T>(ref T field, T value, string what) where T : Object
        {
            if (value == null) { missing.Add(what); return; }
            if (field == null || overwrite) field = value;
        }

        static int Price(Rarity r) => r switch { Rarity.F => 30, Rarity.C => 60, Rarity.B => 80, Rarity.A => 110, _ => 160 };
        static StatModifier Flat(StatType t, float v) => new(t, v);
        static StatModifier Pct(StatType t, float v) => new(t, 0f, v);

        static GameObject Go(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
        static T Comp<T>(string path) where T : Component { var g = Go(path); return g != null ? g.GetComponent<T>() : null; }
        static Enemy EnemyPrefab(string name) => Comp<Enemy>(Prefabs + "Enemies/" + name + ".prefab");
        static GameObject Vfx(string cat, string name) => Go($"{Prefabs}VFX/{cat}/VFX_{cat}_{name}.prefab");

        static Sprite SpriteNamed(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
        /// <summary>Frame đầu của sheet: ưu tiên "&lt;tên file&gt;_0", không có thì ô trên-trái.</summary>
        static Sprite FirstSprite(string path) =>
            SpriteNamed(path, Path.GetFileNameWithoutExtension(path) + "_0")
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).FirstOrDefault();
        static Sprite Sword(int i) => SpriteNamed(SwordSheet, "File_" + i);
        /// <summary>
        /// Frame thứ <paramref name="i"/> (0..13, nửa trên sheet) nhưng lấy bản SẠCH ở nửa dưới (i + 14):
        /// nửa trên Dungeon_Character có chấm đỏ/xanh dưới chân, nửa dưới là cùng hình không có chấm.
        /// </summary>
        static Sprite Dc(int i) => SpriteNamed(DcSheet, "Dungeon_Character_" + (i < 14 ? i + 14 : i));
        static Sprite Fk(string sheet) => SpriteNamed(FkDir + sheet + ".png", sheet + "_1");

        /// <summary>Ô (cột, hàng) trên sheet 16×16, gốc trên-trái — cùng quy ước UIDesignBuilder.Item(c, r).</summary>
        static Sprite Item16(int c, int r)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemSheet);
            if (tex == null) return null;
            var p = new Vector2(c * 16 + 8, tex.height - (r * 16 + 8));
            return AssetDatabase.LoadAllAssetsAtPath(ItemSheet).OfType<Sprite>().FirstOrDefault(s => s.rect.Contains(p));
        }
    }
}
