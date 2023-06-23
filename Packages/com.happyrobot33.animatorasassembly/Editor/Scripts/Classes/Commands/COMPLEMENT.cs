using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class COMPLEMENT : OPCODE
    {
        public Register A;

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="Layer"> The Layer that this command is linked to </param>
        public COMPLEMENT(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, Layer, progressWindow);
        }

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public COMPLEMENT(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("COMPLEMENT");
            this._progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("COMPLEMENT");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("COMPLEMENT", "");
            yield return PB.SetProgress(0);

            FLIP flip = new FLIP(A, _layer, _progressWindow);
            yield return flip;
            yield return PB.SetProgress(0.33f);

            ADD one = new ADD(Globals.ONE, A, _layer, _progressWindow);
            yield return one;
            yield return PB.SetProgress(0.66f);

            MOV mov = new MOV(one.SUM, A, _layer, _progressWindow);
            yield return mov;
            yield return PB.SetProgress(1f);

            flip.Exit.AutomaticallyMovesTo(one.Entry);
            one.Exit.AutomaticallyMovesTo(mov.States[0]);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(flip.States, one.States, mov.States));
            yield break;
        }
    }
}
