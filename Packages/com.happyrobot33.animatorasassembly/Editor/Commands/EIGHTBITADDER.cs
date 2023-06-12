using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;
using AnimatorAsAssembly;
using static AnimatorAsAssembly.Util.Globals;

namespace AnimatorAsAssembly.Commands
{
    public class EIGHTBITADDER
    {
        public AacFlState[] states;
        public Register A;
        public Register B;
        public AacFlBoolParameter CARRY;
        public Register SUM;
        AacFlLayer FX;

        public EIGHTBITADDER(Register A, Register B, AacFlLayer FX, int i = 0)
        {
            this.A = A;
            this.B = B;
            this.FX = FX;
            CARRY = FX.BoolParameter("EIGHTBITADDER/CARRY");
            SUM = new Register("EIGHTBITADDER/SUM", FX);
            states = STATES(i);
        }

        AacFlState[] STATES(int i)
        {
            //globals
            Globals globals = new Globals(FX);

            //entry state
            AacFlState entry = FX.NewState("EIGHTBITADDER");

            //exit state
            AacFlState exit = FX.NewState("EIGHTBITADDER EXIT");

            FULLADDER[] halfAdders = new FULLADDER[Register.bits];

            for (int j = 0; j < Register.bits; j++)
            {
                /// <summary> The previous carry bit </summary>
                AacFlBoolParameter prevcarry = globals.FALSE;
                if (j > 0)
                {
                    prevcarry = halfAdders[j - 1].CARRY;
                }
                // create a half adder for each bit
                halfAdders[j] = new FULLADDER(A[j], B[j], prevcarry, FX, j);
            }
        }
    }
}
