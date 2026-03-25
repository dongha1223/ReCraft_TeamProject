using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 롤링 슬레쉬 참격 이펙트
    /// - 타원 링 메시 + 글로우 레이어
    /// - 페이드 아웃 + 확대 애니메이션
    /// - SkillObjectPool 기반 풀링: Destroy 대신 풀 반환
    /// - 글로우 자식 오브젝트 최초 1회 생성 후 재사용
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RollingSlashVisual : MonoBehaviour
    {
        private MeshFilter   _mf;
        private MeshRenderer _coreMr;
        private Material     _coreMat;

        private GameObject   _glowGO;
        private MeshRenderer _glowMr;
        private Material     _glowMat;

        private void Awake()
        {
            _mf     = GetComponent<MeshFilter>();
            _coreMr = GetComponent<MeshRenderer>();
        }

        public void Initialize(Vector2 ovalSize, float dirSign, float fadeDuration = 0.42f)
        {
            // 코어 메시
            _mf.mesh = BuildOvalRingMesh(ovalSize.x * 0.5f, ovalSize.y * 0.5f, 0.22f, 48);

            // 코어 재질: 최초 1회 생성, 이후 색상만 재설정
            if (_coreMat == null)
            {
                _coreMat              = new Material(Shader.Find("Sprites/Default"));
                _coreMr.sharedMaterial = _coreMat;
                _coreMr.sortingOrder   = 5;
            }
            _coreMat.color  = new Color(0.20f, 0.65f, 1.00f, 0.95f);
            _coreMr.enabled = true;

            // 글로우 자식: 최초 1회 생성, 이후 재사용
            if (_glowGO == null)
            {
                _glowGO = new GameObject("SlashGlow");
                _glowGO.transform.SetParent(transform, false);
                _glowGO.AddComponent<MeshFilter>();
                _glowMr                    = _glowGO.AddComponent<MeshRenderer>();
                _glowMat                   = new Material(Shader.Find("Sprites/Default"));
                _glowMr.sharedMaterial     = _glowMat;
                _glowMr.sortingOrder       = 4;
            }

            _glowGO.transform.localScale = Vector3.one * 1.18f;
            _glowGO.GetComponent<MeshFilter>().mesh =
                BuildOvalRingMesh(ovalSize.x * 0.5f, ovalSize.y * 0.5f, 0.30f, 48);
            _glowMat.color  = new Color(0.50f, 0.85f, 1.00f, 0.45f);
            _glowMr.enabled = true;

            transform.localScale = Vector3.one * 0.55f;

            StartCoroutine(PlayEffect(fadeDuration));
        }

        private IEnumerator PlayEffect(float dur)
        {
            if (this == null) yield break;
            if (_coreMat == null) yield break;

            float coreAlpha = _coreMat.color.a;
            float glowAlpha = _glowMat != null ? _glowMat.color.a : 0f;

            float t = 0f;
            while (t < dur)
            {
                if (this == null || gameObject == null) yield break;

                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);

                if (_coreMat != null)
                    _coreMat.color = new Color(_coreMat.color.r, _coreMat.color.g, _coreMat.color.b,
                                               Mathf.Lerp(coreAlpha, 0f, p));

                if (_glowMat != null)
                    _glowMat.color = new Color(_glowMat.color.r, _glowMat.color.g, _glowMat.color.b,
                                               Mathf.Lerp(glowAlpha, 0f, p));

                float scale = Mathf.Lerp(0.55f, 1.6f, Mathf.SmoothStep(0f, 1f, p));
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            if (this == null || gameObject == null) yield break;

            // 이펙트 완료 → 풀로 반환
            if (SkillObjectPool.Instance != null)
                SkillObjectPool.Instance.ReturnSlashVFX(this);
            else
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
