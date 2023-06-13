using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using static AnimatorAsAssembly.Globals;

namespace AnimatorAsAssembly.Commands
{
    public class MOV
    {
        public AacFlState[] states;
        public Register A;
        public Register B;

        /// <summary> Moves a register to another register </summary>
        /// <param name="A"> The register to copy </param>
        /// <param name="B"> The register to copy to </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public MOV(Register A, Register B, AacFlLayer FX)
        {
            this.A = A;
            this.B = B;
            states = STATES(A, B, FX);
        }

        AacFlState[] STATES(Register A, Register B, AacFlLayer FX)
        {
            //globals
            Globals globals = new Globals(FX);

            //entry state
            AacFlState entry = FX.NewState("MOV");

            for (int i = 0; i < Register.bits; i++)
            {
                entry.DrivingCopies(A[i], B[i]);
            }
            return new AacFlState[] { entry };
        }
    }
}
