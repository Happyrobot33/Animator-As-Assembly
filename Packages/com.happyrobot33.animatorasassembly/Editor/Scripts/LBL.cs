using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimatorAsAssembly
{
    /// <summary> A label is a name and a line number </summary>
    public class LBL
    {
        /// <summary> The name of the label </summary>
        public string Name;

        /// <summary> The line number of the label </summary>
        public int Line;

        /// <summary> Create a new label </summary>
        /// <param name="name">The name of the label</param>
        /// <param name="LineNumber">The line number of the label</param>
        public LBL(string name, int LineNumber)
        {
            this.Name = name;
            this.Line = LineNumber;
        }
    }
}
