using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어 스킬 컨트롤러
    ///
    /// [A 키] 검기 발산
    ///   - 흰색+노랑 2중 레이어 그라데이션 초승달 투사체
    ///   - 상현달(우측) + 하현달(좌측) 연속 발사
    ///   - 적 명중 시 흰/노랑 파티클 임팩트 + 즉시 소멸
    ///
    /// [S 키] 롤링 슬레쉬
    ///   - 3연속 앞구르기 전진
    ///   - 앞구르기 1회전당 → 전진 1회 + 푸른빛 가로 타원 참격 이펙트 1회
    ///   - 캡슐 OverlapAll로 피해 판정
    /// </summary>
    public class PlayerSkill : MonoBehaviour
    {
        // ── 검기 발산 설정 ────────────────────────────────────────────
        [Header("검기 발산 (A 키)")]
        [SerializeField] private float     _energy_Damage      = 30f;
        [SerializeField] private float     _energy_Speed       = 10f;
        [SerializeField] private float     _energy_MaxDistance = 14f;
        [SerializeField] private float     _energy_Cooldown    = 1.5f;
        [SerializeField] private float     _energy_VertOffset  = 0.25f;
        [SerializeField] private LayerMask _enemyLayer;

        // ── 롤링 슬레쉬 설정 ─────────────────────────────────────────
        [Header("롤링 슬레쉬 (S 키)")]
        [SerializeField] private float   _roll_Damage    = 25f;
        [Tooltip("1회 구르기당 전진 거리")]
        [SerializeField] private float   _roll_Distance  = 2.2f;
        [Tooltip("1회 구르기 소요 시간 (초)")]
        [SerializeField] private float   _roll_RollTime  = 0.22f;
        [SerializeField] private float   _roll_Cooldown  = 2.2f;
        [Tooltip("가로 타원 크기 (width > height)")]
        [SerializeField] private Vector2 _roll_OvalSize  = new Vector2(2.6f, 1.0f);
        [Tooltip("텀블링 회전 속도 (deg/sec)")]
        [SerializeField] private float   _roll_SpinSpeed = 720f;

        // ── 컴포넌트 캐시 ─────────────────────────────────────────────
        private SpriteRenderer _sr;
        private Rigidbody2D    _rb;
        private Animator       _anim;

        // ── 상태 ─────────────────────────────────────────────────────
        private bool  _canSkill1   = true;
        private bool  _canSkill2   = true;
        private float _rollDirSign = 1f;

        /// <summary>롤링 중 PlayerController 이동 차단용</summary>
        public bool IsRolling { get; private set; }

        private static readonly int AnimRollingSlash = Animator.StringToHash("RollingSlash");

        // ═════════════════════════════════════════════════════════════
        private void Awake()
        {
            _sr   = GetComponent<SpriteRenderer>();
            _rb   = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.aKey.wasPressedThisFrame && _canSkill1)
                StartCoroutine(Skill1_SwordEnergy());

            if (kb.sKey.wasPressedThisFrame && _canSkill2 && !IsRolling)
                StartCoroutine(Skill2_RollingSlash());
        }

        // ═════════════════════════════════════════════════════════════
        //  스킬 1 – 검기 발산
        // ═════════════════════════════════════════════════════════════
        private IEnumerator Skill1_SwordEnergy()
        {
            _canSkill1 = false;

            // 플레이어가 바라보는 방향으로만 발사
            bool facingLeft = (_sr != null && _sr.flipX);
            Vector2 dir = facingLeft ? Vector2.left : Vector2.right;
            SpawnCrescent(dir, upper: !facingLeft);

            yield return new WaitForSeconds(_energy_Cooldown);
            _canSkill1 = true;
        }

        private void SpawnCrescent(Vector2 dir, bool upper)
        {
            float yOff   = upper ? _energy_VertOffset : -_energy_VertOffset;
            Vector2 pos  = (Vector2)transform.position + new Vector2(0f, yOff);

            var go = new GameObject(upper ? "SwordEnergy_상현달" : "SwordEnergy_하현달");
            go.transform.position = pos;

            var p = go.AddComponent<SwordEnergyProjectile>();
            p.damage        = _energy_Damage;
            p.speed         = _energy_Speed;
            p.maxDistance   = _energy_MaxDistance;
            p.enemyLayer    = _enemyLayer;
            p.facingRight   = (dir.x > 0f);
            p.Launch(dir);
        }

        // ═════════════════════════════════════════════════════════════
        //  스킬 2 – 롤링 슬레쉬
        //  구조: 3회 반복 { 360° 전진 → 이펙트+피해 }
        // ═════════════════════════════════════════════════════════════
        private IEnumerator Skill2_RollingSlash()
        {
            _canSkill2   = false;
            IsRolling    = true;
            _rollDirSign = (_sr != null && _sr.flipX) ? -1f : 1f;

            float moveSpeed = _roll_Distance / _roll_RollTime;

            _anim?.SetTrigger(AnimRollingSlash);

            // Rigidbody 회전 잠금 해제 → 텀블링 허용
            var saved = _rb != null ? _rb.constraints : RigidbodyConstraints2D.FreezeRotation;
            if (_rb != null)
                _rb.constraints = RigidbodyConstraints2D.None;

            var alreadyHit = new HashSet<Collider2D>();

            // ── 앞구르기 3번 ────────────────────────────────────────
            for (int roll = 0; roll < 3; roll++)
            {
                // ① 1회 구르기 이동 (360° 회전하며 전진)
                float elapsed = 0f;
                while (elapsed < _roll_RollTime)
                {
                    if (_rb != null)
                    {
                        _rb.linearVelocity  = new Vector2(moveSpeed * _rollDirSign,
                                                          _rb.linearVelocity.y);
                        _rb.angularVelocity = -_roll_SpinSpeed * _rollDirSign;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // ② 구르기 1회 완료 → 가로 타원 참격 이펙트 + 피해
                SpawnSlashVFX((Vector2)transform.position);
                ApplyOvalHit((Vector2)transform.position, alreadyHit);

                // 구르기 사이 짧은 정지감 (0.04초)
                if (_rb != null) _rb.angularVelocity = 0f;
                yield return new WaitForSeconds(0.04f);
            }

            // ── 종료 정리 ────────────────────────────────────────────
            if (_rb != null)
            {
                _rb.linearVelocity  = new Vector2(0f, _rb.linearVelocity.y);
                _rb.angularVelocity = 0f;
                _rb.constraints     = saved;
            }
            transform.rotation = Quaternion.identity;

            IsRolling = false;

            yield return new WaitForSeconds(_roll_Cooldown);
            _canSkill2 = true;
        }

        // ── 가로 타원 참격 이펙트 생성 ───────────────────────────────
        private void SpawnSlashVFX(Vector2 pos)
        {
            var go = new GameObject("RollingSlash_VFX");
            go.transform.position = pos;
            go.AddComponent<RollingSlashVisual>().Initialize(_roll_OvalSize, _rollDirSign);
        }

        // ── 가로 캡슐 피해 판정 ─────────────────────────────────────
        private void ApplyOvalHit(Vector2 center, HashSet<Collider2D> alreadyHit)
        {
            // 가로 타원 → CapsuleDirection2D.Horizontal
            Collider2D[] hits = Physics2D.OverlapCapsuleAll(
                center, _roll_OvalSize,
                CapsuleDirection2D.Horizontal, 0f, _enemyLayer);

            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col)) continue;
                alreadyHit.Add(col);
                col.GetComponent<EnemyStats>()?.TakeDamage(_roll_Damage);
            }
        }

        // ── 기즈모 (에디터 시각화) ────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.4f);
            const int seg = 32;
            float rx = _roll_OvalSize.x * 0.5f;
            float ry = _roll_OvalSize.y * 0.5f;
            Vector3 prev = transform.position + new Vector3(rx, 0f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float   a   = (float)i / seg * Mathf.PI * 2f;
                Vector3 cur = transform.position
                            + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }
    }
}
