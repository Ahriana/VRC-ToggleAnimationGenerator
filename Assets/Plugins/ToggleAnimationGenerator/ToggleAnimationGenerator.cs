using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public class ToggleAnimationGenerator : EditorWindow
{
    private GameObject avatar;
    private GameObject objectToAnimate;
    private VRCExpressionsMenu expressionsMenu;
    private VRCExpressionParameters expressionParameters;
    private VRCAvatarDescriptor avatarDescriptor;
    private RuntimeAnimatorController fxLayer;

    private string paramiterName;
    private string paramiterPath;

    private bool isToggleEnabledDefault = false;
    private bool isToggleSaved = true;
    private bool isToggleSynced = true;

    private bool writeAnimatorDefaults = false;

    public ToggleAnimationGenerator()
    {
        titleContent = new GUIContent("Toggle Animation Generator");
    }

    [MenuItem("Tools/Toggle Animation Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ToggleAnimationGenerator));
    }

    private void OnGUI()
    {
        GUILayout.Label("Created by AhrianaDev", EditorStyles.boldLabel);

        if (GUILayout.Button("Visit AhrianaDev's Website"))
        {
            Application.OpenURL("https://www.ahrianadev.com/");
        }

        if (GUILayout.Button("Reset"))
        {
            avatar = null;
            objectToAnimate = null;
            expressionsMenu = null;
            expressionParameters = null;
            avatarDescriptor = null;
            fxLayer = null;

            isToggleEnabledDefault = false;
            isToggleSaved = true;
            isToggleSynced = true;

            writeAnimatorDefaults = false;
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        avatar = EditorGUILayout.ObjectField("Avatar base object", avatar, typeof(GameObject), true) as GameObject;
        objectToAnimate = EditorGUILayout.ObjectField("Object to Animate", objectToAnimate, typeof(GameObject), true) as GameObject;

        // Auto-populate fields if they're null
        if (avatar != null && avatarDescriptor == null)
        {
            avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();
        }

        if (avatar != null && expressionsMenu == null && avatarDescriptor != null)
        {
            expressionsMenu = avatarDescriptor.expressionsMenu;
        }

        if (avatar != null && expressionParameters == null && avatarDescriptor != null)
        {
            expressionParameters = avatarDescriptor.expressionParameters;
        }

        if (avatar != null && fxLayer == null && avatarDescriptor != null)
        {
            fxLayer = avatarDescriptor.baseAnimationLayers[(int)VRCAvatarDescriptor.AnimLayerType.FX - 1].animatorController;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Avatar Path:", avatar == null ? "Undefined" : AnimationUtility.CalculateTransformPath(avatar.transform, null));
        EditorGUILayout.LabelField("Object To Animate Path:", objectToAnimate == null ? "Undefined" : AnimationUtility.CalculateTransformPath(objectToAnimate.transform, null));

        string avatarPath = avatar == null ? "" : AnimationUtility.CalculateTransformPath(avatar.transform, null);
        string objectToAnimatePath = objectToAnimate == null ? "" : AnimationUtility.CalculateTransformPath(objectToAnimate.transform, null);

        if (!string.IsNullOrEmpty(objectToAnimatePath) && !string.IsNullOrEmpty(avatarPath))
        {
            paramiterPath = objectToAnimatePath.Replace(avatarPath, "").TrimStart('/');
            EditorGUILayout.LabelField($"Parameter Name:", paramiterPath);

            string[] segments = objectToAnimatePath.Split('/');
            string lastSegment = segments[segments.Length - 1];

            lastSegment = lastSegment.Replace('_', ' ');
            string[] words = Regex.Split(lastSegment, @"(?<!^)(?=[A-Z])|(?<=[a-zA-Z])(?=[0-9])|_");
            lastSegment = string.Join(" ", words.Select(w => char.ToUpper(w[0]) + w.Substring(1)));

            EditorGUILayout.LabelField($"Toggle Name:", lastSegment);
            paramiterName = lastSegment;
        }
        else
        {
            EditorGUILayout.LabelField($"Parameter Name:", "Undefined");
            EditorGUILayout.LabelField($"Toggle Name:", "Undefined");
        }

        EditorGUILayout.Space();

        avatarDescriptor = EditorGUILayout.ObjectField("VRC Avatar Descriptor", avatarDescriptor, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
        expressionsMenu = EditorGUILayout.ObjectField("Expressions Menu", expressionsMenu, typeof(VRCExpressionsMenu), true) as VRCExpressionsMenu;
        expressionParameters = EditorGUILayout.ObjectField("Expression Parameters", expressionParameters, typeof(VRCExpressionParameters), true) as VRCExpressionParameters;
        fxLayer = EditorGUILayout.ObjectField("FX Layer", fxLayer, typeof(RuntimeAnimatorController), true) as RuntimeAnimatorController;

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Expression Parameters Options", EditorStyles.boldLabel);
        isToggleEnabledDefault = EditorGUILayout.Toggle("Toggle Enabled Default", isToggleEnabledDefault);
        isToggleSaved = EditorGUILayout.Toggle("Toggle Saved", isToggleSaved);
        isToggleSynced = EditorGUILayout.Toggle("Toggle Synced", isToggleSynced);

        EditorGUILayout.Space();

        GUILayout.Label("Animation Options", EditorStyles.boldLabel);
        writeAnimatorDefaults = EditorGUILayout.Toggle("Write Animator Defaults", writeAnimatorDefaults);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Select save location and build animations"))
        {
            string[] errors = {
                "Please set the Avatar base object.",
                "Please set the target object to animate.",
                "Please set the Expressions Menu.",
                "Please set the Expressions Parameters.",
                "Please set the Avatar Descriptor.",
                "Please set the FX Layer."
            };

            if (avatar == null || objectToAnimate == null || expressionsMenu == null || expressionParameters == null || avatarDescriptor == null || fxLayer == null)
            {
                string errorMsg = "";
                for (int i = 0; i < errors.Length; i++)
                {
                    if (i > 0 && i < errors.Length && avatar != null && objectToAnimate != null &&
                        expressionsMenu != null && expressionParameters != null && avatarDescriptor != null && fxLayer != null)
                    {
                        break;
                    }
                    if (i > 0 && i < errors.Length && errorMsg.Length > 0)
                    {
                        errorMsg += "\n";
                    }
                    if ((i == 0 && avatar == null) || (i == 1 && objectToAnimate == null) ||
                        (i == 2 && expressionsMenu == null) || (i == 3 && expressionParameters == null) ||
                        (i == 4 && avatarDescriptor == null) || (i == 5 && fxLayer == null))
                    {
                        errorMsg += "- " + errors[i];
                    }
                }

                EditorUtility.DisplayDialog("Error", errorMsg, "OK");
                return;
            }

            CreateAnimations();
        }
    }

    private void CreateAnimations()
    {
        string folderPath = EditorUtility.SaveFolderPanel("Save Animation Clip", "", "");

        if (!folderPath.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Error", "The selected folder must be within the project's Assets folder.", "OK");
            return;
        }

        string relativeFolderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);

        // CREATE ANIMATIONS
        AnimationClip animationOn = CreateAnimation("on", 0, 1, 0, 0);
        AnimationClip animationOff = CreateAnimation("off", 0, 0, 0, 1);

        AssetDatabase.CreateAsset(animationOn, $"{relativeFolderPath}/{animationOn.name}");
        AssetDatabase.CreateAsset(animationOff, $"{relativeFolderPath}/{animationOff.name}");

        // CREATE PARAMS
        VRCExpressionParameters.Parameter newParameter = new VRCExpressionParameters.Parameter()
        {
            name = paramiterPath,
            valueType = VRCExpressionParameters.ValueType.Bool,
            saved = isToggleSaved,
            defaultValue = isToggleEnabledDefault ? 1f : 0f,
            networkSynced = isToggleSynced,
        };

        Array.Resize(ref expressionParameters.parameters, expressionParameters.parameters.Length + 1);
        expressionParameters.parameters[expressionParameters.parameters.Length - 1] = newParameter;

        // CREATE MENU
        VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control();
        control.name = paramiterName;
        control.icon = null;
        control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
        control.parameter = new VRCExpressionsMenu.Control.Parameter() { name = paramiterPath };

        expressionsMenu.controls.Add(control);

        // CREATE FX LAYER
        CreateAnimatorLayer(animationOn, animationOff);

        // SAVE
        EditorUtility.SetDirty(expressionsMenu);
        EditorUtility.SetDirty(expressionParameters);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Toggle Animation Generator", "All Done", "pog!");
    }

    private AnimationClip CreateAnimation(string name, float timeStart, float valueStart, float timeEnd, float valueEnd)
    {
        AnimationClip clip_on = new AnimationClip();
        clip_on.name = $"{paramiterName}_{name}.anim";

        AnimationCurve curve = AnimationCurve.Linear(timeStart, valueStart, timeEnd, valueEnd);
        EditorCurveBinding binding = new EditorCurveBinding();
        binding.path = AnimationUtility.CalculateTransformPath(objectToAnimate.transform, this.avatar.transform);
        binding.type = typeof(GameObject);
        binding.propertyName = "m_IsActive";
        AnimationUtility.SetEditorCurve(clip_on, binding, curve);

        return clip_on;
    }

    private void CreateAnimatorLayer(AnimationClip enabled, AnimationClip disabled)
    {
        // Load the controller
        RuntimeAnimatorController controller = fxLayer;
        AnimatorController animatorController = controller as AnimatorController;

        // Create a new layer
        AnimatorControllerLayer layer = new AnimatorControllerLayer();
        layer.name = paramiterPath;
        layer.defaultWeight = 1;

        AnimatorStateMachine statemachine = new AnimatorStateMachine();
        layer.stateMachine = statemachine;

        AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
        layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

        // create param
        AnimatorControllerParameter[] parameters = animatorController.parameters;
        AnimatorControllerParameter newParameter = new AnimatorControllerParameter();
        newParameter.name = paramiterPath;
        newParameter.type = AnimatorControllerParameterType.Bool;
        ArrayUtility.Add(ref parameters, newParameter);
        animatorController.parameters = parameters;

        // Create an enabled state
        AnimatorState enabledState = new AnimatorState();
        enabledState.name = "Enabled";
        enabledState.motion = enabled;
        enabledState.writeDefaultValues = writeAnimatorDefaults;

        // Create an disabled state
        AnimatorState disabledState = new AnimatorState();
        disabledState.name = "Disabled";
        disabledState.motion = disabled;
        disabledState.writeDefaultValues = writeAnimatorDefaults;

        // Add a transition from the entry enabled to the disabled state
        AnimatorStateTransition transition = new AnimatorStateTransition();
        transition.destinationState = disabledState;
        transition.duration = 0f;
        transition.exitTime = 0;
        transition.hasExitTime = false;

        enabledState.AddTransition(transition);
        AssetDatabase.AddObjectToAsset(transition, AssetDatabase.GetAssetPath(animatorController));
        transition.hideFlags = HideFlags.HideInHierarchy;

        transition.AddCondition(AnimatorConditionMode.IfNot, 1f, paramiterPath);

        // Add a transition from the entry disabled to the enabled state
        AnimatorStateTransition transition2 = new AnimatorStateTransition();
        transition2.destinationState = enabledState;
        transition2.duration = 0f;
        transition2.exitTime = 0;
        transition2.hasExitTime = false;

        disabledState.AddTransition(transition2);
        AssetDatabase.AddObjectToAsset(transition2, AssetDatabase.GetAssetPath(animatorController));
        transition2.hideFlags = HideFlags.HideInHierarchy;

        transition2.AddCondition(AnimatorConditionMode.If, 1f, paramiterPath);

        // Add the states to the state machine
        layer.stateMachine.AddState(enabledState, new Vector3(300, 10, 0));
        AssetDatabase.AddObjectToAsset(enabledState, AssetDatabase.GetAssetPath(animatorController));
        enabledState.hideFlags = HideFlags.HideInHierarchy;

        layer.stateMachine.AddState(disabledState, new Vector3(300, 110, 0));
        AssetDatabase.AddObjectToAsset(disabledState, AssetDatabase.GetAssetPath(animatorController));
        disabledState.hideFlags = HideFlags.HideInHierarchy;

        layer.stateMachine.defaultState = disabledState;

        // Add the layer to the controller
        animatorController.AddLayer(layer);
        EditorUtility.SetDirty(animatorController);
    }
}
