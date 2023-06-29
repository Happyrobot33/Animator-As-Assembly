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
    public class JMP : OPCODE
    {
        public string name;

        /// <summary> Jumps to a line </summary>
        /// <param name="name"> The name of the LBL to jump to</param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JMP(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(name, Layer, progressWindow);
        }

        /// <summary> Jumps to a line </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JMP(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(args[0], Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.name = name;
            this._layer = Layer.NewStateGroup("JMP");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "label" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("LBL");

            //dummy state
            AacFlState state = _layer.NewState("LBL " + name);

            Profiler.EndSample();
            callback(Util.CombineStates(state));
            yield break;
        }

        //override the linker to jump to the LBL instead
        public override void Linker()
        {
            LinkToPrevious();

            LinkToLBL(Entry, name);
        }
    }
}
