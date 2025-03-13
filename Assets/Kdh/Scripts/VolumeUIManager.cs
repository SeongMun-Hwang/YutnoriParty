using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VolumeUIManager : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText;
    public Slider bgmSlider;
    public TextMeshProUGUI bgmText;
    public Slider sfxSlider;    
    public TextMeshProUGUI sfxText;
    public GameObject volumeUI;

    void Start()
    {
        // 기존 볼륨값 불러오기
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        bgmSlider.value = PlayerPrefs.GetFloat("BgmVolume", 1.0f);
        sfxSlider.value = PlayerPrefs.GetFloat("SfxVolume", 1.0f);

        // 슬라이더 값 변경 시 이벤트 추가
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBgmVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        // 초기 볼륨 적용
        UpdateVolumeUI();
    }
    void SetMasterVolume(float volume)
    {
        AudioManager.instance.SetMasterVolume(volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("BgmVolume", volume);
        PlayerPrefs.SetFloat("SfxVolume", volume);
        PlayerPrefs.Save();

        // 개별 슬라이더도 동기화
        bgmSlider.value = volume;
        sfxSlider.value = volume;

        UpdateVolumeUI();
    }
    void SetBgmVolume(float volume)
    {
        AudioManager.instance.SetBgmVolume(volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("BgmVolume", volume); // 저장
        PlayerPrefs.Save();
        UpdateVolumeUI();
    }

    void SetSfxVolume(float volume)
    {
        AudioManager.instance.SetSfxVolume(volume);
        PlayerPrefs.SetFloat("SfxVolume", volume); // 저장
        PlayerPrefs.Save();
        UpdateVolumeUI();
    }

    void UpdateVolumeUI()
    {
        masterVolumeText.text = $"{Mathf.Round(masterVolumeSlider.value * 100)}";
        bgmText.text = $"{Mathf.Round(bgmSlider.value * 100)}";
        sfxText.text = $"{Mathf.Round(sfxSlider.value * 100)}";
    }
    public void ToggleUI()
    {
        volumeUI.SetActive(!volumeUI.activeSelf); 
    }
}
