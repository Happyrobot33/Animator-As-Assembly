using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class DEC : OPCODE
    {
        public Register A;

        /// <summary> Decrements a register by 1</summary>
        /// <param name="A"> The register to Decrements </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DEC(Register A, AacFlLayer Layer)
        {
            init(A, Layer);
        }

        /// <summary> Decrements a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DEC(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), Layer);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer)
        {
            this.A = A;
            this.Layer = Layer.NewStateGroup("DEC");
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("DEC");
            //add 1 to the register
            SUB sub = new SUB(Globals.ONE, A, Layer);

            Profiler.EndSample();
            return sub.states;
        }
    }
}
