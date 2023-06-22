using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class FLIP : OPCODE
    {
        public Register A;
        Register BUFFER;

        /// <summary> Bitwise flips a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public FLIP(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            init(A, Layer, progressWindow);
        }

        /// <summary> Bitwise flips a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public FLIP(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this.BUFFER = new Register("INTERNAL/FLIP/BUFFER", Layer);
            this.Layer = Layer.NewStateGroup("FLIP");
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("FLIP");
            AacFlState entry = Layer.NewState("FLIP");
            for (int i = 0; i < Register.bits; i++)
            {
                entry.DrivingRemaps(A[i], 0f, 1f, BUFFER[i], 1f, 0f);
            }
            AacFlState exit = Layer.NewState("FLIP_EXIT");
            entry.AutomaticallyMovesTo(exit);
            for (int i = 0; i < Register.bits; i++)
            {
                exit.DrivingCopies(BUFFER[i], A[i]);
            }
            Profiler.EndSample();
            callback(Util.CombineStates(entry, exit));
            yield break;
        }
    }
}
