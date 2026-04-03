using UnityEngine;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    /// <summary>
    /// 미니맵 UI를 관리합니다.
    /// 레이아웃과 스타일은 Minimap.uxml / Minimap.uss에 정의되어 있으며,
    /// 이 스크립트는 RenderTexture를 VisualElement에 주입하는 역할만 담당합니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] private Camera _minimapCamera;
        [SerializeField] private RenderTexture _renderTexture;

        private VisualElement _minimapView;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _minimapView = root.Q<VisualElement>("minimap-view");

            if (_minimapCamera == null || _renderTexture == null)
            {
                Debug.LogWarning("[MinimapUI] MinimapCamera 또는 RenderTexture가 할당되지 않았습니다.");
                return;
            }

            _minimapCamera.targetTexture = _renderTexture;
            // UIToolkit에 RenderTexture 주입 (Unity 2022.2+ / Unity 6 지원)
            _minimapView.style.backgroundImage =
                new StyleBackground(Background.FromRenderTexture(_renderTexture));
        }
    }
}
