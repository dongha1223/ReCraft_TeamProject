using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 미니맵 아이콘 컴포넌트
    /// - Awake 시 코드로 원형 스프라이트를 생성해서 SpriteRenderer에 적용
    /// - 레이어를 "Minimap"(6)으로 자동 설정
    /// - iconColor로 색상 지정 (Inspector에서 조정 가능)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MinimapIcon : MonoBehaviour
    {
        [Tooltip("미니맵에 표시될 아이콘 색상")]
        [SerializeField] private Color iconColor = Color.red;

        [Tooltip("아이콘 크기 (픽셀 해상도)")]
        [SerializeField] private int texSize = 16;

        private void Awake()
        {
            // ── Minimap 레이어 설정 ───────────────────────────────────
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer >= 0)
                gameObject.layer = minimapLayer;
            else
                Debug.LogWarning("[MinimapIcon] 'Minimap' 레이어가 없습니다.");

            // ── 원형 스프라이트 생성 → SpriteRenderer에 적용 ─────────
            var sr = GetComponent<SpriteRenderer>();
            sr.sprite       = CreateCircleSprite(texSize, iconColor);
            sr.color        = Color.white;   // 스프라이트 자체에 색상이 있으므로 흰색
            sr.sortingOrder = 10;
        }

        /// <summary>
        /// texSize × texSize 픽셀의 원형 Texture2D를 만들어 Sprite로 반환
        /// </summary>
        private static Sprite CreateCircleSprite(int size, Color col)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float half = size * 0.5f;
            float r    = half - 0.5f;   // 테두리 1px 여유

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx   = x - half + 0.5f;
                    float dy   = y - half + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= r)
                    {
                        // 안티앨리어싱: 가장자리 0.5px 부드럽게
                        float alpha = Mathf.Clamp01(r - dist + 0.5f);
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size   // pixels per unit = texSize → 월드에서 1×1 유닛 크기
            );
        }
    }
}
