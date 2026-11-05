using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonGathering.Core;
using UnityEditor;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Dựng/cập nhật SfxLibrary từ các file trong Audio/SFX theo quy ước tên
    /// <c>sfx_&lt;nhóm&gt;_&lt;snake_case&gt;[_NN].wav</c> → <see cref="SfxId"/> (PascalCase).
    /// Giữ nguyên thông số đã chỉnh tay (volume, cooldown...) của entry có sẵn, chỉ thay danh sách clip.
    /// Tự chạy lại khi có wav mới/xóa trong thư mục SFX.
    /// </summary>
    public static class SfxLibraryBuilder
    {
        public const string SfxFolder = "Assets/Project/Audio/SFX";
        public const string LibraryPath = "Assets/Project/Audio/Resources/Audio/SfxLibrary.asset";

        [MenuItem("Dungeon Gathering/Audio/Rebuild SFX Library")]
        public static void RebuildMenu() => Rebuild(true);

        public static SfxLibrary Rebuild(bool verbose)
        {
            var groups = new Dictionary<SfxId, List<AudioClip>>();
            var unknown = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;
                if (!TryParseId(Path.GetFileNameWithoutExtension(path), out var id)) { unknown.Add(path); continue; }
                if (!groups.TryGetValue(id, out var list)) groups[id] = list = new List<AudioClip>();
                list.Add(clip);
            }

            var lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath)!);
                lib = ScriptableObject.CreateInstance<SfxLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            Undo.RecordObject(lib, "Rebuild SFX Library");
            int added = 0;
            foreach (var kv in groups.OrderBy(k => (int)k.Key))
            {
                var e = lib.Find(kv.Key);
                if (e == null)
                {
                    e = CreateDefault(kv.Key);
                    lib.entries.Add(e);
                    added++;
                }
                e.clips = kv.Value.OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
            }
            // Entry mất hết clip → để trống clip (giữ thông số), AudioManager sẽ bỏ qua.
            foreach (var e in lib.entries)
                if (e != null && !groups.ContainsKey(e.id)) e.clips = Array.Empty<AudioClip>();
            lib.entries.Sort((a, b) => ((int)a.id).CompareTo((int)b.id));

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssetIfDirty(lib);

            var missing = Enum.GetValues(typeof(SfxId)).Cast<SfxId>()
                .Where(id => id != SfxId.None && !groups.ContainsKey(id)).ToList();
            if (verbose || unknown.Count > 0)
            {
                var sb = new StringBuilder($"[Audio] SfxLibrary: {groups.Count} id, {groups.Sum(g => g.Value.Count)} clip, +{added} entry mới.");
                if (missing.Count > 0) sb.Append($"\n  Chưa có clip: {string.Join(", ", missing)}");
                if (unknown.Count > 0) sb.Append($"\n  Tên file không khớp SfxId (bỏ qua): {string.Join(", ", unknown)}");
                Debug.Log(sb.ToString(), lib);
            }
            return lib;
        }

        /// <summary>"sfx_ui_ui_click_02" → UiClick; "sfx_combat_sword_swing_01" → SwordSwing.</summary>
        public static bool TryParseId(string fileName, out SfxId id)
        {
            id = SfxId.None;
            var parts = fileName.ToLowerInvariant().Split('_').ToList();
            if (parts.Count < 3 || parts[0] != "sfx") return false;
            parts.RemoveRange(0, 2); // "sfx" + nhóm
            if (parts.Count > 1 && parts[^1].All(char.IsDigit)) parts.RemoveAt(parts.Count - 1); // biến thể _NN
            var name = string.Concat(parts.Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
            return Enum.TryParse(name, false, out id) && id != SfxId.None;
        }

        /// <summary>Luật phát mặc định theo loại âm — chỉnh tiếp trong Inspector của SfxLibrary.</summary>
        static SfxLibrary.Entry CreateDefault(SfxId id)
        {
            var e = new SfxLibrary.Entry { id = id };
            void Set(float vol, float jitter, float cd, int voices, int prio, bool positional = true)
            {
                e.volume = vol; e.pitchJitter = jitter; e.cooldown = cd; e.maxVoices = voices; e.priority = prio; e.positional = positional;
            }
            switch (id)
            {
                case SfxId.Footstep: Set(0.5f, 0.08f, 0.08f, 2, 10); break;
                case SfxId.EnemyHurt:
                case SfxId.SwordHit:
                case SfxId.ProjectileHit: Set(0.8f, 0.06f, 0.04f, 4, 40); break;
                case SfxId.CritHit: Set(0.9f, 0.03f, 0.05f, 2, 60); break;
                case SfxId.SwordSwing:
                case SfxId.BowShot:
                case SfxId.GunShot:
                case SfxId.MagicShot: Set(0.8f, 0.05f, 0.03f, 3, 50, false); break;
                case SfxId.EnemyShot: Set(0.6f, 0.06f, 0.06f, 3, 30); break;
                case SfxId.EnemyDeath: Set(0.85f, 0.06f, 0.05f, 3, 55); break;
                case SfxId.CoinPickup: Set(0.7f, 0.04f, 0.035f, 4, 45, false); break;
                case SfxId.Explosion: Set(1f, 0.05f, 0.08f, 2, 80); break;
                case SfxId.PlayerHurt: Set(0.9f, 0.04f, 0.15f, 1, 90, false); break;
                case SfxId.SpawnWarning: Set(0.6f, 0f, 0.15f, 2, 55); break;
                case SfxId.DoorOpen:
                case SfxId.DoorClose: Set(0.8f, 0.03f, 0.2f, 1, 60, false); break;
                case SfxId.PlayerDeath:
                case SfxId.BossAppear:
                case SfxId.BossRoar:
                case SfxId.RoomCleared:
                case SfxId.Victory:
                case SfxId.Defeat:
                case SfxId.LevelUp:
                case SfxId.PortalEnter: Set(1f, 0f, 0.5f, 1, 100, false); break;
                case SfxId.Dash:
                case SfxId.Shield:
                case SfxId.Heal:
                case SfxId.Buff:
                case SfxId.SkillBurst:
                case SfxId.WeaponSwap:
                case SfxId.ManaPickup:
                case SfxId.PotionDrink:
                case SfxId.ItemPickup:
                case SfxId.RareItemPickup:
                case SfxId.ChestOpen: Set(0.85f, 0.02f, 0.1f, 1, 75, false); break;
                default:
                    if (id.ToString().StartsWith("Ui", StringComparison.Ordinal)) Set(0.8f, 0f, 0.03f, 2, 95, false);
                    else Set(0.8f, 0.04f, 0.05f, 2, 50);
                    break;
            }
            return e;
        }
    }

    /// <summary>
    /// Import settings cho nhạc nền (MODULES §Audio_BGM): WebGL không hỗ trợ Streaming →
    /// Compressed In Memory + Vorbis ~60%, tải nền để không giật khi Addressables nạp bài.
    /// </summary>
    sealed class BgmImportSettings : AssetPostprocessor
    {
        public const string BgmFolder = "Assets/Project/Audio/BGM";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(BgmFolder + "/", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = false;
            importer.loadInBackground = true;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.preloadAudioData = true;
            importer.defaultSampleSettings = s;
        }
    }

    /// <summary>Import settings cho SFX ngắn + tự dựng lại SfxLibrary khi thư mục SFX thay đổi.</summary>
    sealed class SfxImportSettings : AssetPostprocessor
    {
        static bool IsSfx(string path) =>
            path.StartsWith(SfxLibraryBuilder.SfxFolder + "/", StringComparison.OrdinalIgnoreCase) &&
            (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase));

        void OnPreprocessAudio()
        {
            if (!IsSfx(assetPath)) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad; // clip ngắn: giải nén sẵn → phát không tốn CPU, không giật
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.preloadAudioData = true;
            importer.defaultSampleSettings = s;
        }

        static bool pending;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (pending) return;
            if (!imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsSfx)) return;
            pending = true;
            EditorApplication.delayCall += () =>
            {
                pending = false;
                SfxLibraryBuilder.Rebuild(false);
            };
        }
    }
}
