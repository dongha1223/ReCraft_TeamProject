using System;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 상태이상 적용 데이터.
    /// HitInfo에 포함되어 공격 시 어떤 상태이상을 줄지 정의한다.
    /// </summary>
    [Serializable]
    public class StatusEffectSpec
    {
        [Tooltip("적용할 상태이상 타입")]
        public StatusEffectType effectType;

        [Tooltip("지속시간 (초)")]
        public float duration;

        [Tooltip("틱당 데미지 (Burn/Bleed/Poison 전용)")]
        public float tickDamage;

        [Tooltip("틱 주기 (초, Burn/Bleed/Poison 전용)")]
        public float tickInterval = 0.5f;

        [Tooltip("적용 확률 (0 ~ 1, 1 = 100%)")]
        [Range(0f, 1f)]
        public float chance = 1f;
    }
}
