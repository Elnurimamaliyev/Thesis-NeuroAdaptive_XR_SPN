# NeuroAdaptive XR System: Solving the Midas Touch Problem with SPN

📄 **[Read the Full Thesis (PDF)](./Evaluating_Stimulus_Preceding_Negativity_with_EEG_for_Neuroadaptive_Mixed_Reality_Systems_signed.pdf)**

A neuroadaptive Mixed Reality (MR) system that addresses the "Midas Touch" problem in eye-based Brain-Computer Interfaces (BCIs) by utilizing Stimulus Preceding Negativity (SPN) EEG signals for intent detection in gaze-based interactions.

## 📖 Overview

This project implements a novel approach to distinguish between intentional and unintentional gaze interactions in XR environments using EEG feedback. The system combines eye-tracking with real-time EEG analysis to detect user intent through SPN signals, eliminating false-positive selections that plague traditional gaze-only interfaces.

**Key Innovation:** By monitoring brain activity (SPN) immediately before a gaze fixation, the system can determine whether the user actually intends to select an object or is merely looking at it—effectively solving the "Midas Touch" problem where everything you look at gets selected.

## 🎯 Key Features

- **Real-time EEG-Gaze Integration**: Synchronizes eye-tracking data with EEG signals via Lab Streaming Layer (LSL)
- **SPN-based Intent Detection**: Uses Stimulus Preceding Negativity patterns to distinguish intentional from passive gaze
- **Multiple XR Scenarios**: Three different task environments (App launcher, Document editor, Video player)
- **Experimental Framework**: Complete study implementation with counterbalanced conditions, data logging, and analysis tools
- **VR/MR Hardware Support**: Compatible with Varjo XR-3 headset and Meta Quest platforms

## 🏗️ Project Structure

```
Thesis-NeuroAdaptive_XR_SPN/
├── XR-SPN_GazeWorks/              # Main Unity project
│   ├── Assets/
│   │   ├── Scenes/                # Experimental scenes
│   │   │   ├── App.unity          # App selection task
│   │   │   ├── Document.unity     # Document editing task
│   │   │   ├── Video.unity        # Video control task
│   │   │   └── Training.unity     # Practice trials
│   │   └── Scripts/
│   │       ├── 00-Study/          # Core experiment logic
│   │       │   ├── ExperimentController.cs   # Main experiment flow
│   │       │   ├── DataLogger.cs             # EEG/Gaze data logging
│   │       │   ├── CrossIcon.cs              # Fixation cross interaction
│   │       │   └── IconButton.cs             # UI element interaction
│   │       └── 09-Stabile/        # EEG integration & utilities
│   │           ├── LSLInput.cs               # Lab Streaming Layer input
│   │           ├── GazeXRController.cs       # Eye-tracking control
│   │           └── SignalSample1D.cs         # EEG signal processing
│   ├── Scripts/
│   │   └── analyze_gaze_times.py  # Post-hoc data analysis
│   ├── ProjectSettings/           # Unity configuration
│   └── Packages/                  # Unity packages and dependencies
│
├── Referance_Papers/              # Related research materials
│   └── Eye-BCI (Midas)/
│       └── Figs/                  # Reference figures
│
├── Evaluating_Stimulus_Preceding_Negativity_with_EEG_for_Neuroadaptive_Mixed_Reality_Systems_signed.pdf
└── README.md                      # This file
```

## 🚀 Getting Started

### Prerequisites

#### Hardware Requirements
- **VR/MR Headset**: Varjo XR-3 (primary) or Meta Quest (compatible)
- **EEG System**: Brain Products LiveAmp or compatible LSL-enabled EEG device (minimum 32 channels)
- **PC Requirements**:
  - Windows 10/11
  - NVIDIA RTX 2070 or better
  - 16GB+ RAM
  - USB 3.0 ports for peripherals

#### Software Requirements
- **Unity**: Version 6000.0.37f1 or compatible
- **LSL Library**: Lab Streaming Layer for real-time data streaming
- **Python**: 3.8+ (for data analysis scripts)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Elnurimamaliyev/Thesis-NeuroAdaptive_XR_SPN.git
   cd Thesis-NeuroAdaptive_XR_SPN
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Add" → Navigate to `XR-SPN_GazeWorks/` folder
   - Unity will automatically import required packages (may take several minutes)

3. **Configure XR Hardware**
   - **For Varjo XR-3**:
     - Install Varjo Base software
     - Enable eye-tracking in Varjo settings
   - **For Meta Quest**:
     - Enable Developer Mode
     - Install Oculus app and configure link

4. **Set Up LSL Stream**
   - Start your EEG acquisition software (e.g., BrainVision Recorder)
   - Ensure LSL outlet is enabled for EEG stream
   - Verify stream is visible via LSL LabRecorder

