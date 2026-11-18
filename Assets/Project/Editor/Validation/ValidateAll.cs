using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonGathering.Catalog;
using DungeonGathering.Enemies;
using DungeonGathering.Play;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Kiểm tra toàn bộ dữ liệu game sau mỗi lần sửa nội dung: key đa ngôn ngữ, catalog (id trùng, vũ khí thiếu entry thư
    /// viện), hero (profile, skill), quái / boss (profile, skill, hình), thư viện skill (số enum, trùng key), level
    /// (template từng loại phòng, bảng quái, sinh thử map), PlayPrefabSet. Lỗi = cần sửa; Cảnh báo = nên xem.
    /// </summary>
    public static class ValidateAll
    {
        [MenuItem("Tools/Dungeon Gathering/Validate All")]
        public static void RunMenu()
        {
            var report = Run(out int errors, out int warnings);
            if (errors > 0) Debug.LogError(report);
            else if (warnings > 0) Debug.LogWarning(report);
            else Debug.Log(report);
        }

        public static string Run(out int errors, out int warnings)
        {
            var sb = new StringBuilder();
            int e = 0, w = 0;
            void Err(string m) { e++; sb.AppendLine("  LỖI  " + m); }
            void Warn(string m) { w++; sb.AppendLine("  CẢNH BÁO  " + m); }

            // ---- Đa ngôn ngữ
            LocalizationKeyValidator.Run(out int locProblems);
            if (locProblems > 0) Err($"Đa ngôn ngữ: {locProblems} key thiếu / thiếu bản dịch (chạy Localization ▸ Validate Keys xem chi tiết)");

            // ---- Catalog
            var cat = Find<ItemCatalog>().FirstOrDefault();
            if (cat == null) { Err("Không có ItemCatalog"); errors = e; warnings = w; return sb.ToString(); }
            cat.Register();
            var ids = new HashSet<string>();
            foreach (var it in cat.Items)
            {
                if (it == null) { Err("ItemCatalog có phần tử null"); continue; }
                if (string.IsNullOrEmpty(it.id)) Err($"{it.name}: thiếu id");
                else if (!ids.Add(it.id)) Err($"id vật phẩm trùng: {it.id}");
                if (it is WeaponData wd && !WeaponLibrary.HasEntry(wd.id)) Err($"Vũ khí {wd.id}: chưa có entry trong Sword / Ranged Library");
                if (string.IsNullOrEmpty(it.NameKey)) Warn($"{it.id}: thiếu nameKey");
                if (it.Icon == null) Warn($"{it.id}: thiếu icon");
            }
            if (cat.SwordLibrary == null || cat.SwordLibrary.baseConfig == null) Err("ItemCatalog: thiếu SwordLibrary hoặc SO gốc kiếm");
            if (cat.RangedLibrary == null || cat.RangedLibrary.baseConfig == null) Err("ItemCatalog: thiếu RangedLibrary hoặc SO gốc tầm xa");
            void CheckEntries(IEnumerable<WeaponEntryBase> entries)
            {
                foreach (var en in entries)
                {
                    if (en == null) continue;
                    if (cat.Get<WeaponData>(en.id) == null) Warn($"Entry vũ khí {en.id}: không có WeaponData cùng id trong catalog (không xuất hiện trong game)");
                    if (en.sprite == null) Warn($"Entry vũ khí {en.id}: thiếu sprite");
                    string proj = en is SwordEntry s ? s.waveProjectileId : en is RangedEntry r ? r.projectileId : null;
                    if (!string.IsNullOrEmpty(proj) && !WeaponLibrary.TryGetProjectile(proj, out _)) Err($"Entry vũ khí {en.id}: đạn '{proj}' không có trong ProjectileLibrary");
                }
            }
            if (cat.SwordLibrary != null) CheckEntries(cat.SwordLibrary.swords);
            if (cat.RangedLibrary != null) CheckEntries(cat.RangedLibrary.weapons);

            // ---- Skill
            var lib = cat.SkillLibrary;
            if (lib == null) Err("ItemCatalog: thiếu SkillLibrary");
            else
            {
                var keys = new HashSet<string>();
                var values = new Dictionary<int, string>();
                var bySo = new Dictionary<SkillBaseSO, string>();
                foreach (var s in lib.skills)
                {
                    if (s == null) continue;
                    if (s.skill == null) Err($"Skill {s.key}: chưa gán SO hành vi");
                    else if (bySo.TryGetValue(s.skill, out var other))
                        Err($"Skill {s.key} dùng chung SO '{s.skill.name}' với {other}: runtime tra ghi đè cooldown / mana / VFX theo SO nên entry sau bị bỏ qua — tạo SO riêng (Duplicate asset)");
                    else bySo[s.skill] = s.key;
                    if (s.value > 0 && values.TryGetValue(s.value, out var dup))
                        Err($"Skill {s.key} trùng số enum {s.value} với {dup} — chạy Skills ▸ Generate SkillId");
                    else if (s.value > 0) values[s.value] = s.key;
                    if (string.IsNullOrEmpty(s.key) || !keys.Add(s.key)) Err($"Skill key trống hoặc trùng: '{s.key}'");
                    if (s.value <= 0 || !Enum.IsDefined(typeof(SkillId), s.value)) Err($"Skill {s.key}: chưa có trong enum SkillId — chạy Skills ▸ Generate SkillId");
                }
            }

            // ---- Hero
            foreach (var h in cat.Characters)
            {
                if (h == null) continue;
                if (h.stats == null || h.stats.profile == null) Err($"Hero {h.characterId}: chưa chọn StatProfile");
                if (h.ResolveSkill() == null) Warn($"Hero {h.characterId}: skill chưa chọn hoặc không phải skill hero");
                if (h.startingWeapon == null) Err($"Hero {h.characterId}: thiếu vũ khí khởi đầu");
                if (h.animator == null) Warn($"Hero {h.characterId}: thiếu animator");
            }

            // ---- Quái / boss
            foreach (var m in Find<MonsterDefinitionSO>())
            {
                if (m.stats == null || m.stats.profile == null) Err($"Quái {m.enemyId}: chưa chọn StatProfile");
                var atk = m.ResolveAttacks();
                if (m.skills.Count == 0) Warn($"Quái {m.enemyId}: không có skill / đòn nào (chỉ gây sát thương chạm)");
                else if (atk.Count != m.skills.Count) Err($"Quái {m.enemyId}: {m.skills.Count - atk.Count} skill không phải đòn quái hoặc chưa đăng ký");
                foreach (var p in m.phases)
                    if (p.ResolveAttacks().Count != p.skills.Count) Err($"Boss {m.enemyId}: phase {p.hpThreshold:P0} có skill sai loại / chưa đăng ký");
                if (m.tier != Core.MonsterTier.Boss && m.animator == null) Err($"Quái {m.enemyId}: thiếu animator (prefab nền dùng chung cần hình từ định nghĩa)");
            }

            // ---- PlayPrefabSet
            var set = Find<PlayPrefabSet>().FirstOrDefault();
            if (set == null) Err("Không có PlayPrefabSet");
            else
            {
                if (set.monsterBase == null) Err("PlayPrefabSet: thiếu monsterBase (Enemy_Base)");
                if (set.lootConfig == null) Err("PlayPrefabSet: thiếu LootConfig");
                if (set.monsterTiers == null) Warn("PlayPrefabSet: thiếu MonsterTierConfig (mọi bậc hệ số 1)");
            }

            // ---- Level
            var campaign = cat.Campaign;
            var levels = Find<LevelConfigSO>().OrderBy(l => l.floor).ToList();
            if (campaign == null) Err("ItemCatalog: thiếu Campaign");
            else if (campaign.LevelCount != levels.Count) Warn($"Campaign có {campaign.LevelCount} level, project có {levels.Count} LevelConfig");
            foreach (var lv in levels)
            {
                foreach (var t in new[] { RoomType.Start, RoomType.Combat, RoomType.Boss })
                    if (lv.RoomsFor(t).Count == 0) Err($"{lv.name}: không có template phòng {t}");
                foreach (var sp in lv.specialRooms)
                    if (sp.count > 0 && lv.RoomsFor(sp.type).Count == 0) Err($"{lv.name}: không có template phòng {sp.type}");
                if (lv.enemyTable.Count == 0) Warn($"{lv.name}: bảng quái trống");
                foreach (var en in lv.enemyTable) if (!en.IsValid) Err($"{lv.name}: entry bảng quái chưa chọn định nghĩa");
                if (string.IsNullOrEmpty(lv.bossAddress)) Err($"{lv.name}: thiếu bossAddress");
                int bad = 0;
                for (int seed = 1; seed <= 50; seed++)
                {
                    var gs = lv.ToGeneratorSettings(new System.Random(seed));
                    var layout = DungeonGenerator.Generate(seed, gs);
                    if (layout.Rooms.Count(r => r.Type == RoomType.Combat) != gs.CombatRooms) bad++;
                }
                if (bad > 0) Err($"{lv.name}: {bad}/50 map sinh sai số phòng (lưới {lv.gridSize} quá nhỏ?)");
                if (campaign != null && !string.IsNullOrEmpty(campaign.themeId) && lv.themeId != campaign.themeId)
                    Warn($"{lv.name}: themeId '{lv.themeId}' khác campaign '{campaign.themeId}'");
            }

            // ---- Set trang bị: danh sách pieces phải khớp các trang bị trỏ về set (game đếm theo EquipmentSO.equipmentSet)
            var equipment = cat.Items.OfType<EquipmentSO>().ToList();
            var setIds = new HashSet<string>();
            foreach (var es in Find<EquipmentSetSO>())
            {
                if (string.IsNullOrEmpty(es.setId) || !setIds.Add(es.setId)) Err($"Set {es.name}: setId trống hoặc trùng");
                var actual = equipment.Where(x => x.equipmentSet == es).ToList();
                var listed = es.pieces.Where(x => x != null).ToList();
                foreach (var x in actual.Except(listed)) Warn($"Set {es.setId}: {x.id} trỏ về set nhưng không có trong pieces");
                foreach (var x in listed.Except(actual)) Warn($"Set {es.setId}: {x.id} có trong pieces nhưng equipmentSet không trỏ về set");
                if (es.bonus4Passive != null && actual.Count < 4) Warn($"Set {es.setId}: có bonus 4 món nhưng chỉ {actual.Count} món thuộc set");
            }

            // ---- Skill: cấu hình dễ sai
            if (lib != null)
                foreach (var s in lib.skills)
                {
                    if (s?.skill is DungeonGathering.Combat.SummonCompanionSkill sc && sc.companionSprite == null)
                        Warn($"Skill {s.key}: chưa gán companionSprite (đệ tử mượn hình hero)");
                    if (s?.skill is TeleportAttackSO tp && tp.followUpAttack is TeleportAttackSO)
                        Err($"Skill {s.key}: followUpAttack là Teleport (vòng lặp) — runtime sẽ bỏ qua");
                }

            int monsters = Find<MonsterDefinitionSO>().Count();
            sb.Insert(0, $"[Validate All] {e} lỗi · {w} cảnh báo — đã kiểm {cat.Items.Count()} vật phẩm, " +
                         $"{cat.Characters.Count()} hero, {monsters} quái/boss, {lib?.skills.Count ?? 0} skill, {levels.Count} level\n");
            errors = e;
            warnings = w;
            return sb.ToString();
        }

        static IEnumerable<T> Find<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Project/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(x => x != null);
    }
}
