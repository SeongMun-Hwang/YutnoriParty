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

    private void Start()
    {
        volumeUI.SetActive(false);
        // 기존 볼륨값 불러오기 및 UI 초기화
        LoadVolumeSettings();
        UpdateVolumeUI();

        // 슬라이더 이벤트 리스너 등록
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBgmVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            volumeUI.SetActive(false);
        }
    }
    private void LoadVolumeSettings()
    {
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        bgmSlider.value = PlayerPrefs.GetFloat("BgmVolume", masterVolumeSlider.value);
        sfxSlider.value = PlayerPrefs.GetFloat("SfxVolume", masterVolumeSlider.value);
    }

    private void SetMasterVolume(float volume)
    {
        AudioManager.instance.SetMasterVolume(volume);
        bgmSlider.value = volume;
        sfxSlider.value = volume;
        SaveVolume("MasterVolume", volume);
        SaveVolume("BgmVolume", volume);
        SaveVolume("SfxVolume", volume);
    }

    private void SetBgmVolume(float volume)
    {
        AudioManager.instance.SetBgmVolume(volume);
        SaveVolume("BgmVolume", volume);
    }

    private void SetSfxVolume(float volume)
    {
        AudioManager.instance.SetSfxVolume(volume);
        SaveVolume("SfxVolume", volume);
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        UpdateVolumeUI();
    }

    private void UpdateVolumeUI()
    {
        masterVolumeText.text = Mathf.Round(masterVolumeSlider.value * 100).ToString();
        bgmText.text = Mathf.Round(bgmSlider.value * 100).ToString();
        sfxText.text = Mathf.Round(sfxSlider.value * 100).ToString();
    }

    public void ToggleUI()
    {
        volumeUI.SetActive(!volumeUI.activeSelf);
    }
}