### Running the Experiment

1. **Configure Experiment Parameters**
   - Open `Assets/Scenes/ExperimentFlow.unity`
   - Select `ExperimentController` GameObject in hierarchy
   - Adjust parameters in Inspector:
     - Block count
     - Trial counts
     - Timing parameters (gaze duration, ISI, etc.)

2. **Start the Experiment**
   - Press Play in Unity Editor (for testing) OR
   - Build and run on target XR device
   - Follow on-screen instructions
   - Press 'W' to begin after initial setup

3. **Data Collection**
   - Data is automatically saved to `XR-SPN_GazeWorks/LogData/`
   - Files include:
     - `P[ID]_EEG_[timestamp].csv`: Raw EEG data
     - `P[ID]_Gaze_[timestamp].csv`: Eye-tracking data
     - `P[ID]_Events_[timestamp].csv`: Experimental events

4. **Post-Processing**
   ```bash
   cd XR-SPN_GazeWorks/Scripts
   python analyze_gaze_times.py --input ../LogData/P001_Gaze_*.csv
   ```

## 📊 Experimental Design

### Conditions
- **Baseline (Gaze-only)**: Standard dwell-time selection
- **SPN Feedback**: Neuroadaptive intent detection with visual feedback
- **Counterbalanced**: Latin square design for condition order

### Task Types
1. **App Scene**: Select target application from 5 icons
2. **Document Scene**: Choose document action from 5 options
3. **Video Scene**: Select video control from 5 buttons

### Timing Parameters
- Instruction display: 2.0s
- Gaze duration threshold: 0.75s
- Icon popup duration: 0.5s
- UI action duration: 2.0s
- Inter-stimulus interval (ISI): 1.0s

## 🔧 Key Components

### ExperimentController
Manages experimental flow, block/trial progression, condition assignment, and scene transitions.

### DataLogger
Handles real-time logging of EEG signals, gaze coordinates, event markers, and trial outcomes.

### LSLInput
Receives and processes EEG data streams via Lab Streaming Layer protocol.

### GazeXRController
Interfaces with VR headset eye-tracking, processes fixation detection, and manages gaze events.

### IconButton & CrossIcon
Handle user interactions with UI elements, detect gaze fixations, and trigger SPN analysis windows.

## 📦 Dependencies

### Unity Packages
- XR Interaction Toolkit (v3.0.7)
- High Definition Render Pipeline (HDRP v17.0.3)
- Oculus XR Plugin (v4.5.0)
- Varjo XR Plugin (via GitHub)
- Input System (v1.12.0)
- TextMeshPro

### External Libraries
- **LSL4Unity**: Lab Streaming Layer integration
- **Varjo SDK**: Eye-tracking and Mixed Reality features
- **Meta XR SDK**: Quest compatibility

### Python Libraries (for analysis)
```bash
pip install pandas numpy matplotlib seaborn
```

## 📝 Data Format

### EEG Data (CSV)
```
Time, TimeLSL, Ch1, Ch2, ..., Ch64
1234.567, 1234.567890, 0.123, -0.456, ...
```

### Gaze Data (CSV)
```
Timestamp, ParticipantID, Block, Trial, Stage, State, GazeX, GazeY, GazeZ, TargetIcon, ...
```

### Events (CSV)
```
Timestamp, EventType, BlockIndex, TrialIndex, IconName, Condition, Duration, ...
```

## 🧪 Research Context

This project is part of a Master's thesis investigating neuroadaptive interfaces for Mixed Reality systems. The research evaluates whether Stimulus Preceding Negativity (SPN) EEG signals can effectively disambiguate user intent in gaze-based BCI systems.

**Related Publication:**  
*"Evaluating Stimulus Preceding Negativity with EEG for Neuroadaptive Mixed Reality Systems"*  
(See included PDF in repository root)

## 🤝 Contributing

This is a completed research project. The code is provided as-is for reference and reproducibility purposes. No active development is planned, but feel free to fork and adapt for your own research.

## 📄 License

This project is released for academic and research purposes. Please cite appropriately if you use this code or methodology in your work.

## 👤 Author

**Elnur Imamaliyev**  
Master's Thesis Project - Ludwig Maximilian University of Munich (LMU)

## 🙏 Acknowledgments

- Varjo Technologies for XR hardware support
- Brain Products GmbH for EEG equipment
- Supervisors and colleagues at LMU Human-Computer Interaction Lab

## 📧 Contact

For questions or collaboration inquiries, please open an issue on GitHub.

---

**Note**: This is a research prototype. For production use, additional testing and optimization would be required.
