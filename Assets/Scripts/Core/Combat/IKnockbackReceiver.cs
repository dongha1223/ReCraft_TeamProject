using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 넉백을 받을 수 있는 오브젝트 인터페이스.
    /// KnockbackReceiver 컴포넌트가 구현하며, Controller들은 IsKnockedBack으로 이동을 차단한다.
    /// </summary>
    public interface IKnockbackReceiver
    {
        bool   IsKnockedBack    { get; }
        Vector2 ExternalVelocity { get; }
        void ApplyKnockback(Vector2 sourcePosition, float force);
        void ResetKnockback();
    }
}
