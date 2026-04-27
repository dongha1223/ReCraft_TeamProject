using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    // 설정 창 표시/숨김, 키보드 네비게이션, 슬라이더 조작 관리.
    //
    // [슬라이더 마우스 조작]
    //   UIToolkit 이벤트가 이 환경에서 동작하지 않으므로
    //   PauseMenuController가 Mouse.current 상태를 읽어 HandleMouseInput()으로 전달.
    public class SettingsMenuController : MonoBehaviour
    {
        private const int LeftRowCount  = 8;
        private const int RightRowCount = 7;
        private const int BackRow       = 8;

        private const string RowSelected  = "sett-row--selected";
        private const string BackSelected = "sett-footer-back--selected";

        private VisualElement   _overlay;
        private VisualElement[] _leftRows;
        private VisualElement[] _rightRows;
        private Label           _backLabel;

        private SettingsSlider[] _leftSliders;
        private SettingsSlider[] _rightSliders;
        private SettingsSlider[] _allSliders;   // 마우스 히트 테스트용 평면 배열
        private SettingsSlider   _activeSlider; // 현재 드래그 중인 슬라이더

        private int  _row    = 0;
        private int  _col    = 0;
        private bool _isOpen = false;
        private bool _openedThisFrame = false;
        public  bool IsOpen  => _isOpen;

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            _overlay   = root.Q<VisualElement>("settings-overlay");
            _backLabel = root.Q<Label>("sett-back");

            _leftRows  = new VisualElement[LeftRowCount];
            _rightRows = new VisualElement[RightRowCount];
            for (int i = 0; i < LeftRowCount;  i++) _leftRows[i]  = root.Q<VisualElement>($"sett-left-{i}");
            for (int i = 0; i < RightRowCount; i++) _rightRows[i] = root.Q<VisualElement>($"sett-right-{i}");

            _leftSliders  = new SettingsSlider[LeftRowCount];
            _rightSliders = new SettingsSlider[RightRowCount];
            _leftSliders[4]  = new SettingsSlider(root.Q<Slider>("sett-left-4-slider"));
            _leftSliders[5]  = new SettingsSlider(root.Q<Slider>("sett-left-5-slider"));
            _rightSliders[0] = new SettingsSlider(root.Q<Slider>("sett-right-0-slider"));
            _rightSliders[1] = new SettingsSlider(root.Q<Slider>("sett-right-1-slider"));
            _rightSliders[2] = new SettingsSlider(root.Q<Slider>("sett-right-2-slider"));

            _allSliders = new[]
            {
                _leftSliders[4], _leftSliders[5],
                _rightSliders[0], _rightSliders[1], _rightSliders[2]
            };

            _overlay.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (!_isOpen) return;
            if (_openedThisFrame) { _openedThisFrame = false; return; }
            if (Keyboard.current == null) return;

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                MoveRow(-1);
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                MoveRow(1);

            if (_row < BackRow)
            {
                if (IsSliderRow(_row, _col))
                {
                    if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                        GetSlider(_row, _col)?.AdjustValue(-0.1f);
                    else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                        GetSlider(_row, _col)?.AdjustValue(0.1f);
                }
                else
                {
                    if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                        SwitchCol(0);
                    else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                        SwitchCol(1);
                }
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
                ExecuteSelection();
        }

        // ── 마우스 입력 (PauseMenuController에서 호출) ────────────────────

        public void HandleMouseInput(Vector2 panelPos, bool clicked, bool held, bool released)
        {
            HandleRowMouseInput(panelPos, clicked);

            if (clicked)
            {
                _activeSlider = null;
                foreach (var s in _allSliders)
                {
                    if (s != null && s.Contains(panelPos))
                    {
                        _activeSlider = s;
                        s.SetValue(panelPos);
                        break;
                    }
                }
            }
            else if (held && _activeSlider != null)
            {
                _activeSlider.SetValue(panelPos);
            }

            if (released)
                _activeSlider = null;
        }

        private void HandleRowMouseInput(Vector2 panelPos, bool clicked)
        {
            for (int i = 0; i < LeftRowCount; i++)
            {
                if (_leftRows[i].worldBound.Contains(panelPos))
                {
                    SetSelection(i, 0);
                    if (clicked && !IsSliderRow(i, 0)) ExecuteSelection();
                    return;
                }
            }

            for (int i = 0; i < RightRowCount; i++)
            {
                if (_rightRows[i].worldBound.Contains(panelPos))
                {
                    SetSelection(i, 1);
                    if (clicked && !IsSliderRow(i, 1)) ExecuteSelection();
                    return;
                }
            }

            if (_backLabel.worldBound.Contains(panelPos))
            {
                SetSelection(BackRow, 0);
                if (clicked) ExecuteSelection();
            }
        }

        // ── 외부 호출 ─────────────────────────────────────────────────────

        public void Open()
        {
            _isOpen = true;
            _openedThisFrame = true;
            _overlay.style.display = DisplayStyle.Flex;
            SetSelection(0, 0);
        }

        public void Close()
        {
            _isOpen       = false;
            _activeSlider = null;
            _overlay.style.display = DisplayStyle.None;
        }

        // ── 네비게이션 ────────────────────────────────────────────────────

        private int MaxDataRow(int col) => col == 0 ? LeftRowCount - 1 : RightRowCount - 1;

        private void MoveRow(int delta)
        {
            int next;
            if (_row == BackRow)
                next = delta > 0 ? 0 : MaxDataRow(_col);
            else
            {
                next = _row + delta;
                if      (next > MaxDataRow(_col)) next = BackRow;
                else if (next < 0)                next = BackRow;
            }
            SetSelection(next, _col);
        }

        private void SwitchCol(int newCol)
        {
            if (newCol == _col) return;
            SetSelection(Mathf.Min(_row, MaxDataRow(newCol)), newCol);
        }

        private void SetSelection(int row, int col)
        {
            ClearHighlight(_row, _col);
            _row = row;
            _col = col;
            ApplyHighlight(_row, _col);
        }

        private void ClearHighlight(int row, int col)
        {
            if (row == BackRow) _backLabel.RemoveFromClassList(BackSelected);
            else (col == 0 ? _leftRows[row] : _rightRows[row]).RemoveFromClassList(RowSelected);
        }

        private void ApplyHighlight(int row, int col)
        {
            if (row == BackRow) _backLabel.AddToClassList(BackSelected);
            else (col == 0 ? _leftRows[row] : _rightRows[row]).AddToClassList(RowSelected);
        }

        // ── 실행 ─────────────────────────────────────────────────────────

        private void ExecuteSelection()
        {
            if (_row == BackRow) { Close(); return; }
            Debug.Log($"[SettingsMenu] 항목 실행 예정: row={_row}, col={_col}");
        }

        // ── 슬라이더 유틸 ─────────────────────────────────────────────────

        private bool IsSliderRow(int row, int col)
        {
            if (col == 0) return row == 4 || row == 5;
            if (col == 1) return row == 0 || row == 1 || row == 2;
            return false;
        }

        private SettingsSlider GetSlider(int row, int col)
            => col == 0 ? _leftSliders[row] : _rightSliders[row];

        // ── 슬라이더 내부 클래스 ──────────────────────────────────────────

        private class SettingsSlider
        {
            private readonly Slider _slider;

            public float Value => _slider.value;

            public SettingsSlider(Slider slider)
            {
                _slider = slider;
            }

            // 슬라이더 전체 bound 기준으로 히트 검사
            public bool Contains(Vector2 panelPos)
                => _slider.worldBound.Contains(panelPos);

            // 패널 좌표 → 슬라이더 값 (슬라이더 bound 기준 선형 매핑)
            public void SetValue(Vector2 panelPos)
            {
                Rect b = _slider.worldBound;
                if (b.width <= 0f) return;
                float t = Mathf.Clamp01((panelPos.x - b.x) / b.width);
                _slider.value = Mathf.Lerp(_slider.lowValue, _slider.highValue, t);
            }

            // 방향키 조절 (1/10 단위)
            public void AdjustValue(float delta)
            {
                _slider.value = Mathf.Clamp(_slider.value + delta,
                                            _slider.lowValue, _slider.highValue);
            }
        }
    }
}
