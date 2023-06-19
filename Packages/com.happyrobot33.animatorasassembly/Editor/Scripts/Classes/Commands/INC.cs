using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class INC : OPCODE
    {
        public Register A;

        /// <summary> Increments a register by 1</summary>
        /// <param name="A"> The register to increment </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public INC(Register A, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //add 1 to the register
            ADD add = new ADD(Globals.ONE, A, Layer);

            return add.states;
        }
    }
}
