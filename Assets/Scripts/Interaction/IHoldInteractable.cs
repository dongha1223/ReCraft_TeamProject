namespace _2D_Roguelike
{
    /// <summary>
    /// F키 길게 누름 상호작용을 추가로 지원하는 인터페이스.
    /// IInteractable을 상속하므로 단누름(OnInteract)도 반드시 구현해야 한다.
    ///
    /// 동작 규칙:
    ///   - F키를 HoldDuration 이상 누르면 OnHoldInteract 발동
    ///   - 그 전에 떼면 OnInteract 발동 (단누름으로 처리)
    /// </summary>
    public interface IHoldInteractable : IInteractable
    {
        /// <summary>길게 누름 발동까지 걸리는 시간 (초)</summary>
        float HoldDuration { get; }

        /// <summary>F키 길게 누름 시 실행. 예: 아이템 분해</summary>
        void OnHoldInteract(PlayerStatController statController);
    }
}
