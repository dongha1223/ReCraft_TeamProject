using System.Diagnostics;
using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [SerializeField]
    private     GameObject          hitEffect;
    protected   MovementRigidBody2d movementRigidBody2D;

    public virtual void Setup(Transform target, float damage, int maxCount = 1, int index = 0)
    {
        movementRigidBody2D = GetComponent<MovementRigidBody2d>();
    }

    private void Update()
    {
        Process();
    }

    public abstract void Process();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("PlayerHitBox"))
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);

            //피격처리
        }
    }
}
