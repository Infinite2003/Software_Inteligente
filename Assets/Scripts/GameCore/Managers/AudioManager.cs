using System;
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager _instance;

    public AudioSource sfxAudio;
    public AudioSource musicAudio;

    public List<AudioClip> musicList;
    public List<AudioClip> sfxList;


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = sfxList.Find(s => s.name == sfxName);
        if (clip != null)
        {
            sfxAudio.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"SFX '{sfxName}' not found in the list.");
        }
    }
    public void PlayMusic(string musicName)
    {
        AudioClip clip = musicList.Find(m => m.name == musicName);
        if (clip != null)
        {
            musicAudio.clip = clip;
            musicAudio.loop = true;
            musicAudio.Play();
        }
        else
        {
            Debug.LogWarning($"Music '{musicName}' not found in the list.");
        }
    }
}
