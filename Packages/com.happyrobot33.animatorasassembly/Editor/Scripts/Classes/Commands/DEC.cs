using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class DEC : OPCODE
    {
        public Register A;

        /// <summary> Decrements a register by 1</summary>
        /// <param name="A"> The register to Decrements </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DEC(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            init(A, Layer, progressWindow);
        }

        /// <summary> Decrements a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DEC(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this.Layer = Layer.NewStateGroup("DEC");
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("DEC");
            ProgressBar PB = this.progressWindow.registerNewProgressBar("DEC", "");
            yield return PB.setProgress(0);
            //add 1 to the register
            SUB sub = new SUB(Globals.ONE, A, Layer, progressWindow);
            yield return sub.compile();
            yield return PB.setProgress(1);

            PB.finish();
            Profiler.EndSample();
            callback(sub.states);
            yield break;
        }
    }
}
