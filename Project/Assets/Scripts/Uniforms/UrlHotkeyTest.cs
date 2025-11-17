using UnityEngine;
using System.Collections;

public class UrlHotkeyTest : MonoBehaviour
{
    public PatternDownloader downloader;
    [TextArea] public string urlBlue;
    [TextArea] public string urlPurple;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            StartCoroutine(downloader.ApplyFromUrl(urlBlue, true,  new Vector2(10,10), 1f));
        if (Input.GetKeyDown(KeyCode.P))
            StartCoroutine(downloader.ApplyFromUrl(urlPurple, false, new Vector2(10,10), 1f));
    }
}