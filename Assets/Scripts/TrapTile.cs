using System.Collections;
using UnityEngine;

public class TrapTile : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float timePenalty = 10f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private string sfxResourceName = "TrapHit";

    private GameTimer timer;
    private AudioClip trapSfx;

    private void Start()
    {
        timer = FindAnyObjectByType<GameTimer>();
        trapSfx = Resources.Load<AudioClip>(sfxResourceName);
        if (trapSfx == null) Debug.LogWarning($"[TrapTile] Could not load SFX '{sfxResourceName}' from Resources.");
        else Debug.Log($"[TrapTile] SFX '{sfxResourceName}' loaded OK.");

        var col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        StartCoroutine(Trigger());
    }

    private IEnumerator Trigger()
    {
        // Disable immediately so it can never trigger again
        enabled = false;

        if (trapSfx != null)
        {
            var listenerPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(trapSfx, listenerPos);
        }
        timer?.SubtractTime(timePenalty);

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
