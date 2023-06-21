using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class FULLADDER : OPCODE
    {
        public AacFlBoolParameter A;
        public AacFlBoolParameter B;
        public AacFlBoolParameter C;
        public AacFlBoolParameter SUM;
        public AacFlBoolParameter CARRY;
        public AacFlState carryCalc;
        public AacFlState sumCalc;

        /// <summary> Adds two bits and a carry bit </summary>
        /// <param name="A"> The first bit to add </param>
        /// <param name="B"> The second bit to add </param>
        /// <param name="C"> The carry bit to add </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public FULLADDER(
            AacFlBoolParameter A,
            AacFlBoolParameter B,
            AacFlBoolParameter C,
            AacFlLayer Layer
        )
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.Layer = Layer.NewStateGroup("FULLADDER");
            SUM = Layer.BoolParameter("INTERNAL/FULLADDER/SUM");
            CARRY = Layer.BoolParameter("INTERNAL/FULLADDER/CARRY");
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("FULLADDER");
            //entry state
            AacFlState entry = Layer.NewState("FULLADDER");
            entry.Drives(SUM, false);
            entry.Drives(CARRY, false);

            //first half adder
            HALFADDER firstHalfAdder = new Commands.HALFADDER(A, B, Layer, 0);

            //second half adder
            HALFADDER secondHalfAdder = new Commands.HALFADDER(firstHalfAdder.SUM, C, Layer, 1);

            //set carry based on either half adders carry flag
            carryCalc = Layer.NewState("FULLADDER CARRY");
            carryCalc.Drives(CARRY, true);

            //exit state
            AacFlState exit = Layer.NewState("FULLADDER EXIT");
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

            Profiler.EndSample();
            return Util.CombineStates(
                entry,
                firstHalfAdder.states,
                secondHalfAdder.states,
                carryCalc,
                exit
            );
        }
    }
}
