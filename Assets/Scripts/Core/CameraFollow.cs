using UnityEngine;

namespace _2D_Roguelike
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothSpeed = 6f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, -10f);

        private void LateUpdate()
        {
            if (_target == null)
            {
                // 런타임에 플레이어 자동 탐색
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) _target = playerGO.transform;
                return;
            }

            Vector3 desired = _target.position + _offset;
            // z축은 항상 고정 (-10)
            desired.z = _offset.z;

            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                _smoothSpeed * Time.deltaTime
            );
        }
    }
}
