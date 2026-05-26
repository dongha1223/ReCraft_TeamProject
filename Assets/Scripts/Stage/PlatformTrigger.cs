using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 각 Phase 2 플랫폼 오브젝트에 붙이는 컴포넌트.
    /// 플레이어가 플랫폼 위에 올라서면 PlatformManager에 현재 인덱스를 알린다.
    /// Collider2D는 IsTrigger = false (착지용 콜라이더 포함) + 별도 Trigger 콜라이더로 구성한다.
    /// </summary>
    public class PlatformTrigger : MonoBehaviour
    {
        [Tooltip("이 플랫폼의 인덱스 (PlatformManager._platforms 배열 기준)")]
        [SerializeField] private int _platformIndex;

        [Tooltip("PlatformManager 참조")]
        [SerializeField] private PlatformManager _platformManager;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.CompareTag("Player")) return;
            _platformManager?.SetCurrentPlatform(_platformIndex);
        }
    }
}
