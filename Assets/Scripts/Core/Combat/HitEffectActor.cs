using System;
using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 피격이펙트 프리팹에 부착. 스폰 후 duration 경과 시 풀 반환 콜백 호출.
    /// </summary>
    public class HitEffectActor : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.3f;

        private Action<HitEffectActor> _returnCallback;
        private Coroutine              _lifetimeCoroutine;

        public void Play(Action<HitEffectActor> returnCallback)
        {
            _returnCallback = returnCallback;

            if (_lifetimeCoroutine != null)
                StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(_duration);
            _returnCallback?.Invoke(this);
        }

        public void OnGetFromPool()
        {
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }
            gameObject.SetActive(false);
        }
    }
}
