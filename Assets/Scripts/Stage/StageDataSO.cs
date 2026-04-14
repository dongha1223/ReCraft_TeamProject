using UnityEngine;

namespace _2D_Roguelike
{
    public enum StageType
    {
        Start,
        Normal,
        Shop,
        Boss
    }

    public enum MapTheme
    {
        None,    // Start / Shop / Boss
        Pride,   // 오만
        Greed,   // 탐욕
        Anger,   // 분노
        Sloth,   // 나태
        Envy,    // 질투
        Gluttony,// 탐식
        Lust     // 색욕
    }

    [System.Serializable]
    public struct RewardData
    {
        [Tooltip("클리어 후 지급할 아이템 선택지 수")]
        public int itemChoiceCount;

        [Tooltip("추가 골드 지급량")]
        public int bonusGold;
    }

    [CreateAssetMenu(menuName = "2D Roguelike/Stage Data", fileName = "StageData")]
    public class StageDataSO : ScriptableObject
    {
        [Header("씬")]
        [Tooltip("로드할 씬 이름 (현재 stageRoots 방식 사용 시 미사용)")]
        public string sceneName;

        [Header("스테이지 종류")]
        public StageType stageType;

        [Tooltip("Normal 스테이지일 때만 유효")]
        public MapTheme mapTheme;

        [Header("보상")]
        public RewardData reward;

        [Header("배경음악")]
        public AudioClip bgm;
    }
}
