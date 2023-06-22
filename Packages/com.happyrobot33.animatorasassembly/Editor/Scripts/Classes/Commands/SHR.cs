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
            init(A, Layer, shift);
        }

        /// <summary> Shifts a Registers bits 1 to the right</summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SHR(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            if (args.Length == 1)
                init(new Register(args[0], Layer), Layer, progressWindow);
            else
                init(new Register(args[0], Layer), Layer, progressWindow, int.Parse(args[1]));
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow, int shift = 1)
        {
            this.A = A;
            this.Layer = Layer.NewStateGroup("SHR");
            this.BUFFER = new Register("INTERNAL/SHR/BUFFER", Layer);
            this.shift = shift;
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("SHR");
            ProgressBar PB = this.progressWindow.registerNewProgressBar("SHR", "");
            yield return PB.setProgress(0);
            //copy from A to BUFFER
            MOV mov = new MOV(A, BUFFER, Layer, progressWindow);
            yield return mov.compile();
            yield return PB.setProgress(0.2f);

            //copy them back
            AacFlState emptyBuffer = Layer.NewState("SHR");
            for (int i = 0; i < Register.bits - shift; i++)
            {
                emptyBuffer.DrivingCopies(BUFFER[i + shift], A[i]);
            }
            yield return PB.setProgress(0.4f);

            for (int i = 0; i < shift; i++)
            {
                emptyBuffer.Drives(A[Register.bits - i - 1], false);
            }
            yield return PB.setProgress(0.6f);

            mov.exit.AutomaticallyMovesTo(emptyBuffer);

            PB.finish();
            Profiler.EndSample();
            callback(Util.CombineStates(mov.states, emptyBuffer));
            yield break;
        }
    }
}
