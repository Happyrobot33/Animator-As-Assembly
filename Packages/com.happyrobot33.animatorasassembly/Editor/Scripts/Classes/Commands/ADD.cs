using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class ADD : OPCODE
    {
        public Register A;
        public Register B;
        public Register C;
        public AacFlBoolParameter CARRYOUT;
        public AacFlBoolParameter CARRYIN;
        public Register SUM;

        /// <summary> Adds two registers </summary>
        /// <remarks> The result is stored in the second register or in C </remarks>
        /// <param name="A"> The first register to add </param>
        /// <param name="B"> The second register to add </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public ADD(Register A, Register B, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, B, Layer, progressWindow);
        }

        /// <inheritdoc cref="ADD(Register, Register, AacFlLayer)"/>
        /// <param name="C"> The register to store the result in </param>
        public ADD(
            Register A,
            Register B,
            Register C,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            Init(A, B, Layer, progressWindow, C);
        }

        /// <summary> Adds two registers </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public ADD(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            if (args.Length == 2)
            {
                Init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    progressWindow
                );
            }
            else
            {
                Init(
                    new Register(args[0], Layer),
                    new Register(args[1], Layer),
                    Layer,
                    progressWindow,
                    new Register(args[2], Layer)
                );
            }
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(
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
            this._layer = Layer.NewStateGroup("ADD");
            CARRYOUT = Layer.BoolParameter("INTERNAL/ADD/CARRY");
            SUM = new Register("INTERNAL/ADD/SUM", Layer);
            this._progressWindow = progressWindow;
            this.CARRYIN = Globals._False;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register", "register" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("ADD");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("ADD", "");
            yield return PB.SetProgress(0f);
            //entry state
            AacFlState entry = _layer.NewState("EIGHTBITADDER");
            //clear sum and carry registers
            entry.Drives(CARRYOUT, false);
            SUM.Set(entry, 0);

            //exit state
            AacFlState exit = _layer.NewState("EIGHTBITADDER EXIT");

            FULLADDER[] FullAdders = new FULLADDER[Register._bitDepth];

            for (int j = 0; j < Register._bitDepth; j++)
            {
                Profiler.BeginSample("INTERNAL/ADD/FULLADDER " + j);
                /// <summary> The previous carry bit </summary>
                AacFlBoolParameter prevcarry = CARRYIN;
                if (j > 0)
                {
                    //copy prevcarry into our own register so it isnt cleared
                    prevcarry = _layer.BoolParameter("ADD/PREV_CARRY");
                    FullAdders[j - 1].Exit.DrivingCopies(FullAdders[j - 1].CARRY, prevcarry);
                }
                // create a FullAdder for each bit
                FULLADDER adder = new FULLADDER(A[j], B[j], prevcarry, _layer, _progressWindow);
                yield return adder;
                //copy the sum bit to the output register
                adder.Exit.DrivingCopies(adder.SUM, SUM[j]);

                FullAdders[j] = adder;
                Profiler.EndSample();
                yield return PB.SetProgress((float)j / Register._bitDepth);
            }

            //set the carry bit if the last FullAdder has a carry bit
            FullAdders[FullAdders.Length - 1].carryCalc.Drives(CARRYOUT, true);

            //use a MOV to copy the sum register to the C register
            MOV mov = new MOV(SUM, C, _layer, _progressWindow);
            yield return mov;

            //link the full adders together
            entry.AutomaticallyMovesTo(FullAdders[0].Entry);
            for (int j = 0; j < Register._bitDepth - 1; j++)
            {
                FullAdders[j].Exit.AutomaticallyMovesTo(FullAdders[j + 1].Entry);
            }
            FullAdders[Register._bitDepth - 1].Exit.AutomaticallyMovesTo(mov.Entry);
            mov.Exit.AutomaticallyMovesTo(exit);

            PB.Finish();

            Profiler.EndSample();
            callback(
                Util.CombineStates(
                    entry,
                    FullAdders,
                    mov,
                    exit
                )
            );
            yield break;
        }
    }
}
