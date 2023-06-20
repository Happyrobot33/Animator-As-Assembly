using AnimatorAsCode.Framework;
using System.Linq;
using System.Collections.Generic;
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

        /// <summary> Divides a register by another. Result is stored in A, remainder is available in "INTERNAL/DIV/REMAINDER" </summary>
        /// <param name="A"> The register to multiply </param>
        /// <param name="B"> The register to multiply by </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(Register A, Register B, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.Remainder = new Register("INTERNAL/DIV/REMAINDER", Layer);
            this.Quotient = new Register("INTERNAL/DIV/QUOTIENT", Layer);
            this.Layer = Layer;
            states = STATES();
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
        AacFlState[] STATES()
        {
            Profiler.BeginSample("DIV");
            AacFlState entry = Layer.NewState("DIV");
            AacFlState exit = Layer.NewState("DIV_EXIT");

            //copy the numerator into the remainder
            MOV mov = new MOV(A, Remainder, Layer);

            //set the quotient to 0
            Quotient.Set(entry, 0);

            Profiler.EndSample();
            return new AacFlState[] { };
        }
    }
}
