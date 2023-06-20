using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class INC : OPCODE
    {
        public Register A;

        /// <summary> Increments a register by 1</summary>
        /// <param name="A"> The register to increment </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public INC(Register A, AacFlLayer Layer)
        {
            init(A, Layer);
        }

        /// <summary> Increments a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public INC(string[] args, AacFlLayer Layer)
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
            Profiler.BeginSample("INC");
            //add 1 to the register
            ADD add = new ADD(Globals.ONE, A, Layer);

            Profiler.EndSample();
            return add.states;
        }
    }
}
