using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Bảng SfxId → clip + luật phát. Dựng tự động bằng menu
    /// <c>Dungeon Gathering/Audio/Rebuild SFX Library</c> (quét Audio/SFX), chỉnh tay được trong Inspector.
    /// Lưu tại <c>Assets/Project/Core/Data/SfxLibrary.asset</c> (group Core_Local, key <c>AddressKeys.CoreSfxLibrary</c>).
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Dungeon Gathering/Audio/SFX Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("SfxId mà entry này phục vụ; nếu trùng id thì chỉ entry đầu tiên được dùng.")]
            public SfxId id;
            [Tooltip("Nhiều clip = biến thể, chọn ngẫu nhiên (không lặp liền).")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.3f)] public float pitchJitter = 0.04f;
            [Tooltip("Khoảng tối thiểu (giây, thời gian thực) giữa 2 lần phát — chống spam khi nhiều sự kiện dồn 1 frame.")]
            [Min(0f)] public float cooldown = 0.05f;
            [Tooltip("Số voice tối đa cùng lúc cho id này; vượt → cướp voice cũ nhất của chính nó.")]
            [Min(1)] public int maxVoices = 2;
            [Tooltip("Cao = quan trọng. Khi hết voice, âm ưu tiên thấp hơn sẽ bị cướp.")]
            [Range(0, 100)] public int priority = 50;
            [Tooltip("PlayAt: lệch trái/phải theo vị trí trên màn hình.")]
            public bool positional = true;
        }

        [Tooltip("Bảng SfxId → clip + luật phát; id không có entry thì không phát âm.")]
        public List<Entry> entries = new();

        public Entry Find(SfxId id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].id == id) return entries[i];
            return null;
        }
    }
}
