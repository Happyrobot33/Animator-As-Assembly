using AnimatorAsCode.Framework;
using UnityEngine.Profiling;


namespace AnimatorAsAssembly.Commands
{
    public class SHL : OPCODE
    {
        public Register A;
        Register BUFFER;

        /// <summary> Shifts a Registers bits 1 to the left</summary>
        /// <param name="A"> The register to shift </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SHL(Register A, AacFlLayer Layer, int shift = 1)
        {
            init(A, Layer, shift);
        }

        /// <summary> Shifts a Registers bits 1 to the left</summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SHL(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            if (args.Length == 1)
                init(new Register(args[0], Layer), Layer);
            else
                init(new Register(args[0], Layer), Layer, int.Parse(args[1]));
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer, int shift = 1)
        {
            this.A = A;
            this.Layer = Layer.NewStateGroup("SHL");
            this.BUFFER = new Register("INTERNAL/SHL/BUFFER", Layer);
            states = STATES(shift);
        }

        AacFlState[] STATES(int shift)
        {
            Profiler.BeginSample("SHL");
            //copy from A to BUFFER
            MOV mov = new MOV(A, BUFFER, Layer);

            //copy them back
            AacFlState emptyBuffer = Layer.NewState("SHL");
            for (int i = 0; i < Register.bits - shift; i++)
            {
                emptyBuffer.DrivingCopies(BUFFER[i], A[i + shift]);
            }

            for (int i = 0; i < shift; i++)
            {
                emptyBuffer.Drives(A[i], false);
            }

            mov.exit.AutomaticallyMovesTo(emptyBuffer);

            Profiler.EndSample();
            return Util.CombineStates(mov.states, emptyBuffer);
        }
    }
}
