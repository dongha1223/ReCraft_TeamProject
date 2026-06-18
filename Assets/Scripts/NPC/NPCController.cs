using System;
using UnityEngine;
using UnityEngine.Events;

namespace _2D_Roguelike
{
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueData _dialogueData;
        [SerializeField] private GameObject   _fKeyPrompt;

        [Header("선택지 이벤트 (DialogueData.HasChoice 활성화 시 사용)")]
        [SerializeField] private UnityEvent _onYesChosen;
        [SerializeField] private UnityEvent _onNoChosen;

        public bool CanInteract => !UIState.IsBlockingInput;

        private Action                _yesAction;
        private Action                _noAction;
        private PlayerStatController  _cachedStatController;

        // 재대화 상태 — HasChoice NPC에서만 사용
        private bool         _hasChosen;
        private bool         _choseYes;   // 선행(true) / 악행(false) 기록
        private DialogueData _retalkData; // CreateInstance로 생성, OnDestroy에서 해제

        private void Awake()
        {
            // 선택 기록 + 버프 적용 + 이벤트 발행
            _yesAction = () => { ApplyDialogueEffects(_dialogueData.YesEffects); RecordChoice(true);  _onYesChosen.Invoke(); };
            _noAction  = () => { ApplyDialogueEffects(_dialogueData.NoEffects);  RecordChoice(false); _onNoChosen.Invoke(); };

            // Inspector에서 미할당 시 자식 오브젝트에서 자동 탐색
            if (_fKeyPrompt == null)
            {
                var child = transform.Find("FKeyPrompt");
                if (child != null) _fKeyPrompt = child.gameObject;
            }
        }

        private void OnDestroy()
        {
            if (_retalkData != null) Destroy(_retalkData);
        }

        private void OnEnable() => _fKeyPrompt?.SetActive(false);

        public void OnFocused()   => _fKeyPrompt?.SetActive(true);
        public void OnUnfocused() => _fKeyPrompt?.SetActive(false);

        public void OnInteract(PlayerStatController statController)
        {
            if (_dialogueData == null) return;
            _fKeyPrompt?.SetActive(false);

            // 버프 적용을 위해 statController 캐싱
            _cachedStatController = statController;

            if (_hasChosen)
            {
                // 선택 완료 후 재대화: 선택한 반응 텍스트만 표시
                DialogueUIController.Instance?.StartDialogue(_retalkData);
            }
            else
            {
                DialogueUIController.Instance?.StartDialogue(
                    _dialogueData, _yesAction, _noAction);
            }
        }

        /// <summary>
        /// 선택 시 DialogueData에 등록된 StatModifierEffectDefinition 목록을 StatService에 적용.
        /// sourceId = "NPC_{gameObject.name}_{yes/no}" 로 고유하게 식별.
        /// </summary>
        private void ApplyDialogueEffects(StatModifierEffectDefinition[] effects)
        {
            if (effects == null || effects.Length == 0) return;
            if (_cachedStatController == null)
            {
                Debug.LogWarning($"[NPCController] {gameObject.name}: statController가 null — 버프를 적용할 수 없습니다.");
                return;
            }

            var statService = _cachedStatController.StatService;
            foreach (var effect in effects)
            {
                if (effect == null) continue;
                // sourceId: NPC 이름 + effectId 조합으로 고유성 보장
                string sourceId = $"NPC_{gameObject.name}_{effect.effectId}";
                statService.AddModifier(sourceId, effect.statType, effect.operation, effect.value);
            }
        }

        private void RecordChoice(bool choseYes)
        {
            // HasChoice가 없거나, 반복 대화 허용이거나, 이미 선택한 경우 무시
            if (!_dialogueData.HasChoice || _dialogueData.RepeatDialogue || _hasChosen) return;

            _hasChosen = true;
            _choseYes  = choseYes;

            // 선행/악행 분기 재대화 배열 결정
            string[] retalkLines = choseYes
                ? _dialogueData.YesRetalkLines
                : _dialogueData.NoRetalkLines;

            _retalkData = ScriptableObject.CreateInstance<DialogueData>();

            if (retalkLines != null && retalkLines.Length > 0)
                // 분기별 배열이 있으면 다중 대사로 초기화
                _retalkData.InitRetalkLines(_dialogueData.NpcName, retalkLines);
            else
                // 없으면 기존 단일 RetalkLine 폴백
                _retalkData.InitSingleLine(_dialogueData.NpcName, _dialogueData.RetalkLine);
        }
    }
}
