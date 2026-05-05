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
        [SerializeField, TextArea(2, 4)] private string _yesResponse;
        [SerializeField, TextArea(2, 4)] private string _noResponse;

        [Header("줄별 버튼 텍스트 오버라이드 (비워두면 기본값 사용)")]
        [SerializeField] private string[] _confirmLabels;
        [SerializeField] private string[] _cancelLabels;

        public string   NpcName     => _npcName;
        public string[] Lines       => _lines;
        public bool     HasChoice   => _hasChoice;
        public string   YesResponse => _yesResponse;
        public string   NoResponse  => _noResponse;

        public string GetConfirmLabel(int lineIndex) => GetLabel(_confirmLabels, lineIndex);
        public string GetCancelLabel(int lineIndex)  => GetLabel(_cancelLabels,  lineIndex);

        private static string GetLabel(string[] labels, int lineIndex) =>
            (labels != null && lineIndex < labels.Length && !string.IsNullOrEmpty(labels[lineIndex]))
            ? labels[lineIndex] : null;
    }
}
