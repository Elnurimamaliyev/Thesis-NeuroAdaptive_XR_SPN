using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Oculus.Interaction;
using System.Threading.Tasks;

public class ExperimentController : MonoBehaviour
{
    private static ExperimentController _instance;
    private GameObject[] sceneCanvases;
    private DataLogger dataLogger; 

    #region Configuration Settings

    [Header("Experiment Config")]
    [SerializeField] private int TotalBlockEndCount = 4;
    [SerializeField] private int TotalSceneEndCount = 3;

    [SerializeField] private int ExperimentSceneTrialEndCount = 50;
    [SerializeField] private int TrainingTrialsEndCount = 3;

    [Header("UI Elements")]
    [SerializeField] private Canvas breakCanvas;
    [SerializeField] private TextMeshProUGUI breakText;
    [SerializeField] private Canvas instructionCanvas;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Image instructionIconImage;
    [SerializeField] private Canvas fixationCanvas;
    [SerializeField] private GameObject fixationCross;

    [Header("Trial Timing Parameters")]
    [SerializeField] public float instruction_Duration = 2.0f;
    [SerializeField] public float gaze_Duration = 0.75f;
    [SerializeField] public float icon_Pop_Up_Duration = 0.5f;
    [SerializeField] public float UI_Action_Duration = 2.0f;
    [SerializeField] public float isi_Duration = 1.0f;
    #endregion

    #region State Tracking and Properties
    private bool waitFor_W_ExperimentStart = true;
    private bool waitFor_B_ForBlockFlow = false;
    private bool waitFor_T_TrainingScreen = false;
    private bool waitFor_T_TrainingStart = false;
    private bool waitFor_E_MoveToSceneFlow = false;
    private bool waitFor_R_RestPhase_AfterBlockEnd = false;

    // Block Flow State
    private bool isInTrainingMode = false;

    public List<int> ConditionOrder;
    public int blockIndex = 0;
    public int trialIndex = 0;
    public int sceneIndex = 0;
    public bool select = false;
    public bool withFeedback = false;


    public string currentSceneName;
    public string currentTargetIcon = "";
    private List<string> FlowIconSequence = new List<string>();

    // Scene Management -  Dictionary to store scene sequences for each block
    private List<string> blockScenes = new List<string>(); // Only for the current block's scenes
    private Dictionary<int, List<string>> SceneSequenceForCurrentBlock = new Dictionary<int, List<string>>();
    // Initialize ExperimentSceneNames directly
    private List<string> ExperimentSceneNames = new List<string> { "App", "Document", "Video" };
    // Predefined icon collections 
    private List<string> AppIconNames = new List<string> { "Safari", "Music", "Files", "Tv", "Settings" };
    private List<string> DocumentIconNames = new List<string> { "Save", "Undo", "Redo", "Export", "Close" };
    private List<string> VideoIconNames = new List<string> { "Rewind", "Forward", "Play", "Pause", "Stop" };
    private List<string> TrainingIconNames = new List<string> { "Square", "Triangle", "Circle" };
    // Generated sequences for each scene
    private List<string> AppIconSequence = new List<string>();
    private List<string> VideoIconSequence = new List<string>();
    private List<string> DocumentIconSequence = new List<string>();
    private List<string> TrainingIconSequence = new List<string>();
    #endregion

    #region Initialization and Setup

