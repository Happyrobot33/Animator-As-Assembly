using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class LD : OPCODE
    {
        public Register A;
        public int value;

        /// <summary> Loads a register with a int value </summary>
        /// <param name="A"> The register to load into </param>
        /// <param name="value"> The value to load into the register </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public LD(Register A, int value, AacFlLayer FX)
        {
            this.A = A;
            //truncate the value to fit in the register's bit count
            this.value = value & ((1 << Register.bits) - 1);
            states = STATES(A, this.value, FX);
        }

        AacFlState[] STATES(Register A, int value, AacFlLayer FX)
        {
            AacFlState entry = FX.NewState("LD");
            for (int i = 0; i < Register.bits; i++)
            {
                bool bitValue = (value & (1 << i)) != 0;
                entry.Drives(A[i], bitValue);
            }
            return new AacFlState[] { entry };
        }
    }
}
