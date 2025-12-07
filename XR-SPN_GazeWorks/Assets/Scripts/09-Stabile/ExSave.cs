// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI;
// using TMPro;
// using System.IO;

// public class ExperimentController : MonoBehaviour
// {
//     private static ExperimentController _instance;
//     private GameObject[] sceneCanvases;
//     private DataLogger dataLogger; // Reference to existing DataLogger

//     #region Configuration Settings

//     [Header("Experiment Config")]
//     [SerializeField] private int ExperimentTrialsPerScene = 50;
//     [SerializeField] private int TrainingTrialsCount = 3;

//     [Header("UI Elements")]
//     [SerializeField] private Canvas breakCanvas;
//     [SerializeField] private TextMeshProUGUI breakText;
//     [SerializeField] private Canvas instructionCanvas;
//     [SerializeField] private TextMeshProUGUI instructionText;
//     [SerializeField] private Image instructionIconImage;
//     [SerializeField] private Canvas fixationCanvas;
//     [SerializeField] private Image fixationCross;

//     [Header("Instruction Icons")]
//     [SerializeField] private InstructionIconMapping[] instructionIcons;

//     [Header("Trial Timing Parameters")]
//     [SerializeField] public float instruction_Duration = 4.0f;
//     [SerializeField] public int[] fixation_Durations = new int[] { 1250, 1500, 1750 };
//     [SerializeField] public float gaze_Duration = 0.75f;
//     [SerializeField] public float icon_Pop_Up_Duration = 0.3f;
//     [SerializeField] public float UI_Action_Duration = 2.0f;
//     [SerializeField] public float isi_Duration = 1.0f;

//     #endregion

//     #region State Tracking and Properties

//     private bool waitingForExperimentStart = true; // New state to track resting phase
//     private bool waitingForBlockStart = false;
//     private bool waitingForTToMoveToTraining = false;
//     private bool waitingAfterTrainingScreen = false;
//     private bool waitingForSToMoveToNextExperimentScenes = false;
//     private bool waitingForRestPhaseAfterTaskCompletition = false; // New state to track rest phase after block

//     // Block Flow State
//     public List<int> ConditionOrder;
//     private int currentBlock = 0;
//     private int currentConditionIndex = 0;
//     private int currentSceneIndex = 0;

//     // Training Mode State
//     private bool isInTrainingMode = false;

//     // Trial Flow State
//     public string currentSceneName;
//     public bool trialActive = false;
//     private string currentTargetIcon;
//     private bool processingInteraction = false;
//     private bool isFixationTimedOut = false;
//     private List<string> iconSequence = new List<string>();

//     // Add new dictionary to store prepared icon sequences
//     private Dictionary<int, List<string>> savedBlockSceneSequences = new Dictionary<int, List<string>>();
//     private List<Condition> savedConditionOrder = new List<Condition>();
//     private Dictionary<string, List<string>> savedIconSequences = new Dictionary<string, List<string>>();

//     // Scene Management
//     private string[] scenesInBuild;
//     private Dictionary<string, List<string>> experimentIcons;
//     private Dictionary<string, List<string>> trainingIcons;
//     private List<string> availableScenes = new List<string>();
//     private List<string> trainingScenesList = new List<string>();

//     // Public property to expose current required icon - updated to use PlayerPrefs
//     public string CurrentRequiredIcon =>
//         (iconSequence != null && GetCurrentTrialIndex() < iconSequence.Count)
//         ? iconSequence[GetCurrentTrialIndex()]
//         : string.Empty;

//     #endregion

//     #region Enums for Condition and Scene

//     // Experimental conditions
//     private enum Condition { Select_With_Feedback, Select_No_Feedback, Observe_With_Feedback, Observe_No_Feedback }

//     private List<Condition> conditions = new List<Condition>
//     {
//         Condition.Select_With_Feedback,
//         Condition.Select_No_Feedback,
//         Condition.Observe_With_Feedback,
//         Condition.Observe_No_Feedback
//     };

//     // Experimental Scenes
//     private enum Scene { Video, App, Document, Training }

//     #endregion

//     #region Initialization and Setup

//     void Awake()
//     {
//         // Improved singleton implementation for scene transitions
//         if (_instance != null)
//         {
//             Destroy(gameObject);
//             return;
//         }
        
//         // Make this the singleton instance
//         _instance = this;
//         DontDestroyOnLoad(gameObject);
        
//         // Get reference to existing DataLogger component on the same GameObject
//         dataLogger = GetComponent<DataLogger>();
//         if (dataLogger == null)
//         {
//             Debug.LogError("DataLogger component not found on ExperimentManager!");
//             return;
//         }

//         // Initialize systems
//         scenesInBuild = new string[] { "Video", "App", "Document", "Training" };
//         ValidateTextComponents();
//         HideAllCanvasesAndContent();
        
//         // Initialize scene and icon data
//         InitializeAllIconNames();
        
//         // Setup experimental flow
//         ValidateScenes();
//         if (breakCanvas != null) breakCanvas.gameObject.SetActive(false);
        
//         // Initialize DataLogger first
//         dataLogger.Setup();
//         dataLogger.Initialize();

//         // Generate all sequences just once and setup experiment
//         GenerateAndSaveAllExperimentSequences();
//         SetupEventSubscriptions();

//         // Load condition order from PlayerPrefs if it's not loaded or empty
//         if (ConditionOrder == null || ConditionOrder.Count == 0)
//         {
//             string conditionOrderString = PlayerPrefs.GetString("ConditionOrder", "");
//             if (!string.IsNullOrEmpty(conditionOrderString))
//             {
//                 ConditionOrder = conditionOrderString.Split(',')
//                     .Select(s => int.TryParse(s, out int result) ? result : -1)
//                     .Where(i => i >= 0)
//                     .ToList();
                
//                 Debug.Log($"Initialized condition order from PlayerPrefs: {string.Join(",", ConditionOrder)}");
//             }
//         }

//         // Show start message
//         breakCanvas.gameObject.SetActive(true); breakText.text = "<b>Welcome</b>\n\n" +
//             "In this study, you will be asked to look at targets."; Debug.Log("Showing welcome message, waiting for R key to enter rest phase");
//     }
    
//     // Add a static accessor for the instance
//     public static ExperimentController Instance 
//     { 
//         get { return _instance; }
//     }

//     void Start()
//     {
//     }

//     private void ValidateTextComponents()
//     {
//         if (breakCanvas == null || instructionCanvas == null || fixationCanvas == null)
//         {
//             Debug.LogWarning("One or more required canvases not assigned in inspector!");
//         }
//     }


