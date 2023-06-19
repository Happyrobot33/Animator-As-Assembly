using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class SHL : OPCODE
    {
        public Register A;
        Register BUFFER;

        /// <summary> Shifts a Registers bits 1 to the left</summary>
        /// <param name="A"> The register to shift </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SHL(Register A, AacFlLayer Layer, int shift = 1)
        {
            this.A = A;
            this.Layer = Layer;
            this.BUFFER = new Register("INTERNAL/SHL/BUFFER", Layer);
            states = STATES(shift);
        }

        AacFlState[] STATES(int shift)
        {
            //copy from A to BUFFER
            MOV mov = new MOV(A, BUFFER, Layer);

            //copy them back
            AacFlState emptyBuffer = Layer.NewState("SHL");
            for (int i = 0; i < Register.bits - shift; i++)
            {
                emptyBuffer.DrivingCopies(BUFFER[i], A[i + shift]);
            }

            for (int i = Register.bits - shift; i < Register.bits; i++)
            {
                emptyBuffer.Drives(A[0], false);
            }

            mov.exit.AutomaticallyMovesTo(emptyBuffer);

            return Util.ConcatArrays(mov.states, new AacFlState[] { emptyBuffer });
        }
    }
}
