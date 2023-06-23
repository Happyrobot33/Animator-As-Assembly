using System.Collections.Generic;
using AnimatorAsCode.Framework;

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
        public static int _bitDepth = 8;

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
        public void Set(AacFlState state, int value)
        {
            for (int i = 0; i < _bitDepth; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    state.Drives(boolParams[i], true);
                }
                else
                {
                    state.Drives(boolParams[i], false);
                }
            }
        }

        /// <summary> Set the value of this register upon bootup </summary>
        public void Initialize(int value)
        {
            for (int i = 0; i < _bitDepth; i++)
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

        AacFlBoolParameter[] GenerateRegisterNames(string name)
        {
            string[] names = new string[_bitDepth];
            for (int i = 0; i < _bitDepth; i++)
            {
                names[i] = name + "_" + i;
            }

            AacFlBoolParameter[] boolParams = new AacFlBoolParameter[_bitDepth];
            for (int i = 0; i < _bitDepth; i++)
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
