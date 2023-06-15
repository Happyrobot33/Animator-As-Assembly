using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class DEC : OPCODE
    {
        public Register A;

        /// <summary> Decrements a register by 1</summary>
        /// <param name="A"> The register to Decrements </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public DEC(Register A, AacFlLayer FX)
        {
            this.A = A;
            states = STATES(A, FX);
        }

        AacFlState[] STATES(Register A, AacFlLayer FX)
        {
            //get globals
            Globals globals = new Globals(FX);

            //add 1 to the register
            SUB sub = new SUB(globals.ONE, A, FX);

            return sub.states;
        }
    }
}