    void Awake()
    {
        // Improved singleton implementation for scene transitions
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Make this the singleton instance
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Get reference to existing DataLogger component on the same GameObject
        dataLogger = GetComponent<DataLogger>();
        if (dataLogger == null)
        {
            Debug.LogError("DataLogger component not found on ExperimentManager!");
            return;
        }

        // Initialize systems
        if (breakCanvas == null || instructionCanvas == null || fixationCanvas == null)
        {
        Debug.LogError("One or more required canvases not assigned in inspector!");
        }
        
        // Initialize DataLogger first
        dataLogger.Setup();
        dataLogger.Initialize();
        ConditionOrder = DataLogger.Instance.ConditionOrder;
        Debug.Log($"Experiment initialized successfully - Condition order: {string.Join(", ", ConditionOrder)}");

            }

    
    // Add a static accessor for the instance
    public static ExperimentController Instance 
    { 
        get { return _instance; }
    }
    void Start()
    {
        // Unsubscribe from previous events to avoid duplicates
        IconButton.OnTrialEndingSignal -= HandleTrialEndingSignal;
        IconButton.OnIconGazeTimeout -= HandleIconGazeTimeout;
        CrossIcon.OnCrossEndingSignal -= HandleCrossEndingSignal;        
        CrossIcon.OnFixationTimeout -= HandleFixationTimeout;
        // Subscribe to events for trial flow and timeouts  
        IconButton.OnTrialEndingSignal += HandleTrialEndingSignal;
        IconButton.OnIconGazeTimeout += HandleIconGazeTimeout;
        CrossIcon.OnFixationTimeout += HandleFixationTimeout;
        CrossIcon.OnCrossEndingSignal += HandleCrossEndingSignal;        

        breakCanvas.gameObject.SetActive(true);
        breakText.text = "<b>Welcome</b>\n\n" + "In this study, you will be asked to look at targets.";            Debug.Log("Ready for experiment to start");
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && waitFor_W_ExperimentStart)
        {
            waitFor_W_ExperimentStart = false;         Debug.Log("W pressed - moving to experiment rest phase first time");
            breakCanvas.gameObject.SetActive(false);        
            SetCurrentBlockIndex(0); MoveToRestPhase();
        }
        else if (Input.GetKeyDown(KeyCode.B) && waitFor_B_ForBlockFlow)
        {
            waitFor_B_ForBlockFlow = false;         Debug.Log("B pressed - starting next block");
            breakCanvas.gameObject.SetActive(false);        MoveToBlock();
        }
        else if (Input.GetKeyDown(KeyCode.R) && waitFor_R_RestPhase_AfterBlockEnd)
        {
            breakCanvas.gameObject.SetActive(false);        waitFor_R_RestPhase_AfterBlockEnd = false;         // Debug.Log("R pressed - returning to rest phase after block completion");
            var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
            int nextBlock = blockIndex + 1;
            SetCurrentBlockIndex(nextBlock);     Debug.Log($"Block index set to {nextBlock}");

            if (nextBlock == TotalBlockEndCount)             // This was the last block, experiment is complete
                {                
                ShowBreak("<b>Experiment Complete!</b>\n\nThank you for participating!");             Debug.Log("All blocks completed - experiment ended");
                }

            MoveToRestPhase();
        }
        else if (Input.GetKeyDown(KeyCode.T) && waitFor_T_TrainingScreen)
        {
            breakCanvas.gameObject.SetActive(false);                    waitFor_T_TrainingScreen = false;         // Debug.Log("T pressed - starting training screen");
            ShowBreak("<b>Training</b>\n\nThis is a training session to help you get familiar with the tasks."); Debug.Log("Press T to start training flow");
            waitFor_T_TrainingStart = true;
        }
        else if (Input.GetKeyDown(KeyCode.T) && waitFor_T_TrainingStart)
        {
            breakCanvas.gameObject.SetActive(false); waitFor_T_TrainingStart = false;        // Debug.Log("T pressed - starting training flow");
            MoveToTrainingFlow();
        }
        else if (Input.GetKeyDown(KeyCode.E) && waitFor_E_MoveToSceneFlow)
        {
            breakCanvas.gameObject.SetActive(false); waitFor_E_MoveToSceneFlow = false; Debug.Log("E pressed - starting main scene flow");
            var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
            currentSceneName = blockScenes[sceneIndex];
            MoveToSceneFlow(currentSceneName, sceneIndex);
        }
    }


    private void ConfigureBlockCondition()
    {
        blockIndex = PlayerPrefs.GetInt("BlockIndex", 0);

        // Get the condition value
        int conditionValue = ConditionOrder[blockIndex];
        
        // Correctly map condition values to select/feedback settings
        // bool select, withFeedback;
        switch (conditionValue)
        {
            case 0: // Select_With_Feedback
                select = true;
                withFeedback = true;
                break;
            case 1: // Select_No_Feedback
                select = true;
                withFeedback = false;
                break;
            case 2: // Observe_With_Feedback
                select = false;
                withFeedback = true;
                break;
            case 3: // Observe_No_Feedback
                select = false;
                withFeedback = false;
                break;
            default:
                Debug.LogError($"Invalid condition value: {conditionValue}");
                select = true;
                withFeedback = true;
                break;
        }
        
        string currentCondition = $"{(select ? "Select" : "Observe")}_{(withFeedback ? "With" : "No")}_Feedback";
        Debug.Log($"Configuring Block {blockIndex} - {currentCondition} (Value: {conditionValue})");
        
        // Save to PlayerPrefs
        PlayerPrefs.SetInt("CurrentSceneIndex", 0);
        PlayerPrefs.SetInt("CurrentTrialIndex", 0);
        PlayerPrefs.SetInt("With_Feedback", withFeedback ? 1 : 0);
        PlayerPrefs.SetInt("Select", select ? 1 : 0);
        PlayerPrefs.SetString("Condition", currentCondition);
        PlayerPrefs.Save();
    }

    private void MoveToBlock()
    {        
        ConfigureBlockCondition();
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        blockScenes = SceneSequenceForCurrentBlock[blockIndex];
        
        Debug.Log($"Using scene sequence for Block {blockIndex}: {string.Join(", ", blockScenes)}");
            
        // Reset scene and trial index for new block
        SetCurrentSceneIndex(0);  SetTrialIndex(0);
        Debug.Log("Reset trial and scene index to 0 for new block");
        
        // Log block transition
        DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, "ExperimentFlow", "block_start");

        // Show task message with instructions
        ShowTaskMessage();
        waitFor_T_TrainingScreen = true;
        Debug.Log("Press T to See training Screen");
    }

    private void ShowTaskMessage()
    {
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        
        string taskTypeText;
        if (select)
        {
            taskTypeText = "• GAZE-CLICK buttons to interact";
        }
        else
        {
            taskTypeText = "• Only OBSERVE buttons";
        }
        
        string feedbackText;
        if (withFeedback)
        {
            feedbackText = "• With Icon Pop-up Feedback";
        }
        else
        {
            feedbackText = "• No Icon PopUp Feedback";
        }
        
        ShowBreak($"<b>Task</b>\n\n{taskTypeText}\n\n{feedbackText}");
    }

    private void CompleteSequence()
    {
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();

        Debug.Log($"[CompleteSequence] Sequence complete after {trialIndex} icons. Completing trial.");

        // Reset trial index when completing a scene
        SetTrialIndex(0);

        if (!isInTrainingMode)
        {
            DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, currentSceneName, "end");
            int nextScene = sceneIndex + 1;
            SetCurrentSceneIndex(nextScene); Debug.Log($"Updated scene index to {sceneIndex + 1} / {TotalSceneEndCount}");
            // Check if we've completed all scenes in current block
            if (nextScene == TotalSceneEndCount) // Block Completition
            {
                // Show block completion message
                DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, "", "block_completed");
                // Show rest message
                ShowBreak("<b>Break</b>\n\nYou can take a break now.\n\nTell researcher when you are ready to continue.");
                waitFor_R_RestPhase_AfterBlockEnd = true; Debug.Log("Press R to enter rest phase after block completion");
            }
            else if (nextScene < TotalSceneEndCount)
            {
                ShowBreak($"<b>New Environment</b>\n\nYou will continue with the same task but in different environment."); Debug.Log("Press S to start next scene");
                waitFor_E_MoveToSceneFlow = true;
            }
        }
        // Handle differently depending on if we're in training mode
        else if (isInTrainingMode)
        {
            CompleteTraining(); Debug.Log("[CompleteSequence] Training completed. Transitioning to next phase.");
        }

    }

    #region  Phases
    private void MoveToRestPhase()
    {
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        // Log rest phase
        DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, "ExperimentBreak", "start");
        List<string> shuffledScenes = new List<string>(ExperimentSceneNames).OrderBy(_ => UnityEngine.Random.value).ToList();
        Debug.Log($"Block {blockIndex} scene order: {string.Join(", ", shuffledScenes)}");
        SceneSequenceForCurrentBlock[blockIndex] = shuffledScenes;  
        // Generate training sequence
        TrainingIconSequence = new List<string>(TrainingIconNames).OrderBy(_ => UnityEngine.Random.value).ToList();        Debug.Log($"Training sequence: {string.Join(", ", TrainingIconSequence)}");
        
        // Generate icon sequences for each scene type
        AppIconSequence = CrateSelfHaterIconSequence(AppIconNames, ExperimentSceneTrialEndCount);
        VideoIconSequence = CrateSelfHaterIconSequence(VideoIconNames, ExperimentSceneTrialEndCount);
        DocumentIconSequence = CrateSelfHaterIconSequence(DocumentIconNames, ExperimentSceneTrialEndCount);
        
        Debug.Log($"Generated App sequence with {AppIconSequence.Count} icons: {string.Join(", ", AppIconSequence)}");
        Debug.Log($"Generated Video sequence with {VideoIconSequence.Count} icons: {string.Join(", ", VideoIconSequence)}");
        Debug.Log($"Generated Document sequence with {DocumentIconSequence.Count} icons: {string.Join(", ", DocumentIconSequence)}");
        
        // Only log in editor/development
        Debug.Log("Ready for next block");
        
        // Set state for waiting for B to start next block
        waitFor_B_ForBlockFlow = true;
    }
    #endregion  Phases
    private void MoveToSceneFlow(string sceneName, int sceneIndex)
    {
        isInTrainingMode = false;
        this.currentSceneName = sceneName; // Update currentSceneName
        PlayerPrefs.SetString("CurrentSceneName", sceneName); // Save to PlayerPrefs

        var (blockIndex, _, trialIndex, select, withFeedback) = GetExperimentSettings();

        if (sceneIndex == TotalSceneEndCount) // TotalSceneEndCount = 3.
        {
            Debug.LogError($"Invalid scene index: {sceneIndex}");
            return;
        }

        Debug.Log($"Starting scene {sceneIndex}/{TotalSceneEndCount}: {sceneName}");
        DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, currentSceneName, "start");
        // Load the scene and start trial flow
        LoadSceneAndStartFirstLinearTrialFlow(sceneName);
    }

    private List<string> CrateSelfHaterIconSequence(List<string> TotalSceneIcons, int sequenceLength)
    {
        if (TotalSceneIcons == null || TotalSceneIcons.Count == 0) return new List<string>();

        List<string> result = new List<string>();
        Queue<string> recentlyUsed = new Queue<string>();
        int maxRecentMemory = Mathf.Min(TotalSceneIcons.Count - 1, 3);
        string lastIcon = null;

        for (int i = 0; i < sequenceLength; i++)
        {
            var validIcons = TotalSceneIcons.Where(icon => icon != lastIcon && !recentlyUsed.Contains(icon)).ToList();
            if (validIcons.Count == 0) validIcons = TotalSceneIcons.Where(icon => icon != lastIcon).ToList();
            string selectedIcon = validIcons[UnityEngine.Random.Range(0, validIcons.Count)];
            result.Add(selectedIcon);
            lastIcon = selectedIcon;
            recentlyUsed.Enqueue(selectedIcon);
            if (recentlyUsed.Count > maxRecentMemory) recentlyUsed.Dequeue();
        }        
        return result;
    }

    #endregion

