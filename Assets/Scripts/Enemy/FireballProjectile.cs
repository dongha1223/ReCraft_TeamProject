using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 최종 페이즈 화염구 액터.
    ///
    /// Falling  → FixedUpdate에서 rb.MovePosition으로 낙하 (트리거 정상 감지)
    ///   ├─ Player 충돌  → 40 데미지 후 즉시 제거
    ///   └─ Ground 충돌  → Y 0.4 보정 후 Debris 전환 (Platform은 통과)
    ///
    /// Debris   → 이동 정지, 스프라이트 15-33 재생 후 틱뎀 → 4초 후 제거
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FireballProjectile : MonoBehaviour
    {
        private enum State { Falling, Debris, Dead }

        private State          _state = State.Falling;
        private Rigidbody2D    _rb;
        private BoxCollider2D  _col;
        private SpriteRenderer _renderer;

        private Sprite[] _fallSprites;
        private Sprite[] _impactSprites;
        private float    _fallSpeed;
        private float    _hitDamage;
        private float    _debrisDamage;
        private float    _debrisDuration;
        private float    _debrisTickInterval;
        private float    _debrisWidth;
        private float    _debrisHeight;
        private float    _destroyBelowY;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[1];
        private int     _groundLayer;
        private int     _playerMask;
        private Vector2 _fallingColSize;  // 낙하 중 콜라이더 월드 크기 캐시
        private Vector2 _debrisCheckSize; // 잔해 틱 OverlapBox 크기 캐시

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _col      = GetComponent<BoxCollider2D>();
            _renderer = GetComponent<SpriteRenderer>();

            _rb.bodyType     = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _col.isTrigger   = true;

            _groundLayer = LayerMask.NameToLayer("Ground");
            _playerMask  = 1 << LayerMask.NameToLayer("Player");

            // 스케일 반영한 월드 콜라이더 크기 미리 계산
            var s = transform.lossyScale;
            _fallingColSize = new Vector2(_col.size.x * s.x, _col.size.y * s.y);
        }

        // ── 외부 API ──────────────────────────────────────────────────────

        public void Launch(
            Sprite[] fallSprites,   Sprite[] impactSprites,
            float    fallSpeed,     float    hitDamage,
            float    debrisDamage,  float    debrisDuration,
            float    debrisTickInterval,
            float    debrisWidth,   float    debrisHeight,
            float    destroyBelowY)
        {
            _fallSprites        = fallSprites;
            _impactSprites      = impactSprites;
            _fallSpeed          = fallSpeed;
            _hitDamage          = hitDamage;
            _debrisDamage       = debrisDamage;
            _debrisDuration     = debrisDuration;
            _debrisTickInterval = debrisTickInterval;
            _debrisWidth        = debrisWidth;
            _debrisHeight       = debrisHeight;
            _destroyBelowY      = destroyBelowY;

            // 잔해 틱 OverlapBox 크기 미리 계산 (매 틱 할당 방지)
            _debrisCheckSize = new Vector2(debrisWidth + 0.4f, debrisHeight + 0.5f);

            StartCoroutine(FallAnimation());
        }

        // ── 낙하 이동 + 플레이어 감지 ────────────────────────────────────
        // Kinematic vs Dynamic RB2D는 OnTriggerEnter2D 콜백이 기본 미발생.
        // OverlapBoxNonAlloc으로 직접 플레이어를 체크해 확실히 감지한다.

        private void FixedUpdate()
        {
            if (_state != State.Falling) return;

            _rb.MovePosition(_rb.position + Vector2.down * (_fallSpeed * Time.fixedDeltaTime));

            if (_rb.position.y < _destroyBelowY)
            {
                _state = State.Dead;
                Destroy(gameObject);
                return;
            }

            // 플레이어 직격 감지
            int count = Physics2D.OverlapBoxNonAlloc(
                _rb.position, _fallingColSize, 0f, _overlapBuffer, _playerMask);

            if (count > 0 && _overlapBuffer[0] != null)
                HitPlayer(_overlapBuffer[0]);
        }

        // ── 낙하 애니메이션 ───────────────────────────────────────────────

        private IEnumerator FallAnimation()
        {
            if (_fallSprites == null || _fallSprites.Length == 0) yield break;

            const float frameDuration = 0.07f;
            var wait  = new WaitForSeconds(frameDuration);
            int len   = _fallSprites.Length;
            int idx   = 0;
            while (_state == State.Falling)
            {
                _renderer.sprite = _fallSprites[idx];
                if (++idx >= len) idx = 0;
                yield return wait;
            }
        }

        // ── 충돌 감지 (Ground만 — 플레이어는 FixedUpdate OverlapBox로 처리) ──

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != State.Falling) return;

            // Platform은 통과, Ground에만 착지
            if (other.gameObject.layer == _groundLayer)
                SwitchToDebris();
        }

        private void HitPlayer(Collider2D other)
        {
            _state = State.Dead;
            if (other.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(new HitInfo
                {
                    AttackId       = AttackIdGenerator.Next(),
                    Damage         = _hitDamage,
                    DamageType     = DamageType.Physical,
                    SourcePosition = transform.position,
                    KnockbackForce = 0f,
                });
            }
            Destroy(gameObject);
        }

        // ── 잔해 전환 ─────────────────────────────────────────────────────

        private void SwitchToDebris()
        {
            _state = State.Debris;

            // 착지 시 Y를 0.4 낮춰 바닥에 박힌 느낌
            var pos = transform.position;
            pos.y -= 0.4f;
            transform.position = pos;

            StartCoroutine(DebrisAnimation());
            StartCoroutine(DebrisTickLoop());
        }

        private IEnumerator DebrisAnimation()
        {
            _col.size   = new Vector2(_debrisWidth, _debrisHeight);
            _col.offset = Vector2.zero;

            if (_impactSprites == null || _impactSprites.Length == 0) yield break;

            const float frameDuration = 0.05f;
            var wait = new WaitForSeconds(frameDuration);
            foreach (var spr in _impactSprites)
            {
                if (_state == State.Dead) yield break;
                _renderer.sprite = spr;
                yield return wait;
            }
            _renderer.sprite = _impactSprites[_impactSprites.Length - 1];
        }

        private IEnumerator DebrisTickLoop()
        {
            var   tickWait    = new WaitForSeconds(_debrisTickInterval);
            // Debris는 이동하지 않으므로 위치를 한 번만 읽어 캐시
            var   debrisPos   = (Vector2)transform.position;
            float elapsed     = 0f;

            while (elapsed < _debrisDuration)
            {
                yield return tickWait;
                if (_state == State.Dead) yield break;
                elapsed += _debrisTickInterval;

                int count = Physics2D.OverlapBoxNonAlloc(
                    debrisPos, _debrisCheckSize, 0f, _overlapBuffer, _playerMask);

                if (count > 0 && _overlapBuffer[0] != null &&
                    _overlapBuffer[0].TryGetComponent<IDamageable>(out var t))
                {
                    t.TakeDamage(new HitInfo
                    {
                        AttackId       = AttackIdGenerator.Next(),
                        Damage         = _debrisDamage,
                        DamageType     = DamageType.Magic,
                        SourcePosition = debrisPos,
                        KnockbackForce = 0f,
                    });
                }
            }

            _state = State.Dead;
            Destroy(gameObject);
        }
    }
}
