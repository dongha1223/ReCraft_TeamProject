using _2D_Roguelike;
using UnityEngine;

public class ProjectileCubicHoming : ProjectileBase
{
    private Vector2   start, end, point1, point2;
    private float     duration, t;
    private Transform target;

    public override void Setup(Transform target, HitInfo info, int maxCount = 1, int index = 0)
    {
        base.Setup(target, info);

        this.target = target;
        t           = 0f;
        start       = transform.position;
        end         = target.position;

        float distance = Vector3.Distance(start, end);
        duration       = distance / movementRigidBody2D.MoveSpeed;

        float angle = 360f / maxCount * index + MathUtil.GetAngleFromPosition(start, end);
        point1 = MathUtil.GetNewPoint(start,  angle,      distance * 0.3f);
        point2 = MathUtil.GetNewPoint(start, -angle,      distance * 0.7f);
    }

    public override void Process()
    {
        if (target == null) { OnLifetimeExpired(); return; }
        if (t >= 1f)        { OnLifetimeExpired(); return; }

        end  = target.position;
        t   += Time.deltaTime / duration;
        transform.position = MathUtil.CubicCurve(start, point1, point2, end, Mathf.Clamp01(t));
    }
}
