using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class JIG : OPCODE
    {
        public Register A;
        public Register B;
        public string LBLname;
        private AacFlState _jumpAway;

        /// <summary> Jumps to a LBL if A > B </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="lblname"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIG(
            Register A,
            Register B,
            string lblname,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            Init(A, B, lblname, Layer, progressWindow);
        }

        /// <summary> Jumps to a state if A > B </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JIG(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, "INTERNAL", Layer, progressWindow);
        }

        /// <summary> Jumps to a LBL if A > B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIG(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
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
            this._layer = Layer.NewStateGroup("JIG");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register", "label" };
        }

        // there is a check state for each bit pair
        // if A.bit == 1 and B.bit == 0, then A > B
        // if A.bit == 0 and B.bit == 1, then A < B
        // if A.bit == B.bit, then check the next bit
        // we can compare the two bits to eachother by checking
        // A.bit == true && B.bit == true OR A.bit == false && B.bit == false
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("JIG");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("JIG", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("JIG");
            AacFlState exit = _layer.NewState("JIG_EXIT");
            _jumpAway = _layer.NewState("JIG_JUMPAWAY");

            AacFlState[] states = new AacFlState[Register._bitDepth];
            for (int i = Register._bitDepth - 1; i >= 0; i--)
            {
                states[i] = _layer.NewState("JIG_" + i);
            }

            //go top down through the bits due to endianness
            for (int i = Register._bitDepth - 1; i >= 0; i--)
            {
                AacFlState nextState = exit;
                if (i != 0)
                {
                    nextState = states[i - 1];
                }

                //transition to this state if the conditions defined above are true
                AacFlTransition next = states[i].TransitionsTo(nextState);
                AacFlTransition jump = states[i].TransitionsTo(_jumpAway);
                AacFlTransition end = states[i].TransitionsTo(exit);

                //if A.bit == 1 and B.bit == 0, then A > B
                jump.When(A[i].IsTrue()).And(B[i].IsFalse());
                //if A.bit == 0 and B.bit == 1, then A < B
                end.When(A[i].IsFalse()).And(B[i].IsTrue());
                //if A.bit == B.bit, then check the next bit
                next.When(A[i].IsTrue()).And(B[i].IsTrue()).Or().When(A[i].IsFalse()).And(B[i].IsFalse());

                PB.SetProgress((float)i / Register._bitDepth);
            }

            //connect the entry to the first state
            entry.AutomaticallyMovesTo(states[Register._bitDepth - 1]);

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
