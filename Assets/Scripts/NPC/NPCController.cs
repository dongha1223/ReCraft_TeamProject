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

        private Action _yesAction;
        private Action _noAction;

        // 재대화 상태 — HasChoice NPC에서만 사용
        private bool         _hasChosen;
        private DialogueData _retalkData; // CreateInstance로 생성, OnDestroy에서 해제

        private void Awake()
        {
            // 선택 기록 후 이벤트 발행
            _yesAction = () => { RecordChoice(); _onYesChosen.Invoke(); };
            _noAction  = () => { RecordChoice(); _onNoChosen.Invoke(); };

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

        private void RecordChoice()
        {
            // HasChoice가 없거나, 반복 대화 허용이거나, 이미 선택한 경우 무시
            if (!_dialogueData.HasChoice || _dialogueData.RepeatDialogue || _hasChosen) return;

            _hasChosen  = true;
            _retalkData = ScriptableObject.CreateInstance<DialogueData>();
            _retalkData.InitSingleLine(_dialogueData.NpcName, _dialogueData.RetalkLine);
        }
    }
}
