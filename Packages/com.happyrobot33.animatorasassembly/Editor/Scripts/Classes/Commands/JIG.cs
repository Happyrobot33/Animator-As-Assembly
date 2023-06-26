using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JIG : OPCODE
    {
        public Register A;
        public Register B;
        private Register _compare;
        public string LBLname;
        private AacFlState _jumpAway;
        private SUB _sub;

        /// <summary> Jumps to a LBL if A >= B </summary>
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

        /// <summary> Jumps to a state if A >= B </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JIG(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, "INTERNAL", Layer, progressWindow);
        }

        /// <summary> Jumps to a LBL if A >= B </summary>
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
            this._compare = new Register("INTERNAL/JIG/Compare", Layer);
            this._layer = Layer.NewStateGroup("JIG");
            this._progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("JIG");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("JIG", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("JIG");
            AacFlState exit = _layer.NewState("JIG_EXIT");
            _jumpAway = _layer.NewState("JIG_JUMPAWAY");

            //if A >= B, jump to LBL
            //do this by checking if A - B is negative
            //if it is, then A < B
            //if it isn't, then A >= B
            _sub = new SUB(A, B, _compare, _layer, _progressWindow);
            yield return _sub;
            yield return PB.SetProgress(0.5f);
            entry.AutomaticallyMovesTo(_sub.Entry);

            //if the highest bit is 1, then A < B
            _sub.Exit.TransitionsTo(_jumpAway).When(_compare[Register._bitDepth - 1].IsFalse());
            _sub.Exit.AutomaticallyMovesTo(exit);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, _sub.States, _jumpAway, exit));
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
