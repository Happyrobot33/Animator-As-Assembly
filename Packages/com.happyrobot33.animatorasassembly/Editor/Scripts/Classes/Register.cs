using System;
using System.Collections.Generic;
using System.ComponentModel;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AnimatorAsAssembly
{
    /// <summary> A register is a named parameter that can be used to store a value </summary>
    public class Register
    {
        /// <summary> The internal AAC boolean parameters that this register is linked to </summary>
        public AacFlBoolParameter[] boolParams;

        /// <summary> The name of this register </summary>
        public string Name;

        /// <summary> The bit count per register </summary>
        //store in editor prefs
        public static int BitDepth
        {
            get { return EditorPrefs.GetInt("AAA_BIT_DEPTH", 8); }
            set { EditorPrefs.SetInt("AAA_BIT_DEPTH", value); }
        }

        /// <summary> The FX controller that this register is linked to </summary>
        public AacFlLayer FX;

        /// <summary> Create a new register </summary>
        /// <param name="name"> The name of the register to create </param>
        /// <param name="FX"> The layer that this register is linked to </param>
        public Register(string name, AacFlLayer FX)
        {
            this.Name = name;
            this.FX = FX;
            this.boolParams = GenerateRegisterNames(name);
        }

        /// <summary> Define the bit depth of the registers </summary>
        /// <param name="bitDepth"> The bit depth of the registers </param>
        public static void SetBitDepth(int bitDepth)
        {
            BitDepth = bitDepth;
        }

        /// <summary> Get the individual bits of this register </summary>
        /// <param name="i"> The index of the bit to get </param>
        /// <returns> The AacFlBoolParameter that represents the bit at the given index </returns>
        public AacFlBoolParameter this[int i]
        {
            get { return boolParams[i]; }
        }

        /// <summary> Create a Enumerater for this register </summary>
        /// <returns> An Enumerator for this register </returns>
        public IEnumerator<AacFlBoolParameter> GetEnumerator()
        {
            return ((IEnumerable<AacFlBoolParameter>)boolParams).GetEnumerator();
        }

        /// <summary> Provide a quick way to get a parameter driver set to a specific value </summary>
        public IEnumerator<EditorCoroutine> Set(AacFlState state, int value, ProgressBar progressCallback = null)
        {
            CheckOverflow(value);
            for (int i = 0; i < BitDepth; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    state.Drives(boolParams[i], true);
                }
                else
                {
                    state.Drives(boolParams[i], false);
                }
                if (progressCallback != null)
                {
                    yield return progressCallback.SetProgress((float)i / BitDepth);
                }
            }
        }

        /// <summary> Set the value of this register upon bootup </summary>
        public void Initialize(int value)
        {
            CheckOverflow(value);
            for (int i = 0; i < BitDepth; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    FX.OverrideValue(boolParams[i], true);
                }
                else
                {
                    FX.OverrideValue(boolParams[i], false);
                }
            }
        }

        /// <summary> Check to see if the given value will overflow the register </summary>
        /// <param name="value"> The value to check </param>
        /// <throws> An exception if the value will overflow the register </throws>
        void CheckOverflow(int value)
        {
            int max = (int)Math.Pow(2, BitDepth);
            if (value >= max)
            {
                Debug.LogWarning(IntegerOverflowException(value, max));
            }
        }

        /// <summary> Create an exception for an integer overflow </summary>
        internal string IntegerOverflowException(int value, int max)
        {
            string originalBinary = Convert.ToString(value, 2);
            string truncatedBinary = Convert.ToString(Truncate(value), 2);

            //add spaces to the truncated binary so that it lines up with the original binary
            int difference = originalBinary.Length - truncatedBinary.Length;
            string gapFill = "";
            for (int i = 0; i < difference; i++)
            {
                gapFill += "?";
            }
            truncatedBinary = gapFill + truncatedBinary;

            return String.Format("The value {0} is too large for a register of bit depth {1}. The maximum value is {2}. The value will be truncated to {3}.\n{4}\n{5}", value, BitDepth, max, Truncate(value), originalBinary, truncatedBinary);
        }

        /// <summary> Truncate the given value to the bit depth of this register </summary>
        /// <param name="value"> The value to truncate </param>
        /// <returns> The truncated value </returns>
        private int Truncate(int value)
        {
            //return what the value would be if the extra bits were dropped
            return value & ((int)Math.Pow(2, BitDepth) - 1);
        }

        AacFlBoolParameter[] GenerateRegisterNames(string name)
        {
            string[] names = new string[BitDepth];
            for (int i = 0; i < BitDepth; i++)
            {
                names[i] = name + "_" + i;
            }

            AacFlBoolParameter[] boolParams = new AacFlBoolParameter[BitDepth];
            for (int i = 0; i < BitDepth; i++)
            {
                boolParams[i] = FX.BoolParameter(names[i]);
            }
            return boolParams;
        }

        /// <summary> Find a register in an array of registers </summary>
        /// <param name="name"> The name of the register to find </param>
        /// <param name="registers"> The array of registers to search through </param>
        /// <returns> The register with the given name, or null if no register with that name exists </returns>
        public static Register FindRegisterInArray(string name, Register[] registers)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FindRegisterInArray");
            for (int i = 0; i < registers.Length; i++)
            {
                if (registers[i] == null)
                {
                    continue;
                }

                if (registers[i].Name == name)
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                    return registers[i];
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return null;
        }
    }
}
