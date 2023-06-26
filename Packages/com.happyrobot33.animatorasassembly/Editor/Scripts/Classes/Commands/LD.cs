using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;

namespace AnimatorAsAssembly.Commands
{
    public class LD : OPCODE
    {
        public Register A;
        public int value;

        /// <summary> Loads a register with a int value </summary>
        /// <param name="A"> The register to load into </param>
        /// <param name="value"> The value to load into the register </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LD(Register A, int value, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(A, value, Layer, (object)progressWindow);
        }

        /// <summary> Loads a register with a int value </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public LD(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            string type = "";
            if (args.Length >= 3)
            {
                type = args[2];
            }
            else
            {
                type = "uint";
            }

            switch (type)
            {
                case "int":
                    //offset the value by half the maximum unsigned int value
                    int maxValue = (int)Math.Pow(2, Register._bitDepth);
                    int value = int.Parse(args[1]) + (maxValue / 2);
                    Init(new Register(args[0], Layer), value, Layer, progressWindow);
                    break;
                case "uint":
                    Init(new Register(args[0], Layer), int.Parse(args[1]), Layer, progressWindow);
                    break;
                default:
                    throw new Exception("Invalid type for LD command");
            }
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register A, int value, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.A = A;
            this._layer = Layer.NewStateGroup("LD");
            this.value = value;
            this._progressWindow = progressWindow;
            //states = STATES();
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("LD");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("LD", "");
            AacFlState entry = _layer.NewState("LD");

            yield return EditorCoroutineUtility.StartCoroutineOwnerless(A.Set(entry, value, PB));

            PB.Finish();
            Profiler.EndSample();
            callback(new AacFlState[] { entry });
        }
    }
}
