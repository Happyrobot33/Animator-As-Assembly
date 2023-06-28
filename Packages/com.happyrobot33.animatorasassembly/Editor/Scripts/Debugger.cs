using AnimatorAsCode.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
using System.Collections.Generic;
using Lyuma.Av3Emulator.Runtime;

//the point of this script is to show the binary versions of the registers as something human readable

namespace AnimatorAsAssembly
{
    public struct SimpleRegister
    {
        public string name;
        public string[] bits;
        public bool expanded;
    }

    public class Debugger : MonoBehaviour
    {
        public List<SimpleRegister> registersList = new List<SimpleRegister>();
        [HideInInspector]
        public List<string> profilers = new List<string>();
        AnimatorController ac;

        [HideInInspector]
        public LyumaAv3Runtime runtime;

        [HideInInspector]
        public AnimatorAsAssembly aaa;

        public void Start()
        {
            aaa = GetComponent<AnimatorAsAssembly>();
            ac = aaa.assetContainer;
            runtime = aaa.avatar.GetComponentInParent<LyumaAv3Runtime>();

            //make a register name list
            List<string> registerNames = new List<string>();

            //print all parameters
            foreach (var parameter in ac.parameters)
            {
                //check if it ends like a register bit does
                //registers end with _* where * is a number

                //get the last number
                string[] split = parameter.name.Split('_');
                if (split.Length > 1)
                {
                    string last = split[split.Length - 1];
                    bool isNumber = int.TryParse(last, out int _);
                    if (isNumber)
                    {
                        //if it is a number, then it is a register bit
                        //print the register name
                        string registerName = parameter.name.Substring(
                            0,
                            parameter.name.Length - last.Length - 1
                        );
                        //ignore any CONSTANT or INTERNAL registers
                        if (
                            registerName.StartsWith("CONSTANT/")
                            || registerName.StartsWith("INTERNAL/")
                        )
                        {
                            continue;
                        }
                        //add the register name to the list if it isn't already there
                        if (!registerNames.Contains(registerName))
                        {
                            registerNames.Add(registerName);
                        }
                    }
                }

                //add profiler data
                if (parameter.name.StartsWith("PROFILING/"))
                {
                    //only add the PROFILING/NAME part
                    string profilerName = parameter.name.Split('/')[1];
                    if (!profilers.Contains("PROFILING/" + profilerName))
                        profilers.Add("PROFILING/" + profilerName);
                }
            }

            Debug.Log(registerNames.Count + " registers found");

            //convert them to normal registers
            registersList = new List<SimpleRegister>();
            foreach (string registerName in registerNames)
            {
                Debug.Log("Register: " + registerName);
                //get the register
                SimpleRegister register = new SimpleRegister()
                {
                    name = registerName,
                    bits = new string[Register.BitDepth],
                    expanded = false
                };
                for (int i = 0; i < Register.BitDepth; i++)
                {
                    register.bits[i] = registerName + "_" + i;
                }
                //add it to the list
                registersList.Add(register);
            }
        }
    }

    //inspector
    [CustomEditor(typeof(Debugger))]
    public class DebuggerEditor : Editor
    {
        bool callStackShown = true;
        bool profilingShown = true;

        //keep track of the last time the clock was updated and the last clock value
        float lastClockTime = 0;
        int lastClockValue = 0;
        float HZ = 0;

        LyumaAv3Runtime.Av3EmuParameterAccess PCparam;
        LyumaAv3Runtime.Av3EmuParameterAccess ClockParam;

        //create a on start method
        public void OnEnable()
        {
            //get the debugger
            Debugger debugger = (Debugger)target;
            //get the program counter
            PCparam =
                new LyumaAv3Runtime.Av3EmuParameterAccess()
                {
                    runtime = debugger.runtime,
                    paramName = Globals.PROGRAMCOUNTERSTRING
                };

            ClockParam =
                    new LyumaAv3Runtime.Av3EmuParameterAccess()
                    {
                        runtime = debugger.runtime,
                        paramName = Globals.CLOCKSTRING
                    };
        }

        Debugger debugger = null;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            debugger = (Debugger)target;

