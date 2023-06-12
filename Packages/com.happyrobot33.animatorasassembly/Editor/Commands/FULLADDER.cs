using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using AnimatorAsAssembly;

namespace AnimatorAsAssembly.Commands
{
    public class FULLADDER
    {
        public AacFlState[] states;
        public AacFlBoolParameter A;
        public AacFlBoolParameter B;
        public AacFlBoolParameter C;
        AacFlLayer FX;
        public AacFlBoolParameter SUM;
        public AacFlBoolParameter CARRY;
        public AacFlState entry;
        public AacFlState exit;
        public AacFlState carryCalc;
        public AacFlState sumCalc;

        /// <summary> Adds two bits and a carry bit </summary>
        /// <param name="A"> The first bit to add </param>
        /// <param name="B"> The second bit to add </param>
        /// <param name="C"> The carry bit to add </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        /// <param name="i"> The identifier of this command (avoids command overlap) </param>
        public FULLADDER(
            AacFlBoolParameter A,
            AacFlBoolParameter B,
            AacFlBoolParameter C,
            AacFlLayer FX
        )
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.FX = FX;
            SUM = FX.BoolParameter("FULLADDER/SUM" + this.GetHashCode());
            CARRY = FX.BoolParameter("FULLADDER/CARRY" + this.GetHashCode());
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //entry state
            entry = FX.NewState("FULLADDER");

            //first half adder
            HALFADDER firstHalfAdder = new Commands.HALFADDER(A, B, FX);

            //second half adder
            HALFADDER secondHalfAdder = new Commands.HALFADDER(firstHalfAdder.SUM, C, FX);

            //set carry based on either half adders carry flag
            carryCalc = FX.NewState("FULLADDER CARRY");
            carryCalc.Drives(CARRY, true);

            //exit state
            exit = FX.NewState("FULLADDER EXIT");
            exit.DrivingCopies(secondHalfAdder.SUM, SUM);

            //entry state
            entry.AutomaticallyMovesTo(firstHalfAdder.states[0]);
            firstHalfAdder.exit.AutomaticallyMovesTo(secondHalfAdder.entry);
            secondHalfAdder.exit
                .TransitionsTo(carryCalc)
                .When(firstHalfAdder.CARRY.IsTrue())
                .Or()
                .When(secondHalfAdder.CARRY.IsTrue());
            carryCalc.AutomaticallyMovesTo(exit);
            secondHalfAdder.exit.AutomaticallyMovesTo(exit);

            return Util.ConcatArrays(
                entry,
                firstHalfAdder.states,
                secondHalfAdder.states,
                carryCalc,
                exit
            );
        }
    }
}
