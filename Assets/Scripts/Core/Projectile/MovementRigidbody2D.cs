using UnityEngine;

public class MovementRigidBody2d : MonoBehaviour
{
    [SerializeField]
    private float       moveSpeed = 10f;
    private Rigidbody2D rigid2D;

    public float        MoveSpeed => moveSpeed;

    private void Awake()
    {
        rigid2D = GetComponent<Rigidbody2D>();
    }

    public void MoveTo(Vector3 direction)
    {
        rigid2D.linearVelocity = direction * moveSpeed;
    }

    private void OnDisable()
    {
        if (rigid2D != null) rigid2D.linearVelocity = Vector2.zero;
    }
}