            //detect if in play mode
            if (Application.isPlaying)
            {
                //repaint if not paused
                /* if (!EditorApplication.isPaused)
                {
                    //repaint just self
                    Repaint();
                } */
                #region current executing instruction
                //get the program counter
                int PC = PCparam.intVal;
                EditorGUILayout.LabelField("PC: " + PC);
                if (PC == 0)
                {
                    EditorGUILayout.LabelField("At entry vector");
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Current instruction: " + debugger.aaa.instructionStringList[PC - 1]
                    );
                }
                #endregion

                #region total clock
                int CLOCK = ClockParam.intVal;
                EditorGUILayout.LabelField("Clock Cycle: " + CLOCK);

                //estimate the HZ of the processor using the clock
                if (lastClockValue != CLOCK)
                {
                    //get the time difference
                    float timeDifference = Time.time - lastClockTime;
                    //get the clock difference
                    int clockDifference = CLOCK - lastClockValue;
                    //calculate the HZ
                    HZ = (float)clockDifference / timeDifference;
                    //round it to 2 decimal places
                    HZ = Mathf.Round(HZ * 100) / 100;

                    //update the last clock time
                    lastClockTime = Time.time;
                }
                EditorGUILayout.LabelField("Estimated HZ: " + HZ);
                //compare to a C64 6510 CPU
                EditorGUILayout.LabelField("C64 6510 HZ (NTSC): 1.023 MHz");
                const float C64HZ = 1023000;
                float C64HZDifference = Mathf.Abs(C64HZ - HZ);
                float slowerByPercent = (1 - (C64HZDifference / C64HZ)) * 100;
                EditorGUILayout.LabelField(
                    "Slower by " + C64HZDifference + " HZ (" + slowerByPercent + "%)"
                );

                lastClockValue = CLOCK;
                #endregion

                #region time stepping
                EditorGUILayout.BeginHorizontal();
                //create a button to pause execution
                if (GUILayout.Button("Pause"))
                {
                    EditorApplication.isPaused = true;
                }
                //create a button to step forward
                if (GUILayout.Button("Step forward"))
                {
                    //we need to step forward till the program counter changes
                    //timeout after a set ammount of steps
                    int oldPC = PC;
                    int timeout = 1000;
                    while (oldPC == PCparam.intVal && timeout > 0)
                    {
                        EditorApplication.Step();
                        //repaint
                        //Repaint();
                        timeout--;
                    }
                }
                //create a button to resume execution
                if (GUILayout.Button("Resume"))
                {
                    EditorApplication.isPaused = false;
                }
                EditorGUILayout.EndHorizontal();
                #endregion

