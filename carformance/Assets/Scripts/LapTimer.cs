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

    // -1: Boxutca
    //  0: Kivezetõ kör (Out Lap)
    //  1+: Éles körök
    private int lapCounter = -1;

    private List<string> lapRecords = new List<string>();

    void Start()
    {
        currentLapTime = 0f;
        lapCounter = -1;
        isRacing = true;

        historyText.text = "";
        lapRecords.Clear();
    }

    void Update()
    {
        if (isRacing)
        {
            // CSAK AKKOR számoljuk az idõt, ha már az 1. körben (vagy késõbb) vagyunk
            // A -1 (Box) és 0 (Out Lap) alatt az idõ állni fog 0-n.
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
            FinishLap();
        }
    }

    void FinishLap()
    {
        // Ha még nem éles kör (-1 vagy 0)
        if (lapCounter < 1)
        {
            lapCounter++; // Léptetjük a fázist
            currentLapTime = 0f; // Biztos ami biztos, nullázzuk

            if (lapCounter == 0) Debug.Log("Box elhagyva -> Out Lap (idõmérés áll)");
            if (lapCounter == 1) Debug.Log("Célvonal átlépve -> IDÕMÉRÉS INDUL!");

            return;
        }

        // --- ÉLES KÖRÖK MENTÉSE ---

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
        currentLapTime = 0f; // Nullázzuk a következõ körhöz
    }

    void UpdateTimerUI()
    {
        // Külön szöveg a különbözõ fázisokhoz
        if (lapCounter == -1)
        {
            timerDisplay.text = "PIT LANE";
        }
        else if (lapCounter == 0)
        {
            // Itt most csak kiírjuk a szöveget, idõ nélkül, hiszen az nem számolódik
            timerDisplay.text = "OUT LAP";
        }
        else
        {
            // Éles kör: mutatjuk az idõt
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