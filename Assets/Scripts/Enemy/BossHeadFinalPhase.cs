using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 비-머리 파츠가 모두 파괴되면 자동으로 트리거되는 최종 페이즈.
    ///
    /// 동작 순서:
    ///   1) 패턴 루프 즉시 중단, 머리 피격 차단
    ///   2) 머리 낙하 (localY 하강 + Z축 기울기, easeIn)
    ///   3) 착지 카메라 흔들림 + 착지 대기
    ///   4) 머리 상승 & 플랫폼 동시 밑에서 등장 (easeOut)
    ///   5) 피격 해제 → 입 벌리기 → 레이저 활성 → 무한 bob
    /// </summary>
    public class BossHeadFinalPhase : MonoBehaviour
    {
        [Header("데이터 SO")]
        [SerializeField] private BossHeadFinalPhaseDataSO _data;

        [Header("참조")]
        [SerializeField] private BossMainController _bossMainController;
        [SerializeField] private Transform          _headTransform;
        [SerializeField] private BossPartHP         _headPartHP;

        [Tooltip("머리를 제외한 파츠 (Body, Left, Right). 모두 파괴되면 페이즈 시작.")]
        [SerializeField] private BossPartHP[] _nonHeadParts;

        [Header("레이저 패턴 (공중 부유 시 활성)")]
        [SerializeField] private BossHeadLaserPattern _laserPattern;

        [Header("화염구 낙하 패턴 (공중 부유 시 반복)")]
        [SerializeField] private BossFireballPattern _fireballPattern;

        [Header("플랫폼 (머리 상승 시 밑에서 함께 등장)")]
        [SerializeField] private Transform[] _platforms;
        [Tooltip("플랫폼이 시작하는 오프셋 (최종 위치보다 얼마나 아래에서 시작할지)")]
        [SerializeField] private float _platformRiseOffset = 12f;

        // ── 설정값 ────────────────────────────────────────────────────────
        private float _fallLocalY;
        private float _fallTiltAngle;
        private float _fallDuration;
        private float _slamShakeDuration;
        private float _slamShakeMagnitude;
        private float _pauseAfterFall;
        private float _floatLocalY;
        private float _riseDuration;
        private float _bobAmplitude;
        private float _bobAngularFreq;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        private int          _destroyedCount;
        private bool         _triggered;
        private Coroutine    _routineHandle;
        private Coroutine    _fireballLoopHandle;
        private CameraFollow _cameraFollow;
        private Vector3[]    _platformFinalPositions;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data != null)
            {
                _fallLocalY         = _data.FallLocalY;
                _fallTiltAngle      = _data.FallTiltAngle;
                _fallDuration       = _data.FallDuration;
                _slamShakeDuration  = _data.SlamShakeDuration;
                _slamShakeMagnitude = _data.SlamShakeMagnitude;
                _pauseAfterFall     = _data.PauseAfterFall;
                _floatLocalY        = _data.FloatLocalY;
                _riseDuration       = _data.RiseDuration;
                _bobAmplitude       = _data.BobAmplitude;
                _bobAngularFreq     = _data.BobFrequency * Mathf.PI * 2f;
            }

            foreach (var part in _nonHeadParts)
                if (part != null) part.OnPartDestroyed += OnNonHeadPartDestroyed;

            BossMainController.OnBossEngaged += HandleBossEngaged;
            BossMainController.OnBossDead    += HandleBossDead;
        }

        private void Start()
        {
            _cameraFollow = Camera.main?.GetComponent<CameraFollow>();

            // 플랫폼 최종 위치 캐시 + 처음엔 비활성화 (머리 페이즈 전까지 숨김)
            if (_platforms != null)
            {
                _platformFinalPositions = new Vector3[_platforms.Length];
                for (int i = 0; i < _platforms.Length; i++)
                {
                    if (_platforms[i] == null) continue;
                    _platformFinalPositions[i] = _platforms[i].position;
                    _platforms[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var part in _nonHeadParts)
                if (part != null) part.OnPartDestroyed -= OnNonHeadPartDestroyed;

            BossMainController.OnBossEngaged -= HandleBossEngaged;
            BossMainController.OnBossDead    -= HandleBossDead;
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────────────

        private void HandleBossEngaged()
        {
            _destroyedCount = 0;
            _triggered      = false;
            _headPartHP?.SetForceInvincible(false);
            HidePlatforms();
        }

        private void HandleBossDead()
        {
            _laserPattern?.StopLasers();
            _fireballPattern?.Cancel();
            if (_fireballLoopHandle != null)
            {
                StopCoroutine(_fireballLoopHandle);
                _fireballLoopHandle = null;
            }
            StopRoutine();

            // BossPartHP.gameObject.SetActive(false)는 HP 컴포넌트 오브젝트만 끔.
            // 비주얼 루트(_headTransform)를 여기서 명시적으로 비활성화한다.
            if (_headTransform != null)
                _headTransform.gameObject.SetActive(false);
        }

        private void OnNonHeadPartDestroyed()
        {
            _destroyedCount++;

            if (_triggered) return;
            if (_nonHeadParts == null || _destroyedCount < _nonHeadParts.Length) return;
            if (_headPartHP != null && _headPartHP.IsDead) return;

            _triggered     = true;
            _routineHandle = StartCoroutine(FinalPhaseRoutine());
        }

        // ── 최종 페이즈 루틴 ──────────────────────────────────────────────

        private IEnumerator FinalPhaseRoutine()
        {
            _bossMainController?.StopAllPatternsForHeadPhase();
            _headPartHP?.SetForceInvincible(true);

            if (_headTransform == null) yield break;

            Vector3    startLocalPos = _headTransform.localPosition;
            Vector3    fallLocalPos  = new Vector3(startLocalPos.x, _fallLocalY, startLocalPos.z);
            Vector3    floatLocalPos = new Vector3(startLocalPos.x, _floatLocalY, startLocalPos.z);
            Quaternion startRot      = _headTransform.localRotation;
            Quaternion tiltRot       = Quaternion.Euler(0f, 0f, _fallTiltAngle);
            Quaternion straightRot   = Quaternion.identity;

            // 낙하 (easeIn)
            yield return StartCoroutine(AnimatePosRot(
                startLocalPos, fallLocalPos, startRot, tiltRot,
                _fallDuration, easeIn: true));

            _cameraFollow?.Shake(_slamShakeDuration, _slamShakeMagnitude);
            yield return new WaitForSeconds(_pauseAfterFall);

            // 상승 + 플랫폼 동시 등장 (병렬 코루틴)
            StartCoroutine(RisePlatforms(_riseDuration));
            yield return StartCoroutine(AnimatePosRot(
                fallLocalPos, floatLocalPos, tiltRot, straightRot,
                _riseDuration, easeIn: false));

            _headPartHP?.SetForceInvincible(false);

            if (_laserPattern != null)
                yield return StartCoroutine(_laserPattern.OpenMouthRoutine());
            _laserPattern?.ActivateLasers();

            // 화염구 낙하 패턴 반복 시작
            if (_fireballPattern != null)
                _fireballLoopHandle = StartCoroutine(FireballLoop());

            // 무한 bob
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                _headTransform.localPosition =
                    floatLocalPos + Vector3.up * (Mathf.Sin(elapsed * _bobAngularFreq) * _bobAmplitude);
                yield return null;
            }
        }

        // 화염구 패턴을 계속 반복 실행한다 (레이저와 병렬)
        private IEnumerator FireballLoop()
        {
            while (true)
                yield return _fireballPattern.BeginExecute();
        }

        // ── 플랫폼 제어 ───────────────────────────────────────────────────

        // 플랫폼을 비활성화하고 시작 위치(지면 아래)로 되돌린다.
        private void HidePlatforms()
        {
            if (_platforms == null || _platformFinalPositions == null) return;
            for (int i = 0; i < _platforms.Length; i++)
            {
                if (_platforms[i] == null) continue;
                _platforms[i].gameObject.SetActive(false);
            }
        }

        // 플랫폼을 활성화하고 지면 아래에서 최종 위치까지 easeOut으로 올린다.
        private IEnumerator RisePlatforms(float duration)
        {
            if (_platforms == null || _platformFinalPositions == null) yield break;

            int count = _platforms.Length;

            // 시작 위치(아래) 미리 계산 — 루프 내 매 프레임 Vector3 할당 방지
            var startPositions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                if (_platforms[i] == null) continue;
                var p = _platformFinalPositions[i];
                startPositions[i]      = new Vector3(p.x, p.y - _platformRiseOffset, p.z);
                _platforms[i].position = startPositions[i];
                _platforms[i].gameObject.SetActive(true);
            }

            float elapsed     = 0f;
            float invDuration = 1f / duration;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float raw = Mathf.Clamp01(elapsed * invDuration);
                float t   = 1f - (1f - raw) * (1f - raw); // easeOut

                for (int i = 0; i < count; i++)
                {
                    if (_platforms[i] == null) continue;
                    _platforms[i].position = Vector3.Lerp(startPositions[i], _platformFinalPositions[i], t);
                }
                yield return null;
            }

            // 최종 위치 확정
            for (int i = 0; i < count; i++)
                if (_platforms[i] != null)
                    _platforms[i].position = _platformFinalPositions[i];
        }

        // ── 애니메이션 헬퍼 ───────────────────────────────────────────────

        private IEnumerator AnimatePosRot(
            Vector3 fromPos, Vector3 toPos,
            Quaternion fromRot, Quaternion toRot,
            float duration, bool easeIn)
        {
            float elapsed     = 0f;
            float invDuration = 1f / duration;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float raw    = Mathf.Clamp01(elapsed * invDuration);
                float curved = easeIn ? raw * raw : 1f - (1f - raw) * (1f - raw);
                _headTransform.localPosition = Vector3.Lerp(fromPos, toPos, curved);
                _headTransform.localRotation = Quaternion.Lerp(fromRot, toRot, curved);
                yield return null;
            }
            _headTransform.localPosition = toPos;
            _headTransform.localRotation = toRot;
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

        private void StopRoutine()
        {
            if (_routineHandle == null) return;
            StopCoroutine(_routineHandle);
            _routineHandle = null;
        }
    }
}
