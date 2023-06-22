using AnimatorAsCode.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEditor.Animations;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

namespace AnimatorAsAssembly
{
    public static class Util
    {
        //thank god for chatGPT for this one
        /// <summary> Concatenates states into one single AacFlState array </summary>
        /// <param name="objects">The arrays/AacFlState's to concatenate. Both are accepted</param>
        /// <returns>The concatenated array</returns>
        public static AacFlState[] CombineStates(params object[] objects)
        {
            Profiler.BeginSample("Combine States");
            object[][] arrays = objects
                .Select(x => x is object[] ? (object[])x : new object[] { x })
                .ToArray();
            Profiler.EndSample();
            return arrays.Aggregate((a, b) => a.Concat(b).ToArray()).Cast<AacFlState>().ToArray();
        }

        /// <summary> Cleans a animator controller by removing all unreferenced sub assets </summary>
        public static IEnumerator<EditorCoroutine> CleanAnimatorControllerAsset(
            string path,
            ComplexProgressBar progressBar
        )
        {
            try
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AssetDatabase.StartAssetEditing();
                /* EditorUtility.DisplayProgressBar("Clearing Asset", "Clearing Asset", 0.1f); */
                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

                //add a progress bar
                ProgressBar ourProgressBar = progressBar.registerNewProgressBar(
                    "Clearing Asset",
                    "Cleaning the Animator Controller Asset"
                );

                //get the animator controller
                AnimatorController controller = asset as AnimatorController;

                for (int i = 0; i < subAssets.Length; i++)
                {
                    /* EditorUtility.DisplayProgressBar(
                        "Clearing Asset",
                        "Clearing Asset",
                        (float)i / subAssets.Length
                    ); */
                    if (subAssets[i] == asset)
                        continue;
                    if (subAssets[i] is AnimatorStateMachine)
                    {
                        //determine if the state machine is used
                        bool used = false;
                        foreach (var layer in controller.layers)
                        {
                            if (layer.stateMachine == subAssets[i])
                            {
                                used = true;
                                break;
                            }
                        }
                        if (!used)
                        {
                            AssetDatabase.RemoveObjectFromAsset(subAssets[i]);
                            //Debug.Log(subAssets[i].name);
                        }
                    }
                    else if (subAssets[i] is AnimatorState)
                    {
                        //determine if the state is used
                        bool used = false;
                        foreach (var layer in controller.layers)
                        {
                            foreach (var state in layer.stateMachine.states)
                            {
                                if (state.state == subAssets[i])
                                {
                                    used = true;
                                    break;
                                }
                            }
                            if (used)
                                break;
                        }
                        if (!used)
                        {
                            AssetDatabase.RemoveObjectFromAsset(subAssets[i]);
                            //Debug.Log(subAssets[i].name);
                        }
                    }
                    else if (
                        subAssets[i] is AnimatorTransition
                        || subAssets[i] is AnimatorStateTransition
                    )
                    {
                        //determine if the transition is used
                        bool used = false;
                        foreach (var layer in controller.layers)
                        {
                            foreach (var state in layer.stateMachine.states)
                            {
                                foreach (var transition in state.state.transitions)
                                {
                                    if (transition == subAssets[i])
                                    {
                                        used = true;
                                        break;
                                    }
                                }
                                if (used)
                                    break;
                            }
                            if (used)
                                break;
                        }
                        if (!used)
                        {
                            AssetDatabase.RemoveObjectFromAsset(subAssets[i]);
                            //Debug.Log(subAssets[i].name);
                        }
                    }
                    else if (subAssets[i] is StateMachineBehaviour)
                    {
                        //determine if the state machine behaviour is used
                        bool used = false;
                        foreach (var layer in controller.layers)
                        {
                            foreach (var state in layer.stateMachine.states)
                            {
                                foreach (var behaviour in state.state.behaviours)
                                {
                                    if (behaviour == subAssets[i])
                                    {
                                        used = true;
                                        break;
                                    }
                                }
                                if (used)
                                    break;
                            }
                            if (used)
                                break;
                        }
                        if (!used)
                        {
                            AssetDatabase.RemoveObjectFromAsset(subAssets[i]);
                            //Debug.Log(subAssets[i].name);
                        }
                    }
                    else if (subAssets[i] is Motion)
                    {
                        //determine if the motion is used
                        bool used = false;
                        foreach (var layer in controller.layers)
                        {
                            foreach (var state in layer.stateMachine.states)
                            {
                                if (state.state.motion == subAssets[i])
                                {
                                    used = true;
                                    break;
                                }
                            }
                            if (used)
                                break;
                        }
                        if (!used)
                        {
                            AssetDatabase.RemoveObjectFromAsset(subAssets[i]);
                            //Debug.Log(subAssets[i].name);
                        }
                    }
                    ourProgressBar.setProgress((float)i / subAssets.Length);
                    yield return null;
                }

                ourProgressBar.finish();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                /* EditorUtility.ClearProgressBar(); */
            }
            yield break;
        }
    }

    /// <summary> This class contains all the global const variables used in the controller </summary>
    public class Globals
    {
        /// <summary> A permanent reference to a false boolean value </summary>
        public static AacFlBoolParameter FALSE;

        /// <summary> A permanent reference to a true boolean value </summary>
        public static AacFlBoolParameter TRUE;

        /// <summary> A permanent reference to the number 1 in Register form </summary>
        public static Register ONE;

        /// <summary> A permanent reference to the program counter variable </summary>
        public static AacFlIntParameter PROGRAMCOUNTER;
        public const string PROGRAMCOUNTERSTRING = "INTERNAL/PC";

        /// <summary> Create a new Globals object </summary>
        /// <param name="FX">The AacFlLayer to use</param>
        public Globals(AacFlLayer FX)
        {
            FALSE = FX.BoolParameter("GLOBALS/FALSE");
            TRUE = FX.BoolParameter("GLOBALS/TRUE");
            FX.OverrideValue(TRUE, true);
            ONE = new Register("GLOBALS/ONE", FX);
            ONE.initialize(1);
            PROGRAMCOUNTER = FX.IntParameter(PROGRAMCOUNTERSTRING);
        }
    }
}
