using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class VideoController : MonoBehaviour
{
    [Header("UI Behavior")]
    public float UI_Action_Duration = 2.0f;
    [SerializeField] private float skipTimeAmount = 5.0f;

    [Header("Parent Canvas References")]
    public GameObject MainCanvas;
    [SerializeField] private GameObject videoButtons;
    [SerializeField] private GameObject videoWindows;

    [Header("Video Components")]
    public VideoPlayer VideoPlayer;
    public RawImage VideoRenderTexture;
    
    [Header("Video Buttons")]
    public Button PlayButton;
    public Button PauseButton;
    public Button StopButton;
    public Button RewindButton;
    public Button ForwardButton;

    [Header("Video Windows")]
    public GameObject VideoRenderCanvas;
    
    private ExperimentController experimentController;
    private bool IsSelectionMode => PlayerPrefs.GetInt("Select", 1) == 1;

    void Awake()
    {
        // Only assign MainCanvas if it's null, don't create a new one
        if (MainCanvas == null)
        {
            // Try to find existing canvas in the scene
            GameObject existingCanvas = GameObject.Find("MainVideoCanvas");
            if (existingCanvas != null)
            {
                MainCanvas = existingCanvas;
                Debug.Log($"Found existing MainVideoCanvas");
            }
        }
        
        // Ensure VideoRenderTexture is assigned
        if (VideoRenderTexture == null)
        {
            RawImage[] rawImages = GetComponentsInChildren<RawImage>(true);
            if (rawImages.Length > 0)
            {
                VideoRenderTexture = rawImages[0];
                Debug.Log($"Found VideoRenderTexture: {VideoRenderTexture.name}");
            }
        }
    }
    
    void Start()
    {
        // Find experiment controller and get timing parameters
        experimentController = FindAnyObjectByType<ExperimentController>();
        if (experimentController != null)
        {
            UI_Action_Duration = experimentController.UI_Action_Duration;
        }
        
        // Setup video player
        InitializeVideoPlayer();
        
        // Add button listeners
        AddButtonListeners();
        
        // Log startup for debugging
        Debug.Log("VideoController initialized successfully");
        
        // Default to stopped state
        DefaultStoppedMiddleState();
    }
    
    private void InitializeVideoPlayer()
    {
        // Setup video player
        VideoPlayer.isLooping = IsSelectionMode;
        
        // Disable texture if not in Selection mode
        UpdateVideoVisibility();
        
        VideoPlayer.prepareCompleted += OnVideoPrepareCompleted;
    }
    
    private void AddButtonListeners()
    {
        PlayButton?.onClick.AddListener(PlayVideo);
        PauseButton?.onClick.AddListener(PauseVideo);
        RewindButton?.onClick.AddListener(RewindVideo);
        ForwardButton?.onClick.AddListener(ForwardVideo);
        StopButton?.onClick.AddListener(StopVideo);
    }
    
    // Update video texture visibility based on Selection mode
    private void UpdateVideoVisibility()
    {
        if (VideoRenderTexture != null)
        {
            VideoRenderTexture.enabled = IsSelectionMode;
        }
        
        if (VideoRenderCanvas != null)
        {
            VideoRenderCanvas.SetActive(IsSelectionMode);
        }
    }
    
    // Button action methods
    public void PlayVideo()
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.Play();
            Log_ui_action_start();
            StartCoroutine(VideoActionFeedback());
        }
    }
    
    public void PauseVideo() 
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.Pause();
            Log_ui_action_start();
            StartCoroutine(VideoActionFeedback());
        }
    }

    public void StopVideo()
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.Stop();
            Log_ui_action_start();
            StartCoroutine(VideoActionFeedback());
        }
    }

    public void RewindVideo()
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.time = Mathf.Max(0, (float)(VideoPlayer.time - skipTimeAmount));
            Log_ui_action_start();
            StartCoroutine(VideoActionFeedback());
        }
    }

    public void ForwardVideo()
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.time = Mathf.Min((float)(VideoPlayer.time + skipTimeAmount), (float)VideoPlayer.length);
            Log_ui_action_start();
            StartCoroutine(VideoActionFeedback());
        }
    }
    
    // Logging helpers
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
    
    private IEnumerator VideoActionFeedback()
    {
        // Wait for UI action duration
        yield return new WaitForSeconds(experimentController != null ? experimentController.UI_Action_Duration : 2.0f);
        
        // Log UI action end
        Log_ui_action_end();
    }
    
    // Default state setters for experiment controller
    public void DefaultStoppedMiddleState()
    {
        if (!IsSelectionMode)
        {
            // If in Observe mode, disable everything
            if (VideoRenderCanvas != null)
                VideoRenderCanvas.SetActive(false);
            if (VideoRenderTexture != null)
                VideoRenderTexture.enabled = false;
            return;
        }
        
        // In Selection mode, show everything
        if (VideoRenderCanvas != null)
            VideoRenderCanvas.SetActive(true);
        if (VideoRenderTexture != null)
            VideoRenderTexture.enabled = true;
            
        VideoPlayer.Stop();
        VideoPlayer.time = VideoPlayer.length / 2;
        VideoPlayer.Pause(); 
    }

    public void DefaultPlayingMiddleState()
    {
        if (!IsSelectionMode)
        {
            // If in Observe mode, disable everything
            if (VideoRenderCanvas != null)
                VideoRenderCanvas.SetActive(false);
            if (VideoRenderTexture != null)
                VideoRenderTexture.enabled = false;
            return;
        }
        
        // In Selection mode, show everything
        if (VideoRenderCanvas != null)
            VideoRenderCanvas.SetActive(true);
        if (VideoRenderTexture != null)
            VideoRenderTexture.enabled = true;
            
        VideoPlayer.Stop();
        VideoPlayer.time = VideoPlayer.length / 2;
        VideoPlayer.Play();
    }
    
    private void OnVideoPrepareCompleted(VideoPlayer vp)
    {        
        // Only set video time and enable texture if in Selection mode
        if (IsSelectionMode)
        {
            VideoPlayer.time = VideoPlayer.length / 2;
            
            if (VideoRenderCanvas != null)
                VideoRenderCanvas.SetActive(true);
            if (VideoRenderTexture != null)
                VideoRenderTexture.enabled = true;
        }
        else
        {
            if (VideoRenderCanvas != null)
                VideoRenderCanvas.SetActive(false);
            if (VideoRenderTexture != null)
                VideoRenderTexture.enabled = false;
        }
    }
    
    private void OnEnable()
    {
        // Check mode when enabled
        UpdateVideoVisibility();
    }
    
    private void OnDestroy()
    {
        if (VideoPlayer != null)
            VideoPlayer.prepareCompleted -= OnVideoPrepareCompleted;
    }
    
    // Content visibility methods
    public void ShowMainCanvas()
    {
        // Show main canvas
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(true);
            Debug.Log($"VideoController: Showing MainCanvas {MainCanvas.name}");
        }
    }

    public void HideMainCanvas()
    {        
        // Hide main canvas
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(false);
            Debug.Log($"VideoController: Hiding MainCanvas {MainCanvas.name}");
        }
    }
}