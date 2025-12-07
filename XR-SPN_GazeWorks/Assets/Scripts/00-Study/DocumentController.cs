using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DocumentController : MonoBehaviour
{
    [Header("UI Behavior")]
    public float UI_Action_Duration = 2.0f;

    [Header("Parent Canvas References")]
    public GameObject MainCanvas;
    private ExperimentController experimentController;
    [SerializeField] private GameObject documentButtons;
    [SerializeField] private GameObject documentWindows;

    [Header("Document Components")]
    public TMP_Text feedbackText;

    [Header("Document Buttons")]
    public Button UndoButton;
    public Button RedoButton;
    public Button SaveButton;
    public Button ExportButton;
    public Button CloseButton;

    [Header("Document Windows")]
    public GameObject ScreenWindow;
    public GameObject TextCanvas;


    private bool isInButtonInteraction = false;
    private bool IsSelectionMode => PlayerPrefs.GetInt("Select", 1) == 1;

    void Awake()
    {
        if (MainCanvas == null)
        {
            GameObject existingCanvas = GameObject.Find("MainDocumentCanvas");
            if (existingCanvas != null)
            {
                MainCanvas = existingCanvas;
                Debug.Log("Found existing MainDocumentCanvas");
            }
        }
    }

    private void Start()
    {
        experimentController = FindAnyObjectByType<ExperimentController>();
        if (experimentController != null)
        {
            UI_Action_Duration = experimentController.UI_Action_Duration;
        }

        SaveButton.onClick.AddListener(() => HandleButtonClick("Save", "Document saved."));
        ExportButton.onClick.AddListener(() => HandleButtonClick("Export", "Document Exported."));
        UndoButton.onClick.AddListener(() => HandleButtonClick("Undo", "Undo Successful."));
        RedoButton.onClick.AddListener(() => HandleButtonClick("Redo", "Redo Successful."));
        CloseButton.onClick.AddListener(() => HandleButtonClick("Close", "Document Closed."));

        feedbackText.text = "Hello World!";
        feedbackText.gameObject.SetActive(IsSelectionMode);
        Debug.Log($"Feedback text {(IsSelectionMode ? "enabled" : "disabled")} based on mode");
    }

    private void HandleButtonClick(string buttonName, string screenMessage)
    {
        if (isInButtonInteraction) return;
        isInButtonInteraction = true;

        Log_ui_action_start();
        Debug.Log($"DocumentController: HandleButtonClick for {buttonName}");

        if (buttonName == "Close")
        {
            StartCoroutine(ToggleScreenWindow(UI_Action_Duration));
        }

        StartCoroutine(DocumentFeedback(screenMessage, UI_Action_Duration));
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

    private IEnumerator ToggleScreenWindow(float duration)
    {
        ScreenWindow.SetActive(false);
        yield return new WaitForSeconds(duration / 2);
        ScreenWindow.SetActive(true);
        yield return new WaitForSeconds(duration / 2);
        isInButtonInteraction = false;
    }

    private IEnumerator DocumentFeedback(string message, float duration)
    {
        feedbackText.text = message;
        yield return new WaitForSeconds(duration);
        feedbackText.text = "Hello World!";
        Log_ui_action_end();
        isInButtonInteraction = false;
    }

    public void ShowMainCanvas()
    {
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(true);
            Debug.Log($"DocumentController: Showing MainCanvas {MainCanvas.name}");
        }
    }

    public void HideMainCanvas()
    {
        if (MainCanvas != null)
        {
            MainCanvas.SetActive(false);
            Debug.Log($"DocumentController: Hiding MainCanvas {MainCanvas.name}");
        }
    }
}
