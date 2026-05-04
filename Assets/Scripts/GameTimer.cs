using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float duration = 60f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color penaltyColor = Color.red;
    [SerializeField] private float penaltyFlashDuration = 0.5f;
    [SerializeField] private UnityEvent onTimeUp;

    private float remaining;
    private bool running;
    private Color normalColor = Color.white;
    private Coroutine flashRoutine;
    private TimeUpAlert timeUpAlert;

    private void Start()
    {
        if (timerText == null)
        {
            var go = GameObject.Find("TimerText");
            if (go != null) timerText = go.GetComponent<TMP_Text>();
        }
        if (timerText != null) normalColor = timerText.color;
        timeUpAlert = FindAnyObjectByType<TimeUpAlert>();
        remaining = duration;
        running = true;
        Render();
    }

    private void Update()
    {
        if (!running) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            Render();
            onTimeUp?.Invoke();
            if (timeUpAlert != null)
                timeUpAlert.ShowAlert();
            return;
        }
        Render();
    }

    private void Render()
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(remaining).ToString();
    }

    public void Stop() => running = false;
    public void Resume() => running = true;
    public bool IsRunning => running;
    public float Remaining => remaining;

    public void SubtractTime(float seconds)
    {
        remaining = Mathf.Max(0f, remaining - seconds);
        Render();
        if (timerText != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashPenalty());
        }
    }

    private IEnumerator FlashPenalty()
    {
        timerText.color = penaltyColor;
        yield return new WaitForSeconds(penaltyFlashDuration);
        timerText.color = normalColor;
        flashRoutine = null;
    }
}