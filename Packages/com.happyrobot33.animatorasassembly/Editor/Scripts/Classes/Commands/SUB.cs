using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class SUB : OPCODE
    {
        public Register A;
        public Register B;
        public Register C;

        /// <summary> Subtracts two registers </summary>
        /// <remarks> The result is stored in the second register or C </remarks>
        /// <param name="A"> The first register to sub </param>
        /// <param name="B"> The second register to sub </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(Register A, Register B, AacFlLayer Layer)
        {
            init(A, B, Layer);
        }

        /// <inheritdoc cref="SUB(Register, Register, AacFlLayer)"/>
        /// <param name="C"> The register to store the result in </param>
        public SUB(Register A, Register B, Register C, AacFlLayer Layer)
        {
            init(A, B, Layer, C);
        }

        /// <summary> Subtracts two registers </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(string[] args, AacFlLayer Layer)
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
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("SUB");
            Register Btemp = new Register("INTERNAL/SUB/Btemp", Layer);
            MOV mov = new MOV(B, Btemp, Layer);
            //calculate the complement of B
            COMPLEMENT complement = new COMPLEMENT(Btemp, Layer);

            //do the subtraction
            ADD add = new ADD(A, complement.A, C, Layer);

            mov.exit.AutomaticallyMovesTo(complement.entry);
            complement.exit.AutomaticallyMovesTo(add.entry);

            Profiler.EndSample();
            return Util.CombineStates(mov.states, complement.states, add.states);
        }
    }
}
