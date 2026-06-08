using UnityEngine;
using _2D_Roguelike;

/// <summary>
/// 보스 전용 투사체.
/// 기록된 목표 좌표를 향해 직선 이동하며, 플레이어 충돌 시 데미지를 준다.
/// Phase 1: 바닥/플랫폼 충돌 시 소멸.
/// Phase 2: 바닥/플랫폼 충돌 시 장판(AreaZoneActor)을 생성하고 소멸.
/// </summary>
public class BossProjectile : ProjectileBase
{
    [Header("Phase 2 — 장판")]
    [Tooltip("Phase 2에서 충돌 시 생성할 장판 프리팹 (AreaZoneActor 포함)")]
    [SerializeField] private GameObject    _damageZonePrefab;
    [Tooltip("장판에 적용할 AreaSkillSpec (지속 시간·데미지·반경 등 설정)")]
    [SerializeField] private AreaSkillSpec _damageZoneSpec;

    [Header("충돌 레이어")]
    [Tooltip("바닥/플랫폼 레이어 — 이 레이어에 닿으면 Phase에 따라 소멸 또는 장판 생성")]
    [SerializeField] private LayerMask _groundLayer;

    private bool _isPhase2Mode;

    // ── 공개 API ──────────────────────────────────────────────────────────

    /// <summary>발사 전 BossPatternExecutor가 호출해 Phase 2 동작 여부를 설정한다.</summary>
    public void SetPhase2Mode(bool enabled) => _isPhase2Mode = enabled;

    /// <summary>장판 스폰 시 BossPatternExecutor가 추적 목록에 등록할 수 있도록 콜백 설정.</summary>
    public System.Action<GameObject> OnZoneSpawned;

    // ── ProjectileBase 구현 ───────────────────────────────────────────────

    public override void Setup(Vector3 targetPosition, HitInfo info)
    {
        base.Setup(targetPosition, info);
        // 목표 위치를 향해 직선 이동, 스프라이트를 진행 방향으로 회전
        Vector3 dir = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        movementRigidBody2D.MoveTo(dir);
    }

    public override void Process() { }  // 이동은 MovementRigidBody2D가 처리

    // ── 충돌 처리 ─────────────────────────────────────────────────────────

    protected override void OnTriggerEnter2D(Collider2D col)
    {
        // 플레이어 충돌 → 기본 데미지 처리 (무적 체크 포함)
        if (col.CompareTag("Player"))
        {
            base.OnHit(col);
            return;
        }

        // 바닥/플랫폼 충돌 → Phase에 따라 처리
        if ((_groundLayer.value & (1 << col.gameObject.layer)) != 0)
            HandleGroundHit();
    }

    private void HandleGroundHit()
    {
        if (_isPhase2Mode)
            SpawnDamageZone();

        OnLifetimeExpired();
    }

    private void SpawnDamageZone()
    {
        if (_damageZonePrefab == null || _damageZoneSpec == null) return;

        var zone = Object.Instantiate(_damageZonePrefab, transform.position, Quaternion.identity);
        var actor = zone.GetComponent<AreaZoneActor>();
        actor?.Initialize(null, _damageZoneSpec);
        OnZoneSpawned?.Invoke(zone);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_damageZoneSpec == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _damageZoneSpec.Radius);
    }
#endif
}
