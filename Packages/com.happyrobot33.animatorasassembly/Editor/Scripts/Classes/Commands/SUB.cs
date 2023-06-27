using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class SUB : OPCODE
    {
        public Register A;
        public Register B;
        public Register C;

        /// <summary> Subtracts two registers </summary>
        /// <remarks> The result is stored in the second register or C </remarks>
        /// <param name="A"> The first register to sub </param>
        /// <param name="B"> The second register to sub </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, Layer, progressWindow);
        }

        /// <inheritdoc cref="SUB(Register, Register, AacFlLayer)"/>
        /// <param name="C"> The register to store the result in </param>
        public SUB(
            Register A,
            Register B,
            Register C,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            Init(A, B, Layer, progressWindow, C);
        }

        /// <summary> Subtracts two registers </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            if (args.Length == 2)
            {
                Init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    progressWindow
                );
            }
            else
            {
                Init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    progressWindow,
                    new Register(args[2], Layer)
                );
            }
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(
            Register A,
            Register B,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow,
            Register C = null
        )
        {
            this.A = A;
            this.B = B;
            this.C = C ?? B;
            this._layer = Layer.NewStateGroup("SUB");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register", "register" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("SUB");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("SUB", "");
            yield return PB.SetProgress(0);

            Register Btemp = new Register("INTERNAL/SUB/Btemp", _layer);
            MOV mov = new MOV(B, Btemp, _layer, _progressWindow);
            yield return mov;

            //flip B
            FLIP flip = new FLIP(Btemp, _layer, _progressWindow);
            yield return flip;
            yield return PB.SetProgress(0.5f);

            //do the subtraction
            ADD add = new ADD(A, flip.A, C, _layer, _progressWindow)
            {
                CARRYIN = Globals._True
            };
            yield return add;
            yield return PB.SetProgress(1f);

            mov.Exit.AutomaticallyMovesTo(flip.Entry);
            flip.Exit.AutomaticallyMovesTo(add.Entry);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(mov.States, flip.States, add.States));
            yield break;
        }
    }
}
