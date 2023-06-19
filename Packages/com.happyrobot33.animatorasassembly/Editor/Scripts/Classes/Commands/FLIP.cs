using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class FLIP : OPCODE
    {
        public Register A;

        /// <summary> Bitwise flips a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public FLIP(Register A, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            AacFlState entry = Layer.NewState("FLIP");
            for (int i = 0; i < Register.bits; i++)
            {
                entry.DrivingRemaps(A[i], 0f, 1f, A[i], 1f, 0f);
            }
            return new AacFlState[] { entry };
        }
    }
}
