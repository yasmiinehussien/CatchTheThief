using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimeUpAlert : MonoBehaviour
{
    private TreasureManager treasureManager;
    private GameTimer gameTimer;
    private GameObject panel;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text fragmentCountText;
    private Button playAgainButton;
    private Button homeButton;

    private bool gameEnded = false;   // ensures only one final UI

    private void Awake()
    {
        treasureManager = FindAnyObjectByType<TreasureManager>();
        gameTimer = FindAnyObjectByType<GameTimer>();
        BuildUI();
        panel.SetActive(false);
        var bg = gameObject.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0, 0, 0, 0f);
    }

    private void BuildUI()
    {
        // Overlay — transparent, no raycast blocking
        var bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0f);
        bg.raycastTarget = false;

        // Make sure this Canvas has a GraphicRaycaster
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Card
        panel = new GameObject("AlertCard");
        panel.transform.SetParent(transform, false);
        var cardRT = panel.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(580, 680);
        cardRT.anchoredPosition = Vector2.zero;
        var cardImg = panel.AddComponent<Image>();
        cardImg.color = new Color(1f, 0.97f, 0.91f, 1f);
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.96f, 0.65f, 0.14f, 1f);
        outline.effectDistance = new Vector2(6, 6);

        // Badge
        var badge = MakeChild(panel, "Badge", new Vector2(0, 280), new Vector2(260, 55));
        var badgeImg = badge.AddComponent<Image>();
        badgeImg.color = new Color(0.96f, 0.65f, 0.14f, 1f);
        badgeImg.raycastTarget = false;
        var badgeText = MakeText(badge, "WELL DONE!", 26, new Color(0.48f, 0.24f, 0f));
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.fontStyle = FontStyles.Bold;

        // Title
        var titleGO = MakeChild(panel, "Title", new Vector2(0, 185), new Vector2(520, 80));
        titleText = MakeText(titleGO, "Congratulations!", 48, new Color(0.78f, 0.32f, 0.04f));
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // Subtitle
        var subGO = MakeChild(panel, "Subtitle", new Vector2(0, 120), new Vector2(520, 50));
        subtitleText = MakeText(subGO, "You collected treasures!", 24, new Color(0.48f, 0.24f, 0f));
        subtitleText.alignment = TextAlignmentOptions.Center;

        // Count box
        var countBox = MakeChild(panel, "CountBox", new Vector2(0, -10), new Vector2(480, 140));
        var countBoxImg = countBox.AddComponent<Image>();
        countBoxImg.color = new Color(0.99f, 0.94f, 0.76f, 1f);
        countBoxImg.raycastTarget = false;
        var countOutline = countBox.AddComponent<Outline>();
        countOutline.effectColor = new Color(0.96f, 0.65f, 0.14f, 1f);
        countOutline.effectDistance = new Vector2(4, 4);

        var countLabel = MakeChild(countBox, "CountLabel", new Vector2(0, 30), new Vector2(440, 40));
        var labelText = MakeText(countLabel, "Fragments Collected", 22, new Color(0.48f, 0.24f, 0f));
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        var countGO = MakeChild(countBox, "CountNum", new Vector2(0, -25), new Vector2(440, 65));
        fragmentCountText = MakeText(countGO, "0", 52, new Color(0.78f, 0.32f, 0.04f));
        fragmentCountText.alignment = TextAlignmentOptions.Center;
        fragmentCountText.fontStyle = FontStyles.Bold;
        fragmentCountText.raycastTarget = false;

        // Buttons
        playAgainButton = MakeButton(panel, "Play Again", new Vector2(0, -175), new Vector2(480, 70),
            new Color(0.96f, 0.65f, 0.14f, 1f), new Color(0.48f, 0.24f, 0f));
        playAgainButton.onClick.AddListener(OnPlayAgain);

        homeButton = MakeButton(panel, "Home Screen", new Vector2(0, -265), new Vector2(480, 70),
            Color.white, new Color(0.78f, 0.32f, 0.04f), new Color(0.96f, 0.65f, 0.14f, 1f));
        homeButton.onClick.AddListener(OnHomeScreen);
    }

    // Called from GameTimer when time runs out (failure)
    public void ShowAlert()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (gameTimer != null) gameTimer.Stop();
        DisablePlayerMovement();

        int count = treasureManager != null ? treasureManager.CollectedCount : 0;

        // Dark overlay
        var bg = gameObject.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0, 0, 0, 0.7f);
            bg.raycastTarget = true;
        }

        panel.SetActive(true);

        // --- Failure (Game Over) theme ---
        titleText.text = "Game Over!";
        titleText.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        subtitleText.text = "Time ran out before reaching the bank.";
        subtitleText.color = new Color(0.6f, 0.1f, 0.1f, 1f);
        fragmentCountText.text = count.ToString();
        fragmentCountText.color = new Color(0.8f, 0.1f, 0.1f, 1f);

        var o = panel.GetComponent<Outline>();
        if (o) o.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        panel.GetComponent<Image>().color = new Color(1f, 0.88f, 0.88f, 1f);

        var badge = panel.transform.Find("Badge");
        if (badge != null)
        {
            var badgeImg = badge.GetComponent<Image>();
            if (badgeImg) badgeImg.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        var countBox = panel.transform.Find("CountBox");
        if (countBox != null)
        {
            var boxImg = countBox.GetComponent<Image>();
            if (boxImg) boxImg.color = new Color(1f, 0.82f, 0.82f, 1f);
            var boxOutline = countBox.GetComponent<Outline>();
            if (boxOutline) boxOutline.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        var playBtn = panel.transform.Find("Play AgainBtn");
        if (playBtn != null)
        {
            var btnImg = playBtn.GetComponent<Image>();
            if (btnImg) btnImg.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        var homeBtn = panel.transform.Find("Home ScreenBtn");
        if (homeBtn != null)
        {
            var btnOutline = homeBtn.GetComponent<Outline>();
            if (btnOutline) btnOutline.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        }
    }

    // Called from PlayerMovement when the bank is reached (success)
    public void ShowSuccess()
    {
        // Prevent success if game already ended OR timer has already run out
        if (gameEnded) return;
        if (gameTimer != null && gameTimer.Remaining <= 0f) return;

        gameEnded = true;

        if (gameTimer != null) gameTimer.Stop();
        DisablePlayerMovement();

        int count = treasureManager != null ? treasureManager.CollectedCount : 0;

        var bg = gameObject.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0, 0, 0, 0.7f);
            bg.raycastTarget = true;
        }

        panel.SetActive(true);

        // --- Success theme (gold/orange) ---
        titleText.text = "Congratulations!";
        titleText.color = new Color(0.78f, 0.32f, 0.04f);
        subtitleText.text = "You reached the bank!";
        subtitleText.color = new Color(0.48f, 0.24f, 0f);
        fragmentCountText.text = count.ToString();
        fragmentCountText.color = new Color(0.78f, 0.32f, 0.04f);

        var o = panel.GetComponent<Outline>();
        if (o) o.effectColor = new Color(0.96f, 0.65f, 0.14f, 1f);
        panel.GetComponent<Image>().color = new Color(1f, 0.97f, 0.91f, 1f);

        var badge = panel.transform.Find("Badge");
        if (badge != null)
        {
            var badgeImg = badge.GetComponent<Image>();
            if (badgeImg) badgeImg.color = new Color(0.96f, 0.65f, 0.14f, 1f);
        }

        var countBox = panel.transform.Find("CountBox");
        if (countBox != null)
        {
            var boxImg = countBox.GetComponent<Image>();
            if (boxImg) boxImg.color = new Color(0.99f, 0.94f, 0.76f, 1f);
            var boxOutline = countBox.GetComponent<Outline>();
            if (boxOutline) boxOutline.effectColor = new Color(0.96f, 0.65f, 0.14f, 1f);
        }
    }

    private void DisablePlayerMovement()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            // Add any other movement scripts you use (e.g., PlayerMovement)
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.StopPlayer();
                movement.enabled = false;
            }
        }
    }

    public void OnPlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeScreen()
    {
        SceneManager.LoadScene("MainMenu");
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

    private Button MakeButton(GameObject parent, string label, Vector2 pos, Vector2 size,
        Color bgColor, Color textColor, Color? outlineColor = null)
    {
        var go = MakeChild(parent, label + "Btn", pos, size);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;
        if (outlineColor.HasValue)
        {
            var ol = go.AddComponent<Outline>();
            ol.effectColor = outlineColor.Value;
            ol.effectDistance = new Vector2(4, 4);
        }
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = true;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 28;
        t.color = textColor;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return btn;
    }
}