                #region Display Emulation
                // emulate the on-avatar display
                // create a x y grid of boxes
                const int pixelSize = 12;
                EditorGUILayout.BeginVertical(GUILayout.Height(pixelSize));
                for (int y = 0; y < debugger.aaa.displayHeight; y++)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(pixelSize * debugger.aaa.displayWidth));
                    for (int x = 0; x < debugger.aaa.displayWidth; x++)
                    {
                        //create a filled box
                        Color color = new Color();
                        //get if the pixel bool is on
                        LyumaAv3Runtime.Av3EmuParameterAccess pixelBool =
                            new LyumaAv3Runtime.Av3EmuParameterAccess()
                            {
                                runtime = debugger.runtime,
                                paramName = Globals.PIXELBUFFERSTRINGPREFIX + x + "," + y
                            };
                        color.r = pixelBool.boolVal ? 1 : 0;
                        color.g = color.r;
                        color.b = color.r;
                        color.a = 1;
                        //draw the box
                        EditorGUI.DrawRect(
                            GUILayoutUtility.GetRect(pixelSize, pixelSize),
                            color
                        );
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                #endregion

                #region call stack
                callStackShown = EditorGUILayout.BeginFoldoutHeaderGroup(
                    callStackShown,
                    "Call stack"
                );
                if (callStackShown)
                {
                    EditorGUI.indentLevel++;
                    //get the stack params
                    LyumaAv3Runtime.Av3EmuParameterAccess[] stackParams =
                        new LyumaAv3Runtime.Av3EmuParameterAccess[Globals.StackSize];
                    int usedStackPositions = 0;
                    for (int i = 0; i < Globals.StackSize; i++)
                    {
                        stackParams[i] = GetParam(Globals.STACKSTRINGPREFIX + "REAL/" + i);
                        if (stackParams[i].intVal != -1)
                        {
                            usedStackPositions++;
                        }
                    }
                    //display a label for remaining space on the stack
                    EditorGUILayout.LabelField(
                        "Stack space remaining: " + (Globals.StackSize - usedStackPositions) + "/" + Globals.StackSize, EditorStyles.miniBoldLabel
                    );

                    //display the stack raw
                    foreach (var stackParam in stackParams)
                    {
                        if (stackParam.intVal != -1)
                        {
                            //resolve the name from the PC value
                            string name = debugger.aaa.instructionStringList[stackParam.intVal];
                            EditorGUILayout.LabelField(name);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                #endregion

                #region Profiling
                profilingShown = EditorGUILayout.BeginFoldoutHeaderGroup(
                    profilingShown,
                    "Profiling"
                );
                if (profilingShown)
                {
                    EditorGUI.indentLevel++;
                    //loop through each profiling object
                    foreach (string profilingObject in debugger.profilers)
                    {
                        //get both start and stop clock time
                        int startClock = GetParam(profilingObject + "/START").intVal;
                        int stopClock = GetParam(profilingObject + "/STOP").intVal;
                        string name = profilingObject.Split('/')[1];
                        int value = stopClock - startClock;
                        float seconds = (float)value / HZ;
                        //round the seconds to 2 decimal places
                        seconds = Mathf.Round(seconds * 100) / 100;
                        if (value > 0)
                        {
                            EditorGUILayout.LabelField(
                                name + ": " + (stopClock - startClock) + " cycles (" + seconds + " seconds)"
                            );
                        }
                        else
                        {
                            //check to see if it has begun
                            if (startClock == 0)
                            {
                                //not started yet
                                EditorGUILayout.LabelField(
                                    name + ": not started"
                                );
                            }
                            else
                            {
                                //assume the stop clock is the current clock value
                                value = CLOCK - startClock;
                                EditorGUILayout.LabelField(
                                    name + ": " + value + " cycles (still running)"
                                );
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                #endregion

                #region registers
                //create horizontal divider
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                //show the registers
                for (int i = 0; i < debugger.registersList.Count; i++)
                {
                    SimpleRegister register = debugger.registersList[i];

                    register.expanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                        register.expanded,
                        register.name
                    );
                    //update the register
                    debugger.registersList[i] = register;

                    if (register.expanded)
                    {
                        EditorGUI.indentLevel++;

                        #region get the register bits
                        //get the register values as bools
                        bool[] registerValues = new bool[Register.BitDepth];
                        for (int j = 0; j < Register.BitDepth; j++)
                        {
                            registerValues[j] = GetParam(register.bits[j]).boolVal;
                        }
                        #endregion

                        #region display decimal
                        //show the register values in decimal both int and uint
                        int decimalValue_uint = 0;
                        for (int j = 0; j < Register.BitDepth; j++)
                        {
                            if (registerValues[j])
                            {
                                decimalValue_uint += (int)Mathf.Pow(2, j);
                            }
                        }
                        int decimalValue_int = decimalValue_uint;
                        if (decimalValue_int > Mathf.Pow(2, Register.BitDepth - 1))
                        {
                            decimalValue_int -= (int)Mathf.Pow(2, Register.BitDepth);
                        }
                        EditorGUILayout.LabelField("Decimal (uint): " + decimalValue_uint);
                        EditorGUILayout.LabelField("Decimal (int): " + decimalValue_int);
                        #endregion

                        #region display hex
                        //show the register values in hex
                        string hexValue = decimalValue_uint.ToString("X");
                        EditorGUILayout.LabelField("Hex: " + hexValue);
                        #endregion

                        #region display binary
                        //show the register values in binary
                        EditorGUILayout.BeginHorizontal();
                        GUIContent label = new GUIContent("Binary:");
                        EditorGUILayout.LabelField(label);
                        for (int j = Register.BitDepth - 1; j >= 0; j--)
                        {
                            registerValues[j] = EditorGUILayout.Toggle(registerValues[j]);
                        }
                        EditorGUILayout.EndHorizontal();
                        #endregion

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                #endregion
            }
            else
            {
                base.OnInspectorGUI();
                EditorGUILayout.LabelField(
                    "You need to be in play mode for this script to do anything!"
                );
            }
        }
        private LyumaAv3Runtime.Av3EmuParameterAccess GetParam(string paramName)
        {
            return new LyumaAv3Runtime.Av3EmuParameterAccess()
            {
                runtime = debugger.runtime,
                paramName = paramName
            };
        }
    }
}
