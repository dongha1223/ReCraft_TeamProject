using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 불기둥 한 개의 동작을 담당한다.
    /// ① 경고(크레이터) 페이즈: _craterSprite 를 warningDuration 동안 alpha 0→1 로 페이드인.
    /// ② 불꽃 페이즈: _fireSprites 를 순서대로 재생하며 콜라이더를 활성화해 데미지를 적용한다.
    /// 풀(BossFirePillarPattern)에 의해 재사용된다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class FirePillarActor : MonoBehaviour
    {
        [SerializeField] private float _warningDuration = 3f;
        [SerializeField] private float _frameDuration   = 0.05f;
        [SerializeField] private float _damage          = 30f;

        private SpriteRenderer    _renderer;
        private CapsuleCollider2D _collider;
        private Coroutine         _sequence;
        private WaitForSeconds    _waitFrame;

        private readonly HashSet<Collider2D> _hitTargets = new();

        // ── 외부 주입 — BossFirePillarPattern.BuildPool 에서 Setup 호출 ──────
        private Sprite   _craterSprite;
        private Sprite[] _fireSprites;

        // ── 생명주기 ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _renderer         = GetComponent<SpriteRenderer>();
            _collider         = GetComponent<CapsuleCollider2D>();
            _collider.isTrigger = true;
            _waitFrame        = new WaitForSeconds(_frameDuration);
        }

        private void OnDisable()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }
            _hitTargets.Clear();
        }

        // ── 외부 API ─────────────────────────────────────────────────────────

        public void Setup(Sprite craterSprite, Sprite[] fireSprites)
        {
            _craterSprite = craterSprite;
            _fireSprites  = fireSprites;
        }

        public void Launch(Vector3 worldPos)
        {
            transform.position = worldPos;
            _hitTargets.Clear();
            _collider.enabled = false;

            if (_sequence != null) StopCoroutine(_sequence);
            gameObject.SetActive(true);
            _sequence = StartCoroutine(Sequence());
        }

        // ── 시퀀스 ───────────────────────────────────────────────────────────

        private IEnumerator Sequence()
        {
            // ① 경고 페이즈 — 크레이터 스프라이트 페이드인
            _renderer.sprite = _craterSprite;
            var c = Color.white;
            c.a = 0f;
            _renderer.color = c;

            float elapsed      = 0f;
            float invWarningDur = 1f / _warningDuration;
            while (elapsed < _warningDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed * invWarningDur);
                _renderer.color = c;
                yield return null;
            }

            // ② 불꽃 페이즈 — 콜라이더 활성화 후 스프라이트 재생
            c.a = 1f;
            _renderer.color = c;
            _collider.enabled = true;

            if (_fireSprites != null)
            {
                foreach (var sprite in _fireSprites)
                {
                    _renderer.sprite = sprite;
                    yield return _waitFrame;
                }
            }

            // ③ 종료 — 풀에 반환
            gameObject.SetActive(false);
            _sequence = null;
        }

        // ── 데미지 ───────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_collider.enabled)             return;
            if (!other.CompareTag("Player"))    return;
            if (!_hitTargets.Add(other))        return;   // 중복 타격 방지

            if (other.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(new HitInfo
                {
                    AttackId       = AttackIdGenerator.Next(),
                    Damage         = _damage,
                    DamageType     = DamageType.Physical,
                    SourcePosition = transform.position,
                    KnockbackForce = 0f,
                });
            }
        }
    }
}