//     private void InitializeAllIconNames()
//     {
//         // Initialize scene icon mappings
//         experimentIcons = new Dictionary<string, List<string>>
//         {
//             { "App", new List<string>{ "Safari", "Music", "Files", "Tv", "Settings" } },
//             { "Document", new List<string>{ "Save", "Undo", "Redo", "Export", "Close" } },
//             { "Video", new List<string>{ "Rewind", "Forward", "Play", "Pause", "Stop" } },
//         };

//         // Initialize training icons - 
//         trainingIcons = new Dictionary<string, List<string>>
//         {
//             { "Training", new List<string>{ "Square", "Triangle", "Circle" } }
//         };
//     }

//     private void GenerateAndSaveAllExperimentSequences()
//     {
//         // Initialize condition ordering from the data logger
//         if (ConditionOrder == null || ConditionOrder.Count == 0)
//         {
//             string conditionOrderString = PlayerPrefs.GetString("ConditionOrder", "");
//             if (!string.IsNullOrEmpty(conditionOrderString))
//             {
//                 ConditionOrder = conditionOrderString.Split(',')
//                     .Select(s => int.TryParse(s, out int result) ? result : -1)
//                     .Where(i => i >= 0)
//                     .ToList();
                
//                 Debug.Log($"Loaded condition order from PlayerPrefs: {string.Join(",", ConditionOrder)}");
//             }
//             else
//             {
//                 // Create default order if none exists
//                 ConditionOrder = Enumerable.Range(0, conditions.Count).ToList();
//                 Debug.Log($"Created default condition order: {string.Join(",", ConditionOrder)}");
//             }
//         }
        
//         // Save the block and scene sequences
//         savedConditionOrder = new List<Condition>();
//         foreach (int conditionIndex in ConditionOrder)
//         {
//             if (conditionIndex >= 0 && conditionIndex < conditions.Count)
//             {
//                 savedConditionOrder.Add(conditions[conditionIndex]);
//             }
//         }
        
//         Debug.Log($"Saved condition order: {string.Join(", ", savedConditionOrder)}");

//         // Create randomized scene order for each block
//         List<string> sceneNames = new List<string>();
//         foreach (string sceneName in experimentIcons.Keys)
//         {
//             if (sceneName != "Training")
//             {
//                 sceneNames.Add(sceneName);
//             }
//         }

//         // Generate scenes for each block (1-based for display, 0-based for internal)
//         for (int block = 0; block < 4; block++)
//         {
//             List<string> shuffledScenes = new List<string>(sceneNames);
//             shuffledScenes = RandomizeList(shuffledScenes);
//             savedBlockSceneSequences[block] = shuffledScenes;
//             Debug.Log($"Saved Block {block + 1} scene sequence: {string.Join(", ", shuffledScenes)}");
//         }

//         // Generate icon sequences for each scene - using full ExperimentTrialsPerScene trials per scene
//         foreach (string sceneName in sceneNames)
//         {
//             List<string> iconNames = experimentIcons[sceneName];
//             List<string> iconSequence = GenerateSequenceWithNoConsecutiveRepeats(iconNames, ExperimentTrialsPerScene);
            
//             savedIconSequences[sceneName] = iconSequence;
//             Debug.Log($"Generated & saved {iconSequence.Count} icons for scene: {sceneName}");
//         }

//         // Generate training sequence
//         List<string> trainingSequence = new List<string>();
//         if (trainingIcons.ContainsKey("Training"))
//         {
//             while (trainingSequence.Count < TrainingTrialsCount)
//             {
//                 int randomIndex = UnityEngine.Random.Range(0, trainingIcons["Training"].Count);
//                 trainingSequence.Add(trainingIcons["Training"][randomIndex]);
//             }
            
//             savedIconSequences["Training"] = trainingSequence;
//             Debug.Log($"Generated & saved training sequence with {trainingSequence.Count} trials: {string.Join(", ", trainingSequence)}");
//         }
//     }

//     private List<string> GenerateSequenceWithNoConsecutiveRepeats(List<string> availableIcons, int sequenceLength)
//     {
//         if (availableIcons == null || availableIcons.Count == 0)
//         {
//             Debug.LogError("No icons available to generate a sequence");
//             return new List<string>();
//         }

//         List<string> result = new List<string>();
        
//         // If we don't need to repeat icons, use simple randomization
//         if (availableIcons.Count >= sequenceLength)
//         {
//             // Create a copy to shuffle
//             List<string> iconPool = new List<string>(availableIcons);
            
//             for (int i = 0; i < sequenceLength; i++)
//             {
//                 int randomIndex = UnityEngine.Random.Range(0, iconPool.Count);
//                 result.Add(iconPool[randomIndex]);
//                 iconPool.RemoveAt(randomIndex);
//             }
//         }
//         else
//         {
//             // We need to repeat icons - ensure no consecutive repeats
//             string lastIcon = null;
            
//             for (int i = 0; i < sequenceLength; i++)
//             {
//                 // Create a pool excluding the last used icon to avoid repeats
//                 List<string> validIcons = new List<string>();
                
//                 foreach (string icon in availableIcons)
//                 {
//                     if (icon != lastIcon)
//                     {
//                         validIcons.Add(icon);
//                     }
//                 }
                
//                 // Select random icon from valid pool
//                 int randomIndex = UnityEngine.Random.Range(0, validIcons.Count);
//                 string selectedIcon = validIcons[randomIndex];
                
//                 result.Add(selectedIcon);
//                 lastIcon = selectedIcon;
//             }
//         }
        
//         // Verify and log sequence
//         for (int i = 1; i < result.Count; i++)
//         {
//             if (result[i] == result[i-1])
//             {
//                 Debug.LogError($"Generated sequence contains consecutive repeats: {result[i-1]} at positions {i-1} and {i}");
//             }
//         }
        
//         return result;
//     }

//     private void SetupEventSubscriptions()        // Clean up any existing subscriptions first
//     {
//         IconButton.OnIconInteracted += HandleIconInteraction;
//         CrossIcon.OnFixationTimeout += HandleFixationTimeout;
//         IconButton.OnIconGazeTimeout += HandleIconGazeTimeout;
//     }

