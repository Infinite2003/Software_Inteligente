using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CardImageLoader : MonoBehaviour
{
    private static Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public void LoadImage(string url, RawImage target)
    {
        if (string.IsNullOrEmpty(url) || target == null) return;

        if (cache.TryGetValue(url, out Texture2D cached))
        {
            target.texture = cached;
            return;
        }

        StartCoroutine(DownloadImage(url, target));
    }

    private IEnumerator DownloadImage(string url, RawImage target)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            cache[url] = texture;

            if (target != null)
                target.texture = texture;
        }
        else
        {
            Debug.LogWarning($"[CardImageLoader] Error cargando imagen: {url} → {request.error}");
        }
    }

    public static void ClearCache()
    {
        foreach (var texture in cache.Values)
            Destroy(texture);

        cache.Clear();
        Debug.Log("[CardImageLoader] Caché de imágenes limpiado.");
    }
}