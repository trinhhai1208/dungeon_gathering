using System.IO;
using DungeonGathering.Core;
using UnityEditor;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>Menu bật / tắt chế độ test "mở full" (<see cref="EditorUnlockAll"/>) và xóa save riêng của chế độ này.</summary>
    public static class EditorUnlockAllMenu
    {
        const string ToggleMenu = "Tools/Dungeon Gathering/Debug/Mở full hero + vũ khí (Editor)";
        const string ResetMenu = "Tools/Dungeon Gathering/Debug/Xóa save mở full";

        [MenuItem(ToggleMenu, priority = 0)]
        static void Toggle()
        {
            bool on = !EditorUnlockAll.Active;
            EditorPrefs.SetBool(EditorUnlockAll.PrefKey, on);
            Debug.Log(on
                ? $"[Editor] BẬT mở full — lần Play sau: mọi hero / vũ khí / trang bị mở, {EditorUnlockAll.Coins:N0} coin, save riêng '{EditorUnlockAll.SaveFileName}'. Save thật không bị đụng."
                : "[Editor] TẮT mở full — lần Play sau dùng lại save thật (save.json).");
            if (EditorApplication.isPlaying) Debug.LogWarning("[Editor] Đang Play: thay đổi có hiệu lực từ lần Play sau.");
        }

        [MenuItem(ToggleMenu, true)]
        static bool ToggleValidate()
        {
            UnityEditor.Menu.SetChecked(ToggleMenu, EditorUnlockAll.Active);
            return true;
        }

        [MenuItem(ResetMenu, priority = 1)]
        static void ResetSave()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Editor] Dừng Play trước khi xóa save mở full."); return; }
            string main = Path.Combine(Application.persistentDataPath, EditorUnlockAll.SaveFileName);
            int n = 0;
            foreach (var p in new[] { main, main + ".tmp", main + ".bak" })
                if (File.Exists(p)) { File.Delete(p); n++; }
            Debug.Log(n > 0 ? $"[Editor] Đã xóa save mở full ({n} file). Save thật không bị đụng."
                            : "[Editor] Chưa có save mở full để xóa.");
        }
    }
}
