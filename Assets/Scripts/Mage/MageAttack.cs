using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 마법사 공격 시스템 (96px 버전)
    ///
    /// [X 1타] 근거리 마법 타격 — attack1 애니메이션 + 히트박스 + 파티클
    /// [X 2타] 마법 구체 발사  — attack2 애니메이션 + MagicOrb 생성
    ///
    /// 콤보: 1타 후 _comboClearTime 내 X 재입력 → 2타
    ///       1타 모션 중 X 입력 예약 지원
    /// </summary>
    public class MageAttack : MonoBehaviour
    {
        // ── 공격1 ──────────────────────────────────────────────────────
        [Header("공격1 — 근거리")]
        [SerializeField] private float   _atk1Damage   = 14f;
        [SerializeField] private float   _atk1Duration = 0.35f;
        [SerializeField] private Vector2 _atk1BoxSize  = new Vector2(1.5f, 1.0f);
        [SerializeField] private Vector2 _atk1BoxOff   = new Vector2(0.75f, 0.1f);

        // ── 공격2 ──────────────────────────────────────────────────────
        [Header("공격2 — 마법 구체")]
        [SerializeField] private float _atk2Damage   = 25f;
        [SerializeField] private float _atk2Speed    = 10f;
        [SerializeField] private float _atk2Distance = 18f;
        [SerializeField] private float _atk2Duration = 0.40f;

        // ── 공통 ───────────────────────────────────────────────────────
        [Header("공통")]
        [SerializeField] private float     _comboClearTime = 0.55f;
        [SerializeField] private float     _cooldown       = 0.8f;
        [SerializeField] private LayerMask _enemyLayer;

        // ── 컴포넌트 ───────────────────────────────────────────────────
        private Animator       _anim;
        private SpriteRenderer _sr;

        // ── 상태 ───────────────────────────────────────────────────────
        private int   _combo       = 0;
        private bool  _attacking   = false;
        private bool  _canAttack   = true;
        private float _comboTimer  = 0f;
        private bool  _queued      = false;

        private static readonly int HashAtk1 = Animator.StringToHash("Attack1");
        private static readonly int HashAtk2 = Animator.StringToHash("Attack2");

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _sr   = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!_canAttack) return;

            // 콤보 타이머
            if (_combo == 1)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f) _combo = 0;
            }

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.xKey.wasPressedThisFrame)
            {
                if (_attacking && _combo == 1)      // 1타 중 예약
                    _queued = true;
                else if (!_attacking)
                {
                    if (_combo == 1) StartCoroutine(Atk2());
                    else             StartCoroutine(Atk1());
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  공격1
        // ═══════════════════════════════════════════════════════════════
        private IEnumerator Atk1()
        {
            _attacking = true;
            _queued    = false;
            TryTrigger(HashAtk1);

            yield return new WaitForSeconds(_atk1Duration * 0.3f);
            MeleeHit();

            yield return new WaitForSeconds(_atk1Duration * 0.7f);
            _combo      = 1;
            _comboTimer = _comboClearTime;
            _attacking  = false;

            if (_queued) { _queued = false; StartCoroutine(Atk2()); }
        }

        // ═══════════════════════════════════════════════════════════════
        //  공격2
        // ═══════════════════════════════════════════════════════════════
        private IEnumerator Atk2()
        {
            _attacking = true;
            _combo     = 0;
            _canAttack = false;
            TryTrigger(HashAtk2);

            yield return new WaitForSeconds(_atk2Duration * 0.4f);
            FireOrb();

            yield return new WaitForSeconds(_atk2Duration * 0.6f);
            _attacking = false;

            yield return new WaitForSeconds(_cooldown);
            _canAttack = true;
        }

        // ── 근거리 히트박스 ────────────────────────────────────────────
        private void MeleeHit()
        {
            float   dir = FacingDir();
            Vector2 ctr = (Vector2)transform.position
                        + new Vector2(_atk1BoxOff.x * dir, _atk1BoxOff.y);

            SpawnMeleeVFX(ctr, dir);

            // LayerMask
            if (_enemyLayer.value != 0)
            {
                foreach (var h in Physics2D.OverlapBoxAll(ctr, _atk1BoxSize, 0f, _enemyLayer))
                    h.GetComponent<EnemyStats>()?.TakeDamage(_atk1Damage);
            }
            // 태그 폴백
            foreach (var h in Physics2D.OverlapBoxAll(ctr, _atk1BoxSize, 0f))
                if (h.CompareTag("Enemy"))
                    h.GetComponent<EnemyStats>()?.TakeDamage(_atk1Damage);
        }

        // ── 마법 구체 발사 ─────────────────────────────────────────────
        private void FireOrb()
        {
            float   dir      = FacingDir();
            Vector2 spawnPos = (Vector2)transform.position + new Vector2(dir * 0.5f, 0.15f);

            var go = new GameObject("MagicOrb");
            go.transform.position   = spawnPos;
            go.transform.localScale = Vector3.one * 0.4f;
            go.layer = gameObject.layer;

            // Trail
            var trail          = go.AddComponent<TrailRenderer>();
            trail.time         = 0.12f;
            trail.startWidth   = 0.3f;
            trail.endWidth     = 0.02f;
            trail.startColor   = new Color(0.5f, 0.9f, 1f, 0.9f);
            trail.endColor     = new Color(0.8f, 0.3f, 1f, 0f);
            trail.sortingOrder = 4;
            trail.generateLightingData = false;

            var orb          = go.AddComponent<MagicOrb>();
            orb.damage       = _atk2Damage;
            orb.speed        = _atk2Speed;
            orb.maxDistance  = _atk2Distance;
            orb.enemyLayer   = _enemyLayer;
            orb.orbColor     = new Color(0.4f, 0.85f, 1f, 1f);
            orb.Launch(new Vector2(dir, 0f));

            SpawnFireVFX(spawnPos);
        }

        // ── 근접 파티클 VFX ────────────────────────────────────────────
        private void SpawnMeleeVFX(Vector2 pos, float dir)
        {
            var root = new GameObject("MageAtk1_VFX");
            root.transform.position = pos;
            SpawnBurst(root, new Color(0.5f, 0.85f, 1f), 14, 0.3f, 5f, 0.08f, 0.22f, 6, dir);
            SpawnBurst(root, new Color(1f, 0.5f, 1f),     8, 0.2f, 7f, 0.05f, 0.14f, 5, dir);
            Destroy(root, 0.7f);
        }

        // ── 발사 VFX ─────────────────────────────────────────────────
        private void SpawnFireVFX(Vector2 pos)
        {
            var root = new GameObject("MageAtk2_VFX");
            root.transform.position = pos;
            SpawnBurst(root, new Color(0.3f, 0.7f, 1f), 8, 0.2f, 3f, 0.06f, 0.14f, 6, 0f);
            Destroy(root, 0.5f);
        }

        private static void SpawnBurst(GameObject parent, Color col,
            int count, float life, float spd, float sMin, float sMax, int order, float dirX = 0f)
        {
            var go = new GameObject("B");
            go.transform.SetParent(parent.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var m = ps.main;
            m.loop          = false;
            m.duration      = 0.1f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            m.startSpeed    = new ParticleSystem.MinMaxCurve(1f, spd);
            m.startSize     = new ParticleSystem.MinMaxCurve(sMin, sMax);
            m.startColor    = col;
            m.maxParticles  = count + 4;
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle     = dirX != 0f ? 35f : 180f;
            sh.radius    = 0.1f;
            if (dirX != 0f) sh.rotation = new Vector3(0f, 0f, dirX > 0f ? 0f : 180f);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = order;
            ps.Play();
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        private float FacingDir() => (_sr != null && _sr.flipX) ? -1f : 1f;

        private void TryTrigger(int hash)
        {
            if (_anim == null) return;
            foreach (var p in _anim.parameters)
                if (p.nameHash == hash) { _anim.SetTrigger(hash); return; }
        }

        private void OnDrawGizmosSelected()
        {
            float   dir = FacingDir();
            Vector2 ctr = (Vector2)transform.position
                        + new Vector2(_atk1BoxOff.x * dir, _atk1BoxOff.y);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(ctr, _atk1BoxSize);
        }
    }
}
