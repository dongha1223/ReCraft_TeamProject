using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 단일 공격이 가진 효과 데이터.
    /// 공격자는 이 구조체를 채워서 IDamageable.TakeDamage()에 전달한다.
    /// </summary>
    public struct HitInfo
    {
        /// <summary>입힐 데미지량</summary>
        public float Damage;

        /// <summary>공격 발생 위치 (넉백 방향 계산용)</summary>
        public Vector2 SourcePosition;

        /// <summary>넉백 강도. 0이면 넉백 없음. 감쇠는 KnockbackReceiver가 자동 처리</summary>
        public float KnockbackForce;
    }
}
