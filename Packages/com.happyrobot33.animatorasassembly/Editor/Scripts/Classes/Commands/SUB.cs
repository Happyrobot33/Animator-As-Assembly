using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class SUB : OPCODE
    {
        public Register A;
        public Register B;

        /// <summary> Subtracts two registers </summary>
        /// <remarks> The result is stored in the second register </remarks>
        /// <param name="A"> The first register to sub </param>
        /// <param name="B"> The second register to sub </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(Register A, Register B, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //get globals
            Globals globals = new Globals(Layer);

            //calculate the complement of B
            COMPLEMENT complement = new COMPLEMENT(B, Layer);

            //do the subtraction
            ADD add = new ADD(A, complement.A, Layer);

            complement.exit.AutomaticallyMovesTo(add.entry);

            return Util.ConcatArrays(complement.states, add.states);
        }
    }
}
