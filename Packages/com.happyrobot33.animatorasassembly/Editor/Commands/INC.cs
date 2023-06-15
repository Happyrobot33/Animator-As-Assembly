using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class INC
    {
        public AacFlState[] states;
        public Register A;
        public AacFlState entry;
        public AacFlState exit;

        /// <summary> Increments a register by 1</summary>
        /// <param name="A"> The register to increment </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public INC(Register A, AacFlLayer FX)
        {
            this.A = A;
            states = STATES(A, FX);
        }

        AacFlState[] STATES(Register A, AacFlLayer FX)
        {
            //get globals
            Globals globals = new Globals(FX);

            //add 1 to the register
            ADD add = new ADD(globals.ONE, A, FX);

            //set our entry and exit
            entry = add.entry;
            exit = add.exit;

            return add.states;
        }
    }
}
