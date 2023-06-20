using AnimatorAsCode.Framework;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JIG : OPCODE
    {
        public Register A;
        public Register B;
        Register Compare;
        public string LBLname;
        AacFlState JumpAway;
        SUB sub;

        /// <summary> Jumps to a LBL if A >= B </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="LBL"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIG(Register A, Register B, string lblname, AacFlLayer Layer)
        {
            init(A, B, lblname, Layer);
        }

        /// <summary> Jumps to a state if A >= B </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JIG(Register A, Register B, AacFlLayer Layer)
        {
            init(A, B, "INTERNAL", Layer);
        }

        /// <summary> Jumps to a LBL if A >= B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIG(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), new Register(args[1], Layer), args[2], Layer);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, Register B, string lblname, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.LBLname = lblname;
            this.Compare = new Register("INTERNAL/JIG/Compare", Layer);
            this.Layer = Layer;
            states = STATES();
        }

        AacFlState[] STATES()
        {
            Profiler.BeginSample("JIG");
            AacFlState entry = Layer.NewState("JIG");
            AacFlState exit = Layer.NewState("JIG_EXIT");
            JumpAway = Layer.NewState("JIG_JUMPAWAY");

            //if A >= B, jump to LBL
            //do this by checking if A - B is negative
            //if it is, then A < B
            //if it isn't, then A >= B
            sub = new SUB(A, B, Compare, Layer);
            entry.AutomaticallyMovesTo(sub.entry);

            //if the highest bit is 1, then A < B
            sub.exit.TransitionsTo(JumpAway).When(Compare[Register.bits - 1].IsFalse());
            sub.exit.AutomaticallyMovesTo(exit);

            Profiler.EndSample();
            return Util.ConcatArrays(entry, sub.states, JumpAway, exit);
        }

        public override void Link(List<OPCODE> opcodes)
        {
            //find the LBL
            foreach (OPCODE opcode in opcodes)
            {
                if (opcode.GetType() == typeof(LBL))
                {
                    LBL lbl = (LBL)opcode;
                    if (lbl.name == LBLname)
                    {
                        //transition to the LBL
                        JumpAway.AutomaticallyMovesTo(lbl.entry);

                        //set the program counter to the index of the LBL
                        JumpAway.Drives(Globals.PROGRAMCOUNTER, opcodes.IndexOf(lbl));
                        break;
                    }
                }
            }

            base.Link(opcodes);
        }

        public void Link(AacFlState destination)
        {
            JumpAway.AutomaticallyMovesTo(destination);
        }
    }
}
