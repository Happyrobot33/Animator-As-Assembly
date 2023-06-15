using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class INC : OPCODE
    {
        public Register A;

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

            return add.states;
        }
    }
}
