using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    public CarData selectedCar;
    public int selectedLiveryIndex;
    public TrackData selectedTrack;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}