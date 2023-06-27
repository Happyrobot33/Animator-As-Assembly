using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;

namespace AnimatorAsAssembly.Commands
{
    public class DELAY : OPCODE
    {
        int seconds = 0;
        /// <summary> Does nothing for a set ammount of seconds </summary>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DELAY(int frames, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(frames, Layer, progressWindow);
        }

        /// <summary> Does nothing for a set ammount of seconds </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public DELAY(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(int.Parse(args[0]), Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(int frames, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this._layer = Layer.NewStateGroup("DELAY");
            this.seconds = frames;
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "number" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("DELAY");
            AacFlState entry = _layer.NewState("DELAY").WithAnimation(_base.DummyClipLasting(seconds, AacFlUnit.Seconds));
            AacFlState exit = _layer.NewState("DELAY EXIT");

            entry.TransitionsTo(exit).AfterAnimationFinishes();

            yield return null;
            Profiler.EndSample();
            callback(Util.CombineStates(entry, exit));
        }
    }
}
