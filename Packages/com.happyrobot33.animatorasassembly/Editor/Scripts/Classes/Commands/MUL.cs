using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class MUL : OPCODE
    {
        public Register A;
        public Register B;

        /// <summary> A internal register that stores the intermediate results of the multiplication </summary>
        Register Intermediate;

        /// <summary> The final result of the multiplication, after adding all intermediates </summary>
        public Register Result;

        /// <summary> Multiplies a register by another. Result is stored in A </summary>
        /// <param name="A"> The register to multiply </param>
        /// <param name="B"> The register to multiply by </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public MUL(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, Layer, progressWindow);
        }

        /// <summary> Multiplies a register by another. Result is stored in A </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public MUL(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), new Register(args[1], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this.B = B;
            this.Intermediate = new Register("INTERNAL/MUL/Intermediate", Layer);
            this.Result = new Register("INTERNAL/MUL/Result", Layer);
            this._layer = Layer.NewStateGroup("MUL");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register", "register" };
        }

        // Binary multiplication is complicated
        // Essentially, for each bit in A, we multiply it by each bit in B
        /*        1011   (this is binary for decimal 11)
                x 1110   (this is binary for decimal 14)
                ======
              00000000   (this is 1011 x 0)
              00010110   (this is 1011 x 1, shifted one position to the left)
              00101100   (this is 1011 x 1, shifted two positions to the left)
            + 01011000   (this is 1011 x 1, shifted three positions to the left)
              =========
              10011010   (this is binary for decimal 154) */
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("MUL");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("MUL", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("MUL");
            AacFlState exit = _layer.NewState("MUL_EXIT");

            Result.Set(entry, 0);

            List<AacFlState> interstates = new List<AacFlState>();
            for (int i = 0; i < Register._bitDepth; i++)
            {
                Profiler.BeginSample("MUL_INTERMEDIATE_" + i);
                //create a new intermediate state
                //this state determines if to add or skip the intermediate result
                AacFlState interSplit = _layer.NewState("MUL_INTERMEDIATE_" + i + "_SPLIT");
                //link to the one before
                if (i > 0)
                {
                    interstates.Last().AutomaticallyMovesTo(interSplit);
                }
                else
                {
                    entry.AutomaticallyMovesTo(interSplit);
                }

                AacFlState interExit = _layer.NewState("MUL_INTERMEDIATE_" + i + "_EXIT");

                //define the intermediate register
                MOV mov = new MOV(A, Intermediate, _layer, _progressWindow);
                yield return mov;

                //shift the intermediate register by i bits
                SHL shl = new SHL(Intermediate, _layer, _progressWindow, i);
                yield return shl;

                //mov in nothing if the bit is 0
                AacFlState mul0 = _layer.NewState("MUL_INTERMEDIATE_" + i + "_0");
                mul0.AutomaticallyMovesTo(interExit);
                Intermediate.Set(mul0, 0);

                //add intermediate to the result
                ADD add = new ADD(Intermediate, Result, _layer, _progressWindow);
                yield return add;

                interSplit.TransitionsTo(mul0).When(B[i].IsFalse());
                interSplit.TransitionsTo(mov.Entry).When(B[i].IsTrue());
                mov.Exit.AutomaticallyMovesTo(shl.Entry);
                shl.Exit.AutomaticallyMovesTo(add.Entry);
                add.Exit.AutomaticallyMovesTo(interExit);

                interstates.Add(interSplit);
                interstates.Add(mul0);
                interstates.AddRange(mov.States);
                interstates.AddRange(shl.States);
                interstates.AddRange(add.States);
                interstates.Add(interExit);
                Profiler.EndSample();

                yield return PB.SetProgress((float)i / (Register._bitDepth + 1));
            }

            MOV movToResult = new MOV(Result, A, _layer, _progressWindow);
            yield return movToResult;
            yield return PB.SetProgress(1);
            interstates.Last().AutomaticallyMovesTo(movToResult.Entry);
            movToResult.Exit.AutomaticallyMovesTo(exit);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, interstates.ToArray(), movToResult.States, exit));
            yield break;
        }
    }
}
