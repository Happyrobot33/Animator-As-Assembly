using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class COMPLEMENT : OPCODE
    {
        public Register A;

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="Layer"> The Layer that this command is linked to </param>
        public COMPLEMENT(Register A, AacFlLayer Layer)
        {
            init(A, Layer);
        }

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public COMPLEMENT(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), Layer);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("COMPLEMENT");
            FLIP flip = new FLIP(A, Layer);
            ADD one = new ADD(Globals.ONE, A, Layer);
            MOV mov = new MOV(one.SUM, A, Layer);
            flip.exit.AutomaticallyMovesTo(one.entry);
            one.exit.AutomaticallyMovesTo(mov.states[0]);

            Profiler.EndSample();
            return Util.CombineStates(flip.states, one.states, mov.states);
        }
    }
}
