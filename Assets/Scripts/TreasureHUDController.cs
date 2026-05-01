using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text treasureText;
    [SerializeField] private Slider flashBar;
    [SerializeField] private string treasurePrefix = "Treasure : ";

    private void Awake()
    {
        if (treasureText == null) treasureText = FindInScene<TMP_Text>("FragmentText");
        if (flashBar == null) flashBar = FindInScene<Slider>("flashbar");
    }

    public void Render(int collected, float flash)
    {
        if (treasureText != null) treasureText.text = treasurePrefix + collected;
        if (flashBar != null) flashBar.value = Mathf.Clamp01(flash);
    }

    private static T FindInScene<T>(string objectName) where T : Component
    {
        var go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<T>() : null;
    }
}
