using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
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
    [SerializeField] private GameObject loginPanel; // Panel chứa tất cả UI login (background)
    
    [Header("Optional UI Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text logoutHintText; // Text hiển thị hint "Press Tab to logout"
    
    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "https://localhost:7237/api/Auth/Login";
    
    private const string TOKEN_KEY = "JWT_TOKEN";
    private const string USER_ID_KEY = "USER_ID";
    private const string USERNAME_KEY = "USERNAME";
    private const string ROLE_KEY = "USER_ROLE";
    
    private bool isLoggingIn = false;

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
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
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
        if (isLoggingIn) return;
        
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
        // Clear saved data
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.DeleteKey(ROLE_KEY);
        PlayerPrefs.Save();
        
        // Deactivate game and unlock cursor
        StarterAssets.StarterAssetsInputs.SetGameActive(false);
        
        // Show login UI again
        ShowLoginUI();
    }
    
    // Method called when ESC is pressed during gameplay
    // Pauses game and shows Continue/Logout menu
    public void ShowPauseMenu()
    {
        GameController.ShowPanel(loginPanel);
        
        if (usernameInput != null) usernameInput.gameObject.SetActive(false);
        if (passwordInput != null) passwordInput.gameObject.SetActive(false);
        if (loginButton != null) loginButton.gameObject.SetActive(false);
        
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
        // Show login panel (background)
        if (loginPanel != null) loginPanel.SetActive(true);
        
        // Show login inputs and buttons
        if (usernameInput != null) usernameInput.gameObject.SetActive(true);
        if (passwordInput != null) passwordInput.gameObject.SetActive(true);
        if (loginButton != null) loginButton.gameObject.SetActive(true);
        
        // Hide logout button during login
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
        
        // Show login panel (background)
        if (loginPanel != null) loginPanel.SetActive(true);
        
        // Show login inputs (allow login with new account)
        if (usernameInput != null) usernameInput.gameObject.SetActive(true);
        if (passwordInput != null) passwordInput.gameObject.SetActive(true);
        if (loginButton != null) loginButton.gameObject.SetActive(true);
        
        // Show continue and logout buttons
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
        // HIDE LOGIN PANEL - ẨN HOÀN TOÀN BACKGROUND
        if (loginPanel != null) loginPanel.SetActive(false);
        
        // Hide all UI components during gameplay
        if (usernameInput != null) usernameInput.gameObject.SetActive(false);
        if (passwordInput != null) passwordInput.gameObject.SetActive(false);
        if (loginButton != null) loginButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (logoutButton != null) logoutButton.gameObject.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        

        // Keep statusText visible (not inside loginPanel)
    }
    
    private IEnumerator LoginCoroutine(string username, string password)
    {
        isLoggingIn = true;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        if (loginButton != null)
        {
            loginButton.interactable = false;
        }
        
        ShowStatus("Logging in...", false);
        
        // Prepare request data
        LoginRequest loginData = new LoginRequest
        {
            userName = username,
            password = password
        };
        
        string jsonData = JsonUtility.ToJson(loginData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        // Create web request
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("accept", "text/plain");
            request.certificateHandler = new AcceptAllCertificates();
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
                        // Save token and user data
                        PlayerPrefs.SetString(TOKEN_KEY, response.token);
                        PlayerPrefs.SetInt(USER_ID_KEY, response.id);
                        PlayerPrefs.SetString(USERNAME_KEY, response.userName);
                        PlayerPrefs.SetString(ROLE_KEY, response.roleName);
                        PlayerPrefs.Save();
                        
                        // Show user info on statusText
                        ShowUserInfo(response);
                        
                        // Activate game and lock cursor
                        StarterAssets.StarterAssetsInputs.SetGameActive(true);
                        
                        // Hide login UI
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
        
        isLoggingIn = false;
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
    
    // Public method to get stored token
    public static string GetStoredToken()
    {
        return PlayerPrefs.GetString(TOKEN_KEY, "");
    }
    
    // Public method to check if user is logged in
    public static bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(GetStoredToken());
    }
    
    // Public method to logout
    public static void Logout()
    {
        string username = GetUsername();
        int userId = GetUserId();
        
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.DeleteKey(ROLE_KEY);
        PlayerPrefs.Save();
        
        StarterAssets.StarterAssetsInputs.SetGameActive(false);
        
        Debug.Log($"🔓 LOGOUT | User: {username} | ID: {userId}");
    }
    
    // Public method to get user info
    public static string GetUsername()
    {
        return PlayerPrefs.GetString(USERNAME_KEY, "");
    }
    
    public static int GetUserId()
    {
        return PlayerPrefs.GetInt(USER_ID_KEY, -1);
    }
    
    public static string GetUserRole()
    {
        return PlayerPrefs.GetString(ROLE_KEY, "");
    }
}

// Certificate handler to accept self-signed certificates (localhost HTTPS)
public class AcceptAllCertificates : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // WARNING: Only use this for localhost development
        // In production, properly validate certificates
        return true;
    }
}
