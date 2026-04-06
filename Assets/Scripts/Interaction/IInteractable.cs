namespace _2D_Roguelike
{
    /// <summary>
    /// F키 단누름으로 상호작용 가능한 모든 오브젝트의 공통 인터페이스.
    /// 새로운 상호작용 기능은 이 인터페이스를 구현하는 클래스를 추가하기만 하면 된다.
    /// PlayerInteractor는 이 인터페이스만 알고, 구체적인 동작은 모른다.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 현재 상호작용 가능한 상태인지. false면 F키를 눌러도 무시된다.
        /// 예: 표지판은 적 전멸 후에만 true
        /// </summary>
        bool CanInteract { get; }

        /// <summary>플레이어가 범위에 들어왔을 때 (프롬프트·하이라이트 ON)</summary>
        void OnFocused();

        /// <summary>플레이어가 범위를 벗어났을 때 (프롬프트·하이라이트 OFF)</summary>
        void OnUnfocused();

        /// <summary>F키 단누름 시 실행</summary>
        void OnInteract(PlayerStatController statController);
    }
}
