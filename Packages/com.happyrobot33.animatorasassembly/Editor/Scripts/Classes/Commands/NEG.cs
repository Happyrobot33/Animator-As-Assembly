using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class NEG : OPCODE
    {
        public Register A;

        /// <summary> Negates a register </summary>
        /// <param name="A"> The register to Negates </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public NEG(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, Layer, progressWindow);
        }

        /// <summary> Negates a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public NEG(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("NEG");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("NEG");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("NEG", "");
            yield return PB.SetProgress(0);

            FLIP flip = new FLIP(A, _layer, _progressWindow);
            yield return PB.SetProgress(0.55f);
            INC inc = new INC(A, _layer, _progressWindow);
            yield return flip;
            yield return inc;
            flip.Exit.AutomaticallyMovesTo(inc.Entry);

            yield return PB.SetProgress(1);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(flip, inc));
            yield break;
        }
    }
}
