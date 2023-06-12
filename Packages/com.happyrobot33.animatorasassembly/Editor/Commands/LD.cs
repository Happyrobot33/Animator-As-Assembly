using AnimatorAsCode;
using AnimatorAsCode.Framework;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class LD
    {
        public AacFlState[] states;

        /// <summary> Loads a bit with a boolean value </summary>
        /// <param name="A"> The bit to load </param>
        /// <param name="value"> The value to load into the bit </param>
        /// <param name="FX"> The FX controller that this command is linked to </param>
        public LD(AacFlBoolParameter A, bool value, AacFlLayer FX)
        {
            states = STATES(A, value, FX);
        }

        AacFlState[] STATES(AacFlBoolParameter A, bool value, AacFlLayer FX)
        {
            AacFlState entry = FX.NewState("LD");
            entry.Drives(A, value);
            return new AacFlState[] { entry };
        }
    }
}
