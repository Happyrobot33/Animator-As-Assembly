using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class SUB : OPCODE
    {
        public Register A;
        public Register B;
        public Register C;

        /// <summary> Subtracts two registers </summary>
        /// <remarks> The result is stored in the second register or C </remarks>
        /// <param name="A"> The first register to sub </param>
        /// <param name="B"> The second register to sub </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            init(A, B, Layer, progressWindow);
        }

        /// <inheritdoc cref="SUB(Register, Register, AacFlLayer)"/>
        /// <param name="C"> The register to store the result in </param>
        public SUB(
            Register A,
            Register B,
            Register C,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            init(A, B, Layer, progressWindow, C);
        }

        /// <summary> Subtracts two registers </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SUB(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            if (args.Length == 2)
                init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    progressWindow
                );
            else
                init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    progressWindow,
                    new Register(args[2], Layer)
                );
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(
            Register A,
            Register B,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow,
            Register C = null
        )
        {
            this.A = A;
            this.B = B;
            this.C = C ?? B;
            this.Layer = Layer.NewStateGroup("SUB");
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("SUB");
            ProgressBar PB = this.progressWindow.registerNewProgressBar("SUB", "");
            yield return PB.setProgress(0);

            Register Btemp = new Register("INTERNAL/SUB/Btemp", Layer);
            MOV mov = new MOV(B, Btemp, Layer, progressWindow);
            yield return mov.compile();

            //calculate the complement of B
            COMPLEMENT complement = new COMPLEMENT(Btemp, Layer, progressWindow);
            yield return complement.compile();
            yield return PB.setProgress(0.5f);

            //do the subtraction
            ADD add = new ADD(A, complement.A, C, Layer, progressWindow);
            yield return add.compile();
            yield return PB.setProgress(1f);

            mov.exit.AutomaticallyMovesTo(complement.entry);
            complement.exit.AutomaticallyMovesTo(add.entry);

            PB.finish();
            Profiler.EndSample();
            callback(Util.CombineStates(mov.states, complement.states, add.states));
            yield break;
        }
    }
}
