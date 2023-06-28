using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class RAND : OPCODE
    {
        public Register A;
        public Register min;
        public Register max;

        /// <summary> Loads a register with a random value with a defined range </summary>
        /// <param name="MIN"> The minimum value that can be generated </param>
        /// <param name="MAX"> The maximum value that can be generated </param>
        /// <param name="A"> The register to load the value into </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public RAND(Register min, Register max, Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            throw new NotImplementedException("Not complete yet");
            Init(min, max, A, Layer, progressWindow);
        }

        /// <summary> Loads a register with a int value </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public RAND(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            throw new NotImplementedException("Not complete yet");
            //split the args into the register and the value
            if (args.Length == 3)
            {
                Init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    new Register(args[2], Layer),
                    Layer,
                    progressWindow
                );
            }
            else
            {
                throw new ArgumentException("Invalid number of arguments for RAND");
            }
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register min, Register max, Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this.min = min;
            this.max = max;
            this._layer = Layer.NewStateGroup("RAND");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register", "register" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("RAND");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("RAND", "");
            AacFlState entry = _layer.NewState("RAND");
            AacFlState exit = _layer.NewState("EXIT");

            //clamping
            MOV clampMin = new MOV(min, A, _layer, _progressWindow);
            yield return clampMin;
            clampMin.Exit.AutomaticallyMovesTo(exit);
            MOV clampMax = new MOV(max, A, _layer, _progressWindow);
            yield return clampMax;
            clampMax.Exit.AutomaticallyMovesTo(exit);

            for (int i = 0; i < Register.BitDepth; i++)
            {
                entry.DrivingRandomizes(A[i], 0.5f);
            }

            //if A is less than min, set A to min
            JIG minCheck = new JIG(A, min, _layer, _progressWindow);
            yield return minCheck;

            //if A is greater than max, set A to max
            JIG maxCheck = new JIG(A, max, _layer, _progressWindow);
            yield return maxCheck;

            minCheck.Link(maxCheck.Entry);
            minCheck.Exit.AutomaticallyMovesTo(clampMin.Entry);
            maxCheck.Link(clampMax.Entry);
            maxCheck.Exit.AutomaticallyMovesTo(exit);

            entry.AutomaticallyMovesTo(minCheck.Entry);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, clampMin, clampMax, minCheck, maxCheck, exit));
        }
    }
}
