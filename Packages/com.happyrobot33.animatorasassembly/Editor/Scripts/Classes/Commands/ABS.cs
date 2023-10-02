using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class ABS : OPCODE
    {
        public Register A;

        /// <summary> Makes a value always positive </summary>
        /// <param name="A"> The register to absolute </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public ABS(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, Layer, progressWindow);
        }

        /// <summary> Makes a value always positive </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public ABS(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("ABS");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("ABS");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("ABS", "");
            yield return PB.SetProgress(0);

            //create entry, exit states
            AacFlState entry = _layer.NewState("ABS Entry");
            AacFlState exit = _layer.NewState("ABS Exit");

            //if the value is not negative, skip the absolute
            JIN skip = new JIN(A, _layer, _progressWindow);
            yield return skip;
            skip.Exit.AutomaticallyMovesTo(exit);
            entry.AutomaticallyMovesTo(skip.Entry);

            yield return PB.SetProgress(0.33f);

            //if it is negative, make it positive
            NEG neg = new NEG(A, _layer, _progressWindow);
            yield return neg;
            skip.Link(neg.Entry);
            neg.Exit.AutomaticallyMovesTo(exit);

            yield return PB.SetProgress(1);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, skip, neg, exit));
            yield break;
        }
    }
}
