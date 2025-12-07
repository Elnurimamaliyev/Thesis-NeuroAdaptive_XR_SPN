using UnityEngine;
using Varjo.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Linq;

public class GazeXRContoller : MonoBehaviour
{
    private IconButton currentIcon;
    private CrossIcon currentCrossIcon;
    // private FixationGaze fixationGaze;

    private Vector3 hitPoint;
    public Transform controllerAnchor;

    [Header("Tracking Mode")]
    public TrackingMode trackingMode = TrackingMode.VarjoGaze;

    [Header("Simulator Actions")]
    public InputAction mousePositionAction;

    [Header("Varjo Gaze Settings")]
    public VarjoEyeTracking.GazeOutputFilterType filterType = VarjoEyeTracking.GazeOutputFilterType.Standard;
    public VarjoEyeTracking.GazeOutputFrequency frequency = VarjoEyeTracking.GazeOutputFrequency.MaximumSupported;
    public KeyCode calibrationKey = KeyCode.C;

    [Header("Debug")]
    public bool showDebugInfo = false;

    [Header("Performance Settings")]
    [Tooltip("Lower values = faster reaction but less stability")]
    [Range(0f, 0.9f)] public float smoothingFactor = 0.0f;
    [Tooltip("Skip transform conversions for faster response")]
    public bool useDirectGazeRay = true;
    [Tooltip("Skip fixation point calculation for better performance")]
    public bool skipFixationPointCalculation = true;

    // XR device variables
    private List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    private UnityEngine.XR.InputDevice device;
    private Eyes eyes;

    // Varjo variables
    private VarjoEyeTracking.GazeData varjoGazeData;

    // Optimization variables
    private Camera cachedMainCamera;
    private Ray cachedRay;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public enum TrackingMode { VarjoGaze, XRSubsystem, EyeSimulator }

    void Start()
    {
        // Initialize Varjo gaze settings
        VarjoEyeTracking.SetGazeOutputFilterType(filterType);
        VarjoEyeTracking.SetGazeOutputFrequency(frequency);

        // Enable simulator action
        mousePositionAction.Enable();

        // Get XR device
        GetXRDevice();

        cachedMainCamera = Camera.main;
    }

