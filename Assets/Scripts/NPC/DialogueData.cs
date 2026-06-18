using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "DialogueData", menuName = "2D Roguelike/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] private string _npcName = "NPC";
        [SerializeField, TextArea(2, 6)] private string[] _lines;

        [Header("선택지 — 마지막 대사를 예/아니오 질문으로 사용할 때 체크")]
        [SerializeField] private bool   _hasChoice;
        [Tooltip("체크 시 선택 후에도 매번 전체 대화를 다시 출력합니다.")]
        [SerializeField] private bool   _repeatDialogue;
        [SerializeField, TextArea(2, 4)] private string _yesResponse;
        [SerializeField, TextArea(2, 4)] private string _noResponse;

        [Header("선택 후 루트 대사 — 선택 직후 NPC가 순차 출력할 대사 (비워두면 위의 Yes/No Response 사용)")]
        [SerializeField, TextArea(2, 4)] private string[] _yesLines;
        [SerializeField, TextArea(2, 4)] private string[] _noLines;

        [Header("재대화 — 선택 완료 후 다시 말을 걸었을 때 출력할 대사")]
        [Tooltip("선행(Yes) 선택 후 재대화 대사 목록. 비워두면 Retalk Line 사용.")]
        [SerializeField, TextArea(2, 4)] private string[] _yesRetalkLines;
        [Tooltip("악행(No) 선택 후 재대화 대사 목록. 비워두면 Retalk Line 사용.")]
        [SerializeField, TextArea(2, 4)] private string[] _noRetalkLines;
        [Tooltip("선행/악행 구분 없이 단일 재대화 대사 (위 두 항목이 비어있을 때 폴백)")]
        [SerializeField, TextArea(2, 4)] private string _retalkLine;

        [Header("선택지 버튼 텍스트 (비워두면 '예' / '아니오' 사용)")]
        [SerializeField] private string _choiceConfirmLabel;
        [SerializeField] private string _choiceNoLabel;

        [Header("줄별 버튼 텍스트 오버라이드 (비워두면 기본값 사용)")]
        [SerializeField] private string[] _confirmLabels;
        [SerializeField] private string[] _cancelLabels;

        [Header("선택지 버프 — 선택 시 플레이어에게 영구 적용되는 스탯 효과")]
        [Tooltip("'예(선행)' 선택 시 적용할 StatModifierEffectDefinition 목록")]
        [SerializeField] private StatModifierEffectDefinition[] _yesEffects;
        [Tooltip("'아니오(악행)' 선택 시 적용할 StatModifierEffectDefinition 목록")]
        [SerializeField] private StatModifierEffectDefinition[] _noEffects;

        public string   NpcName            => _npcName;
        public string[] Lines              => _lines;
        public bool     HasChoice          => _hasChoice;
        public bool     RepeatDialogue     => _repeatDialogue;
        public string   YesResponse        => _yesResponse;
        public string   NoResponse         => _noResponse;
        public string   RetalkLine         => _retalkLine;
        public string   ChoiceConfirmLabel => string.IsNullOrEmpty(_choiceConfirmLabel) ? "예"     : _choiceConfirmLabel;
        public string   ChoiceNoLabel      => string.IsNullOrEmpty(_choiceNoLabel)      ? "아니오" : _choiceNoLabel;

        // 선택 후 루트 대사 — 배열이 비어있으면 기존 단일 Response로 폴백
        public string[] YesLines => (_yesLines != null && _yesLines.Length > 0) ? _yesLines : null;
        public string[] NoLines  => (_noLines  != null && _noLines.Length  > 0) ? _noLines  : null;

        // 재대화 분기 — 배열이 비어있으면 _retalkLine으로 폴백
        public string[] YesRetalkLines => (_yesRetalkLines != null && _yesRetalkLines.Length > 0) ? _yesRetalkLines : null;
        public string[] NoRetalkLines  => (_noRetalkLines  != null && _noRetalkLines.Length  > 0) ? _noRetalkLines  : null;

        public StatModifierEffectDefinition[] YesEffects => _yesEffects;
        public StatModifierEffectDefinition[] NoEffects  => _noEffects;

        public string GetConfirmLabel(int lineIndex) => GetLabel(_confirmLabels, lineIndex);
        public string GetCancelLabel(int lineIndex)  => GetLabel(_cancelLabels,  lineIndex);

        private static string GetLabel(string[] labels, int lineIndex) =>
            (labels != null && lineIndex < labels.Length && !string.IsNullOrEmpty(labels[lineIndex]))
            ? labels[lineIndex] : null;

        public void InitSingleLine(string npcName, string line)
        {
            _npcName   = npcName;
            _lines     = new[] { line };
            _hasChoice = false;
        }

        /// <summary>재대화용 다중 대사 초기화 (NPCController.RecordChoice에서 런타임 생성 시 사용)</summary>
        public void InitRetalkLines(string npcName, string[] lines)
        {
            _npcName   = npcName;
            _lines     = lines;
            _hasChoice = false;
        }
    }
}
