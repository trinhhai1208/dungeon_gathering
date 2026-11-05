using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Điểm gọi SFX duy nhất cho gameplay/UI. "Bắn rồi quên":
    /// - Không bao giờ throw, không await, không trả về gì → không thể chặn hay làm hỏng luồng game.
    /// - Thiếu AudioManager / thư viện / clip, hoặc âm lượng = 0 → bỏ qua im lặng.
    /// - Chống spam (cooldown, giới hạn voice) do AudioManager xử lý.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Âm 2D (UI, jingle, âm của người chơi).</summary>
        public static void Play(SfxId id, float volumeScale = 1f)
        {
            if (id == SfxId.None) return;
            var audio = Services.Audio;
            if (audio == null) return;
            audio.TryPlay(id, volumeScale, false, default);
        }

        /// <summary>Âm gắn vị trí thế giới: lệch trái/phải theo màn hình, nhỏ dần rồi tắt khi ra ngoài khung hình.</summary>
        public static void PlayAt(SfxId id, Vector3 worldPosition, float volumeScale = 1f)
        {
            if (id == SfxId.None) return;
            var audio = Services.Audio;
            if (audio == null) return;
            audio.TryPlay(id, volumeScale, true, worldPosition);
        }

        /// <summary>Ưu tiên clip tùy chỉnh (Inspector) nếu có, ngược lại dùng id mặc định.</summary>
        public static void PlayOverride(AudioClip custom, SfxId fallback, float volumeScale = 1f)
        {
            var audio = Services.Audio;
            if (audio == null) return;
            if (custom != null) audio.PlaySfx(custom, volumeScale);
            else if (fallback != SfxId.None) audio.TryPlay(fallback, volumeScale, false, default);
        }

        /// <inheritdoc cref="PlayOverride(AudioClip, SfxId, float)"/>
        public static void PlayOverrideAt(AudioClip custom, SfxId fallback, Vector3 worldPosition, float volumeScale = 1f)
        {
            var audio = Services.Audio;
            if (audio == null) return;
            if (custom != null) audio.PlaySfx(custom, volumeScale);
            else if (fallback != SfxId.None) audio.TryPlay(fallback, volumeScale, true, worldPosition);
        }
    }
}
