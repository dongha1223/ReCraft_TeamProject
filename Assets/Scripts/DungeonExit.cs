
using UnityEngine;

/// <summary>
/// 던전 출구 트리거. 플레이어가 닿으면 다음 씬으로 이동합니다.
/// 타일맵 위 투명 오브젝트에 BoxCollider2D + 이 스크립트를 붙이세요.
/// </summary>
public class DungeonExit : MonoBehaviour
{
    [Header("Exit Settings")]
    public int targetStage = 0; // 0 = 다음 스테이지 자동, 1~4 = 특정 스테이지
    public string playerTag = "Player";

    [Header("Visual")]
    public Color gizmoColor = new Color(0f, 1f, 0.5f, 0.4f);
    public Vector2 gizmoSize = new Vector2(1f, 2f);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        TriggerExit();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        TriggerExit();
    }

    void TriggerExit()
    {
        if (DungeonManager.Instance == null)
        {
            Debug.LogWarning("[DungeonExit] DungeonManager를 찾을 수 없습니다!");
            return;
        }
        if (targetStage == 0)
            DungeonManager.Instance.GoToNextStage();
        else
            DungeonManager.Instance.GoToStage(targetStage);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(gizmoSize.x, gizmoSize.y, 0.1f));
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(gizmoSize.x, gizmoSize.y, 0.1f));
    }
}
