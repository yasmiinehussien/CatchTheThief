using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryWarningAlert : MonoBehaviour
{
    private GameObject panel;
    private Button okButton;
    private bool isShowing = false;

    private void Awake()
    {
        BuildUI();
        panel.SetActive(false);
        var bg = gameObject.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0, 0, 0, 0f);
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        Debug.Log("SPACE PRESSED!");
    //        var treasureManager = FindAnyObjectByType<TreasureManager>();
    //        int count = treasureManager != null ? treasureManager.CollectedCount : 0;
    //        Debug.Log("Fragment count: " + count);

    //        if (count < 3)
    //            ShowWarning();
    //        else
    //            HideWarning();
    //    }

    //    if (isShowing && Input.GetKeyDown(KeyCode.Escape))
    //        HideWarning();
    //}

    public void ShowWarning()
    {
        transform.SetAsLastSibling();
        var bg = gameObject.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0, 0, 0, 0.6f);
        panel.SetActive(true);
        isShowing = true;
    }

    public void HideWarning()
    {
        var bg = gameObject.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0, 0, 0, 0f);
        panel.SetActive(false);
        isShowing = false;
    }

    private void BuildUI()
    {
        var bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0f);
        bg.raycastTarget = false;

        panel = new GameObject("WarningCard");
        panel.transform.SetParent(transform, false);
        var cardRT = panel.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(560, 580);
        cardRT.anchoredPosition = Vector2.zero;
        var cardImg = panel.AddComponent<Image>();
        cardImg.color = new Color(1f, 0.97f, 0.91f, 1f);
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.91f, 0.63f, 0f, 1f);
        outline.effectDistance = new Vector2(5, 5);

        // WARNING badge
        var badge = MakeChild(panel, "Badge", new Vector2(0, 245), new Vector2(260, 55));
        var badgeImg = badge.AddComponent<Image>();
        badgeImg.color = new Color(0.91f, 0.63f, 0f, 1f);
        badgeImg.raycastTarget = false;
        var badgeText = MakeText(badge, "WARNING", 26, new Color(0.48f, 0.24f, 0f));
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.fontStyle = FontStyles.Bold;

        // Title
        var titleGO = MakeChild(panel, "Title", new Vector2(0, 155), new Vector2(500, 80));
        var titleText = MakeText(titleGO, "Cannot Use Memory Flash!", 38, new Color(0.78f, 0.32f, 0.04f));
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // Count box
        var countBox = MakeChild(panel, "CountBox", new Vector2(0, 15), new Vector2(460, 150));
        var countBoxImg = countBox.AddComponent<Image>();
        countBoxImg.color = new Color(0.99f, 0.94f, 0.76f, 1f);
        countBoxImg.raycastTarget = false;
        var countOutline = countBox.AddComponent<Outline>();
        countOutline.effectColor = new Color(0.91f, 0.63f, 0f, 1f);
        countOutline.effectDistance = new Vector2(3, 3);

        var needLabel = MakeChild(countBox, "NeedLabel", new Vector2(0, 40), new Vector2(400, 35));
        var needText = MakeText(needLabel, "You need at least", 18, new Color(0.48f, 0.24f, 0f));
        needText.alignment = TextAlignmentOptions.Center;
        needText.raycastTarget = false;

        var numGO = MakeChild(countBox, "Num", new Vector2(0, -5), new Vector2(400, 60));
        var numText = MakeText(numGO, "3 fragments", 42, new Color(0.78f, 0.32f, 0.04f));
        numText.alignment = TextAlignmentOptions.Center;
        numText.fontStyle = FontStyles.Bold;
        numText.raycastTarget = false;

        var subLabel = MakeChild(countBox, "SubLabel", new Vector2(0, -52), new Vector2(400, 35));
        var subText = MakeText(subLabel, "to activate Memory Flash", 16, new Color(0.48f, 0.24f, 0f));
        subText.alignment = TextAlignmentOptions.Center;
        subText.raycastTarget = false;

        // Hint text
        var hintGO = MakeChild(panel, "Hint", new Vector2(0, -130), new Vector2(480, 40));
        var hintText = MakeText(hintGO, "Collect more treasures first!", 18, new Color(0.60f, 0.35f, 0f));
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.raycastTarget = false;

        // OK button
        var okGO = MakeChild(panel, "OKBtn", new Vector2(0, -220), new Vector2(460, 70));
        var okImg = okGO.AddComponent<Image>();
        okImg.color = new Color(0.91f, 0.63f, 0f, 1f);
        okImg.raycastTarget = true;
        okButton = okGO.AddComponent<Button>();
        okButton.targetGraphic = okImg;
        okButton.interactable = true;
        okButton.onClick.AddListener(HideWarning);

        var okTxtGO = new GameObject("Label");
        okTxtGO.transform.SetParent(okGO.transform, false);
        var trt = okTxtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var okTxt = okTxtGO.AddComponent<TextMeshProUGUI>();
        okTxt.text = "OK, Got it!";
        okTxt.fontSize = 28;
        okTxt.color = new Color(0.48f, 0.24f, 0f);
        okTxt.alignment = TextAlignmentOptions.Center;
        okTxt.fontStyle = FontStyles.Bold;
        okTxt.raycastTarget = false;
    }

    private GameObject MakeChild(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private TMP_Text MakeText(GameObject parent, string content, int size, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        return t;
    }
}