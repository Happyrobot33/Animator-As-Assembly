using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using AnimatorAsAssembly;
using static AnimatorAsAssembly.Globals;

namespace AnimatorAsAssembly.Commands
{
    public class ADD
    {
        public AacFlState[] states;
        public Register A;
        public Register B;
        public AacFlBoolParameter CARRY;
        public Register SUM;
        AacFlLayer FX;

        /// <summary> Adds two registers </summary>
        /// <param name="A"> The first register to add </param>
        /// <param name="B"> The second register to add </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public ADD(Register A, Register B, AacFlLayer FX)
        {
            this.A = A;
            this.B = B;
            this.FX = FX;
            CARRY = FX.BoolParameter("EIGHTBITADDER/CARRY");
            SUM = new Register("EIGHTBITADDER/SUM", FX);
            states = STATES();
        }

        AacFlState[] STATES()
        {
            //globals
            Globals globals = new Globals(FX);

            //entry state
            AacFlState entry = FX.NewState("EIGHTBITADDER");

            //exit state
            AacFlState exit = FX.NewState("EIGHTBITADDER EXIT");

            FULLADDER[] FullAdders = new FULLADDER[Register.bits];

            for (int j = 0; j < Register.bits; j++)
            {
                /// <summary> The previous carry bit </summary>
                AacFlBoolParameter prevcarry = globals.FALSE;
                if (j > 0)
                {
                    prevcarry = FullAdders[j - 1].CARRY;
                }
                // create a FullAdder for each bit
                FULLADDER adder = new FULLADDER(A[j], B[j], prevcarry, FX);
                //copy the sum bit to the output register
                adder.exit.DrivingCopies(adder.SUM, SUM[j]);

                FullAdders[j] = adder;
            }

            //set the carry bit if the last FullAdder has a carry bit
            FullAdders[FullAdders.Length - 1].carryCalc.Drives(CARRY, true);

            //link the full adders together
            entry.AutomaticallyMovesTo(FullAdders[0].entry);
            for (int j = 0; j < Register.bits - 1; j++)
            {
                FullAdders[j].exit.AutomaticallyMovesTo(FullAdders[j + 1].entry);
            }
            FullAdders[Register.bits - 1].exit.AutomaticallyMovesTo(exit);

            //convert the FullAdder states into a single array
            AacFlState[] FullAdderStates = new AacFlState[
                Register.bits * FullAdders[0].states.Length
            ];
            for (int j = 0; j < Register.bits; j++)
            {
                for (int k = 0; k < FullAdders[j].states.Length; k++)
                {
                    FullAdderStates[j * FullAdders[j].states.Length + k] = FullAdders[j].states[k];
                }
            }

            return Util.ConcatArrays(
                new AacFlState[] { entry },
                FullAdderStates,
                new AacFlState[] { exit }
            );
        }
    }
}
