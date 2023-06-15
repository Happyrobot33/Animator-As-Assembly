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
    public struct simpleRegister
    {
        public string name;
        public string[] bits;
        public bool expanded;
    }

    public class Debugger : MonoBehaviour
    {
        public List<simpleRegister> registersList = new List<simpleRegister>();
        AnimatorController ac;

        [HideInInspector]
        public LyumaAv3Runtime runtime;
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
                    bool isNumber = int.TryParse(last, out int result);
                    if (isNumber)
                    {
                        //if it is a number, then it is a register bit
                        //print the register name
                        string registerName = parameter.name.Substring(
                            0,
                            parameter.name.Length - last.Length - 1
                        );
                        //ignore any GLOBALS or INTERNAL registers
                        if (
                            registerName.StartsWith("GLOBALS/")
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
            }

            Debug.Log(registerNames.Count + " registers found");

            //convert them to normal registers
            registersList = new List<simpleRegister>();
            foreach (string registerName in registerNames)
            {
                Debug.Log("Register: " + registerName);
                //get the register
                simpleRegister register = new simpleRegister();
                register.name = registerName;
                register.bits = new string[Register.bits];
                register.expanded = false;
                for (int i = 0; i < Register.bits; i++)
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
        public override void OnInspectorGUI()
        {
            Debugger debugger = (Debugger)target;

            //detect if in play mode
            if (Application.isPlaying)
            {
                #region current executing instruction
                //get the program counter
                LyumaAv3Runtime.Av3EmuParameterAccess PCparam =
                    new LyumaAv3Runtime.Av3EmuParameterAccess();
                PCparam.runtime = debugger.runtime;
                PCparam.paramName = "INTERNAL/PC";
                int PC = PCparam.intVal;
                EditorGUILayout.LabelField("PC: " + PC);
                EditorGUILayout.LabelField(
                    "Current instruction: " + debugger.aaa.instructionStringList[PC - 1]
                );
                #endregion

                //show the registers
                for (int i = 0; i < debugger.registersList.Count; i++)
                {
                    simpleRegister register = debugger.registersList[i];

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
                        bool[] registerValues = new bool[Register.bits];
                        for (int j = 0; j < Register.bits; j++)
                        {
                            LyumaAv3Runtime.Av3EmuParameterAccess paramAccess =
                                new LyumaAv3Runtime.Av3EmuParameterAccess();
                            paramAccess.runtime = debugger.runtime;
                            paramAccess.paramName = register.bits[j];
                            registerValues[j] = paramAccess.boolVal;
                        }
                        #endregion

                        #region display decimal
                        //show the register values in decimal
                        int decimalValue = 0;
                        for (int j = 0; j < Register.bits; j++)
                        {
                            if (registerValues[j])
                            {
                                decimalValue += (int)Mathf.Pow(2, j);
                            }
                        }
                        EditorGUILayout.LabelField("Decimal: " + decimalValue);
                        #endregion

                        #region display hex
                        //show the register values in hex
                        string hexValue = decimalValue.ToString("X");
                        EditorGUILayout.LabelField("Hex: " + hexValue);
                        #endregion

                        #region display binary
                        //show the register values in binary
                        EditorGUILayout.BeginHorizontal();
                        GUIContent label = new GUIContent("Binary:");
                        EditorGUILayout.LabelField(label);
                        for (int j = Register.bits - 1; j >= 0; j--)
                        {
                            registerValues[j] = EditorGUILayout.Toggle(registerValues[j]);
                        }
                        EditorGUILayout.EndHorizontal();
                        #endregion


                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
            else
            {
                base.OnInspectorGUI();
                EditorGUILayout.LabelField(
                    "You need to be in play mode for this script to do anything!"
                );
            }
        }
    }
}
