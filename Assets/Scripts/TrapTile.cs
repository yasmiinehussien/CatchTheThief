using System.Collections;
using UnityEngine;

public class TrapTile : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float timePenalty = 10f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float flashDuration = 0.5f;

    private GameTimer timer;

    private void Start()
    {
        timer = FindAnyObjectByType<GameTimer>();

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
