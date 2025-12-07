using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;

public class DataLogger : MonoBehaviour
{
    #region Singleton Implementation
    private static DataLogger _instance;
    public static DataLogger Instance => _instance;
    public static string rootFolder = "./LogData/";

    internal void Setup()
    {
        if (_instance == null)
        {
            _instance = this;

            // Set up participant ID once and save to PlayerPrefs
            SetupParticipantIDAndConditions();
        }
    }
    #endregion
    
    #region Fields and Properties
    public static int ParticipantID { get; private set; } = 1;
    
    private Dictionary<int, List<string>> participantConditions = new Dictionary<int, List<string>>();
    
    public List<int> ConditionOrder { get; private set; }
    public string stateFilePath { get; private set; }
    public string crossFilePath { get; private set; }
    public string itemFilePath { get; private set; }
    public string taskFilePath { get; private set; }
    public string eegFilePath { get; private set; }
    private StreamWriter swState;
    private StreamWriter swCross;
    private StreamWriter swItem;
    private StreamWriter swTask;
    private StreamWriter swEeg; 
    private StringBuilder stringbuilderEeg = new StringBuilder();
    private int countedEeg = 0;
    [Header("Participant Settings")]
    [SerializeField] private int participantID = 1;
    private HashSet<string> rejectedStreamNames = new HashSet<string>();
    #endregion
    
    #region EEG Data Handling
    internal void write(string name, SignalSample1D s)
    {
        if (swEeg == null)
        {
            EnsureInitialized();
        }
        
        if (name.ToLower() == "eeg" || name.StartsWith("LiveAmp"))
        {
            if (s.values == null)
            {
                Debug.LogError("EEG values array is null!");
                return;
            }
            
            if (countedEeg == 0)
            {
                Debug.Log($"EEG stream detected: '{name}' with {s.values.Length} channels");
            }
            
            if (s.values.Length == 33)
            {
                stringbuilderEeg.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33}{34}", s.time, s.timeLsl, s.values[0], s.values[1], s.values[2], s.values[3], s.values[4], s.values[5], s.values[6], s.values[7], s.values[8], s.values[9], s.values[10], s.values[11], s.values[12], s.values[13], s.values[14], s.values[15], s.values[16], s.values[17], s.values[18], s.values[19], s.values[20], s.values[21], s.values[22], s.values[23], s.values[24], s.values[25], s.values[26], s.values[27], s.values[28], s.values[29], s.values[30], s.values[31], Environment.NewLine);
            }
            else if (s.values.Length == 32)
            {
                stringbuilderEeg.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33}{34}", s.time, s.timeLsl, s.values[0], s.values[1], s.values[2], s.values[3], s.values[4], s.values[5], s.values[6], s.values[7], s.values[8], s.values[9], s.values[10], s.values[11], s.values[12], s.values[13], s.values[14], s.values[15], s.values[16], s.values[17], s.values[18], s.values[19], s.values[20], s.values[21], s.values[22], s.values[23], s.values[24], s.values[25], s.values[26], s.values[27], s.values[28], s.values[29], s.values[30], s.values[31], Environment.NewLine);
            }
            else if (s.values.Length == 65)
            {
                stringbuilderEeg.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{47},{48},{49},{50},{51},{52},{53},{54},{55},{56},{57},{58},{59},{60},{61},{62},{63},{64},{65},{66}{67}", s.time, s.timeLsl, s.values[0], s.values[1], s.values[2], s.values[3], s.values[4], s.values[5], s.values[6], s.values[7], s.values[8], s.values[9], s.values[10], s.values[11], s.values[12], s.values[13], s.values[14], s.values[15], s.values[16], s.values[17], s.values[18], s.values[19], s.values[20], s.values[21], s.values[22], s.values[23], s.values[24], s.values[25], s.values[26], s.values[27], s.values[28], s.values[29], s.values[30], s.values[31], s.values[32], s.values[33], s.values[34], s.values[35], s.values[36], s.values[37], s.values[38], s.values[39], s.values[40], s.values[41], s.values[42], s.values[43], s.values[44], s.values[45], s.values[46], s.values[47], s.values[48], s.values[49], s.values[50], s.values[51], s.values[52], s.values[53], s.values[54], s.values[55], s.values[56], s.values[57], s.values[58], s.values[59], s.values[60], s.values[61], s.values[62], s.values[63], Environment.NewLine);
            }
            else if (s.values.Length == 64)
            {
                stringbuilderEeg.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{47},{48},{49},{50},{51},{52},{53},{54},{55},{56},{57},{58},{59},{60},{61},{62},{63},{64},{65}{66}", s.time, s.timeLsl, s.values[0], s.values[1], s.values[2], s.values[3], s.values[4], s.values[5], s.values[6], s.values[7], s.values[8], s.values[9], s.values[10], s.values[11], s.values[12], s.values[13], s.values[14], s.values[15], s.values[16], s.values[17], s.values[18], s.values[19], s.values[20], s.values[21], s.values[22], s.values[23], s.values[24], s.values[25], s.values[26], s.values[27], s.values[28], s.values[29], s.values[30], s.values[31], s.values[32], s.values[33], s.values[34], s.values[35], s.values[36], s.values[37], s.values[38], s.values[39], s.values[40], s.values[41], s.values[42], s.values[43], s.values[44], s.values[45], s.values[46], s.values[47], s.values[48], s.values[49], s.values[50], s.values[51], s.values[52], s.values[53], s.values[54], s.values[55], s.values[56], s.values[57], s.values[58], s.values[59], s.values[60], s.values[61], s.values[62], s.values[63], Environment.NewLine);
            }
            else
            {
                throw new NotImplementedException("Your electrode count is not 32/33/64/65, please adjust the script");
            }

