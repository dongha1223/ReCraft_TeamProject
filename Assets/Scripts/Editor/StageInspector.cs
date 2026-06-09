using UnityEngine;
using UnityEditor;
using System.Text;

namespace _2D_Roguelike
{
    public static class StageInspector
    {
        [MenuItem("Tools/2D Roguelike/Inspect Stages")]
        public static void InspectStages()
        {
            // ##--STAGES--## 찾기 (비활성 포함)
            GameObject stagesRoot = null;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "##--STAGES--##" && t.parent == null)
                {
                    stagesRoot = t.gameObject;
                    break;
                }
                if (t.name == "##--STAGES--##")
                {
                    stagesRoot = t.gameObject;
                    break;
                }
            }

            if (stagesRoot == null)
            {
                Debug.LogError("[StageInspector] ##--STAGES--## not found");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== STAGES ({stagesRoot.transform.childCount} children) ===");

            foreach (Transform stage in stagesRoot.transform)
            {
                var prefabType   = PrefabUtility.GetPrefabAssetType(stage.gameObject);
                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(stage.gameObject);
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(stage.gameObject);

                sb.AppendLine($"\n[{(stage.gameObject.activeSelf ? "ON" : "OFF")}] {stage.name}");
                sb.AppendLine($"  PrefabType={prefabType}, Status={prefabStatus}");
                sb.AppendLine($"  PrefabPath={prefabPath}");
                sb.AppendLine($"  Children={stage.childCount}");
                foreach (Transform child in stage)
                    sb.AppendLine($"    - {child.name} (active={child.gameObject.activeSelf})");
            }

            Debug.Log(sb.ToString());
        }
    }
}