//     void Update()
//     {
//         // Initial start with R key
//         if (waitingForExperimentStart && Input.GetKeyDown(KeyCode.R))
//         {
//             Debug.Log("R pressed - moving to experiment rest phase");
//             waitingForExperimentStart = false;
//             breakCanvas.gameObject.SetActive(false);
//             MoveToExperimentRestPhase();
//         }
//         // Handle block transitions with B key
//         else if (waitingForBlockStart && Input.GetKeyDown(KeyCode.B))
//         {
//             Debug.Log("B pressed - starting next block");
//             waitingForBlockStart = false;
//             breakCanvas.gameObject.SetActive(false);
//             MoveToNextBlock();
//         }
//         // Handle spacebar after block completion to go to rest phase
//         else if (waitingForRestPhaseAfterTaskCompletition && Input.GetKeyDown(KeyCode.R))
//         {
//             Debug.Log("R pressed - returning to rest phase after block completion");
//             waitingForRestPhaseAfterTaskCompletition = false;
//             breakCanvas.gameObject.SetActive(false);
//             MoveToExperimentRestPhase();
//         }
//         // Handle training with T key and main scene with M key
//         else if (waitingForTToMoveToTraining && Input.GetKeyDown(KeyCode.T))
//         {
//             Debug.Log("T pressed - starting training flow");
//             waitingForTToMoveToTraining = false;
//             breakCanvas.gameObject.SetActive(false);
//             StartTrainingFlow();
//         }
//         else if (waitingForSToMoveToNextExperimentScenes && Input.GetKeyDown(KeyCode.S))
//         {
//             Debug.Log("S pressed - starting main scene flow");
//             waitingForSToMoveToNextExperimentScenes = false;
//             breakCanvas.gameObject.SetActive(false);
            
//             // Important: Do NOT reset currentSceneIndex here
//             isInTrainingMode = false;
            
//             // Get appropriate scene list based on mode
//             List<string> currentSceneList = isInTrainingMode ? trainingScenesList : availableScenes;

//             if (currentSceneIndex < 0 || currentSceneIndex >= currentSceneList.Count)
//             {
//                 Debug.LogError($"Invalid scene index: {currentSceneIndex}");
//                 return;
//             }

//             string sceneName = currentSceneList[currentSceneIndex];
//             Debug.Log($"Starting scene {currentSceneIndex + 1}/{currentSceneList.Count}: {sceneName}");

//             // Log scene start
//             bool intentionType = PlayerPrefs.GetInt("Select", 0) == 1;
//             bool feedbackType = PlayerPrefs.GetInt("With_Feedback", 0) == 1;

//             string intention = intentionType ? "Select" : "Observe";
//             string feedback = feedbackType ? "With_Feedback" : "No_Feedback";

//             DataLogger.Instance.LogState($"Block{currentBlock}", intention, feedback, sceneName, "start");

//             // Prepare the scene icon sequence
//             Debug.Log($"Preparing scene: {sceneName}");
//             currentSceneName = sceneName;

//             // Use pre-generated sequence for this scene
//             if (savedIconSequences.ContainsKey(sceneName))
//             {
//                 iconSequence = savedIconSequences[sceneName];
//                 Debug.Log($"Using pre-generated sequence for scene '{sceneName}' with {iconSequence.Count} icons");
//             }
//             else
//             {
//                 Debug.LogError($"No pre-generated sequence found for scene: {sceneName}!");
//             }

//             // Verify we have a valid icon sequence
//             if (iconSequence == null || iconSequence.Count == 0)
//             {
//                 Debug.LogError($"Failed to generate icon sequence for scene: {sceneName}");
//                 iconSequence = new List<string>(); // Empty list to prevent null references
//             }

//             // Reset icon index to start at the beginning
//             SetCurrentTrialIndex(0);

//             // Get the current condition settings
//             bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//             bool select = PlayerPrefs.GetInt("Select", 0) == 1;

//             // Fix: Use StartCoroutine to properly handle the IEnumerator method
//             if (iconSequence != null && iconSequence.Count > 0 && GetCurrentTrialIndex() < iconSequence.Count)
//             {
//                 StartCoroutine(LinearTrialFlow(CurrentRequiredIcon, withFeedback, select, sceneName));
//             }
//             else
//             {
//                 Debug.LogError($"Invalid icon sequence for scene {sceneName}");
//             }
//         }
//     }

//     private void ConfigureCurrentCondition()
//     {
//         // Load condition order from PlayerPrefs if it's not loaded or empty
//         if (ConditionOrder == null || ConditionOrder.Count == 0)
//         {
//             string conditionOrderString = PlayerPrefs.GetString("ConditionOrder", "");
//             if (!string.IsNullOrEmpty(conditionOrderString))
//             {
//                 ConditionOrder = conditionOrderString.Split(',')
//                     .Select(s => int.TryParse(s, out int result) ? result : -1)
//                     .Where(i => i >= 0)
//                     .ToList();
                
//                 Debug.Log($"Loaded condition order from PlayerPrefs: {string.Join(",", ConditionOrder)}");
//             }
//             else
//             {
//                 // Fallback - rebuild from saved conditions
//                 ConditionOrder = new List<int>();
//                 for (int i = 0; i < conditions.Count; i++)
//                 {
//                     ConditionOrder.Add(i);
//                 }
//                 Debug.Log($"Created default condition order: {string.Join(",", ConditionOrder)}");
//             }
//         }

//         // Validate condition index is in valid range
//         if (ConditionOrder == null || ConditionOrder.Count == 0)
//         {
//             Debug.LogError("No conditions available. Check condition initialization.");
//             return;
//         }

//         // Use modulo to ensure the index is valid
//         if (currentConditionIndex < 0 || currentConditionIndex >= ConditionOrder.Count)
//         {
//             int oldIndex = currentConditionIndex;
//             currentConditionIndex = ((currentConditionIndex % ConditionOrder.Count) + ConditionOrder.Count) % ConditionOrder.Count;
//             Debug.LogWarning($"Adjusted condition index from {oldIndex} to {currentConditionIndex} (valid range: 0-{ConditionOrder.Count - 1})");
//         }

//         // Get the current condition enum value
//         int conditionValue = ConditionOrder[currentConditionIndex];
//         if (conditionValue < 0 || conditionValue >= conditions.Count)
//         {
//             Debug.LogError($"Invalid condition value: {conditionValue}. Valid range: 0-{conditions.Count - 1}");
//             return;
//         }

//         Condition currentCondition = conditions[conditionValue];
//         Debug.Log($"Configuring Block {currentBlock + 1} - {currentCondition}");

//         // Configure feedback and selection mode based on condition
//         bool withFeedback = false;
//         bool select = false;

//         switch (currentCondition)
//         {
//             case Condition.Select_With_Feedback:
//                 withFeedback = true;
//                 select = true;
//                 break;
//             case Condition.Select_No_Feedback:
//                 withFeedback = false;
//                 select = true;
//                 break;
//             case Condition.Observe_With_Feedback:
//                 withFeedback = true;
//                 select = false;
//                 break;
//             case Condition.Observe_No_Feedback:
//                 withFeedback = false;
//                 select = false;
//                 break;
//         }

//         // Save to PlayerPrefs
//         PlayerPrefs.SetInt("With_Feedback", withFeedback ? 1 : 0);
//         PlayerPrefs.SetInt("Select", select ? 1 : 0);
//         PlayerPrefs.SetString("Condition", currentCondition.ToString());
//         PlayerPrefs.Save();

