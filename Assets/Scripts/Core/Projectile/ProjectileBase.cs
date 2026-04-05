using UnityEngine;
using _2D_Roguelike;

public abstract class ProjectileBase : MonoBehaviour
{
    [SerializeField] private   GameObject     hitEffect;
    [SerializeField] protected LayerMask      _hitLayers;
    [SerializeField] private   float          _lifetime    = 0f;   // 0 = 비활성
    [SerializeField] private   float          _maxDistance = 0f;   // 0 = 비활성

    protected MovementRigidBody2d movementRigidBody2D;

    // HitInfo를 보관 — 서브클래스가 Setup에서 채우고, OnHit에서 IDamageable로 전달
    protected HitInfo _hitInfo;

    private float   _elapsed;
    private Vector3 _startPosition;

    protected virtual void Awake()
    {
        movementRigidBody2D = GetComponent<MovementRigidBody2d>();
    }

    // 풀에서 꺼내 SetActive(true) 될 때 자동 초기화
    protected virtual void OnEnable()
    {
        _elapsed       = 0f;
        _startPosition = transform.position;
    }

    public virtual void Setup(Transform target, HitInfo info, int maxCount = 1, int index = 0)
    {
        _hitInfo = info;
    }

    private void Update()
    {
        if (_lifetime > 0f)
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime) { OnLifetimeExpired(); return; }
        }

        if (_maxDistance > 0f)
        {
            if (Vector3.Distance(transform.position, _startPosition) >= _maxDistance)
            { OnDistanceExceeded(); return; }
        }

        Process();
    }

    public abstract void Process();

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        // _hitLayers가 설정된 경우에만 레이어 필터 적용
        if (_hitLayers.value != 0 && (_hitLayers.value & (1 << col.gameObject.layer)) == 0) return;
        OnHit(col);
    }

    /// <summary>
    /// 충돌 반응 — 서브클래스에서 override하여 구현.
    /// 기본 동작: PlayerHitBox 태그에 닿으면 IDamageable로 데미지 전달 후 소멸 (적 발사체용)
    /// </summary>
    protected virtual void OnHit(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        // PlayerHitBox가 자식 오브젝트일 수 있으므로 InParent로 탐색
        var damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        // 무적 중이고 관통 불가 공격이면 투사체가 통과 (소멸하지 않음)
        if (damageable.IsInvincible && !_hitInfo.IgnoreInvincibility) return;

        var info = _hitInfo;
        info.SourcePosition = transform.position;
        damageable.TakeDamage(info);

        if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
        OnLifetimeExpired();
    }

    /// <summary>수명 종료 (시간/거리/충돌). 풀링 오브젝트는 override하여 풀 반환 처리.</summary>
    protected virtual void OnLifetimeExpired()
    {
        Destroy(gameObject);
    }

    protected virtual void OnDistanceExceeded()
    {
        OnLifetimeExpired();
    }
}
