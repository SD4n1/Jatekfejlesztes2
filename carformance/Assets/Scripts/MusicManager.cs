using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [Header("Ide húzd be a zenéket!")]
    public AudioClip[] songs;

    [Header("Melyik Scene-ekben szóljon a zene?")]
    public List<string> menuScenes;

    private AudioSource audioSource;
    private bool canPlayMusic = true;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        audioSource.volume = savedVolume;
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (menuScenes.Contains(scene.name))
        {
            canPlayMusic = true;
            if (!audioSource.isPlaying) PlayRandomSong();
        }
        else
        {
            canPlayMusic = false;
            audioSource.Stop();
        }
    }

    void Update()
    {
        if (canPlayMusic && !audioSource.isPlaying) PlayRandomSong();
    }

    void PlayRandomSong()
    {
        if (songs.Length > 0 && canPlayMusic)
        {
            int randomIndex = Random.Range(0, songs.Length);
            audioSource.clip = songs[randomIndex];
            audioSource.Play();
        }
    }


    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
    }
}