//         Debug.Log($"Saved to PlayerPrefs: Block {currentBlock + 1}, With_Feedback={withFeedback}, Select={select}, Condition={currentCondition}");
//     }

//     private void MoveToNextBlock()
//     {
//         // Increment block counter
//         currentBlock++;
        
//         // Log block transition
//         DataLogger.Instance.LogState($"Block{currentBlock}", "block_start", "", "", "start");
        
//         // Configure condition based on current block
//         currentConditionIndex = currentBlock % conditions.Count;
//         ConfigureCurrentCondition();
        
//         // Load scene sequence for this block
//         List<string> blockSceneSequence = new List<string>();
//         if (savedBlockSceneSequences.ContainsKey(currentBlock - 1)) // -1 since blocks are 1-indexed in UI
//         {
//             availableScenes = savedBlockSceneSequences[currentBlock - 1];
//             Debug.Log($"Loaded saved scene sequence for Block {currentBlock}: {string.Join(", ", availableScenes)}");
//         }
//         else
//         {
//             // Fallback - use default scenes
//             availableScenes = new List<string>();
//             foreach (string scene in experimentIcons.Keys)
//             {
//                 if (scene != "Training")
//                 {
//                     availableScenes.Add(scene);
//                 }
//             }
//             Debug.Log($"Created default scene sequence for Block {currentBlock}: {string.Join(", ", availableScenes)}");
//         }
        
//         // Reset scene index for new block
//         currentSceneIndex = 0;
//         PlayerPrefs.SetInt("CurrentSceneIndex", currentSceneIndex);
//         PlayerPrefs.Save();
        
//         // Reset trial index for new block
//         SetCurrentTrialIndex(0);
//         Debug.Log("Reset trial index to 0 for new block");
        
//         // Show task message with instructions
//         ShowTaskMessage();
//         waitingForTToMoveToTraining = true;
//     }

//     private void ShowTaskMessage()
//     {
//         bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//         bool select = PlayerPrefs.GetInt("Select", 0) == 1;
        
//         string taskTypeText;
//         if (select)
//         {
//             taskTypeText = "• GAZE-CLICK buttons to interact";
//         }
//         else
//         {
//             taskTypeText = "• Only OBSERVE buttons";
//         }
        
//         string feedbackText;
//         if (withFeedback)
//         {
//             feedbackText = "• With Icon Pop-up Feedback";
//         }
//         else
//         {
//             feedbackText = "• No Icon PopUp Feedback";
//         }
        
//         ShowBreak($"<b>Task</b>\n\n{taskTypeText}\n\n{feedbackText}\n\nPress T to start training.");
//     }

//     private void CompleteCurrentSequence()
//     {
//         Debug.Log($"Sequence complete after {GetCurrentTrialIndex()} icons - completing trial");
//         Debug.Log($"CompleteTrial called - isInTrainingMode: {isInTrainingMode}, currentTrialIndex: {GetCurrentTrialIndex()}, iconSequence count: {iconSequence?.Count ?? 0}");

//         trialActive = false;
//         HideAllCanvasesAndContent();

//         // Reset trial index when completing a scene
//         SetCurrentTrialIndex(0);

//         // Handle differently depending on if we're in training mode
//         if (isInTrainingMode)
//         {
//             Debug.Log("Training completed - showing completion message");
//             CompleteTraining();
//         }
//         else
//         {
//             bool intentionType = PlayerPrefs.GetInt("Select", 0) == 1;
//             bool feedbackType = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//             string intention = intentionType ? "Select" : "Observe";
//             string feedback = feedbackType ? "With_Feedback" : "No_Feedback";

//             // Log the scene end
//             DataLogger.Instance.LogState($"Block{currentBlock}", intention, feedback, currentSceneName, "end");

//             // Reset UI and task state
//             HideAllCanvasesAndContent();
//             trialActive = false;

//             // For main scenes, advance to next scene
//             currentSceneIndex++;
            
//             // Store the updated scene index to PlayerPrefs
//             PlayerPrefs.SetInt("CurrentSceneIndex", currentSceneIndex);
//             PlayerPrefs.Save();
            
//             Debug.Log($"Updated scene index to {currentSceneIndex} of {availableScenes.Count}");

//             // Check if we've completed all scenes in current block
//             if (currentSceneIndex >= availableScenes.Count)
//             {
//                 ShowTaskCompletionMessage();
//             }
//             else
//             {
//                 // Show "New Environment" message between scenes
//                 string nextScene = availableScenes[currentSceneIndex];
//                 ShowBreak($"<b>New Environment</b>\n\nPreparing next environment: {nextScene}\n\nPress S to continue with the same task.");
//                 waitingForSToMoveToNextExperimentScenes = true;
//             }
//         }
//     }

//     private void ShowTaskCompletionMessage()
//     {
//         // Show block completion message
//         DataLogger.Instance.LogState($"Block{currentBlock}", "block_completed", "", "", "end");

//         // Check if all blocks are complete
//         if (currentBlock >= 4)
//         {
//             // This was the last block, experiment is complete
//             ShowBreak("<b>Experiment Complete!</b>\n\nThank you for participating!");
//             Debug.Log("All blocks completed - experiment ended");
//         }
//         else
//         {
//             // More blocks to go, show rest phase message
//             ShowBreak($"<b>Block {currentBlock} Complete</b>\n\nPress R to continue to rest phase.");
//             waitingForRestPhaseAfterTaskCompletition = true;
//         }
//     }

//     private void MoveToExperimentRestPhase()
//     {
//         // Log rest phase
//         DataLogger.Instance.LogState($"Block{currentBlock}", "resting", "", "ExperimentFlow", "start");
        
//         // Show rest message
//         ShowBreak("<b>Break</b>\n\nYou can take a break now..n/n/Tell researcher when you ready to continue.");
        
//                 // Set state for waiting for B to start next block
//         waitingForBlockStart = true;
//         waitingForRestPhaseAfterTaskCompletition = false;
//     }

//     #endregion

//     #region Trial Flow - Unified Linear Flow

//     // Single entry point for all trial flows (first, subsequent, or timeout recovery)
//     public IEnumerator LinearTrialFlow(string iconName, bool withFeedback, bool select, string sceneName, bool requiresSceneLoad = true)
//     {
//         currentTargetIcon = iconName;
//         currentSceneName = sceneName;

//         Debug.Log($"LinearTrialFlow: iconName={iconName}, With_Feedback={withFeedback}, Select={select}, sceneName={sceneName}, requiresSceneLoad={requiresSceneLoad}");

//         // Determine if we need to load the scene first
//         if (requiresSceneLoad && SceneManager.GetActiveScene().name != sceneName)
//         {
//             // Load scene and continue trial flow after scene load
//             LoadScene(sceneName);
            
//             // Wait one frame for scene to load properly
//             yield return null;
            
