using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class FLIP
    {
        public AacFlState[] states;
        public Register A;
        public AacFlState entry;

        /// <summary> Bitwise flips a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public FLIP(Register A, AacFlLayer FX)
        {
            this.A = A;
            states = STATES(A, FX);
        }

        AacFlState[] STATES(Register A, AacFlLayer FX)
        {
            entry = FX.NewState("FLIP");
            for (int i = 0; i < Register.bits; i++)
            {
                entry.DrivingRemaps(A[i], 0f, 1f, A[i], 1f, 0f);
            }
            return new AacFlState[] { entry };
        }
    }
}
