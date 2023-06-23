using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class SBR : OPCODE
    {
        public string name;

        /// <summary> Denotes a SBR </summary>
        /// <param name="name"> The name of the SBR </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SBR(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(name, Layer, progressWindow);
        }

        /// <summary> Denotes a SBR </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public SBR(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(args[0], Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.name = name;
            this._layer = Layer.NewStateGroup("SBR");
            this._progressWindow = progressWindow;
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("SBR");

            //dummy state
            AacFlState state = _layer.NewState("SBR " + name);

            Profiler.EndSample();
            callback(new AacFlState[] { state });
            yield break;
        }

        //override the linker to do nothing
        public override void Link(List<OPCODE> opcodes)
        {
            //do nothing
        }
    }
}