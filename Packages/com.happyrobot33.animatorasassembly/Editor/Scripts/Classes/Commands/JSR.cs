using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class JSR : OPCODE
    {
        AacFlState jumpState;
        public string name;

        /// <summary> Jumps to a subroutine </summary>
        /// <param name="name"> The name of the subroutine to jump to</param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JSR(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(name, Layer, progressWindow);
        }

        /// <summary> Jumps to a subroutine </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public JSR(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(args[0], Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.name = name;
            this._layer = Layer.NewStateGroup("JSR " + name);
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "subroutine" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("JSR");

            PUSH push = new PUSH(Globals._ProgramCounter, _layer, _progressWindow);
            yield return push;

            //enter state
            jumpState = _layer.NewState("JSR " + name);
            AacFlState exitState = _layer.NewState("JSR " + name + " Return Vector");
            exitState.DrivingIncreases(Globals._ProgramCounter, 1);

            push.Exit.AutomaticallyMovesTo(jumpState);

            Profiler.EndSample();
            callback(Util.CombineStates(push, jumpState, exitState));
            yield break;
        }

        //override the linker to jump to the subroutine instead
        public override void Linker()
        {
            LinkToPrevious();
            DriveProgramCounter();

            //find the subroutine
            foreach (OPCODE opcode in ReferenceProgram)
            {
                if (opcode.GetType() == typeof(SBR))
                {
                    SBR sbr = (SBR)opcode;
                    if (sbr.name == name)
                    {
                        //transition to the SBR
                        jumpState.AutomaticallyMovesTo(sbr.Entry);

                        //set the program counter to the index of the SBR
                        jumpState.Drives(Globals._ProgramCounter, ReferenceProgram.IndexOf(sbr));
                        break;
                    }
                }
            }
        }
    }
}
