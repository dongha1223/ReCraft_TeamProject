using UnityEngine;

/// <summary>
/// 일정 시간 동안만 플레이어를 추적하고, 이후 마지막 방향으로 직진하는 투사체.
/// homingDuration 경과 후에는 MoveTo 호출을 중단 → Rigidbody2D가 마지막 velocity 유지.
/// </summary>
public class ProjectileHomingTimed : ProjectileBase
{
    [SerializeField] private float _homingDuration = 1.5f;

    private Transform _target;
    private float     _homingElapsed;

    protected override void OnEnable()
    {
        base.OnEnable();
        _homingElapsed = 0f;
    }

    public override void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        base.Setup(target, damage);
        _target        = target;
        _homingElapsed = 0f;

        // 발사 즉시 초기 방향으로 이동 시작
        if (_target != null)
            movementRigidBody2D.MoveTo((_target.position - transform.position).normalized);
    }

    public override void Process()
    {
        if (_target == null || _homingElapsed >= _homingDuration) return;

        _homingElapsed += Time.deltaTime;
        movementRigidBody2D.MoveTo((_target.position - transform.position).normalized);
    }
}
