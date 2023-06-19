using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class SUB : OPCODE
    {
        public Register A;
        public Register B;
        public Register C;

        /// <summary> Subtracts two registers </summary>
        /// <remarks> The result is stored in the second register or C </remarks>
        /// <param name="A"> The first register to sub </param>
        /// <param name="B"> The second register to sub </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(Register A, Register B, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.C = B;
            this.Layer = Layer;
            states = STATES();
        }

        /// <inheritdoc cref="SUB(Register, Register, AacFlLayer)"/>
        /// <param name="C"> The register to store the result in </param>
        public SUB(Register A, Register B, Register C, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //calculate the complement of B
            COMPLEMENT complement = new COMPLEMENT(B, Layer);

            //do the subtraction
            ADD add = new ADD(A, complement.A, C, Layer);

            complement.exit.AutomaticallyMovesTo(add.entry);

            return Util.ConcatArrays(complement.states, add.states);
        }
    }
}
