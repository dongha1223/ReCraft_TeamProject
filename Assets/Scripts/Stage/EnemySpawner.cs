using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// StageRoot에 부착. 스테이지가 활성화될 때 하위 EnemyStats를 StageManager에 등록.
    /// 재방문 시 죽어서 비활성화된 적도 복구(ResetStats)하여 다시 등록한다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        private void OnEnable()
        {
            if (StageManager.Instance == null) return;

            // includeInactive: true — 이전 방문에서 사망해 비활성화된 적도 포함
            var enemies = GetComponentsInChildren<EnemyStats>(includeInactive: true);
            foreach (var e in enemies)
            {
                // 비활성 상태(사망)였다면 복구
                if (!e.gameObject.activeSelf)
                    e.gameObject.SetActive(true);

                e.ResetStats();
                StageManager.Instance.RegisterEnemy();
            }
        }
    }
}
