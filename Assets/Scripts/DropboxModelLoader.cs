using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility; // Make sure GLTFUtility is imported in your project

public class DropboxModelLoader : MonoBehaviour
{
    [Header("Metadata JSON URL (Dropbox raw link)")]
    public string metadataUrl;

    [Header("Where to spawn the model")]
    public Transform spawnPoint;

    [Header("Optional loading indicator")]
    public GameObject loadingIndicator;

    private GameObject currentModel;

    // Call this function from a button
    public void LoadLatestModel()
    {
        StartCoroutine(LoadFromMetadata());
    }

    private IEnumerator LoadFromMetadata()
    {
        Debug.Log("LoadFromMetadata() called");
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        // 1️⃣ Fetch metadata.json from Dropbox
        UnityWebRequest request = UnityWebRequest.Get(metadataUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch metadata: " + request.error);
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            yield break;
        }

        string jsonText = request.downloadHandler.text;
        Metadata metadata = JsonUtility.FromJson<Metadata>(jsonText);

        if (string.IsNullOrEmpty(metadata.latestModelURL))
        {
            Debug.LogError("No latestModelURL found in metadata.json");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            yield break;
        }

        Debug.Log("Loading model from URL: " + metadata.latestModelURL);

        // 2️⃣ Remove previous model if exists
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        // 3️⃣ Load the GLB asynchronously using GLTFUtility
        UnityWebRequest modelRequest = UnityWebRequest.Get(metadata.latestModelURL);
        yield return modelRequest.SendWebRequest();

        if (modelRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download model: " + modelRequest.error);
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            yield break;
        }

        byte[] glbData = modelRequest.downloadHandler.data;

        // Load model
        GameObject loaded = Importer.LoadFromBytes(glbData);
        loaded.transform.SetParent(spawnPoint, false);
        loaded.transform.localPosition = Vector3.zero;
        loaded.transform.localRotation = Quaternion.identity;

        currentModel = loaded;

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        Debug.Log("Model loaded successfully!");
    }

    [System.Serializable]
    private class Metadata
    {
        public string latestModelURL;
    }
}
