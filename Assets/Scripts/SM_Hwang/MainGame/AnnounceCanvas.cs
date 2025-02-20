using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AnnounceCanvas : NetworkBehaviour
{
    [SerializeField] GameObject announceTmp;

    //[ClientRpc]
    public void ShowAnnounceText(string str,float time)
    {
        StartCoroutine(ShowAnnounceTextIEnumerator(str, time));
    }
    private IEnumerator ShowAnnounceTextIEnumerator(string str, float time)
    {
        GameObject go=Instantiate(announceTmp,transform.position,Quaternion.identity,transform);
        go.GetComponent<TextMeshProUGUI>().text = str;

        Color textColor= go.GetComponent<TextMeshProUGUI>().color;
        textColor.a = 1f;
        go.GetComponent<TextMeshProUGUI>().color = textColor;

        for(float f = 0; f < time; f += Time.deltaTime)
        {
            textColor.a = Mathf.Lerp(1f, 0f, f/time);
            go.GetComponent<TextMeshProUGUI>().color = textColor;
            yield return null;
        }
        Destroy(go);
    }
}
