using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace _2D_Roguelike
{
    /// <summary>
    /// 효과음 재생 전담 싱글턴.
    /// AudioSource 풀을 관리하며, 씬 어디서든 PlayOneShot()으로 소리를 낼 수 있다.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        public static SfxManager Instance { get; private set; }

        [SerializeField] private AudioSource _sourcePrefab;
        [SerializeField] private int         _poolSize    = 32;
        [SerializeField] private int         _maxPoolSize = 64;

        private ObjectPool<AudioSource> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _pool = new ObjectPool<AudioSource>(
                createFunc:      CreateSource,
                actionOnGet:     s => s.gameObject.SetActive(true),
                actionOnRelease: s => s.gameObject.SetActive(false),
                actionOnDestroy: s => Destroy(s.gameObject),
                collectionCheck: false,
                defaultCapacity: _poolSize,
                maxSize:         _maxPoolSize
            );
        }

        /// <summary>
        /// 지정 위치에서 클립을 한 번 재생한다.
        /// </summary>
        /// <param name="clip">재생할 AudioClip. null이면 무시.</param>
        /// <param name="worldPos">재생 위치.</param>
        /// <param name="volume">볼륨 (0~1).</param>
        /// <param name="pitch">피치 (1 = 원래 속도).</param>
        public void PlayOneShot(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            var source = _pool.Get();
            source.transform.position = worldPos;
            source.volume             = Mathf.Clamp01(volume);
            source.pitch              = pitch;
            source.PlayOneShot(clip);

            StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch))));
        }

        private IEnumerator ReturnAfterPlay(AudioSource source, float duration)
        {
            yield return new WaitForSecondsRealtime(duration + 0.05f);
            if (source != null)
                _pool.Release(source);
        }

        private AudioSource CreateSource()
        {
            var s = Instantiate(_sourcePrefab, transform);
            s.gameObject.SetActive(false);
            return s;
        }
    }
}
