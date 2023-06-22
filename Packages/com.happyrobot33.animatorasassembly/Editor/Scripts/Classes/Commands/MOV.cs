using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class MOV : OPCODE
    {
        public Register A;
        public Register B;

        /// <summary> Moves a register to another register </summary>
        /// <param name="A"> The register to copy </param>
        /// <param name="B"> The register to copy to </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public MOV(Register A, Register B, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            init(A, B, Layer, progressWindow);
        }

        /// <summary> Moves a register to another register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public MOV(string[] args, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), new Register(args[1], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, Register B, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            this.A = A;
            this.B = B;
            this.Layer = Layer.NewStateGroup("MOV");
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("MOV");
            //entry state
            AacFlState entry = Layer.NewState("MOV");
            AacFlState exit = entry;

            for (int i = 0; i < Register.bits; i++)
            {
                entry.DrivingCopies(A[i], B[i]);
            }

            Profiler.EndSample();
            yield return null;
            callback(new AacFlState[] { entry });
            yield break;
        }
    }
}
