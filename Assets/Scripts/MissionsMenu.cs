using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MissionsMenu : MonoBehaviour
{
    public static MissionsMenu Instance;   // global access

    [Header("Missions order")]
    public MissionConfig[] missions;       // assign ScriptableObjects in Inspector

    public int CurrentIndex { get; private set; } = -1;

    public MissionConfig CurrentMission =>
        (CurrentIndex >= 0 && CurrentIndex < missions.Length)
            ? missions[CurrentIndex]
            : null;

    private void Awake()
    {
        // simple singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartMission(int index, bool skipPreface = false)
    {
        if (missions == null || missions.Length == 0)
        {
            Debug.LogWarning("MissionsMenu: missions array is empty.");
            return;
        }

        if (index < 0 || index >= missions.Length)
        {
            Debug.LogWarning("MissionsMenu: invalid mission index " + index);
            return;
        }

        CurrentIndex = index;
        Debug.Log("Setting async...");
        await SceneReturn.SetAsync("AttackerSchene", CurrentIndex);
        Debug.Log("Set!");

        if (CurrentMission.showPreface && !skipPreface)
        {
            SceneManager.LoadScene("Preface");
        }
        else
        {
            SceneManager.LoadScene("AttackerSchene");
        }
    }

    public async Task StartMissionAfterPreface()
    {
        SceneManager.LoadScene("AttackerSchene");
    }

    public void Reset()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        CurrentIndex = 0;
        SceneManager.LoadScene("Menu");
    }

    public void ToLoseScreen()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        CurrentIndex = 0;
        SceneManager.LoadScene("Lose");
    }

    public async Task LoadNextMissionOrBackToMenu()
    {
        if (missions == null || missions.Length == 0)
        {
            SceneManager.LoadScene("Menu");
            return;
        }

        int nextIndex = CurrentIndex + 1;

        if (nextIndex < missions.Length)
        {
            await StartMission(nextIndex);
        }
        else
        {
            await SceneReturn.SetAsync("AttackerSchene", 0);

            Debug.Log("No more missions, going back to main menu.");
            SceneManager.LoadScene("End");
        }
    }
}