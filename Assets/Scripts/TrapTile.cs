using System.Collections;
using UnityEngine;

public class TrapTile : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float timePenalty = 10f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private string sfxResourceName = "TrapHit";

    [HideInInspector] public Vector2Int gridCell;

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
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (trapSfx != null)
        {
            Vector3 listenerPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(trapSfx, listenerPos);
        }

        timer?.SubtractTime(timePenalty);
        trapManager?.OnTrapTriggered(gridCell);

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
            if (mat != null) mat.SetColor(colorProp, original);
        }
        else
        {
            yield return new WaitForSeconds(flashDuration);
        }
    }
}
