using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class COMPLEMENT : OPCODE
    {
        public Register A;

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="Layer"> The Layer that this command is linked to </param>
        public COMPLEMENT(Register A, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            FLIP flip = new FLIP(A, Layer);
            ADD one = new ADD(A, Globals.ONE, Layer);
            MOV mov = new MOV(one.SUM, A, Layer);
            flip.entry.AutomaticallyMovesTo(one.entry);
            one.exit.AutomaticallyMovesTo(mov.states[0]);

            return Util.ConcatArrays(flip.states, one.states, mov.states);
        }
    }
}
