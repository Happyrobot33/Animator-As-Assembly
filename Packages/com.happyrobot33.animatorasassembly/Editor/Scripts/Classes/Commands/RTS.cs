using AnimatorAsCode;
using AnimatorAsCode.Framework;
using System;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using AnimatorAsAssembly;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly.Commands
{
    public class RTS : OPCODE
    {
        /// <summary> Returns from subroutine </summary>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public RTS(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(Layer, progressWindow);
        }

        /// <summary> Returns from subroutine </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        public RTS(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(Layer, progressWindow);
        }
#pragma warning restore RCS1163, IDE0060 // Unused parameter.

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this._layer = Layer.NewStateGroup("RTS");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("RTS");

            //remove the last PC from the stack
            POP pop = new POP(_layer, _progressWindow);
            yield return pop;

            AacFlState jumpState = _layer.NewState("RTS PC");
            jumpState.DrivingCopies(pop.value, Globals._ProgramCounter);

            pop.Exit.AutomaticallyMovesTo(jumpState);

            Profiler.EndSample();
            callback(Util.CombineStates(pop.States, jumpState));
            yield break;
        }

        //override the linker to jump back to the JSR instead
        public override void Link(List<OPCODE> opcodes)
        {
            //find self in list using the ID
            int index = opcodes.FindIndex(x => x.ID == this.ID);

            //link the previous opcode to this one
            //skip if this is the first opcode
            if (index != 0)
            {
                opcodes[index - 1].Exit.AutomaticallyMovesTo(Entry);
            }

            //find our subroutine name by looking for the previous SBR
            string name = "";
            for (int i = index - 1; i >= 0; i--)
            {
                if (opcodes[i].GetType() == typeof(SBR))
                {
                    SBR sbr = (SBR)opcodes[i];
                    name = sbr.name;
                    break;
                }
            }

            //find all JSR opcodes that link to this RTS
            foreach (OPCODE opcode in opcodes)
            {
                if (opcode.GetType() == typeof(JSR))
                {
                    JSR jsr = (JSR)opcode;
                    if (jsr.name == name)
                    {
                        int jsrIndex = opcodes.FindIndex(x => x.ID == jsr.ID);
                        //transition to the SBR if the PC equals the index of the JSR
                        Exit.TransitionsTo(jsr.Exit).When(Globals._ProgramCounter.IsEqualTo(jsrIndex));
                    }
                }
            }
        }
    }
}
