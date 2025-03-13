using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("#BGM")]
    public AudioClip bgmClip;
    public float bgmVolume;
    AudioSource bgmPlayer;

    [Header("#SFX")]
    public AudioClip[] sfxClips;
    public float sfxVolume;
    public int channels;
    AudioSource[] sfxPlayers;
    int channelIndex;

    



    void Awake()
    {
        instance = this;
        Init();

        if (!bgmPlayer.isPlaying)
        {
            bgmPlayer.Play();
        }
    }

    void Init()
    {
        //bgm 초기화
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip = bgmClip;


        //sfx 초기화
        GameObject sfxObject = new GameObject("sfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].volume = sfxVolume;
        }
    }

    public void Playsfx(int index)
    {
        if (index < 0 || index >= sfxClips.Length)
            return; 

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxPlayers.Length;

            if (sfxPlayers[loopIndex].isPlaying)
                continue;

            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[index];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }
   
    //  BGM 볼륨 조절
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmPlayer.volume = bgmVolume;
    }

    //  SFX 볼륨 조절 
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (AudioSource source in sfxPlayers)
        {
            source.volume = sfxVolume;
        }
    }

  

    //  전체 볼륨 조절 (BGM + SFX)
    public void SetMasterVolume(float volume)
    {
        SetBgmVolume(volume);
        SetSfxVolume(volume);
      
    }
    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("BgmVolume", bgmVolume);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
        PlayerPrefs.Save();
    }
    public void LoadVolumeSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1.0f);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1.0f);
        SetBgmVolume(bgmVolume);
        SetSfxVolume(sfxVolume);
    }

}

