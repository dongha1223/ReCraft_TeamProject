using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 같은 공격 ID(AttackId)로 발생한 피격 이벤트를 짧은 시간 동안 모아
    /// 대표 타격음 1회만 재생하는 싱글턴.
    ///
    /// 흐름:
    ///   EnemyStats.TakeDamage() → RegisterHit()
    ///   LateUpdate → aggregationWindow 경과 그룹 플러시 → SfxManager.PlayOneShot()
    /// </summary>
    public class HitSoundAggregator : MonoBehaviour
    {
        public static HitSoundAggregator Instance { get; private set; }

        [Tooltip("같은 공격 ID의 피격 이벤트를 묶는 시간 창(초). 너무 크면 타격음이 늦게 들린다.")]
        [SerializeField] private float _aggregationWindow = 0.03f;

        [Tooltip("이 수 이상 적중 시 heavyClips를 우선 사용한다.")]
        [SerializeField] private int _heavyHitThreshold = 3;

        [Range(0f, 1f)]
        [Tooltip("타격음 볼륨 (0~1).")]
        [SerializeField] private float _hitVolume = 1f;

        [Range(0f, 0.2f)]
        [Tooltip("재생 시 pitch 랜덤 범위 (±값). 0이면 pitch 고정.")]
        [SerializeField] private float _pitchVariance = 0.05f;

        private class HitGroup
        {
            public int          HitCount;
            public Vector3      SumPosition;
            public AudioClip[]  Clips;
            public AudioClip[]  HeavyClips;
            public float        FirstHitTime;
        }

        private readonly Dictionary<int, HitGroup> _groups    = new();
        private readonly List<int>                  _toFlush   = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 피격 이벤트를 등록한다. attackId == 0이면 즉시 단독 재생한다.
        /// </summary>
        /// <param name="attackId">공격 식별 ID (AttackIdGenerator.Next() 값).</param>
        /// <param name="worldPos">피격 위치.</param>
        /// <param name="clips">기본 타격 클립 목록.</param>
        /// <param name="heavyClips">다중 적중 시 사용할 클립 목록 (null이면 clips로 대체).</param>
        public void RegisterHit(int attackId, Vector3 worldPos,
                                AudioClip[] clips, AudioClip[] heavyClips = null)
        {
            if (SfxManager.Instance == null) return;

            // AttackId 미설정 → 즉시 재생
            if (attackId == 0)
            {
                PlayClip(PickRandom(clips), worldPos);
                return;
            }

            if (!_groups.TryGetValue(attackId, out var group))
            {
                group = new HitGroup
                {
                    Clips        = clips,
                    HeavyClips   = heavyClips,
                    FirstHitTime = Time.unscaledTime,
                };
                _groups[attackId] = group;
            }

            group.HitCount++;
            group.SumPosition += worldPos;
        }

        private void LateUpdate()
        {
            if (_groups.Count == 0) return;

            float now = Time.unscaledTime;

            foreach (var pair in _groups)
            {
                if (now - pair.Value.FirstHitTime >= _aggregationWindow)
                    _toFlush.Add(pair.Key);
            }

            foreach (int id in _toFlush)
            {
                Flush(_groups[id]);
                _groups.Remove(id);
            }

            _toFlush.Clear();
        }

        private void Flush(HitGroup group)
        {
            Vector3     repPos  = group.SumPosition / group.HitCount;
            AudioClip[] pool    = group.HitCount >= _heavyHitThreshold && group.HeavyClips != null
                                  ? group.HeavyClips
                                  : group.Clips;
            PlayClip(PickRandom(pool), repPos);
        }

        private void PlayClip(AudioClip clip, Vector3 pos)
        {
            if (clip == null) return;
            float pitch = 1f + Random.Range(-_pitchVariance, _pitchVariance);
            SfxManager.Instance.PlayOneShot(clip, pos, _hitVolume, pitch);
        }

        private static AudioClip PickRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
