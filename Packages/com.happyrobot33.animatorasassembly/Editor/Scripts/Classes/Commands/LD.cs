using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class LD : OPCODE
    {
        public Register A;
        public int value;

        /// <summary> Loads a register with a int value </summary>
        /// <param name="A"> The register to load into </param>
        /// <param name="value"> The value to load into the register </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LD(Register A, int value, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer;
            //truncate the value to fit in the register's bit count
            this.value = value & ((1 << Register.bits) - 1);
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("LD");
            AacFlState entry = Layer.NewState("LD");
            for (int i = 0; i < Register.bits; i++)
            {
                bool bitValue = (value & (1 << i)) != 0;
                entry.Drives(A[i], bitValue);
            }
            Profiler.EndSample();
            return new AacFlState[] { entry };
        }
    }
}
