using AnimatorAsCode.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine.Profiling;

namespace AnimatorAsAssembly
{
    public static class Util
    {
        //thank god for chatGPT for this one
        /// <summary> Concatenates states into one single AacFlState array </summary>
        /// <param name="objects">The arrays/AacFlState's to concatenate. Both are accepted</param>
        /// <returns>The concatenated array</returns>
        public static AacFlState[] CombineStates(params object[] objects)
        {
            Profiler.BeginSample("Combine States");
            object[][] arrays = objects
                .Select(x => x is object[] ? (object[])x : new object[] { x })
                .ToArray();
            Profiler.EndSample();
            return arrays.Aggregate((a, b) => a.Concat(b).ToArray()).Cast<AacFlState>().ToArray();
        }
    }

    /// <summary> This class contains all the global const variables used in the controller </summary>
    public class Globals
    {
        /// <summary> A permanent reference to a false boolean value </summary>
        public static AacFlBoolParameter FALSE;

        /// <summary> A permanent reference to a true boolean value </summary>
        public static AacFlBoolParameter TRUE;

        /// <summary> A permanent reference to the number 1 in Register form </summary>
        public static Register ONE;

        /// <summary> A permanent reference to the program counter variable </summary>
        public static AacFlIntParameter PROGRAMCOUNTER;
        public const string PROGRAMCOUNTERSTRING = "INTERNAL/PC";

        /// <summary> Create a new Globals object </summary>
        /// <param name="FX">The AacFlLayer to use</param>
        public Globals(AacFlLayer FX)
        {
            FALSE = FX.BoolParameter("GLOBALS/FALSE");
            TRUE = FX.BoolParameter("GLOBALS/TRUE");
            FX.OverrideValue(TRUE, true);
            ONE = new Register("GLOBALS/ONE", FX);
            ONE.initialize(1);
            PROGRAMCOUNTER = FX.IntParameter(PROGRAMCOUNTERSTRING);
        }
    }
}
