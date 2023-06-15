using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

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
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public SUB(Register A, Register B, AacFlLayer FX)
        {
            this.A = A;
            this.B = B;
            states = STATES(A, B, FX);
        }

        AacFlState[] STATES(Register A, Register B, AacFlLayer FX)
        {
            //get globals
            Globals globals = new Globals(FX);

            //calculate the complement of B
            COMPLEMENT complement = new COMPLEMENT(B, FX);

            //do the subtraction
            ADD add = new ADD(A, complement.A, FX);

            complement.exit.AutomaticallyMovesTo(add.entry);

            return Util.ConcatArrays(complement.states, add.states);
        }
    }
}
