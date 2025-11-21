using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace AudioController
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Mixer Settings")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup soundsGroup;
        [SerializeField] private AudioMixerGroup musicGroup;

        [Header("References")]
        [SerializeField] private GameObject refSound2D;
        [SerializeField] private GameObject refSound3D;

        public static AudioManager Instance { get; private set; }
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            SceneManager.activeSceneChanged += delegate (Scene newScene, Scene scene) { CleanAudioListeners(); };

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(Instance);
            }
            else
            {
                Destroy(gameObject);
            }

            CleanAudioListeners();
        }

        private void CleanAudioListeners()
        {
            // JAVÍTVA: FindObjectsOfType helyett FindObjectsByType(FindObjectsSortMode.None)
            // Ez sokkal gyorsabb és eltünteti a warningot.
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            if (listeners.Length > 1)
            {
                foreach (var listener in listeners)
                {
                    // Ha az adott listeneren van AudioManager, azt hagyjuk békén
                    if (listener.gameObject.GetComponent<AudioManager>() != null) continue;

                    // A többi felesleges listenert töröljük
                    DestroyImmediate(listener);
                }
            }
        }

        // --- ONE SHOT METHODS ---

        public void PlaySound2D(AudioClip audioClip, float volume = 1f)
        {
            if (audioClip == null) return;
            var soundObj = Instantiate(refSound2D, Vector3.zero, Quaternion.identity, transform);
            var source = soundObj.GetComponent<AudioSource>();
            SetupSource(source, audioClip, volume, false, soundsGroup);
            source.Play();
            Destroy(soundObj, audioClip.length + 0.1f);
        }

        public void PlaySound3D(AudioClip audioClip, Vector3 position, Transform parent = null, float volume = 1f)
        {
            if (audioClip == null) return;
            var soundObj = Instantiate(refSound3D, position, Quaternion.identity, parent);
            var source = soundObj.GetComponent<AudioSource>();
            SetupSource(source, audioClip, volume, false, soundsGroup);
            source.spatialBlend = 1f;
            source.Play();
            Destroy(soundObj, audioClip.length + 0.1f);
        }

        // --- PERSISTENT SOURCE METHODS ---

        public AudioSource CreateLoopingSource(Transform parent, AudioClip clip, float startVolume = 0f, bool is3D = true)
        {
            GameObject go = new GameObject("LoopingAudio_" + (clip ? clip.name : "Null"));
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            AudioSource source = go.AddComponent<AudioSource>();
            SetupSource(source, clip, startVolume, true, soundsGroup);

            source.spatialBlend = is3D ? 1f : 0f;
            source.dopplerLevel = is3D ? 1f : 0f;
            source.minDistance = 5f;
            source.maxDistance = 150f;

            source.Play();
            return source;
        }

        private void SetupSource(AudioSource source, AudioClip clip, float volume, bool loop, AudioMixerGroup group)
        {
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.outputAudioMixerGroup = group;
            source.playOnAwake = false;
        }

        // --- MUSIC METHODS ---

        public void PlayMusic(AudioClip audioClip, float volume = 1f)
        {
            var musicObj = new GameObject("BackgroundMusic");
            musicObj.transform.SetParent(transform);
            var music = musicObj.AddComponent<AudioSource>();

            SetupSource(music, audioClip, volume, true, musicGroup);
            music.Play();
            IsPlaying = true;
        }

        public void StopMusic()
        {
            var sources = GetComponentsInChildren<AudioSource>();
            foreach (var s in sources)
            {
                if (s.outputAudioMixerGroup == musicGroup) Destroy(s.gameObject);
            }
            IsPlaying = false;
        }

        public void SetMixerVolume(string parameterName, float volume)
        {
            mixer.SetFloat(parameterName, Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        }
    }
}