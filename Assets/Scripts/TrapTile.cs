using System.Collections;
using UnityEngine;

public class TrapTile : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float timePenalty = 10f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private string sfxResourceName = "TrapHit";

    // ── NEW: grid position so TrapManager can remove us from A* list ──
    [HideInInspector] public Vector2Int gridCell; // Set by TrapManager after spawning

    private GameTimer timer;
    private TrapManager trapManager;
    private AudioClip trapSfx;

    private void Start()
    {
        timer = FindAnyObjectByType<GameTimer>();
        trapManager = FindAnyObjectByType<TrapManager>();
        trapSfx = Resources.Load<AudioClip>(sfxResourceName);

        var col = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        StartCoroutine(Trigger());
    }

    //private IEnumerator Trigger()
    //{
    //    enabled = false; // Disable so it can never trigger again

    //    if (trapSfx != null)
    //    {
    //        var listenerPos = Camera.main != null ? Camera.main.transform.position : transform.position;
    //        AudioSource.PlayClipAtPoint(trapSfx, listenerPos);
    //    }

    //    // Subtract time from the timer
    //    timer?.SubtractTime(timePenalty);

    //    // ── NEW: notify TrapManager this trap was triggered ──
    //    trapManager?.OnTrapTriggered(gridCell);

    //    // Flash red
    //    var renderer = GetComponentInChildren<Renderer>();
    //    if (renderer != null)
    //    {
    //        var mat = renderer.material;
    //        var original = mat.color;
    //        mat.color = flashColor;
    //        yield return new WaitForSeconds(flashDuration);
    //        mat.color = original;
    //    }
    //    else
    //    {
    //        yield return new WaitForSeconds(flashDuration);
    //    }
    //}

    //Changed
    private IEnumerator Trigger()
    {
        // 1. Immediately disable the collider so it can't be hit twice
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. Safety Check: Audio
        if (trapSfx != null)
        {
            Vector3 listenerPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(trapSfx, listenerPos);
        }

        // 3. Safety Check: Timer
        // Use an explicit null check here. Unity's '==' operator is more reliable
        // than '?' for destroyed objects.
        if (timer != null)
        {
            timer.SubtractTime(timePenalty);
        }

        // 4. Safety Check: TrapManager
        if (trapManager != null)
        {
            trapManager.OnTrapTriggered(gridCell);
        }

        // 5. Visual Flash Logic — raycast down to find the road tile's renderer
        Renderer roadRenderer = null;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
            roadRenderer = hit.collider.GetComponent<Renderer>() ?? hit.collider.GetComponentInChildren<Renderer>();

        if (roadRenderer != null)
        {
            var mat = roadRenderer.material;
            string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            var original = mat.GetColor(colorProp);
            mat.SetColor(colorProp, flashColor);

            yield return new WaitForSeconds(flashDuration);

            if (mat != null)
                mat.SetColor(colorProp, original);
        }
        else
        {
            yield return new WaitForSeconds(flashDuration);
        }
    }
}