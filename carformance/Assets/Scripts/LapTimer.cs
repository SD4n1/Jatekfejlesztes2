using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LapTimer : MonoBehaviour
{
    [Header("UI Elemek")]
    public TMP_Text timerDisplay;
    public TMP_Text historyText;

    private float currentLapTime = 0f;
    private bool isRacing = false;
    private int lapCounter = -1;
    private List<string> lapRecords = new List<string>();

    private float lastFinishTime = -10f;
    private float finishCooldown = 4f;

    void Start()
    {
        currentLapTime = 0f;
        lapCounter = -1;
        isRacing = true;
        historyText.text = "";
        lapRecords.Clear();
        lastFinishTime = -10f;
    }

    void Update()
    {
        if (isRacing)
        {
            if (lapCounter >= 1)
            {
                currentLapTime += Time.deltaTime;
            }
            UpdateTimerUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FinishLine"))
        {
            // Csak ha elég idő telt el az utolsó áthaladás óta
            if (Time.time - lastFinishTime > finishCooldown)
            {
                lastFinishTime = Time.time;
                FinishLap();
            }
        }
    }

    void FinishLap()
    {
        if (lapCounter < 1)
        {
            lapCounter++;
            currentLapTime = 0f;
            if (lapCounter == 0) Debug.Log("Box elhagyva -> Out Lap (időmérés áll)");
            if (lapCounter == 1) Debug.Log("Célvonal átlépve -> IDŐMÉRÉS INDUL!");
            return;
        }

        float minutes = Mathf.FloorToInt(currentLapTime / 60);
        float seconds = Mathf.FloorToInt(currentLapTime % 60);
        float milliseconds = (currentLapTime % 1) * 1000;
        string formattedTime = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);

        string lapEntry = "Lap " + lapCounter + " - " + formattedTime;
        lapRecords.Add(lapEntry);

        if (lapRecords.Count > 5)
        {
            lapRecords.RemoveAt(0);
        }

        UpdateHistoryUI();
        lapCounter++;
        currentLapTime = 0f;
    }

    void UpdateTimerUI()
    {
        if (lapCounter == -1)
        {
            timerDisplay.text = "PIT LANE";
        }
        else if (lapCounter == 0)
        {
            timerDisplay.text = "OUT LAP";
        }
        else
        {
            float minutes = Mathf.FloorToInt(currentLapTime / 60);
            float seconds = Mathf.FloorToInt(currentLapTime % 60);
            float milliseconds = (currentLapTime % 1) * 1000;
            timerDisplay.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
    }

    void UpdateHistoryUI()
    {
        historyText.text = "";
        foreach (string record in lapRecords)
        {
            historyText.text += record + "\n";
        }
    }
}