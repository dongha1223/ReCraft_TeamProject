namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어 전투 이벤트 허브.
    /// 피해·회복 등 전투 관련 이벤트를 구독·발행하는 정적 클래스.
    /// </summary>
    public static class PlayerCombatEvents
    {
        /// <summary>플레이어가 적에게 피해를 입힌 직후 발생. float = 실제 적용된 피해량.</summary>
        public static event System.Action<float> OnDamageDealt;

        public static void InvokeDamageDealt(float damage)
        {
            OnDamageDealt?.Invoke(damage);
        }
    }
}
