using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class CoinFlyEffect : MonoBehaviour
    {
        private const float BurstDuration = 0.4f;
        private const float FlyDuration   = 0.35f;
        private const float HalfGravity   = -6f;   // 0.5 * -12

        public void Play(Vector3 startPos, Vector3 target, int goldAmount)
        {
            StopAllCoroutines();
            transform.position   = startPos;
            transform.localScale = Vector3.one;
            StartCoroutine(AnimateCoroutine(startPos, target, goldAmount));
        }

        private IEnumerator AnimateCoroutine(Vector3 startPos, Vector3 target, int goldAmount)
        {
            // Phase 1: Burst — 포물선 호
            float angle = Random.Range(40f, 140f) * Mathf.Deg2Rad;
            float speed = Random.Range(2.5f, 4.5f);
            float vx    = Mathf.Cos(angle) * speed;
            float vy    = Mathf.Sin(angle) * speed;

            float elapsed = 0f;
            while (elapsed < BurstDuration)
            {
                elapsed += Time.deltaTime;
                float x  = startPos.x + vx * elapsed;
                float y  = startPos.y + vy * elapsed + HalfGravity * elapsed * elapsed;
                transform.position = new Vector3(x, y, startPos.z);
                yield return null;
            }

            Vector3 restPos  = transform.position;

            // Phase 2: Rest — 바닥 대기
            float restTime    = Random.Range(0.45f, 0.75f);
            float restElapsed = 0f;
            while (restElapsed < restTime)
            {
                restElapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 3: Fly — UI 아이콘으로 수렴, 도착 시 골드 지급
            elapsed = 0f;
            while (elapsed < FlyDuration)
            {
                elapsed     += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / FlyDuration);
                float smooth = t * t * (3f - 2f * t);
                transform.position   = Vector3.Lerp(restPos, target, smooth);
                transform.localScale = Vector3.one * (1f - smooth * 0.8f);
                yield return null;
            }

            GoldManager.Instance?.AddGold(goldAmount);
            CoinPool.Instance.Release(this);
        }
    }
}
