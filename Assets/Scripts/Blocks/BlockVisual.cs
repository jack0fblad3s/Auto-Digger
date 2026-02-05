using UnityEngine;
using System.Collections;

public class BlockVisual : MonoBehaviour
{
    private Vector3 originalScale;
    private float hitPulseTime = 0.08f;
    private bool pulsing;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnHit(float damagePercent)
    {
        // Shrink slightly based on damage taken
        float scaleFactor = Mathf.Lerp(1f, 0.7f, damagePercent);
        transform.localScale = originalScale * scaleFactor;

        if (!pulsing)
            StartCoroutine(HitPulse());
    }

    // Make public so other scripts can call it
    public IEnumerator HitPulse()
    {
        pulsing = true;
        transform.localScale *= 0.95f;
        yield return new WaitForSeconds(hitPulseTime);
        pulsing = false;
    }
}
