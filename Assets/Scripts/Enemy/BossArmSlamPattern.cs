using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 팔 슬램 패턴.
    /// 플레이어가 있는 방향의 팔을 들어올린 뒤 떨다가 바닥을 쾅 찍는다.
    /// BossHeavyAttackPattern이 FirePillar와 쿨을 공유하며 둘 중 하나를 호출한다.
    ///
    /// 동작 순서:
    ///   1) 플레이어 위치 기준으로 보스 중앙의 좌·우 판단 → 해당 팔 선택
    ///   2) 팔을 _liftHeight 만큼 위로 들어올린다 (lerp)
    ///   3) _trembleDuration 동안 진동 (플레이어 경고)
    ///   4) _slamDuration 내에 팔을 원위치로 빠르게 내려찍는다
    ///   5) 슬램 순간 플레이어 위치가 (arm.x ±slamRangeX, arm.y ~ arm.y+slamRangeY) 안이면 피해
    /// </summary>
    public class BossArmSlamPattern : BossCustomPatternBase
    {
        public override string PatternId => "ArmSlam";

        [Header("데이터 SO")]
        [SerializeField] private BossArmSlamDataSO _data;

        [Header("팔 Transform")]
        [SerializeField] private Transform _leftArmTransform;
        [SerializeField] private Transform _rightArmTransform;

        [Header("팔 파츠 (파괴 여부 확인)")]
        [SerializeField] private BossPartHP _leftArmHP;
        [SerializeField] private BossPartHP _rightArmHP;

        [Header("플레이어")]
        [SerializeField] private Transform _playerTransform;

        [Header("들어올리기")]
        [SerializeField] private float _liftHeight   = 2f;
        [SerializeField] private float _liftDuration  = 1.2f;

        [Header("떨림")]
        [SerializeField] private float _trembleDuration  = 1.2f;
        [SerializeField] private float _trembleFrequency = 8f;
        [SerializeField] private float _trembleAmplitude = 0.15f;

        [Header("슬램")]
        [SerializeField] private float _slamDuration = 0.15f;

        [Header("피해")]
        [SerializeField] private float _slamDamage    = 30f;
        [SerializeField] private float _slamKnockback = 8f;

        [Header("피해 범위")]
        [SerializeField] private float _slamRangeX = 6f;
        [SerializeField] private float _slamRangeY = 2f;

        [Header("화면 흔들림")]
        [SerializeField] private float _shakeDuration  = 0.3f;
        [SerializeField] private float _shakeMagnitude = 0.25f;

        private CameraFollow _cameraFollow;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data == null) return;
            _liftHeight       = _data.LiftHeight;
            _liftDuration     = _data.LiftDuration;
            _trembleDuration  = _data.TrembleDuration;
            _trembleFrequency = _data.TrembleFrequency;
            _trembleAmplitude = _data.TrembleAmplitude;
            _slamDuration     = _data.SlamDuration;
            _slamDamage       = _data.SlamDamage;
            _slamKnockback    = _data.SlamKnockback;
            _slamRangeX       = _data.SlamRangeX;
            _slamRangeY       = _data.SlamRangeY;
            _shakeDuration    = _data.ShakeDuration;
            _shakeMagnitude   = _data.ShakeMagnitude;
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) _playerTransform = playerGO.transform;
            }
            _cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        }

        // ── BossCustomPatternBase ──────────────────────────────────────────

        protected override IEnumerator ExecuteRoutine()
        {
            Transform selectedArm = SelectArm();
            if (selectedArm == null) yield break;

            Vector3 originalLocalPos = selectedArm.localPosition;
            Vector3 raisedLocalPos   = originalLocalPos + Vector3.up * _liftHeight;

            yield return StartCoroutine(LiftArm(selectedArm, originalLocalPos, raisedLocalPos));
            yield return StartCoroutine(TrembleArm(selectedArm, raisedLocalPos));
            yield return StartCoroutine(SlamArm(selectedArm, raisedLocalPos, originalLocalPos));

            ApplySlamDamage(selectedArm, originalLocalPos);
            _cameraFollow?.Shake(_shakeDuration, _shakeMagnitude);

            selectedArm.localPosition = originalLocalPos;
        }

        protected override void OnCancel() { }

        // ── 애니메이션 서브루틴 ───────────────────────────────────────────

        private IEnumerator LiftArm(Transform arm, Vector3 from, Vector3 to)
        {
            float elapsed    = 0f;
            float invDuration = 1f / _liftDuration;
            while (elapsed < _liftDuration)
            {
                if (!IsBossFrozen) elapsed += Time.deltaTime;
                arm.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed * invDuration));
                yield return null;
            }
            arm.localPosition = to;
        }

        private IEnumerator TrembleArm(Transform arm, Vector3 baseLocal)
        {
            float elapsed      = 0f;
            float angularFreq  = _trembleFrequency * Mathf.PI * 2f;
            while (elapsed < _trembleDuration)
            {
                if (!IsBossFrozen)
                {
                    elapsed += Time.deltaTime;
                    float xOffset     = Mathf.Sin(elapsed * angularFreq) * _trembleAmplitude;
                    arm.localPosition = baseLocal + new Vector3(xOffset, 0f, 0f);
                }
                yield return null;
            }
            arm.localPosition = baseLocal;
        }

        private IEnumerator SlamArm(Transform arm, Vector3 from, Vector3 to)
        {
            float elapsed     = 0f;
            float invDuration  = 1f / _slamDuration;
            while (elapsed < _slamDuration)
            {
                elapsed           += Time.deltaTime; // 슬램은 freeze 무시 (빠른 연출)
                arm.localPosition  = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed * invDuration));
                yield return null;
            }
            arm.localPosition = to;
        }

        // ── 피해 판정 ────────────────────────────────────────────────────

        private static readonly Collider2D[] _slamHitBuffer = new Collider2D[4];

        private void ApplySlamDamage(Transform arm, Vector3 originalLocalPos)
        {
            Vector3 armWorld = arm.parent != null
                             ? arm.parent.TransformPoint(originalLocalPos)
                             : originalLocalPos;

            // OverlapBox: 팔 위치 중심으로 좌우 slamRangeX, 상하 slamRangeY 범위
            var size  = new Vector2(_slamRangeX * 2f, _slamRangeY * 2f);
            int count = Physics2D.OverlapBoxNonAlloc(armWorld, size, 0f, _slamHitBuffer);

            for (int i = 0; i < count; i++)
            {
                if (!_slamHitBuffer[i].CompareTag("Player")) continue;

                var target = _slamHitBuffer[i].GetComponentInParent<IDamageable>();
                if (target == null) continue;

                target.TakeDamage(new HitInfo
                {
                    AttackId       = AttackIdGenerator.Next(),
                    Damage         = _slamDamage,
                    DamageType     = DamageType.Physical,
                    SourcePosition = armWorld,
                    KnockbackForce = _slamKnockback,
                });
                break; // 플레이어는 한 명
            }
        }

        // ── 팔 선택 ──────────────────────────────────────────────────────

        // Boss_left / Boss_right 는 화면상 좌우가 반전되어 있으므로
        // 플레이어가 오른쪽 → 화면상 오른쪽 팔 = Boss_left 사용.
        // 해당 팔이 파괴됐으면 반대편 팔을 사용하고, 둘 다 파괴되면 null 반환.
        private Transform SelectArm()
        {
            if (_playerTransform == null) return null;

            bool playerIsRight = _playerTransform.position.x > transform.position.x;

            Transform  primary    = playerIsRight ? _leftArmTransform  : _rightArmTransform;
            BossPartHP primaryHP  = playerIsRight ? _leftArmHP         : _rightArmHP;
            Transform  fallback   = playerIsRight ? _rightArmTransform : _leftArmTransform;
            BossPartHP fallbackHP = playerIsRight ? _rightArmHP        : _leftArmHP;

            if (IsArmAlive(primary,  primaryHP))  return primary;
            if (IsArmAlive(fallback, fallbackHP)) return fallback;
            return null;
        }

        private static bool IsArmAlive(Transform arm, BossPartHP hp)
            => arm != null && (hp == null || !hp.IsDead);

        // ── Gizmos ───────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform arm = _leftArmTransform != null ? _leftArmTransform : _rightArmTransform;
            if (arm == null) return;

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            // ApplySlamDamage와 동일: arm 위치 중심, 상하 slamRangeY 대칭
            Gizmos.DrawWireCube(arm.position, new Vector3(_slamRangeX * 2f, _slamRangeY * 2f, 0.1f));
        }
#endif
    }
}