//             // Hide content initially after scene load
//             HideAllCanvasesAndContent();
//         }
        
//         // Start the unified trial flow directly
//         yield return StartCoroutine(LinearTrialFlowCoroutine(withFeedback, select));
//     }

//     // Unified trial flow coroutine used for all scenarios
//     private IEnumerator LinearTrialFlowCoroutine(bool withFeedback, bool select)
//     {
//         // Reset timeout flag at the start of each flow
//         isFixationTimedOut = false;
        
//         // Hide all UI elements and reset interaction states
//         HideAllCanvasesAndContent();
//         ResetAllInteractionStates();
        
//         // 1. Instruction phase
//         yield return StartCoroutine(InstructionPhase());
        
//         // 2. Setup scene content
//         FindSceneCanvases();
//         PrepareForTargetIcon(currentTargetIcon, withFeedback, select);
        
//         // 3. Fixation phase - CrossIcon will handle completion
//         yield return StartCoroutine(FixationPhase());
        
//         // 4. Show scene content if fixation was successful
//         if (!isFixationTimedOut)
//         {
//             Debug.Log($"Showing scene content for: {currentSceneName}");

//             // Hide experiment UI first
//             HideAllCanvasesAndContent();
//             FindSceneCanvases();

//             // Log icon presented start when search actually begins
//             string block = $"Block{currentBlock}";
//             string intention = PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe";
//             string feedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback";
//             int TrialIndex = GetCurrentTrialIndex();
//             DataLogger.Instance.LogTask(block, intention, feedback, currentSceneName, TrialIndex, "icon_presented", "start");
//             Debug.Log($"Logged icon_presented start when search begins");

//             // Show main canvases
//             if (sceneCanvases != null)
//                 foreach (GameObject canvas in sceneCanvases.Where(c => c != null))
//                 {
//                     canvas.SetActive(true);
//                     Debug.Log($"Showing scene canvas: {canvas.name}");
//                 }

//             // Handle scene-specific content
//             switch (currentSceneName)
//             {
//                 case "Video":
//                     // ShowVideoContent();
//                     ShowContent();
//                     break;
//                 case "Document":
//                     ShowContent();
//                     break;
//                 case "App":
//                     ShowContent();
//                     break;
//                 case "Training":
//                     ShowContent();
//                     break;
//             }
            
//             trialActive = true;
//             BroadcastMessage("OnTrialSequenceStarted", SendMessageOptions.DontRequireReceiver);
//             Debug.Log($"Trial active for icon: {currentTargetIcon} in scene: {currentSceneName}");
//         }
//         else
//         {
//             Debug.LogWarning("Fixation timed out - trial not activated");
//         }
//     }

//     // Unified timeout handling for both CrossIcon and IconButton timeouts
//     private void HandleTimeout(string timeoutType)
//     {
//         // Log the timeout with appropriate message
//         string timeoutMessage = "";
//         if (timeoutType == "fixation")
//         {
//             isFixationTimedOut = true;
//             timeoutMessage = "fixation_timeout_recovery_start";            Debug.Log("<color=red>Fixation cross timeout detected</color>");
//         }
//         else if (timeoutType == "icon")
//         {
//             timeoutMessage = "icon_search_timeout_recovery_start";            Debug.Log("<color=red>Icon search timeout detected</color>");
//         }
        
//         // Log the timeout state
//         LogTimeoutState(timeoutMessage);
        
//         // Restart flow with the current target and settings
//         bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//         bool select = PlayerPrefs.GetInt("Select", 0) == 1;
        
//         // Fix: Use StartCoroutine to properly handle the IEnumerator method
//         StartCoroutine(LinearTrialFlow(currentTargetIcon, withFeedback, select, currentSceneName, false));
//     }
    
//     // Event handlers for timeouts 
//     private void HandleFixationTimeout()
//     {
//         HandleTimeout("fixation");
//     }
    
//     private void HandleIconGazeTimeout()
//     {
//         HandleTimeout("icon"); 
//     }

//     #endregion

//     #region Trial Flow - Within-Scene Trial Execution

//     private IEnumerator InstructionPhase()
//     {
//         // Hide other UI elements
//         if (fixationCanvas && fixationCanvas.gameObject.activeSelf)
//             fixationCanvas.gameObject.SetActive(false);
            
//         // Log instruction phase start
//         string block = $"Block{currentBlock}";
//         string intention = PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe";
//         string feedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback";
//         int trialIndex = GetCurrentTrialIndex();
//         DataLogger.Instance?.LogTask(block, intention, feedback, currentSceneName, trialIndex, "instruction", "start");

//         // Get current settings
//         bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//         bool select = PlayerPrefs.GetInt("Select", 0) == 1;

//         // Hide all UI and content
//         HideAllCanvasesAndContent();

//         // Show instruction with icon
//         instructionCanvas.gameObject.SetActive(true);
//         instructionText.text = "The next target is:";

//         // Display the icon image
//         Sprite iconSprite = GetInstructionIconSprite(currentTargetIcon);
//         if (iconSprite != null)
//         {
//             instructionIconImage.sprite = iconSprite;
//             instructionIconImage.enabled = true;
//             Debug.Log($"Showing instruction icon for '{currentTargetIcon}'");
//         }
//         else
//         {
//             instructionIconImage.enabled = false;
//             Debug.LogError($"No instruction icon found for '{currentTargetIcon}'");
//         }

//         // Wait for instruction duration
//         yield return new WaitForSeconds(instruction_Duration);

//         // Hide instruction
//         // instructionCanvas.gameObject.SetActive(false);
//         HideAllCanvasesAndContent();

//         // Log instruction phase end
//         DataLogger.Instance?.LogTask(block, intention, feedback, currentSceneName, trialIndex, "instruction", "end");
//     }

//     private IEnumerator FixationPhase()
//     {
//         // Show fixation canvas and set up cross
//         if (fixationCanvas) 
//             fixationCanvas.gameObject.SetActive(true);
            
//         // Get random duration
//         int fixationDurationMs = fixation_Durations[UnityEngine.Random.Range(0, fixation_Durations.Length)];
//         float durationSeconds = fixationDurationMs / 1000f;
        
//         // Set up the cross icon
//         CrossIcon crossIcon = fixationCross?.GetComponent<CrossIcon>();
//         if (crossIcon != null)
//         {
//             // Configure CrossIcon
//             if (string.IsNullOrEmpty(crossIcon.iconName))
//                 crossIcon.iconName = "FixationCross";
                
//             crossIcon.ResetFixation();
//             crossIcon.SetFixationDuration(durationSeconds);
            
//             // Log start of fixation
//             string block = $"Block{currentBlock}";
//             string intention = PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe";
//             string feedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback";
//             int trialIndex = GetCurrentTrialIndex();
//             Vector3 position = fixationCross.transform.position;
            
