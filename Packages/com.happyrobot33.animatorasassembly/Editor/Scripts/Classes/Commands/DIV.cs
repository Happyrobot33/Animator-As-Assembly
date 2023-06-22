using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class DIV : OPCODE
    {
        public Register A;
        public Register B;
        public Register Remainder;

        /// <summary> The final result of the multiplication, after adding all intermediates </summary>
        public Register Quotient;

        /// <summary> Divides a register by another. Quotient is stored in A, remainder is stored in B </summary>
        /// <param name="A"> The register to multiply </param>
        /// <param name="B"> The register to multiply by </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(Register A, Register B, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            init(A, B, Layer, progressWindow);
        }

        /// <summary> Divides a register by another. Quotient is stored in A, remainder is stored in B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(string[] args, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), new Register(args[1], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, Register B, AacFlLayer Layer, NestedProgressBar progressWindow)
        {
            this.A = A;
            this.B = B;
            this.Remainder = new Register("INTERNAL/DIV/REMAINDER", Layer);
            this.Quotient = new Register("INTERNAL/DIV/QUOTIENT", Layer);
            this.Layer = Layer.NewStateGroup("DIV");
            this.progressWindow = progressWindow;
        }

        /*
        Nieve implementation of division, using repeated subtraction
        Could be faster by using a different algorithm
        N is the numerator
        D is the denominator
        R := N
        Q := 0
        while R ≥ D do
            R := R − D
            Q := Q + 1
        end
        return (Q,R)
        */
        public override IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("DIV");
            ProgressBar PB = this.progressWindow.registerNewProgressBar("DIV", "");
            yield return PB.setProgress(0);

            AacFlState entry = Layer.NewState("DIV");
            AacFlState exit = Layer.NewState("DIV_EXIT");

            //copy the numerator into the remainder
            MOV mov = new MOV(A, Remainder, Layer, progressWindow);
            yield return mov.compile();
            yield return PB.setProgress(0.1f);

            //set the quotient to 0
            Quotient.Set(entry, 0);

            entry.AutomaticallyMovesTo(mov.entry);

            #region WHILE R >= D
            SUB sub = new SUB(Remainder, B, Remainder, Layer, progressWindow);
            yield return sub.compile();
            yield return PB.setProgress(0.2f);

            INC inc = new INC(Quotient, Layer, progressWindow);
            yield return inc.compile();
            yield return PB.setProgress(0.3f);

            //while R >= D
            JIG jig = new JIG(Remainder, B, Layer, progressWindow);
            yield return jig.compile();
            yield return PB.setProgress(0.4f);
            jig.Link(sub.entry);

            sub.exit.AutomaticallyMovesTo(inc.entry);
            inc.exit.AutomaticallyMovesTo(jig.entry);
            #endregion

            mov.exit.AutomaticallyMovesTo(jig.entry);

            MOV returnQ = new MOV(Quotient, A, Layer, progressWindow);
            yield return returnQ.compile();
            yield return PB.setProgress(0.7f);

            MOV returnR = new MOV(Remainder, B, Layer, progressWindow);
            yield return returnR.compile();
            yield return PB.setProgress(1f);

            jig.exit.AutomaticallyMovesTo(returnQ.entry);
            returnQ.exit.AutomaticallyMovesTo(returnR.entry);
            returnR.exit.AutomaticallyMovesTo(exit);

            PB.finish();
            Profiler.EndSample();
            callback(
                Util.CombineStates(
                    entry,
                    mov.states,
                    sub.states,
                    inc.states,
                    jig.states,
                    returnQ.states,
                    returnR.states,
                    exit
                )
            );
            yield break;
        }
    }
}
