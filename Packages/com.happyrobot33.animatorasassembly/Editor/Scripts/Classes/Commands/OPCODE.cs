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

        /// <summary> This links this opcode to the previous opcode. This is overridable by inheritors to provide different behaviour </summary>
        public virtual void Link(List<OPCODE> opCodes)
        {
            //find self in list using the ID
            int index = opCodes.FindIndex(x => this == x);

            //link the previous opcode to this one
            //skip if this is the first opcode
            if (index != 0)
            {
                opCodes[index - 1].Exit.AutomaticallyMovesTo(Entry);
            }

            //ensure the program counter increments
            Entry.DrivingIncreases(Globals._ProgramCounter, 1);
        }

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
    }
}
