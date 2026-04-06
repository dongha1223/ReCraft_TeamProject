using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어에 붙는 스탯 시스템 진입점.
    /// StatService 인스턴스를 소유하며, 다른 Player 컴포넌트들이 이를 참조해
    /// 기본값 등록과 최종 스탯 조회를 수행한다.
    ///
    /// 실행 순서를 -100으로 지정해 다른 컴포넌트의 Awake보다 먼저 초기화된다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerStatController : MonoBehaviour
    {
        public StatService StatService { get; private set; }

        private void Awake()
        {
            StatService = new StatService();
        }
    }
}
