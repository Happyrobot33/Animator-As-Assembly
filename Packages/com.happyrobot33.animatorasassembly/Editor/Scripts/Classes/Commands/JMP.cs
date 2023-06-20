using AnimatorAsCode.Framework;
using UnityEngine.Profiling;
using System.Collections.Generic;

namespace AnimatorAsAssembly.Commands
{
    public class JMP : OPCODE
    {
        public string name;

        /// <summary> Jumps to a line </summary>
        /// <param name="name"> The name of the LBL to jump to</param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JMP(string name, AacFlLayer Layer)
        {
            init(name, Layer);
        }

        /// <summary> Jumps to a line </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JMP(string[] args, AacFlLayer Layer)
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

        //override the linker to jump to the LBL instead
        public override void Link(List<OPCODE> opcodes)
        {
            //find self in list using the ID
            int index = opcodes.FindIndex(x => x.ID == this.ID);

            //link the previous opcode to this one
            //skip if this is the first opcode
            if (index != 0)
            {
                opcodes[index - 1].exit.AutomaticallyMovesTo(entry);
            }

            //find the LBL
            foreach (OPCODE opcode in opcodes)
            {
                if (opcode.GetType() == typeof(LBL))
                {
                    LBL lbl = (LBL)opcode;
                    if (lbl.name == name)
                    {
                        //transition to the LBL
                        entry.AutomaticallyMovesTo(lbl.entry);

                        //set the program counter to the index of the LBL
                        entry.Drives(Globals.PROGRAMCOUNTER, opcodes.IndexOf(lbl));
                        break;
                    }
                }
            }
        }
    }
}
