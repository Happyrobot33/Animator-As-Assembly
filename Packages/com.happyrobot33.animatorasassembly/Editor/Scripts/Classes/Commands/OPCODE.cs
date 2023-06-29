using AnimatorAsCode.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace AnimatorAsAssembly.Commands
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    /// <summary> Base class for all commands.
    /// Provides entry and exit states for the command automatically
    /// </summary>
    public abstract class OPCODE
    {
        /// <summary> The ID of this opcode. This is used to identify the opcode in a list </summary>
        public int ID
        {
            get { return this.GetHashCode(); }
        }

        /// <summary> The progress window of the compiler </summary>
        internal ComplexProgressBar _progressWindow;

        /// <summary> The states that make up this opcode. May contain states from other nested opcodes </summary>
        public AacFlState[] States;

        /// <summary> Get the individual states of this opcode by index </summary>
        /// <param name="i"> The index of the state to get </param>
        /// <returns> The AacFlState at the given index </returns>
        public AacFlState this[int index]
        {
            get { return States[index]; }
        }

        /// <summary> Get the length of the states in this opcode </summary>
        public int Length
        {
            get { return States.Length; }
        }

        /// <summary> The FX layer that this command is linked to </summary>
        internal AacFlLayer _layer;
        internal AacFlBase _base;

        /// <summary> The entry state for this opcode </summary>
        public AacFlState Entry
        {
            get { return States[0]; }
        }

        /// <summary> The exit state for this opcode </summary>
        public AacFlState Exit
        {
            get { return States[States.Length - 1]; }
        }

        /// <summary> This is set before linking, it is a list reference to the entire program </summary>
        public List<OPCODE> ReferenceProgram;

        /// <summary> Finds the index of this opcode in the provided list </summary>
        /// <returns> The index of this opcode in the list </returns>
        internal int FindSelfInProgram()
        {
            //find self in list using the ID
            return ReferenceProgram.FindIndex(x => x.ID == this.ID);
        }

        #region Linking
        /// <summary> This is the default linker. This is overridable by inheritors to provide different behaviour </summary>
        public virtual void Linker()
        {
            LinkToPrevious();
            DriveProgramCounter();
        }

        /// <summary> Drives the program counter to the location of this opcode </summary>
        internal void DriveProgramCounter()
        {
            //ensure the program counter is what it should be
            Entry.Drives(Globals._ProgramCounter, FindSelfInProgram());
        }

        /// <summary> This links the previous opcode to this one. </summary>
        internal void LinkToPrevious()
        {
            int index = FindSelfInProgram();

            //link the previous opcode to this one
            //skip if this is the first opcode
            if (index != 0)
            {
                ReferenceProgram[index - 1].Exit.AutomaticallyMovesTo(Entry);
            }
        }

        /// <summary> Will link a provided state to the provided LBL name </summary>
        /// <remarks> This isolates the LBL to the subroutine it is in, which prevents breaking the call stack </remarks>
        /// <param name="state"> The state to link to the LBL </param>
        /// <param name="LBLname"> The name of the LBL to link to </param>
        internal void LinkToLBL(AacFlState state, string LBLname)
        {
            List<OPCODE> localOpcodes = GetLocalOpcodes();

            //find the LBL
            //if we are in a subroutine, contain ourselves to that subroutine
            foreach (OPCODE opcode in localOpcodes)
            {
                if (opcode.GetType() == typeof(LBL))
                {
                    LBL lbl = (LBL)opcode;
                    if (lbl.name == LBLname)
                    {
                        //transition to the LBL
                        state.AutomaticallyMovesTo(lbl.Entry);
                        break;
                    }
                }
            }
        }

        private List<OPCODE> GetLocalOpcodes()
        {
            int index = FindSelfInProgram();

            //find the subroutine that this LBL is in
            int subroutineStart = 0;
            int subroutineEnd = ReferenceProgram.Count - 1;
            //first determine if we are in a subroutine by checking up
            for (int i = index; i >= 0; i--)
            {
                if (ReferenceProgram[i].GetType() == typeof(SBR))
                {
                    //we are in a subroutine
                    //set the start of the subroutine to the index of the SUB
                    subroutineStart = i;
                    break;
                }
            }

            //find the end of the subroutine
            for (int i = index; i < ReferenceProgram.Count; i++)
            {
                if (ReferenceProgram[i].GetType() == typeof(RTS) || ReferenceProgram[i].GetType() == typeof(SBR))
                {
                    //we are in a subroutine
                    //set the start of the subroutine to the index of the SUB
                    subroutineEnd = i;
                    break;
                }
            }

            //get the local opcodes
            List<OPCODE> localOpcodes = new List<OPCODE>();
            for (int i = subroutineStart; i <= subroutineEnd; i++)
            {
                localOpcodes.Add(ReferenceProgram[i]);
            }

            return localOpcodes;
        }
        #endregion

        internal void Init(params object[] args)
        {
            throw new NotImplementedException(
                "The init method is not implemented for this opcode!"
            );
        }

        public virtual IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            throw new NotImplementedException(
                "The STATES method is not implemented for this opcode!"
            );
        }

        public override bool Equals(object obj)
        {
            return obj is OPCODE opCode && ID == opCode.ID;
        }

        #region Conversions
        //implicit conversion to AacFlState[] returns the states array
        /// <summary> Implicit conversion to AacFlState[] returns the states array </summary>
        public static implicit operator AacFlState[](OPCODE opCode)
        {
            if (opCode.States == null)
            {
                throw new Exception("States have not been compiled yet!");
            }
            return opCode.States;
        }

        public static implicit operator List<AacFlState>(OPCODE opCode)
        {
            if (opCode.States == null)
            {
                throw new Exception("States have not been compiled yet!");
            }
            return opCode.States.ToList();
        }

        //implicit conversion to a EditorCoroutine starts a coroutine to compile the opcode
        /// <summary> Compile the opcode into states, this is not done at object creation since it needs to be async </summary>
        public static implicit operator EditorCoroutine(OPCODE opCode)
        {
            return EditorCoroutineUtility.StartCoroutineOwnerless(
                opCode.GenerateStates((AacFlState[] states) => opCode.States = states)
            );
        }
        #endregion
    }
}
