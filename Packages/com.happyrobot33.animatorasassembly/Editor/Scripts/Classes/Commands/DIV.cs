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

        /// <summary> Divides a register by another. Quotient is stored in A, remainder is stored in B </summary>
        /// <param name="A"> The register to multiply </param>
        /// <param name="B"> The register to multiply by </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(Register A, Register B, AacFlLayer Layer)
        {
            init(A, B, Layer);
        }

        /// <summary> Divides a register by another. Quotient is stored in A, remainder is stored in B </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DIV(string[] args, AacFlLayer Layer)
        {
            //split the args into the register and the value
            init(new Register(args[0], Layer), new Register(args[1], Layer), Layer);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void init(Register A, Register B, AacFlLayer Layer)
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

            //decrement the denominator into a temp register
            //this allows for R to equal D
            //Register Btemp = new Register("INTERNAL/DIV/TEMP", Layer);
            //MOV mov2 = new MOV(B, Btemp, Layer);
            //DEC dec = new DEC(Btemp, Layer);

            entry.AutomaticallyMovesTo(mov.entry);
            //mov.exit.AutomaticallyMovesTo(mov2.entry);
            //mov2.exit.AutomaticallyMovesTo(dec.entry);

            #region WHILE R >= D
            SUB sub = new SUB(Remainder, B, Remainder, Layer);
            INC inc = new INC(Quotient, Layer);

            //while R >= D
            JIG jig = new JIG(Remainder, B, Layer);
            jig.Link(sub.entry);

            sub.exit.AutomaticallyMovesTo(inc.entry);
            inc.exit.AutomaticallyMovesTo(jig.entry);
            #endregion

            mov.exit.AutomaticallyMovesTo(jig.entry);

            MOV returnQ = new MOV(Quotient, A, Layer);
            MOV returnR = new MOV(Remainder, B, Layer);

            jig.exit.AutomaticallyMovesTo(returnQ.entry);
            returnQ.exit.AutomaticallyMovesTo(returnR.entry);
            returnR.exit.AutomaticallyMovesTo(exit);

            Profiler.EndSample();
            return Util.ConcatArrays(
                entry,
                mov.states,
                sub.states,
                inc.states,
                jig.states,
                returnQ.states,
                returnR.states,
                exit
            );
        }
    }
}
