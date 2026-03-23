using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 롤링 슬레쉬 참격 이펙트
    /// - 가로로 넓은 타원 고리 (너비 > 높이)
    /// - 두께 2배 적용 (thickness = 0.22f)
    /// - 푸른빛 색상 + 빠른 확장 후 페이드아웃
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RollingSlashVisual : MonoBehaviour
    {
        /// <summary>
        /// 이펙트 초기화 및 재생
        /// </summary>
        /// <param name="ovalSize">타원의 (width, height) — width > height 이면 가로형</param>
        /// <param name="dirSign">진행 방향 부호</param>
        /// <param name="fadeDuration">페이드아웃 시간</param>
        public void Initialize(Vector2 ovalSize, float dirSign, float fadeDuration = 0.42f)
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            // ── 가로 타원 링 메시 (두께 2배: 0.22f) ─────────────────
            mf.mesh = BuildOvalRingMesh(
                halfW    : ovalSize.x * 0.5f,
                halfH    : ovalSize.y * 0.5f,
                thickness: 0.22f,                // ← 원래 0.11의 2배
                segments : 48
            );

            // 푸른빛 색상
            var mat = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.20f, 0.65f, 1.00f, 0.95f)
            };
            mr.material     = mat;
            mr.sortingOrder = 5;

            // 글로우 레이어 (더 밝은 하늘색, 약간 더 큼)
            var glowGO = new GameObject("SlashGlow");
            glowGO.transform.SetParent(transform, false);
            glowGO.transform.localPosition = Vector3.zero;
            glowGO.transform.localScale    = Vector3.one * 1.18f;

            var glowMf = glowGO.AddComponent<MeshFilter>();
            var glowMr = glowGO.AddComponent<MeshRenderer>();

            glowMf.mesh = BuildOvalRingMesh(
                halfW    : ovalSize.x * 0.5f,
                halfH    : ovalSize.y * 0.5f,
                thickness: 0.30f,
                segments : 48
            );
            var glowMat = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.50f, 0.85f, 1.00f, 0.45f)
            };
            glowMr.material     = glowMat;
            glowMr.sortingOrder = 4;

            StartCoroutine(PlayEffect(mr, glowMr, fadeDuration));
        }

        // ── 확장 + 페이드아웃 애니메이션 ─────────────────────────────
        private IEnumerator PlayEffect(MeshRenderer mr, MeshRenderer glowMr, float dur)
        {
            var   matCore = mr.material;
            var   matGlow = glowMr.material;
            float t       = 0f;

            float coreAlpha = matCore.color.a;
            float glowAlpha = matGlow.color.a;

            // 초기 스케일 (임팩트 느낌)
            transform.localScale = Vector3.one * 0.55f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;

                // 코어 페이드
                float ca = Mathf.Lerp(coreAlpha, 0f, p);
                matCore.color = new Color(matCore.color.r, matCore.color.g,
                                           matCore.color.b, ca);

                // 글로우 페이드
                float ga = Mathf.Lerp(glowAlpha, 0f, p);
                matGlow.color = new Color(matGlow.color.r, matGlow.color.g,
                                           matGlow.color.b, ga);

                // 빠르게 확장 후 안정
                float scale = Mathf.Lerp(0.55f, 1.6f, Mathf.SmoothStep(0f, 1f, p));
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            Destroy(gameObject);
        }

        // ── 가로 타원 링 메시 빌더 ────────────────────────────────────
        /// <summary>
        /// 바깥 타원과 안쪽 타원 사이 두꺼운 고리 메시 생성
        /// </summary>
        private static Mesh BuildOvalRingMesh(float halfW, float halfH,
                                               float thickness, int segments)
        {
            // 바깥 타원 (더 두꺼운 외곽)
            float owx = halfW + thickness;
            float ohy = halfH + thickness;
            // 안쪽 타원
            float iwx = Mathf.Max(halfW - thickness * 0.5f, 0.05f);
            float ihy = Mathf.Max(halfH - thickness * 0.5f, 0.05f);

            var verts = new Vector3[2 * segments];
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i]            = new Vector3(owx * c, ohy * s, 0f);
                verts[segments + i] = new Vector3(iwx * c, ihy * s, 0f);
            }

            var tris = new List<int>(segments * 6);
            for (int i = 0; i < segments; i++)
            {
                int oa = i,             ob = (i + 1) % segments;
                int ia = segments + i,  ib = segments + (i + 1) % segments;
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
