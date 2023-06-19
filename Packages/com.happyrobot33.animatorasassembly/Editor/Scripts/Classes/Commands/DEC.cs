using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

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
            Profiler.BeginSample("DEC");
            //add 1 to the register
            SUB sub = new SUB(Globals.ONE, A, Layer);

            Profiler.EndSample();
            return sub.states;
        }
    }
}
