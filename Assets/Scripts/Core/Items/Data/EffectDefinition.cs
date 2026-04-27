using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 모든 효과 정의의 추상 베이스.
    /// 새 효과 타입은 이 클래스를 상속해 파생 ScriptableObject로 만든다.
    /// </summary>
    public abstract class EffectDefinition : ScriptableObject
    {
        public string        effectId;
        public EffectTrigger trigger;
    }
}
