using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 롤링 슬레쉬 참격 이펙트
    /// 수정 사항:
    ///   - 코루틴 실행 중 오브젝트 파괴 시 에러 방지 (null 체크)
    ///   - this == null 조기 탈출
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RollingSlashVisual : MonoBehaviour
    {
        public void Initialize(Vector2 ovalSize, float dirSign, float fadeDuration = 0.42f)
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            mf.mesh = BuildOvalRingMesh(ovalSize.x * 0.5f, ovalSize.y * 0.5f, 0.22f, 48);

            mr.material     = new Material(Shader.Find("Sprites/Default"))
                              { color = new Color(0.20f, 0.65f, 1.00f, 0.95f) };
            mr.sortingOrder = 5;

            // 글로우 레이어
            var glowGO = new GameObject("SlashGlow");
            glowGO.transform.SetParent(transform, false);
            glowGO.transform.localScale = Vector3.one * 1.18f;

            var glowMf = glowGO.AddComponent<MeshFilter>();
            var glowMr = glowGO.AddComponent<MeshRenderer>();
            glowMf.mesh = BuildOvalRingMesh(ovalSize.x * 0.5f, ovalSize.y * 0.5f, 0.30f, 48);
            glowMr.material     = new Material(Shader.Find("Sprites/Default"))
                                  { color = new Color(0.50f, 0.85f, 1.00f, 0.45f) };
            glowMr.sortingOrder = 4;

            StartCoroutine(PlayEffect(mr, glowMr, fadeDuration));
        }

        private IEnumerator PlayEffect(MeshRenderer mr, MeshRenderer glowMr, float dur)
        {
            if (this == null) yield break;

            // 재료를 로컬 변수에 보관 — 오브젝트 파괴 후에도 안전
            Material matCore = mr != null ? mr.material : null;
            Material matGlow = glowMr != null ? glowMr.material : null;

            if (matCore == null) yield break;

            float coreAlpha = matCore.color.a;
            float glowAlpha = matGlow != null ? matGlow.color.a : 0f;

            transform.localScale = Vector3.one * 0.55f;

            float t = 0f;
            while (t < dur)
            {
                // 오브젝트가 파괴됐으면 중단
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

        private static Mesh BuildOvalRingMesh(float halfW, float halfH,
                                               float thickness, int segments)
        {
            float owx = halfW + thickness;
            float ohy = halfH + thickness;
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
