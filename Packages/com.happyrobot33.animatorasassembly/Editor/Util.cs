using AnimatorAsCode.Framework;
using System.Linq;
using UnityEditor;

namespace AnimatorAsAssembly
{
    public static class Util
    {
        //set the second dimension of the array to the array
        /// <summary> Copies an array into a 2D array </summary>
        /// <param name="array">The 2D array to copy into</param>
        /// <param name="array2">The array to copy from</param>
        /// <param name="index">The index to copy into</param>
        /// <returns>The 2D array with the array copied into it</returns>
        public static AacFlState[,] CopyIntoArray(
            AacFlState[,] array,
            AacFlState[] array2,
            int index
        )
        {
            UnityEngine.Profiling.Profiler.BeginSample("CopyIntoArray [AacFlState]");
            //copy each element in, one by one

            //extend the second dimension of the array if it is too small
            if (array2.Length > array.GetLength(1))
            {
                AacFlState[,] newArray = new AacFlState[array.GetLength(0), array2.Length];
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    for (int j = 0; j < array.GetLength(1); j++)
                    {
                        newArray[i, j] = array[i, j];
                    }
                }
                array = newArray;
            }

            for (int i = 0; i < array2.Length; i++)
            {
                array[index, i] = array2[i];
            }

            UnityEngine.Profiling.Profiler.EndSample();
            return array;
        }

        /// <summary> Copies an value into an array </summary>
        /// <param name="array">The array to copy into</param>
        /// <param name="value">The value to copy</param>
        /// <returns>The array with the value copied into it</returns>
        public static Register[] CopyIntoArray(Register[] array, Register value)
        {
            UnityEngine.Profiling.Profiler.BeginSample("CopyIntoArray [Register]");
            //create a new array with a length of the old array + 1
            Register[] newArray = new Register[array.Length + 1];

            //copy each element in, one by one
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            //add the new value to the end of the array
            newArray[newArray.Length - 1] = value;

            UnityEngine.Profiling.Profiler.EndSample();
            return newArray;
        }

        /// <inheritdoc cref="CopyIntoArray(Register[], Register)"/>
        public static LBL[] CopyIntoArray(LBL[] array, LBL value)
        {
            UnityEngine.Profiling.Profiler.BeginSample("CopyIntoArray [LBL]");
            //create a new array with a length of the old array + 1
            LBL[] newArray = new LBL[array.Length + 1];

            //copy each element in, one by one
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            //add the new value to the end of the array
            newArray[newArray.Length - 1] = value;

            UnityEngine.Profiling.Profiler.EndSample();
            return newArray;
        }

        //thank god for chatGPT for this one
        /// <summary> Concatenates multiple arrays into one </summary>
        /// <param name="objects">The arrays to concatenate</param>
        /// <returns>The concatenated array</returns>
        public static AacFlState[] ConcatArrays(params object[] objects)
        {
            object[][] arrays = objects
                .Select(x => x is object[] ? (object[])x : new object[] { x })
                .ToArray();
            return arrays.Aggregate((a, b) => a.Concat(b).ToArray()).Cast<AacFlState>().ToArray();
        }
    }

    /// <summary> This class contains all the global const variables used in the animation </summary>
    public class Globals
    {
        public AacFlLayer FX;

        /// <summary> A permanent reference to a false boolean value </summary>
        public AacFlBoolParameter FALSE;

        /// <summary> A permanent reference to a true boolean value </summary>
        public AacFlBoolParameter TRUE;

        public Register ONE;

        /// <summary> Create a new Globals object </summary>
        /// <param name="FX">The AacFlLayer to use</param>
        public Globals(AacFlLayer FX)
        {
            this.FX = FX;
            FALSE = FX.BoolParameter("GLOBALS/FALSE");
            TRUE = FX.BoolParameter("GLOBALS/TRUE");
            ONE = new Register("GLOBALS/ONE", FX);
            ONE.initialize(1);
        }
    }
}
