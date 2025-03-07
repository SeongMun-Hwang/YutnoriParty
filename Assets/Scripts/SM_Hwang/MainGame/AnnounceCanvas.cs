using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AnnounceCanvas : NetworkBehaviour
{
    [SerializeField] GameObject announceTmp;

    public void ShowAnnounceText(string str, float time = 2f, Color? color = null)
    {
        if (color == null) color = Color.black;
        StartCoroutine(ShowAnnounceTextIEnumerator(str, time, color.Value));
    }
    [ClientRpc]
    public void ShowAnnounceTextClientRpc(string str, float time = 2f, Color? color = null)
    {
        if (color == null) color = Color.black;
        StartCoroutine(ShowAnnounceTextIEnumerator(str, time, color.Value));
    }
    private IEnumerator ShowAnnounceTextIEnumerator(string str, float time, Color color)
    {
        GameObject go = Instantiate(announceTmp, transform.position, Quaternion.identity, transform);
        go.GetComponent<TextMeshProUGUI>().text = str;

        Color textColor = color;
        textColor.a = 1f;
        go.GetComponent<TextMeshProUGUI>().color = textColor;

        for (float f = 0; f < time; f += Time.deltaTime)
        {
            textColor.a = Mathf.Lerp(1f, 0f, f / time);
            go.GetComponent<TextMeshProUGUI>().color = textColor;
            yield return null;
        }
        Destroy(go);
    }
}
