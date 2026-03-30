using UnityEngine;

public class GhostFade : MonoBehaviour
{
    public float ghostDelay;
    private float ghostDelaySeconds;
    public GameObject ghostFade;
    public bool makeGhost = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ghostDelaySeconds = ghostDelay;
    }

    // Update is called once per frame
    void Update()
    {
        if(makeGhost)
        {
            if(ghostDelaySeconds > 0)
            {
                ghostDelaySeconds -= Time.deltaTime;
            }
            else
            {
                GameObject currentGhost = Instantiate(ghostFade, transform.position , transform.rotation);
                Sprite currentSpite = GetComponent<SpriteRenderer>().sprite;
                currentGhost.transform.localScale = this.transform.localScale;
                currentGhost.GetComponent<SpriteRenderer>().sprite = currentSpite;
                ghostDelaySeconds = ghostDelay;
                Destroy(currentGhost,0.35f);
            }
        }
    }
}
