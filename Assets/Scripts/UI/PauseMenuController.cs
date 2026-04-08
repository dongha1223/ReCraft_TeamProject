using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    [DefaultExecutionOrder(-10)]
    public class PauseMenuController : MonoBehaviour
    {
        private const int MenuCount = 5;
        private const string SelectedClass = "menu-item--selected";
        private const string BtnSelectedClass = "confirm-btn--selected";

        private VisualElement _overlay;
        private Label[] _menuItems;
        private int _selectedIndex = 0;
        private bool _isPaused = false;

        private ControlsMenuController  _controlsMenu;
        private SettingsMenuController  _settingsMenu;

        [SerializeField] private InventoryController _inventoryController;

        // 확인 다이얼로그
        private VisualElement _confirmOverlay;
        private Label _confirmYes;
        private Label   _confirmNo;
        private Label[] _confirmBtns;
        private int _confirmIndex = 0; // 0: 예, 1: 아니오
        private bool _isConfirmOpen = false;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _overlay = root.Q<VisualElement>("pause-overlay");

            _menuItems = new Label[MenuCount];
            for (int i = 0; i < MenuCount; i++)
                _menuItems[i] = root.Q<Label>($"menu-item-{i}");

            _confirmOverlay = root.Q<VisualElement>("confirm-overlay");
            _confirmYes  = root.Q<Label>("confirm-yes");
            _confirmNo   = root.Q<Label>("confirm-no");
            _confirmBtns = new[] { _confirmYes, _confirmNo };

            _controlsMenu = GetComponent<ControlsMenuController>();
            _settingsMenu = GetComponent<SettingsMenuController>();

            _overlay.style.display        = DisplayStyle.None;
            _confirmOverlay.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // ESC는 항상 최우선으로 처리 — 인벤토리 → 하위 창 → 포즈 메뉴 순으로 닫기
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_inventoryController != null && _inventoryController.IsOpen) { _inventoryController.Close(); return; }
                if (_controlsMenu != null && _controlsMenu.IsOpen)
                {
                    // 리바인딩 중이면 ControlsMenuController가 ESC를 처리하도록 넘김
                    if (!_controlsMenu.IsRebinding) _controlsMenu.Close();
                    return;
                }
                if (_settingsMenu != null && _settingsMenu.IsOpen) { _settingsMenu.Close(); return; }
                if (_isConfirmOpen) { CloseConfirmDialog(); return; }
                if (_isPaused) ClosePauseMenu();
                else OpenPauseMenu();
                return;
            }

            HandleMouseInput();

            // 하위 창이 열려 있으면 포즈 메뉴 키보드 입력 차단
            if (_controlsMenu != null && _controlsMenu.IsOpen) return;
            if (_settingsMenu != null && _settingsMenu.IsOpen) return;

            if (_isConfirmOpen)
            {
                HandleConfirmInput();
                return;
            }

            if (!_isPaused) return;

            // 위/아래 화살표로 항목 이동
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                MoveSelection(-1);
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                MoveSelection(1);

            // Enter 또는 Space로 선택 실행
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
                ExecuteSelected();
        }

        // ── 마우스 입력 ───────────────────────────────────────────────────
        // 마우스 상태를 한 번만 읽고 활성화된 창으로 라우팅.

        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;

            Vector2 sp       = Mouse.current.position.ReadValue();
            Vector2 panelPos = new Vector2(sp.x, Screen.height - sp.y);
            bool clicked     = Mouse.current.leftButton.wasPressedThisFrame;

            if (_controlsMenu != null && _controlsMenu.IsOpen)
            {
                _controlsMenu.HandleMouseInput(panelPos, clicked);
                return;
            }

            if (_settingsMenu != null && _settingsMenu.IsOpen)
            {
                _settingsMenu.HandleMouseInput(panelPos, clicked,
                    Mouse.current.leftButton.isPressed,
                    Mouse.current.leftButton.wasReleasedThisFrame);
                return;
            }

            if (!_isPaused) return;

            if (_isConfirmOpen)
            {
                if (_confirmBtns[0].worldBound.Contains(panelPos))
                {
                    SetConfirmSelection(0);
                    if (clicked) ConfirmNewGame();
                }
                else if (_confirmBtns[1].worldBound.Contains(panelPos))
                {
                    SetConfirmSelection(1);
                    if (clicked) CloseConfirmDialog();
                }
                return;
            }

            for (int i = 0; i < MenuCount; i++)
            {
                if (_menuItems[i].worldBound.Contains(panelPos))
                {
                    SetSelection(i);
                    if (clicked) ExecuteSelected();
                    break;
                }
            }
        }

        // ── 포즈 메뉴 ────────────────────────────────────────────────────

        private void OpenPauseMenu()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            _overlay.style.display = DisplayStyle.Flex;
            SetSelection(0);
        }

        private void ClosePauseMenu()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            _overlay.style.display = DisplayStyle.None;
        }

        private void MoveSelection(int delta)
        {
            int next = (_selectedIndex + delta + MenuCount) % MenuCount;
            SetSelection(next);
        }

        private void SetSelection(int index)
        {
            _menuItems[_selectedIndex].RemoveFromClassList(SelectedClass);
            _selectedIndex = index;
            _menuItems[_selectedIndex].AddToClassList(SelectedClass);
        }

        private void ExecuteSelected()
        {
            switch (_selectedIndex)
            {
                case 0: // 돌아가기
                    ClosePauseMenu();
                    break;
                case 1: // 새 게임
                    OpenConfirmDialog();
                    break;
                case 2: // 컨트롤
                    OpenControlsMenu();
                    break;
                case 3: // 설정
                    _settingsMenu.Open();
                    break;
                case 4: // 게임종료
                    QuitGame();
                    break;
            }
        }

        // ── 컨트롤 메뉴 ──────────────────────────────────────────────────

        private void OpenControlsMenu()
        {
            // 포즈 메뉴는 뒤에 유지, 컨트롤 창이 그 위에 표시됨
            _controlsMenu.Open();
        }

        // ── 확인 다이얼로그 ───────────────────────────────────────────────

        private void OpenConfirmDialog()
        {
            _isConfirmOpen = true;
            _confirmOverlay.style.display = DisplayStyle.Flex;
            SetConfirmSelection(0);
        }

        private void CloseConfirmDialog()
        {
            _isConfirmOpen = false;
            _confirmOverlay.style.display = DisplayStyle.None;
        }

        private void HandleConfirmInput()
        {
            // ESC or 아니오 방향키 → 닫기
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseConfirmDialog();
                return;
            }

            // 좌우 방향키로 예/아니오 전환
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                SetConfirmSelection(0);
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                SetConfirmSelection(1);

            // Enter / Space로 실행
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (_confirmIndex == 0)
                    ConfirmNewGame();
                else
                    CloseConfirmDialog();
            }
        }

        private void SetConfirmSelection(int index)
        {
            _confirmBtns[_confirmIndex].RemoveFromClassList(BtnSelectedClass);
            _confirmIndex = index;
            _confirmBtns[_confirmIndex].AddToClassList(BtnSelectedClass);
        }

        private void ConfirmNewGame()
        {
            CloseConfirmDialog();
            ClosePauseMenu();
            StageManager.Instance.RestartGame();
        }

        // ── 기타 ─────────────────────────────────────────────────────────

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
