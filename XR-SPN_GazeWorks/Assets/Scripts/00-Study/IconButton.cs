using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Threading.Tasks;

public class IconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Events and Properties
    public static event Action OnTrialEndingSignal;  // Renamed from OnIconInteracted
    public static event Action OnIconGazeTimeout;
    
    private string iconName;
    [Header("Icon Attachments")]
    [SerializeField] private Transform background;
    [SerializeField] private Transform foreground;
    [SerializeField] public Button button;
    
    [Header("Timeout Settings")]
    [SerializeField] private float iconSearchTimeoutDuration = 5.0f;
    #endregion

    #region Parameters
    // Configuration parameters
    private float gaze_Duration_Threshold = 0.75f;
    private float popupDuration = 0.5f;
    private float UI_Action_Duration = 2.0f;
    
    // State tracking
    private bool ishovering;
    private bool isTimeoutTriggered;
    private bool isGazeCompleted;  // Track if gaze is completed

    private float _gazeDuration  = 0f;
    private float _searchDuration = 0f;
    
    // Timestamp tracking for accurate measurements
    private string _IconGazeStartTimestamp;
    // private string _IconGazeEndTimestamp;

    // Settings from PlayerPrefs
    public bool _With_Feedback = true;
    public bool _Select = true;

    // Positions and references
    private Vector3 _foregroundOriginalPosition;
    private Vector3 _hoveredForegroundPosition;
    private ExperimentController experimentController;
    private bool IsTargetIcon => iconName == experimentController.currentTargetIcon;
    private int blockIndex => experimentController.blockIndex;
    private int trialIndex => experimentController.trialIndex;

    private bool select => experimentController.select;
    private bool withFeedback => experimentController.withFeedback;
    private string scene;


    #endregion

    #region Initialization
    private void Start()
    {
        // Set positions and name
        // _foregroundOriginalPosition = foreground.localPosition;
        // _hoveredForegroundPosition = _foregroundOriginalPosition + new Vector3(0, 0, -100);
        // iconName = gameObject.name;
        // LoadSettings();
        // ResetInteractionState();    
    }

    private void OnEnable() 
    {
        _foregroundOriginalPosition = foreground.localPosition;
        _hoveredForegroundPosition = _foregroundOriginalPosition + new Vector3(0, 0, -100);
        iconName = gameObject.name;
        scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        ResetInteractionState();
    }
    
    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (!ishovering && IsTargetIcon) {
            ishovering = true;
            // _gazeEnterTimestamp = UnixTime.GetTime().ToString(); // Capture exact enter time
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ishovering) {
            ishovering = false;
            _gazeDuration = 0f;
            // _gazeExitTimestamp = UnixTime.GetTime().ToString(); // Capture exact exit time
        }
    }
    #endregion

    #region Interaction Processing
    private void Update()
    {
        
        if (isGazeCompleted || isTimeoutTriggered || !IsTargetIcon || !gameObject.activeSelf) return;

        if (ishovering)
        {
            // Set the timestamp only once when hovering starts
            if (string.IsNullOrEmpty(_IconGazeStartTimestamp))
            {
                _IconGazeStartTimestamp = UnixTime.GetTime().ToString(); // Capture exact enter time
            }
            _gazeDuration += Time.deltaTime;
            _searchDuration = 0;
            Debug.Log($"<color=green>Found target icon {iconName}</color> - Resetting search timer");
            
            if (_gazeDuration >= gaze_Duration_Threshold)
            {
                DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "icon_gazed", "start", _IconGazeStartTimestamp);isGazeCompleted = true;
                TriggerInteractionFlow();
            }
        }
        else if (!ishovering)
        {
            _gazeDuration=0;
            _IconGazeStartTimestamp=null; // Reset the start timestamp when not hovering
            _searchDuration += Time.deltaTime; // Increment search duration only when not hovering
            Debug.Log($"<color=yellow> '{iconName}' search time: {_searchDuration:F2}/{iconSearchTimeoutDuration}s</color>");
            if (_searchDuration >= iconSearchTimeoutDuration)
            {
                isTimeoutTriggered = true; // Mark timeout as triggered
                Debug.Log($"<color=red>TIMEOUT TRIGGERED</color>: {iconName}");
                TriggerSearchTimeout();
            }
        }
    }
    #endregion

    #region Feedback and Action
    private async void TriggerInteractionFlow()
    {
        DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "icon_gazed", "end", UnixTime.GetTime().ToString());
        DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "icon_presented", "end");
        DataLogger.Instance.LogItem($"Block{blockIndex}", select, withFeedback, scene, trialIndex, iconName, transform.position); 
        Debug.Log("<color=green>USER FIXATED AT ICON COMPLETED</color>");
        // 1. Show visual feedback if enabled
        if (_With_Feedback)
        {
            string feedbackStartTime = UnixTime.GetTime().ToString();
            DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "feedback", "start", feedbackStartTime);
            foreground.localPosition = _hoveredForegroundPosition;
            await Task.Delay((int)(popupDuration * 1000)); // Replace yield with blocking delay

            foreground.localPosition = _foregroundOriginalPosition;
            string feedbackEndTime = UnixTime.GetTime().ToString();
            DataLogger.Instance.LogTask($"Block{blockIndex}", select, withFeedback, scene, trialIndex, "feedback", "end", feedbackEndTime);
        }

        // 2. Perform button action if in selection mode
        if (_Select)
        {
            button.onClick.Invoke();  
            await Task.Delay((int)(UI_Action_Duration * 1000)); // Replace yield with blocking delay
        }

        // 3. Complete the interaction
        Debug.Log($"IconButton: Notified trial ending signal with {iconName}"); 
        OnTrialEndingSignal?.Invoke();  
    }
    #endregion

    #region Event Handlers    
    private void TriggerSearchTimeout()
    {
        Debug.Log($"<color=red>TRIGGERING SEARCH TIMEOUT EVENT</color> for {iconName}");
        OnIconGazeTimeout?.Invoke();
    }
    #endregion

    #region Public Methods    
    public void ResetInteractionState()
    {
        // Find experiment controller if not already assigned
        if (experimentController == null)
            experimentController = FindFirstObjectByType<ExperimentController>();

        // Retrieve experiment settings
        var (_, _, _, select, withFeedback) = ExperimentController.Instance.GetExperimentSettings();

        // Reset all state variables and visual state
        isTimeoutTriggered = false;
        isGazeCompleted = false; // Reset gaze completion state
        ishovering = false;

        _gazeDuration = 0f;        
        _searchDuration = 0f;
        _IconGazeStartTimestamp = null;

        foreground.localPosition = _foregroundOriginalPosition;        
        _Select = select;
        _With_Feedback = withFeedback;

        // Update button state
        button.interactable = IsTargetIcon && select;

        // Log the reset interaction state with experiment settings
        Debug.Log($"Resetting interaction state for IconButton. BlockIndex: {blockIndex}, SceneIndex: {blockIndex}, TrialIndex: {trialIndex}, Select: {select}, WithFeedback: {withFeedback}");
        Debug.Log($"IconButton: Reset interaction state for {iconName}, ready for new interactions");
    }
    #endregion
}