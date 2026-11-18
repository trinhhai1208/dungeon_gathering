using System.Collections.Generic;
using DungeonGathering.Catalog;
using UnityEditor;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Inspector cho Sword / Ranged Library: chọn một entry để xem trước sprite (đã xoay theo góc vẽ gốc),
    /// hitbox / tầm đánh vẽ theo tỉ lệ thật, và thông số đã resolve (SO gốc ← đạn ← entry).
    /// </summary>
    public abstract class WeaponLibraryEditorBase : Editor
    {
        int selected;
        const float PreviewSize = 180f;
        const float UnitsShown = 4f; // khung xem trước rộng 4 đơn vị thế giới

        protected abstract WeaponBaseSO Base { get; }
        protected abstract IReadOnlyList<WeaponEntryBase> Entries { get; }
        protected abstract ProjectileLibrarySO FindProjectiles();

        public override void OnInspectorGUI()
        {
            DrawPreview();
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        void DrawPreview()
        {
            var entries = Entries;
            if (Base == null || entries == null || entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Gán SO gốc và thêm entry để xem trước.", MessageType.Info);
                return;
            }
            var names = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++) names[i] = string.IsNullOrEmpty(entries[i].id) ? $"#{i}" : entries[i].id;
            selected = Mathf.Clamp(EditorGUILayout.Popup("Xem trước", selected, names), 0, entries.Count - 1);
            var e = entries[selected];

            // Resolve như runtime. Ngoài Play Mode: đăng ký thư viện đạn để tra id đạn.
            // Trong Play Mode KHÔNG đăng ký lại (sẽ xóa registry runtime) — dùng cái game đã đăng ký.
            if (!Application.isPlaying) WeaponLibrary.Register(null, null, FindProjectiles());
            var spec = WeaponLibrary.Build(null, Base, e);

            var rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
            rect.x = (EditorGUIUtility.currentViewWidth - PreviewSize) * 0.5f;
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.15f, 0.14f));
            float ppu = PreviewSize / UnitsShown;
            Vector2 hand = new(rect.x + PreviewSize * 0.3f, rect.center.y); // tay cầm, ngắm sang phải

            // Hitbox / tầm đánh
            Handles.BeginGUI();
            Handles.color = new Color(1f, 0.45f, 0.3f, 0.9f);
            if (spec.weaponType == WeaponType.Melee)
            {
                var hb = spec.hitbox;
                if (hb.shape == HitboxShape.Box || (hb.shape == HitboxShape.Auto && spec.meleeStyle == MeleeAttackStyle.Thrust))
                {
                    var size = hb.shape == HitboxShape.Box ? hb.size : new Vector2(Mathf.Max(0.8f, spec.attackRange), 0.9f);
                    var o = hand + new Vector2(hb.offset.x, -hb.offset.y) * ppu;
                    var r = new Rect(o.x, o.y - size.y * 0.5f * ppu, size.x * ppu, size.y * ppu);
                    Handles.DrawSolidRectangleWithOutline(r, new Color(1f, 0.45f, 0.3f, 0.12f), Handles.color);
                }
                else
                {
                    float radius = (hb.shape == HitboxShape.Arc ? hb.size.x : spec.attackRange) * ppu;
                    float arc = hb.shape == HitboxShape.Arc ? hb.arc : (spec.meleeStyle == MeleeAttackStyle.Spin360 ? 360f : spec.swingArc);
                    var o = hand + new Vector2(hb.offset.x, -hb.offset.y) * ppu;
                    Vector3 from = Quaternion.Euler(0, 0, -arc * 0.5f) * Vector3.right;
                    Handles.DrawSolidArc(o, Vector3.forward, from, arc, radius);
                }
            }
            else
            {
                Vector2 muzzle = hand + new Vector2(spec.muzzleOffset.x, -spec.muzzleOffset.y) * ppu;
                Handles.DrawLine(muzzle, new Vector2(rect.xMax, muzzle.y));
                Handles.DrawSolidDisc(muzzle, Vector3.forward, 3f);
            }
            Handles.EndGUI();

            // Sprite
            var sp = spec.weaponSprite;
            if (sp != null)
            {
                var tex = sp.texture;
                var tr = sp.textureRect;
                var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                float scale = Mathf.Max(0.05f, spec.visualScale);
                var size = new Vector2(tr.width, tr.height) / sp.pixelsPerUnit * scale * ppu;
                var pivot = sp.pivot / new Vector2(tr.width, tr.height);
                var drawPos = hand + new Vector2(spec.holdOffset.x, -spec.holdOffset.y) * ppu * scale;
                var r = new Rect(drawPos.x - size.x * pivot.x, drawPos.y - size.y * (1f - pivot.y), size.x, size.y);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(spec.GetForwardAngle(135f, 0f), drawPos);
                GUI.DrawTextureWithTexCoords(r, tex, uv);
                GUI.matrix = m;
            }
            EditorGUI.DrawRect(new Rect(hand.x - 2, hand.y - 2, 4, 4), Color.white);

            EditorGUILayout.HelpBox(
                $"{e.id} · {(spec.weaponType == WeaponType.Melee ? spec.meleeStyle.ToString() : spec.fireMode.ToString())}\n" +
                $"Sát thương {spec.damage:0.#} · {spec.fireRate:0.##} đòn/s · DPS {spec.Dps:0.#} · tầm {spec.attackRange:0.##} · đẩy {spec.knockback:0.#}\n" +
                (spec.weaponType == WeaponType.Ranged
                    ? $"Đạn {(spec.projectilePrefab != null ? spec.projectilePrefab.name : "tia tức thời")} · {spec.projectileCount} viên × {spec.burstCount} loạt · lệch {spec.spread:0}° · tốc {spec.projectileSpeed:0.#}\n"
                    : $"Cung {spec.swingArc:0}° · {spec.swingDuration:0.##}s · hitbox {spec.hitbox.shape}{(spec.projectilePrefab != null ? " · kiếm khí " + spec.projectilePrefab.name : "")}\n") +
                $"Hiệu ứng khi trúng: {spec.onHitStatus.Count} · Mana {spec.manaCost}", MessageType.None);
        }
    }

    [CustomEditor(typeof(SwordLibrarySO))]
    public sealed class SwordLibraryEditor : WeaponLibraryEditorBase
    {
        SwordLibrarySO Lib => (SwordLibrarySO)target;
        protected override WeaponBaseSO Base => Lib.baseConfig;
        protected override IReadOnlyList<WeaponEntryBase> Entries => Lib.swords;
        protected override ProjectileLibrarySO FindProjectiles() => ProjectileLibraryLookup.Find();
    }

    [CustomEditor(typeof(RangedLibrarySO))]
    public sealed class RangedLibraryEditor : WeaponLibraryEditorBase
    {
        RangedLibrarySO Lib => (RangedLibrarySO)target;
        protected override WeaponBaseSO Base => Lib.baseConfig;
        protected override IReadOnlyList<WeaponEntryBase> Entries => Lib.weapons;
        protected override ProjectileLibrarySO FindProjectiles() => ProjectileLibraryLookup.Find();
    }

    static class ProjectileLibraryLookup
    {
        public static ProjectileLibrarySO Find()
        {
            var g = AssetDatabase.FindAssets("t:ProjectileLibrarySO");
            return g.Length > 0 ? AssetDatabase.LoadAssetAtPath<ProjectileLibrarySO>(AssetDatabase.GUIDToAssetPath(g[0])) : null;
        }
    }
}
