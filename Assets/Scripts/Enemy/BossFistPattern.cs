using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 팔 주먹 상시 패턴.
    /// OnBossEngaged 이벤트를 수신해 자동으로 시작·종료된다.
    ///
    /// 타이밍 (기본값 기준):
    ///   왼팔: t=0  조준(3s) → t=3  발사 → t=8  재등장(대기) → t=10 조준 → ... (10초 사이클)
    ///   오른팔: t=6  조준(3s) → t=9  발사 → t=14 재등장(대기) → t=16 조준 → ... (6초 오프셋)
    ///
    /// 각 팔이 파괴되면 해당 주먹 루프만 종료.
    /// 양쪽 모두 파괴되면 ExecuteRoutine 이 자연 완료된다.
    /// </summary>
    public class BossFistPattern : BossCustomPatternBase
    {
        public override string PatternId => "FistAttack";

        [Header("팔 파츠 (생존 여부 체크)")]
        [SerializeField] private BossPartHP _leftArm;
        [SerializeField] private BossPartHP _rightArm;

        [Header("주먹 오브젝트 (씬에 배치된 오브젝트 직접 연결)")]
        [Tooltip("왼팔 주먹 오브젝트 (FistProjectile 컴포넌트 포함)")]
        [SerializeField] private FistProjectile _leftFist;
        [Tooltip("오른팔 주먹 오브젝트 (FistProjectile 컴포넌트 포함)")]
        [SerializeField] private FistProjectile _rightFist;

        [Header("스폰 위치")]
        [Tooltip("왼쪽 주먹이 대기·발사되는 월드 위치")]
        [SerializeField] private Transform _leftSpawnPoint;
        [Tooltip("오른쪽 주먹이 대기·발사되는 월드 위치")]
        [SerializeField] private Transform _rightSpawnPoint;

        [Header("플레이어")]
        [SerializeField] private Transform _playerTransform;

        [Header("타이밍 (초)")]
        [Tooltip("스폰 후 발사까지 조준 시간")]
        [SerializeField] private float _aimDuration    = 3f;
        [Tooltip("발사 후 재등장까지 대기 시간 (기본 5s → 총 8s 경과)")]
        [SerializeField] private float _respawnDelay   = 5f;
        [Tooltip("재등장 후 조준 시작까지 대기 시간 (기본 2s → 총 10s 경과)")]
        [SerializeField] private float _idleDelay      = 2f;
        [Tooltip("오른팔 사이클 시작 오프셋 (기본 6s)")]
        [SerializeField] private float _rightArmOffset = 6f;

        // ── 내부 ──────────────────────────────────────────────────────────
        private bool _leftArmAlive  = true;
        private bool _rightArmAlive = true;
        private bool _leftLoopDone;
        private bool _rightLoopDone;

        private Coroutine _leftLoop;
        private Coroutine _rightLoop;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_leftArm  != null) _leftArm.OnPartDestroyed  += () => _leftArmAlive  = false;
            if (_rightArm != null) _rightArm.OnPartDestroyed += () => _rightArmAlive = false;

            BossMainController.OnBossEngaged += HandleBossEngaged;
            BossMainController.OnBossDead    += Cancel;
        }

        private void OnDestroy()
        {
            BossMainController.OnBossEngaged -= HandleBossEngaged;
            BossMainController.OnBossDead    -= Cancel;
        }

        private void HandleBossEngaged()
        {
            _leftArmAlive  = true;
            _rightArmAlive = true;

            // 첫 진입·재진입 모두 두 팔을 스폰 위치에 즉시 표시
            if (_leftFist  != null && _leftSpawnPoint  != null) _leftFist.ShowAtSpawn(_leftSpawnPoint.position);
            if (_rightFist != null && _rightSpawnPoint != null) _rightFist.ShowAtSpawn(_rightSpawnPoint.position);

            BeginExecute();
        }

        // ── BossCustomPatternBase ──────────────────────────────────────────

        protected override IEnumerator ExecuteRoutine()
        {
            _leftLoopDone  = !_leftArmAlive  || _leftFist  == null;
            _rightLoopDone = !_rightArmAlive || _rightFist == null;

            if (!_leftLoopDone)  _leftLoop  = StartCoroutine(FistLoop(true));
            if (!_rightLoopDone) _rightLoop = StartCoroutine(FistLoop(false));

            while (!_leftLoopDone || !_rightLoopDone)
                yield return null;
        }

        protected override void OnCancel()
        {
            if (_leftLoop  != null) { StopCoroutine(_leftLoop);  _leftLoop  = null; }
            if (_rightLoop != null) { StopCoroutine(_rightLoop); _rightLoop = null; }
            _leftFist?.Hide();
            _rightFist?.Hide();
        }

        // ── 주먹 루프 ─────────────────────────────────────────────────────

        private IEnumerator FistLoop(bool isLeft)
        {
            FistProjectile fist       = isLeft ? _leftFist       : _rightFist;
            Transform      spawnPoint = isLeft ? _leftSpawnPoint : _rightSpawnPoint;

            // 오른팔은 오프셋 후 시작 (포효 중 일시정지 포함)
            if (!isLeft) yield return StartCoroutine(PauseableWait(_rightArmOffset));

            Vector3 spawnPos = spawnPoint.position;  // 스폰 위치 캐시 (이동하지 않음)

            while (isLeft ? _leftArmAlive : _rightArmAlive)
            {
                if (_playerTransform == null) { yield return null; continue; }

                // 조준 단계: 매 프레임 플레이어 방향으로 회전 갱신
                Vector2 initDir = ((Vector2)(_playerTransform.position - spawnPos)).normalized;
                fist.Prepare(spawnPos, initDir);

                float aimElapsed = 0f;
                while (aimElapsed < _aimDuration)
                {
                    Vector2 dir = ((Vector2)(_playerTransform.position - spawnPos)).normalized;
                    fist.UpdateAim(dir);
                    if (!IsBossFrozen) aimElapsed += Time.deltaTime;
                    yield return null;
                }

                fist.Fire();
                yield return StartCoroutine(PauseableWait(_respawnDelay));

                // 재등장 (대기 상태)
                fist.ShowIdle(spawnPos);
                yield return StartCoroutine(PauseableWait(_idleDelay));
            }

            fist.Hide();

            if (isLeft) _leftLoopDone  = true;
            else        _rightLoopDone = true;
        }

    }
}
