using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using TMPro;

[Serializable]
public class RegisterRequest
{
    public string userName;
    public string password;
    public string fullName;
    public string phoneNumber;
}

[Serializable]
public class RegisterResponse
{
    public int id;
    public string userName;
    public string fullName;
    public string phoneNumber;
    public int roleID;
    public string roleName;
}

public class Register : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField fullNameInput;
    [SerializeField] private TMP_InputField phoneNumberInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backToLoginButton;
    
    [Header("Optional UI Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "https://localhost:7237/api/Auth/Register";
    
    [Header("References")]
    [SerializeField] private Login loginScript;
    
    private bool _isRegistering;

    void Start()
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
        }
        
        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        }
        
        if (backToLoginButton != null)
        {
            backToLoginButton.onClick.RemoveAllListeners();
            backToLoginButton.onClick.AddListener(OnBackToLoginClicked);
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (statusText != null)
        {
            statusText.text = "";
        }
        
        if (loginScript == null)
        {
            loginScript = FindFirstObjectByType<Login>();
        }
    }

    public void OnRegisterButtonClicked()
    {
        if (_isRegistering) return;
        
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";
        string fullName = fullNameInput != null ? fullNameInput.text.Trim() : "";
        string phoneNumber = phoneNumberInput != null ? phoneNumberInput.text.Trim() : "";
        
        if (string.IsNullOrEmpty(username))
        {
            ShowStatus("Username cannot be empty", true);
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("Password cannot be empty", true);
            return;
        }
        
        if (string.IsNullOrEmpty(fullName))
        {
            ShowStatus("Full name cannot be empty", true);
            return;
        }
        
        if (string.IsNullOrEmpty(phoneNumber))
        {
            ShowStatus("Phone number cannot be empty", true);
            return;
        }
        
        StartCoroutine(RegisterCoroutine(username, password, fullName, phoneNumber));
    }

    private IEnumerator RegisterCoroutine(string username, string password, string fullName, string phoneNumber)
    {
        _isRegistering = true;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        if (registerButton != null)
        {
            registerButton.interactable = false;
        }
        
        ShowStatus("Registering...", false);
        
        RegisterRequest registerData = new RegisterRequest
        {
            userName = username,
            password = password,
            fullName = fullName,
            phoneNumber = phoneNumber
        };
        
        string jsonData = JsonUtility.ToJson(registerData);
        
        using (UnityWebRequest request = APIHelper.CreateAuthenticatedPostRequest(apiUrl, jsonData))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            if (registerButton != null)
            {
                registerButton.interactable = true;
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                RegisterResponse response;
                
                try
                {
                    response = JsonUtility.FromJson<RegisterResponse>(responseText);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Register Error: Failed to parse response - {ex.Message}");
                    ShowStatus("Registration failed: Could not parse response", true);
                    _isRegistering = false;
                    yield break;
                }
                
                if (response != null && response.id > 0)
                {
                    ShowStatus($"Registration successful! User: {response.userName}", false);
                    
                    if (usernameInput != null) usernameInput.text = "";
                    if (passwordInput != null) passwordInput.text = "";
                    if (fullNameInput != null) fullNameInput.text = "";
                    if (phoneNumberInput != null) phoneNumberInput.text = "";
                    
                    _isRegistering = false;
                    yield return new WaitForSeconds(2f);
                    OnBackToLoginClicked();
                    yield break;
                }
                else
                {
                    Debug.LogError("Register Error: Invalid response from server");
                    ShowStatus("Registration failed: Invalid response", true);
                }
            }
            else
            {
                string errorMsg = $"Error: {request.error}";
                
                if (request.responseCode == 400)
                {
                    errorMsg = "Invalid data or username already exists";
                    Debug.LogError($"Register Error: Bad Request (400) - {request.downloadHandler.text}");
                }
                else if (request.responseCode == 409)
                {
                    errorMsg = "Username already exists";
                    Debug.LogError($"Register Error: Conflict (409) - Username '{username}' already exists");
                }
                else if (request.responseCode == 0)
                {
                    errorMsg = "Cannot connect to server";
                    Debug.LogError($"Register Error: Cannot connect to {apiUrl}");
                }
                else
                {
                    Debug.LogError($"Register Error: {request.result} | Code: {request.responseCode} | {request.error}");
                }
                
                ShowStatus(errorMsg, true);
            }
        }
        
        _isRegistering = false;
    }

    private void OnBackToLoginClicked()
    {
        if (loginScript != null)
        {
            loginScript.ShowLoginFromRegister();
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }
    }
}
