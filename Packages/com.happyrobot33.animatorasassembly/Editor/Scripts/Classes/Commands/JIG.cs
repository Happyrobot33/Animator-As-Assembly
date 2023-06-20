using AnimatorAsCode.Framework;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JIG : OPCODE
    {
        public Register A;
        public Register B;
        Register Compare;
        public AacFlState DestState;

        /// <summary> Jumps to a LBL if A >= B </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="LBL"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIG(Register A, Register B, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.Compare = new Register("INTERNAL/JIG/Compare", Layer);
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("JIG");
            AacFlState entry = Layer.NewState("JIG");
            AacFlState exit = Layer.NewState("JIG_EXIT");
            DestState = Layer.NewState("JIG_DEST");

            //if A >= B, jump to LBL
            //do this by checking if A - B is negative
            //if it is, then A < B
            //if it isn't, then A >= B
            SUB sub = new SUB(A, B, Compare, Layer);
            entry.AutomaticallyMovesTo(sub.entry);

            //if the highest bit is 1, then A < B
            sub.exit.TransitionsTo(DestState).When(Compare[Register.bits - 1].IsFalse());
            sub.exit.AutomaticallyMovesTo(exit);

            Profiler.EndSample();
            return Util.ConcatArrays(
                new AacFlState[] { entry },
                sub.states,
                new AacFlState[] { DestState, exit }
            );
        }
    }
}