#region Trial Flow - Within-Scene Trial Execution

// Method to handle scene loading and starting trials
private async void LoadSceneAndStartFirstLinearTrialFlow(string sceneName)
{
    this.currentSceneName = sceneName; // Update currentSceneName
    PlayerPrefs.SetString("CurrentSceneName", sceneName); // Save to PlayerPrefs
    Debug.Log($"Setting current scene name to: {sceneName}");

    // Set proper icon sequence based on scene
    FlowIconSequence = currentSceneName switch {
        "Training" => TrainingIconSequence,
        "Document" => DocumentIconSequence,
        "Video" => VideoIconSequence,
        "App" => AppIconSequence,
        _ => throw new System.ArgumentException($"Invalid scene: {currentSceneName}")
    };

    // Get current target icon before loading scene
    currentTargetIcon = GetCurrentTargetIcon();
    Debug.Log($"Target icon: {currentTargetIcon}");

    // Load scene
    SceneManager.LoadScene(currentSceneName);

    await Task.Delay(100); // 100ms delay to ensure scene is fully loaded
    // Trigger the trial flow
    TrialFlow1(currentSceneName, currentTargetIcon);
}

public void TrialFlow1(string sceneName, string targetIcon)
{
    this.currentSceneName = sceneName; // Update currentSceneName
    PlayerPrefs.SetString("CurrentSceneName", sceneName); // Save to PlayerPrefs

    if (!string.IsNullOrEmpty(targetIcon))
        this.currentTargetIcon = targetIcon;
    // PlayerPrefs.SetString("currentTargetIcon", targetIcon); // Save to PlayerPrefs
    Debug.Log($"[TrialFlow1] for icon: {currentTargetIcon} in scene: {currentSceneName}");
    TrialFlow2(this.currentSceneName, this.currentTargetIcon);
}

