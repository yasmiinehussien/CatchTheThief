using System.Collections;
using UnityEngine;

public class TreasureCollectible : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;
    [SerializeField] private GameObject[] extraOpenVisuals;
    [SerializeField] private string sfxResourceName = "TreasureCollect";
    [SerializeField] private float destroyDelay = 0.8f;

    private TreasureManager manager;
    private bool collected;
    private Collider col;
    private AudioClip openSfx;
    private GameObject chestRoot;

    public void Initialize(TreasureManager treasureManager, GameObject root)
    {
        manager = treasureManager;
        chestRoot = root;
        openSfx = Resources.Load<AudioClip>(sfxResourceName);
        col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag(playerTag)) return;
        collected = true;
        StartCoroutine(Collect());
    }

    private IEnumerator Collect()
    {
        manager?.RegisterCollected(this);
        if (col != null) col.enabled = false;

        if (closedVisual != null && openVisual != null)
        {
            openVisual.transform.localPosition = closedVisual.transform.localPosition;
            openVisual.transform.localRotation = closedVisual.transform.localRotation;
            if (extraOpenVisuals != null)
                foreach (var extra in extraOpenVisuals)
                    if (extra != null)
                    {
                        extra.transform.localPosition = closedVisual.transform.localPosition;
                        extra.transform.localRotation = closedVisual.transform.localRotation;
                    }
        }

        if (closedVisual != null) closedVisual.SetActive(false);
        if (openVisual != null) openVisual.SetActive(true);
        if (extraOpenVisuals != null)
            foreach (var extra in extraOpenVisuals)
                if (extra != null) extra.SetActive(true);

        if (openSfx != null)
        {
            var listenerPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(openSfx, listenerPos);
        }

        yield return new WaitForSeconds(destroyDelay);
        Destroy(chestRoot != null ? chestRoot : gameObject);
    }
}
