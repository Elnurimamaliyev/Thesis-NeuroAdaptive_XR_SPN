using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;

public class CrossIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Types and Properties
    public static event Action OnCrossEndingSignal; // Event for ExperimentController
    public static event Action OnFixationTimeout; // New event for timeout
    

    [Header("Fixation Cross References")]
    [SerializeField] private Canvas fixationCanvas;

    [Header("Timeout Settings")]
    [SerializeField] private float fixationTimeoutDuration = 5.0f; // Timeout after 5 seconds
    #endregion

    #region Private Fields
    // Parameters
    private float fixation_Duration_Threshold = 10; // Default threshold, will be set from ExperimentController
    [SerializeField] public float[] fixation_Durations = new float[] { 1.250f, 1.500f, 1.750f};

    // State tracking
    private bool isCrossCompleted;
    private bool isTimeoutTriggered;
    private bool ishovering;
    private float _gazeDuration;
    private float _searchDuration;
    
    // Timestamp tracking for accurate measurements
    private string _IconGazeStartTimestamp;
    // private string _gazeExitTimestamp;
    
    // References
    private ExperimentController experimentController;
    private int blockIndex => experimentController.blockIndex;
    private int trialIndex => experimentController.trialIndex;

    private bool select => experimentController.select;
    private bool withFeedback => experimentController.withFeedback;
    private string scene;
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Find experiment controller
        experimentController = FindAnyObjectByType<ExperimentController>();
        ResetFixation();    
    }
    private void OnEnable() 
    {
        experimentController = FindAnyObjectByType<ExperimentController>();
        scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        DataLogger.Instance?.LogFixationCross($"Block{blockIndex}", select, withFeedback, scene, fixation_Duration_Threshold*1000, gameObject.transform.position, "start");
        ResetFixation();
    }
    #endregion


    #region UI Interaction
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!ishovering) {
            ishovering = true;
            // _gazeEnterTimestamp = UnixTime.GetTime().ToString(); // Capture exact enter time
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ishovering) {
            ishovering = false;
            _gazeDuration = 0f; // Reset gaze duration when exiting
            // _gazeExitTimestamp = UnixTime.GetTime().ToString(); // Capture exact exit time
        }
    }
    #endregion

    #region Interaction Processing
    private void Update()
    {
        if (!gameObject.activeSelf || isCrossCompleted || isTimeoutTriggered) return;

        if (ishovering)
        {
            {
                _IconGazeStartTimestamp = UnixTime.GetTime().ToString(); // Capture exact enter time
            }
            _gazeDuration += Time.deltaTime;
            _searchDuration = 0;
            Debug.Log($"<color=green>Hovering={ishovering}, GazeDuration={_gazeDuration:F2}/{fixation_Duration_Threshold:F2}s, AttemptDuration={_searchDuration:F2}/{fixationTimeoutDuration:F2}s");
            
            if (_gazeDuration >= fixation_Duration_Threshold && !isCrossCompleted)
            {
                DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "cross_gazed", "start", _IconGazeStartTimestamp); isCrossCompleted = true; // Mark gaze as completed
                CompleteFixation();
            }
        }
        else if (!ishovering)
        {
            _gazeDuration=0;
            _searchDuration += Time.deltaTime; // Increment search duration only when not hovering
            Debug.Log($"<color=red>USER IS NOT LOOKING AT FIXATION CROSS</color> - Attempt Duration: {_searchDuration:F2}/{fixationTimeoutDuration:F2}s");
            if (_searchDuration >= fixationTimeoutDuration)
            {
                Debug.Log($"<color=red>FIXATION TIMEOUT</color> - User couldn't fixate on cross for {fixationTimeoutDuration}s");
                TriggerSearchTimeout();
            }
        }
    }
    #endregion

    #region Fixation Completion
    private void CompleteFixation()
    {                
        // Use the cached timestamps for more accurate timing measurement
        DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "cross_gazed", "end", UnixTime.GetTime().ToString());
        DataLogger.Instance?.LogFixationCross($"Block{blockIndex}", select, withFeedback, scene, fixation_Duration_Threshold*1000, transform.position, "end");
        DataLogger.Instance?.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "cross_presented", "end");
        Debug.Log("<color=green>USER FIXATED AT CROSS COMPLETED</color>");

        // Hide fixation canvas
        if (fixationCanvas != null)
        {
            fixationCanvas.gameObject.SetActive(false);
        }
                
        // Notify experiment controller
        OnCrossEndingSignal?.Invoke();
    }
    #endregion


    #region Public Methods
    // Method to reset the fixation state for new trials
    public void ResetFixation()
    {
    // Find experiment controller if not already assigned
    if (experimentController == null)
        experimentController = FindFirstObjectByType<ExperimentController>();
        // Randomly select a new fixation duration for each trial
        float selectedDurationMs = fixation_Durations[UnityEngine.Random.Range(0, fixation_Durations.Length)];
        fixation_Duration_Threshold = selectedDurationMs / 1000f; // Convert ms to seconds

        // Reset all state variables
        isCrossCompleted = false;        
        isTimeoutTriggered = false;
        ishovering = false;

        _gazeDuration = 0f;
        _searchDuration = 0f;
        _IconGazeStartTimestamp = null;

    }
    
    
    // Timeout handling
    private void TriggerSearchTimeout()
    {
        // Notify experiment controller about timeout
        Debug.Log("<color=red>Fixation timeout triggered - going back to instruction phase</color>");        
        OnFixationTimeout?.Invoke();
    }
    #endregion
}