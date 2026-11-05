using DG.Tweening;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.VFX
{
    /// <summary>
    /// VFX one-shot: tự thu hồi sau duration. Nếu được lấy từ PoolManager → trả về pool (kiểm spawnVersion);
    /// nếu Instantiate thường → Destroy như trước. Khi về pool: dừng + Clear ParticleSystem, Kill tween.
    /// </summary>
    public class AutoDestroyEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private float duration = 1f;

        ParticleSystem[] particles;
        Animator[] animators;
        bool pooled;

        public float Duration
        {
            get => duration;
            set => duration = value;
        }

        void Awake()
        {
            particles = GetComponentsInChildren<ParticleSystem>(true);
            animators = GetComponentsInChildren<Animator>(true);
        }

        void Start()
        {
            if (pooled || GetComponent<PooledObject>() != null) return;
            if (duration > 0f) Destroy(gameObject, duration);
        }

        public void OnSpawn()
        {
            pooled = true;
            foreach (var a in animators)
                if (a != null && a.runtimeAnimatorController != null) { a.Rebind(); a.Update(0f); }
            foreach (var p in particles) if (p != null) { p.Clear(true); p.Play(true); }
            if (duration > 0f) Services.Pool?.ReturnAfter(gameObject, duration);
        }

        public void OnDespawn()
        {
            DOTween.Kill(transform);
            foreach (var p in particles)
                if (p != null) { p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); p.Clear(true); }
        }
    }
}
