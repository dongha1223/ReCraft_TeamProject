using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어 스킬 컨트롤러
    ///
    /// [A 키] 검기 발산 — 바라보는 방향으로 상·하현달 투사체 2발
    /// [S 키] 롤링 슬레쉬 — 앞구르기 3회
    ///
    /// ★ 롤링 회전 방식 개선
    ///   - Rigidbody2D constraints 는 절대 변경하지 않음
    ///   - transform.rotation을 코루틴에서 직접 보간 제어
    ///   - 1회 구르기 = 360° 회전 후 반드시 0°(정자세)로 복귀
    /// </summary>
    public class PlayerSkill : MonoBehaviour
    {
        // ── 검기 발산 ─────────────────────────────────────────────────
        [Header("검기 발산 (A 키)")]
        [SerializeField] private float     _energy_Damage      = 30f;
        [SerializeField] private float     _energy_Speed       = 10f;
        [SerializeField] private float     _energy_MaxDistance = 14f;
        [SerializeField] private float     _energy_Cooldown    = 1.5f;
        [SerializeField] private float     _energy_VertOffset  = 0.25f;
        [SerializeField] private LayerMask _enemyLayer;

        // ── 롤링 슬레쉬 ──────────────────────────────────────────────
        [Header("롤링 슬레쉬 (S 키)")]
        [SerializeField] private float   _roll_Damage    = 25f;
        [Tooltip("1회 구르기당 전진 거리")]
        [SerializeField] private float   _roll_Distance  = 1.1f;
        [Tooltip("1회 구르기 소요 시간 (초)")]
        [SerializeField] private float   _roll_RollTime  = 0.22f;
        [SerializeField] private float   _roll_Cooldown  = 2.2f;
        [Tooltip("가로 타원 크기 (width > height)")]
        [SerializeField] private Vector2 _roll_OvalSize  = new Vector2(2.6f, 1.0f);

        // ── 컴포넌트 ─────────────────────────────────────────────────
        private SpriteRenderer _sr;
        private Rigidbody2D    _rb;
        private Animator       _anim;

        // ── 상태 ─────────────────────────────────────────────────────
        private bool  _canSkill1   = true;
        private bool  _canSkill2   = true;
        private float _rollDirSign = 1f;

        public bool IsRolling { get; private set; }

        /// <summary>0 = 사용 가능, 1 = 방금 사용(쿨타임 시작), 사이값 = 남은 비율</summary>
        public float Skill1CooldownRatio { get; private set; } = 0f;
        public float Skill2CooldownRatio { get; private set; } = 0f;

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
        //  스킬 1 — 검기 발산
        // ═════════════════════════════════════════════════════════════
        private IEnumerator Skill1_SwordEnergy()
        {
            _canSkill1 = false;

            bool    facingLeft = (_sr != null && _sr.flipX);
            Vector2 dir        = facingLeft ? Vector2.left : Vector2.right;

            SpawnCrescent(dir,  _energy_VertOffset);
            SpawnCrescent(dir, -_energy_VertOffset);

            // 쿨타임 진행 (UI 비율 갱신)
            float elapsed = 0f;
            while (elapsed < _energy_Cooldown)
            {
                elapsed += Time.deltaTime;
                Skill1CooldownRatio = 1f - Mathf.Clamp01(elapsed / _energy_Cooldown);
                yield return null;
            }
            Skill1CooldownRatio = 0f;
            _canSkill1 = true;
        }

        private void SpawnCrescent(Vector2 dir, float yOffset)
        {
            Vector2 pos      = (Vector2)transform.position + new Vector2(0f, yOffset);
            bool    fr       = (dir.x > 0f);

            SwordEnergyProjectile p;
            if (SkillObjectPool.Instance != null)
            {
                p = SkillObjectPool.Instance.GetProjectile(fr, pos);
            }
            else
            {
                // 풀이 없는 경우 폴백
                var go = new GameObject(yOffset > 0 ? "SwordEnergy_상현달" : "SwordEnergy_하현달");
                go.transform.position = pos;
                p = go.AddComponent<SwordEnergyProjectile>();
                p.Setup(fr);
            }

            p.damage      = _energy_Damage;
            p.speed       = _energy_Speed;
            p.maxDistance = _energy_MaxDistance;
            p.enemyLayer  = _enemyLayer;
            p.Launch(dir);
        }

        // ═════════════════════════════════════════════════════════════
        //  스킬 2 — 롤링 슬레쉬
        //  Rigidbody 회전 완전 분리: transform.eulerAngles.z만 직접 제어
        //  각 구르기: 0° → 360°(방향 부호 포함) → snap to 0°
        // ═════════════════════════════════════════════════════════════
        private IEnumerator Skill2_RollingSlash()
        {
            _canSkill2   = false;
            IsRolling    = true;
            _rollDirSign = (_sr != null && _sr.flipX) ? -1f : 1f;

            float moveSpeed = _roll_Distance / _roll_RollTime;

            SafeAnimTrigger(_anim, AnimRollingSlash);

            var alreadyHit = new HashSet<Collider2D>();

            for (int roll = 0; roll < 3; roll++)
            {
                // ── 1회 구르기: 이동 + 360° 회전 ──────────────────────
                yield return StartCoroutine(SingleRoll(moveSpeed, alreadyHit));

                // ── 판정 & 이펙트 ──────────────────────────────────────
                Vector2 center = transform.position;
                SpawnSlashVFX(center);
                ApplyOvalHit(center, alreadyHit);

                // 구르기 사이 짧은 인터벌
                yield return new WaitForSeconds(0.04f);
            }

            // ── 완전 종료: 수평 속도 멈추고 정자세 확정 ──────────────
            if (_rb != null)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

            // 혹시 남은 각도 오차 완전 제거
            transform.rotation = Quaternion.identity;

            IsRolling = false;

            // 쿨타임 진행 (UI 비율 갱신)
            float elapsed = 0f;
            while (elapsed < _roll_Cooldown)
            {
                elapsed += Time.deltaTime;
                Skill2CooldownRatio = 1f - Mathf.Clamp01(elapsed / _roll_Cooldown);
                yield return null;
            }
            Skill2CooldownRatio = 0f;
            _canSkill2 = true;
        }

        /// <summary>
        /// 1회 구르기: _roll_RollTime 동안 전진 + Z축 360° 회전
        /// 종료 시 transform.rotation을 Quaternion.identity로 snap
        /// </summary>
        private IEnumerator SingleRoll(float moveSpeed, HashSet<Collider2D> alreadyHit)
        {
            float elapsed   = 0f;
            float totalTime = _roll_RollTime;

            // 회전 방향: 앞구르기 = 진행 방향 반대로 회전(자연스러운 텀블링)
            // 오른쪽 이동 → 시계방향(Z 감소), 왼쪽 → 반시계(Z 증가)
            float rotDir = -_rollDirSign;   // Unity Z축: + = 반시계

            while (elapsed < totalTime)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // 전진
                if (_rb != null)
                    _rb.linearVelocity = new Vector2(moveSpeed * _rollDirSign,
                                                     _rb.linearVelocity.y);

                // 회전: 경과 비율에 맞춰 Z각도를 직접 설정 (360° * 비율)
                float t       = Mathf.Clamp01(elapsed / totalTime);
                float zAngle  = rotDir * 360f * t;
                transform.rotation = Quaternion.Euler(0f, 0f, zAngle);

                yield return null;
            }

            // ★ 1회 구르기 끝 → 반드시 정자세(0°)로 스냅
            transform.rotation = Quaternion.identity;

            // 전진 속도도 일시 정지 (다음 구르기 전 안정화)
            if (_rb != null)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        // ── VFX & 피해 ────────────────────────────────────────────────
        private void SpawnSlashVFX(Vector2 pos)
        {
            RollingSlashVisual v;
            if (SkillObjectPool.Instance != null)
            {
                v = SkillObjectPool.Instance.GetSlashVFX(pos);
            }
            else
            {
                // 풀이 없는 경우 폴백
                var go = new GameObject("RollingSlash_VFX");
                go.transform.position = pos;
                v = go.AddComponent<RollingSlashVisual>();
            }
            v.Initialize(_roll_OvalSize, _rollDirSign);
        }

        private void ApplyOvalHit(Vector2 center, HashSet<Collider2D> alreadyHit)
        {
            // 1차: LayerMask
            if (_enemyLayer.value != 0)
            {
                Collider2D[] hits = Physics2D.OverlapCapsuleAll(
                    center, _roll_OvalSize, CapsuleDirection2D.Horizontal, 0f, _enemyLayer);
                foreach (var col in hits)
                {
                    if (alreadyHit.Contains(col)) continue;
                    alreadyHit.Add(col);
                    col.GetComponent<EnemyStats>()?.TakeDamage(_roll_Damage);
                }
            }

            // 2차: 태그 "Enemy" 폴백
            Collider2D[] all = Physics2D.OverlapCapsuleAll(
                center, _roll_OvalSize, CapsuleDirection2D.Horizontal, 0f);
            foreach (var col in all)
            {
                if (alreadyHit.Contains(col)) continue;
                if (!col.CompareTag("Enemy")) continue;
                alreadyHit.Add(col);
                col.GetComponent<EnemyStats>()?.TakeDamage(_roll_Damage);
            }
        }

        // ── 기즈모 ───────────────────────────────────────────────────
        // ── 안전한 Animator 트리거 ─────────────────────────────────────────
        /// <summary>애니메이터 파라미터가 없어도 에러 없이 무시</summary>
        private static void SafeAnimTrigger(Animator anim, int hash)
        {
            if (anim == null) return;
            foreach (var p in anim.parameters)
                if (p.nameHash == hash) { anim.SetTrigger(hash); return; }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.4f);
            const int seg = 32;
            float rx = _roll_OvalSize.x * 0.5f, ry = _roll_OvalSize.y * 0.5f;
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
