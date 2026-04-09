namespace _2D_Roguelike
{
    /// <summary>
    /// UI 입력 차단 상태를 한 곳에서 집약한다.
    /// 새 UI가 추가되면 이 파일만 수정하면 된다.
    /// </summary>
    public static class UIState
    {
        /// <summary>어떤 UI라도 입력을 차단 중이면 true</summary>
        public static bool IsBlockingInput =>
            DialogueUIController.IsActive ||
            PauseMenuController.IsPaused  ||
            InventoryController.IsOpen;
    }
}
