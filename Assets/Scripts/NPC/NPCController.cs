using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// NPC 상호작용 컨트롤러.
    /// 플레이어 근접 시 F키 프롬프트를 표시하고, 상호작용 시 대화를 시작한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueData _dialogueData;
        [SerializeField] private GameObject   _fKeyPrompt;

        public bool CanInteract => !DialogueUIController.IsActive;

        private void Awake()
        {
            // Inspector에서 미할당 시 자식 오브젝트에서 자동 탐색
            if (_fKeyPrompt == null)
            {
                var child = transform.Find("FKeyPrompt");
                if (child != null) _fKeyPrompt = child.gameObject;
            }
        }

        private void OnEnable() => _fKeyPrompt?.SetActive(false);

        public void OnFocused()   => _fKeyPrompt?.SetActive(true);
        public void OnUnfocused() => _fKeyPrompt?.SetActive(false);

        public void OnInteract(PlayerStatController statController)
        {
            if (_dialogueData == null) return;
            _fKeyPrompt?.SetActive(false);
            DialogueUIController.Instance?.StartDialogue(_dialogueData);
        }
    }
}
