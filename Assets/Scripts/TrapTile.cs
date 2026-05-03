// TrapTile.cs  (UPDATED VERSION)
// Place this in: Assets/Scripts/Gameplay/
//
// CHANGES FROM ORIGINAL:
//   Added: reports its grid position to TrapManager when triggered.
//   This lets TrapManager remove it from the A* target list.
//   Everything else is identical to the original TrapTile.cs.

using System.Collections;
using UnityEngine;

public class TrapTile : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float timePenalty = 10f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.5f);
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

    private IEnumerator Trigger()
    {
        enabled = false; // Disable so it can never trigger again

        if (trapSfx != null)
        {
            var listenerPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(trapSfx, listenerPos);
        }

        // Subtract time from the timer
        timer?.SubtractTime(timePenalty);

        // ── NEW: notify TrapManager this trap was triggered ──
        trapManager?.OnTrapTriggered(gridCell);

        // Flash red
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            var mat = renderer.material;
            var original = mat.color;
            mat.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            mat.color = original;
        }
        else
        {
            yield return new WaitForSeconds(flashDuration);
        }
    }
}