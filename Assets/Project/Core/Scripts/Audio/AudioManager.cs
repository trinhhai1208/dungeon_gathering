using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DungeonGathering.Core
{
    /// <summary>
    /// BGM (crossfade 2 nguồn, mỗi bản nhạc một scope riêng để Release khi đổi) + SFX (pool voice có quản lý).
    /// Âm lượng 0..10 nấc từ SettingsData. Trên WebGL audio chỉ phát sau cú chạm đầu (trình duyệt tự xử lý).
    ///
    /// SFX được thiết kế để KHÔNG ảnh hưởng luồng game:
    /// - Gọi qua <see cref="Sfx"/> (static, không throw, không await). Mọi lỗi bị nuốt và chỉ log 1 lần.
    /// - Pool voice cố định tạo sẵn ở Awake → không cấp phát / Instantiate khi đang chơi.
    /// - Chống spam: cooldown theo id (thời gian thực, không phụ thuộc timeScale), giới hạn voice theo id,
    ///   cướp voice theo độ ưu tiên khi hết chỗ (âm quan trọng như UI/jingle không bị âm vặt đè).
    /// - Clip import "Decompress On Load" + preload (xem SfxImportSettings) → không giật khi phát lần đầu.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioMixerGroup bgmGroup;
        [SerializeField] AudioMixerGroup sfxGroup;
        [SerializeField, Min(1)] int sfxVoices = 16;
        [Tooltip("Nạp từ Addressables Core_Local (core/sfx-library) qua Systems.Start hoặc gán trực tiếp trong Inspector")]
        [SerializeField] SfxLibrary sfxLibrary;
        [Tooltip("Độ lệch trái/phải tối đa của PlayAt (0 = tắt).")]
        [SerializeField, Range(0f, 1f)] float maxPan = 0.45f;

        AudioSource bgmA, bgmB;
        // Mỗi nguồn BGM giữ scope của clip nó đang phát; scope chỉ Dispose khi chính nguồn đó dừng
        // → không bao giờ Release clip đang phát (kể cả khi Play/Stop chồng nhau).
        ModuleScope scopeA, scopeB;
        AudioSource bgmActive;   // nguồn đang/ sắp phát chính (null = đang tắt nhạc)
        string bgmAddress;
        float bgmVolume = 0.7f, sfxVolume = 0.8f;
        int bgmRequest;
        int fadeId;              // fade mới thay thế fade cũ (fade cũ thoát giữa chừng)

        // --- SFX voice pool ---
        struct Voice
        {
            public AudioSource Source;
            public SfxId Id;        // None = clip trực tiếp (PlaySfx(AudioClip))
            public int Priority;
            public float StartTime;
        }

        const int MaxId = 256;
        Voice[] voices;
        int nextVoice;
        readonly SfxLibrary.Entry[] entries = new SfxLibrary.Entry[MaxId];
        readonly float[] lastPlayed = new float[MaxId];
        readonly int[] lastClip = new int[MaxId];
        readonly GameSfxHooks hooks = new();
        GameBgmHooks bgmHooks;
        Camera cachedCamera;
        bool warnedError;

        public SfxLibrary Library => sfxLibrary;
        /// <summary>Address của BGM đang phát / đang tải (null nếu không có).</summary>
        public string CurrentBgm => bgmAddress;

        void Awake()
        {
            bgmA = CreateSource("BGM_A", bgmGroup, true);
            bgmB = CreateSource("BGM_B", bgmGroup, true);
            voices = new Voice[sfxVoices];
            for (int i = 0; i < sfxVoices; i++) voices[i].Source = CreateSource("SFX_" + i, sfxGroup, false);
            for (int i = 0; i < MaxId; i++) { lastPlayed[i] = -999f; lastClip[i] = -1; }
            if (sfxLibrary != null) SetLibrary(sfxLibrary);
        }

        void OnEnable()
        {
            hooks.Bind();
            (bgmHooks ??= new GameBgmHooks(this)).Bind();
        }

        void OnDisable()
        {
            hooks.Unbind();
            bgmHooks?.Unbind();
        }

        AudioSource CreateSource(string n, AudioMixerGroup group, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.outputAudioMixerGroup = group;
            s.spatialBlend = 0f;
            return s;
        }

        /// <summary>Đổi thư viện SFX lúc chạy (VD: test, mod). null → tắt SFX theo id.</summary>
        public void SetLibrary(SfxLibrary library)
        {
            sfxLibrary = library;
            Array.Clear(entries, 0, entries.Length);
            if (library == null)
            {
                Debug.LogWarning("[Audio] Không có SfxLibrary — SFX theo id sẽ im lặng.");
                return;
            }
            foreach (var e in library.entries)
            {
                int k = e != null ? (int)e.id : 0;
                if (k <= 0 || k >= MaxId || e.clips == null || e.clips.Length == 0) continue;
                entries[k] = e;
            }
        }

        public void ApplySettings(SettingsData settings)
        {
            if (settings == null) return;
            bgmVolume = Mathf.Clamp01(settings.bgmVolume / 10f);
            sfxVolume = Mathf.Clamp01(settings.sfxVolume / 10f);
            if (bgmActive != null && bgmActive.isPlaying) bgmActive.volume = bgmVolume;
            if (sfxVolume <= 0f) StopAllSfx();
        }

        // =====================================================================
        // BGM
        // =====================================================================

        /// <summary>Phát BGM theo address (tải async, yêu cầu sau cùng thắng).</summary>
        public async void PlayBgm(string address, float fade = 0.6f)
        {
            if (string.IsNullOrEmpty(address) || address == bgmAddress) return;
            int request = ++bgmRequest;
            bgmAddress = address;
            if (!AddressablesUtil.KeyExists(address))
            {
                Debug.LogWarning($"[Audio] BGM '{address}' chưa có address.");
                bgmAddress = null; // cho phép gọi lại (vd sau khi tải thêm catalog)
                return;
            }
            var scope = Services.Assets.CreateScope("bgm:" + address);
            AudioClip clip;
            try { clip = await scope.Load<AudioClip>(address); }
            catch (Exception e)
            {
                scope.Dispose();
                // Lỗi tải: quên address để lần PlayBgm sau (cùng address) được thử lại.
                if (request == bgmRequest) bgmAddress = null;
                if (e is not OperationCanceledException) Debug.LogWarning($"[Audio] {e.Message}");
                return;
            }
            if (this == null || request != bgmRequest || clip == null)
            {
                scope.Dispose();
                if (this != null && request == bgmRequest) bgmAddress = null;
                return;
            }

            // Nguồn đích = nguồn không phải bgmActive; nếu nó còn đang fade-out bản cũ thì dừng hẳn (Dispose scope của nó).
            var to = bgmActive != null ? (bgmActive == bgmA ? bgmB : bgmA)
                   : !bgmA.isPlaying ? bgmA : !bgmB.isPlaying ? bgmB : bgmA.volume <= bgmB.volume ? bgmA : bgmB;
            StopSource(to);
            to.clip = clip;
            to.volume = 0f;
            SetScope(to, scope);
            to.Play();
            bgmActive = to;
            await FadeTo(to, fade);
        }

        public async void StopBgm(float fade = 0.4f)
        {
            bgmRequest++;
            bgmAddress = null;
            bgmActive = null;
            if (!bgmA.isPlaying && !bgmB.isPlaying)
            {
                StopSource(bgmA);
                StopSource(bgmB);
                return;
            }
            await FadeTo(null, fade);
        }

        /// <summary>
        /// Fade <paramref name="keep"/> lên bgmVolume, mọi nguồn khác về 0 rồi dừng + Dispose scope của chúng.
        /// Fade mới hơn bắt đầu → fade này thoát ngay (fade mới tiếp quản cả hai nguồn).
        /// </summary>
        async Awaitable FadeTo(AudioSource keep, float duration)
        {
            int id = ++fadeId;
            float startA = bgmA.volume, startB = bgmB.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                bgmA.volume = Mathf.Lerp(startA, keep == bgmA ? bgmVolume : 0f, k);
                bgmB.volume = Mathf.Lerp(startB, keep == bgmB ? bgmVolume : 0f, k);
                try { await Awaitable.NextFrameAsync(destroyCancellationToken); }
                catch (OperationCanceledException) { return; }
                if (id != fadeId) return;
            }
            if (id != fadeId) return;
            if (keep != bgmA) StopSource(bgmA);
            if (keep != bgmB) StopSource(bgmB);
            if (keep != null) keep.volume = bgmVolume;
        }

        void StopSource(AudioSource s)
        {
            if (s == null) return;
            s.Stop();
            s.clip = null;
            SetScope(s, null);
        }

        void SetScope(AudioSource s, ModuleScope scope)
        {
            if (s == bgmA) { if (scopeA != scope) scopeA?.Dispose(); scopeA = scope; }
            else if (s == bgmB) { if (scopeB != scope) scopeB?.Dispose(); scopeB = scope; }
        }

        // =====================================================================
        // SFX
        // =====================================================================

        /// <summary>Phát clip trực tiếp (clip gán trong Inspector). Ưu tiên trung bình, không cooldown.</summary>
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (clip == null || voices == null || sfxVolume <= 0f) return;
            try
            {
                int v = AcquireVoice(SfxId.None, 50, int.MaxValue);
                if (v < 0) return;
                StartVoice(v, SfxId.None, 50, clip, volume, pitchJitter, 0f);
            }
            catch (Exception e) { ReportOnce(e); }
        }

        /// <summary>Phát theo id. Dùng <see cref="Sfx"/> thay vì gọi thẳng. Trả false nếu bị bỏ qua.</summary>
        public bool TryPlay(SfxId id, float volumeScale, bool positional, Vector3 worldPosition)
        {
            try
            {
                int k = (int)id;
                if (voices == null || sfxVolume <= 0f || volumeScale <= 0f || k <= 0 || k >= MaxId) return false;
                var e = entries[k];
                if (e == null) return false;

                float now = Time.realtimeSinceStartup;
                if (now - lastPlayed[k] < e.cooldown) return false;

                float pan = 0f;
                if (positional && e.positional && !ScreenPlacement(worldPosition, ref volumeScale, out pan)) return false;

                int v = AcquireVoice(id, e.priority, Mathf.Max(1, e.maxVoices));
                if (v < 0) return false;

                var clip = PickClip(e, k);
                if (clip == null) return false;
                lastPlayed[k] = now;
                StartVoice(v, id, e.priority, clip, e.volume * volumeScale, e.pitchJitter, pan);
                return true;
            }
            catch (Exception ex)
            {
                ReportOnce(ex);
                return false;
            }
        }

        public void StopAllSfx()
        {
            if (voices == null) return;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i].Source != null) voices[i].Source.Stop();
        }

        AudioClip PickClip(SfxLibrary.Entry e, int k)
        {
            int n = e.clips.Length;
            if (n == 1) return e.clips[0];
            int i = UnityEngine.Random.Range(0, n);
            if (i == lastClip[k]) i = (i + 1) % n; // không lặp liền cùng một biến thể
            lastClip[k] = i;
            return e.clips[i];
        }

        /// <summary>
        /// Chọn voice: (1) đủ maxVoices của id → cướp cái cũ nhất của chính nó; (2) voice rảnh;
        /// (3) cướp voice ưu tiên thấp nhất (≤ mình), cũ nhất; (4) không có → bỏ qua (-1).
        /// </summary>
        int AcquireVoice(SfxId id, int priority, int maxVoices)
        {
            int sameCount = 0, sameOldest = -1, free = -1, victim = -1;
            float sameOldestT = float.MaxValue;
            for (int j = 0; j < voices.Length; j++)
            {
                int i = (nextVoice + j) % voices.Length;
                ref var v = ref voices[i];
                if (v.Source == null) continue;
                if (!v.Source.isPlaying)
                {
                    if (free < 0) free = i;
                    continue;
                }
                if (id != SfxId.None && v.Id == id)
                {
                    sameCount++;
                    if (v.StartTime < sameOldestT) { sameOldestT = v.StartTime; sameOldest = i; }
                }
                if (v.Priority <= priority &&
                    (victim < 0 || v.Priority < voices[victim].Priority ||
                     (v.Priority == voices[victim].Priority && v.StartTime < voices[victim].StartTime)))
                    victim = i;
            }
            if (sameCount >= maxVoices) return sameOldest;
            if (free >= 0) return free;
            return victim;
        }

        void StartVoice(int index, SfxId id, int priority, AudioClip clip, float volume, float pitchJitter, float pan)
        {
            ref var v = ref voices[index];
            var s = v.Source;
            s.Stop();
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume * sfxVolume);
            s.pitch = 1f + (pitchJitter > 0f ? UnityEngine.Random.Range(-pitchJitter, pitchJitter) : 0f);
            s.panStereo = pan;
            s.Play();
            v.Id = id;
            v.Priority = priority;
            v.StartTime = Time.realtimeSinceStartup;
            nextVoice = (index + 1) % voices.Length;
        }

        /// <summary>Tính pan + suy giảm theo vị trí màn hình. false = quá xa ngoài khung hình → không phát.</summary>
        bool ScreenPlacement(Vector3 world, ref float volume, out float pan)
        {
            pan = 0f;
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled) cachedCamera = Camera.main;
            if (cachedCamera == null) return true;
            Vector3 vp = cachedCamera.WorldToViewportPoint(world);
            float off = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f)) - 0.5f; // >0: ngoài màn hình
            if (off > 0f)
            {
                float att = 1f - off * 2.5f; // tắt hẳn khi lệch 0.4 viewport ra ngoài
                if (att <= 0.02f) return false;
                volume *= att;
            }
            pan = Mathf.Clamp((vp.x - 0.5f) * 2f, -1f, 1f) * maxPan;
            return true;
        }

        void ReportOnce(Exception e)
        {
            if (warnedError) return;
            warnedError = true;
            Debug.LogWarning($"[Audio] SFX lỗi (đã bỏ qua, game vẫn chạy): {e}");
        }

        void OnDestroy()
        {
            hooks.Unbind();
            bgmHooks?.Unbind();
            scopeA?.Dispose(); scopeA = null;
            scopeB?.Dispose(); scopeB = null;
        }
    }
}
