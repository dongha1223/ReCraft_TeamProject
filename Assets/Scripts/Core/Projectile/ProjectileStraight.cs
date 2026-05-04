using UnityEngine;
using _2D_Roguelike;

public class ProjectileStraight : ProjectileBase
{
    public override void Setup(Transform target, HitInfo info, int maxCount = 1, int index = 0)
    {
        base.Setup(target, info);
        movementRigidBody2D.MoveTo((target.position - transform.position).normalized);
    }

    public override void Setup(Vector3 targetPosition, HitInfo info)
    {
        base.Setup(targetPosition, info);
        movementRigidBody2D.MoveTo((targetPosition - transform.position).normalized);
    }

    public override void Process() { }
}
