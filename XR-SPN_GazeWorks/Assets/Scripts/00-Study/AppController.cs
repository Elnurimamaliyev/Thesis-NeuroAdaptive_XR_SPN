using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AppController : MonoBehaviour
{
    public GameObject MainCanvas;
    private ExperimentController experimentController;

    void Awake()
    {
        if (MainCanvas == null)
        {
            GameObject existingCanvas = GameObject.Find("MainAppCanvas");
            if (existingCanvas != null)
            {
                MainCanvas = existingCanvas;
                Debug.Log($"Found existing MainAppCanvas");
            }
        }
    } 

    [Header("UI Behavior")]
    private float UI_Action_Duration = 2.0f;

    [Header("Parent Canvas References")]
    [SerializeField] private GameObject App_Buttons;    
    [SerializeField] private GameObject App_Windows;        

    [Header("App Buttons")]
    public Button SafariButton;
    public Button TvButton;
    public Button SettingsButton;
    public Button FilesButton;         
    public Button MusicButton;

    [Header("App Windows")]
    [SerializeField] private GameObject SafariWindow;
    [SerializeField] private GameObject TvWindow;
    [SerializeField] private GameObject SettingsWindow;
    [SerializeField] private GameObject FilesWindow; 
    [SerializeField] private GameObject MusicWindow;

    private bool isWindowOpen = false;
    private GameObject currentWindow;
    private Coroutine currentWindowCoroutine;
    private float lastWindowOperationTime = 0f;

    private void Start()
    {
        // Find experiment controller reference
        experimentController = FindAnyObjectByType<ExperimentController>();

        // Make sure we have a valid MainCanvas reference
        if (MainCanvas == null)
        {
            Debug.LogError("AppController: MainCanvas reference is missing!");
            MainCanvas = GameObject.Find("MainAppCanvas");
            if (MainCanvas == null)
            {
                Debug.LogError("Failed to find MainAppCanvas by name!");
            }
        }

        // Get timing from ExperimentController if available
        if (experimentController != null)
        {
            UI_Action_Duration = experimentController.UI_Action_Duration;
        }

        // Add click listeners for buttons - with button name and window
        SafariButton.onClick.AddListener(() => HandleButtonClick("Safari", SafariWindow));
        TvButton.onClick.AddListener(() => HandleButtonClick("Tv", TvWindow));
        SettingsButton.onClick.AddListener(() => HandleButtonClick("Settings", SettingsWindow));
        FilesButton.onClick.AddListener(() => HandleButtonClick("Files", FilesWindow));
        MusicButton.onClick.AddListener(() => HandleButtonClick("Music", MusicWindow));

        // Log startup for debugging
        Debug.Log("AppController initialized successfully");
    }

    // Button click handler
    private void HandleButtonClick(string buttonName, GameObject windowToShow)
    {
        // Log UI action start
        Log_ui_action_start();

        // Check if window is already open or coroutine is running
        if (isWindowOpen || currentWindowCoroutine != null)
        {
            // Force reset if more than 3 seconds has passed since last window operation
            // This prevents the controller from getting permanently stuck
            if (Time.time - lastWindowOperationTime > 3.0f)
            {
                Debug.LogWarning($"Forcing reset of window state for {buttonName}. Previous window may have been stuck.");
                ForceResetWindowState();
            }
            else
            {
                Debug.Log($"Window operation already in progress, ignoring click on {buttonName}");
                return;
            }
        }
            
        // Log the interaction for debugging
        Debug.Log($"AppController: HandleButtonClick for {buttonName}");
        
        // Record timestamp of window operation start
        lastWindowOperationTime = Time.time;
            
        // Start window display coroutine - make sure it completes fully
        currentWindowCoroutine = StartCoroutine(OpenAndCloseWindow(windowToShow, buttonName));
    }

    private void Log_ui_action_start() 
    { 
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = ExperimentController.Instance.GetExperimentSettings();
        DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "ui_action", "start");
    }
    private void Log_ui_action_end() 
    { 
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = ExperimentController.Instance.GetExperimentSettings();
        DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "ui_action", "end");
    }

    // Add a method to force reset window state if it gets stuck
    private void ForceResetWindowState()
    {
        isWindowOpen = false;
        
        if (currentWindowCoroutine != null)
        {
            StopCoroutine(currentWindowCoroutine);
            currentWindowCoroutine = null;
        }
        
        if (currentWindow != null)
        {
            currentWindow.SetActive(false);
            currentWindow = null;
        }
        
        Debug.Log("Window state forcibly reset");
    }

    // Window management coroutine
    private IEnumerator OpenAndCloseWindow(GameObject windowToShow, string buttonName)
    {
        try 
        {
            isWindowOpen = true;
            
            // Validate window reference
            if (windowToShow == null)
            {
                Debug.LogError("Attempted to open null window");
                isWindowOpen = false;
                currentWindowCoroutine = null;
                yield break;
            }
            
            Debug.Log($"Opening window: {windowToShow.name}");
                    
            // Show selected window
            windowToShow.gameObject.SetActive(true);
            currentWindow = windowToShow;
            
            // Wait for display duration
            float waitDuration = UI_Action_Duration;
            Debug.Log($"Waiting {waitDuration}s to display window before closing");
            yield return new WaitForSeconds(waitDuration);
            
            // Log UI action end
            Log_ui_action_end();
            
            // Close current window without affecting button visibility
            Debug.Log($"Closing window: {windowToShow.name}");
            if (currentWindow) currentWindow.gameObject.SetActive(false);
            
            Debug.Log("App UI action completed");
        }
        finally
        {
            // Always reset state even if an exception occurred
            isWindowOpen = false;
            currentWindow = null;
            currentWindowCoroutine = null;
        }
    }
    
    // Methods to handle content visibility directly from the controller
    public void ShowMainCanvas()
    {
        // Show main canvas
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(true);
            Debug.Log($"AppController: Showing MainCanvas {MainCanvas.name}");
        }
    }

    public void HideMainCanvas()
    { 
        // Hide main canvas
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(false);
            Debug.Log($"AppController: Hiding MainCanvas {MainCanvas.name}");
        }
    }

}