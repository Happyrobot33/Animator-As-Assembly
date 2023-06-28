using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class FULLADDER : OPCODE
    {
        public AacFlBoolParameter A;
        public AacFlBoolParameter B;
        public AacFlBoolParameter C;
        public AacFlBoolParameter SUM;
        public AacFlBoolParameter CARRY;
        public AacFlState carry;
        public AacFlState sumAndCarry;

        /// <summary> Adds two bits and a carry bit </summary>
        /// <param name="A"> The first bit to add </param>
        /// <param name="B"> The second bit to add </param>
        /// <param name="C"> The carry bit to add </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public FULLADDER(
            AacFlBoolParameter A,
            AacFlBoolParameter B,
            AacFlBoolParameter C,
            AacFlLayer Layer,
            ComplexProgressBar progressWindow
        )
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this._layer = Layer.NewStateGroup("FULLADDER");
            SUM = Layer.BoolParameter("INTERNAL/FULLADDER/SUM");
            CARRY = Layer.BoolParameter("INTERNAL/FULLADDER/CARRY");
            this._progressWindow = progressWindow;
        }

        // previous iterations of this microcode used individual half adders
        // this new version hardcodes the truth table for a full adder as it actually saves cycles, and isnt too complicated
        //
        // A   B   C   SUM CARRY
        // 0   0   0   0   0
        // 0   0   1   1   0
        // 0   1   0   1   0
        // 0   1   1   0   1
        // 1   0   0   1   0
        // 1   0   1   0   1
        // 1   1   0   0   1
        // 1   1   1   1   1
        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("FULLADDER");
            //entry state
            AacFlState entry = _layer.NewState("FULLADDER");
            AacFlState exit = _layer.NewState("FULLADDER/EXIT");
            entry.Drives(SUM, false);
            entry.Drives(CARRY, false);

            AacFlState sum = _layer.NewState("FULLADDER/SUM");
            sum.Drives(SUM, true);
            this.carry = _layer.NewState("FULLADDER/CARRY");
            carry.Drives(CARRY, true);
            this.sumAndCarry = _layer.NewState("FULLADDER/SUMANDCARRY");
            sumAndCarry.Drives(SUM, true).Drives(CARRY, true);

            //A = 0, B = 0, C = 0
            entry.TransitionsTo(exit).When(A.IsFalse()).And(B.IsFalse()).And(C.IsFalse());
            //SUM
            entry.TransitionsTo(sum).When(A.IsFalse()).And(B.IsFalse()).And(C.IsTrue());
            entry.TransitionsTo(sum).When(A.IsFalse()).And(B.IsTrue()).And(C.IsFalse());
            entry.TransitionsTo(sum).When(A.IsTrue()).And(B.IsFalse()).And(C.IsFalse());
            //CARRY
            entry.TransitionsTo(carry).When(A.IsFalse()).And(B.IsTrue()).And(C.IsTrue());
            entry.TransitionsTo(carry).When(A.IsTrue()).And(B.IsFalse()).And(C.IsTrue());
            entry.TransitionsTo(carry).When(A.IsTrue()).And(B.IsTrue()).And(C.IsFalse());
            //SUM AND CARRY
            entry.AutomaticallyMovesTo(sumAndCarry);

            //transitions out
            sum.AutomaticallyMovesTo(exit);
            carry.AutomaticallyMovesTo(exit);
            sumAndCarry.AutomaticallyMovesTo(exit);

            Profiler.EndSample();
            callback(
                Util.CombineStates(
                    entry,
                    sum,
                    carry,
                    sumAndCarry,
                    exit
                )
            );
            yield break;
        }
    }
}
