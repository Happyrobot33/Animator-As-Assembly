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
        public Register ReturnQuotient;
        public Register ReturnRemainder;
        public Register Numerator;
        public Register Denominator;
        public Register Remainder;

        /// <summary> The final result of the multiplication, after adding all intermediates </summary>
        public Register Quotient;

        /// <summary> Divides a register by another. Quotient is stored in A, remainder is stored in B </summary>
        /// <param name="A"> The register to multiply </param>
        /// <param name="B"> The register to multiply by </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow, Register ReturnQuotient = null, Register ReturnRemainder = null)
        {
            Init(A, B, Layer, progressWindow, ReturnQuotient, ReturnRemainder);
        }

        /// <summary> Divides a register by another. Quotient is stored in A, remainder is stored in B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), new Register(args[1], Layer), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow, Register ReturnQuotient = null, Register ReturnRemainder = null)
        {
            this.Numerator = A;
            this.Denominator = B;

            //set these to A and B if they are null
            this.ReturnQuotient = ReturnQuotient ?? A;
            this.ReturnRemainder = ReturnRemainder ?? B;
            this.Remainder = new Register("INTERNAL/DIV/REMAINDER", Layer);
            this.Quotient = new Register("INTERNAL/DIV/QUOTIENT", Layer);
            this._layer = Layer.NewStateGroup("DIV");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register" };
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
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("DIV");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("DIV", "");
            yield return PB.SetProgress(0);

            AacFlState entry = _layer.NewState("DIV");
            AacFlState exit = _layer.NewState("DIV_EXIT");

            //copy the numerator into the remainder
            MOV mov = new MOV(Numerator, Remainder, _layer, _progressWindow);
            yield return mov;
            yield return PB.SetProgress(0.1f);

            //set the quotient to 0
            Quotient.Set(entry, 0);

            entry.AutomaticallyMovesTo(mov.Entry);

            #region WHILE R >= D
            SUB sub = new SUB(Remainder, Denominator, Remainder, _layer, _progressWindow);
            yield return sub;
            yield return PB.SetProgress(0.2f);

            INC inc = new INC(Quotient, _layer, _progressWindow);
            yield return inc;
            yield return PB.SetProgress(0.3f);

            //while R >= D
            JIF jif = new JIF(Remainder, Conditional.GreaterThanOrEqual, Denominator, _layer, _progressWindow);
            yield return jif;
            yield return PB.SetProgress(0.4f);
            jif.Link(sub.Entry);

            sub.Exit.AutomaticallyMovesTo(inc.Entry);
            inc.Exit.AutomaticallyMovesTo(jif.Entry);
            #endregion

            mov.Exit.AutomaticallyMovesTo(jif.Entry);

            MOV returnQ = new MOV(Quotient, ReturnQuotient, _layer, _progressWindow);
            yield return returnQ;
            yield return PB.SetProgress(0.7f);

            MOV returnR = new MOV(Remainder, ReturnRemainder, _layer, _progressWindow);
            yield return returnR;
            yield return PB.SetProgress(1f);

            jif.Exit.AutomaticallyMovesTo(returnQ.Entry);
            returnQ.Exit.AutomaticallyMovesTo(returnR.Entry);
            returnR.Exit.AutomaticallyMovesTo(exit);

            PB.Finish();
            Profiler.EndSample();
            callback(
                Util.CombineStates(
                    entry,
                    mov,
                    sub,
                    inc,
                    jif,
                    returnQ,
                    returnR,
                    exit
                )
            );
            yield break;
        }
    }
}
