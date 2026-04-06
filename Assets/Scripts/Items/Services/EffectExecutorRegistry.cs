using System;
using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// EffectDefinition 타입 → IEffectExecutor 매핑 레지스트리.
    /// 새 효과 타입이 추가될 때마다 Register를 한 번만 호출하면 된다.
    /// </summary>
    public class EffectExecutorRegistry
    {
        private readonly Dictionary<Type, IEffectExecutor> _executors = new();

        public void Register<T>(IEffectExecutor executor) where T : EffectDefinition
        {
            _executors[typeof(T)] = executor;
        }

        public IEffectExecutor GetExecutor(EffectDefinition definition)
        {
            var type = definition.GetType();
            if (_executors.TryGetValue(type, out var executor))
                return executor;

            throw new InvalidOperationException(
                $"[EffectExecutorRegistry] '{type.Name}'에 대한 실행기가 등록되지 않았습니다.");
        }
    }
}