private async void TrialFlow2(string sceneName, string targetIcon)
{
    this.currentSceneName = sceneName; // Update currentSceneName
    PlayerPrefs.SetString("CurrentSceneName", sceneName); // Save to PlayerPrefs

    if (!string.IsNullOrEmpty(targetIcon))
        this.currentTargetIcon = targetIcon;

    var (blockIndex, _, trialIndex, select, withFeedback) = GetExperimentSettings();
    SetTrialIndex(trialIndex);
    Debug.Log($"trialIndex: {trialIndex} FlowIconSequence: " + string.Join(", ", FlowIconSequence) + $" FlowIconSequence.Count: {FlowIconSequence.Count}.");

    // Initial cleanup & reset
    HideAllExperimentCanvases();
    ResetAllInteractionStates();
    FindSceneCanvases(string.IsNullOrEmpty(sceneName) ? this.currentSceneName : sceneName);
    PrepareForTargetIcon(this.currentTargetIcon, withFeedback, select);

    // 1. INSTRUCTION PHASE
    DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, currentSceneName, trialIndex, "instruction", "start");
    Debug.Log("Instruction Start!");

    instructionCanvas.gameObject.SetActive(true);
    instructionText.text = "The next target is:";
    instructionIconImage.sprite = GetInstructionIconSprite(currentTargetIcon);
    // Hide main scene canvases during fixation
    Hide_MainSceneCanvas_OfType<VideoController>();
    Hide_MainSceneCanvas_OfType<DocumentController>();
    Hide_MainSceneCanvas_OfType<AppController>();
    Hide_MainSceneCanvas_OfType<TrainingController>();
    Debug.Log($"Showing instruction icon for '{currentTargetIcon}'");
    await Task.Delay((int)(instruction_Duration * 1000)); // Replace yield with blocking delay
    instructionCanvas.gameObject.SetActive(false);
    DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, currentSceneName, trialIndex, "instruction", "end");
    Debug.Log("Instruction End!");

    // 2. FIXATION PHASE
    if (fixationCanvas) fixationCanvas.gameObject.SetActive(true);
    Debug.Log("Fixation Start!");


    DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, currentSceneName, trialIndex, "cross_presented", "start");
    var crossIcon = fixationCross?.GetComponent<CrossIcon>();
    if (crossIcon)
    {
        crossIcon.ResetFixation();
    }
}


    private async void HandleCrossEndingSignal()
    {
        Debug.Log("<color=green>Fixation gaze completed. Moving to the next step in the flow.</color>");
        await TrialFlow3(); // Move to the next step in the trial flow
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task TrialFlow3()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {

        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        SetTrialIndex(trialIndex);
        // 4. Show scene content
        Debug.Log($"Showing scene content for: {currentSceneName}");
        FindSceneCanvases(currentSceneName);

        DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, currentSceneName, trialIndex, "icon_presented", "start");
        Debug.Log("Logged icon_presented start when search begins");

        foreach (var canvas in sceneCanvases.Where(c => c != null))
        {
            canvas.SetActive(true);
            Debug.Log($"Showing scene canvas: {canvas.name}");
        }
    }
    private async void HandleTrialEndingSignal()
    {
    // Hide main scene canvases during fixation
    Hide_MainSceneCanvas_OfType<VideoController>();
    Hide_MainSceneCanvas_OfType<DocumentController>();
    Hide_MainSceneCanvas_OfType<AppController>();
    Hide_MainSceneCanvas_OfType<TrainingController>();

        await TrialFlow4();
    }

    private async Task TrialFlow4()
    {
        // Get state values to decide whether go to next trial or end the sequence
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        int expectedTrialEnd = isInTrainingMode ? TrainingTrialsEndCount : ExperimentSceneTrialEndCount;
        // ISI PHASE
        {
            HideAllExperimentCanvases();
            DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, currentSceneName, trialIndex, "isi", "start"); Debug.Log("ISI Start!");
            await Task.Delay((int)(isi_Duration * 1000)); // Non-blocking delay
            DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, currentSceneName, trialIndex, "isi", "end"); Debug.Log("ISI End!");
        }

        // Increment trial index
        int nextTrialIndex = trialIndex + 1;
        SetTrialIndex(nextTrialIndex);        Debug.Log($"[ISIPhase] Trial index incremented to {nextTrialIndex}");

        if (nextTrialIndex == expectedTrialEnd)    // Simplified logic: if this is beyond last trial, complete the sequence    // Otherwise move to the next trial
        {
            CompleteSequence();         Debug.Log($"[] It was last trial ({trialIndex}/{expectedTrialEnd}). Completing sequence.");
        }
        else
        {
            Debug.Log($"trialIndex: {nextTrialIndex} FlowIconSequence: " + string.Join(", ", FlowIconSequence) + $" FlowIconSequence.Count: {FlowIconSequence.Count}.");
            currentTargetIcon = GetCurrentTargetIcon(); // Update current target icon
            Debug.Log($"Next Icon is: {currentTargetIcon}");
            TrialFlow1(currentSceneName, currentTargetIcon);
        }
    }

    // Unified timeout handling for both Cross and Icon timeouts
    private void HandleTimeout(string timeoutType, string sceneName)
    {
        HideAllExperimentCanvases();
        // Get values once
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        
        // Log the timeout with appropriate message
        string timeoutMessage = "";
        if (timeoutType == "fixation")
        {        
            DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, sceneName, trialIndex, "cross_gazed", "timeout");
            timeoutMessage = "cross_timeout_restart";            Debug.Log("<color=red>Fixation cross timeout detected</color>");
        }
        else if (timeoutType == "icon")
        {
            DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, sceneName, trialIndex, "icon_gazed", "timeout");
            timeoutMessage = "icon_timeout_restart";            Debug.Log("<color=red>Icon search timeout detected</color>");
        }

        // Log the timeout state - use the stored currentSceneName
        DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, currentSceneName, timeoutMessage);
        currentTargetIcon = GetCurrentTargetIcon();
        // Restart flow with the current target and settings
        TrialFlow1(currentSceneName, currentTargetIcon);
    }

    // Event handlers for timeouts 
    private void HandleFixationTimeout()
    {
        HandleTimeout("fixation", this.currentSceneName); 
    }

    private void HandleIconGazeTimeout()
    {
        HandleTimeout("icon", this.currentSceneName); 
    }

    #endregion
    #region UI Management Methods

    private void HideAllExperimentCanvases()
    {
        if (breakCanvas) breakCanvas.gameObject.SetActive(false);
        if (fixationCanvas) fixationCanvas.gameObject.SetActive(false);
        if (instructionCanvas) instructionCanvas.gameObject.SetActive(false);
        
        Hide_MainSceneCanvas_OfType<VideoController>();
        Hide_MainSceneCanvas_OfType<DocumentController>();
        Hide_MainSceneCanvas_OfType<AppController>();
        Hide_MainSceneCanvas_OfType<TrainingController>();
    }

    private void Hide_MainSceneCanvas_OfType<T>() where T : MonoBehaviour
    {
        // Find controllers even if their GameObjects are inactive
        foreach (var ctrl in Resources.FindObjectsOfTypeAll<T>())
        {
            try {
                // Try to get the MainCanvas from a field or property
                GameObject canvas = null;
                
                // Try field first
                var field = typeof(T).GetField("MainCanvas", System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                    canvas = field.GetValue(ctrl) as GameObject;
                    
                // Then try property
                if (canvas == null) {
                    var prop = typeof(T).GetProperty("MainCanvas");
                    if (prop != null)
                        canvas = prop.GetValue(ctrl) as GameObject;
                }
                
                if (canvas != null) {
                    Debug.Log($"Hiding canvas from {typeof(T).Name}: {canvas.name}");
                    canvas.SetActive(false);
                }

                // Call HideMainCanvas method if it exists
                var hideMethod = typeof(T).GetMethod("HideMainCanvas");
                hideMethod?.Invoke(ctrl, null);
            }
            catch (System.Exception ex) {
                Debug.LogWarning($"Error hiding canvas for {typeof(T).Name}: {ex.Message}");
            }
        }
    }

    private void ShowSceneCanvas_OfType<T>(bool activate = true) where T : MonoBehaviour
    {
        bool foundCanvas = false;
        
        // Find controllers even if their GameObjects are inactive
        foreach (var ctrl in Resources.FindObjectsOfTypeAll<T>())
        {
            try {
                // Try to get the MainCanvas from a field or property
                GameObject canvas = null;
                
                // Try field first
                var field = typeof(T).GetField("MainCanvas", System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                    canvas = field.GetValue(ctrl) as GameObject;
                    
                // Then try property
                if (canvas == null) {
                    var prop = typeof(T).GetProperty("MainCanvas");
                    if (prop != null)
                        canvas = prop.GetValue(ctrl) as GameObject;
                }
                
                if (canvas != null) {
                    Debug.Log($"Showing canvas from {typeof(T).Name}: {canvas.name}");
                    canvas.SetActive(activate);
                    foundCanvas = true;
                    
                    // Also ensure parent controller is active
                    ctrl.gameObject.SetActive(true);
                }

                // Call ShowMainCanvas method if it exists
                var showMethod = typeof(T).GetMethod("ShowMainCanvas");
                showMethod?.Invoke(ctrl, null);
            }
            catch (System.Exception ex) {
                Debug.LogWarning($"Error showing canvas for {typeof(T).Name}: {ex.Message}");
            }
        }
        
        if (!foundCanvas)
            Debug.LogWarning($"Could not find any canvas for {typeof(T).Name}");
    }

    private void FindSceneCanvases(string sceneName)
    {
        // If scene name is empty, use the stored current scene name
        if (string.IsNullOrEmpty(sceneName))
            sceneName = this.currentSceneName;
            
        Debug.Log($"Finding scene canvases for scene: {sceneName}");
        List<GameObject> foundCanvases = new List<GameObject>();

        var SceneControllerTypes = new Dictionary<string, System.Type>
        {
            { "Video", typeof(VideoController) },
            { "Document", typeof(DocumentController) },
            { "App", typeof(AppController) },
            { "Training", typeof(TrainingController) }
        };

        if (SceneControllerTypes.TryGetValue(sceneName, out var type))
        {
            // Use Resources.FindObjectsOfTypeAll to find ALL objects including inactive ones
            var controllers = Resources.FindObjectsOfTypeAll(type);
            
            foreach (var obj in controllers)
            {
                var controller = obj as MonoBehaviour;
                if (controller == null) continue;
                
                // Try field first
                var field = type.GetField("MainCanvas", System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                GameObject canvas = null;
                
                if (field != null)
                    canvas = field.GetValue(controller) as GameObject;
                    
                // Then try property
                if (canvas == null) {
                    var prop = type.GetProperty("MainCanvas");
                    if (prop != null)
                        canvas = prop.GetValue(controller) as GameObject;
                }

                if (canvas != null) {
                    foundCanvases.Add(canvas);
                    Debug.Log($"Found MainCanvas in {type.Name}: {canvas.name}");
                }
            }
        }

        sceneCanvases = foundCanvases.ToArray();

        if (sceneCanvases.Length == 0)
            Debug.LogWarning($"Could not find scene canvas for {sceneName}. UI may not display correctly.");
    }

    private void ShowBreak(string message)
    {
        breakCanvas.gameObject.SetActive(true);
        breakText.text = message;     Debug.Log(message);
    }

    #endregion
    #region Icon Management Methods

    private Sprite GetInstructionIconSprite(string sceneName)
    {
        // Then try loading directly from Assets path
        Sprite loadedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Icons-Backgrounds/Instruction-Icons/{currentTargetIcon}.png");
        if (loadedSprite != null) return loadedSprite;
        Debug.LogWarning($"[GetInstructionIconSprite] Sprite not found for '{sceneName}'.");
        return null;
    }
    private void PrepareForTargetIcon(string currentTargetIcon, bool withFeedback, bool select)
    {
        Debug.Log($"[PrepareForTargetIcon] Icon: {currentTargetIcon}, Feedback: {withFeedback}, Select: {select}");
        if (currentSceneName == "Video")
        {
            var videoController = FindAnyObjectByType<VideoController>();
            if (!select)
            {
                Debug.Log("Observe mode - skipping video icon prep.");
                return;
            }
            videoController.VideoRenderTexture.enabled = true;
            if (currentTargetIcon == "Play")
                videoController.DefaultStoppedMiddleState();
            else if (new[] { "Pause", "Stop", "Rewind", "Forward" }.Contains(currentTargetIcon))
                videoController.DefaultPlayingMiddleState();
        }
        else if (currentSceneName == "Document")
        {
            var documentController = FindAnyObjectByType<DocumentController>();
            if (!select)
            {
                Debug.Log("Observe mode - skipping video icon prep.");
                documentController.TextCanvas.SetActive(false);
                return;
            }
            documentController.TextCanvas.SetActive(true);;
        }
    }
    #endregion
    #region Training Flow Methods
    private void MoveToTrainingFlow()
    {
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();
        SetTrialIndex(0);     // Debug.Log("Training started. Trial index reset.");
        isInTrainingMode = true;
        DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, "Training", "start");
        // Use the dedicated TrainingIconSequence
        Debug.Log($"Training trials: {TrainingIconSequence.Count}");
        
        LoadSceneAndStartFirstLinearTrialFlow("Training");
    }

    private void CompleteTraining()
    {
        var (blockIndex, sceneIndex, trialIndex, select, withFeedback) = GetExperimentSettings();;
        DataLogger.Instance.LogState($"Block{blockIndex}", select, withFeedback, "Training", "end");
        isInTrainingMode = false;
        ShowBreak("<b>Training Completed!</b>\n\nNow you will have to perform the same task in different environments.");
        waitFor_E_MoveToSceneFlow = true;
    }

    #endregion

    #region Error Handling Methods

    private void ResetAllInteractionStates()
    {
        foreach (var icon in FindObjectsByType<IconButton>(FindObjectsSortMode.None))
        {
            icon?.ResetInteractionState();
        }

        Debug.Log("Reset all interaction states.");
    }

    #endregion
    #region Get and set Methods
    public string GetCurrentTargetIcon()
    {
        // Fix: Use the same PlayerPrefs key as in SetTrialIndex 
        int trialIndex = PlayerPrefs.GetInt("TrialIndex", 0);
        
        // Add bounds checking to prevent issues
        if (trialIndex >= FlowIconSequence.Count)
        {
            Debug.LogError($"Trial index {trialIndex} exceeds sequence length {FlowIconSequence.Count}");
        }
        
        Debug.Log($"GetCurrentTargetIcon: Using trial index {trialIndex} for {FlowIconSequence.Count} icons");
        return FlowIconSequence[trialIndex];
    }

    public (int blockIndex, int sceneIndex, int trialIndex, bool select, bool withFeedback) GetExperimentSettings()
    {
        int blockIndex = PlayerPrefs.GetInt("BlockIndex", 0);
        int sceneIndex = PlayerPrefs.GetInt("CurrentSceneIndex", 0);
        int trialIndex = PlayerPrefs.GetInt("TrialIndex", 0);
        bool select = PlayerPrefs.GetInt("Select", 0) == 1;
        bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
        
        return (blockIndex, sceneIndex, trialIndex, select, withFeedback);
    }

    public void SetCurrentBlockIndex(int index)
    {
        PlayerPrefs.SetInt("BlockIndex", index);
        blockIndex = index;
        PlayerPrefs.Save(); Debug.Log($"BlockIndex set to {index}");    
    }
    public void SetTrialIndex(int index)
    {
        // Fix: Also save to both keys for backward compatibility
        PlayerPrefs.SetInt("TrialIndex", index); // Set both for consistency
        trialIndex = index;
        PlayerPrefs.Save();
        Debug.Log($"TrialIndex set to {index}");
    }
    public void SetCurrentSceneIndex(int index)
    {
        PlayerPrefs.SetInt("CurrentSceneIndex", index);
        sceneIndex = index;
        PlayerPrefs.Save(); Debug.Log($"CurrentSceneIndex set to {index}");    
    }
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        CrossIcon.OnCrossEndingSignal -= HandleCrossEndingSignal;
        CrossIcon.OnFixationTimeout -= HandleFixationTimeout;
    
    }
}