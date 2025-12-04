using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using TMPro;
using Script.UI;

[Serializable]
public class LoginRequest
{
    public string userName;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public int id;
    public string userName;
    public string fullName;
    public int roleID;
    public string roleName;
    public string token;
    public string message;
}

public class Login : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    
    [Header("Optional UI Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "https://localhost:7237/api/Auth/Login";
    
    private const string TokenKey = "JWT_TOKEN";
    private const string UserIdKey = "USER_ID";
    private const string UsernameKey = "USERNAME";
    private const string RoleKey = "USER_ROLE";
    
    private bool _isLoggingIn;

    void Start()
    {
        StarterAssets.StarterAssetsInputs.UnlockCursor();
        Time.timeScale = 1.0f;
        
        if (usernameInput == null)
        {
            Debug.LogError("Login: Username Input is not assigned!");
        }
        
        if (passwordInput == null)
        {
            Debug.LogError("Login: Password Input is not assigned!");
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
        }
        
        if (loginButton == null)
        {
            Debug.LogError("Login: Login Button is not assigned!");
        }
        else
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutButtonClicked);
        }
        
        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (registerPanel != null)
        {
            registerPanel.SetActive(false);
        }
        
        if (statusText != null)
        {
            statusText.text = "";
        }
        
        if (IsLoggedIn())
        {
            ShowLoggedInUI();
        }
        else
        {
            ShowLoginUI();
        }
        
        if (loginPanel != null)
        {
            GameController.ShowPanel(loginPanel);
        }
    }


    public void OnLoginButtonClicked()
    {
        if (_isLoggingIn) return;
        
        if (usernameInput == null || passwordInput == null)
        {
            Debug.LogError("Login: UI components not configured!");
            ShowStatus("Error: UI components not properly configured", true);
            return;
        }
        
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        
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
        
        StartCoroutine(LoginCoroutine(username, password));
    }
    
    public void OnContinueButtonClicked()
    {
        if (!IsLoggedIn())
        {
            ShowStatus("No saved account found!", true);
            return;
        }
        
        string username = GetUsername();
        int userId = GetUserId();
        string role = GetUserRole();
        
        GameController.HidePanel(loginPanel);
        StarterAssets.StarterAssetsInputs.SetGameActive(true);
        HideLoginUIForGameplay();
        
        if (statusText != null)
        {
            statusText.text = $"🔐 LOGGED IN\n" +
                             $"User: {username}\n" +
                             $"Role: {role}\n" +
                             $"ID: {userId}";
            statusText.color = Color.cyan;
        }
    }
    
    public void OnLogoutButtonClicked()
    {
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(UsernameKey);
        PlayerPrefs.DeleteKey(RoleKey);
        PlayerPrefs.Save();
        
        StarterAssets.StarterAssetsInputs.SetGameActive(false);
        
        ShowLoginUI();
        
        if (loginPanel != null)
        {
            GameController.ShowPanel(loginPanel);
        }
    }
    
    public void OnRegisterButtonClicked()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        
        if (registerPanel != null)
        {
            registerPanel.SetActive(true);
        }
    }
    
    public void ShowLoginFromRegister()
    {
        if (registerPanel != null)
        {
            registerPanel.SetActive(false);
        }
        
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
    }
    
    // Method called when ESC is pressed during gameplay
    // Pauses game and shows Continue/Logout menu
    public void ShowPauseMenu()
    {
        GameController.ShowPanel(loginPanel);
        
        if (usernameInput != null) usernameInput.gameObject.SetActive(false);
        if (passwordInput != null) passwordInput.gameObject.SetActive(false);
        if (loginButton != null) loginButton.gameObject.SetActive(false);
        if (registerButton != null) registerButton.gameObject.SetActive(false);
        
        if (continueButton != null) continueButton.gameObject.SetActive(true);
        if (logoutButton != null) logoutButton.gameObject.SetActive(true);
        
        if (statusText != null)
        {
            string username = GetUsername();
            string role = GetUserRole();
            statusText.text = $"⏸️ GAME PAUSED\n\nUser: {username}\nRole: {role}\n\nPress Continue to resume or Logout";
            statusText.color = Color.yellow;
        }
    }
    
    private void ShowLoginUI()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        
        if (usernameInput != null) usernameInput.gameObject.SetActive(true);
        if (passwordInput != null) passwordInput.gameObject.SetActive(true);
        if (loginButton != null) loginButton.gameObject.SetActive(true);
        if (registerButton != null) registerButton.gameObject.SetActive(true);
        
        if (logoutButton != null) logoutButton.gameObject.SetActive(false);
        
        // Show or hide continue button based on saved data
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(IsLoggedIn());
        }
        
        // Clear input fields
        if (usernameInput != null) usernameInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
        
        if (statusText != null)
        {
            statusText.text = "";
        }
    }
    
    private void ShowLoggedInUI()
    {
        string username = GetUsername();
        int userId = GetUserId();
        string role = GetUserRole();
        
        if (loginPanel != null) loginPanel.SetActive(true);
        
        if (usernameInput != null) usernameInput.gameObject.SetActive(true);
        if (passwordInput != null) passwordInput.gameObject.SetActive(true);
        if (loginButton != null) loginButton.gameObject.SetActive(true);
        if (registerButton != null) registerButton.gameObject.SetActive(true);
        
        if (continueButton != null) continueButton.gameObject.SetActive(true);
        if (logoutButton != null) logoutButton.gameObject.SetActive(true);
        
        // Show saved account info
        if (statusText != null)
        {
            statusText.text = $"💾 Saved Account:\nUser: {username}\nRole: {role}\nID: {userId}\n\nPress Continue or Login with new account";
            statusText.color = Color.yellow;
        }
    }
    
    private void HideLoginUIForGameplay()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        
        if (usernameInput != null) usernameInput.gameObject.SetActive(false);
        if (passwordInput != null) passwordInput.gameObject.SetActive(false);
        if (loginButton != null) loginButton.gameObject.SetActive(false);
        if (registerButton != null) registerButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (logoutButton != null) logoutButton.gameObject.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    private IEnumerator LoginCoroutine(string username, string password)
    {
        _isLoggingIn = true;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        if (loginButton != null)
        {
            loginButton.interactable = false;
        }
        
        ShowStatus("Logging in...", false);
        
        LoginRequest loginData = new LoginRequest
        {
            userName = username,
            password = password
        };
        
        string jsonData = JsonUtility.ToJson(loginData);
        
        using (UnityWebRequest request = APIHelper.CreateAuthenticatedPostRequest(apiUrl, jsonData))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            if (loginButton != null)
            {
                loginButton.interactable = true;
            }
            
            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    LoginResponse response = JsonUtility.FromJson<LoginResponse>(responseText);
                    
                    if (response != null && !string.IsNullOrEmpty(response.token))
                    {
                        PlayerPrefs.SetString(TokenKey, response.token);
                        PlayerPrefs.SetInt(UserIdKey, response.id);
                        PlayerPrefs.SetString(UsernameKey, response.userName);
                        PlayerPrefs.SetString(RoleKey, response.roleName);
                        PlayerPrefs.Save();
                        
                        ShowUserInfo(response);
                        
                        GameController.HidePanel(loginPanel);
                        StarterAssets.StarterAssetsInputs.SetGameActive(true);
                        HideLoginUIForGameplay();
                    }
                    else
                    {
                        Debug.LogError("Login Error: Server response missing token");
                        ShowStatus("Login failed: Invalid response from server", true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Login Error: Failed to parse response - {ex.Message}");
                    ShowStatus("Login failed: Could not parse server response", true);
                }
            }
            else
            {
                string errorMsg = $"Error: {request.error}";
                
                if (request.responseCode == 401)
                {
                    errorMsg = "Invalid username or password";
                    Debug.LogError($"Login Error: Authentication failed (401) - Invalid credentials for user '{username}'");
                }
                else if (request.responseCode == 0)
                {
                    errorMsg = "Cannot connect to server. Is the API running?";
                    Debug.LogError($"Login Error: Cannot connect to {apiUrl} - Check if API server is running on port 7237");
                }
                else
                {
                    Debug.LogError($"Login Error: {request.result} | Code: {request.responseCode} | {request.error}");
                }
                
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"Server Response: {request.downloadHandler.text}");
                }
                
                ShowStatus(errorMsg, true);
            }
        }
        
        _isLoggingIn = false;
    }
    
    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }
    }
    
    private void ShowUserInfo(LoginResponse response)
    {
        if (statusText != null)
        {
            statusText.text = $"🔐 LOGGED IN\n" +
                             $"User: {response.userName}\n" +
                             $"Full Name: {response.fullName}\n" +
                             $"Role: {response.roleName}\n" +
                             $"ID: {response.id}";
            statusText.color = Color.cyan;
        }
    }
    
    public static string GetStoredToken()
    {
        return PlayerPrefs.GetString(TokenKey, "");
    }
    
    public static bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(GetStoredToken());
    }
    
    public static void Logout()
    {
        string username = GetUsername();
        int userId = GetUserId();
        
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(UsernameKey);
        PlayerPrefs.DeleteKey(RoleKey);
        PlayerPrefs.Save();
        
        StarterAssets.StarterAssetsInputs.SetGameActive(false);
        
        Debug.Log($"🔓 LOGOUT | User: {username} | ID: {userId}");
    }
    
    public static string GetUsername()
    {
        return PlayerPrefs.GetString(UsernameKey, "");
    }
    
    public static int GetUserId()
    {
        return PlayerPrefs.GetInt(UserIdKey, -1);
    }
    
    public static string GetUserRole()
    {
        return PlayerPrefs.GetString(RoleKey, "");
    }
}

