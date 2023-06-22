using AnimatorAsCode.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace AnimatorAsAssembly.Commands
{
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
        internal NestedProgressBar progressWindow;

        /// <summary> The states that make up this opcode. May contain states from other nested opcodes </summary>
        public AacFlState[] states;

        /// <summary> Get the individual states of this opcode by index </summary>
        /// <param name="i"> The index of the state to get </param>
        /// <returns> The AacFlState at the given index </returns>
        public AacFlState this[int index]
        {
            get { return states[index]; }
        }

        /// <summary> Get the length of the states in this opcode </summary>
        public int Length
        {
            get { return states.Length; }
        }

        /// <summary> The FX layer that this command is linked to </summary>
        internal AacFlLayer Layer;

        /// <summary> The entry state for this opcode </summary>
        public AacFlState entry
        {
            get { return states[0]; }
        }

        /// <summary> The exit state for this opcode </summary>
        public AacFlState exit
        {
            get { return states[states.Length - 1]; }
        }

        /// <summary> This links this opcode to the previous opcode. This is overridable by inheritors to provide different behaviour </summary>
        public virtual void Link(List<OPCODE> opcodes)
        {
            //find self in list using the ID
            int index = opcodes.FindIndex(x => x.ID == this.ID);

            //link the previous opcode to this one
            //skip if this is the first opcode
            if (index != 0)
            {
                opcodes[index - 1].exit.AutomaticallyMovesTo(entry);
            }

            //ensure the program counter increments
            entry.DrivingIncreases(Globals.PROGRAMCOUNTER, 1);
        }

        internal void init(params object[] args)
        {
            throw new NotImplementedException(
                "The init method is not implemented for this opcode!"
            );
        }

        public virtual EditorCoroutine compile()
        {
            return EditorCoroutineUtility.StartCoroutineOwnerless(
                STATES(
                    (AacFlState[] states) =>
                    {
                        this.states = states;
                    }
                )
            );
        }

        public virtual IEnumerator<EditorCoroutine> STATES(Action<AacFlState[]> callback)
        {
            throw new NotImplementedException(
                "The STATES method is not implemented for this opcode!"
            );
        }
    }
}
