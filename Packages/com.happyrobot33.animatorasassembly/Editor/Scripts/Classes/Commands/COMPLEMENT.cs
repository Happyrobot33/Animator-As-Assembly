using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class COMPLEMENT : OPCODE
    {
        public Register A;

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="A"> The register to flip </param>
        /// <param name="Layer"> The Layer that this command is linked to </param>
        public COMPLEMENT(Register A, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            init(A, Layer, progressWindow);
        }

        /// <summary> Calculates the Two's Complement of a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public COMPLEMENT(string[] args, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            this.A = A;
            this.Layer = Layer.NewStateGroup("COMPLEMENT");
            this.progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("COMPLEMENT");
            ProgressBar PB = this.progressWindow.registerNewProgressBar("COMPLEMENT", "");
            yield return PB.setProgress(0);

            FLIP flip = new FLIP(A, Layer, progressWindow);
            yield return flip.compile();
            yield return PB.setProgress(0.33f);

            ADD one = new ADD(Globals.ONE, A, Layer, progressWindow);
            yield return one.compile();
            yield return PB.setProgress(0.66f);

            MOV mov = new MOV(one.SUM, A, Layer, progressWindow);
            yield return mov.compile();
            yield return PB.setProgress(1f);

            flip.exit.AutomaticallyMovesTo(one.entry);
            one.exit.AutomaticallyMovesTo(mov.states[0]);

            PB.finish();
            Profiler.EndSample();
            callback(Util.CombineStates(flip.states, one.states, mov.states));
            yield break;
        }
    }
}
