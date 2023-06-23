using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class LBL : OPCODE
    {
        public string name;

        /// <summary> Denotes a LBL </summary>
        /// <param name="name"> The name of the LBL </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LBL(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(name, Layer, progressWindow);
        }

        /// <summary> Denotes a LBL </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LBL(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(args[0], Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.name = name;
            this._layer = Layer.NewStateGroup("LBL");
            this._progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("LBL");

            //dummy state
            AacFlState state = _layer.NewState("LBL " + name);

            Profiler.EndSample();
            callback(new AacFlState[] { state });
            yield break;
        }
    }
}
