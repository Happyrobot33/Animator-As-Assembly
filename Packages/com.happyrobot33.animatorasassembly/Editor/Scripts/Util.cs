using AnimatorAsCode.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEditor.Animations;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using System.Numerics;
using AnimatorAsAssembly.Commands;

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
            // this will combine any combination of these objects into a single AacFlState array
            // AacFlState
            // AacFlState[]
            // OPCODE
            // OPCODE[]
            // any objects that derive from OPCODE will first need to be casted to OPCODE then to AacFlState[]

            //create a list to store the states
            List<AacFlState> states = new List<AacFlState>();

            //iterate through all the objects
            foreach (var obj in objects)
            {
                //if the object is a state
                if (obj is AacFlState)
                {
                    //add the state to the list
                    states.Add(obj as AacFlState);
                }
                //if the object is an array of states
                else if (obj is AacFlState[])
                {
                    //add all the states to the list
                    states.AddRange(obj as AacFlState[]);
                }
                //if the object is an opcode
                else if (obj is OPCODE)
                {
                    //add the opcode's states array to the list
                    states.AddRange((obj as OPCODE)?.States);
                }
                //if the object is an array of opcodes
                else if (obj is OPCODE[])
                {
                    //add all the opcode's states to the list
                    foreach (var opcode in obj as OPCODE[])
                    {
                        states.AddRange(opcode.States);
                    }
                }
                //if the object is anything else
                else
                {
                    //throw an error
                    throw new Exception("Invalid object type");
                }
            }
            return states.ToArray();
        }

        /// <summary> Reverses a string </summary>
        /// <param name="s">The string to reverse</param>
        /// <returns>The reversed string</returns>
        public static string ReverseString(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static void EmptyController(AnimatorController controller)
        {
            //completely empty the controller except for the base information
            AnimatorController newController = new AnimatorController();

            //overwrite the asset
            EditorUtility.CopySerialized(newController, controller);

            //save the asset
            AssetDatabase.SaveAssets();
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
                ProgressBar ourProgressBar = progressBar.RegisterNewProgressBar(
                    "Clearing Asset",
                    "Cleaning the Animator Controller Asset"
                );

                //get the animator controller
                AnimatorController controller = asset as AnimatorController;

                //remove all parameters from the controller
                foreach (var parameter in controller.parameters)
                {
                    controller.RemoveParameter(parameter);
                }

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
                    ourProgressBar.SetProgress((float)i / subAssets.Length);
                    yield return null;
                }

                ourProgressBar.Finish();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path);
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
        public static AacFlBoolParameter _False;

        /// <summary> A permanent reference to a true boolean value </summary>
        public static AacFlBoolParameter _True;

        /// <summary> A permanent reference to the number 1 in Register form </summary>
        public static Register _One;

        /// <summary> The current program counter </summary>
        public static AacFlIntParameter _ProgramCounter;
        public const string PROGRAMCOUNTERSTRING = "INTERNAL/PC";

        /// <summary> The current clock. Incrementes with each state </summary>
        public static AacFlIntParameter _Clock;
        public const string CLOCKSTRING = "INTERNAL/CLOCK";

        /// <summary> How many elements the stack can hold </summary>
        public static int _StackSize = 10;
        /// <summary> The stack parameters </summary>
        public static AacFlIntParameter[] _Stack;
        public static AacFlIntParameter[] _StackBuffer;
        public const string STACKSTRINGPREFIX = "INTERNAL/STACK/";

        public static AacFlFloatParameter[] _PixelBuffer;
        public static Vector2Int _PixelBufferSize = new Vector2Int(0, 0);
        public const string PIXELBUFFERSTRINGPREFIX = "INTERNAL/GPU/";

        /// <summary> Create a new Globals object </summary>
        /// <param name="Layer">The AacFlLayer to use</param>
        public Globals(AacFlLayer Layer)
        {
            _False = Layer.BoolParameter("CONSTANT/FALSE");
            _True = Layer.BoolParameter("CONSTANT/TRUE");
            Layer.OverrideValue(_True, true);
            _One = new Register("CONSTANT/ONE", Layer);
            _One.Initialize(1);
            _ProgramCounter = Layer.IntParameter(PROGRAMCOUNTERSTRING);
            CreateStack(Layer);
            _Clock = Layer.IntParameter(CLOCKSTRING);
        }

        /// <summary> Create a new stack </summary>
        /// <remarks> All values in the stack are initialized to -1 </remarks>
        private void CreateStack(AacFlLayer Layer)
        {
            AacFlIntParameter[] stack = new AacFlIntParameter[_StackSize];
            AacFlIntParameter[] stackBuffer = new AacFlIntParameter[_StackSize];
            for (int i = 0; i < _StackSize; i++)
            {
                stack[i] = Layer.IntParameter(STACKSTRINGPREFIX + "REAL/" + i);
                stackBuffer[i] = Layer.IntParameter(STACKSTRINGPREFIX + "BUFFER/" + i);
                Layer.OverrideValue(stack[i], -1);
                Layer.OverrideValue(stackBuffer[i], -1);
            }
            _Stack = stack;
            _StackBuffer = stackBuffer;
        }

        /// <summary> Set the global stack size </summary>
        /// <param name="size">The new stack size</param>
        public static void SetStackSize(int size)
        {
            _StackSize = size;
        }
    }
}
