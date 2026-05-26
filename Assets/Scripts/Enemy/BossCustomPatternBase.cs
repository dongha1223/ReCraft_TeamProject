using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Animator 명령 외에 코드 로직이 필요한 보스 패턴의 기반 클래스.
    /// BossPattern.patternName 이 PatternId 와 일치할 때 BossMainController 가 호출한다.
    /// </summary>
    public abstract class BossCustomPatternBase : MonoBehaviour
    {
        public abstract string PatternId { get; }

        private Coroutine           _handle;
        private BossMainController  _pauseCtrl;

        // BossMainController.IsFrozen 체크 (포효·컷신 등 일시정지 여부)
        protected bool IsBossFrozen
        {
            get
            {
                if (_pauseCtrl == null) _pauseCtrl = GetComponent<BossMainController>();
                return _pauseCtrl != null && _pauseCtrl.IsFrozen;
            }
        }

        // 포효·컷신 등 BossMainController.IsFrozen 구간에서 시간이 멈추는 대기
        protected IEnumerator PauseableWait(float duration)
        {
            if (_pauseCtrl == null) _pauseCtrl = GetComponent<BossMainController>();
            bool hasPauseCtrl = _pauseCtrl != null;  // null 체크를 루프 밖으로 캐시
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!hasPauseCtrl || !_pauseCtrl.IsFrozen)
                    elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>패턴 실행을 시작하고 Coroutine 핸들을 반환한다.</summary>
        public Coroutine BeginExecute()
        {
            _handle = StartCoroutine(ExecuteRoutine());
            return _handle;
        }

        /// <summary>보스 사망 등으로 패턴을 강제 종료할 때 호출한다.</summary>
        public void Cancel()
        {
            if (_handle != null)
            {
                StopCoroutine(_handle);
                _handle = null;
            }
            OnCancel();
        }

        protected abstract IEnumerator ExecuteRoutine();

        /// <summary>Cancel 호출 시 자원 정리를 위해 하위 클래스가 override한다.</summary>
        protected virtual void OnCancel() { }
    }
}
