using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검기 발산 투사체.
    /// - ProjectileBase 상속: 레이어 필터(_hitLayers), 수명/거리 관리(_maxDistance) Inspector 설정
    /// - 비주얼: Inspector에서 스프라이트 직접 할당 (런타임 메시/머티리얼 생성 없음)
    /// - 이동: MovementRigidBody2D (속도는 Inspector의 moveSpeed)
    /// - 풀링: SkillObjectPool 기반, OnLifetimeExpired에서 풀 반환
    /// </summary>
    public class SwordEnergyProjectile : ProjectileBase
    {
        [Header("넉백")]
        [SerializeField] private float _knockbackForce = 4f;

        private float                    _damage;
        private bool                     _isReturning;
        private readonly HashSet<Collider2D> _hit = new HashSet<Collider2D>();

        protected override void OnEnable()
        {
            base.OnEnable();
            _isReturning = false;
            _hit.Clear();
        }

        /// <summary>
        /// 풀에서 꺼낸 뒤 PlayerSkill에서 호출.
        /// damage는 런타임 값, 속도/_maxDistance/_hitLayers는 Inspector(프리팹) 설정.
        /// </summary>
        public void Launch(Vector2 direction, float damage)
        {
            _damage = damage;

            // 진행 방향에 따라 오브젝트 전체 반전
            Vector3 scale = transform.localScale;
            scale.x = direction.x >= 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = -scale;

            movementRigidBody2D.MoveTo(direction.normalized);
        }

        public override void Process() { }

        protected override void OnHit(Collider2D col)
        {
            if (_hit.Contains(col)) return;

            var damageable = col.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;

            _hit.Add(col);
            damageable.TakeDamage(new HitInfo
            {
                Damage         = _damage,
                SourcePosition = transform.position,
                KnockbackForce = _knockbackForce
            });
            OnLifetimeExpired();
        }

        protected override void OnLifetimeExpired()
        {
            if (_isReturning) return;
            _isReturning = true;

            if (SkillObjectPool.Instance != null)
                SkillObjectPool.Instance.ReturnProjectile(this);
            else
                Destroy(gameObject);
        }
    }
}
