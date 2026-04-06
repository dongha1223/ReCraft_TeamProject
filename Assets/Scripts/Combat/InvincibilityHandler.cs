using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 무적 상태 관리 컴포넌트.
    /// 여러 소스(대시, 스킬, 아이템 등)가 독립적으로 무적을 요청할 수 있으며,
    /// 하나라도 활성 상태이면 IsInvincible이 true를 반환한다.
    /// </summary>
    public class InvincibilityHandler : MonoBehaviour
    {
        // 활성 무적 소스 수 — 0 초과면 무적
        private int _activeCount;

        public bool IsInvincible => _activeCount > 0;

        /// <summary>무적 시작 (duration초 후 자동 해제)</summary>
        public void SetInvincible(float duration)
        {
            StartCoroutine(InvincibleCoroutine(duration));
        }

        /// <summary>수동 무적 시작 — 반드시 Exit()와 쌍으로 호출</summary>
        public void Enter() => _activeCount++;

        /// <summary>수동 무적 해제</summary>
        public void Exit() => _activeCount = Mathf.Max(0, _activeCount - 1);

        /// <summary>풀 반환 등 강제 초기화 시 호출</summary>
        public void ResetInvincibility() => _activeCount = 0;

        private IEnumerator InvincibleCoroutine(float duration)
        {
            Enter();
            yield return new WaitForSeconds(duration);
            Exit();
        }
    }
}
