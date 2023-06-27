using AnimatorAsCode;
using AnimatorAsCode.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AnimatorAsAssembly.Commands
{
    public class PROFILING : OPCODE
    {
        public Register ClockTrack;
        public string name;
        private bool _startStop;
        private AacFlIntParameter _clockStart;
        private AacFlIntParameter _clockStop;

        /// <summary> starts/stops profiling to a named register </summary>
        /// <param name="STARTSTOP"> bool to start or stop profiling </param>
        /// <param name="name"> name to profile track </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PROFILING(bool STARTSTOP, string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            Init(STARTSTOP, name, Layer, (object)progressWindow);
        }

        /// <summary> Loads a register with a int value </summary>
        /// <param name="args"> The arguments for the command </param>
        /// <param name="Layer"> The FX controller that this command is linked to </param>
        public PROFILING(string[] args, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            bool STARTSTOP = false;
            switch (args[0])
            {
                case "START":
                    STARTSTOP = true;
                    break;
                case "STOP":
                    STARTSTOP = false;
                    break;
                default:
                    Debug.LogError("PROFILING: Invalid START/STOP argument");
                    break;
            }
            //split the args into the register and the value
            Init(STARTSTOP, args[1], Layer, progressWindow);
        }

        /// <summary> Initialize the variables. This is seperate so multiple constructors can use the same init functionality </summary>
        void Init(bool STARTSTOP, string name, AacFlLayer Layer, ComplexProgressBar progressWindow)
        {
            this.name = name;
            this._startStop = STARTSTOP;
            this._layer = Layer.NewStateGroup("PROFILING " + name);
            this._progressWindow = progressWindow;
            this._clockStart = Layer.IntParameter("PROFILING/" + name + "/START");
            this._clockStop = Layer.IntParameter("PROFILING/" + name + "/STOP");
        }

        public static string[] GetColoration()
        {
            return new string[] { "instruction", "command", "profilerName" };
        }

        public override IEnumerator<EditorCoroutine> GenerateStates(Action<AacFlState[]> callback)
        {
            Profiler.BeginSample("PROFILING");
            AacFlState entry = _layer.NewState("PROFILING");

            // copy the clock value to the start/stop parameter depending on the start/stop bool
            switch (_startStop)
            {
                case true:
                    entry.DrivingCopies(Globals._Clock, _clockStart);
                    break;
                case false:
                    entry.DrivingCopies(Globals._Clock, _clockStop);
                    break;
            }
            yield return null;

            Profiler.EndSample();
            callback(new AacFlState[] { entry });
        }
    }
}