//             DataLogger.Instance?.LogTask(block, intention, feedback, currentSceneName, trialIndex, "cross_presented", "start");
//             DataLogger.Instance?.LogFixationCross(block, intention, feedback, currentSceneName, fixationDurationMs, position, "start");

//             // Wait for fixation to complete
//             while (!crossIcon.isFixationCompleted && !isFixationTimedOut)
//                 yield return null;
//         }
//         else
//         {
//             // Fallback if no CrossIcon component found
//             yield return new WaitForSeconds(durationSeconds);
//         }
//     }

//     private IEnumerator ISIPhase()
//     {
//         // Hide other UI elements
//         if (instructionCanvas.gameObject.activeSelf)
//             instructionCanvas.gameObject.SetActive(false);
            
//         if (fixationCanvas && fixationCanvas.gameObject.activeSelf)
//             fixationCanvas.gameObject.SetActive(false);

//         Debug.Log("Interstimulus Interval (ISI) Phase");

//         // Log ISI start
//         string block = $"Block{currentBlock}";
//         string intention = PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe";
//         string feedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback";
//         int trialIndex = GetCurrentTrialIndex();
        
//         DataLogger.Instance.LogTask(block, intention, feedback, currentSceneName, trialIndex, "isi", "start");

//         // Hide scene content
//         HideMainSceneCanvas();

//         // Wait for ISI duration
//         yield return new WaitForSeconds(isi_Duration);

//         // Log ISI end - DataLogger will automatically increment the trial index when logging "isi" "end"
//         DataLogger.Instance.LogTask(block, intention, feedback, currentSceneName, trialIndex, "isi", "end");
        
//         // Now read the updated trial index for the next trial
//         int updatedTrialIndex = GetCurrentTrialIndex();
//         Debug.Log($"Moving to trial index {updatedTrialIndex} after ISI end");
//     }

//     #endregion

//     #region UI Management Methods

//     // Hide all UI elements and scene content
//     private void HideAllCanvasesAndContent()
//     {
//         // Hide UI elements
//         if (instructionCanvas) instructionCanvas.gameObject.SetActive(false);
//         if (fixationCanvas) instructionCanvas.gameObject.SetActive(false);
//         if (breakCanvas) breakCanvas.gameObject.SetActive(false);

//         // Disable scene controllers
//         DisableAllSceneControllers();

//         // Hide cached scene content
//         if (sceneCanvases != null && sceneCanvases.Length > 0)
//         {
//             foreach (GameObject canvas in sceneCanvases)
//             {
//                 if (canvas != null)
//                     canvas.SetActive(false);
//             }
//         }
//     }

//     // Hide only the main scene canvas
//     public void HideMainSceneCanvas()
//     {
//         // Hide main canvases
//         if (sceneCanvases != null)
//             foreach (GameObject canvas in sceneCanvases.Where(c => c != null))
//                 canvas.SetActive(false);

//         // Handle video-specific content
//         if (currentSceneName == "Video")
//         {
//             VideoController videoController = FindAnyObjectByType<VideoController>();
//             if (videoController != null)
//             {
//                 videoController.HideMainCanvas();
//             }
//             else
//             {
//                 Debug.LogError("Flow is not correct");
//             }
//         }
//     }

//     // Disable scene controllers
//     private void DisableAllSceneControllers()
//     {
//         // Video Controller
//         VideoController[] videoControllers = FindObjectsByType<VideoController>(FindObjectsSortMode.None);
//         foreach (var controller in videoControllers)
//         {
//             if (controller.MainCanvas != null)
//             {
//                 Debug.Log($"Hiding canvas from VideoController: {controller.MainCanvas.name}");
//                 controller.MainCanvas.SetActive(false);
//             }
//         }

//         // Document Controller
//         DocumentController[] documentControllers = FindObjectsByType<DocumentController>(FindObjectsSortMode.None);
//         foreach (var controller in documentControllers)
//         {
//             if (controller.MainCanvas != null)
//             {
//                 Debug.Log($"Hiding canvas from DocumentController: {controller.MainCanvas.name}");
//                 controller.MainCanvas.SetActive(false);
//             }
//         }

//         // App Controller
//         AppController[] appControllers = FindObjectsByType<AppController>(FindObjectsSortMode.None);
//         foreach (var controller in appControllers)
//         {
//             if (controller.MainCanvas != null)
//             {
//                 Debug.Log($"Hiding canvas from AppController: {controller.MainCanvas.name}");
//                 controller.MainCanvas.SetActive(false);
//             }
//         }

//         // Training Controller
//         TrainingController[] trainingControllers = FindObjectsByType<TrainingController>(FindObjectsSortMode.None);
//         foreach (var controller in trainingControllers)
//         {
//             if (controller.MainCanvas != null)
//             {
//                 Debug.Log($"Hiding canvas from TrainingController: {controller.MainCanvas.name}");
//                 controller.MainCanvas.SetActive(false);
//             }
//         }
//     }

//     private void ShowContent()
//     {
//         FindObjectsByType<IconButton>(FindObjectsSortMode.None)
//             .Where(btn => !btn.transform.IsChildOf(instructionCanvas.transform) &&
//                           !btn.transform.IsChildOf(fixationCanvas.transform) &&
//                           !btn.transform.IsChildOf(breakCanvas.transform))
//             .ToList()
//             .ForEach(btn => btn.gameObject.SetActive(true));
//     }

//     // Find canvas references for current scene
//     private void FindSceneCanvases()
//     {
//         Debug.Log($"Finding scene canvases for scene: {currentSceneName}");

//         GameObject mainCanvas = null;

//         // Get canvas from scene controller
//         switch (currentSceneName)
//         {
//             case "Video":
//                 VideoController videoController = FindAnyObjectByType<VideoController>();
//                 if (videoController != null && videoController.MainCanvas != null)
//                 {
//                     mainCanvas = videoController.MainCanvas;
//                     Debug.Log($"Using MainCanvas from VideoController: {mainCanvas.name}");
//                 }
//                 break;

//             case "Document":
//                 DocumentController documentController = FindAnyObjectByType<DocumentController>();
//                 if (documentController != null && documentController.MainCanvas != null)
//                 {
//                     mainCanvas = documentController.MainCanvas;
//                     Debug.Log($"Using MainCanvas from DocumentController: {mainCanvas.name}");
//                 }
//                 break;

//             case "App":
//                 AppController appController = FindAnyObjectByType<AppController>();
//                 if (appController != null && appController.MainCanvas != null)
//                 {
//                     mainCanvas = appController.MainCanvas;
//                     Debug.Log($"Using MainCanvas from AppController: {mainCanvas.name}");
//                 }
//                 break;

