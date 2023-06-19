using AnimatorAsCode.Framework;
using System.Linq;
using System.Collections.Generic;

namespace AnimatorAsAssembly.Commands
{
    public class MUL : OPCODE
    {
        public Register A;
        public Register B;

        /// <summary> A internal register that stores the intermediate results of the multiplication </summary>
        Register Intermediate;

        /// <summary> The final result of the multiplication, after adding all intermediates </summary>
        public Register Result;

        /// <summary> Multiplies a register by another. Result is stored in A </summary>
        /// <param name="A"> The register to multiply </param>
        /// <param name="B"> The register to multiply by </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public MUL(Register A, Register B, AacFlLayer Layer)
        {
            this.A = A;
            this.B = B;
            this.Layer = Layer;
            states = STATES();
        }

        // Binary multiplication is complicated
        // Essentially, for each bit in A, we multiply it by each bit in B
        /*        1011   (this is binary for decimal 11)
                x 1110   (this is binary for decimal 14)
                ======
                  0000   (this is 1011 x 0)
                 1011    (this is 1011 x 1, shifted one position to the left)
                1011     (this is 1011 x 1, shifted two positions to the left)
             + 1011      (this is 1011 x 1, shifted three positions to the left)
             =========
              10011010   (this is binary for decimal 154) */
        AacFlState[] STATES()
        {
            AacFlState entry = Layer.NewState("MUL");
            AacFlState exit = Layer.NewState("MUL_EXIT");

            //create the intermediate register
            Intermediate = new Register("Intermediate", Layer);

            //create the result register
            Result = new Register("Result", Layer);

            List<AacFlState> interstates = new List<AacFlState>();
            for (int i = 0; i < Register.bits; i++)
            {
                //create a new intermediate state
                //this state determines if to add or skip the intermediate result
                AacFlState interSplit = Layer.NewState("MUL_INTERMEDIATE_" + i + "_SPLIT");
                //link to the one before
                if (i > 0)
                {
                    interstates.Last().AutomaticallyMovesTo(interSplit);
                }
                else
                {
                    entry.AutomaticallyMovesTo(interSplit);
                }

                AacFlState interExit = Layer.NewState("MUL_INTERMEDIATE_" + i + "_EXIT");

                //define the intermediate register
                MOV mov = new MOV(A, Intermediate, Layer);

                //shift the intermediate register by i bits
                SHL shl = new SHL(Intermediate, Layer, i);

                //mov in nothing if the bit is 0
                AacFlState mul0 = Layer.NewState("MUL_INTERMEDIATE_" + i + "_0");
                mul0.AutomaticallyMovesTo(interExit);
                for (int j = 0; j < Register.bits; j++)
                {
                    mul0.Drives(Intermediate[j], false);
                }

                //add intermediate to the result
                ADD add = new ADD(Intermediate, Result, Layer);

                interSplit.TransitionsTo(mul0).When(B[i].IsFalse());
                interSplit.TransitionsTo(mov.entry).When(B[i].IsTrue());
                mov.exit.AutomaticallyMovesTo(shl.entry);
                shl.exit.AutomaticallyMovesTo(add.entry);
                add.exit.AutomaticallyMovesTo(interExit);

                interstates.Add(interSplit);
                interstates.Add(mul0);
                interstates.AddRange(mov.states);
                interstates.AddRange(shl.states);
                interstates.AddRange(add.states);
                interstates.Add(interExit);
            }

            MOV movToResult = new MOV(Result, A, Layer);
            interstates.Last().AutomaticallyMovesTo(movToResult.entry);
            movToResult.exit.AutomaticallyMovesTo(exit);

            return Util.ConcatArrays(entry, interstates.ToArray(), movToResult.states, exit);
        }
    }
}
