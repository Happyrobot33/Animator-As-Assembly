using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using AnimatorAsAssembly;

namespace AnimatorAsAssembly.Commands
{
    public class HALFADDER
    {
        public AacFlState[] states;
        public AacFlBoolParameter A;
        public AacFlBoolParameter B;
        AacFlLayer FX;
        public AacFlBoolParameter SUM;
        public AacFlBoolParameter CARRY;
        public AacFlState entry;
        public AacFlState exit;
        public AacFlState carryCalc;
        public AacFlState sumCalc;

        /// <summary> Adds two bits together </summary>
        /// <param name="A"> The first bit to add </param>
        /// <param name="B"> The second bit to add </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        /// <param name="i"> The identifier of this command (avoids command overlap) </param>
        public HALFADDER(AacFlBoolParameter A, AacFlBoolParameter B, AacFlLayer FX)
        {
            this.A = A;
            this.B = B;
            this.FX = FX;
            SUM = FX.BoolParameter("HALFADDER/SUM" + this.GetHashCode());
            CARRY = FX.BoolParameter("HALFADDER/CARRY" + this.GetHashCode());
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //entry state
            entry = FX.NewState("HALFADDER");

            //sum calc
            sumCalc = FX.NewState("HALFADDER SUM");
            sumCalc.Drives(SUM, true);

            //carry calc
            carryCalc = FX.NewState("HALFADDER CARRY");
            carryCalc.Drives(CARRY, true);

            //exit state
            exit = FX.NewState("HALFADDER EXIT");

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

            return Util.ConcatArrays(entry, sumCalc, carryCalc, exit);
        }
    }
}
