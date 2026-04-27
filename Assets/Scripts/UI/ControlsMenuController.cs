using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    // 컨트롤 창 표시/숨김, 키보드 네비게이션, 키 리바인딩 관리.
    //
    // [네비게이션]
    //   상하 방향키  → 행 이동
    //   좌우 방향키  → 컬럼 전환 (바인딩 행에서만)
    //                  방향키대쉬(row 5, 왼쪽 열)에서는 켜짐/꺼짐 토글
    //   Enter/Space  → 선택 실행 (바인딩 행: 리바인드 오버레이, 기타: 돌아가기/초기화)
    //   ESC          → 리바인딩 중이면 취소, 아니면 PauseMenuController가 닫음
    //
    // [리바인딩]
    //   Enter/Space/클릭으로 항목 선택 → "새 키 누르기" 오버레이 표시
    //   아무 키(ESC 제외) 입력 → 해당 Action에 저장 후 뱃지 갱신
    //   ESC 입력 → 취소

    public class ControlsMenuController : MonoBehaviour
    {
        private const int BindingRowCount = 7;
        private const int TotalRows       = 9;  // 7 바인딩 + 초기화(7) + 돌아가기(8)
        private const int DirDashRow      = 5;  // 방향키대쉬 전용 토글 행 (왼쪽 열)

        private const string RowSelected  = "ctrl-row--selected";
        private const string FootSelected = "ctrl-footer-item--selected";
        private const string BackSelected = "ctrl-footer-back--selected";
        private const string KeyBadgeOff  = "ctrl-key-badge--off";

        // ── 열별 액션 매핑 ────────────────────────────────────────────
        // row 5 왼쪽(DirDashRow)은 특수 처리이므로 플레이스홀더를 넣는다.
        private static readonly KeyBindingService.Action[] LeftActions =
        {
            KeyBindingService.Action.MoveUp,
            KeyBindingService.Action.MoveDown,
            KeyBindingService.Action.MoveLeft,
            KeyBindingService.Action.MoveRight,
            KeyBindingService.Action.Inventory,
            KeyBindingService.Action.MoveUp,   // DirDashRow — 실제로 사용되지 않음
            KeyBindingService.Action.Interact,
        };

        private static readonly KeyBindingService.Action[] RightActions =
        {
            KeyBindingService.Action.Attack,
            KeyBindingService.Action.Jump,
            KeyBindingService.Action.Dash,
            KeyBindingService.Action.Skill1,
            KeyBindingService.Action.Skill2,
            KeyBindingService.Action.Essence,
            KeyBindingService.Action.Swap,
        };

        // ── UI 참조 ───────────────────────────────────────────────────
        private VisualElement   _overlay;
        private VisualElement[] _leftRows;
        private VisualElement[] _rightRows;
        private Label           _resetLabel;
        private Label           _backLabel;

        private Label[] _leftKeyLabels;   // ctrl-left-{i}-key
        private Label[] _rightKeyLabels;  // ctrl-right-{i}-key

        private VisualElement _rebindPanel; // "새 키 누르기" 오버레이

        // ── 상태 ─────────────────────────────────────────────────────
        private int  _row = 0;
        private int  _col = 0;
        private bool _isOpen          = false;
        private bool _openedThisFrame = false;
        private bool _isRebinding     = false;
        private int  _rebindRow;
        private int  _rebindCol;

        public bool IsOpen      => _isOpen;
        public bool IsRebinding => _isRebinding;

        // ESC·None 제외, 현재 키보드에서 지원되는 Key만 (Start에서 한 번 필터링)
        private Key[] _validKeys;

        // ── 생명주기 ──────────────────────────────────────────────────

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _overlay     = root.Q<VisualElement>("controls-overlay");
            _resetLabel  = root.Q<Label>("ctrl-reset");
            _backLabel   = root.Q<Label>("ctrl-back");
            _rebindPanel = root.Q<VisualElement>("rebind-panel");

            _leftRows       = new VisualElement[BindingRowCount];
            _rightRows      = new VisualElement[BindingRowCount];
            _leftKeyLabels  = new Label[BindingRowCount];
            _rightKeyLabels = new Label[BindingRowCount];

            for (int i = 0; i < BindingRowCount; i++)
            {
                _leftRows[i]       = root.Q<VisualElement>($"ctrl-left-{i}");
                _rightRows[i]      = root.Q<VisualElement>($"ctrl-right-{i}");
                _leftKeyLabels[i]  = root.Q<Label>($"ctrl-left-{i}-key");
                _rightKeyLabels[i] = root.Q<Label>($"ctrl-right-{i}-key");
            }

            _overlay.style.display     = DisplayStyle.None;
            _rebindPanel.style.display = DisplayStyle.None;

            // 리바인딩 감지용 유효 Key 목록 사전 생성
            // — None·Escape 제외, 현재 키보드에서 접근 불가한 키도 제외
            var kb  = Keyboard.current;
            var tmp = new System.Collections.Generic.List<Key>();
            foreach (Key k in (Key[])Enum.GetValues(typeof(Key)))
            {
                if (k == Key.None || k == Key.Escape) continue;
                if (kb == null) { tmp.Add(k); continue; }
                try   { _ = kb[k]; tmp.Add(k); }
                catch { /* 이 키보드에서 지원되지 않는 키 — 제외 */ }
            }
            _validKeys = tmp.ToArray();
        }

        private void Update()
        {
            if (!_isOpen) return;
            if (_openedThisFrame) { _openedThisFrame = false; return; }
            if (Keyboard.current == null) return;

            // 리바인딩 오버레이 활성화 중 — 키 입력 독점
            if (_isRebinding)
            {
                HandleRebindInput();
                return;
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                MoveRow(-1);
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                MoveRow(1);

            // 방향키대쉬 행: 좌우 = 켜짐/꺼짐 토글
            if (_col == 0 && _row == DirDashRow)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                    Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    ToggleDirDash();
            }
            else if (_row < BindingRowCount)
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

        // ── 마우스 입력 (PauseMenuController에서 호출) ────────────────

        public void HandleMouseInput(Vector2 panelPos, bool clicked)
        {
            if (_isRebinding) return;

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

        // ── 외부 호출 ─────────────────────────────────────────────────

        public void Open()
        {
            _isOpen          = true;
            _openedThisFrame = true;
            _overlay.style.display = DisplayStyle.Flex;
            RefreshAllKeyLabels();
            SetSelection(0, 0);
        }

        public void Close()
        {
            if (_isRebinding) CloseRebindPanel();
            _isOpen = false;
            _overlay.style.display = DisplayStyle.None;
        }

        // ── 리바인딩 ──────────────────────────────────────────────────

        private void HandleRebindInput()
        {
            var kb = Keyboard.current;

            // ESC → 취소
            if (kb.escapeKey.wasPressedThisFrame)
            {
                CloseRebindPanel();
                return;
            }

            if (!kb.anyKey.wasPressedThisFrame) return;

            foreach (var key in _validKeys)
            {
                if (kb[key].wasPressedThisFrame)
                {
                    ApplyRebind(key);
                    return;
                }
            }
        }

        private void ApplyRebind(Key key)
        {
            var targetAction = _rebindCol == 0 ? LeftActions[_rebindRow] : RightActions[_rebindRow];
            Key oldKey = KeyBindingService.Get(targetAction);

            // 새 키가 다른 액션에 이미 할당되어 있으면 서로 교체
            if (FindActionForKey(key, targetAction, out var conflictAction))
            {
                KeyBindingService.Set(conflictAction, oldKey);
                UpdateLabelForAction(conflictAction, oldKey);
            }

            KeyBindingService.Set(targetAction, key);
            GetKeyLabel(_rebindRow, _rebindCol).text = KeyBindingService.ToDisplayString(key);
            CloseRebindPanel();
        }

        // 지정된 키를 사용 중인 다른 액션을 반환한다. (DirDashRow 제외, excludeAction 제외)
        private bool FindActionForKey(Key key, KeyBindingService.Action excludeAction,
                                      out KeyBindingService.Action result)
        {
            for (int i = 0; i < BindingRowCount; i++)
            {
                if (i == DirDashRow) continue;
                var a = LeftActions[i];
                if (a != excludeAction && KeyBindingService.Get(a) == key) { result = a; return true; }
            }
            for (int i = 0; i < BindingRowCount; i++)
            {
                var a = RightActions[i];
                if (a != excludeAction && KeyBindingService.Get(a) == key) { result = a; return true; }
            }
            result = default;
            return false;
        }

        // 특정 액션이 표시된 라벨을 찾아 텍스트를 갱신한다.
        private void UpdateLabelForAction(KeyBindingService.Action action, Key key)
        {
            string text = KeyBindingService.ToDisplayString(key);
            for (int i = 0; i < BindingRowCount; i++)
            {
                if (i == DirDashRow) continue;
                if (LeftActions[i] == action)  { _leftKeyLabels[i].text  = text; return; }
            }
            for (int i = 0; i < BindingRowCount; i++)
            {
                if (RightActions[i] == action) { _rightKeyLabels[i].text = text; return; }
            }
        }

        private void StartRebind(int row, int col)
        {
            _isRebinding       = true;
            _rebindRow         = row;
            _rebindCol         = col;
            _rebindPanel.style.display = DisplayStyle.Flex;
        }

        private void CloseRebindPanel()
        {
            _isRebinding               = false;
            _rebindPanel.style.display = DisplayStyle.None;
        }

        // ── 방향키대쉬 토글 ───────────────────────────────────────────

        private void ToggleDirDash()
        {
            KeyBindingService.SetDirectionalDash(!KeyBindingService.DirectionalDash);
            RefreshDirDashLabel();
        }

        private void RefreshDirDashLabel()
        {
            var  label = _leftKeyLabels[DirDashRow];
            bool on    = KeyBindingService.DirectionalDash;
            label.text = on ? "켜짐" : "꺼짐";
            if (on) label.RemoveFromClassList(KeyBadgeOff);
            else    label.AddToClassList(KeyBadgeOff);
        }

        // ── 키 라벨 갱신 ─────────────────────────────────────────────

        private void RefreshAllKeyLabels()
        {
            for (int i = 0; i < BindingRowCount; i++)
            {
                if (i == DirDashRow)
                {
                    RefreshDirDashLabel();
                    continue;
                }
                _leftKeyLabels[i].text  = KeyBindingService.ToDisplayString(KeyBindingService.Get(LeftActions[i]));
                _rightKeyLabels[i].text = KeyBindingService.ToDisplayString(KeyBindingService.Get(RightActions[i]));
            }
        }

        private Label GetKeyLabel(int row, int col)
            => col == 0 ? _leftKeyLabels[row] : _rightKeyLabels[row];

        // ── 네비게이션 ────────────────────────────────────────────────

        private void MoveRow(int delta)
        {
            int next = (_row + delta + TotalRows) % TotalRows;
            SetSelection(next, (next < BindingRowCount) ? _col : 0);
        }

        private void SetCol(int col) => SetSelection(_row, col);

        private void SetSelection(int row, int col)
        {
            ClearHighlight(_row, _col);
            _row = row;
            _col = col;
            ApplyHighlight(_row, _col);
        }

        private void ClearHighlight(int row, int col)
        {
            if      (row < BindingRowCount)  (col == 0 ? _leftRows[row] : _rightRows[row]).RemoveFromClassList(RowSelected);
            else if (row == BindingRowCount) _resetLabel.RemoveFromClassList(FootSelected);
            else                             _backLabel.RemoveFromClassList(BackSelected);
        }

        private void ApplyHighlight(int row, int col)
        {
            if      (row < BindingRowCount)  (col == 0 ? _leftRows[row] : _rightRows[row]).AddToClassList(RowSelected);
            else if (row == BindingRowCount) _resetLabel.AddToClassList(FootSelected);
            else                             _backLabel.AddToClassList(BackSelected);
        }

        // ── 실행 ─────────────────────────────────────────────────────

        private void ExecuteSelection()
        {
            if (_row == TotalRows - 1)              { Close(); return; }
            if (_row == BindingRowCount)             { ResetToDefaults(); return; }
            if (_col == 0 && _row == DirDashRow)    { ToggleDirDash(); return; }
            StartRebind(_row, _col);
        }

        private void ResetToDefaults()
        {
            KeyBindingService.ResetToDefaults();
            RefreshAllKeyLabels();
        }
    }
}
