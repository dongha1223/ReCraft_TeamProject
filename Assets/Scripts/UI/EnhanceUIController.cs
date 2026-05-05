using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    public class EnhanceUIController : MonoBehaviour
    {
        public static EnhanceUIController Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        [Serializable]
        private struct NodeEntry
        {
            [Tooltip("UXML name 속성 (예: courage-node-0)")]
            public string uiName;
            public EnhanceNodeDefinition definition;
        }

        [SerializeField] private UIDocument  _uiDocument;
        [SerializeField] private NodeEntry[] _nodeEntries;

        [Header("강화 결과 피드백")]
        [SerializeField] private string _successFeedback = "강화되었어요!";
        [SerializeField] private string _failFeedback    = "신념이 부족해!";
        [SerializeField] private string _resetFeedback   = "강화가 초기화되었어요.";

        private VisualElement _enhancePanel;
        private Button        _btnReset;
        private Label         _infoNameLabel;
        private Label         _infoLevelLabel;
        private Label         _infoCurrentDesc;
        private Label         _infoCostAmount;
        private Label         _infoNextDesc;

        private PlayerStatController  _playerStatCtrl;
        private EnhanceNodeDefinition _selectedDef;
        private int                   _selectedNodeIndex = -1;

        // 시작 시 캐싱 — RefreshAllNodeLevels/OnNodeClicked에서 DOM 재쿼리 방지
        private VisualElement[] _nodeElements;
        private Label[]         _nodeLevelLabels;

        private const string SelectedClass  = "node-selected";
        private const string NodeLevelClass = "node-level";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var root = _uiDocument.rootVisualElement;

            _enhancePanel    = root.Q<VisualElement>("enhance-panel");
            _btnReset        = root.Q<Button>("btn-reset");
            _infoNameLabel   = root.Q<Label>("info-name-label");
            _infoLevelLabel  = root.Q<Label>("info-level-label");
            _infoCurrentDesc = root.Q<Label>("info-current-desc");
            _infoCostAmount  = root.Q<Label>("info-cost-amount");
            _infoNextDesc    = root.Q<Label>("info-next-desc");

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                _playerStatCtrl = playerGO.GetComponent<PlayerStatController>();

            ApplyAllSavedModifiers();
            RegisterNodeCallbacks(root);
            Debug.Assert(_btnReset != null, "[EnhanceUI] btn-reset not found in UXML.");
            _btnReset.focusable = false;
            _btnReset.RegisterCallback<PointerUpEvent>(_ => ResetAllUpgrades());
        }

        private void Update()
        {
            if (!IsOpen) return;

            if (!DialogueUIController.IsActive)
            {
                HidePanel();
                return;
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        // ── 공개 API ─────────────────────────────────────────────────

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            ClearSelection();
            _enhancePanel.AddToClassList("panel-visible");
            DialogueUIController.Instance?.ShowBeliefPanel(true);
            DialogueUIController.Instance?.SetActionLabels("강화", "나가기");
            RefreshAllNodeLevels();
            ResetInfoPanel();
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

        /// <summary>강화 UI가 열린 상태에서 Enter/Space 입력 시 DialogueUIController가 호출.</summary>
        public void TryConfirmUpgrade()
        {
            if (_selectedDef == null) return;

            int currentLevel = EnhanceSaveService.GetLevel(_selectedDef.nodeId);
            if (currentLevel >= _selectedDef.maxLevel) return;

            int cost = _selectedDef.GetCost(currentLevel);
            if (!BeliefManager.Instance.TrySpendBelief(cost))
            {
                DialogueUIController.Instance?.ShowEnhanceFeedback(_failFeedback);
                return;
            }

            int newLevel = currentLevel + 1;
            EnhanceSaveService.SetLevel(_selectedDef.nodeId, newLevel);
            ApplyModifiers(_selectedDef, newLevel);
            RefreshAllNodeLevels();
            RefreshInfoPanel(_selectedDef, newLevel);
            DialogueUIController.Instance?.ShowEnhanceFeedback(_successFeedback);
        }

        private void ResetAllUpgrades()
        {
            foreach (var entry in _nodeEntries)
            {
                if (entry.definition == null) continue;
                EnhanceSaveService.SetLevelNoFlush(entry.definition.nodeId, 0);
                ApplyModifiers(entry.definition, 0);
            }
            EnhanceSaveService.FlushSave();

            ClearSelection();
            RefreshAllNodeLevels();
            ResetInfoPanel();
            DialogueUIController.Instance?.ShowEnhanceFeedback(_resetFeedback);
        }

        private void ClearSelection()
        {
            if (_selectedNodeIndex >= 0)
                _nodeElements?[_selectedNodeIndex]?.RemoveFromClassList(SelectedClass);
            _selectedNodeIndex = -1;
            _selectedDef = null;
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void RegisterNodeCallbacks(VisualElement root)
        {
            _nodeElements    = new VisualElement[_nodeEntries.Length];
            _nodeLevelLabels = new Label[_nodeEntries.Length];

            for (int i = 0; i < _nodeEntries.Length; i++)
            {
                var entry = _nodeEntries[i];
                if (entry.definition == null) continue;
                var node = root.Q<VisualElement>(entry.uiName);
                if (node == null) continue;
                _nodeElements[i]    = node;
                _nodeLevelLabels[i] = node.Q<Label>(className: NodeLevelClass);
                int idx = i;
                node.RegisterCallback<ClickEvent>(_ => OnNodeClicked(idx));
            }
        }

        private void OnNodeClicked(int index)
        {
            if (_selectedNodeIndex >= 0)
                _nodeElements[_selectedNodeIndex]?.RemoveFromClassList(SelectedClass);

            _selectedNodeIndex = index;
            _nodeElements[index]?.AddToClassList(SelectedClass);
            _selectedDef = _nodeEntries[index].definition;
            RefreshInfoPanel(_selectedDef);
        }

        private void RefreshInfoPanel(EnhanceNodeDefinition def) =>
            RefreshInfoPanel(def, EnhanceSaveService.GetLevel(def.nodeId));

        private void RefreshInfoPanel(EnhanceNodeDefinition def, int level)
        {
            bool maxed = level >= def.maxLevel;

            _infoNameLabel.text   = def.displayName;
            _infoLevelLabel.text  = $"Lv. {level}/{def.maxLevel}";
            _infoCurrentDesc.text = def.description;
            _infoCostAmount.text  = maxed ? "-" : def.GetCost(level).ToString();
            _infoNextDesc.text    = def.GetNextLevelPreview(level);
        }

        private void ResetInfoPanel()
        {
            _infoNameLabel.text   = "강화 선택";
            _infoLevelLabel.text  = "";
            _infoCurrentDesc.text = "강화할 항목을 선택하세요.";
            _infoCostAmount.text  = "0";
            _infoNextDesc.text    = "";
        }

        private void RefreshAllNodeLevels()
        {
            for (int i = 0; i < _nodeEntries.Length; i++)
            {
                if (_nodeEntries[i].definition == null || _nodeLevelLabels?[i] == null) continue;
                _nodeLevelLabels[i].text = EnhanceSaveService.GetLevel(_nodeEntries[i].definition.nodeId).ToString();
            }
        }

        private void ApplyAllSavedModifiers()
        {
            foreach (var entry in _nodeEntries)
            {
                if (entry.definition == null) continue;
                ApplyModifiers(entry.definition, EnhanceSaveService.GetLevel(entry.definition.nodeId));
            }
        }

        private void ApplyModifiers(EnhanceNodeDefinition def, int level)
        {
            if (_playerStatCtrl == null || def.statModifiers == null) return;
            var svc = _playerStatCtrl.StatService;

            svc.RemoveAllModifiersFromSource(def.SourceId);
            if (level <= 0) return;

            foreach (var spec in def.statModifiers)
            {
                float value = spec.operation == ModifierOperation.Multiply
                    ? 1f + level * spec.valuePerLevel
                    : level * spec.valuePerLevel;
                svc.AddModifier(def.SourceId, spec.stat, spec.operation, value);
            }
        }

        private void HidePanel()
        {
            ClearSelection();
            IsOpen = false;
            _enhancePanel.RemoveFromClassList("panel-visible");
            DialogueUIController.Instance?.ShowBeliefPanel(false);
        }
    }
}
