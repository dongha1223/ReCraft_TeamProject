using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    /// <summary>
    /// 대화 UI 싱글톤 컨트롤러.
    /// 시네마틱 상단 바와 대화 패널의 열기/닫기 트랜지션을 관리한다.
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        public static DialogueUIController Instance { get; private set; }
        /// <summary>대화 중 플레이어 입력 잠금 등에 활용</summary>
        public static bool                 IsActive { get; private set; }

        [SerializeField] private UIDocument _uiDocument;

        private const float BarAnimDuration   = 0.4f;
        private const float PanelAnimDuration = 0.25f;

        // WaitForSeconds 캐싱 — 매 호출마다 힙 할당 방지
        private static readonly WaitForSeconds _waitBar   = new(BarAnimDuration);
        private static readonly WaitForSeconds _waitPanel = new(PanelAnimDuration);

        private VisualElement _cinematicTop;
        private VisualElement _cinematicBottom;
        private VisualElement _dialoguePanel;
        private Label         _npcNameLabel;
        private Label         _dialogueText;
        private Button        _btnContinue;
        private Button        _btnCancel;
        private Label         _beliefCountLabel;
        private VisualElement _beliefPanel;
        private IPanel        _panel;

        [SerializeField] private float _typewriterSpeed = 40f; // 초당 출력 문자 수

        private DialogueData _currentData;
        private int          _lineIndex;
        private int          _selectedIndex;    // 0=대화, 1=취소
        private bool         _isPanelVisible;   // 패널 완전히 열린 후에만 키 입력 수락
        private bool         _isTyping;         // 타이프라이터 진행 중
        private bool         _showingResponse;  // 선택 후 반응 문구 표시 중
        private string       _currentResponseText;
        private Coroutine    _typewriterCoroutine;

        private Action _onYes;
        private Action _onNo;

        // ── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var root = _uiDocument.rootVisualElement;

            _cinematicTop    = root.Q<VisualElement>("cinematic-top");
            _cinematicBottom = root.Q<VisualElement>("cinematic-bottom");
            _dialoguePanel   = root.Q<VisualElement>("dialogue-panel");
            _npcNameLabel  = root.Q<Label>("npc-name-label");
            _dialogueText  = root.Q<Label>("dialogue-text");
            _btnContinue   = root.Q<Button>("btn-continue");
            _btnCancel     = root.Q<Button>("btn-cancel");

            _beliefPanel      = root.Q<VisualElement>("belief-panel");
            _beliefCountLabel = root.Q<Label>("belief-count");
            _panel            = root.panel;
        }

        private void Update()
        {
            if (!_isPanelVisible) return;

            // 마우스 클릭/호버 처리 (UIToolkit 이벤트 우회, Input System 직접 폴링)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var screenPos = mouse.position.ReadValue();
                var flipped   = new Vector2(screenPos.x, Screen.height - screenPos.y);
                var uiPos     = RuntimePanelUtils.ScreenToPanel(_panel, flipped);

                bool overContinue = _btnContinue.worldBound.Contains(uiPos);
                bool overCancel   = _btnCancel.worldBound.Contains(uiPos);

                if      (overContinue) SetSelection(0);
                else if (overCancel)   SetSelection(1);

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (overContinue)
                    {
                        if (EnhanceUIController.IsOpen) EnhanceUIController.Instance?.TryConfirmUpgrade();
                        else                            OnContinueClicked();
                    }
                    else if (overCancel)
                    {
                        OnCancelClicked();
                    }
                }
            }

            var kb = Keyboard.current;
            if (kb == null) return;

            // ↑↓ 선택 전환
            if (kb.downArrowKey.wasPressedThisFrame)
                SetSelection(Mathf.Min(_selectedIndex + 1, 1));
            else if (kb.upArrowKey.wasPressedThisFrame)
                SetSelection(Mathf.Max(_selectedIndex - 1, 0));

            // Enter / Space / F키 → 현재 선택 항목 실행
            bool confirm = kb.enterKey.wasPressedThisFrame
                        || kb.spaceKey.wasPressedThisFrame
                        || KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Interact);
            if (confirm)
            {
                if (EnhanceUIController.IsOpen)
                {
                    if (_selectedIndex == 0) EnhanceUIController.Instance?.TryConfirmUpgrade();
                    else                     OnCancelClicked();
                }
                else
                {
                    if (_selectedIndex == 0) OnContinueClicked();
                    else                     OnCancelClicked();
                }
                return;
            }

            // ESC → 취소 (강화 UI가 열려있으면 EnhanceUIController가 ESC를 처리)
            if (kb.escapeKey.wasPressedThisFrame && !EnhanceUIController.IsOpen)
                OnCancelClicked();
        }

        // ── 공개 API ─────────────────────────────────────────────

        public void SetActionLabels(string confirm, string cancel)
        {
            if (_btnContinue != null) _btnContinue.text = confirm;
            if (_btnCancel   != null) _btnCancel.text   = cancel;
        }

        public void ShowBeliefPanel(bool visible)
        {
            if (_beliefPanel == null) return;
            _beliefPanel.EnableInClassList("belief-visible", visible);

            var bm = BeliefManager.Instance;
            if (bm == null) return;

            if (visible)
            {
                bm.OnBeliefChanged -= UpdateBeliefLabel;
                bm.OnBeliefChanged += UpdateBeliefLabel;
                UpdateBeliefLabel();
            }
            else
            {
                bm.OnBeliefChanged -= UpdateBeliefLabel;
            }
        }

        public void ShowEnhanceFeedback(string text)
        {
            if (!IsActive) return;
            StopTypewriterIfRunning();
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
        }

        public void StartDialogue(DialogueData data, Action onYes = null, Action onNo = null)
        {
            if (IsActive) return;
            _currentData         = data;
            _lineIndex           = 0;
            _showingResponse     = false;
            _currentResponseText = null;
            _onYes               = onYes;
            _onNo                = onNo;
            StartCoroutine(OpenSequence());
        }

        // ── 버튼 콜백 ────────────────────────────────────────────

        private void OnContinueClicked()
        {
            if (!_isPanelVisible) return;

            // 타이핑 중이면 전체 텍스트 즉시 표시 (스킵)
            if (_isTyping) { SkipTypewriter(); return; }

            // 반응 문구 표시 중 → 닫기
            if (_showingResponse) { StartCoroutine(CloseSequence()); return; }

            _lineIndex++;
            if (_lineIndex >= _currentData.Lines.Length)
            {
                if (_currentData.HasChoice)
                {
                    _onYes?.Invoke();
                    ShowResponse(_currentData.YesResponse);
                }
                else
                {
                    StartCoroutine(CloseSequence());
                }
            }
            else
            {
                ShowCurrentLine();
            }
        }

        private void OnCancelClicked()
        {
            if (!_isPanelVisible) return;
            if (_isTyping) { SkipTypewriter(); return; }

            if (_showingResponse) { StartCoroutine(CloseSequence()); return; }

            if (_currentData.HasChoice)
            {
                _onNo?.Invoke();
                ShowResponse(_currentData.NoResponse);
            }
            else
            {
                StartCoroutine(CloseSequence());
            }
        }

        // ── 내부 ─────────────────────────────────────────────────

        private void ShowCurrentLine()
        {
            bool isChoiceLine = _currentData.HasChoice && _lineIndex == _currentData.Lines.Length - 1;
            if (isChoiceLine)
            {
                _btnContinue.text = "예";
                _btnCancel.text   = "아니오";
            }
            else
            {
                _btnContinue.text = _currentData.GetConfirmLabel(_lineIndex) ?? "대화";
                _btnCancel.text   = _currentData.GetCancelLabel(_lineIndex)  ?? "취소";
            }

            StopTypewriterIfRunning();
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(_currentData.Lines[_lineIndex]));
        }

        /// <summary>강화 UI 등 외부 UI가 즉시 대화를 닫을 때 사용. 애니메이션 없이 즉시 초기화.</summary>
        public void ForceClose()
        {
            if (!IsActive) return;
            StopAllCoroutines();
            _isPanelVisible = false;
            _cinematicTop.RemoveFromClassList("bar-open");
            _cinematicBottom.RemoveFromClassList("bar-open");
            _dialoguePanel.RemoveFromClassList("panel-visible");
            _btnContinue.RemoveFromClassList("btn-selected");
            _btnCancel.RemoveFromClassList("btn-selected");
            ShowBeliefPanel(false);
            IsActive             = false;
            _currentData         = null;
            _onYes               = null;
            _onNo                = null;
            _showingResponse     = false;
            _currentResponseText = null;
        }

        private void ShowResponse(string text)
        {
            if (!IsActive) return;
            _showingResponse     = true;
            _currentResponseText = text ?? "";

            // 강화 UI가 열린 경우 Open()이 설정한 레이블("강화"/"나가기")을 유지
            if (!EnhanceUIController.IsOpen)
            {
                _btnContinue.text = "대화";
                _btnCancel.text   = "취소";
            }

            StopTypewriterIfRunning();
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(_currentResponseText));
        }

        private void StopTypewriterIfRunning()
        {
            if (_typewriterCoroutine == null) return;
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        private IEnumerator TypewriterCoroutine(string fullText)
        {
            _isTyping          = true;
            _dialogueText.text = "";

            float elapsed = 0f;
            int   shown   = 0;

            while (shown < fullText.Length)
            {
                elapsed += Time.deltaTime;
                int target = Mathf.Min(Mathf.FloorToInt(elapsed * _typewriterSpeed), fullText.Length);

                if (target > shown)
                {
                    shown              = target;
                    _dialogueText.text = fullText[..shown]; // C# 8 range syntax
                }

                yield return null;
            }

            _isTyping            = false;
            _typewriterCoroutine = null;
        }

        private void SkipTypewriter()
        {
            StopTypewriterIfRunning();
            _dialogueText.text = _showingResponse ? _currentResponseText : _currentData.Lines[_lineIndex];
            _isTyping          = false;
        }

        private void SetSelection(int index)
        {
            if (_selectedIndex == index) return;
            _selectedIndex = index;
            _btnContinue.EnableInClassList("btn-selected", index == 0);
            _btnCancel.EnableInClassList("btn-selected",   index == 1);
        }

        private IEnumerator OpenSequence()
        {
            IsActive = true;
            _cinematicTop.AddToClassList("bar-open");
            _cinematicBottom.AddToClassList("bar-open");
            yield return _waitBar;

            _npcNameLabel.text = _currentData.NpcName;
            ShowCurrentLine();
            SetSelection(0); // 대화 버튼 기본 선택
            _dialoguePanel.AddToClassList("panel-visible");
            _isPanelVisible = true;
        }

        private IEnumerator CloseSequence()
        {
            _isPanelVisible = false;
            _dialoguePanel.RemoveFromClassList("panel-visible");
            _btnContinue.RemoveFromClassList("btn-selected");
            _btnCancel.RemoveFromClassList("btn-selected");
            ShowBeliefPanel(false);
            EnhanceUIController.Instance?.FadeOutWithDialogue();
            yield return _waitPanel;

            _cinematicTop.RemoveFromClassList("bar-open");
            _cinematicBottom.RemoveFromClassList("bar-open");
            yield return _waitBar;

            IsActive             = false;
            _currentData         = null;
            _onYes               = null;
            _onNo                = null;
            _showingResponse     = false;
            _currentResponseText = null;
        }

        private void UpdateBeliefLabel()
        {
            if (_beliefCountLabel != null)
                _beliefCountLabel.text = (BeliefManager.Instance?.TotalBelief ?? 0).ToString();
        }
    }
}
