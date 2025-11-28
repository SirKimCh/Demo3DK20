using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APIHelper : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string baseApiUrl = "https://localhost:7237/api";
    
    /// <summary>
    /// Create a GET request with authentication
    /// </summary>
    public static UnityWebRequest CreateAuthenticatedGetRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.certificateHandler = new AcceptAllCertificates();
        
        string token = Login.GetStoredToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
        
        request.SetRequestHeader("accept", "text/plain");
        return request;
    }
    
    /// <summary>
    /// Create a POST request with authentication
    /// </summary>
    public static UnityWebRequest CreateAuthenticatedPostRequest(string url, string jsonBody)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.certificateHandler = new AcceptAllCertificates();
        
        string token = Login.GetStoredToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "text/plain");
        
        return request;
    }
    
    /// <summary>
    /// Create a PUT request with authentication
    /// </summary>
    public static UnityWebRequest CreateAuthenticatedPutRequest(string url, string jsonBody)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        
        UnityWebRequest request = UnityWebRequest.Put(url, bodyRaw);
        request.certificateHandler = new AcceptAllCertificates();
        
        string token = Login.GetStoredToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "text/plain");
        
        return request;
    }
    
    /// <summary>
    /// Create a DELETE request with authentication
    /// </summary>
    public static UnityWebRequest CreateAuthenticatedDeleteRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.certificateHandler = new AcceptAllCertificates();
        
        string token = Login.GetStoredToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
        
        request.SetRequestHeader("accept", "text/plain");
        return request;
    }
}

