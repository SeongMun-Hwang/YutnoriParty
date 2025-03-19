using TMPro;
using UnityEngine;
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
    public GameObject titleMenuCanvas;

    private float masterVolume = 1.0f;
    private float bgmVolume = 1.0f;
    private float sfxVolume = 1.0f;

    private void Start()
    {
        volumeUI.SetActive(false);

        LoadVolumeSettings();
        UpdateVolumeUI();

        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBgmVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            volumeUI.SetActive(false);

            if (titleMenuCanvas != null)
            {
                titleMenuCanvas.SetActive(true);
            }
        }
    }

    private void LoadVolumeSettings()
    {
        // 개별 볼륨 값 불러오기
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        bgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1.0f);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1.0f);

        // UI 반영
        masterVolumeSlider.value = masterVolume;
        bgmSlider.value = bgmVolume;
        sfxSlider.value = sfxVolume;

        ApplyVolume();
    }

    private void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    private void SetBgmVolume(float volume)
    {
        bgmVolume = volume;
        PlayerPrefs.SetFloat("BgmVolume", bgmVolume);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    private void SetSfxVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        AudioManager.instance.SetBgmVolume(bgmVolume * masterVolume);
        AudioManager.instance.SetSfxVolume(sfxVolume * masterVolume);
        UpdateVolumeUI();
    }

    private void UpdateVolumeUI()
    {
        masterVolumeText.text = Mathf.Round(masterVolume * 100).ToString();
        bgmText.text = Mathf.Round(bgmVolume * 100).ToString();
        sfxText.text = Mathf.Round(sfxVolume * 100).ToString();
    }

    public void ToggleUI()
    {
        volumeUI.SetActive(!volumeUI.activeSelf);
    }
}
