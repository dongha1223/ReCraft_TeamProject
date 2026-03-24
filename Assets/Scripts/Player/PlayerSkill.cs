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
    ///   - 흰색+노랑 그라데이션 초승달 투사체 (바라보는 방향)
    ///   - 적 명중 시 흰/노랑 파티클 임팩트 + 즉시 소멸
    ///   - LayerMask + 태그("Enemy") 이중 감지
    ///
    /// [S 키] 롤링 슬레쉬
    ///   - 3연속 앞구르기 전진
    ///   - 1회전당 → 가로 타원 참격 이펙트 1회 + 피해 판정
    ///   - LayerMask + 태그("Enemy") 이중 감지
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

        public bool IsRolling { get; private set; }

        private static readonly int AnimRollingSlash = Animator.StringToHash("RollingSlash");

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

            bool facingLeft = (_sr != null && _sr.flipX);
            Vector2 dir = facingLeft ? Vector2.left : Vector2.right;

            // 상현달(위 오프셋) + 하현달(아래 오프셋) 동시 발사
            SpawnCrescent(dir, yOffset:  _energy_VertOffset);
            SpawnCrescent(dir, yOffset: -_energy_VertOffset);

            yield return new WaitForSeconds(_energy_Cooldown);
            _canSkill1 = true;
        }

        private void SpawnCrescent(Vector2 dir, float yOffset)
        {
            Vector2 pos = (Vector2)transform.position + new Vector2(0f, yOffset);

            var go = new GameObject(yOffset > 0 ? "SwordEnergy_상현달" : "SwordEnergy_하현달");
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
        // ═════════════════════════════════════════════════════════════
        private IEnumerator Skill2_RollingSlash()
        {
            _canSkill2   = false;
            IsRolling    = true;
            _rollDirSign = (_sr != null && _sr.flipX) ? -1f : 1f;

            float moveSpeed = _roll_Distance / _roll_RollTime;

            _anim?.SetTrigger(AnimRollingSlash);

            var saved = _rb != null ? _rb.constraints : RigidbodyConstraints2D.FreezeRotation;
            if (_rb != null)
                _rb.constraints = RigidbodyConstraints2D.None;

            var alreadyHit = new HashSet<Collider2D>();

            for (int roll = 0; roll < 3; roll++)
            {
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

                Vector2 center = transform.position;
                SpawnSlashVFX(center);
                ApplyOvalHit(center, alreadyHit);

                if (_rb != null) _rb.angularVelocity = 0f;
                yield return new WaitForSeconds(0.04f);
            }

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

        private void SpawnSlashVFX(Vector2 pos)
        {
            var go = new GameObject("RollingSlash_VFX");
            go.transform.position = pos;
            go.AddComponent<RollingSlashVisual>().Initialize(_roll_OvalSize, _rollDirSign);
        }

        private void ApplyOvalHit(Vector2 center, HashSet<Collider2D> alreadyHit)
        {
            // 1차: LayerMask 기반 감지
            if (_enemyLayer.value != 0)
            {
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

            // 2차 폴백: 태그 "Enemy" 기반 감지
            Collider2D[] allCols = Physics2D.OverlapCapsuleAll(
                center, _roll_OvalSize, CapsuleDirection2D.Horizontal, 0f);
            foreach (var col in allCols)
            {
                if (alreadyHit.Contains(col)) continue;
                if (!col.CompareTag("Enemy")) continue;
                alreadyHit.Add(col);
                col.GetComponent<EnemyStats>()?.TakeDamage(_roll_Damage);
            }
        }

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
