using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    public class EnhanceUIController : MonoBehaviour
    {
        public static EnhanceUIController Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _enhancePanel;
        private Label         _infoNameLabel;
        private Label         _infoLevelLabel;
        private Label         _infoCurrentDesc;
        private Label         _infoCostAmount;
        private Label         _infoNextDesc;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var root = _uiDocument.rootVisualElement;

            _enhancePanel    = root.Q<VisualElement>("enhance-panel");
            _infoNameLabel   = root.Q<Label>("info-name-label");
            _infoLevelLabel  = root.Q<Label>("info-level-label");
            _infoCurrentDesc = root.Q<Label>("info-current-desc");
            _infoCostAmount  = root.Q<Label>("info-cost-amount");
            _infoNextDesc    = root.Q<Label>("info-next-desc");
        }

        private void Update()
        {
            if (!IsOpen) return;

            // 대화창이 먼저 닫히면 강화 UI도 닫기
            if (!DialogueUIController.IsActive)
            {
                HidePanel();
                return;
            }

            if (Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _enhancePanel.AddToClassList("panel-visible");
            DialogueUIController.Instance?.ShowBeliefPanel(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            HidePanel();
            DialogueUIController.Instance?.ForceClose();
        }

        /// <summary>CloseSequence와 동시에 호출 — enhance 패널만 fade, 대화 시스템은 건드리지 않음.</summary>
        public void FadeOutWithDialogue()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _enhancePanel.RemoveFromClassList("panel-visible");
        }

        private void HidePanel()
        {
            IsOpen = false;
            _enhancePanel.RemoveFromClassList("panel-visible");
            DialogueUIController.Instance?.ShowBeliefPanel(false);
        }
    }
}
