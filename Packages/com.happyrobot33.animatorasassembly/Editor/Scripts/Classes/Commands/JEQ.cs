using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JEQ : OPCODE
    {
        public Register A;
        public Register B;
        public string LBLname;
        private AacFlState _jumpAway;

        /// <summary> Jumps to a LBL if A == B </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="lblname"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JEQ(
            Register A,
            Register B,
            string lblname,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            Init(A, B, lblname, Layer, progressWindow);
        }

        /// <summary> Jumps to a state if A == B </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JEQ(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, "INTERNAL", Layer, progressWindow);
        }

        /// <summary> Jumps to a LBL if A == B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JEQ(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(
                new Register(args[0], Layer),
                new Register(args[1], Layer),
                args[2],
                Layer,
                progressWindow
            );
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(
            Register A,
            Register B,
            string lblname,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            this.A = A;
            this.B = B;
            this.LBLname = lblname;
            this._layer = Layer.NewStateGroup("JEQ");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register", "label" };
        }

        // this entire method is a crazy hack, but essentially if we do a parallel comparison between both bits to see if both are true or both are false, we can see if both bits are equal
        // IF A.bit == TRUE && B.bit == TRUE, OR
        // IF A.bit == FALSE && B.bit == FALSE, THEN A.bit == B.bit
        // if both of these fail, automatically jump to the exit state
        // this method means this instruction means the instruction requires 2 cycles to complete, instead of the many a SUB instruction would need
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("JEQ");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("JEQ", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("JEQ");
            AacFlState exit = _layer.NewState("JEQ_EXIT");
            _jumpAway = _layer.NewState("JEQ_JUMPAWAY");


            AacFlState[] states = new AacFlState[Register._bitDepth];
            for (int i = 0; i < Register._bitDepth; i++)
            {
                states[i] = _layer.NewState("JEQ_" + i);
            }

            for (int i = 0; i < Register._bitDepth; i++)
            {
                AacFlState next = _jumpAway;
                if (i < Register._bitDepth - 1)
                {
                    next = states[i + 1];
                }

                AacFlTransition transitionFalse = states[i].TransitionsTo(next);
                AacFlTransition transitionTrue = states[i].TransitionsTo(next);

                //if A.bit == TRUE && B.bit == TRUE, OR
                //if A.bit == FALSE && B.bit == FALSE, THEN A.bit == B.bit
                //if both of these fail, automatically jump to the exit state
                transitionTrue.When(A[i].IsTrue()).And(B[i].IsTrue());
                transitionFalse.When(A[i].IsFalse()).And(B[i].IsFalse());
                states[i].AutomaticallyMovesTo(exit);
                yield return PB.SetProgress((float)i / Register._bitDepth);
            }

            entry.AutomaticallyMovesTo(states[0]);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, states, _jumpAway, exit));
            yield break;
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
                        _jumpAway.AutomaticallyMovesTo(lbl.Entry);

                        //set the program counter to the index of the LBL
                        _jumpAway.Drives(Globals._ProgramCounter, opcodes.IndexOf(lbl));
                        break;
                    }
                }
            }

            base.Link(opcodes);
        }

        public void Link(AacFlState destination)
        {
            _jumpAway.AutomaticallyMovesTo(destination);
        }
    }
}
