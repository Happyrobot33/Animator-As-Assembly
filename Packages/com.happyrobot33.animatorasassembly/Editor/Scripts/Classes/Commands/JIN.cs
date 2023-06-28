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
    public class JIN : OPCODE
    {
        public Register A;
        public Register B;
        public string LBLname;
        private AacFlState _jumpAway;

        /// <summary> Jumps to a LBL if A is negative </summary>
        /// <param name="A"> </param>
        /// <param name="B"> </param>
        /// <param name="lblname"> The LBL to jump to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIN(Register A, string lblname, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, lblname, Layer, progressWindow);
        }

        /// <summary> Jumps to a state if A > B </summary>
        /// <remarks> This is used for internal jumps. After initializing this, Link(state) MUST be called </remarks>
        public JIN(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, "INTERNAL", Layer, progressWindow);
        }

        /// <summary> Jumps to a LBL if A > B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JIN(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), args[1], Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, string lblname, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this.LBLname = lblname;
            this._layer = Layer.NewStateGroup("JIN");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "label" };
        }

        // 2s complement makes this easy, literally just check if the MSB is 1
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("JIN");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("JIN", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("JIN");
            AacFlState exit = _layer.NewState("JIN_EXIT");
            _jumpAway = _layer.NewState("JIN_JUMPAWAY");

            entry.TransitionsTo(exit).When(A[Register.BitDepth - 1].IsFalse());
            entry.TransitionsTo(_jumpAway).When(A[Register.BitDepth - 1].IsTrue());

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, _jumpAway, exit));
            yield break;
        }

        public override void Link(List<OPCODE> opcodes)
        {
            //find self in list using the ID
            int index = opcodes.FindIndex(x => x.ID == this.ID);

            //link the previous opcode to this one
            //skip if this is the first opcode
            if (index != 0)
            {
                opcodes[index - 1].Exit.AutomaticallyMovesTo(Entry);
            }

            //find the subroutine that this LBL is in
            int subroutineStart = 0;
            int subroutineEnd = opcodes.Count - 1;
            //first determine if we are in a subroutine by checking up
            for (int i = index; i >= 0; i--)
            {
                if (opcodes[i].GetType() == typeof(SBR))
                {
                    //we are in a subroutine
                    //set the start of the subroutine to the index of the SUB
                    subroutineStart = i;
                    break;
                }
            }

            //find the end of the subroutine
            for (int i = index; i < opcodes.Count; i++)
            {
                if (opcodes[i].GetType() == typeof(RTS))
                {
                    //we are in a subroutine
                    //set the start of the subroutine to the index of the SUB
                    subroutineEnd = i;
                    break;
                }
            }

            //find the LBL
            foreach (OPCODE opcode in opcodes)
            {
                if (opcode.GetType() == typeof(LBL))
                {
                    //check if it is within the subroutine
                    if (opcodes.IndexOf(opcode) < subroutineStart || opcodes.IndexOf(opcode) > subroutineEnd)
                    {
                        //not in the subroutine
                        continue;
                    }
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
