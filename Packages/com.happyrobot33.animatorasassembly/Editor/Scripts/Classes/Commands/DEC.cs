using AnimatorAsCode.Framework;

namespace AnimatorAsAssembly.Commands
{
    public class DEC : OPCODE
    {
        public Register A;

        /// <summary> Decrements a register by 1</summary>
        /// <param name="A"> The register to Decrements </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DEC(Register A, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //get globals
            Globals globals = new Globals(Layer);

            //add 1 to the register
            SUB sub = new SUB(globals.ONE, A, Layer);

            return sub.states;
        }
    }
}
