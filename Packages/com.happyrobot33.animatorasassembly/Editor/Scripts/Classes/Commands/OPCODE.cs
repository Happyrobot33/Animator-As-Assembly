using AnimatorAsCode.Framework;
using System.ComponentModel.DataAnnotations;

namespace AnimatorAsAssembly.Commands
{
    /// <summary> Base class for all commands.
    /// Provides entry and exit states for the command automatically
    /// </summary>
    public abstract class OPCODE
    {
        /// <summary> The states that make up this opcode. May contain states from other nested opcodes </summary>
        public AacFlState[] states;

        /// <summary> The FX layer that this command is linked to </summary>
        //[Required(ErrorMessage = "Layer is required")]
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
    }
}
