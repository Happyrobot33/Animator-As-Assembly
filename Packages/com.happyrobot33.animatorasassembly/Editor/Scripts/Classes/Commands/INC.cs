using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class INC : OPCODE
    {
        public Register A;

        /// <summary> Increments a register by 1</summary>
        /// <param name="A"> The register to increment </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public INC(Register A, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            init(A, Layer, progressWindow);
        }

        /// <summary> Increments a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public INC(string[] args, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            this.A = A;
            this.Layer = Layer.NewStateGroup("INC");
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("INC");
            ProgressBar PB = this.progressWindow.registerNewProgressBar("INC", "");
            yield return PB.setProgress(0);
            //add 1 to the register
            ADD add = new ADD(Globals.ONE, A, Layer, progressWindow);
            yield return add.compile();
            yield return PB.setProgress(1);

            PB.finish();
            Profiler.EndSample();
            callback(add.states);
            yield break;
        }
    }
}
