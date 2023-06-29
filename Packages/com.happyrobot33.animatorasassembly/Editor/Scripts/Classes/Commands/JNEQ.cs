using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JNEQ : OPCODE
    {
        public Register A;
        public Register B;
        public string LBLname;
        private AacFlState _jumpAway;

        /// <summary> Jumps to a LBL if A != B </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="lblname"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JNEQ(
            Register A,
            Register B,
            string lblname,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            Init(A, B, lblname, Layer, progressWindow);
        }

        /// <summary> Jumps to a state if A != B </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JNEQ(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, "INTERNAL", Layer, progressWindow);
        }

        /// <summary> Jumps to a LBL if A != B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JNEQ(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
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
            this._layer = Layer.NewStateGroup("JNEQ");
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
            Profiler.BeginSample("JNEQ");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("JNEQ", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("JNEQ");
            AacFlState exit = _layer.NewState("JNEQ_EXIT");
            _jumpAway = _layer.NewState("JNEQ_JUMPAWAY");

            AacFlState[] states = new AacFlState[Register.BitDepth];
            for (int i = 0; i < Register.BitDepth; i++)
            {
                states[i] = _layer.NewState("JNEQ_" + i);
            }

            for (int i = 0; i < Register.BitDepth; i++)
            {
                AacFlState next = exit;
                if (i < Register.BitDepth - 1)
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
                states[i].AutomaticallyMovesTo(_jumpAway);
                yield return PB.SetProgress((float)i / Register.BitDepth);
            }

            entry.AutomaticallyMovesTo(states[0]);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, states, _jumpAway, exit));
            yield break;
        }

        public override void Linker()
        {
            LinkToPrevious();
            LinkToLBL(_jumpAway, LBLname);
        }

        public void Link(AacFlState destination)
        {
            _jumpAway.AutomaticallyMovesTo(destination);
        }
    }
}
