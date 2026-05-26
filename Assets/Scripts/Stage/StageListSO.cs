using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 전체 100층 스테이지 데이터 목록. 인덱스 = 층 번호 - 1.
    /// </summary>
    [CreateAssetMenu(menuName = "2D Roguelike/Stage List", fileName = "StageList")]
    public class StageListSO : ScriptableObject
    {
        [Tooltip("인덱스 0 = 1층, 인덱스 99 = 100층")]
        public StageDataSO[] stages;

        public StageDataSO Get(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= stages.Length) return null;
            return stages[floorIndex];
        }

        public int Count => stages?.Length ?? 0;
    }
}
