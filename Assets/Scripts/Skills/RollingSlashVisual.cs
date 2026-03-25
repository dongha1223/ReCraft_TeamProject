using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 롤링 슬레쉬 참격 이펙트
    /// - 가로 타원 링 (width > height, 90도 기울인 형태)
    /// - ovalSize.x = 가로(긴 축), ovalSize.y = 세로(짧은 축)
    /// - 푸른빛 코어 + 하늘색 글로우
    /// - 확장 후 페이드아웃
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RollingSlashVisual : MonoBehaviour
    {
        public void Initialize(Vector2 ovalSize, float dirSign, float fadeDuration = 0.42f)
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            // ── 가로 타원 메시 생성 ──────────────────────────────────
            // halfW = 가로(긴축), halfH = 세로(짧은축)
            // ovalSize.x 가 가로이므로 그대로 halfW에 대입
            float halfW = ovalSize.x * 0.5f;   // 긴 축 (가로)
            float halfH = ovalSize.y * 0.5f;   // 짧은 축 (세로)

            mf.mesh = BuildOvalRingMesh(halfW, halfH, 0.22f, 48);

            mr.material     = new Material(Shader.Find("Sprites/Default"))
                              { color = new Color(0.20f, 0.65f, 1.00f, 0.95f) };
            mr.sortingOrder = 5;

            // ── 글로우 레이어 ────────────────────────────────────────
            var glowGO = new GameObject("SlashGlow");
            glowGO.transform.SetParent(transform, false);
            glowGO.transform.localScale = Vector3.one * 1.18f;

            var glowMf = glowGO.AddComponent<MeshFilter>();
            var glowMr = glowGO.AddComponent<MeshRenderer>();
            glowMf.mesh = BuildOvalRingMesh(halfW, halfH, 0.30f, 48);
            glowMr.material     = new Material(Shader.Find("Sprites/Default"))
                                  { color = new Color(0.50f, 0.85f, 1.00f, 0.45f) };
            glowMr.sortingOrder = 4;

            StartCoroutine(PlayEffect(mr, glowMr, fadeDuration));
        }

        private IEnumerator PlayEffect(MeshRenderer mr, MeshRenderer glowMr, float dur)
        {
            if (this == null) yield break;

            Material matCore = mr != null ? mr.material : null;
            Material matGlow = glowMr != null ? glowMr.material : null;
            if (matCore == null) yield break;

            float coreAlpha = matCore.color.a;
            float glowAlpha = matGlow != null ? matGlow.color.a : 0f;

            transform.localScale = Vector3.one * 0.55f;

            float t = 0f;
            while (t < dur)
            {
                if (this == null || gameObject == null) yield break;

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);

                if (matCore != null)
                    matCore.color = new Color(matCore.color.r, matCore.color.g, matCore.color.b,
                                              Mathf.Lerp(coreAlpha, 0f, p));
                if (matGlow != null)
                    matGlow.color = new Color(matGlow.color.r, matGlow.color.g, matGlow.color.b,
                                              Mathf.Lerp(glowAlpha, 0f, p));

                float scale = Mathf.Lerp(0.55f, 1.6f, Mathf.SmoothStep(0f, 1f, p));
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            if (this != null && gameObject != null)
                Destroy(gameObject);
        }

        /// <summary>
        /// 타원 링(고리) 메시 빌더
        /// halfW = 가로 반지름 (X축 방향, 긴 축)
        /// halfH = 세로 반지름 (Y축 방향, 짧은 축)
        /// halfW > halfH → 가로로 납작한 타원 (눕힌 형태)
        /// </summary>
        private static Mesh BuildOvalRingMesh(float halfW, float halfH,
                                               float thickness, int segments)
        {
            // 바깥 타원: 두께만큼 확장
            float owx = halfW + thickness;
            float ohy = halfH + thickness;
            // 안쪽 타원: 두께만큼 축소 (최소 0.05 보장)
            float iwx = Mathf.Max(halfW - thickness * 0.5f, 0.05f);
            float ihy = Mathf.Max(halfH - thickness * 0.5f, 0.05f);

            var verts = new Vector3[2 * segments];
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                // 바깥: X = owx(가로), Y = ohy(세로)
                verts[i]            = new Vector3(owx * c, ohy * s, 0f);
                // 안쪽: X = iwx(가로), Y = ihy(세로)
                verts[segments + i] = new Vector3(iwx * c, ihy * s, 0f);
            }

            var tris = new List<int>(segments * 6);
            for (int i = 0; i < segments; i++)
            {
                int oa = i,            ob = (i + 1) % segments;
                int ia = segments + i, ib = segments + (i + 1) % segments;
                tris.Add(oa); tris.Add(ob); tris.Add(ia);
                tris.Add(ob); tris.Add(ib); tris.Add(ia);
            }

            var mesh = new Mesh { name = "OvalRing_Horizontal" };
            mesh.vertices  = verts;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
