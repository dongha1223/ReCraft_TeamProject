using UnityEditor;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 레이어 이름 확인 + PlayerSkill/PlayerAttack EnemyLayer 자동 재설정 도구
    /// Window > ReCraft > Check Layers
    /// </summary>
    public static class LayerChecker
    {
        [MenuItem("Window/ReCraft/Check Layers")]
        public static void CheckLayers()
        {
            Debug.Log("=== Layer Check ===");
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    Debug.Log($"  Layer {i} = \"{name}\"");
            }

            // Player 오브젝트의 컴포넌트 레이어 마스크 자동 설정
            var player = GameObject.FindWithTag("Player");
            if (player == null) { Debug.LogWarning("Player 태그 오브젝트 없음"); return; }

            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex < 0)
            {
                // "Enemy" 이름이 없으면 인덱스 11로 직접 설정
                enemyLayerIndex = 11;
                Debug.LogWarning($"'Enemy' 레이어 없음 → 인덱스 11로 강제 설정");
            }

            int mask = 1 << enemyLayerIndex;
            Debug.Log($"Enemy 레이어 인덱스={enemyLayerIndex}, 마스크 비트={mask}");

            // PlayerSkill 레이어 설정
            var skill = player.GetComponent<FormSkillController>();
            if (skill != null)
            {
                var so   = new UnityEditor.SerializedObject(skill);
                var prop = so.FindProperty("_enemyLayer");
                prop.intValue = mask;
                so.ApplyModifiedProperties();
                Debug.Log($"PlayerSkill._enemyLayer = {mask} 설정 완료");
            }

            // PlayerAttack 레이어 설정
            var attack = player.GetComponent<PlayerAttack>();
            if (attack != null)
            {
                var so   = new UnityEditor.SerializedObject(attack);
                var prop = so.FindProperty("_enemyLayer");
                prop.intValue = mask;
                so.ApplyModifiedProperties();
                Debug.Log($"PlayerAttack._enemyLayer = {mask} 설정 완료");
            }

            UnityEditor.EditorUtility.SetDirty(player);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("=== 레이어 설정 완료 ===");
        }
    }
}
