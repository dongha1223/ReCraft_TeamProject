
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 던전 스테이지 전환을 관리합니다.
/// 각 던전 씬의 출구 트리거에 부착하세요.
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("Stage Info")]
    public int currentStage = 1;
    public string currentSinName = "Superbia";

    [Header("Scene Names")]
    public string[] dungeonSceneNames = {
        "Stage1_Superbia",
        "Stage2_Avaritia",
        "Stage3_Luxuria",
        "Stage4_Ira"
    };

    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public CanvasGroup fadeCanvas;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 현재 씬에서 스테이지 번호 자동 감지
        var info = FindObjectOfType<DungeonStageInfo>();
        if (info != null)
        {
            currentStage   = info.stageNumber;
            currentSinName = info.sinName;
        }
        Debug.Log($"[DungeonManager] Stage {currentStage}: {currentSinName} 시작");
    }

    /// <summary>다음 스테이지로 이동</summary>
    public void GoToNextStage()
    {
        int next = currentStage; // 0-based index
        if (next < dungeonSceneNames.Length)
        {
            Debug.Log($"[DungeonManager] → Stage{next + 1} {dungeonSceneNames[next]}로 이동");
            StartCoroutine(LoadStageCoroutine(dungeonSceneNames[next]));
        }
        else
        {
            Debug.Log("[DungeonManager] 모든 던전 클리어!");
            // TODO: 엔딩 씬 전환
        }
    }

    /// <summary>특정 스테이지로 직접 이동</summary>
    public void GoToStage(int stageNumber)
    {
        int idx = stageNumber - 1;
        if (idx >= 0 && idx < dungeonSceneNames.Length)
            StartCoroutine(LoadStageCoroutine(dungeonSceneNames[idx]));
    }

    System.Collections.IEnumerator LoadStageCoroutine(string sceneName)
    {
        // 페이드 아웃
        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                fadeCanvas.alpha = t / transitionDuration;
                yield return null;
            }
            fadeCanvas.alpha = 1f;
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        SceneManager.LoadScene(sceneName);
    }
}
