using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;

namespace AnimatorAsAssembly.Commands
{
    public class LD : OPCODE
    {
        public Register A;
        public int value;

        /// <summary> Loads a register with a int value </summary>
        /// <param name="A"> The register to load into </param>
        /// <param name="value"> The value to load into the register </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LD(Register A, int value, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, value, Layer, (object)progressWindow);
        }

        /// <summary> Loads a register with a int value </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LD(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(new Register(args[0], Layer), int.Parse(args[1]), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, int value, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("LD");
            //truncate the value to fit in the register's bit count
            this.value = value & ((1 << Register.bits) - 1);
            this._progressWindow = progressWindow;
            //states = STATES();
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("LD");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("LD", "");
            AacFlState entry = _layer.NewState("LD");
            for (int i = 0; i < Register.bits; i++)
            {
                bool bitValue = (value & (1 << i)) != 0;
                entry.Drives(A[i], bitValue);
                yield return PB.SetProgress((float)i / Register.bits);
            }
            PB.Finish();
            Profiler.EndSample();
            callback(new AacFlState[] { entry });
            yield break;
            //return new AacFlState[] { entry };
        }
    }
}
