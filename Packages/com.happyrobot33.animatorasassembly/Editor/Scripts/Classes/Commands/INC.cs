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
        public INC(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, Layer, progressWindow);
        }

        /// <summary> Increments a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public INC(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("INC");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register" };
        }

        // TODO: Make this faster by instead of using ADD, implement a custom setup that ends early when it can
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("INC");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("INC", "");
            yield return PB.SetProgress(0);
            //add 1 to the register
            ADD add = new ADD(Globals._One, A, _layer, _progressWindow);
            yield return add;
            yield return PB.SetProgress(1);

            PB.Finish();
            Profiler.EndSample();
            callback(add);
            yield break;
        }
    }
}
