using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스테이지 루트에 부착. OnEnable/OnDisable 시 배경 스프라이트를 교체/복원한다.
    /// </summary>
    public class StageBackgroundSetter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _backgroundRenderers;
        [SerializeField] private Sprite _stageSprite;

        private Sprite[]  _originalSprites;
        private Vector3[] _originalScales;
        private Vector3[] _correctedScales;

        private void Awake()
        {
            if (_backgroundRenderers == null) return;
            int len = _backgroundRenderers.Length;
            _originalSprites = new Sprite[len];
            _originalScales  = new Vector3[len];
            _correctedScales = new Vector3[len];

            for (int i = 0; i < len; i++)
            {
                if (_backgroundRenderers[i] == null) continue;
                _originalSprites[i] = _backgroundRenderers[i].sprite;
                _originalScales[i]  = _backgroundRenderers[i].transform.localScale;
            }

            // 스프라이트·scale은 런타임 중 변하지 않으므로 보정 scale을 한 번만 계산
            float factor = ComputeScaleFactor();
            for (int i = 0; i < len; i++)
                _correctedScales[i] = _originalScales[i] * factor;
        }

        private void OnEnable()
        {
            if (_backgroundRenderers == null || _stageSprite == null) return;
            for (int i = 0; i < _backgroundRenderers.Length; i++)
            {
                if (_backgroundRenderers[i] == null) continue;
                _backgroundRenderers[i].sprite                = _stageSprite;
                _backgroundRenderers[i].transform.localScale  = _correctedScales[i];
            }
        }

        private void OnDisable()
        {
            if (_backgroundRenderers == null || _originalSprites == null) return;
            for (int i = 0; i < _backgroundRenderers.Length; i++)
            {
                if (_backgroundRenderers[i] == null) continue;
                _backgroundRenderers[i].sprite                = _originalSprites[i];
                _backgroundRenderers[i].transform.localScale  = _originalScales[i];
            }
        }

        // 원본/신규 스프라이트 크기 비율을 계산해 잘림 없이 덮을 수 있는 최소 배율을 반환한다.
        private float ComputeScaleFactor()
        {
            if (_stageSprite == null) return 1f;

            Sprite original = null;
            for (int i = 0; i < _backgroundRenderers.Length; i++)
            {
                if (_backgroundRenderers[i] != null && _originalSprites[i] != null)
                { original = _originalSprites[i]; break; }
            }
            if (original == null) return 1f;

            float scaleX = (original.rect.width  / original.pixelsPerUnit) / (_stageSprite.rect.width  / _stageSprite.pixelsPerUnit);
            float scaleY = (original.rect.height / original.pixelsPerUnit) / (_stageSprite.rect.height / _stageSprite.pixelsPerUnit);
            return Mathf.Max(1f, Mathf.Max(scaleX, scaleY));
        }
    }
}