//             case "Training":
//                 TrainingController trainingController = FindAnyObjectByType<TrainingController>();
//                 if (trainingController != null && trainingController.MainCanvas != null)
//                 {
//                     mainCanvas = trainingController.MainCanvas;
//                     Debug.Log($"Using MainCanvas from TrainingController: {mainCanvas.name}");
//                 }
//                 break;
//         }

//         // Set canvases array
//         if (mainCanvas != null)
//         {
//             sceneCanvases = new GameObject[] { mainCanvas };
//         }
//         else
//         {
//             Debug.LogError($"Could not find scene canvas for {currentSceneName}. Make sure the scene controller is properly set up.");
//             sceneCanvases = new GameObject[0]; // Empty array to avoid null references
//         }
//     }

//     // Show break screen
//     private void ShowBreak(string message)
//     {
//         // Hide Everything first
//         HideAllCanvasesAndContent();

//         // Display break canvas
//         if (breakCanvas != null)
//         {
//             breakCanvas.gameObject.SetActive(true);
//             breakText.text = message;
//             Debug.Log(message); 

//         }
//         else
//         {
//             Debug.LogError("Break canvas not found!");
//         }
//     }

//     // Generate task description based on condition
//     private string GetTaskDescriptionForCondition(bool intentionType, bool feedbackType)
//     {
//         string taskInfo = "Task:\n\n";
        
//         if (intentionType && feedbackType) {
//             // Select with Feedback
//             return taskInfo + "• GAZE-CLICK buttons to interact\n• With Icon Pop-up Feedback";
//         }
//         else if (intentionType && !feedbackType) {
//             // Select without Feedback
//             return taskInfo + "• GAZE-CLICK buttons to interact\n• No Icon Pop-up Feedback";
//         }
//         else if (!intentionType && feedbackType) {
//             // Observe with Feedback
//             return taskInfo + "• FIXATE at the indicated icons\n• With Icon Pop-up Feedback";
//         }
//         else {
//             // Observe without Feedback
//             return taskInfo + "• FIXATE at the indicated icons\n• No Icon Pop-up Feedback";
//         }
//     }

//     #endregion

//     #region Scene Management Methods

//     // Load a scene
//     private void LoadScene(string sceneName)
//     {
//         Debug.Log($"Loading scene: {sceneName}");
//         PlayerPrefs.SetString("CurrentScene", sceneName);
//         PlayerPrefs.Save();
//         SceneManager.LoadScene(sceneName);
//     }

//     // Validate scenes in build settings
//     public void ValidateScenes()
//     {
//         Debug.Log("Validate Scenes");
//         availableScenes.Clear();

//         // Use predefined scenes instead of searching build settings
//         foreach (string sceneName in scenesInBuild)
//         {
//             if (sceneName != "Training" && sceneName != "ExperimentFlow" && !sceneName.ToLower().Contains("title"))
//             {
//                 availableScenes.Add(sceneName);
//                 Debug.Log($"Added scene to available scenes: {sceneName}");
//             }
//         }

//         if (availableScenes.Count == 0)
//         {
//             Debug.LogError("Flow is not correct, no scenes found in predefined scenes!");
//         }
//     }

//     #endregion

//     #region Icon Management Methods

//     // Handles icon interaction logic
//     private void HandleIconInteraction(string iconName)
//     {
//         // Check if already at end of sequence
//         if (GetCurrentTrialIndex() >= iconSequence.Count)
//         {
//             Debug.Log($"Ignoring interaction with {iconName} - already at end of sequence (index {GetCurrentTrialIndex()}, count {iconSequence?.Count ?? 0})");
//             return;
//         }

//         // Process interaction
//         processingInteraction = true;

//         Debug.Log($"Processing interaction with {iconName} - incrementing index from {GetCurrentTrialIndex()} to {GetCurrentTrialIndex() + 1} of {iconSequence?.Count ?? 0}");
        
//         // Important: Increment the trial index first
//         int currentIndex = GetCurrentTrialIndex();
//         SetCurrentTrialIndex(currentIndex + 1);
        
//         // Now check if we've completed all trials after incrementing
//         if (GetCurrentTrialIndex() >= iconSequence.Count)
//         {
//             // We've reached the end of the sequence
//             CompleteCurrentSequence();
//         }
//         else
//         {
//             // Continue with next icon in the sequence
//             Debug.Log($"Continuing with next icon {GetCurrentTrialIndex() + 1}/{iconSequence?.Count ?? 0}");
//             StartCoroutine(DelayAndContinueTrialFlow());
//         }
//     }

//     // Separate coroutine to handle the delay and continuation of the trial flow
//     private IEnumerator DelayAndContinueTrialFlow()
//     {
//         // First run the ISI (inter-stimulus interval) phase
//         yield return StartCoroutine(ISIPhase());
        
//         // Then continue with the next trial
//         bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//         bool select = PlayerPrefs.GetInt("Select", 0) == 1;
        
//         // Make sure we have a valid icon to continue with
//         if (iconSequence != null && GetCurrentTrialIndex() < iconSequence.Count)
//         {
//             string nextIcon = iconSequence[GetCurrentTrialIndex()];
//             Debug.Log($"Starting next trial with icon: {nextIcon}");
            
//             // Start the next trial without reloading the scene
//             yield return StartCoroutine(LinearTrialFlow(nextIcon, withFeedback, select, currentSceneName, false));
//         }
//         else
//         {
//             Debug.LogError("Cannot continue trial flow: Invalid icon index or sequence");
//         }
        
//         // Reset processing flag
//         processingInteraction = false;
//     }


//     // Get icon sprite for instructions
//     private Sprite GetInstructionIconSprite(string iconName)
//     {
//         // Check mappings first
//         if (instructionIcons != null)
//         {
//             foreach (var mapping in instructionIcons)
//             {
//                 if (mapping.iconName == iconName && mapping.iconSprite != null)
//                     return mapping.iconSprite;
//             }
//         }

//         // Try Resources
//         string resourcePath = "Instruction-Icons/" + iconName;
//         Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
//         if (loadedSprite != null)
//             return loadedSprite;

//         // Try direct asset path
// #if UNITY_EDITOR
//         string fullPath = "Assets/Icons-Backgrounds/Instruction-Icons/" + iconName + ".png";
//         loadedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
//         if (loadedSprite != null)
//             return loadedSprite;
// #endif

//         // Return null if not found
//         Debug.LogWarning($"Could not find icon sprite for '{iconName}'.");
//         return null;
//     }

//     // Prepare icon targets
//     private void PrepareForTargetIcon(string iconName, bool withFeedback, bool select)
//     {
//         Debug.Log($"Preparing all icons for {iconName} - With_Feedback: {withFeedback}, Select: {select}");

//         currentTargetIcon = iconName;

