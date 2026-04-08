using UnityEngine;
using _2D_Roguelike;

/// <summary>
/// 고정 방향으로 날아가는 투사체.
/// Setup의 maxCount / index를 이용해 360도를 균등 분배한 각도로 발사됩니다.
/// 예) maxCount=6, index=0~5 → 60도 간격 6방향
/// _startAngle로 패턴 전체를 회전시킬 수 있습니다 (Inspector).
/// </summary>
public class ProjectileDirectional : ProjectileBase
{
    [Tooltip("패턴의 시작 각도 (0 = 오른쪽, 90 = 위)")]
    [SerializeField] private float _startAngle = 90f;

    public override void Setup(Transform target, HitInfo info, int maxCount = 1, int index = 0)
    {
        base.Setup(target, info);

        float angle = _startAngle + (360f / maxCount) * index;
        Vector2 dir = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        movementRigidBody2D.MoveTo(dir);
    }

    public override void Process() { }
}
