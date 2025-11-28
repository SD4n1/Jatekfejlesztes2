using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadScene : MonoBehaviour
{
    public enum PageType { Menu, TrackSelection, CarSelection, LiverySelection, Credits }

    [Header("MÛKÖDÉS")]
    public PageType currentPageType;
    public string nextSceneName;

    [Header("UI Elemek (Csak Livery választónál kell)")]
    public TextMeshProUGUI nameText;

    [Header("ADATOK")]
    public TrackData[] allTracks;
    public CarData[] allCars;

    private int currentLiveryIndex = 0;

    void Start()
    {
        if (currentPageType == PageType.LiverySelection)
        {
            UpdateLiveryUI();
        }
    }

    // --- NYILAK KEZELÉSE ---
    public void NextLivery()
    {
        CarData category = RaceManager.Instance.selectedCar;
        if (category != null && category.carOptions.Count > 0)
        {
            currentLiveryIndex = (currentLiveryIndex + 1) % category.carOptions.Count;
            UpdateLiveryUI();
        }
    }

    public void PrevLivery()
    {
        CarData category = RaceManager.Instance.selectedCar;
        if (category != null && category.carOptions.Count > 0)
        {
            currentLiveryIndex--;
            if (currentLiveryIndex < 0) currentLiveryIndex = category.carOptions.Count - 1;
            UpdateLiveryUI();
        }
    }

    void UpdateLiveryUI()
    {
        if (RaceManager.Instance == null)
        {
            Debug.LogError("HIBA: Nincs RaceManager! (Nem a Menübõl indultál?)");
            return;
        }

        CarData category = RaceManager.Instance.selectedCar;
        if (category == null)
        {
            Debug.LogError("HIBA: Nincs kiválasztva Kategória (F1/GT) a RaceManagerben!");
            return;
        }

      
        if (nameText == null) Debug.LogError("HIBA: Nincs behúzva a Szöveg az Inspectorban!");

        if (category != null && nameText != null)
        {
            var options = category.carOptions;
            Debug.Log("Siker! Csapatok száma: " + options.Count + ". Jelenlegi index: " + currentLiveryIndex);

            if (options.Count > currentLiveryIndex)
            {
                nameText.text = options[currentLiveryIndex].teamName;

            }
        }
    }

    public void StartRace()
    {
        RaceManager.Instance.selectedLiveryIndex = currentLiveryIndex;
        string sceneToLoad = RaceManager.Instance.selectedTrack.sceneName;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void StartGameFromMenu()
    {
        if (currentPageType == PageType.Menu) SceneManager.LoadScene(nextSceneName);
    }
    public void SelectTrackAndGo(int index)
    {
        RaceManager.Instance.selectedTrack = allTracks[index];
        SceneManager.LoadScene(nextSceneName);
    }
    public void SelectCategoryAndGo(int index)
    {
        RaceManager.Instance.selectedCar = allCars[index];
        SceneManager.LoadScene(nextSceneName);
    }
}