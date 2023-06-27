using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class FULLADDER : OPCODE
    {
        public AacFlBoolParameter A;
        public AacFlBoolParameter B;
        public AacFlBoolParameter C;
        public AacFlBoolParameter SUM;
        public AacFlBoolParameter CARRY;
        public AacFlState carryCalc;
        public AacFlState sumCalc;

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

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("FULLADDER");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("FULLADDER", "");
            yield return PB.SetProgress(0f);
            //entry state
            AacFlState entry = _layer.NewState("FULLADDER");
            entry.Drives(SUM, false);
            entry.Drives(CARRY, false);

            //first half adder
            HALFADDER firstHalfAdder = new HALFADDER(A, B, _layer, _progressWindow, 0);
            yield return firstHalfAdder;
            yield return PB.SetProgress(0.5f);

            //second half adder
            HALFADDER secondHalfAdder = new HALFADDER(
                firstHalfAdder.SUM,
                C,
                _layer,
                _progressWindow,
                1
            );
            yield return secondHalfAdder;
            yield return PB.SetProgress(1f);

            //set carry based on either half adders carry flag
            carryCalc = _layer.NewState("FULLADDER CARRY");
            carryCalc.Drives(CARRY, true);

            //exit state
            AacFlState exit = _layer.NewState("FULLADDER EXIT");
            exit.DrivingCopies(secondHalfAdder.SUM, SUM);

            //entry state
            entry.AutomaticallyMovesTo(firstHalfAdder.States[0]);
            firstHalfAdder.Exit.AutomaticallyMovesTo(secondHalfAdder.Entry);
            secondHalfAdder.Exit
                .TransitionsTo(carryCalc)
                .When(firstHalfAdder.CARRY.IsTrue())
                .Or()
                .When(secondHalfAdder.CARRY.IsTrue());
            carryCalc.AutomaticallyMovesTo(exit);
            secondHalfAdder.Exit.AutomaticallyMovesTo(exit);
            PB.Finish();

            Profiler.EndSample();
            callback(
                Util.CombineStates(
                    entry,
                    firstHalfAdder,
                    secondHalfAdder,
                    carryCalc,
                    exit
                )
            );
            yield break;
        }
    }
}
