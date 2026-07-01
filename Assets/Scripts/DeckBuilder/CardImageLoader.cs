using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CardImageLoader : MonoBehaviour
{
    private static Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public void LoadImage(string url, RawImage target, System.Action onComplete = null)
    {
        if (string.IsNullOrEmpty(url) || target == null) return;

        if (cache.TryGetValue(url, out Texture2D cached))
        {
            target.texture = cached;
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(DownloadImage(url, target, onComplete));
    }

    private IEnumerator DownloadImage(string url, RawImage target, System.Action onComplete)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            cache[url] = texture;

            if (target != null)
                target.texture = texture;

            onComplete?.Invoke();
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