using UnityEngine;
using _2D_Roguelike;

public class ProjectileHoming : ProjectileBase
{
    private Transform target;

    public override void Setup(Transform target, HitInfo info, int maxCount = 1, int index = 0)
    {
        base.Setup(target, info);
        this.target = target;
    }

    public override void Process()
    {
        movementRigidBody2D.MoveTo((target.position - transform.position).normalized);
    }
}
