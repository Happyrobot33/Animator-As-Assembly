using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JIF : OPCODE
    {
        public Register A;
        public Register B;
        public string LBLname;
        private AacFlState _jumpAway;
        public Conditional cond;

        /// <summary> Jumps to a LBL if the condition is true </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="lblname"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIF(
            Register A,
            Conditional condition,
            Register B,
            string lblname,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            Init(A, condition, B, lblname, Layer, progressWindow);
        }

        /// <summary> Jumps to a state if the condition is true </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JIF(Register A, Conditional condition, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, condition, B, "INTERNAL", Layer, progressWindow);
        }

        /// <summary> Jumps to a LBL if the condition is true </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIF(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //we need to parse the second arg as a conditional
            Conditional parsedCond;
            switch (args[1])
            {
                case "<":
                    parsedCond = Conditional.LessThan;
                    break;
                case "<=":
                    parsedCond = Conditional.LessThanOrEqual;
                    break;
                case "==":
                    parsedCond = Conditional.Equal;
                    break;
                case ">=":
                    parsedCond = Conditional.GreaterThanOrEqual;
                    break;
                case ">":
                    parsedCond = Conditional.GreaterThan;
                    break;
                case "!=":
                    parsedCond = Conditional.NotEqual;
                    break;
                default:
                    throw new Exception("Invalid conditional: " + args[1]);
            }
            //split the args into the register and the value
            Init(
                new Register(args[0], Layer),
                parsedCond,
                new Register(args[2], Layer),
                args[3],
                Layer,
                progressWindow
            );
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(
            Register A,
            Conditional condition,
            Register B,
            string lblname,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            this.A = A;
            this.cond = condition;
            this.B = B;
            this.LBLname = lblname;
            this._layer = Layer.NewStateGroup("JIF " + A.Name + " " + condition + " " + B.Name + " " + lblname);
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "conditional", "register", "label" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("JIF");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("JIF", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("JIF");
            AacFlState exit = _layer.NewState("JIF_EXIT");
            _jumpAway = _layer.NewState("JIF_JUMPAWAY");

            AacFlState[] states = new AacFlState[Register.BitDepth];
            for (int i = Register.BitDepth - 1; i >= 0; i--)
            {
                states[i] = _layer.NewState("JIF_" + i);
            }

            //go top down through the bits due to endianness
            for (int i = Register.BitDepth - 1; i >= 0; i--)
            {
                AacFlState nextState;
                AacFlTransition transitionToNext;
                AacFlTransition transitionToJump;
                AacFlTransition transitionToExit;
                AacFlTransition transitionFalse;
                AacFlTransition transitionTrue;
                switch (cond)
                {
                    case Conditional.LessThan:
                        nextState = exit;
                        if (i != 0)
                        {
                            nextState = states[i - 1];
                        }

                        //transition to this state if the conditions defined above are true
                        transitionToNext = states[i].TransitionsTo(nextState);
                        transitionToJump = states[i].TransitionsTo(_jumpAway);
                        transitionToExit = states[i].TransitionsTo(exit);

                        //if A.bit == 1 and B.bit == 0, then A > B
                        transitionToExit.When(A[i].IsTrue()).And(B[i].IsFalse());
                        //if A.bit == 0 and B.bit == 1, then A < B
                        transitionToJump.When(A[i].IsFalse()).And(B[i].IsTrue());
                        //if A.bit == B.bit, then check the next bit
                        transitionToNext.When(A[i].IsTrue()).And(B[i].IsTrue()).Or().When(A[i].IsFalse()).And(B[i].IsFalse());
                        break;
                    case Conditional.LessThanOrEqual:
                        nextState = _jumpAway;
                        if (i != 0)
                        {
                            nextState = states[i - 1];
                        }

                        //transition to this state if the conditions defined above are true
                        transitionToNext = states[i].TransitionsTo(nextState);
                        transitionToJump = states[i].TransitionsTo(_jumpAway);
                        transitionToExit = states[i].TransitionsTo(exit);

                        //if A.bit == 1 and B.bit == 0, then A > B
                        transitionToExit.When(A[i].IsTrue()).And(B[i].IsFalse());
                        //if A.bit == 0 and B.bit == 1, then A < B
                        transitionToJump.When(A[i].IsFalse()).And(B[i].IsTrue());
                        //if A.bit == B.bit, then check the next bit
                        transitionToNext.When(A[i].IsTrue()).And(B[i].IsTrue()).Or().When(A[i].IsFalse()).And(B[i].IsFalse());
                        break;
                    case Conditional.Equal:
                        nextState = _jumpAway;
                        if (i != 0)
                        {
                            nextState = states[i - 1];
                        }

                        transitionFalse = states[i].TransitionsTo(nextState);
                        transitionTrue = states[i].TransitionsTo(nextState);

                        //if A.bit == TRUE && B.bit == TRUE, OR
                        //if A.bit == FALSE && B.bit == FALSE, THEN A.bit == B.bit
                        //if both of these fail, automatically jump to the exit state
                        transitionTrue.When(A[i].IsTrue()).And(B[i].IsTrue());
                        transitionFalse.When(A[i].IsFalse()).And(B[i].IsFalse());
                        states[i].AutomaticallyMovesTo(exit);
                        break;
                    case Conditional.GreaterThanOrEqual:
                        nextState = _jumpAway;
                        if (i != 0)
                        {
                            nextState = states[i - 1];
                        }

                        //transition to this state if the conditions defined above are true
                        transitionToNext = states[i].TransitionsTo(nextState);
                        transitionToJump = states[i].TransitionsTo(_jumpAway);
                        transitionToExit = states[i].TransitionsTo(exit);

                        //if A.bit == 1 and B.bit == 0, then A > B
                        transitionToJump.When(A[i].IsTrue()).And(B[i].IsFalse());
                        //if A.bit == 0 and B.bit == 1, then A < B
                        transitionToExit.When(A[i].IsFalse()).And(B[i].IsTrue());
                        //if A.bit == B.bit, then check the next bit
                        transitionToNext.When(A[i].IsTrue()).And(B[i].IsTrue()).Or().When(A[i].IsFalse()).And(B[i].IsFalse());
                        break;
                    case Conditional.GreaterThan:
                        nextState = exit;
                        if (i != 0)
                        {
                            nextState = states[i - 1];
                        }

                        //transition to this state if the conditions defined above are true
                        transitionToNext = states[i].TransitionsTo(nextState);
                        transitionToJump = states[i].TransitionsTo(_jumpAway);
                        transitionToExit = states[i].TransitionsTo(exit);

                        //if A.bit == 1 and B.bit == 0, then A > B
                        transitionToJump.When(A[i].IsTrue()).And(B[i].IsFalse());
                        //if A.bit == 0 and B.bit == 1, then A < B
                        transitionToExit.When(A[i].IsFalse()).And(B[i].IsTrue());
                        //if A.bit == B.bit, then check the next bit
                        transitionToNext.When(A[i].IsTrue()).And(B[i].IsTrue()).Or().When(A[i].IsFalse()).And(B[i].IsFalse());
                        break;
                    case Conditional.NotEqual:
                        nextState = exit;
                        if (i != 0)
                        {
                            nextState = states[i - 1];
                        }

                        transitionFalse = states[i].TransitionsTo(nextState);
                        transitionTrue = states[i].TransitionsTo(nextState);

                        //if A.bit == TRUE && B.bit == TRUE, OR
                        //if A.bit == FALSE && B.bit == FALSE, THEN A.bit == B.bit
                        //if both of these fail, automatically jump to the exit state
                        transitionTrue.When(A[i].IsTrue()).And(B[i].IsTrue());
                        transitionFalse.When(A[i].IsFalse()).And(B[i].IsFalse());
                        states[i].AutomaticallyMovesTo(_jumpAway);
                        break;
                    default:
                        throw new Exception("Invalid conditional");
                }
                yield return PB.SetProgress((float)i / Register.BitDepth);
            }

            //connect the entry to the first state
            entry.AutomaticallyMovesTo(states[Register.BitDepth - 1]);

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
