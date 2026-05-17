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

        [Header("재대화 — 선택 완료 후 다시 말을 걸었을 때 출력할 대사")]
        [SerializeField, TextArea(2, 4)] private string _retalkLine;

        [Header("선택지 버튼 텍스트 (비워두면 '예' / '아니오' 사용)")]
        [SerializeField] private string _choiceConfirmLabel;
        [SerializeField] private string _choiceNoLabel;

        [Header("줄별 버튼 텍스트 오버라이드 (비워두면 기본값 사용)")]
        [SerializeField] private string[] _confirmLabels;
        [SerializeField] private string[] _cancelLabels;

        public string   NpcName            => _npcName;
        public string[] Lines              => _lines;
        public bool     HasChoice          => _hasChoice;
        public bool     RepeatDialogue     => _repeatDialogue;
        public string   YesResponse        => _yesResponse;
        public string   NoResponse         => _noResponse;
        public string   RetalkLine         => _retalkLine;
        public string   ChoiceConfirmLabel => string.IsNullOrEmpty(_choiceConfirmLabel) ? "예"     : _choiceConfirmLabel;
        public string   ChoiceNoLabel      => string.IsNullOrEmpty(_choiceNoLabel)      ? "아니오" : _choiceNoLabel;

        public string GetConfirmLabel(int lineIndex) => GetLabel(_confirmLabels, lineIndex);
        public string GetCancelLabel(int lineIndex)  => GetLabel(_cancelLabels,  lineIndex);

        private static string GetLabel(string[] labels, int lineIndex) =>
            (labels != null && lineIndex < labels.Length && !string.IsNullOrEmpty(labels[lineIndex]))
            ? labels[lineIndex] : null;

        /// <summary>재대화용 단일 줄 데이터를 런타임에 초기화한다 (CreateInstance 후 즉시 호출).</summary>
        public void InitSingleLine(string npcName, string line)
        {
            _npcName   = npcName;
            _lines     = new[] { line };
            _hasChoice = false;
        }
    }
}
