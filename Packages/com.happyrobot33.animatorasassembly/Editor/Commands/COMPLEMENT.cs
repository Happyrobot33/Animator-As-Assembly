using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using static AnimatorAsAssembly.Globals;

namespace AnimatorAsAssembly.Commands
{
    public class COMPLEMENT : OPCODE
    {
        public Register A;

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public COMPLEMENT(Register A, AacFlLayer FX)
        {
            this.A = A;
            states = STATES(A, FX);
        }

        AacFlState[] STATES(Register A, AacFlLayer FX)
        {
            //globals
            Globals globals = new Globals(FX);

            FLIP flip = new FLIP(A, FX);
            ADD one = new ADD(A, globals.ONE, FX);
            MOV mov = new MOV(one.SUM, A, FX);
            flip.entry.AutomaticallyMovesTo(one.entry);
            one.exit.AutomaticallyMovesTo(mov.states[0]);

            return Util.ConcatArrays(flip.states, one.states, mov.states);
        }
    }
}