//         // Find and configure all icon buttons
//         IconButton[] iconButtons = FindObjectsByType<IconButton>(FindObjectsSortMode.None);
//         foreach (var icon in iconButtons)
//         {
//             if (icon == null) continue;

//             // Reset state for all icons
//             icon.ResetInteractionState();

//             // Set interaction mode
//             icon.SetInteraction(select);
//             icon.Set_With_Feedback(withFeedback);

//             // Set interactability based on target
//             bool isTarget = (icon.iconName == iconName);
//             if (icon.GetComponent<Button>() != null)
//                 icon.GetComponent<Button>().interactable = (isTarget && select);
//         }

//         // Video scene requires special handling
//         if (currentSceneName == "Video")
//         {
//             VideoController videoController = FindAnyObjectByType<VideoController>();
//             if (videoController == null)
//             {
//                 Debug.LogWarning("VideoController not found in scene.");
//                 return;
//             }

//             if (select)
//             {
//                 if (iconName == "Play")
//                 {
//                     videoController.VideoRenderTexture.enabled = true;
//                     videoController.DefaultStoppedMiddleState();
//                 }
//                 else if (new string[] { "Pause", "Stop", "Rewind", "Forward" }.Contains(iconName))
//                 {
//                     videoController.VideoRenderTexture.enabled = true;
//                     videoController.DefaultPlayingMiddleState();
//                 }
//             }
//             else
//             {
//                 Debug.Log("Observe mode - skipping video icon prep.");
//             }
//         }
//     }

//     // Icon mapping class for instructions
//     [System.Serializable]
//     public class InstructionIconMapping
//     {
//         public string iconName;
//         public Sprite iconSprite;
//     }

//     #endregion

//     #region Training Flow Methods

//     // Initializes and starts the training flow
//     private void StartTrainingFlow()
//     {
//         // Reset trial index for fresh training start
//         SetCurrentTrialIndex(0);
//         Debug.Log("Reset trial index to 0 for training");
        
//         // Set training mode flag
//         isInTrainingMode = true;
        
//         // Log training start
//         DataLogger.Instance.LogState($"Block{currentBlock}", 
//             PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe", 
//             PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback", 
//             "Training", "start");
        
//         // Initialize training scene list if it doesn't exist
//         if (trainingScenesList == null || trainingScenesList.Count == 0)
//         {
//             trainingScenesList = new List<string>() { "Training" };
//         }
        
//         // Reset trial index again to be absolutely sure
//         SetCurrentTrialIndex(0);
        
//         // Start with the training icons
//         if (savedIconSequences.ContainsKey("Training"))
//         {
//             iconSequence = savedIconSequences["Training"];
//             Debug.Log($"Starting TRAINING with {iconSequence.Count} trials");
            
//             // Get the current condition settings
//             bool withFeedback = PlayerPrefs.GetInt("With_Feedback", 0) == 1;
//             bool select = PlayerPrefs.GetInt("Select", 0) == 1;
            
//             // Start the first training trial
//             if (iconSequence.Count > 0)
//             {
//                 StartCoroutine(LinearTrialFlow(iconSequence[0], withFeedback, select, "Training", true));
//             }
//             else
//             {
//                 Debug.LogError("Training icon sequence is empty!");
//             }
//         }
//         else
//         {
//             Debug.LogError("No training icon sequence found!");
//         }
//     }

//     // Handles completion of the training sequence
//     private void CompleteTraining()
//     {
//         // Log completion message
//         Debug.Log($"Training completed - processed {GetCurrentTrialIndex()} icons out of {TrainingTrialsCount} trials");
        
//         // Log the training end in DataLogger
//         DataLogger.Instance.LogState($"Block{currentBlock}",
//             PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe", 
//             PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback", 
//             "Training", "end");
        
//         // Reset the trial index for the main experiment
//         SetCurrentTrialIndex(0);
        
//         // Reset training mode flag
//         isInTrainingMode = false;
        
//         // Show completion message and wait for S key to start the main experiment
//         ShowBreak("<b>Training Completed!</b>\n\nPress S to begin the experiment.");
//         waitingForSToMoveToNextExperimentScenes = true;
//     }

//     #endregion

//     #region Error Handling Methods

//     // Log timeout recovery state
//     private void LogTimeoutState(string stateMessage)
//     {
//         DataLogger.Instance.LogState(
//             $"Block{currentBlock}",
//             PlayerPrefs.GetInt("Select", 0) == 1 ? "Select" : "Observe",
//             PlayerPrefs.GetInt("With_Feedback", 0) == 1 ? "With_Feedback" : "No_Feedback",
//             currentSceneName,
//             stateMessage
//         );
//     }


//         // Add this method to fix the missing ResetAllInteractionStates reference
//     private void ResetAllInteractionStates()
//     {
//         // Find all IconButton components in the scene
//         IconButton[] iconButtons = FindObjectsByType<IconButton>(FindObjectsSortMode.None);
        
//         // Reset the state of each button
//         foreach (var icon in iconButtons)
//         {
//             if (icon != null)
//             {
//                 icon.ResetInteractionState();
//                 Debug.Log($"Reset interaction state for icon: {icon.iconName}");
//             }
//         }
        
//         Debug.Log($"Resetting all interaction states for a fresh task start");
//     }


//     #endregion

//     #region Utility Methods

//     // Randomize list items
//     private List<T> RandomizeList<T>(List<T> list)
//     {
//         List<T> randomizedList = new List<T>(list);
//         System.Random random = new System.Random();
//         List<T> result = new List<T>(list.Count);

//         while (randomizedList.Count > 0)
//         {
//             int randomIndex = random.Next(0, randomizedList.Count);
//             result.Add(randomizedList[randomIndex]);
//             randomizedList.RemoveAt(randomIndex);
//         }

//         return result;
//     }

//     // Get current block name
//     public string GetCurrentBlockName()
//     {
//         return $"Block{currentBlock}";
//     }

//     // Get current target icon
//     public string GetCurrentTargetIcon() => CurrentRequiredIcon;

//     #endregion

//     #region Trial Index Management

//     // Helper methods for managing the trial index in PlayerPrefs
//     public int GetCurrentTrialIndex()
//     {
//         return PlayerPrefs.GetInt("CurrentTrialIndex", 0);
//     }

//     public void SetCurrentTrialIndex(int index)
//     {
//         PlayerPrefs.SetInt("CurrentTrialIndex", index);
//         PlayerPrefs.Save();
//         Debug.Log($"CurrentTrialIndex set to {index}");
//     }

//     public void SetCurrentSceneIndex(int index)
//     {
//         PlayerPrefs.SetInt("CurrentSceneIndex", index);
//         PlayerPrefs.Save();
//         Debug.Log($"CurrentSceneIndex set to {index}"); // Fix: Was incorrectly saying "CurrentTrialIndex"
//     }

//     #endregion
// }