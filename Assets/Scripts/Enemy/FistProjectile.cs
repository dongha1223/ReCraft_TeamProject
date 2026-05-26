using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 팔 패턴에서 사용하는 주먹 발사체.
    /// BossFistPattern이 Prepare → Fire → Hide 순서로 제어한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class FistProjectile : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed     = 15f;
        [SerializeField] private float _damage        = 25f;
        [SerializeField] private float _maxTravelTime = 3f;

        private CapsuleCollider2D _collider;
        private Vector2           _fireDir;
        private Coroutine         _moveRoutine;

        private void Awake()
        {
            _collider = GetComponent<CapsuleCollider2D>();
        }

        private void OnEnable()
        {
            if (_collider == null) return;
            _collider.isTrigger = true;
            _collider.enabled   = false;  // 활성화 시 기본적으로 콜라이더 비활성
        }

        private void OnDisable()
        {
            if (_moveRoutine != null) { StopCoroutine(_moveRoutine); _moveRoutine = null; }
            if (_collider != null) _collider.enabled = false;
        }

        // ── 외부 API ─────────────────────────────────────────────────────

        /// <summary>스폰 위치에 현재 회전을 유지한 채 표시. 콜라이더 비활성.</summary>
        public void ShowAtSpawn(Vector3 worldPos)
        {
            transform.position = worldPos;
            if (_collider != null) _collider.enabled = false;
            gameObject.SetActive(true);
        }

        /// <summary>조준 단계: 스폰 위치에 표시, 발사 방향으로 회전, 콜라이더 OFF</summary>
        public void Prepare(Vector3 worldPos, Vector2 fireDir)
        {
            _fireDir           = fireDir;
            transform.position = worldPos;
            _collider.enabled  = false;

            float angle        = Mathf.Atan2(fireDir.y, fireDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            gameObject.SetActive(true);
        }

        /// <summary>대기 단계: 스폰 위치에 표시, 기본 방향, 콜라이더 OFF</summary>
        public void ShowIdle(Vector3 worldPos)
        {
            transform.position = worldPos;
            transform.rotation = Quaternion.identity;
            _collider.enabled  = false;
            gameObject.SetActive(true);
        }

        /// <summary>조준 중 방향 갱신 (매 프레임 호출)</summary>
        public void UpdateAim(Vector2 dir)
        {
            _fireDir           = dir;
            float angle        = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>발사: 콜라이더 ON, 직선 이동 시작</summary>
        public void Fire()
        {
            _collider.enabled = true;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine());
        }

        /// <summary>비활성화 (풀 반환)</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ── 내부 ─────────────────────────────────────────────────────────

        private IEnumerator MoveRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _maxTravelTime)
            {
                float dt           = Time.deltaTime;
                transform.position += (Vector3)(_fireDir * (_moveSpeed * dt));
                elapsed            += dt;
                yield return null;
            }
            Hide();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_collider.enabled)          return;
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(new HitInfo
                {
                    AttackId       = AttackIdGenerator.Next(),
                    Damage         = _damage,
                    DamageType     = DamageType.Physical,
                    SourcePosition = transform.position,
                    KnockbackForce = 5f,
                });
            }
            Hide();
        }
    }
}
