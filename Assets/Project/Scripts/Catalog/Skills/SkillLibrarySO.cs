using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Một skill trong thư viện: key + số enum cố định + SO hành vi + ghi đè thời gian hồi / mana + VFX / âm thanh khi
    /// kích hoạt. Hero, quái, boss chỉ chọn <see cref="SkillId"/>.
    /// </summary>
    [Serializable]
    public sealed class SkillEntry
    {
        [Tooltip("Key định danh (chữ thường, gạch ngang). Tên enum SkillId sinh từ key này.")]
        public string key;
        [Tooltip("Số của enum SkillId — cố định, do menu Generate SkillId cấp. Không sửa tay.")]
        public int value;
        [Tooltip("SO hành vi (ActiveSkillSO cho hero / vật phẩm, EnemyAttackSO cho quái / boss) kèm tham số mặc định")]
        public SkillBaseSO skill;

        [Header("Ghi đè")]
        [Tooltip("Thời gian hồi (giây). Bỏ tick = cooldown của SO hành vi")] public Overridable<float> cooldown;
        [Tooltip("Chỉ áp cho skill của hero / vật phẩm")] public Overridable<float> manaCost;

        [Header("Hiệu ứng khi kích hoạt")]
        [Tooltip("VFX phát tại người dùng skill lúc kích hoạt (ngoài VFX riêng của SO hành vi)")]
        public GameObject castVfx;
        [Tooltip("Âm phát khi dùng skill (hero và quái/boss), thêm vào âm riêng của SO hành vi. Trống → không phát")] public AudioClip castSfx;
    }

    /// <summary>
    /// Thư viện skill của cả game. Thêm skill: thêm entry (key + SO hành vi) → menu
    /// Tools ▸ Dungeon Gathering ▸ Skills ▸ Generate SkillId → chọn enum trên hero / quái / phase boss.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Skill Library", fileName = "SkillLibrary")]
    public sealed class SkillLibrarySO : ScriptableObject
    {
        [Tooltip("Mọi skill của game; thêm entry xong chạy menu Generate SkillId")] public List<SkillEntry> skills = new();

        [Tooltip("Số enum SkillId lớn nhất từng cấp (kể cả skill đã xóa). Generate SkillId không cấp lại số ≤ giá trị này " +
                 "để hero / quái đang lưu số của skill đã xóa không âm thầm trỏ sang skill mới. Không sửa tay.")]
        public int highestIssuedValue;
    }

    /// <summary>Tra skill theo <see cref="SkillId"/> hoặc theo SO (để runner lấy ghi đè). ItemCatalog.Register đăng ký.</summary>
    public static class SkillRegistry
    {
        static readonly Dictionary<int, SkillEntry> byId = new();
        static readonly Dictionary<SkillBaseSO, SkillEntry> bySkill = new();

        public static void Register(SkillLibrarySO lib)
        {
            byId.Clear();
            bySkill.Clear();
            if (lib == null) return;
            foreach (var e in lib.skills)
            {
                if (e == null || e.value == 0) continue;
                if (byId.TryGetValue(e.value, out var prev))
                    Debug.LogWarning($"[Skills] '{e.key}' trùng số enum {e.value} với '{prev.key}' — chạy Generate SkillId.");
                byId[e.value] = e;
                if (e.skill == null) continue;
                if (bySkill.TryGetValue(e.skill, out var same))
                    Debug.LogWarning($"[Skills] '{e.key}' dùng chung SO '{e.skill.name}' với '{same.key}': ghi đè của '{e.key}' bị bỏ qua.");
                else bySkill[e.skill] = e;
            }
        }

        public static SkillEntry Entry(SkillId id) => byId.TryGetValue((int)id, out var e) ? e : null;
        public static SkillEntry Entry(SkillBaseSO skill) => skill != null && bySkill.TryGetValue(skill, out var e) ? e : null;

        /// <summary>SO hành vi của skill; null nếu chưa đăng ký hoặc sai loại (vd chọn đòn quái cho hero).</summary>
        public static T Get<T>(SkillId id) where T : SkillBaseSO => Entry(id)?.skill as T;

        public static List<T> GetAll<T>(IEnumerable<SkillId> ids) where T : SkillBaseSO
        {
            var list = new List<T>();
            if (ids == null) return list;
            foreach (var id in ids) if (Get<T>(id) is { } s) list.Add(s);
            return list;
        }

        public static float Cooldown(SkillBaseSO skill)
        {
            if (skill == null) return 0f;
            var e = Entry(skill);
            return e != null ? e.cooldown.Resolve(skill.cooldown) : skill.cooldown;
        }

        public static float ManaCost(ActiveSkillSO skill)
        {
            if (skill == null) return 0f;
            var e = Entry(skill);
            return e != null ? e.manaCost.Resolve(skill.manaCost) : skill.manaCost;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { byId.Clear(); bySkill.Clear(); }
    }
}
