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

            //find the subroutine that this LBL is in
            int subroutineStart = 0;
            int subroutineEnd = opcodes.Count - 1;
            //first determine if we are in a subroutine by checking up
            for (int i = index; i >= 0; i--)
            {
                if (opcodes[i].GetType() == typeof(SBR))
                {
                    //we are in a subroutine
                    //set the start of the subroutine to the index of the SUB
                    subroutineStart = i;
                    break;
                }
            }

            //find the end of the subroutine
            for (int i = index; i < opcodes.Count; i++)
            {
                if (opcodes[i].GetType() == typeof(RTS))
                {
                    //we are in a subroutine
                    //set the start of the subroutine to the index of the SUB
                    subroutineEnd = i;
                    break;
                }
            }

            //find the LBL
            //if we are in a subroutine, contain ourselves to that subroutine
            foreach (OPCODE opcode in opcodes)
            {
                if (opcode.GetType() == typeof(LBL))
                {
                    //check if it is within the subroutine
                    if (opcodes.IndexOf(opcode) < subroutineStart || opcodes.IndexOf(opcode) > subroutineEnd)
                    {
                        //not in the subroutine
                        continue;
                    }
                    LBL lbl = (LBL)opcode;
                    if (lbl.name == name)
                    {
                        //transition to the LBL
                        Entry.AutomaticallyMovesTo(lbl.Entry);

                        //set the program counter to the index of the LBL
                        Entry.Drives(Globals._ProgramCounter, opcodes.IndexOf(lbl));
                        break;
                    }
                }
            }
        }
    }
}
