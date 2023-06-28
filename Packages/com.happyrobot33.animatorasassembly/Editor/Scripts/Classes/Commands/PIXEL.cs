using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class PIXEL : OPCODE
    {
        public Register X;
        public Register Y;

        /// <summary> Draws a pixel at a X Y position </summary>
        /// <param name="X"> The X position of the pixel </param>
        /// <param name="Y"> The Y position of the pixel </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PIXEL(Register X, Register Y, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(X, Y, Layer, progressWindow);
        }

        /// <summary> Draws a pixel at a X Y position </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PIXEL(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            //split the args into the register and the value
            Init(
                new Register(args[0], Layer),
                new Register(args[1], Layer),
                Layer,
                progressWindow
            );
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(Register X, Register Y, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.X = X;
            this.Y = Y;
            this._layer = Layer.NewStateGroup("PIXEL");
            this._progressWindow = progressWindow;
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "register", "register" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("PIXEL");
            ProgressBar PB = this._progressWindow.RegisterNewProgressBar("PIXEL", "");
            AacFlState entry = _layer.NewState("PIXEL");
            AacFlState exit = _layer.NewState("PIXEL EXIT");

            //we need to have a state for each pixel
            //so we need to loop through all the pixels
            AacFlState[] states = new AacFlState[Globals._PixelBuffer.Length];
            for (int i = 0; i < Globals._PixelBuffer.Length; i++)
            {
                //determine the X and Y position of the pixel based off the known width and height
                int x = i % Globals._PixelBufferSize.x;
                int y = i / Globals._PixelBufferSize.x;
                //create a new state for the pixel
                states[i] = _layer.NewState("PIXEL " + x + " " + y);
                //set the pixel to be on when entered
                states[i].Drives(Globals._PixelBuffer[i], 1);

                //transition from entry to here if the X and Y registers match the current pixel
                //we need to convert the X and Y of the pixel of this state to a binary array
                string xBinary = Convert.ToString(x, 2).PadLeft(Register.BitDepth, '0');
                string yBinary = Convert.ToString(y, 2).PadLeft(Register.BitDepth, '0');

                //make a transition from entry to here if each bit of the X and Y registers match the current pixel
                AacFlTransition transition = entry.TransitionsTo(states[i]);
                for (int j = 0; j < Register.BitDepth; j++)
                {
                    int inverseJ = Register.BitDepth - j - 1;
                    bool checkBitX = xBinary[inverseJ] == '1';
                    bool checkBitY = yBinary[inverseJ] == '1';

                    transition.When(X[j].IsEqualTo(checkBitX)).And(Y[j].IsEqualTo(checkBitY));
                }

                //transition to the exit
                states[i].AutomaticallyMovesTo(exit);
                yield return PB.SetProgress(i / (float)Globals._PixelBuffer.Length);
            }

            PB.Finish();
            Profiler.EndSample();
            callback(Util.CombineStates(entry, states, exit));
            yield return null;
        }
    }
}
