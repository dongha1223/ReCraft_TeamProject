using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    // 컨트롤 창 표시/숨김 및 키보드 네비게이션 관리.
    // 키 변경 기능은 추후 InputActionRebindingExtensions를 이용해 구현 예정.
    //
    // [네비게이션]
    //   상하 방향키 → 행 이동 (0~8: 7바인딩행 + 초기화 + 돌아가기)
    //   좌우 방향키 → 컬럼 전환 (행 0~6에서만: 0=좌, 1=우)
    //   Enter/Space → 선택 실행 (현재는 돌아가기만 동작, 나머지 placeholder)
    //   ESC         → 창 닫기
    public class ControlsMenuController : MonoBehaviour
    {
        private const int BindingRowCount = 7;
        // 전체 네비게이션 행 수: 7 바인딩 + 초기화(7) + 돌아가기(8)
        private const int TotalRows = 9;

        private const string RowSelected  = "ctrl-row--selected";
        private const string FootSelected = "ctrl-footer-item--selected";
        private const string BackSelected = "ctrl-footer-back--selected";

        private VisualElement   _overlay;
        private VisualElement[] _leftRows;
        private VisualElement[] _rightRows;
        private Label           _resetLabel;
        private Label           _backLabel;

        // 현재 선택 위치
        private int _row = 0;  // 0~8
        private int _col = 0;  // 0=좌, 1=우 (행 0~6에서만 유효)
        private bool _isOpen = false;
        public bool IsOpen => _isOpen;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _overlay    = root.Q<VisualElement>("controls-overlay");
            _resetLabel = root.Q<Label>("ctrl-reset");
            _backLabel  = root.Q<Label>("ctrl-back");

            _leftRows  = new VisualElement[BindingRowCount];
            _rightRows = new VisualElement[BindingRowCount];
            for (int i = 0; i < BindingRowCount; i++)
            {
                _leftRows[i]  = root.Q<VisualElement>($"ctrl-left-{i}");
                _rightRows[i] = root.Q<VisualElement>($"ctrl-right-{i}");
            }

            _overlay.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (!_isOpen) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                MoveRow(-1);
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                MoveRow(1);

            // 바인딩 행(0~6)에서만 좌우 컬럼 전환
            if (_row < BindingRowCount)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    SetCol(0);
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    SetCol(1);
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
                ExecuteSelection();
        }

        // ── 마우스 입력 (PauseMenuController에서 호출) ────────────────────

        public void HandleMouseInput(Vector2 panelPos, bool clicked)
        {
            for (int i = 0; i < BindingRowCount; i++)
            {
                if (_leftRows[i].worldBound.Contains(panelPos))
                {
                    SetSelection(i, 0);
                    if (clicked) ExecuteSelection();
                    return;
                }
                if (_rightRows[i].worldBound.Contains(panelPos))
                {
                    SetSelection(i, 1);
                    if (clicked) ExecuteSelection();
                    return;
                }
            }

            if (_resetLabel.worldBound.Contains(panelPos))
            {
                SetSelection(BindingRowCount, 0);
                if (clicked) ExecuteSelection();
                return;
            }
            if (_backLabel.worldBound.Contains(panelPos))
            {
                SetSelection(TotalRows - 1, 0);
                if (clicked) ExecuteSelection();
            }
        }

        // ── 외부 호출 ─────────────────────────────────────────────────────

        public void Open()
        {
            _isOpen = true;
            _overlay.style.display = DisplayStyle.Flex;
            SetSelection(0, 0);
        }

        public void Close()
        {
            _isOpen = false;
            _overlay.style.display = DisplayStyle.None;
        }

        // ── 네비게이션 ────────────────────────────────────────────────────

        private void MoveRow(int delta)
        {
            int next = (_row + delta + TotalRows) % TotalRows;
            // 초기화/돌아가기 행으로 이동할 때는 컬럼 초기화
            SetSelection(next, (next < BindingRowCount) ? _col : 0);
        }

        private void SetCol(int col)
        {
            SetSelection(_row, col);
        }

        private void SetSelection(int row, int col)
        {
            // 이전 선택 해제
            ClearHighlight(_row, _col);

            _row = row;
            _col = col;

            // 새 선택 적용
            ApplyHighlight(_row, _col);
        }

        private void ClearHighlight(int row, int col)
        {
            if (row < BindingRowCount)
                (col == 0 ? _leftRows[row] : _rightRows[row]).RemoveFromClassList(RowSelected);
            else if (row == BindingRowCount)
                _resetLabel.RemoveFromClassList(FootSelected);
            else
                _backLabel.RemoveFromClassList(BackSelected);
        }

        private void ApplyHighlight(int row, int col)
        {
            if (row < BindingRowCount)
            {
                VisualElement target = (col == 0) ? _leftRows[row] : _rightRows[row];
                target.AddToClassList(RowSelected);
            }
            else if (row == BindingRowCount)
                _resetLabel.AddToClassList(FootSelected);
            else
                _backLabel.AddToClassList(BackSelected);
        }

        // ── 실행 ─────────────────────────────────────────────────────────

        private void ExecuteSelection()
        {
            if (_row < BindingRowCount)
            {
                // TODO: 키 리바인딩 구현
                // InputActionRebindingExtensions.PerformInteractiveRebinding()으로
                // 다음 키 입력을 캡처해 해당 액션에 override 적용 후 PlayerPrefs에 저장
                Debug.Log($"[ControlsMenu] 리바인딩 예정: row={_row}, col={_col}");
            }
            else if (_row == BindingRowCount)
            {
                // TODO: 기본값으로 초기화
                Debug.Log("[ControlsMenu] 초기화 예정");
            }
            else
            {
                Close();
            }
        }
    }
}
