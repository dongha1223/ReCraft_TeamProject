using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 최종 페이즈 화염구 낙하 패턴.
    ///
    /// 플레이어 X 위치 기준으로 화면 위에서 화염구를 5개 1초 간격으로 순차 스폰.
    /// 각 화염구(FireballProjectile)는 독립적으로 낙하·충돌·잔해 처리를 담당한다.
    ///
    /// BossHeadFinalPhase가 FireballLoop 코루틴에서 반복 호출한다.
    /// </summary>
    public class BossFireballPattern : BossCustomPatternBase
    {
        public override string PatternId => "FireballRain";

        [Header("데이터 SO")]
        [SerializeField] private BossFireballDataSO _data;

        [Header("플레이어")]
        [SerializeField] private Transform _playerTransform;

        // ── 캐시 ──────────────────────────────────────────────────────────
        private float      _spawnY;
        private float      _fallSpeed;
        private int        _ballCount;
        private float      _spawnInterval;
        private float      _spawnXRange;
        private float      _destroyBelowY;
        private float      _hitDamage;
        private float      _debrisDamage;
        private float      _debrisDuration;
        private float      _debrisTickInterval;
        private float      _debrisWidth;
        private float      _debrisHeight;
        private float      _patternCooldown;
        private Sprite[]   _fallSprites;
        private Sprite[]   _impactSprites;
        private GameObject _prefab;

        private readonly List<GameObject> _activeBalls = new();

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data == null) return;
            _spawnY             = _data.SpawnY;
            _fallSpeed          = _data.FallSpeed;
            _ballCount          = _data.BallCount;
            _spawnInterval      = _data.SpawnInterval;
            _spawnXRange        = _data.SpawnXRange;
            _destroyBelowY      = _data.DestroyBelowY;
            _hitDamage          = _data.HitDamage;
            _debrisDamage       = _data.DebrisDamage;
            _debrisDuration     = _data.DebrisDuration;
            _debrisTickInterval = _data.DebrisTickInterval;
            _debrisWidth        = _data.DebrisWidth;
            _debrisHeight       = _data.DebrisHeight;
            _patternCooldown    = _data.PatternCooldown;
            _fallSprites        = _data.FallSprites;
            _impactSprites      = _data.ImpactSprites;
            if (_data.FireballPrefab != null) _prefab = _data.FireballPrefab;
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) _playerTransform = go.transform;
            }
        }

        // ── BossCustomPatternBase ──────────────────────────────────────────

        protected override IEnumerator ExecuteRoutine()
        {
            if (_prefab == null || _playerTransform == null)
            {
                Debug.LogWarning("[FireballRain] 프리팹 또는 플레이어 트랜스폼 미연결");
                yield break;
            }

            _activeBalls.RemoveAll(b => b == null);

            for (int i = 0; i < _ballCount; i++)
            {
                SpawnFireball();

                if (i < _ballCount - 1)
                    yield return StartCoroutine(PauseableWait(_spawnInterval));
            }

            // 마지막 구 스폰 후 쿨다운 — 다음 사이클까지 간격 확보
            yield return StartCoroutine(PauseableWait(_patternCooldown));

            _activeBalls.RemoveAll(b => b == null);
        }

        protected override void OnCancel()
        {
            foreach (var ball in _activeBalls)
                if (ball != null) Destroy(ball);
            _activeBalls.Clear();
        }

        // ── 스폰 ──────────────────────────────────────────────────────────

        private void SpawnFireball()
        {
            float spawnX = _playerTransform.position.x
                         + Random.Range(-_spawnXRange, _spawnXRange);
            var pos = new Vector3(spawnX, _spawnY, 0f);
            var go  = Instantiate(_prefab, pos, Quaternion.identity);

            if (go.TryGetComponent<FireballProjectile>(out var proj))
            {
                proj.Launch(
                    _fallSprites, _impactSprites,
                    _fallSpeed,   _hitDamage,
                    _debrisDamage, _debrisDuration, _debrisTickInterval,
                    _debrisWidth,  _debrisHeight,
                    _destroyBelowY);
            }

            _activeBalls.Add(go);
        }
    }
}
