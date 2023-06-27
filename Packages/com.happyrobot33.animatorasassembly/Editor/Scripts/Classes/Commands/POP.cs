using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class POP : OPCODE
    {
        public AacFlIntParameter value;

        /// <summary> Pops a int off the stack </summary>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public POP(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(Layer, progressWindow);
        }

        /// <summary> Increments a register </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        public POP(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(Layer, progressWindow);
        }
#pragma warning restore RCS1163, IDE0060 // Unused parameter.

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this._layer = Layer.NewStateGroup("POP");
            this._progressWindow = progressWindow;
            this.value = Layer.IntParameter("INTERNAL/POP/Value");
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("POP");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("POP", "");
            yield return PB.SetProgress(0);

            AacFlState entry = _layer.NewState("POP");
            AacFlState exit = _layer.NewState("POP");

            //copy from the stack to the buffer in the entry state
            for (int i = 0; i < Globals._StackSize - 1; i++)
            {
                entry.DrivingCopies(Globals._Stack[i + 1], Globals._StackBuffer[i]);
                yield return PB.SetProgress((float)i / (Globals._StackSize - 1) / 2);
            }

            //reset the last value in the stack
            entry.Drives(Globals._StackBuffer[Globals._StackSize - 1], -1);

            //copy the lowest to the value
            entry.DrivingCopies(Globals._Stack[0], value);

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