    private void GetXRDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
    }

    void Update()
    {
        // Request calibration if key is pressed
        if (Input.GetKeyDown(calibrationKey))
        {
            VarjoEyeTracking.RequestGazeCalibration();
        }

        switch (trackingMode)
        {
            case TrackingMode.VarjoGaze:
                UpdateWithVarjoGaze();
                break;

            case TrackingMode.XRSubsystem:
                UpdateWithXRSubsystem();
                break;

            case TrackingMode.EyeSimulator:
                UpdateControllerAnchorRotationWithMouse();
                break;
        }

        // Apply smoothing if needed
        if (smoothingFactor > 0)
        {
            controllerAnchor.position = Vector3.Lerp(controllerAnchor.position, targetPosition, 1 - smoothingFactor);
            controllerAnchor.rotation = Quaternion.Slerp(controllerAnchor.rotation, targetRotation, 1 - smoothingFactor);
        }

        // Cast ray and process hit - caching the ray for better performance
        cachedRay.origin = controllerAnchor.position;
        cachedRay.direction = controllerAnchor.forward;
        ProcessRay(cachedRay);

        // Only log debug info if absolutely necessary
        if (showDebugInfo && Time.frameCount % 60 == 0) // Only log once per ~60 frames
        {
            Debug.Log($"Gaze calibrated: {VarjoEyeTracking.IsGazeCalibrated()}, Gaze allowed: {VarjoEyeTracking.IsGazeAllowed()}");
        }
    }

    private void UpdateWithVarjoGaze()
    {
        if (!VarjoEyeTracking.IsGazeAllowed() || !VarjoEyeTracking.IsGazeCalibrated())
        {
            if (Time.frameCount % 300 == 0) // Reduce log frequency 
                Debug.LogWarning($"[{UnixTime.GetTime()}] Eye tracking not calibrated or not available");
            return;
        }

        // Get the latest gaze data from Varjo
        varjoGazeData = VarjoEyeTracking.GetGaze();

        if (varjoGazeData.status != VarjoEyeTracking.GazeStatus.Invalid)
        {
            if (useDirectGazeRay)
            {
                // Use gaze data directly without transformations for better performance
                Vector3 gazeOrigin = cachedMainCamera.transform.position;
                Vector3 gazeForward = cachedMainCamera.transform.TransformDirection(varjoGazeData.gaze.forward);

                if (skipFixationPointCalculation || varjoGazeData.focusDistance <= 0)
                {
                    targetPosition = gazeOrigin;
                }
                else
                {
                    // Use focus distance from Varjo API directly
                    targetPosition = gazeOrigin + gazeForward * varjoGazeData.focusDistance;
                }

                targetRotation = Quaternion.LookRotation(gazeForward);

                // Apply immediately if no smoothing
                if (smoothingFactor <= 0)
                {
                    controllerAnchor.position = targetPosition;
                    controllerAnchor.rotation = targetRotation;
                }
            }
            else
            {
                // Original approach with full transformations
                Vector3 gazeOrigin = cachedMainCamera.transform.TransformPoint(varjoGazeData.gaze.origin);
                Vector3 gazeForward = cachedMainCamera.transform.TransformDirection(varjoGazeData.gaze.forward);

                targetPosition = gazeOrigin;
                targetRotation = Quaternion.LookRotation(gazeForward);

                if (smoothingFactor <= 0)
                {
                    controllerAnchor.position = targetPosition;
                    controllerAnchor.rotation = targetRotation;
                }
            }
        }
    }

    private void UpdateWithXRSubsystem()
    {
        // Make sure we have a valid device
        if (!device.isValid)
        {
            GetXRDevice();
            if (!device.isValid) return;
        }

        // Get data from the XR subsystem
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
        {
            Vector3 eyePosition = Vector3.zero;
            Quaternion eyeRotation = Quaternion.identity;

            // Try to get eye data
            if (eyes.TryGetLeftEyePosition(out Vector3 leftPos) && eyes.TryGetLeftEyeRotation(out Quaternion leftRot))
            {
                eyePosition = leftPos;
                eyeRotation = leftRot;
            }
            else if (eyes.TryGetRightEyePosition(out Vector3 rightPos) && eyes.TryGetRightEyeRotation(out Quaternion rightRot))
            {
                eyePosition = rightPos;
                eyeRotation = rightRot;
            }

            // Update only if we have valid data
            if (eyePosition != Vector3.zero || eyeRotation != Quaternion.identity)
            {
                controllerAnchor.localPosition = eyePosition;
                controllerAnchor.localRotation = eyeRotation;
            }
        }
    }

    private void UpdateControllerAnchorRotationWithMouse()
    {
        Vector2 mousePosition = mousePositionAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        controllerAnchor.position = Camera.main.transform.position;
        controllerAnchor.rotation = Quaternion.LookRotation(ray.direction);
    }

    private void ProcessRay(Ray ray)
    {
        RaycastHit hit;
        // Use non-alloc version of Raycast for better performance
        if (Physics.Raycast(ray, out hit))
        {
            hitPoint = hit.point;
            currentIcon = hit.collider.GetComponent<IconButton>();
            currentCrossIcon = hit.collider.GetComponent<CrossIcon>();
        }
        else
        {
            hitPoint = ray.origin + ray.direction * 10f;
            currentIcon = null;
            currentCrossIcon = null;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 rayOrigin = controllerAnchor.position;
        Vector3 rayDirection = controllerAnchor.forward;

        Gizmos.DrawLine(rayOrigin, rayOrigin + rayDirection * 10f);
        Gizmos.DrawSphere(hitPoint, 0.05f);
    }

}
