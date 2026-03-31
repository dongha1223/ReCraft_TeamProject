using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    public static class ColliderDebug
    {
        [MenuItem("Tools/[Debug] Print Player Collider")]
        public static void Print()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) { Debug.LogError("Player 없음"); return; }

            var col = player.GetComponent<BoxCollider2D>();
            if (col == null) { Debug.LogError("BoxCollider2D 없음"); return; }

            Debug.Log($"[Collider] localScale   = {player.transform.localScale}");
            Debug.Log($"[Collider] offset       = {col.offset}");
            Debug.Log($"[Collider] size         = {col.size}");
            Debug.Log($"[Collider] bounds.min.y = {col.bounds.min.y}");
            Debug.Log($"[Collider] bounds.max.y = {col.bounds.max.y}");
            Debug.Log($"[Collider] position.y   = {player.transform.position.y}");

            // SpriteRenderer bounds도 확인
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr?.sprite != null)
            {
                Debug.Log($"[Sprite]   sprite name   = {sr.sprite.name}");
                Debug.Log($"[Sprite]   sprite bounds = {sr.sprite.bounds}");
                Debug.Log($"[Sprite]   sr.bounds     = {sr.bounds}");
            }
        }
    }
}
