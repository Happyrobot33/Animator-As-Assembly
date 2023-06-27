using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;

namespace AnimatorAsAssembly.Commands
{
    public class NOP : OPCODE
    {
        /// <summary> Does nothing for a line </summary>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public NOP(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(Layer, progressWindow);
        }

        /// <summary> Does nothing for a line </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        public NOP(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(Layer, progressWindow);
        }
#pragma warning restore RCS1163, IDE0060 // Unused parameter.

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this._layer = Layer.NewStateGroup("NOP");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("NOP");
            AacFlState entry = _layer.NewState("NOP");

            yield return null;
            Profiler.EndSample();
            callback(new AacFlState[] { entry });
        }
    }
}
