using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class PUSH : OPCODE
    {
        public object A;

        /// <summary> Pushes a int to the stack </summary>
        /// <param name="A"> The value to push to the stack </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PUSH(int A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, Layer, progressWindow);
        }

        /// <summary> Pushes a parameter to the stack </summary>
        /// <param name="A"> The parameter to push to the stack </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PUSH(AacFlIntParameter A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, Layer, progressWindow);
        }

        /// <summary> Increments a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PUSH(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(int.Parse(args[0]), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(object A, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("PUSH");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "number" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("PUSH");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("PUSH", "");
            yield return PB.SetProgress(0);

            AacFlState entry = _layer.NewState("PUSH");
            AacFlState exit = _layer.NewState("PUSH");

            //copy from the stack to the buffer in the entry state
            for (int i = 0; i < Globals._StackSize - 1; i++)
            {
                entry.DrivingCopies(Globals._Stack[i], Globals._StackBuffer[i + 1]);
                yield return PB.SetProgress((float)i / (Globals._StackSize - 1) / 2);
            }

            //copy the value to the stack
            if (A is int x)
            {
                entry.Drives(Globals._StackBuffer[0], x);
            }
            else if (A is AacFlIntParameter aacFlIntParameter)
            {
                entry.DrivingCopies(aacFlIntParameter, Globals._StackBuffer[0]);
            }

            //copy from the buffer to the stack in the exit state
            for (int i = 0; i < Globals._StackSize - 1; i++)
            {
                exit.DrivingCopies(Globals._StackBuffer[i], Globals._Stack[i]);
                yield return PB.SetProgress(0.5f + ((float)i / (Globals._StackSize - 1) / 2));
            }

            entry.AutomaticallyMovesTo(exit);

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, exit));
        }
    }
}
