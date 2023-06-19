using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class MOV : OPCODE
    {
        public Register A;
        public Register B;

        /// <summary> Moves a register to another register </summary>
        /// <param name="A"> The register to copy </param>
        /// <param name="B"> The register to copy to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public MOV(Register A, Register B, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //entry state
            AacFlState entry = Layer.NewState("MOV");
            AacFlState exit = entry;

            for (int i = 0; i < Register.bits; i++)
            {
                entry.DrivingCopies(A[i], B[i]);
            }
            return new AacFlState[] { entry };
        }
    }
}
