using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AuthSceneController : MonoBehaviour
{
    public enum AccountMode { SignIn, SignUp }

    [Header("Panels")]
    [SerializeField] GameObject panelWelcome;
    [SerializeField] GameObject panelAccount;

    [Header("Welcome UI")]
    [SerializeField] Button guestButton;
    [SerializeField] Button openAccountButton;
    [SerializeField] TMP_Text welcomeStatus;

    [Header("Account UI")]
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] TMP_InputField confirmPasswordInput;

    // Two mode buttons (replace the toggle)
    [SerializeField] Button modeSignInButton;
    [SerializeField] Button modeSignUpButton;

    // One submit button that performs the selected mode
    [SerializeField] public Button submitButton;

    [SerializeField] Button backButton;
    [SerializeField] TMP_Text accountStatus;

    AccountMode mode = AccountMode.SignIn;
    bool busy;

    async void Awake()
    {
        // Hook events once (Awake runs before Start)
        if (guestButton) guestButton.onClick.AddListener(OnGuestClicked);
        if (openAccountButton) openAccountButton.onClick.AddListener(OnOpenAccountClicked);

        if (modeSignInButton) modeSignInButton.onClick.AddListener(() => SetMode(AccountMode.SignIn));
        if (modeSignUpButton) modeSignUpButton.onClick.AddListener(() => SetMode(AccountMode.SignUp));

        if (submitButton) submitButton.onClick.AddListener(OnSubmitClicked);
        if (backButton) backButton.onClick.AddListener(OnBackClicked);

        ShowWelcome("Initializing...");
        SetBusy(true);

        try
        {
            await InitServicesAsync();

            // Auto-restore cached session (works for anon + username/password).
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    LoadNextScene();
                    return;
                }
                catch
                {
                    // If restoring fails, fall through to manual choice
                }
            }

            ShowWelcome("Choose how you want to play.");
        }
        catch (Exception e)
        {
            ShowWelcome("Init failed: " + ShortErr(e));
        }
        finally
        {
            SetBusy(false);
        }
    }

    static async System.Threading.Tasks.Task InitServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
    }

    // ---------- UI State ----------

    void ShowWelcome(string status)
    {
        if (panelWelcome) panelWelcome.SetActive(true);
        if (panelAccount) panelAccount.SetActive(false);
        if (welcomeStatus) welcomeStatus.text = status ?? "";
        if (accountStatus) accountStatus.text = "";
    }

    void ShowAccount(string status = "")
    {
        if (panelWelcome) panelWelcome.SetActive(false);
        if (panelAccount) panelAccount.SetActive(true);

        if (accountStatus) accountStatus.text = status ?? "";
        ApplyModeUI();
    }

    void SetMode(AccountMode newMode)
    {
        mode = newMode;
        ApplyModeUI();
    }

    void ApplyModeUI()
    {
        bool isSignUp = mode == AccountMode.SignUp;

        if (confirmPasswordInput)
            confirmPasswordInput.gameObject.SetActive(isSignUp);

        // Reuse submitButton: set its label automatically
        if (submitButton)
        {
            var label = submitButton.GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = isSignUp ? "CREATE" : "ENTER";
        }

        // Optional “selected” feel: disable the active mode button
        if (modeSignInButton) modeSignInButton.interactable = (mode != AccountMode.SignIn);
        if (modeSignUpButton) modeSignUpButton.interactable = (mode != AccountMode.SignUp);
    }

    void SetBusy(bool value)
    {
        busy = value;

        if (guestButton) guestButton.interactable = !busy;
        if (openAccountButton) openAccountButton.interactable = !busy;

        if (modeSignInButton) modeSignInButton.interactable = !busy && (mode != AccountMode.SignIn);
        if (modeSignUpButton) modeSignUpButton.interactable = !busy && (mode != AccountMode.SignUp);

        if (submitButton) submitButton.interactable = !busy;
        if (backButton) backButton.interactable = !busy;

        if (usernameInput) usernameInput.interactable = !busy;
        if (passwordInput) passwordInput.interactable = !busy;
        if (confirmPasswordInput) confirmPasswordInput.interactable = !busy;
    }

    // ---------- Button Handlers ----------

    async void OnGuestClicked()
    {
        if (busy) return;
        SetBusy(true);
        if (welcomeStatus) welcomeStatus.text = "Signing in as guest...";

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();



            LoadNextScene();
        }
        catch (Exception e)
        {
            ShowWelcome("Guest sign-in failed: " + ShortErr(e));
            SetBusy(false);
        }
    }

    void OnOpenAccountClicked()
    {
        if (busy) return;
        SetMode(AccountMode.SignIn);
        ShowAccount();
    }

    void OnBackClicked()
    {
        if (busy) return;
        ShowWelcome("Choose how you want to play.");
    }

    async void OnSubmitClicked()
    {
        if (busy) return;

        string username = (usernameInput ? usernameInput.text : "")?.Trim();
        string password = (passwordInput ? passwordInput.text : "") ?? "";
        string confirm = (confirmPasswordInput ? confirmPasswordInput.text : "") ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            if (accountStatus) accountStatus.text = "Username and password required.";
            return;
        }

        bool isSignUp = mode == AccountMode.SignUp;
        if (isSignUp && password != confirm)
        {
            if (accountStatus) accountStatus.text = "Passwords do not match.";
            return;
        }

        SetBusy(true);
        if (accountStatus) accountStatus.text = isSignUp ? "Creating account..." : "Signing in...";

        try
        {
            if (isSignUp)
            {
                // If already signed in (usually as guest), LINK so they keep guest progress
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
                }
                else
                {
                    await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
                }

                LoadNextScene();
                return;
            }
            else
            {
                // Sign in to existing account.
                // If currently signed in as guest, we sign out first to avoid conflicts.
                if (AuthenticationService.Instance.IsSignedIn)
                    AuthenticationService.Instance.SignOut(); // does NOT clear token by default

                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
                LoadNextScene();
                return;
            }
        }
        catch (Exception e)
        {
            if (accountStatus) accountStatus.text = (isSignUp ? "Sign-up failed: " : "Sign-in failed: ") + ShortErr(e);
            SetBusy(false);
        }
    }

    public void OnStartClick()
    {
        _OnStartClick();
    }

    private async void _OnStartClick()
    {
        try
        {
            Debug.Log("Start Clicked");

            if (MissionsMenu.Instance == null)
            {
                Debug.LogWarning("MissionsMenu.Instance is null");
                return;
            }

            Debug.Log("Before PeekAsync");
            var data = await SceneReturn.PeekAsync();
            Debug.Log($"After PeekAsync: found={data.found} scene={data.sceneName} index={data.missionIndex}");

            Debug.Log("Before StartMission");

            await MissionsMenu.Instance.StartMission(data.missionIndex >= 0 ? data.missionIndex : 0);

            Debug.Log("After StartMission call");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ---------- Helpers ----------

    void LoadNextScene()
    {
        _OnStartClick();
    }

    static string ShortErr(Exception e)
    {
        // Avoid dumping huge stack traces into UI
        return e?.Message ?? "Unknown error";
    }
}
