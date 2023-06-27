using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;

namespace AnimatorAsAssembly.Commands
{
    public class CLEARSCREEN : OPCODE
    {
        /// <summary> clears the screen </summary>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public CLEARSCREEN(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(Layer, progressWindow);
        }

        /// <summary> clears the screen </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public CLEARSCREEN(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this._layer = Layer.NewStateGroup("CLEARSCREEN");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("CLEARSCREEN");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("DIV", "");
            yield return PB.SetProgress(0);
            AacFlState entry = _layer.NewState("CLEARSCREEN");

            for (int i = 0; i < Globals._PixelBuffer.Length; i++)
            {
                entry.Drives(Globals._PixelBuffer[i], 0);
                yield return PB.SetProgress((float)i / (float)Globals._PixelBuffer.Length);
            }

            PB.Finish();
            yield return null;
            Profiler.EndSample();
            callback(Util.CombineStates(entry));
        }
    }
}
