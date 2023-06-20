using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class ADD : OPCODE
    {
        public Register A;
        public Register B;
        public Register C;
        public AacFlBoolParameter CARRY;
        public Register SUM;

        /// <summary> Adds two registers </summary>
        /// <remarks> The result is stored in the second register or in C </remarks>
        /// <param name="A"> The first register to add </param>
        /// <param name="B"> The second register to add </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public ADD(Register A, Register B, AacFlLayer Layer)
        {
            init(A, B, Layer);
        }

        /// <inheritdoc cref="ADD(Register, Register, AacFlLayer)"/>
        /// <param name="C"> The register to store the result in </param>
        public ADD(Register A, Register B, Register C, AacFlLayer Layer)
        {
            init(A, B, Layer, C);
        }

        /// <summary> Adds two registers </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public ADD(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            if (args.Length == 2)
                init(new Register(args[0], Layer), new Register(args[1], Layer), Layer);
            else
                init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    new Register(args[2], Layer)
                );
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, Register B, AacFlLayer Layer, Register C = null)
        {
            this.A = A;
            this.B = B;
            this.C = C ?? B;
            this.Layer = Layer;
            CARRY = Layer.BoolParameter("INTERNAL/ADD/CARRY");
            SUM = new Register("INTERNAL/ADD/SUM", Layer);
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("ADD");
            //entry state
            AacFlState entry = Layer.NewState("EIGHTBITADDER");
            //clear sum and carry registers
            entry.Drives(CARRY, false);
            SUM.Set(entry, 0);

            //exit state
            AacFlState exit = Layer.NewState("EIGHTBITADDER EXIT");

            FULLADDER[] FullAdders = new FULLADDER[Register.bits];

            for (int j = 0; j < Register.bits; j++)
            {
                Profiler.BeginSample("ADD/FULLADDER " + j);
                /// <summary> The previous carry bit </summary>
                AacFlBoolParameter prevcarry = Globals.FALSE;
                if (j > 0)
                {
                    //copy prevcarry into our own register so it isnt cleared
                    prevcarry = Layer.BoolParameter("ADD/PREV_CARRY");
                    FullAdders[j - 1].exit.DrivingCopies(FullAdders[j - 1].CARRY, prevcarry);
                }
                // create a FullAdder for each bit
                FULLADDER adder = new FULLADDER(A[j], B[j], prevcarry, Layer);
                //copy the sum bit to the output register
                adder.exit.DrivingCopies(adder.SUM, SUM[j]);

                FullAdders[j] = adder;
                Profiler.EndSample();
            }

            //set the carry bit if the last FullAdder has a carry bit
            FullAdders[FullAdders.Length - 1].carryCalc.Drives(CARRY, true);

            //use a MOV to copy the sum register to the C register
            MOV mov = new MOV(SUM, C, Layer);

            //link the full adders together
            entry.AutomaticallyMovesTo(FullAdders[0].entry);
            for (int j = 0; j < Register.bits - 1; j++)
            {
                FullAdders[j].exit.AutomaticallyMovesTo(FullAdders[j + 1].entry);
            }
            FullAdders[Register.bits - 1].exit.AutomaticallyMovesTo(mov.entry);
            mov.exit.AutomaticallyMovesTo(exit);

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

            Profiler.EndSample();
            return Util.ConcatArrays(
                new AacFlState[] { entry },
                FullAdderStates,
                mov.states,
                new AacFlState[] { exit }
            );
        }
    }
}
