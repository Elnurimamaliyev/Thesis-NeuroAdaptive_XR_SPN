using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TrainingController : MonoBehaviour
{
    public GameObject MainCanvas;
    private ExperimentController experimentController;

    void Awake()
    {
        if (MainCanvas == null)
        {
            GameObject existingCanvas = GameObject.Find("MainTrainingCanvas");
            if (existingCanvas != null)
            {
                MainCanvas = existingCanvas;
                Debug.Log($"Found existing MainTrainingCanvas");
            }
        }
    } 

    [Header("UI Behavior")]
    private float UI_Action_Duration = 2.0f;

    [Header("Parent Canvas References")]
    [SerializeField] private GameObject TrainingButtons;    
    [SerializeField] private GameObject TrainingWindows;        

    [Header("Training Buttons")]
    public Button SquareButton;
    public Button TriangleButton;
    public Button CircleButton;

    [Header("Training Windows")]
    [SerializeField] private GameObject TrainingWindow;

    private GameObject currentWindow;

    private void Start()
    {
        // Find experiment controller reference
        experimentController = FindAnyObjectByType<ExperimentController>();

        // Make sure we have a valid MainCanvas reference
        if (MainCanvas == null)
        {
            Debug.LogError("TrainingController: MainCanvas reference is missing!");
            MainCanvas = GameObject.Find("MainTrainingCanvas");
            if (MainCanvas == null)
            {
                Debug.LogError("Failed to find MainTrainingCanvas by name!");
            }
        }

        // Get timing from ExperimentController if available
        if (experimentController != null)
        {
            UI_Action_Duration = experimentController.UI_Action_Duration;
        }

        // Add click listener for the button
        SquareButton.onClick.AddListener(() => HandleButtonClick("Square", TrainingWindow));
        TriangleButton.onClick.AddListener(() => HandleButtonClick("Triangle", TrainingWindow));
        CircleButton.onClick.AddListener(() => HandleButtonClick("Circle", TrainingWindow));

        // Log startup for debugging
        Debug.Log("TrainingController initialized successfully");
    }


    // Button click handler
    private void HandleButtonClick(string buttonName, GameObject windowToOpen)
    {
        Debug.Log($"TrainingController: HandleButtonClick for {buttonName}");
        
        // Now handles Square, Triangle, and Circle icons
        if (buttonName == "Square" || buttonName == "Triangle" || buttonName == "Circle")
        {            
            // Show appropriate window based on the button name
            StartCoroutine(OpenAndCloseWindow(windowToOpen));
        }
        else
        {
            Debug.LogWarning($"Unrecognized button name in training: {buttonName}");
        }
    }

    private void Log_ui_action_start() 
    { 
        // Log UI actions for Training
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = ExperimentController.Instance.GetExperimentSettings();
        DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "ui_action", "start");
    }

    private void Log_ui_action_end() 
    { 
        // Log UI actions for Training
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = ExperimentController.Instance.GetExperimentSettings();
        DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "ui_action", "end");
    }

    // Window management coroutine
    private IEnumerator OpenAndCloseWindow(GameObject windowToShow)
    {
        // Log UI action start
        Log_ui_action_start();
        
        // Validate window reference
        if (windowToShow == null)
        {
            Debug.LogError("Attempted to open null window");
            currentWindow = null;
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
        
        // Log UI action end ONLY HERE
        Log_ui_action_end();
        
        // Close current window without affecting button visibility
        Debug.Log($"Closing window: {windowToShow.name}");
        if (currentWindow) currentWindow.gameObject.SetActive(false);
        
        // Reset state
        currentWindow = null;
        
        Debug.Log("Training UI action completed");
    }
    
    
    // Methods to handle content visibility directly from the controller
    public void ShowMainCanvas()
    {
        // Show main canvas
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(true);
            Debug.Log($"TrainingController: Showing MainCanvas {MainCanvas.name}");
        }
    }

    public void HideMainCanvas()
    {
        // Hide main canvas
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(false);
            Debug.Log($"TrainingController: Hiding MainCanvas {MainCanvas.name}");
        }
    }
}