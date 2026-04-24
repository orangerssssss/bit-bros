using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[AddComponentMenu("Audio/Scene Music Controller")]
public class SceneMusicController : MonoBehaviour
{
    [Tooltip("BGM clip to play (drag AudioClip here)")]
    public AudioClip bgmClip;

    [Tooltip("If set, BGM will play only in scenes with these names (exact match).")]
    public List<string> sceneNamesToPlay = new List<string>();

    [Tooltip("If sceneNamesToPlay is empty, use build indexes here.")]
    public List<int> sceneBuildIndexesToPlay = new List<int>();

    [Tooltip("Keep this object across scenes (DontDestroyOnLoad)")]
    public bool persistAcrossScenes = true;

    [Tooltip("Loop the audio source")]
    public bool loop = true;

    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;
    private static SceneMusicController instance;

    void Awake()
    {
        if (persistAcrossScenes)
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.clip = bgmClip;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPlay = ShouldPlayInScene(scene);
        if (shouldPlay)
        {
            if (audioSource.clip != bgmClip) audioSource.clip = bgmClip;
            audioSource.loop = loop;
            audioSource.volume = volume;
            if (!audioSource.isPlaying) audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();
        }
    }

    private bool ShouldPlayInScene(Scene scene)
    {
        if (sceneNamesToPlay != null && sceneNamesToPlay.Count > 0)
        {
            return sceneNamesToPlay.Contains(scene.name);
        }
        if (sceneBuildIndexesToPlay != null && sceneBuildIndexesToPlay.Count > 0)
        {
            return sceneBuildIndexesToPlay.Contains(scene.buildIndex);
        }
        return false;
    }

    // helper API to change clip at runtime
    public void SetClipAndPlay(AudioClip clip)
    {
        bgmClip = clip;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
