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
            AacFlLayer FX,
            int i = 0
        )
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.FX = FX;
            SUM = FX.BoolParameter("FULLADDER/SUM" + i);
            CARRY = FX.BoolParameter("FULLADDER/CARRY" + i);
            states = STATES(i);
        }

        AacFlState[] STATES(int i = 0)
        {
            //entry state
            AacFlState entry = FX.NewState("FULLADDER");

            //first half adder
            HALFADDER firstHalfAdder = new Commands.HALFADDER(A, B, FX, 1);

            //second half adder
            HALFADDER secondHalfAdder = new Commands.HALFADDER(firstHalfAdder.SUM, C, FX, 2);

            //set carry based on either half adders carry flag
            AacFlState carryCalc = FX.NewState("FULLADDER CARRY");
            carryCalc.Drives(CARRY, true);

            //exit state
            AacFlState exit = FX.NewState("FULLADDER EXIT");
            exit.DrivingCopies(secondHalfAdder.SUM, SUM);

            //entry state
            entry.AutomaticallyMovesTo(firstHalfAdder.states[0]);
            firstHalfAdder.states[firstHalfAdder.states.Length - 1].AutomaticallyMovesTo(
                secondHalfAdder.states[0]
            );
            secondHalfAdder.states[secondHalfAdder.states.Length - 1]
                .TransitionsTo(carryCalc)
                .When(firstHalfAdder.CARRY.IsTrue())
                .Or()
                .When(secondHalfAdder.CARRY.IsTrue());
            carryCalc.AutomaticallyMovesTo(exit);
            secondHalfAdder.states[secondHalfAdder.states.Length - 1].AutomaticallyMovesTo(exit);

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
