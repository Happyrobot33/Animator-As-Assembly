using AnimatorAsCode.Framework;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class LBL : OPCODE
    {
        public string name;

        /// <summary> Denotes a LBL </summary>
        /// <param name="name"> The name of the LBL </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LBL(string name, AacFlLayer Layer)
        {
            init(name, Layer);
        }

        /// <summary> Denotes a LBL </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LBL(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            init(args[0], Layer);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(string name, AacFlLayer Layer)
        {
            this.name = name;
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("LBL");

            //dummy state
            AacFlState state = Layer.NewState("LBL " + name);

            Profiler.EndSample();
            return new AacFlState[] { state };
        }
    }
}
