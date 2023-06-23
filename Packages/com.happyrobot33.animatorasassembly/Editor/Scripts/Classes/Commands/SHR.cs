using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class SHR : OPCODE
    {
        public Register A;
        int shift;
        Register BUFFER;

        /// <summary> Shifts a Registers bits 1 to the right</summary>
        /// <param name="A"> The register to shift </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SHR(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow, int shift = 1)
        {
            Init(A, Layer, shift, progressWindow);
        }

        /// <summary> Shifts a Registers bits 1 to the right</summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SHR(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            if (args.Length == 1)
                Init(new Register(args[0], Layer), Layer, progressWindow);
            else
                Init(new Register(args[0], Layer), Layer, progressWindow, int.Parse(args[1]));
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow, int shift = 1)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("SHR");
            this.BUFFER = new Register("INTERNAL/SHR/BUFFER", Layer);
            this.shift = shift;
            this._progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("SHR");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("SHR", "");
            yield return PB.SetProgress(0);
            //copy from A to BUFFER
            MOV mov = new MOV(A, BUFFER, _layer, _progressWindow);
            yield return mov;
            yield return PB.SetProgress(0.2f);

            //copy them back
            AacFlState emptyBuffer = _layer.NewState("SHR");
            for (int i = 0; i < Register._bitDepth - shift; i++)
            {
                emptyBuffer.DrivingCopies(BUFFER[i + shift], A[i]);
            }
            yield return PB.SetProgress(0.4f);

            for (int i = 0; i < shift; i++)
            {
                emptyBuffer.Drives(A[Register._bitDepth - i - 1], false);
            }
            yield return PB.SetProgress(0.6f);

            mov.Exit.AutomaticallyMovesTo(emptyBuffer);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(mov.States, emptyBuffer));
            yield break;
        }
    }
}