            countedEeg++;
            if (countedEeg % 1000 == 0)
            {
                swEeg.WriteLine(stringbuilderEeg);
                stringbuilderEeg.Clear();
                swEeg.Flush();
            }
        }
        else
        {
            if (!rejectedStreamNames.Contains(name))
            {
                Debug.LogWarning($"Logger Data Dropped - Unknown stream type: {name}. Add this device type to the DataLogger if it's an EEG device.");
                rejectedStreamNames.Add(name);
            }
        }
    }
    #endregion

    #region Initialization Methods
    private void Start()
    {        
        EnsureRootFolderExists();
        // Initialize();
    }
    
    private void LoadConditionsFromCSV()
    {
        string csvFilePath = Path.Combine(Application.dataPath, "Scripts", "00-Study", "PredefinedLatinSquare.csv");
        
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"CSV file not found at path: {csvFilePath}");
            throw new System.Exception($"CSV file not found at path: {csvFilePath}");
        }
        
        try
        {
            string[] lines = File.ReadAllLines(csvFilePath);
            
            // Find the row with matching participant ID
            bool found = false;
            for (int i = 1; i < lines.Length; i++) // Start from 1 to skip header
            {
                string[] values = lines[i].Split(',');
                if (values.Length < 5) // ID + 4 conditions
                {
                    continue; // Skip malformed rows
                }
                
                if (int.TryParse(values[0], out int id) && id == ParticipantID)
                {
                    // Direct mapping: columns 1-4 are conditions for blocks 1-4
                    List<string> conditions = new List<string>();
                    for (int j = 1; j <= 4; j++) // Get conditions from columns 1-4
                    {
                        conditions.Add(values[j].Trim());
                    }
                    
                    // Set conditions and convert to numeric indices
                    ConditionOrder = ConvertConditionsToIndices(conditions);
                    
                    // Save to PlayerPrefs
                    string conditionOrderString = string.Join(",", ConditionOrder);
                    PlayerPrefs.SetString("ConditionOrder", conditionOrderString);
                    PlayerPrefs.Save();
                    
                    Debug.Log($"Loaded condition order for Participant {ParticipantID}: {string.Join(", ", ConditionOrder)}");
                    
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogError($"No condition order found for Participant ID {ParticipantID} in the CSV file!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading CSV file: {ex.Message}");
        }
    }
    
    private List<int> ConvertConditionsToIndices(List<string> conditionNames)
    {
        List<int> indices = new List<int>();
        
        foreach (string conditionName in conditionNames)
        {
            int index = -1;
            
            switch (conditionName)
            {
                case "Select_With_Feedback": index = 0; break;
                case "Select_No_Feedback": index = 1; break;
                case "Observe_With_Feedback": index = 2; break;
                case "Observe_No_Feedback": index = 3; break;
                default:
                    Debug.LogError($"Unknown condition name in CSV: {conditionName}");
                    throw new System.Exception($"Unknown condition name in CSV: {conditionName}");
            }
            
            if (index >= 0)
                indices.Add(index);
        }
        
        return indices;
    }
    
    private void SetupParticipantIDAndConditions()
    {
        // Always use the Inspector value directly and save it
        ParticipantID = participantID;
        PlayerPrefs.SetInt("ParticipantID", ParticipantID);
        // Debug.Log($"Participant ID: {ParticipantID}");
        
        // Load conditions directly from CSV
        LoadConditionsFromCSV();
    }
    
    public List<int> GetConditionOrder()
    {
        // If we already have the condition order, return it
        if (ConditionOrder != null && ConditionOrder.Count > 0)
            return ConditionOrder;
        
        // Otherwise try to load from PlayerPrefs
        string conditionOrderString = PlayerPrefs.GetString("ConditionOrder", "");
        
        if (!string.IsNullOrEmpty(conditionOrderString))
        {
            string[] orderStrings = conditionOrderString.Split(',');
            ConditionOrder = new List<int>();
            
            foreach (string orderStr in orderStrings)
            {
                if (int.TryParse(orderStr, out int order))
                    ConditionOrder.Add(order);
            }
            
            if (ConditionOrder.Count > 0)
            {
                // Use the same formatting for consistency
                Debug.Log($"Loaded condition order from PlayerPrefs: {string.Join(", ", ConditionOrder)}");
                return ConditionOrder;
            }
        }
        
        // If not in PlayerPrefs, load from CSV
        LoadConditionsFromCSV();
        return ConditionOrder;
    }
    
    private void EnsureRootFolderExists()
    {
        rootFolder = rootFolder.Replace(" ", "");
        if (!rootFolder.EndsWith("/") && !rootFolder.EndsWith("\\"))
            rootFolder = rootFolder + "/";
            
        if (!Directory.Exists(rootFolder))
            Directory.CreateDirectory(rootFolder);
    }

    public void Initialize()
    {        
        string basePath = Path.Combine(rootFolder, $"ID-{ParticipantID}");
        
        stateFilePath = $"{basePath}-state.csv";
        crossFilePath = $"{basePath}-cross.csv";
        itemFilePath = $"{basePath}-item.csv";
        taskFilePath = $"{basePath}-task.csv";
        eegFilePath = $"{basePath}-EEG.csv";
        
        InitializeFile(stateFilePath, "Time,Block,Intention,Feedback,Scene,State");
        InitializeFile(crossFilePath, "Time,Block,Intention,Feedback,Scene,Duration,X,Y,Z,State");
        InitializeFile(itemFilePath, "Time,Block,Intention,Feedback,Scene,Taskcount,Item,X,Y,Z");
        InitializeFile(taskFilePath, "Time,Block,Intention,Feedback,Scene,Taskcount,Stage,State");
        InitializeFile(eegFilePath, "Time,TimeLsl,Fp1,Fz,F3,F7,F9,FC5,FC1,C3,T7,CP5,CP1,Pz,P3,P7,P9,O1,Oz,O2,P10,P8,P4,CP2,CP6,T8,C4,Cz,FC2,FC6,F10,F8,F4,Fp2,AF7,AF3,AFz,F1,F5,FT7,FC3,C1,C5,TP7,CP3,P1,P5,PO7,PO3,Iz,POz,PO4,PO8,P6,P2,CPz,CP4,TP8,C6,C2,FC4,FT8,F6,F2,AF4,AF8");
        
        InitializeWriters();
    }
    
    private void EnsureInitialized()
    {
        if (string.IsNullOrEmpty(stateFilePath) || string.IsNullOrEmpty(eegFilePath))
        {
            Debug.Log("DataLogger not initialized, initializing now...");
            Initialize();
        }
        
        if (swState == null || swCross == null || swItem == null || swTask == null || swEeg == null)
            InitializeWriters();
    }
    
    private void InitializeWriters()
    {
        CloseAllWriters();
        
        System.Threading.Thread.Sleep(100);
        
        swState = CreateOrOpenWriter(stateFilePath);
        swCross = CreateOrOpenWriter(crossFilePath);
        swItem = CreateOrOpenWriter(itemFilePath);
        swTask = CreateOrOpenWriter(taskFilePath);
        swEeg = CreateOrOpenWriter(eegFilePath);
        
        // Verify all writers were created successfully
        if (swState == null || swCross == null || swItem == null || swTask == null || swEeg == null)
        {
            Debug.LogError("Failed to initialize one or more writers");
            throw new System.Exception("Failed to initialize one or more writers");
        }
    }
    #endregion
    
    #region Core Logging Methods
    private bool LogToFile(StreamWriter writer, string record, string errorContext = "logging record")
    {
        EnsureInitialized();
        
        if (writer == null || writer.BaseStream == null || !writer.BaseStream.CanWrite)
        {
            Debug.LogError($"Writer is null or not writable when {errorContext}");
        }
        
        writer.WriteLine(record);
        writer.Flush();
        return true;
    }
    
    public void LogState(string block, bool select, bool withFeedback, string scene, string state)
    {
        string unixTime = UnixTime.GetTime().ToString(); // Use Unix time
        string intention = select ? "Select" : "Observe";
        string feedback = withFeedback ? "With_Feedback" : "No_Feedback";

        string record = $"{unixTime},{block},{intention},{feedback},{scene},{state}";
        
        if (LogToFile(swState, record, "logging state"))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"State: {state} in {block}, {intention}+{feedback}, scene={scene}");
            #endif
        }
    }

    public void LogItem(string block, bool select, bool withFeedback, string scene, int trialIndex, string itemName, Vector3 position)
    {
        string unixTime = UnixTime.GetTime().ToString(); // Use Unix time

        string intention = select ? "Select" : "Observe";
        string feedback = withFeedback ? "With_Feedback" : "No_Feedback";

        if (!itemName.StartsWith(scene + "_"))
            itemName = scene + "_" + itemName;
        string record = $"{unixTime},{block},{intention},{feedback},{scene},{trialIndex},{itemName},{position.x},{position.y},{position.z}";
        
        LogToFile(swItem, record, "logging item");
    }
    
    public void LogTask(string block, bool select, bool withFeedback, string scene, int trialIndex, string stage, string state, string timestamp = null)
    {
        string unixTime; string intention = select ? "Select" : "Observe"; string feedback = withFeedback ? "With_Feedback" : "No_Feedback";
        if (timestamp != null)
        {
            unixTime = timestamp;
        }
        else
        {
            unixTime = UnixTime.GetTime().ToString(); // Use Unix time
        }
        
        string record = $"{unixTime},{block},{intention},{feedback},{scene},{trialIndex},{stage},{state}";
        
        if (LogToFile(swTask, record, "logging task"))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (stage == "instruction" || stage == "cross_presented" || stage.Contains("timeout") || stage == "cross_gazed")
            {
                Debug.Log($"Task: {stage} {state} (#{trialIndex})");
            }
            #endif
        }
    }

    public void LogFixationCross(string block, bool select, bool withFeedback, string scene, float duration, Vector3 position, string state)
    {
        string unixTime = UnixTime.GetTime().ToString(); // Use Unix time
        string intention = select ? "Select" : "Observe";
        string feedback = withFeedback ? "With_Feedback" : "No_Feedback";

        string record = $"{unixTime},{block},{intention},{feedback},{scene},{duration:F2},{position.x},{position.y},{position.z},{state}";
        
        LogToFile(swCross, record, "logging fixation cross");
    }
    #endregion
    
    #region File Management
    private StreamWriter CreateOrOpenWriter(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) 
            throw new System.ArgumentException("File path cannot be null or empty");
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        try
        {
            if (File.Exists(filePath))
                return new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
            else
                return new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create writer for {filePath}: {ex.Message}");
            throw; // Re-throw the exception to stop execution
        }
    }
    
    private void InitializeFile(string filePath, string headers)
    {
        if (!File.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine(headers);
            }
        }
    }
    
    private void CloseWriter(StreamWriter writer)
    {
        try
        {
            if (writer != null && writer.BaseStream != null && writer.BaseStream.CanWrite)
            {
                writer.Flush();
                writer.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error closing writer: {ex.Message}");
        }
    }
    
    private void CloseAllWriters()
    {
        foreach(var writer in new StreamWriter[] { swState, swCross, swItem, swTask, swEeg })
            CloseWriter(writer);
    }
    
    private void OnDestroy()
    {
        try {
            if (swEeg != null && stringbuilderEeg.Length > 0 && swEeg.BaseStream != null && swEeg.BaseStream.CanWrite)
            {
                swEeg.WriteLine(stringbuilderEeg);
                stringbuilderEeg.Clear();
                // swState
                // swCross
                // swItem
                // swTask
            }
        }
        catch (Exception ex) {
            Debug.LogError($"Error flushing EEG data: {ex.Message}");
        }
        
        CloseAllWriters();
    }
    #endregion
    

}