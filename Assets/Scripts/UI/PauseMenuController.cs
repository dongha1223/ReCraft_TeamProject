using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    public class PauseMenuController : MonoBehaviour
    {
        private const int MenuCount = 5;
        private const string SelectedClass = "menu-item--selected";

        private VisualElement _overlay;
        private Label[] _menuItems;
        private int _selectedIndex = 0;
        private bool _isPaused = false;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _overlay = root.Q<VisualElement>("pause-overlay");

            _menuItems = new Label[MenuCount];
            for (int i = 0; i < MenuCount; i++)
                _menuItems[i] = root.Q<Label>($"menu-item-{i}");

            // 시작 시 메뉴 숨김
            _overlay.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // ESC: 메뉴 열기/닫기
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isPaused) ClosePauseMenu();
                else OpenPauseMenu();
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
            // 이전 선택 해제
            _menuItems[_selectedIndex].RemoveFromClassList(SelectedClass);
            _selectedIndex = index;
            // 새 선택 적용
            _menuItems[_selectedIndex].AddToClassList(SelectedClass);
        }

        private void ExecuteSelected()
        {
            switch (_selectedIndex)
            {
                case 0: // 돌아가기
                    ClosePauseMenu();
                    break;
                case 1: // 새 게임 (미구현)
                    break;
                case 2: // 컨트롤 (미구현)
                    break;
                case 3: // 설정 (미구현)
                    break;
                case 4: // 게임종료
                    QuitGame();
                    break;
            }
        }

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
