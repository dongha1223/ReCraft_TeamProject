using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 머리 최종 페이즈 상시 레이저 패턴 (스우 스타일).
    ///
    /// 사용 순서:
    ///   1) BossHeadFinalPhase가 yield return OpenMouthRoutine()  → 입 벌리기 대기
    ///   2) BossHeadFinalPhase가 ActivateLasers()                → 레이저 + 회전 시작
    ///   3) BossHeadFinalPhase가 StopLasers() (사망 등)          → 전체 정리
    ///
    /// 씬 구성:
    ///   MouthPoint(Boss_Head 자식)
    ///   └─ LaserPivot (회전 기준, Z=-1로 Ground 앞에 렌더)
    ///      ├─ Razer_Ball  (중심 구체, SpriteRenderer+Animator)
    ///      ├─ Laser_Right (rot 0°)
    ///      ├─ Laser_Left  (rot 180°)
    ///      ├─ Laser_Up    (rot 90°)
    ///      └─ Laser_Down  (rot -90°)
    /// </summary>
    public class BossHeadLaserPattern : MonoBehaviour
    {
        [Header("데이터 SO")]
        [SerializeField] private BossHeadLaserDataSO _data;

        [Header("피벗 (회전 대상)")]
        [SerializeField] private Transform _laserPivot;

        [Header("레이저 볼 (LaserPivot 중앙, 입 열릴 때만 활성)")]
        [SerializeField] private GameObject _razerBall;

        
[Header("레이저 오브젝트 (LaserPivot 자식)")]
        [SerializeField] private GameObject _laserRight;
        [SerializeField] private GameObject _laserLeft;
        [SerializeField] private GameObject _laserUp;
        [SerializeField] private GameObject _laserDown;

        [Header("참조")]
        [Tooltip("입 위치 Transform (Boss_Head 자식 빈 오브젝트)")]
        [SerializeField] private Transform _mouthPoint;
        [SerializeField] private Transform _playerTransform;

        [Header("입 애니메이션")]
        [SerializeField] private BossRoomManager _bossRoomManager;

        [Header("회전 (반시계 방향)")]
        [Tooltip("초당 회전 각도 (양수 = 반시계)")]
        [SerializeField] private float _rotationSpeed = 15f;

        // ── 설정값 ────────────────────────────────────────────────────────
        private float _damagePerTick;
        private float _tickInterval;
        private float _laserHitLength;
        private float _laserHalfWidth;

        private Coroutine _tickRoutine;
        private Coroutine _rotateRoutine;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data == null) return;
            _damagePerTick  = _data.DamagePerTick;
            _tickInterval   = _data.TickInterval;
            _laserHitLength = _data.LaserHitLength;
            _laserHalfWidth = _data.LaserHitWidth * 0.5f;

            if (_mouthPoint != null)
                ApplyLaserScale(_data.LaserScaleX);
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) _playerTransform = go.transform;
            }
            SetLasersActive(false);
        }

        // ── 외부 API ──────────────────────────────────────────────────────

        /// <summary>입 벌리기 애니메이션이 완료될 때까지 대기하는 루틴.</summary>
        public IEnumerator OpenMouthRoutine()
        {
            if (_bossRoomManager == null) yield break;
            yield return _bossRoomManager.OpenMouth();
        }

        /// <summary>레이저 활성화 + 반시계 회전 시작. OpenMouthRoutine 완료 후 호출.</summary>
        public void ActivateLasers()
        {
            SetLasersActive(true);

            if (_tickRoutine != null) StopCoroutine(_tickRoutine);
            _tickRoutine = StartCoroutine(DamageTick());

            if (_rotateRoutine != null) StopCoroutine(_rotateRoutine);
            _rotateRoutine = StartCoroutine(RotatePivot());
        }

        /// <summary>레이저 비활성화 + 회전 중지 + 입 다물기.</summary>
        public void StopLasers()
        {
            if (_tickRoutine   != null) { StopCoroutine(_tickRoutine);   _tickRoutine   = null; }
            if (_rotateRoutine != null) { StopCoroutine(_rotateRoutine); _rotateRoutine = null; }
            SetLasersActive(false);
            _bossRoomManager?.CloseMouth();
            if (_laserPivot != null) _laserPivot.localRotation = Quaternion.identity;
        }

        // ── 피해 틱 ───────────────────────────────────────────────────────

        private IEnumerator DamageTick()
        {
            var wait = new WaitForSeconds(_tickInterval);
            while (true)
            {
                yield return wait;
                if (IsPlayerInLaser()) ApplyDamage();
            }
        }

        // 피벗이 회전하므로 피해 판정은 MouthPoint 기준 십자를 월드 좌표로 변환해 체크
        private bool IsPlayerInLaser()
        {
            if (_playerTransform == null || _mouthPoint == null) return false;

            // 회전된 피벗 기준으로 플레이어의 로컬 위치를 구해 십자 축 체크
            Vector2 origin    = _mouthPoint.position;
            Vector2 playerPos = _playerTransform.position;
            Vector2 delta     = playerPos - origin;

            // LaserPivot의 현재 Z 회전각으로 역변환
            float angle  = _laserPivot != null ? -_laserPivot.eulerAngles.z * Mathf.Deg2Rad : 0f;
            float cos    = Mathf.Cos(angle);
            float sin    = Mathf.Sin(angle);
            float localX = cos * delta.x - sin * delta.y;
            float localY = sin * delta.x + cos * delta.y;

            bool onVertical   = Mathf.Abs(localX) <= _laserHalfWidth && Mathf.Abs(localY) <= _laserHitLength;
            bool onHorizontal = Mathf.Abs(localY) <= _laserHalfWidth && Mathf.Abs(localX) <= _laserHitLength;

            return onVertical || onHorizontal;
        }

        private void ApplyDamage()
        {
            if (_playerTransform.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(new HitInfo
                {
                    AttackId       = AttackIdGenerator.Next(),
                    Damage         = _damagePerTick,
                    DamageType     = DamageType.Magic,
                    SourcePosition = _mouthPoint != null ? _mouthPoint.position : transform.position,
                    KnockbackForce = 0f,
                });
            }
        }

        // ── 반시계 회전 ───────────────────────────────────────────────────

        private IEnumerator RotatePivot()
        {
            while (true)
            {
                if (_laserPivot != null)
                    _laserPivot.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // ── 레이저 스케일 초기화 ───────────────────────────────────────────

        private void ApplyLaserScale(float worldScaleX)
        {
            if (_mouthPoint == null) return;
            float parentScale = _mouthPoint.lossyScale.x;
            if (Mathf.Approximately(parentScale, 0f)) return;
            float local = worldScaleX / parentScale;

            SetLaserScaleX(_laserRight, local);
            SetLaserScaleX(_laserLeft,  local);
            SetLaserScaleX(_laserUp,    local);
            SetLaserScaleX(_laserDown,  local);
        }

        private static void SetLaserScaleX(GameObject laser, float localScaleX)
        {
            if (laser == null) return;
            var s = laser.transform.localScale;
            laser.transform.localScale = new Vector3(localScaleX, s.y, s.z);
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

private void SetLasersActive(bool active)
        {
            if (_razerBall  != null) _razerBall.SetActive(active);
            if (_laserRight != null) _laserRight.SetActive(active);
            if (_laserLeft  != null) _laserLeft.SetActive(active);
            if (_laserUp    != null) _laserUp.SetActive(active);
            if (_laserDown  != null) _laserDown.SetActive(active);
        }
    }
}
