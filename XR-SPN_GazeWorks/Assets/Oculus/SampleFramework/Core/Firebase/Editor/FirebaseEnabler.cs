using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FirebaseEnabler
{
    // Helper method to convert from BuildTargetGroup to NamedBuildTarget
    private static UnityEditor.Build.NamedBuildTarget GetNamedBuildTarget(BuildTargetGroup buildTargetGroup)
    {
        switch (buildTargetGroup)
        {
            case BuildTargetGroup.Android:
                return UnityEditor.Build.NamedBuildTarget.Android;
            case BuildTargetGroup.iOS:
                return UnityEditor.Build.NamedBuildTarget.iOS;
            case BuildTargetGroup.Standalone:
                return UnityEditor.Build.NamedBuildTarget.Standalone;
            case BuildTargetGroup.WebGL:
                return UnityEditor.Build.NamedBuildTarget.WebGL;
            // Add other cases as needed
            default:
                return UnityEditor.Build.NamedBuildTarget.Unknown;
        }
    }

    public static void EnableFirebase()
    {
        // Use the modern API with NamedBuildTarget instead of BuildTargetGroup
        string symbols = PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget(BuildTargetGroup.Android));
        if (!symbols.Contains("OVR_SAMPLES_ENABLE_FIREBASE"))
        {
            symbols = string.Join(";", symbols, "OVR_SAMPLES_ENABLE_FIREBASE");
        }
        PlayerSettings.SetScriptingDefineSymbols(GetNamedBuildTarget(BuildTargetGroup.Android), symbols);
    }

    public static void DisableFirebase()
    {
        // Use the modern API with NamedBuildTarget instead of BuildTargetGroup
        string symbols = PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget(BuildTargetGroup.Android));
        if (symbols.Contains("OVR_SAMPLES_ENABLE_FIREBASE"))
        {
            symbols = symbols.Replace("OVR_SAMPLES_ENABLE_FIREBASE", "");
        }
        PlayerSettings.SetScriptingDefineSymbols(GetNamedBuildTarget(BuildTargetGroup.Android), symbols);
    }
}
