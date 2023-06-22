using AnimatorAsCode;
using AnimatorAsCode.Framework;

using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class HALFADDER : OPCODE
    {
        public AacFlBoolParameter A;
        public AacFlBoolParameter B;
        public AacFlBoolParameter SUM;
        public AacFlBoolParameter CARRY;
        public AacFlState carryCalc;
        public AacFlState sumCalc;

        /// <summary> Adds two bits together </summary>
        /// <param name="A"> The first bit to add </param>
        /// <param name="B"> The second bit to add </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        /// <param name="i"> The identifier of this command (avoids command overlap) </param>
        public HALFADDER(AacFlBoolParameter A, AacFlBoolParameter B, AacFlLayer Layer, int i = 0)
        {
            this.A = A;
            this.B = B;
            this.Layer = Layer.NewStateGroup("HALFADDER");
            SUM = Layer.BoolParameter("INTERNAL/HALFADDER/SUM" + i);
            CARRY = Layer.BoolParameter("INTERNAL/HALFADDER/CARRY" + i);
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("HALFADDER");
            //entry state
            AacFlState entry = Layer.NewState("HALFADDER");
            entry.Drives(SUM, false);
            entry.Drives(CARRY, false);

            //sum calc
            sumCalc = Layer.NewState("HALFADDER SUM");
            sumCalc.Drives(SUM, true);

            //carry calc
            carryCalc = Layer.NewState("HALFADDER CARRY");
            carryCalc.Drives(CARRY, true);

            //exit state
            AacFlState exit = Layer.NewState("HALFADDER EXIT");

            //entry state
            //XOR A and B to get the sum
            entry.TransitionsTo(sumCalc).When(A.IsTrue()).And(B.IsFalse());
            entry.TransitionsTo(sumCalc).When(A.IsFalse()).And(B.IsTrue());

            //AND A and B to get the carry
            entry.TransitionsTo(carryCalc).When(A.IsTrue()).And(B.IsTrue());
            sumCalc.TransitionsTo(carryCalc).When(A.IsFalse()).And(B.IsFalse());
            entry.AutomaticallyMovesTo(exit);
            sumCalc.AutomaticallyMovesTo(exit);
            carryCalc.AutomaticallyMovesTo(exit);

            Profiler.EndSample();
            return Util.CombineStates(entry, sumCalc, carryCalc, exit);
        }
    }
}
