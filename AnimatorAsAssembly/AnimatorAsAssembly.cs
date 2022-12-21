#if UNITY_EDITOR
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using UnityEditor.Animations;
using AnimatorAsCode.V0;
using UnityEngine.Profiling;
using System;
using System.Linq;
using System.Collections.Generic;

/*
Common symbols to look out for when it comes to variables:
$ - register
% - label
& - internal register
! - input register
* - GPU register

useful symbols
Hash - comment
; - subroutine

GPU Commands
DRAWCHAR - Draws a character to the screen {DRAWCHAR CHAR}
WRITECHAR - Writes a character to the screen {WRITECHAR CHAR} Only difference with DRAWCHAR is that it also shifts the 5 pixel rows to the right 4 pixels
DRAWSTRING - Writes a string to the screen {DRAWSTRING STRING}
DRAWREGISTER - Writes a single digit register to the screen {DRAWREGISTER REGISTER_NUMBER}
DRAWCOMPLETEREGISTER - Writes a register to the screen {DRAWCOMPLETEREGISTER REGISTER_NUMBER}
SHIFTSCREENRIGHT - Shifts the screen right by inputted amount {SHIFTSCREENRIGHT AMOUNT}
SHIFTLINERIGHT - Shifts the line right by inputted amount {SHIFTLINERIGHT AMOUNT}
SHIFTSCREENDOWN - Shifts the screen down by inputted amount {SHIFTSCREENDOWN AMOUNT}
UPDATESCREEN - Stops the GPU where it is, clears the screen and starts a new loop of redrawing

Opcodes
INC: Increments the register by 1
DEC: Decrements the register by 1
JMP: Jumps to the label
LBL: Creates a label to jump to
JEN: Jumps to a label if the register is equal to the number {JEN REGISTER_NUMBER NUMBER LABEL}
JNEN: Jumps to a label if the register is not equal to the number {JNEN REGISTER_NUMBER NUMBER LABEL}
NOP: Does nothing for 1 cycle
MOV: Copys the first register into the second register
LD: Loads the number into the register {LD REGISTER_NUMBER NUMBER}
LDB: Loads a boolean into the register {LDB REGISTER_NUMBER BOOLEAN}
ADD: adds the first register to the second register and stores the result in the third register
SUB: Subtracts the first register from the second register and stores the result in the third register
JEQ: Compares the first register to the second register, and if they are equal it jumps to the label {JEQ REGISTER_NUMBER REGISTER_NUMBER LABEL}
JIG: Compares the first register to the second register, and if the first register is greater than the second register it jumps to the label {JIG REGISTER_NUMBER REGISTER_NUMBER LABEL}
MUL: Multiplies the first register by the second register and stores the result in the third register
MULN: Multiplies the first register by a static number and stores the result in the second register {MULN REGISTER_NUMBER NUMBER REGISTER_NUMBER}
    Significantly faster than MUL
DIV: Divides the first register by the second register and stores the result in the third register (Rounds up) remainder is stored in DAC2 if you need it. if you use the remainder, keep in mind that the result will be 1 larger than it should be. TODO: Remainder calculation is not accurate
NOCONNECT: Does nothing, but does not connect the previous instruction to itself
SWAP: Swaps the contents of the first register with the contents of the second register
JSR: Jumps to a subroutine {JSR SUBROUTINE_NAME} Stores the return address in PC
RTS: Returns from a subroutine {RTS} Jumps to the address stored in PC
PUT: Puts a register into the stack, moving the stack up
POP: Pops a register from the stack, putting it into the register provided
DOUBLE: Doubles the register
HALVE: Halves the register
SHL: Shifts the register left once
SHR: Shifts the register right once
BOOLTOINT: Drives a register to a value if a VRC Contact Receiver is set to 1 {BOOLTOINT CONTACT_REGISTER RECEIVING_REGISTER VALUE}
SEGINT: turns a up to 8 digit int into two 4 digit ints {SEGINT REGISTER_NUMBER REGISTER_NUMBER REGISTER_NUMBER}
DELAY: wait for the set ammount of frames provided
GETDIGIT: gets a digit from a number {GETDIGIT NUMBER_REGISTER DIGIT_REGISTER DIGIT_NUMBER}
INTTOBINARY: turns a number into a binary number {INTTOBINARY NUMBER_REGISTER BINARY_REGISTER}
BINARYTOINT: turns a binary number into a number {BINARYTOINT BINARY_REGISTER NUMBER_REGISTER}
RAND8: generates a random number between 0 and 255 {RAND8 REGISTER_NUMBER}
*/

namespace AnimatorAsCodeFramework.Examples
{
    //this file converts assembly based commands into AAC commands to generate an avatar that can run CPU level instructions
    public class AnimatorAsAssembly : MonoBehaviour
    {
        public VRCAvatarDescriptor avatar;
        public AnimatorController assetContainer;
        public string assetKey;
        [HideInInspector]
        public int RegistersUsed = 0;

        [ReadOnly] public LBL[] Labels = new LBL[0];

        [Header("Compiler Options")]
        [Tooltip("This is used to determine the maximum size of the stack. If you are using a lot of PUSH, POP or subroutine commands, you may need to increase this.")]
        public int stackSize = 5;

        [Header("Graph Options")]
        public bool organizeGraph = true;
        public int horizontalGraphScale = 1;
        public int verticalGraphScale = 2;
        [Header("Display Options")]
        [Tooltip("If true, layers and registers needed for a display will be created")]
        public bool useDisplay = false;
        public int displayWidth = 20;
        public int displayHeight = 20;
        public GameObject displayXDrive;
        public GameObject displayBeamPrefab;
        public GameObject pixelTransformReference;

        [Header("Contact Senders")]
        public GameObject[] contactSenders;

        [CodeArea]
        public string RAWINSTRUCTIONS;

        [CodeArea(true)]
        public string CompiledCode;

        private AacFlBase aac;

        public void Create()
        {
            try {
                //Place the Asset Database in a state where
                //importing is suspended for most APIs
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("Clearing Asset", "Clearing Asset", 0.1f);
                //list how many sub assets are in the controller
                var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(assetContainer));
                Debug.Log("Sub Assets: " + allSubAssets.Length);
                //remove all sub assets except for the controller
                for (int i = 0; i < allSubAssets.Length; i++)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar("Clearing Asset", "Clearing Asset", ((float)i / (float)allSubAssets.Length));
                    if (cancel)
                    {
                        EditorUtility.ClearProgressBar();
                        AssetDatabase.StopAssetEditing();
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        Resources.UnloadUnusedAssets();
                        AssetDatabase.StartAssetEditing();
                        return;
                    }
                    if (allSubAssets[i].GetType() != typeof(AnimatorController) && allSubAssets[i].GetType() != typeof(AnimatorStateMachine))
                    {
                        DestroyImmediate(allSubAssets[i], true);
                    }
                }
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Resources.UnloadUnusedAssets();
                EditorUtility.ClearProgressBar();
                AssetDatabase.StartAssetEditing();

                Profiler.BeginSample("Compile");
                RegistersUsed = 0;
                Labels = new LBL[0];

                aac = AacExample.AnimatorAsCode("COMPUTER", avatar, assetContainer, assetKey, AacExample.Options().WriteDefaultsOn());

                var FX = aac.CreateMainFxLayer();

                Register[] registers = new Register[0];

                //create a list of states that will be used to store the program
                AacFlState[,] Instructions = new AacFlState[1,1];

                //create a dummy default state
                AacFlState DefaultState = FX.NewState("Default");

                //remove comments
                CompiledCode = Cleanup(RAWINSTRUCTIONS);

                //run the display generation code if the user wants it
                if (useDisplay)
                {
                    GenerateDisplay(FX, registers, displayWidth, displayHeight);
                }

                GenerateContactSenderSystem(FX);

                //read in the instructions
                Instructions = ParseInstructions(CompiledCode, FX, out registers);

                //correlate all of the instructions with their paths
                CorrelatePaths(Instructions, FX, CompiledCode, registers);

                //create final connection between default state and the first instruction
                DefaultState.AutomaticallyMovesTo(Instructions[0, 0]);
                Profiler.EndSample();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                //By adding a call to StopAssetEditing inside
                //a "finally" block, we ensure the AssetDatabase
                //state will be reset when leaving this function
                AssetDatabase.StopAssetEditing();
            }
        }

        //this function generates a display for the CPU to interface with
        //the display is a 2d array of boolean values
        //all GPU registers use a * prefix to differentiate them from the CPU registers
        public void GenerateDisplay(AacFlLayer FX, Register[] registers, int width, int height)
        {
            //progress bar
            EditorUtility.DisplayProgressBar("Compiling", "Generating Display", 0.1f);

            Profiler.BeginSample("GenerateDisplay");

            //create the horizontal drive layer
            AacFlLayer HD = aac.CreateSupportingFxLayer("GPU Horizontal Drive");

            //create a refresh layer for each row of the display
            AacFlLayer[] RL = new AacFlLayer[height];
            for (int i = 0; i < RL.Length; i++)
            {
                RL[i] = aac.CreateSupportingFxLayer("GPU Refresh Layer " + i);
            }

            //create a pixel emitter for each row of the display using the beam prefab
            GameObject[] PE = new GameObject[height];
            //remove all previous pixel emitters
            var tempList = displayXDrive.transform.Cast<Transform>().ToList();
            foreach(var child in tempList)
            {
                DestroyImmediate(child.gameObject);
            }
            //create new pixel emitters
            for (int i = 0; i < PE.Length; i++)
            {
                PE[i] = Instantiate(displayBeamPrefab);
                PE[i].name = "Pixel Emitter " + i;
                PE[i].transform.SetParent(displayXDrive.transform);
                PE[i].transform.localPosition = new Vector3(0, 0, -i);
                PE[i].transform.localRotation = Quaternion.Euler(0, 0, 0);
                PE[i].transform.localScale = new Vector3(1, 1, 1);
                //set the particle system to use the custom simulation space
                var main = PE[i].GetComponent<ParticleSystem>().main;
                main.simulationSpace = ParticleSystemSimulationSpace.Custom;
                main.customSimulationSpace = pixelTransformReference.transform;
            }

            //create the registers needed for the display
            AacFlIntParameter X = FX.IntParameter("*X");
            AacFlFloatParameter Xf = FX.FloatParameter("*Xf");
            AacFlBoolParameter[] VRAM = new AacFlBoolParameter[width * height];
            AacFlBoolParameter FakeFalse = FX.BoolParameter("*FakeFalse");

            //create the registers for the VRAM
            for (int i = 0; i < VRAM.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                VRAM[i] = FX.BoolParameter("*VRAM " + x + "," + y);
            }

            //HORIZONTAL DRIVE GENERATION
            //generate the animation to drive the Xdrive gameobject from 0 to width linearly from 0 to keyframe width
            AacFlState HORIZDRIVE = HD.NewState("HORIZDRIVE").WithAnimation(aac.NewClip("Horizontal Drive").Animating(clip => {
                clip.Animates(displayXDrive.transform, "localPosition.x").WithFrameCountUnit(keyframes =>
                    keyframes.Linear(0, 0).Linear(width - 1, displayWidth - 1)
                );
            })).MotionTime(Xf);
            //make the state loop on itself
            AacFlTransition HORIZDRIVELOOP = HORIZDRIVE.TransitionsTo(HORIZDRIVE).WithTransitionToSelf();
            HORIZDRIVELOOP.When(X.IsLessThan(width));
            //the percent should be about 3 frames
            float percent = 3f / (float)width;
            HORIZDRIVELOOP.AfterAnimationIsAtLeastAtPercent(percent);
            HORIZDRIVE.DrivingCasts(X, 0, width - 1, Xf, 0, 1);
            HORIZDRIVE.DrivingIncreases(X, 1);

            for (int i = 0; i < RL.Length; i++)
            {
                AacFlLayer Refresh = RL[i];
                GameObject PixelEmitter = PE[i];
                //generate the animation to draw the pixels for this row
                AacFlClip BeamOn = aac.NewClip("Beam On (" + i + ")").Animating(clip => {
                    clip.Animates(PixelEmitter.GetComponent<ParticleSystem>(), "EmissionModule.enabled").WithFrameCountUnit(keyframes =>
                        keyframes.Bool(0, true).Bool(1, true).Bool(2, false)
                    );
                });

                AacFlState VRAMBRANCH = Refresh.NewState("VRAMBRANCH");
                //generate the pixel draw states
                AacFlState[] PixelStates = new AacFlState[width];
                for (int x = 0; x < PixelStates.Length; x++)
                {
                    EditorUtility.DisplayProgressBar("Compiling", "Generating Display {Pixel Draw State " + x + "} Layer " + i, 0.1f + 0.8f * (float)(i * width + x) / (float)(width * height));
                    //create the state
                    AacFlState PixelDrawState = Refresh.NewState("Draw " + x);
                    //set the animation of the beam
                    PixelDrawState.WithAnimation(BeamOn);
                    //make it transition from the vram state to this draw state if the X and Y registers are correct, and the correct VRAM is true
                    AacFlTransition PixelDrawTransition = VRAMBRANCH.TransitionsTo(PixelDrawState);
                    PixelDrawTransition.When(
                        X.IsEqualTo(x + 1))
                        .And(VRAM[i * width + x].IsEqualTo(true));
                    //make it transition back to the VRAM state
                    AacFlTransition PixelDrawTransitionBack = PixelDrawState.TransitionsTo(VRAMBRANCH);
                    PixelDrawTransitionBack.When(X.IsNotEqualTo(x));
                    PixelStates[x] = PixelDrawState;
                }
            }


            /* OLD SCREEN REFRESH
            //SCREEN REFRSH GENERATION
            //generate the VRAM decision states
            AacFlState VRAMBRANCH = Refresh.NewState("VRAMBRANCH").DrivingIncreases(X, 1);
            AacFlState XRESET = Refresh.NewState("XRESET").Drives(X, -1).DrivingIncreases(Y, 1);
            AacFlState FINISHEDRENDERING = Refresh.NewState("FINISHEDRENDERING");
            AacFlTransition FINISHEDRENDERINGTRANSITION = VRAMBRANCH.TransitionsTo(FINISHEDRENDERING);
            FINISHEDRENDERINGTRANSITION.When(Y.IsGreaterThan(height - 1)).And(X.IsGreaterThan(width - 1));
            AacFlTransition STARTRENDERINGAGAIN = FINISHEDRENDERING.TransitionsTo(VRAMBRANCH);
            STARTRENDERINGAGAIN.When(Y.IsLessThan(height - 1)).And(X.IsLessThan(width - 1));
            AacFlTransition XRESETTRANSITION = VRAMBRANCH.TransitionsTo(XRESET);
            XRESETTRANSITION.When(X.IsGreaterThan(width - 1));
            XRESET.AutomaticallyMovesTo(VRAMBRANCH);

            //generate the animation to draw a pixel on the screen
            AacFlClip BeamOn = aac.NewClip("Display Beam On").Animating(clip => {
                clip.Animates(displayBeam, "EmissionModule.enabled").WithFrameCountUnit(keyframes =>
                    keyframes.Bool(0, false).Bool(1, true).Bool(2, true)
                );
            });
            //generate the pixel draw states
            AacFlState[] PixelStates = new AacFlState[VRAM.Length];
            for (int i = 0; i < VRAM.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Compiling", "Generating Display {Pixel Draw State " + i + "}", 0.1f + 0.8f * i / VRAM.Length);
                int x = i % width;
                int y = i / width;
                //create the state
                AacFlState PixelDrawState = Refresh.NewState("Draw " + x + "," + y);
                //set the animation of the beam
                PixelDrawState.WithAnimation(BeamOn);
                //make it transition from the vram state to this draw state if the X and Y registers are correct, and the correct VRAM is true
                AacFlTransition PixelDrawTransition = VRAMBRANCH.TransitionsTo(PixelDrawState);
                PixelDrawTransition.When(
                    X.IsEqualTo(x))
                    .And(Y.IsEqualTo(y))
                    .And(VRAM[i].IsEqualTo(true));
                //make it transition back to the VRAM state
                AacFlTransition PixelDrawTransitionBack = PixelDrawState.TransitionsTo(VRAMBRANCH);
                PixelDrawTransitionBack.AfterAnimationFinishes();
                PixelStates[i] = PixelDrawState;
            }

            //make the VRAM branch state transition to itself if there is no pixel to draw
            AacFlTransition VRAMBRANCHLOOP = VRAMBRANCH.TransitionsTo(VRAMBRANCH).WithTransitionToSelf();
            VRAMBRANCHLOOP.When(FakeFalse.IsEqualTo(false));
            */

            EditorUtility.ClearProgressBar();
        }

        //this function generates a layer per contact sender, which will turn the contact sender gameobject on and off depending on a boolean with the same name as the contact sender itself
        public void GenerateContactSenderSystem(AacFlLayer FX)
        {
            //progress bar
            EditorUtility.DisplayProgressBar("Compiling", "Generating Contact Sender System", 0.1f);

            foreach (GameObject sender in contactSenders)
            {
                EditorUtility.DisplayProgressBar("Compiling", "Generating Contact Sender System {Sender " + sender.name + "}", 0.5f);
                //create a new layer for the contact sender
                AacFlLayer ContactSenderLayer = aac.CreateSupportingFxLayer(sender.name + " Contact Sender");
                //create a new state for the contact sender (this is the default state, which will be off)
                AacFlState Off = ContactSenderLayer.NewState("Off");
                //create a new state for the contact sender (this is the state which will be on)
                AacFlState On = ContactSenderLayer.NewState("On");

                //create a new int for the contact sender
                AacFlIntParameter ContactSenderBool = FX.IntParameter(sender.name);

                //create a transition from the off state to the on state if the boolean is true
                AacFlTransition OnTransition = Off.TransitionsTo(On);
                OnTransition.When(ContactSenderBool.IsGreaterThan(0));
                //create a transition from the on state to the off state if the boolean is false
                AacFlTransition OffTransition = On.TransitionsTo(Off);
                OffTransition.When(ContactSenderBool.IsEqualTo(0));

                //create the animation for the contact sender to be on
                AacFlClip OnAnimation = aac.NewClip(sender.name + " On").Animating(clip => {
                    clip.Animates(sender).WithFrameCountUnit(keyframes =>
                        keyframes.Bool(0, true)
                    );
                });

                On.WithAnimation(OnAnimation);
            }

            EditorUtility.ClearProgressBar();
        }

        //Cleanup removes all comments from the raw instructions
        //Cleanup also removes all new lines from the raw instructions, unless a ; is present
        //Comment: #This is a comment
        //Forced New Line: ;This is a forced new line
        //Forced new lines can be used to denote subroutines, as the first instruction after a forced new line will not be connected to the previous instruction
        public string Cleanup(string raw)
        {
            //progress bar
            EditorUtility.DisplayProgressBar("Compiling", "Cleaning up code", 0.1f);

            Profiler.BeginSample("Cleanup");
            string[] lines = raw.Split('\n');

            string output = "";

            //cleans up and removes empty lines / comments
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                //remove comments
                //completely remove them from the output
                if (line.StartsWith("#"))
                {
                    //do nothing
                }
                else if (line == "" || line == "\r" || line == "\n" || line == " ")
                {
                    //do nothing
                }
                else
                {
                    //add the line to the output
                    output += line + "\n";
                }
            }

            //Handle the RTS instruction
            //this is complicated, as it needs to know the line number of all the JSR instructions that reference the subroutine it is in
            //first we need to find the subroutine that the RTS is in
            //then we need to find all the JSR instructions that reference that subroutine
            //then we need to append to the RTS every single line number of the JSR instructions
            for (int i = 0; i < output.Split('\n').Length; i++)
            {
                string line = output.Split('\n')[i];

                if (line == "RTS")
                {
                    //find the subroutine that the RTS is in
                    //subroutines are denoted by ;
                    for (int j = i; j >= 0; j--)
                    {
                        string line2 = output.Split('\n')[j];

                        if (line2.StartsWith(";"))
                        {
                            //we have found the subroutine
                            //now we need to find all the JSR instructions that reference this subroutine
                            for (int k = 0; k < output.Split('\n').Length; k++)
                            {
                                string line3 = output.Split('\n')[k];

                                if (line3.StartsWith("JSR " + line2))
                                {
                                    //we have found a JSR instruction that references the subroutine
                                    //append the line number to the RTS
                                    line += " " + k;
                                }
                            }

                            //modify the relevant JSR instructions to point to the subroutine
                            for (int k = 0; k < output.Split('\n').Length; k++)
                            {
                                string line3 = output.Split('\n')[k];

                                if (line3.StartsWith("JSR " + line2))
                                {
                                    //we have found a JSR instruction that references the subroutine
                                    //modify the JSR instruction to point to the subroutine instead of the identifier
                                    //dont use replace as it is messy
                                    output = output.Substring(0, output.IndexOf(line3)) + "JSR " + j + output.Substring(output.IndexOf(line3) + 4 + line2.Length);
                                }
                            }

                            //we have found the subroutine, break out of the loop
                            break;
                        }
                    }

                    //replace the RTS with the new RTS. we cant use replace as it will replace all instances of RTS
                    //we also need to make sure we are ONLY replacing the RTS instruction at line i
                    for (int j = 0; j < output.Split('\n').Length; j++)
                    {
                        string line2 = output.Split('\n')[j];

                        if (line2 == "RTS")
                        {
                            if (j == i)
                            {
                                //replace the RTS with the new RTS
                                //split, replace, join
                                string[] split = output.Split('\n');
                                split[j] = line;
                                output = string.Join("\n", split);
                            }
                        }
                    }
                }
            }

            //handle subroutines, denoted by ; they should be replaced with NOCONNECT
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.StartsWith(";"))
                {
                    //replace the ; with NOCONNECT
                    //dont use replace as it is messy
                    output = output.Substring(0, output.IndexOf(line)) + "NOCONNECT" + output.Substring(output.IndexOf(line) + line.Length);
                }
            }

            //remove the last \n
            output = output.Substring(0, output.Length - 1);

            //find all the labels in output and store them in the Labels array
            for (int i = 0; i < output.Split('\n').Length; i++)
            {
                string line = output.Split('\n')[i];

                if (line.Contains("LBL"))
                {
                    LBL newLabel = new LBL(line.Split(' ')[1], i);
                    Labels = CopyIntoArray(Labels, newLabel);
                }
            }

            //loop through and resolve all JMP to their relevant LBL
            for (int i = 0; i < Labels.Length; i++)
            {
                //JMP
                //output = output.Replace("JMP " + Labels[i].Name, "JMP " + Labels[i].Line); //messy comparison, leads to issues
                
                //special case for the JEN, JNEN and JEQ instructions
                //these instructions need to have the register number and the number they are comparing to kept
                for (int j = 0; j < output.Split('\n').Length; j++)
                {
                    string line = output.Split('\n')[j];

                    if (line.Contains("JEN ") || line.Contains("JNEN ") || line.Contains("JEQ ") || line.Contains("JIG "))
                    {
                        string[] split = line.Split(' ');
                        //make sure this is the right label
                        if (split[3] == Labels[i].Name)
                        {
                            //dont use replace as it is messy
                            //output = output.Replace(line, split[0] + " " + split[1] + " " + split[2] + " " + Labels[i].Line);
                            output = output.Substring(0, output.IndexOf(line)) + split[0] + " " + split[1] + " " + split[2] + " " + Labels[i].Line + output.Substring(output.IndexOf(line) + line.Length);
                        }
                    }

                    if (line.Contains("JMP "))
                    {
                        string[] split = line.Split(' ');
                        //make sure this is the right label
                        if (split[1] == Labels[i].Name)
                        {
                            //dont use replace as it is messy
                            //output = output.Replace(line, split[0] + " " + Labels[i].Line);
                            output = output.Substring(0, output.IndexOf(line)) + split[0] + " " + Labels[i].Line + output.Substring(output.IndexOf(line) + line.Length);
                        }
                    }
                }
            }

            //if there is a JSR instruction at the end of the program, add a NOP to the end of the program
            if (output.Split('\n')[output.Split('\n').Length - 1].StartsWith("JSR"))
            {
                output += "\nNOP";
            }

            Profiler.EndSample();
            //end the progress bar
            EditorUtility.ClearProgressBar();
            return output;
        }

        public void CorrelatePaths(AacFlState[,] Instructions, AacFlLayer FX, string raw, Register[] registers)
        {
            Profiler.BeginSample("Path Correlation");

            //progress bar
            EditorUtility.DisplayProgressBar("Correlating Paths", "Correlating Paths", 0);

            //split the instructions into an array
            string[] instructions = raw.Split('\n');

            //place every opcode based on their index in the array
            //[x, y]
            if (organizeGraph){
                EditorUtility.DisplayProgressBar("Correlating Paths", "Organizing Graph", 0f);
                Vector3 zero = new Vector3(0, 1000, 0);
                for (int x = 0; x < Instructions.GetLength(0); x++)
                {
                    //the Y may not be the same for every X, so we need to check for null
                    for (int y = 0; y < Instructions.GetLength(1); y++)
                    {
                        if (Instructions[x, y] == null)
                        {
                            //go to the next X
                            break;
                        }
                        //shift the instruction to the correct position
                        Instructions[x, y].Shift(zero, (x * horizontalGraphScale), y * verticalGraphScale);
                        EditorUtility.DisplayProgressBar("Correlating Paths", "Organizing Graph", (float)x / (float)Instructions.GetLength(0));
                    }
                    //create a empty state above the instruction to denote what line it is on
                    AacFlState LineIndicator = FX.NewState("Line: " + x);
                    LineIndicator.Over(Instructions[x, 0]);
                }
                EditorUtility.ClearProgressBar();
            }

            //create the correct instant transitions based on the instruction
            for (int i = 0; i < instructions.Length; i++)
            {
                string instruction = instructions[i];

                //split the instruction into an array
                string[] instructionParts = instruction.Split(' ');

                //get the instruction type
                string instructionType = instructionParts[0];

                //progress bar
                EditorUtility.DisplayProgressBar("Correlating Paths", "Correlating Paths {" + instruction + "}", (float)i / (float)instructions.Length);

                //try to connect the previous instruction to the current one
                //do this by connecting the Last node in the previous instruction to the first node in the current instruction
                AacFlState PreviousFirstNode = null;
                AacFlState PreviousLastNode = null;
                AacFlState CurrentFirstNode = null;
                AacFlState CurrentLastNode = null;
                
                try { 
                    int CurrentLastNodeSecondIndex = 0;
                    for (int j = 0; j < Instructions.GetLength(1); j++)
                    {
                        if (Instructions[i, j] != null)
                        {
                            CurrentLastNodeSecondIndex = j;
                        }
                    }

                    CurrentLastNode = Instructions[i, CurrentLastNodeSecondIndex];
                    //get the first node in the current instruction
                    CurrentFirstNode = Instructions[i, 0];

                    //get the last node in the previous instruction
                    //we cant just use the length as the last node, we need to search for the last valid node
                    int PreviousLastNodeSecondIndex = 0;
                    for (int j = 0; j < Instructions.GetLength(1); j++)
                    {
                        if (Instructions[i - 1, j] != null)
                        {
                            PreviousLastNodeSecondIndex = j;
                        }
                    }
                    PreviousLastNode = Instructions[i - 1, PreviousLastNodeSecondIndex];

                    PreviousFirstNode = Instructions[i - 1, 0];
                    //connect the last node in the previous instruction to the first node in the current instruction
                    if (instructionType != "NOCONNECT")
                    {
                        PreviousLastNode.AutomaticallyMovesTo(CurrentFirstNode);
                    }
                }
                catch { }

                //define common transition names
                AacFlTransition trueCon;
                AacFlTransition falseCon;

                //create the relevant state
                switch (instructionType)
                {
                    case "JMP":
                        CurrentLastNode.AutomaticallyMovesTo(Instructions[int.Parse(instructionParts[1]), 0]);
                        //modify PC
                        CurrentLastNode.Drives(FX.IntParameter("&PC"),int.Parse(instructionParts[1]));
                        break;
                    case "JSR":
                        CurrentLastNode.AutomaticallyMovesTo(Instructions[int.Parse(instructionParts[1]), 0]);
                        //modify PC
                        CurrentLastNode.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[1]));
                        break;
                    case "JEN": //JEN (Jump If Equal To Number) Jumps to the line in the program list if the register is equal to the number
                        var JENPCMod = FX.NewState("{JEN} MODIFY PC");
                        JENPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                        JENPCMod.AutomaticallyMovesTo(Instructions[int.Parse(instructionParts[3]), 0]);
                        trueCon = CurrentLastNode.TransitionsTo(JENPCMod);
                        trueCon.When(Register.FindRegisterInArray(instructionParts[1], registers).param.IsEqualTo(int.Parse(instructionParts[2])));

                        //make sure if the condition is not met, that the PC is incremented
                        CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                        break;
                    case "JNEN": //JNEN (Jump If Not Equal To Number) Jumps to the line in the program list if the register is not equal to the number
                        var JNENPCMod = FX.NewState("{JNEN} MODIFY PC");
                        //JNENPCMod is the state that modifies the PC if the JNEN condition is true, then we jump to the line in the program list
                        JNENPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                        JNENPCMod.AutomaticallyMovesTo(Instructions[int.Parse(instructionParts[3]), 0]);
                        falseCon = CurrentLastNode.TransitionsTo(JNENPCMod);
                        falseCon.When(Register.FindRegisterInArray(instructionParts[1], registers).param.IsNotEqualTo(int.Parse(instructionParts[2])));

                        //make sure if the condition is not met, that the PC is incremented
                        CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                        break;
                    case "JEQ": //JEQ (Jump If Equal To) Compares the first register to the second register, and if they are equal it jumps to the line in the program list
                        //if the JEQR flag is true, then we jump to the line in the program list
                        //if the JEQR flag is false, then we dont jump, letting the program continue as normal
                        var JEQPCMod = FX.NewState("{JEQ} MODIFY PC");
                        JEQPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                        JEQPCMod.AutomaticallyMovesTo(Instructions[int.Parse(instructionParts[3]), 0]);
                        trueCon = CurrentLastNode.TransitionsTo(JEQPCMod);
                        var JEQR = FX.BoolParameter("&JEQR");
                        trueCon.When(JEQR.IsEqualTo(true));

                        //make sure if the condition is not met, that the PC is incremented
                        CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                        break;
                    case "JIG": //JIG (Jump If Greater Than) Compares the first register to the second register, and if the first register is greater than the second register it jumps to the line in the program list
                        //if the JIGR flag is true, then we jump to the line in the program list
                        //if the JIGR flag is false, then we dont jump, letting the program continue as normal
                        var JIGPCMod = FX.NewState("{JIG} MODIFY PC");
                        JIGPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                        JIGPCMod.AutomaticallyMovesTo(Instructions[int.Parse(instructionParts[3]), 0]);
                        trueCon = CurrentLastNode.TransitionsTo(JIGPCMod);
                        var JIGR = FX.BoolParameter("&JIGR");
                        trueCon.When(JIGR.IsEqualTo(true));

                        //make sure if the condition is not met, that the PC is incremented
                        CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                        break;
                    case "RTS":
                        //RTS is going to come into here with every single line it might go to deliminated with a space
                        int[] rtsLines = new int[instructionParts.Length - 1];
                        for (int j = 1; j < instructionParts.Length; j++)
                        {
                            rtsLines[j - 1] = int.Parse(instructionParts[j]);
                        }
                        //create a transition from the RTS state to each state defined by the RTS instruction
                        //this transition is true when the PC is equal to the line number
                        //we actually want it to go the the line after where the JSR was called, so we add 1 to the line number
                        for (int j = 0; j < rtsLines.Length; j++)
                        {
                            var RTSState = FX.NewState("{RTS} " + rtsLines[j]).DrivingIncreases(FX.IntParameter("&PC"), 1);
                            //throw an exception if there is no state to go to after the JSR
                            if (rtsLines[j] + 1 >= Instructions.GetLength(0))
                            {
                                throw new Exception("RTS instruction at line " + i + " is trying to go to a line that does not exist. The JSR instruction at line " + i + " needs a opcode after it.");
                            }
                            RTSState.AutomaticallyMovesTo(Instructions[rtsLines[j] + 1, 0]);
                            trueCon = CurrentLastNode.TransitionsTo(RTSState);
                            trueCon.When(FX.IntParameter("&PC").IsEqualTo(rtsLines[j]));
                        }
                        break;
                    default:
                        try{
                            //always make the program counter increment on the current instruction
                            CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                        }
                        catch { }
                        break;
                }
            }

            //end the progress bar
            EditorUtility.ClearProgressBar();
            Profiler.EndSample();
        }

        public AacFlState[,] ParseInstructions(string raw, AacFlLayer FX, out Register[] registers)
        {
            Profiler.BeginSample("ParseInstructions");

            //begin a progress bar
            EditorUtility.DisplayProgressBar("Parsing Instructions", "Parsing Instructions", 0);

            //split the instructions into an array
            string[] instructions = raw.Split('\n');

            //create a list of states that will be used to store the program
            //the max sub states a opcode can have is 20
            AacFlState[,] Instructions = new AacFlState[instructions.Length, 1];

            Register[] Registers = new Register[1];

            //loop through the instructions, making the relevant states based on the instruction
            for (int i = 0; i < instructions.Length; i++)
            {
                string instruction = instructions[i];

                //split the instruction into an array
                string[] instructionParts = instruction.Split(' ');

                //get the instruction type
                string instructionType = instructionParts[0];

                //progress bar
                EditorUtility.DisplayProgressBar("Parsing Instructions", "Parsing Instruction {" + instruction + "}", (float)i / (float)instructions.Length);

                //create a register for each register in the instruction
                //a register must include atleast one non number
                for (int j = 1; j < instructionParts.Length; j++)
                {
                    string part = instructionParts[j];
                    
                    //check to see if the part is not just a number using int.TryParse
                    int number;
                    if (!int.TryParse(part, out number) && !part.Contains("%"))
                    {
                        //if the part is not just a number, then it must be a register
                        //check to see if the register already exists
                        if (Register.FindRegisterInArray(part, Registers) == null)
                        {
                            //if the register does not exist, then create it
                            Register newReg = Register.CreateRegister(part, FX);

                            //increment the ammount of registers used
                            RegistersUsed++;

                            //add it into the registers array
                            Registers = CopyIntoArray(Registers, newReg);
                        }
                    }
                }

                //create the relevant state
                switch (instructionType)
                {
                    case "INC":
                        Instructions = CopyIntoArray(Instructions, INC(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "DEC":
                        Instructions = CopyIntoArray(Instructions, DEC(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "JMP":
                        Instructions = CopyIntoArray(Instructions, JMP(FX), i);
                        break;
                    case "JEN": //JEN (Jump If Equal To Number) Jumps to the line in the program list if the register is equal to the number
                        Instructions = CopyIntoArray(Instructions, JEN(FX), i);
                        break;
                    case "JNEN": //JNEN (Jump If Not Equal To Number) Jumps to the line in the program list if the register is not equal to the number
                        Instructions = CopyIntoArray(Instructions, JNEN(FX), i);
                        break;
                    case "NOP":
                        Instructions = CopyIntoArray(Instructions, NOP(FX), i);
                        break;
                    case "MOV":
                        Instructions = CopyIntoArray(Instructions, MOV(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), FX), i);
                        break;
                    case "LD":
                        Instructions = CopyIntoArray(Instructions, LD(Register.FindRegisterInArray(instructionParts[1], Registers), int.Parse(instructionParts[2]), FX), i);
                        break;
                    case "ADD":
                        Instructions = CopyIntoArray(Instructions, ADD(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), Register.FindRegisterInArray(instructionParts[3], Registers), FX), i);
                        break;
                    case "SUB":
                        Instructions = CopyIntoArray(Instructions, SUB(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), Register.FindRegisterInArray(instructionParts[3], Registers), FX), i);
                        break;
                    case "JEQ":
                        Instructions = CopyIntoArray(Instructions, JEQ(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), FX), i);
                        break;
                    case "MUL":
                        Instructions = CopyIntoArray(Instructions, MUL(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), Register.FindRegisterInArray(instructionParts[3], Registers), FX), i);
                        break;
                    case "MULN":
                        Instructions = CopyIntoArray(Instructions, MULN(Register.FindRegisterInArray(instructionParts[1], Registers), int.Parse(instructionParts[2]), Register.FindRegisterInArray(instructionParts[3], Registers), FX), i);
                        break;
                    case "DIV":
                        Instructions = CopyIntoArray(Instructions, DIV(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), Register.FindRegisterInArray(instructionParts[3], Registers), FX), i);
                        break;
                    case "NOCONNECT":
                        Instructions = CopyIntoArray(Instructions, NOP(FX), i);
                        break;
                    case "LBL":
                        Instructions = CopyIntoArray(Instructions, NOP(FX), i);
                        break;
                    case "JIG":
                        Instructions = CopyIntoArray(Instructions, JIG(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), FX), i);
                        break;
                    case "SWAP":
                        Instructions = CopyIntoArray(Instructions, SWAP(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), FX), i);
                        break;
                    case "PUT":
                        Instructions = CopyIntoArray(Instructions, PUT(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "POP":
                        Instructions = CopyIntoArray(Instructions, POP(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "JSR":
                        Instructions = CopyIntoArray(Instructions, JSR(FX), i);
                        break;
                    case "RTS":
                        Instructions = CopyIntoArray(Instructions, RTS(FX), i);
                        break;
                    case "DOUBLE":
                        Instructions = CopyIntoArray(Instructions, DOUBLE(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "HALVE":
                        Instructions = CopyIntoArray(Instructions, HALF(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "SHL":
                        Instructions = CopyIntoArray(Instructions, SHL(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "SHR":
                        Instructions = CopyIntoArray(Instructions, SHR(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "BOOLTOINT":
                        Instructions = CopyIntoArray(Instructions, BOOLTOINT(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), int.Parse(instructionParts[3]), FX), i);
                        break;
                    case "SEGINT":
                        Instructions = CopyIntoArray(Instructions, SEGINT(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), Register.FindRegisterInArray(instructionParts[3], Registers), FX), i);
                        break;
                    case "DRAWCHAR":
                        Instructions = CopyIntoArray(Instructions, DRAWCHAR(instructionParts[1], FX), i);
                        break;
                    case "WRITECHAR":
                        Instructions = CopyIntoArray(Instructions, WRITECHAR(instructionParts[1], FX), i);
                        break;
                    case "DELAY":
                        Instructions = CopyIntoArray(Instructions, DELAY(int.Parse(instructionParts[1]), FX), i);
                        break;
                    case "SHIFTSCREENRIGHT":
                        Instructions = CopyIntoArray(Instructions, SHIFTSCREENRIGHT(int.Parse(instructionParts[1]), FX), i);
                        break;
                    case "SHIFTSCREENDOWN":
                        Instructions = CopyIntoArray(Instructions, SHIFTSCREENDOWN(int.Parse(instructionParts[1]), FX), i);
                        break;
                    case "GETDIGIT":
                        Instructions = CopyIntoArray(Instructions, GETDIGIT(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), int.Parse(instructionParts[3]), FX), i);
                        break;
                    case "DRAWREGISTER":
                        Instructions = CopyIntoArray(Instructions, DRAWREGISTER(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "DRAWCOMPLETEREGISTER":
                        Instructions = CopyIntoArray(Instructions, DRAWCOMPLETEREGISTER(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    case "SHIFTLINERIGHT":
                        Instructions = CopyIntoArray(Instructions, SHIFTLINERIGHT(int.Parse(instructionParts[1]), FX), i);
                        break;
                    case "UPDATESCREEN":
                        Instructions = CopyIntoArray(Instructions, UPDATESCREEN(FX), i);
                        break;
                    case "DRAWSTRING":
                        Instructions = CopyIntoArray(Instructions, DRAWSTRING(instructionParts[1], FX), i);
                        break;
                    case "INTTOBINARY":
                        Instructions = CopyIntoArray(Instructions, INTTOBINARY(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), FX), i);
                        break;
                    case "BINARYTOINT":
                        Instructions = CopyIntoArray(Instructions, BINARYTOINT(Register.FindRegisterInArray(instructionParts[1], Registers), Register.FindRegisterInArray(instructionParts[2], Registers), FX), i);
                        break;
                    case "RAND8":
                        Instructions = CopyIntoArray(Instructions, RAND8(Register.FindRegisterInArray(instructionParts[1], Registers), FX), i);
                        break;
                    default:
                        //throw an exception if the instruction is not valid
                        throw new Exception("Invalid instruction: " + instruction);
                        break;
                }
            }

            registers = Registers;
            Profiler.EndSample();
            //end the progress bar
            EditorUtility.ClearProgressBar();
            return Instructions;
        }

        public string AddInstruction(string RAWINSTRUCTIONS, string instruction)
        {
            return RAWINSTRUCTIONS + instruction + "\n";
        }

        //set the second dimension of the array to the array
        public AacFlState[,] CopyIntoArray(AacFlState[,] array, AacFlState[] array2, int index)
        {
            Profiler.BeginSample("CopyIntoArray [AacFlState]");
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

            Profiler.EndSample();
            return array;
        }

        public Register[] CopyIntoArray(Register[] array, Register value)
        {
            Profiler.BeginSample("CopyIntoArray [Register]");
            //create a new array with a length of the old array + 1
            Register[] newArray = new Register[array.Length + 1];

            //copy each element in, one by one
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            //add the new value to the end of the array
            newArray[newArray.Length - 1] = value;

            Profiler.EndSample();
            return newArray;
        }

        public LBL[] CopyIntoArray(LBL[] array, LBL value)
        {
            Profiler.BeginSample("CopyIntoArray [LBL]");
            //create a new array with a length of the old array + 1
            LBL[] newArray = new LBL[array.Length + 1];

            //copy each element in, one by one
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            //add the new value to the end of the array
            newArray[newArray.Length - 1] = value;

            Profiler.EndSample();
            return newArray;
        }

        //thank god for chatGPT for this one
        public static AacFlState[] ConcatArrays(params object[] objects)
        {
            object[][] arrays = objects.Select(x => x is object[] ? (object[])x : new object[] { x }).ToArray();
            return arrays.Aggregate((a, b) => a.Concat(b).ToArray()).Cast<AacFlState>().ToArray();
        }

        public AacFlState[] INC(Register register, AacFlLayer FX)
        {
            var state = FX.NewState("INC").DrivingIncreases(register.param, 1);
            return new AacFlState[] { state };
        }

        public AacFlState[] DEC(Register register, AacFlLayer FX)
        {
            var state = FX.NewState("DEC").DrivingDecreases(register.param, 1);
            return new AacFlState[] { state };
        }

        public AacFlState[] JMP(AacFlLayer FX)
        {
            var state = FX.NewState("JMP");
            return new AacFlState[] { state };
        }

        public AacFlState[] JEN(AacFlLayer FX)
        {
            var state = FX.NewState("JEN");
            return new AacFlState[] { state };
        }

        public AacFlState[] JNEN(AacFlLayer FX)
        {
            var state = FX.NewState("JNEN");
            return new AacFlState[] { state };
        }

        public AacFlState[] NOP(AacFlLayer FX)
        {
            var state = FX.NewState("NOP");
            return new AacFlState[] { state };
        }

        public AacFlState[] MOV(Register register1, Register register2, AacFlLayer FX)
        {
            var state = FX.NewState("MOV").DrivingCopies(register1.param, register2.param);
            return new AacFlState[] { state };
        }

        public AacFlState[] LD(Register register, int number, AacFlLayer FX)
        {
            var state = FX.NewState("LD").Drives(register.param, number);
            return new AacFlState[] { state };
        }

        public AacFlState[] SWAP(Register register1, Register register2, AacFlLayer FX)
        {
            var SWAP1 = Register.CreateRegister("&SWAP1", FX);
            var SWAP2 = Register.CreateRegister("&SWAP2", FX);

            var movToSWAP = FX.NewState("{SWAP} MOV TO SWAP").DrivingCopies(register1.param, SWAP1.param).DrivingCopies(register2.param, SWAP2.param);
            var movToRegisters = FX.NewState("{SWAP} MOV TO REGISTERS").DrivingCopies(SWAP1.param, register2.param).DrivingCopies(SWAP2.param, register1.param);

            //transition from movToSWAP to movToRegisters
            movToSWAP.AutomaticallyMovesTo(movToRegisters);

            return new AacFlState[] { movToSWAP, movToRegisters };
        }

        //uses registers AAC1 and AAC2 for calculation, then stores the result in register3
        public AacFlState[] ADD(Register register1, Register register2, Register register3, AacFlLayer FX)
        {
            var AAC1 = Register.CreateRegister("&AAC1", FX);
            var AAC2 = Register.CreateRegister("&AAC2", FX);
            var JIGR = FX.BoolParameter("&JIGR");

            var movToAAC = FX.NewState("{ADD} MOV TO AAC").DrivingCopies(register1.param, AAC1.param).DrivingCopies(register2.param, AAC2.param);
            var CalculationBranch = FX.NewState("{ADD} CALC BRANCH");
            var Calculation1x = FX.NewState("{ADD} CALC 1x").DrivingIncreases(AAC1.param, 1).DrivingDecreases(AAC2.param, 1);
            var Calculation10x = FX.NewState("{ADD} CALC 10x").DrivingIncreases(AAC1.param, 10).DrivingDecreases(AAC2.param, 10);
            var Calculation100x = FX.NewState("{ADD} CALC 100x").DrivingIncreases(AAC1.param, 100).DrivingDecreases(AAC2.param, 100);
            var Calculation1000x = FX.NewState("{ADD} CALC 1000x").DrivingIncreases(AAC1.param, 1000).DrivingDecreases(AAC2.param, 1000);
            var Calculation10000x = FX.NewState("{ADD} CALC 10000x").DrivingIncreases(AAC1.param, 10000).DrivingDecreases(AAC2.param, 10000);
            var Calculation100000x = FX.NewState("{ADD} CALC 100000x").DrivingIncreases(AAC1.param, 100000).DrivingDecreases(AAC2.param, 100000);
            var Calculation1000000x = FX.NewState("{ADD} CALC 1000000x").DrivingIncreases(AAC1.param, 1000000).DrivingDecreases(AAC2.param, 1000000);
            var Calculation10000000x = FX.NewState("{ADD} CALC 10000000x").DrivingIncreases(AAC1.param, 10000000).DrivingDecreases(AAC2.param, 10000000);

            var movToRegister = FX.NewState("{ADD} MOV TO REGISTER").DrivingCopies(AAC1.param, register3.param);

            //Speed Checks. if (AAC1 is less than AAC2, or AAC2 is less than 0) and AAC1 is greater than 0, then we want to swap them
            //(NOT JIGR OR AAC2 < 0) AND AAC1 > 0
            //rewrite the boolean equation to not have a nested OR
            //(NOT JIGR AND AAC1 > 0) OR (AAC2 < 0 AND AAC1 > 0)
            var swapAAC = SWAP(AAC1, AAC2, FX);
            var JIGRCheck = JIG(AAC1, AAC2, FX);

            //transitions
            movToAAC.AutomaticallyMovesTo(JIGRCheck[0]);
            AacFlTransition JIGRCheckTrue = JIGRCheck[JIGRCheck.Length - 1].TransitionsTo(swapAAC[0]);
            JIGRCheckTrue.When(JIGR.IsEqualTo(false)).And(AAC1.param.IsGreaterThan(0)).Or().When(AAC2.param.IsLessThan(0)).And(AAC1.param.IsGreaterThan(0));
            JIGRCheck[JIGRCheck.Length - 1].AutomaticallyMovesTo(CalculationBranch);
            swapAAC[swapAAC.Length - 1].AutomaticallyMovesTo(CalculationBranch);

            //check if 0
            //if AAC2 is 0, then move to movToRegister
            AacFlTransition movToRegister1 = CalculationBranch.TransitionsTo(movToRegister);
            movToRegister1.When(AAC2.param.IsEqualTo(0));

            //if AAC2 is greater than 100, then do 100x
            //if AAC2 is greater than 10, then do 10x
            //if AAC2 is greater than 1, then do 1x
            //if AAC2 is 0, then move to movToRegister
            AacFlTransition movToAAC10000000x = CalculationBranch.TransitionsTo(Calculation10000000x);
            movToAAC10000000x.When(AAC2.param.IsGreaterThan(10000000));
            AacFlTransition movToAAC1000000x = CalculationBranch.TransitionsTo(Calculation1000000x);
            movToAAC1000000x.When(AAC2.param.IsGreaterThan(1000000));
            AacFlTransition movToAAC100000x = CalculationBranch.TransitionsTo(Calculation100000x);
            movToAAC100000x.When(AAC2.param.IsGreaterThan(100000));
            AacFlTransition movToAAC10000x = CalculationBranch.TransitionsTo(Calculation10000x);
            movToAAC10000x.When(AAC2.param.IsGreaterThan(10000));
            AacFlTransition movToAAC1000x = CalculationBranch.TransitionsTo(Calculation1000x);
            movToAAC1000x.When(AAC2.param.IsGreaterThan(1000));
            AacFlTransition movToAAC100x = CalculationBranch.TransitionsTo(Calculation100x);
            movToAAC100x.When(AAC2.param.IsGreaterThan(100));
            AacFlTransition movToAAC10x = CalculationBranch.TransitionsTo(Calculation10x);
            movToAAC10x.When(AAC2.param.IsGreaterThan(10));
            AacFlTransition movToAAC1x = CalculationBranch.TransitionsTo(Calculation1x);
            movToAAC1x.When(AAC2.param.IsGreaterThan(0));
            
            //loop back to CalculationBranch
            Calculation1x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000000x.AutomaticallyMovesTo(CalculationBranch);

            //return ConcatFlStateArrays(new AacFlState[] { movToAAC }, ConcatFlStateArrays(JIGRCheck, ConcatFlStateArrays(swapAAC, new AacFlState[] { CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, Calculation10000x, movToRegister })));
            return ConcatArrays(movToAAC, JIGRCheck, swapAAC, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, Calculation10000x, Calculation100000x, Calculation1000000x, Calculation10000000x, movToRegister);
        }

        public AacFlState[] SUB(Register register1, Register register2, Register register3, AacFlLayer FX)
        {
            var SAC1 = Register.CreateRegister("&SAC1", FX);
            var SAC2 = Register.CreateRegister("&SAC2", FX);

            var movToSAC = FX.NewState("{SUB} MOV TO SAC").DrivingCopies(register1.param, SAC1.param).DrivingCopies(register2.param, SAC2.param);
            var CalculationBranch = FX.NewState("{SUB} CALC BRANCH");
            var Calculation1x = FX.NewState("{SUB} CALC 1x").DrivingDecreases(SAC1.param, 1).DrivingDecreases(SAC2.param, 1);
            var Calculation10x = FX.NewState("{SUB} CALC 10x").DrivingDecreases(SAC1.param, 10).DrivingDecreases(SAC2.param, 10);
            var Calculation100x = FX.NewState("{SUB} CALC 100x").DrivingDecreases(SAC1.param, 100).DrivingDecreases(SAC2.param, 100);
            var Calculation1000x = FX.NewState("{SUB} CALC 1000x").DrivingDecreases(SAC1.param, 1000).DrivingDecreases(SAC2.param, 1000);
            var Calculation10000x = FX.NewState("{SUB} CALC 10000x").DrivingDecreases(SAC1.param, 10000).DrivingDecreases(SAC2.param, 10000);
            var Calculation100000x = FX.NewState("{SUB} CALC 100000x").DrivingDecreases(SAC1.param, 100000).DrivingDecreases(SAC2.param, 100000);
            var Calculation1000000x = FX.NewState("{SUB} CALC 1000000x").DrivingDecreases(SAC1.param, 1000000).DrivingDecreases(SAC2.param, 1000000);
            var Calculation10000000x = FX.NewState("{SUB} CALC 10000000x").DrivingDecreases(SAC1.param, 10000000).DrivingDecreases(SAC2.param, 10000000);

            var movToRegister = FX.NewState("{SUB} MOV TO REGISTER").DrivingCopies(SAC1.param, register3.param);

            //transitions
            movToSAC.AutomaticallyMovesTo(CalculationBranch);

            //if SAC2 is greater than 100, then do 100x
            //if SAC2 is greater than 10, then do 10x
            //if SAC2 is greater than 1, then do 1x
            //if SAC2 is 0, then move to movToRegister
            AacFlTransition movToSAC10000000x = CalculationBranch.TransitionsTo(Calculation10000000x);
            movToSAC10000000x.When(SAC2.param.IsGreaterThan(10000000));
            AacFlTransition movToSAC1000000x = CalculationBranch.TransitionsTo(Calculation1000000x);
            movToSAC1000000x.When(SAC2.param.IsGreaterThan(1000000));
            AacFlTransition movToSAC100000x = CalculationBranch.TransitionsTo(Calculation100000x);
            movToSAC100000x.When(SAC2.param.IsGreaterThan(100000));
            AacFlTransition movToSAC10000x = CalculationBranch.TransitionsTo(Calculation10000x);
            movToSAC10000x.When(SAC2.param.IsGreaterThan(10000));
            AacFlTransition movToSAC1000x = CalculationBranch.TransitionsTo(Calculation1000x);
            movToSAC1000x.When(SAC2.param.IsGreaterThan(1000));
            AacFlTransition movToSAC100x = CalculationBranch.TransitionsTo(Calculation100x);
            movToSAC100x.When(SAC2.param.IsGreaterThan(100));
            AacFlTransition movToSAC10x = CalculationBranch.TransitionsTo(Calculation10x);
            movToSAC10x.When(SAC2.param.IsGreaterThan(10));
            AacFlTransition movToSAC1x = CalculationBranch.TransitionsTo(Calculation1x);
            movToSAC1x.When(SAC2.param.IsGreaterThan(0));

            //check if 0
            //if SAC2 is 0, then move to movToRegister
            AacFlTransition movToRegister1 = CalculationBranch.TransitionsTo(movToRegister);
            movToRegister1.When(SAC2.param.IsEqualTo(0));
            
            //loop back to CalculationBranch
            Calculation1x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000000x.AutomaticallyMovesTo(CalculationBranch);
            //return new AacFlState[] { movToSAC, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, Calculation10000x, movToRegister };
            return ConcatArrays(movToSAC, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, Calculation10000x, Calculation100000x, Calculation1000000x, Calculation10000000x, movToRegister);
        }

        //uses registers JEQ1 and JEQ2 for calculation, then sets the flag JEQR to 1 if they are equal, and 0 if they are not
        //JEQR is reset to 0 every time this instruction is called
        public AacFlState[] JEQ(Register register1, Register register2, AacFlLayer FX)
        {
            var JEQ1 = Register.CreateRegister("&JEQ1", FX);
            var JEQ2 = Register.CreateRegister("&JEQ2", FX);
            var JEQR = FX.BoolParameter("&JEQR");

            var movToJEQ = FX.NewState("{JEQ} MOV TO JEQ").DrivingCopies(register1.param, JEQ1.param).DrivingCopies(register2.param, JEQ2.param).Drives(JEQR, false); //resets the flag to false
            var CalculationBranch = FX.NewState("{JEQ} CALC BRANCH");
            var Calculation1x = FX.NewState("{JEQ} CALC").DrivingDecreases(JEQ1.param, 1).DrivingDecreases(JEQ2.param, 1);
            var Calculation10x = FX.NewState("{JEQ} CALC").DrivingDecreases(JEQ1.param, 10).DrivingDecreases(JEQ2.param, 10);
            var Calculation100x = FX.NewState("{JEQ} CALC").DrivingDecreases(JEQ1.param, 100).DrivingDecreases(JEQ2.param, 100);
            var Calculation1000x = FX.NewState("{JEQ} CALC").DrivingDecreases(JEQ1.param, 1000).DrivingDecreases(JEQ2.param, 1000);

            var setJEQR = FX.NewState("{JEQ} SET JEQR").Drives(JEQR, true); //only drives it true if this state is entered
            var NOP = FX.NewState("{JEQ} BRANCH MERGE");

            //transitions
            movToJEQ.AutomaticallyMovesTo(CalculationBranch);
            setJEQR.AutomaticallyMovesTo(NOP);

            AacFlTransition NOPCon = CalculationBranch.TransitionsTo(NOP);
            NOPCon.When(JEQ1.param.IsLessThan(0)).Or().When(JEQ2.param.IsLessThan(0)); //determine they arent equal when one of the registers goes below -1, this is because the registers are decremented by 1 each cycle, so if one of them is lower than -1, they arent equal, since the other one would have to be higher or equal to -1

            //if JEQ1 or JEQ2 is greater than 1000, then do 1000x
            //if JEQ1 or JEQ2 is greater than 100, then do 100x
            //if JEQ1 or JEQ2 is greater than 10, then do 10x
            //if JEQ1 or JEQ2 is greater than 1, then do 1x
            //if JEQ1 or JEQ2 is 0, then move to setJEQR
            AacFlTransition movToJEQ1000x = CalculationBranch.TransitionsTo(Calculation1000x);
            movToJEQ1000x.When(JEQ1.param.IsGreaterThan(1000)).Or().When(JEQ2.param.IsGreaterThan(1000));
            AacFlTransition movToJEQ100x = CalculationBranch.TransitionsTo(Calculation100x);
            movToJEQ100x.When(JEQ1.param.IsGreaterThan(100)).Or().When(JEQ2.param.IsGreaterThan(100));
            AacFlTransition movToJEQ10x = CalculationBranch.TransitionsTo(Calculation10x);
            movToJEQ10x.When(JEQ1.param.IsGreaterThan(10)).Or().When(JEQ2.param.IsGreaterThan(10));
            AacFlTransition movToJEQ1x = CalculationBranch.TransitionsTo(Calculation1x);
            movToJEQ1x.When(JEQ1.param.IsGreaterThan(0)).Or().When(JEQ2.param.IsGreaterThan(0));

            //loop back to CalculationBranch
            Calculation1x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000x.AutomaticallyMovesTo(CalculationBranch);

            AacFlTransition setJEQRCon = CalculationBranch.TransitionsTo(setJEQR); 
            setJEQRCon.When(JEQ1.param.IsLessThan(1)).And(JEQ2.param.IsLessThan(1)); //determine they are equal when both registers are less than 1, this is because the registers are decremented by 1 each cycle, so if both are less than 1, they are equal, since they would have to be equal to -1
            //return new AacFlState[] { movToJEQ, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, setJEQR, NOP };
            return ConcatArrays(movToJEQ, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, setJEQR, NOP);
        }

        //uses registers JIG1 and JIG2 for calculation, then sets the flag JIGR to 1 if register1 is greater than register2, and 0 if it is not
        public AacFlState[] JIG(Register register1, Register register2, AacFlLayer FX)
        {
            var JIG1 = Register.CreateRegister("&JIG1", FX);
            var JIG2 = Register.CreateRegister("&JIG2", FX);
            var JIGR = FX.BoolParameter("&JIGR");

            var movToJIG = FX.NewState("{JIG} MOV TO JIG").DrivingCopies(register1.param, JIG1.param).DrivingCopies(register2.param, JIG2.param).Drives(JIGR, false); //resets the flag to false
            var CalculationBranch = FX.NewState("{JIG} CALC BRANCH");
            var Calculation1x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 1).DrivingDecreases(JIG2.param, 1);
            var Calculation10x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 10).DrivingDecreases(JIG2.param, 10);
            var Calculation100x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 100).DrivingDecreases(JIG2.param, 100);
            var Calculation1000x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 1000).DrivingDecreases(JIG2.param, 1000);
            var Calculation10000x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 10000).DrivingDecreases(JIG2.param, 10000);
            var Calculation100000x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 100000).DrivingDecreases(JIG2.param, 100000);
            var Calculation1000000x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 1000000).DrivingDecreases(JIG2.param, 1000000);
            var Calculation10000000x = FX.NewState("{JIG} CALC").DrivingDecreases(JIG1.param, 10000000).DrivingDecreases(JIG2.param, 10000000);

            var setJIGR = FX.NewState("{JIG} SET JIGR").Drives(JIGR, true); //only drives it true if this state is entered
            var NOP = FX.NewState("{JIG} BRANCH MERGE");

            //transitions
            movToJIG.AutomaticallyMovesTo(CalculationBranch);
            setJIGR.AutomaticallyMovesTo(NOP);

            //if JIG2 is less than 0, but JIG1 is greater than 0, then set JIGR to 1
            AacFlTransition movToJIGR = CalculationBranch.TransitionsTo(setJIGR);
            movToJIGR.When(JIG1.param.IsGreaterThan(-1)).And(JIG2.param.IsLessThan(0));

            //if JIG1 is less than 0, but JIG2 is greater than 0, then set JIGR to 0
            AacFlTransition movToNOP = CalculationBranch.TransitionsTo(NOP);
            movToNOP.When(JIG1.param.IsLessThan(1)).And(JIG2.param.IsGreaterThan(-1));

            //if JIG1 or JIG2 is greater than 1000, then do 1000x
            //if JIG1 or JIG2 is greater than 100, then do 100x
            //if JIG1 or JIG2 is greater than 10, then do 10x
            //if JIG1 or JIG2 is greater than 1, then do 1x
            //if JIG1 or JIG2 is 0, then move to setJIGR
            AacFlTransition movToJIG10000000x = CalculationBranch.TransitionsTo(Calculation10000000x);
            movToJIG10000000x.When(JIG1.param.IsGreaterThan(10000000)).Or().When(JIG2.param.IsGreaterThan(10000000));
            AacFlTransition movToJIG1000000x = CalculationBranch.TransitionsTo(Calculation1000000x);
            movToJIG1000000x.When(JIG1.param.IsGreaterThan(1000000)).Or().When(JIG2.param.IsGreaterThan(1000000));
            AacFlTransition movToJIG100000x = CalculationBranch.TransitionsTo(Calculation100000x);
            movToJIG100000x.When(JIG1.param.IsGreaterThan(100000)).Or().When(JIG2.param.IsGreaterThan(100000));
            AacFlTransition movToJIG10000x = CalculationBranch.TransitionsTo(Calculation10000x);
            movToJIG10000x.When(JIG1.param.IsGreaterThan(10000)).Or().When(JIG2.param.IsGreaterThan(10000));
            AacFlTransition movToJIG1000x = CalculationBranch.TransitionsTo(Calculation1000x);
            movToJIG1000x.When(JIG1.param.IsGreaterThan(1000)).Or().When(JIG2.param.IsGreaterThan(1000));
            AacFlTransition movToJIG100x = CalculationBranch.TransitionsTo(Calculation100x);
            movToJIG100x.When(JIG1.param.IsGreaterThan(100)).Or().When(JIG2.param.IsGreaterThan(100));
            AacFlTransition movToJIG10x = CalculationBranch.TransitionsTo(Calculation10x);
            movToJIG10x.When(JIG1.param.IsGreaterThan(10)).Or().When(JIG2.param.IsGreaterThan(10));
            AacFlTransition movToJIG1x = CalculationBranch.TransitionsTo(Calculation1x);
            movToJIG1x.When(JIG1.param.IsGreaterThan(0)).Or().When(JIG2.param.IsGreaterThan(0));

            Calculation1x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000000x.AutomaticallyMovesTo(CalculationBranch);

            //return new AacFlState[] { movToJIG, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, setJIGR, NOP };
            return ConcatArrays(movToJIG, CalculationBranch, Calculation1x, Calculation10x, Calculation100x, Calculation1000x, Calculation10000x, Calculation100000x, Calculation1000000x, Calculation10000000x, setJIGR, NOP);
        }

        //uses registers MAC1-4 for calculation, then stores the result in register3
        //MAC1 //used for storing the result between loops
        //MAC2 //Initially loaded with input 2, is used as the addition number
        //MAC3 //Is loaded with the initial MAC1 number (How many times we need to add MAC2 to MAC1)
        //MAC4 //starts at 0 and counts up to MAC3 every calculation cycle (How many times we have added MAC3 into MAC1)
        public AacFlState[] MUL(Register register1, Register register2, Register register3, AacFlLayer FX)
        {
            var MAC1 = Register.CreateRegister("&MAC1", FX);
            var MAC2 = Register.CreateRegister("&MAC2", FX);
            var MAC3 = Register.CreateRegister("&MAC3", FX);
            var MAC4 = Register.CreateRegister("&MAC4", FX);
            var JEQR = FX.BoolParameter("&JEQR");
            var JIGR = FX.BoolParameter("&JIGR");

            var movToMAC = FX.NewState("{MUL} MOV TO MAC").DrivingCopies(register2.param, MAC2.param).DrivingCopies(register1.param, MAC3.param).Drives(MAC4.param, 0).Drives(MAC1.param, 0);

            //Speed Checks. if MAC2 is larger than MAC3, then swap them
            var swapMAC = SWAP(MAC2, MAC3, FX);
            var JIGRCheck = JIG(MAC3, MAC2, FX);

            //use the ADD function to add MAC1 to MAC2, and store the result in MAC1
            var Calculation = ADD(MAC1, MAC2, MAC1, FX);

            //increment MAC4 by 1
            var incrementMAC4 = FX.NewState("{MUL} INCREMENT MAC4").DrivingIncreases(MAC4.param, 1);

            //check if MAC4 is equal to MAC3
            AacFlState[] Condition = JEQ(MAC4, MAC3, FX);

            //move the result to the register
            var movToRegister = FX.NewState("{MUL} MOV TO REGISTER").DrivingCopies(MAC1.param, register3.param);

            //transitions
            movToMAC.AutomaticallyMovesTo(JIGRCheck[0]);
            AacFlTransition JIGRCheckTrue = JIGRCheck[JIGRCheck.Length - 1].TransitionsTo(swapMAC[0]);
            JIGRCheckTrue.When(JIGR.IsEqualTo(true));
            JIGRCheck[JIGRCheck.Length - 1].AutomaticallyMovesTo(Calculation[0]);
            swapMAC[swapMAC.Length - 1].AutomaticallyMovesTo(Calculation[0]);
            Calculation[Calculation.Length - 1].AutomaticallyMovesTo(incrementMAC4);
            incrementMAC4.AutomaticallyMovesTo(Condition[0]);
            AacFlTransition CalcNotDone = Condition[Condition.Length - 1].TransitionsTo(Calculation[0]);
            CalcNotDone.When(JEQR.IsEqualTo(false));
            Condition[Condition.Length - 1].AutomaticallyMovesTo(movToRegister);
            
            //concat the arrays and single states
            //AacFlState[] states = new AacFlState[1] { movToMAC }.Concat(Calculation).Concat(new AacFlState[1] { incrementMAC4 }).Concat(Condition).Concat(new AacFlState[1] { movToRegister }).ToArray();
            // movToMAC, JIGRCheck, swapMAC, Calculation, incrementMAC4, Condition, movToRegister
            //AacFlState[] states = ConcatFlStateArrays(new AacFlState[1] { movToMAC }, ConcatFlStateArrays(JIGRCheck, ConcatFlStateArrays(swapMAC, ConcatFlStateArrays(Calculation, ConcatFlStateArrays(new AacFlState[1] { incrementMAC4 }, ConcatFlStateArrays(Condition, new AacFlState[1] { movToRegister }))))));

            return ConcatArrays(movToMAC, JIGRCheck, swapMAC, Calculation, incrementMAC4, Condition, movToRegister);
        }

        public AacFlState[] MULN(Register rin, int value, Register rout, AacFlLayer FX)
        {
            //create a state with a parameter driver
            AacFlState staticMultiply = FX.NewState("{MULN} MULTIPLY").DrivingRemaps(rin.param, 0, ((int.MaxValue - 1) / value), rout.param, 0, int.MaxValue - 1);
            return new AacFlState[] { staticMultiply };
        }

        //uses registers DAC1-3 for calculation, then stores the result in register3
        //DAC1 //Loaded with register 1, used during calculation
        //DAC2 //Loaded with register 2, used during calculation and stores the remainder
        //DAC3 //Stores the count of how many times we have divided, and is also the final result
        public AacFlState[] DIV(Register register1, Register register2, Register register3, AacFlLayer FX)
        {
            var DAC1 = Register.CreateRegister("&DAC1", FX);
            var DAC2 = Register.CreateRegister("&DAC2", FX);
            var DAC3 = Register.CreateRegister("&DAC3", FX);

            var movToDAC = FX.NewState("{DIV} MOV TO DAC").DrivingCopies(register1.param, DAC1.param).DrivingCopies(register2.param, DAC2.param).Drives(DAC3.param, 0);

            //use the SUB function to subtract DAC2 from DAC1, and store the result in DAC1
            var Calculation = SUB(DAC1, DAC2, DAC1, FX);

            //increment DAC3 by 1
            var incrementDAC3 = FX.NewState("{DIV} INCREMENT DAC3").DrivingIncreases(DAC3.param, 1);

            //use the ADD function to add DAC2 to DAC2, and store the result in DAC2, which is the remainder
            var Remainder = ADD(DAC1, DAC2, DAC2, FX);

            //automatically set the remainder to 1 and result to 0 if DAC1 is 1 because it will not work otherwise
            var OneDivideRemainderFix = FX.NewState("{DIV} ONE DIVIDE REMAINDER FIX").Drives(DAC2.param, 1).Drives(DAC3.param, 0);

            //if it is divided evenly, set DAC2 to 0, since the above calculation wont work
            var DividedEvenly = FX.NewState("{DIV} DIVIDED EVENLY").Drives(DAC2.param, 0);

            //move the result to the register
            var movToRegister = FX.NewState("{DIV} MOV TO REGISTER").DrivingCopies(DAC3.param, register3.param);

            //transitions
            AacFlTransition OneDivideRemainderFixTransition = movToDAC.TransitionsTo(OneDivideRemainderFix);
            OneDivideRemainderFixTransition.When(DAC1.param.IsEqualTo(1));
            OneDivideRemainderFix.AutomaticallyMovesTo(movToRegister);
            movToDAC.AutomaticallyMovesTo(Calculation[0]);
            Calculation[Calculation.Length - 1].AutomaticallyMovesTo(incrementDAC3);
            AacFlTransition CalcNotDone = incrementDAC3.TransitionsTo(Calculation[0]);
            CalcNotDone.When(DAC1.param.IsGreaterThan(1));
            AacFlTransition DividedEvenlyTransition = incrementDAC3.TransitionsTo(DividedEvenly);
            DividedEvenlyTransition.When(DAC1.param.IsEqualTo(0));
            incrementDAC3.AutomaticallyMovesTo(Remainder[0]);
            Remainder[Remainder.Length - 1].AutomaticallyMovesTo(movToRegister);
            DividedEvenly.AutomaticallyMovesTo(movToRegister);

            return ConcatArrays(movToDAC, Calculation, incrementDAC3, Remainder, OneDivideRemainderFix, DividedEvenly, movToRegister);
        }

        //create X ammount of registers to use as the stack
        //PUT opcode will push the value of the register onto the stack, moving the stack pointer up
        //POP opcode will pop the value of the stack onto the register, moving the stack pointer down
        public Register[] CreateStack(int stackSize, AacFlLayer FX)
        {
            Register[] stack = new Register[stackSize];
            for (int i = 0; i < stackSize; i++)
            {
                stack[i] = Register.CreateRegister("&Stack_" + i, FX);
            }
            return stack;
        }

        //pushes the value of the register onto the stack, moving the stack pointer up
        public AacFlState[] PUT(Register register, AacFlLayer FX)
        {
            var stackPointer = Register.CreateRegister("&StackPointer", FX);
            Register[] stack = CreateStack(stackSize, FX);

            //the way this works is there is an entry state, which then branches to a state for each stack entry
            //the state for each stack entry will move the value of the register to the stack entry, then move the stack pointer up
            var entranceState = FX.NewState("{PUT} STACK BRANCH");
            var incrementStackPointer = FX.NewState("{PUT} INCREMENT STACK POINTER").DrivingIncreases(stackPointer.param, 1);
            AacFlState[] stackEntrys = new AacFlState[stackSize];
            for (int i = 0; i < stackSize; i++)
            {
                var movToStack = FX.NewState("{PUT} MOV TO STACK").DrivingCopies(register.param, stack[i].param);

                //create a transition from the entrance state to the stack copy state, then from the stack copy state to the increment stack pointer state
                AacFlTransition pointerIsThisEntry = entranceState.TransitionsTo(movToStack);
                pointerIsThisEntry.When(stackPointer.param.IsEqualTo(i));
                movToStack.AutomaticallyMovesTo(incrementStackPointer);

                //add the state to the array
                stackEntrys[i] = movToStack;
            }

            //return ConcatFlStateArrays(new AacFlState[1] { entranceState }, ConcatFlStateArrays(stackEntrys, new AacFlState[1] { incrementStackPointer }));
            return ConcatArrays(entranceState, stackEntrys, incrementStackPointer);
        }

        //pops the value of the stack onto the register, moving the stack pointer down
        public AacFlState[] POP(Register register, AacFlLayer FX)
        {
            var stackPointer = Register.CreateRegister("&StackPointer", FX);
            Register[] stack = CreateStack(stackSize, FX);

            //the way this works is there is an entry state, which then branches to a state for each stack entry
            //the state for each stack entry will move the value of the stack entry to the register, then move the stack pointer down
            var entranceState = FX.NewState("{POP} STACK BRANCH").DrivingDecreases(stackPointer.param, 1);
            var branchMerge = FX.NewState("{POP} BRANCH MERGE");
            AacFlState[] stackEntrys = new AacFlState[stackSize];
            for (int i = 0; i < stackSize; i++)
            {
                var movToRegister = FX.NewState("{POP} MOV TO REGISTER").DrivingCopies(stack[i].param, register.param);

                //create a transition from the entrance state to the stack copy state, then from the stack copy state to the increment stack pointer state
                AacFlTransition pointerIsThisEntry = entranceState.TransitionsTo(movToRegister);
                pointerIsThisEntry.When(stackPointer.param.IsEqualTo(i));
                movToRegister.AutomaticallyMovesTo(branchMerge);

                //add the state to the array
                stackEntrys[i] = movToRegister;
            }

            //return ConcatFlStateArrays(new AacFlState[1] { entranceState }, ConcatFlStateArrays(stackEntrys, new AacFlState[1] { branchMerge }));
            return ConcatArrays(entranceState, stackEntrys, branchMerge);
        }

        //JSR opcode will push the value of the program counter onto the stack
        public AacFlState[] JSR(AacFlLayer FX)
        {
            Register programCounter = Register.CreateRegister("&PC", FX);

            return PUT(programCounter, FX);
        }

        //RTS opcode will pop the value of the stack onto the program counter
        public AacFlState[] RTS(AacFlLayer FX)
        {
            Register programCounter = Register.CreateRegister("&PC", FX);

            return POP(programCounter, FX);
        }

        //annoyedly, due to how emulators work, we need a sub register to move into then move out
        public AacFlState[] DOUBLE(Register register, AacFlLayer FX)
        {
            var doubleRegister = Register.CreateRegister("&DOUB", FX);
            var doubleState = FX.NewState("{DOUBLE} DOUBLE REGISTER").DrivingRemaps(register.param, 0, (int.MaxValue / 2) - 1, doubleRegister.param, 0, int.MaxValue - 1);
            var move = FX.NewState("{DOUBLE} MOVE REGISTER").DrivingCopies(doubleRegister.param, register.param);

            doubleState.AutomaticallyMovesTo(move);

            return new AacFlState[2] { doubleState, move };
        }

        public AacFlState[] HALF(Register register, AacFlLayer FX)
        {
            var halfRegister = Register.CreateRegister("&HALF", FX);
            var halfState = FX.NewState("{HALF} HALF REGISTER").DrivingRemaps(register.param, 0, int.MaxValue - 1, halfRegister.param, 0, (int.MaxValue / 2) - 1);
            var move = FX.NewState("{HALF} MOVE REGISTER").DrivingCopies(halfRegister.param, register.param);

            halfState.AutomaticallyMovesTo(move);

            return new AacFlState[2] { halfState, move };
        }

        //Shift left by multiplying by 10
        public AacFlState[] SHL(Register register, AacFlLayer FX)
        {
            var shlRegister = Register.CreateRegister("&SHL", FX);
            var shlState = FX.NewState("{SHL} SHL REGISTER").DrivingRemaps(register.param, 0, (int.MaxValue / 10) - 1, shlRegister.param, 0, int.MaxValue - 1);
            var move = FX.NewState("{SHL} MOVE REGISTER").DrivingCopies(shlRegister.param, register.param);

            shlState.AutomaticallyMovesTo(move);

            return new AacFlState[2] { shlState, move };
        }

        //shift right by using a copy driver
        public AacFlState[] SHR(Register register, AacFlLayer FX)
        {
            var shrRegister = Register.CreateRegister("&SHR", FX);
            //10-99 becomes 1-9
            var shrState = FX.NewState("{SHR} SHR REGISTER").DrivingRemaps(register.param, 10, int.MaxValue - 1, shrRegister.param, 1, (int.MaxValue / 10) - 1);
            var move = FX.NewState("{SHR} MOVE REGISTER").DrivingCopies(shrRegister.param, register.param);

            shrState.AutomaticallyMovesTo(move);

            return new AacFlState[2] { shrState, move };
        }

        public AacFlState[] BOOLTOINT(Register contact, Register register, int value, AacFlLayer FX)
        {
            var enter = FX.NewState("{BOOLTOINT} ENTER");
            var driveIntIfContact = FX.NewState("{BOOLTOINT} DRIVE INT IF CONTACT").Drives(register.param, value);
            var exit = FX.NewState("{BOOLTOINT} EXIT");

            AacFlTransition contactIsOne = enter.TransitionsTo(driveIntIfContact);
            contactIsOne.When(contact.param.IsEqualTo(1));
            enter.AutomaticallyMovesTo(exit);
            driveIntIfContact.AutomaticallyMovesTo(exit);

            return new AacFlState[3] { enter, driveIntIfContact, exit };
        }

        //this takes in a up to 8 digit int and returns two 4 digit ints
        public AacFlState[] SEGINT(Register rin, Register out_1, Register out_2, AacFlLayer FX)
        {
            var SI1 = Register.CreateRegister("&SI1", FX);
            var SI2 = Register.CreateRegister("&SI2", FX);

            var movToRegister = FX.NewState("{SEGMENTINT} MOV TO REGISTER").DrivingCopies(rin.param, SI1.param).DrivingCopies(rin.param, SI2.param);

            var numberNotLargeEnough = FX.NewState("{SEGMENTINT} NUMBER NOT LARGE ENOUGH").DrivingCopies(SI1.param, out_2.param).Drives(out_1.param, 0);

            var branchMeet = FX.NewState("{SEGMENTINT} BRANCH MEET");
            
            //This clears the 4 rightmost digits to 0
            //shift right by 4
            var shr1 = SHR(SI1, FX);
            var shr2 = SHR(SI1, FX);
            var shr3 = SHR(SI1, FX);
            var shr4 = SHR(SI1, FX);
            //shift left by 4
            var shl1 = SHL(SI1, FX);
            var shl2 = SHL(SI1, FX);
            var shl3 = SHL(SI1, FX);
            var shl4 = SHL(SI1, FX);

            //This clears the 4 leftmost digits of SI2 to 0
            var calcSI2 = SUB(SI2, SI1, SI2, FX);

            //move SI1 back to be just 4 digits
            var shrFinal1 = SHR(SI1, FX);
            var shrFinal2 = SHR(SI1, FX);
            var shrFinal3 = SHR(SI1, FX);
            var shrFinal4 = SHR(SI1, FX);

            //transfer SI1 and SI2 to out_1 and out_2
            var movToOut = FX.NewState("{SEGMENTINT} MOV TO OUT").DrivingCopies(SI1.param, out_1.param).DrivingCopies(SI2.param, out_2.param);

            //link all the states together
            AacFlTransition numberIsLargeEnough = movToRegister.TransitionsTo(shr1[0]);
            numberIsLargeEnough.When(rin.param.IsGreaterThan(9999));
            movToRegister.AutomaticallyMovesTo(numberNotLargeEnough);
            shr1[1].AutomaticallyMovesTo(shr2[0]);
            shr2[1].AutomaticallyMovesTo(shr3[0]);
            shr3[1].AutomaticallyMovesTo(shr4[0]);
            shr4[1].AutomaticallyMovesTo(shl1[0]);
            shl1[1].AutomaticallyMovesTo(shl2[0]);
            shl2[1].AutomaticallyMovesTo(shl3[0]);
            shl3[1].AutomaticallyMovesTo(shl4[0]);
            shl4[1].AutomaticallyMovesTo(calcSI2[0]);
            calcSI2[calcSI2.Length - 1].AutomaticallyMovesTo(shrFinal1[0]);
            shrFinal1[1].AutomaticallyMovesTo(shrFinal2[0]);
            shrFinal2[1].AutomaticallyMovesTo(shrFinal3[0]);
            shrFinal3[1].AutomaticallyMovesTo(shrFinal4[0]);
            shrFinal4[1].AutomaticallyMovesTo(movToOut);
            movToOut.AutomaticallyMovesTo(branchMeet);
            numberNotLargeEnough.AutomaticallyMovesTo(branchMeet);

            return ConcatArrays(movToRegister, shr1, shr2, shr3, shr4, shl1, shl2, shl3, shl4, calcSI2, shrFinal1, shrFinal2, shrFinal3, shrFinal4, movToOut, numberNotLargeEnough, branchMeet);
        }

        public AacFlState[] DELAY(int frames, AacFlLayer FX)
        {
            //convert frames to seconds
            float seconds = frames / 60f;
            var delay = FX.NewState("{DELAY} DELAY").WithAnimation(aac.DummyClipLasting(seconds, AacFlUnit.Seconds));
            var exit = FX.NewState("{DELAY} EXIT");

            AacFlTransition delayFrames = delay.TransitionsTo(exit);
            delayFrames.AfterAnimationFinishes();

            return new AacFlState[2] { delay, exit };
        }

        //GPU SPECIFIC COMMANDS
        public AacFlState[] DRAWCHAR(string character, AacFlLayer FX)
        {
            //create a font dictionary for a 3x5 font, left to right, top to bottom
            var font = AnimatorAsAssemblyFont.GetFont();

            //create a state that drives the correct VRAM addresses
            AacFlState driveVRAM = FX.NewState("{DRAWCHAR} DRIVE VRAM");
            for (int i = 0; i < 15; i++)
            {
                bool value = font[character[0]][i];
                int VRAMx = i % 3;
                int VRAMy = i / 3;
                string VRAMAddress = "*VRAM " + VRAMx + "," + VRAMy;
                AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                driveVRAM.Drives(ADDRESS, value);
            }

            return new AacFlState[] { driveVRAM };
        }

        //this shifts everything in vram right one address
        public AacFlState[] SHIFTSCREENRIGHT(int pixels, AacFlLayer FX)
        {
            //create a state that drives the correct VRAM addresses
            AacFlState[] shiftPixels = new AacFlState[displayWidth];
            //make each state copy from the pixels to the left of it
            for (int x = displayWidth; x > 0; x--)
            {
                shiftPixels[x - 1] = FX.NewState("{SHIFTSCREENRIGHT} SHIFT PIXEL " + x);
                for (int y = 0; y < displayHeight; y++)
                {
                    string VRAMAddress = "*VRAM " + x + "," + y;
                    string VRAMAddressLeft = "*VRAM " + (x - pixels) + "," + y;
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    AacFlBoolParameter ADDRESSLEFT = FX.BoolParameter(VRAMAddressLeft);
                    shiftPixels[x - 1].DrivingCopies(ADDRESSLEFT, ADDRESS);
                }
            }

            //reverse the array
            Array.Reverse(shiftPixels);

            //link all of the states together
            for (int i = 0; i < shiftPixels.Length - 1; i++)
            {
                shiftPixels[i].AutomaticallyMovesTo(shiftPixels[i + 1]);
            }

            //make a final state that clears the leftmost pixel
            AacFlState clearLeftmostPixels = FX.NewState("{SHIFTSCREENRIGHT} CLEAR LEFTMOST PIXEL");
            for (int y = 0; y < displayHeight; y++)
            {
                string VRAMAddress = "*VRAM 0," + y;
                AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                clearLeftmostPixels.Drives(ADDRESS, false);
            }

            //link the last state to the clear state
            shiftPixels[shiftPixels.Length - 1].AutomaticallyMovesTo(clearLeftmostPixels);

            return ConcatArrays(shiftPixels, clearLeftmostPixels);
        }

        //shifts just the topmost 6pxl rows of the screen right
        public AacFlState[] SHIFTLINERIGHT(int pixels, AacFlLayer FX)
        {
            AacFlBoolParameter[] VRAMBUFFER = new AacFlBoolParameter[displayWidth * 6];
            //make a state to copy from the screen to the buffer, offset by the number of pixels
            AacFlState copyToBuffer = FX.NewState("{SHIFTLINERIGHT} COPY TO BUFFER");
            for (int x = 0; x < displayWidth; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    string VRAMAddress = "*VRAM " + x + "," + y;
                    string VRAMAddressBuffer = "*VRAMBUFFER " + (x + pixels) + "," + y;
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    AacFlBoolParameter ADDRESSBUFFER = FX.BoolParameter(VRAMAddressBuffer);
                    copyToBuffer.DrivingCopies(ADDRESS, ADDRESSBUFFER);
                }
            }

            //make a state to copy from the buffer to the screen
            AacFlState copyFromBuffer = FX.NewState("{SHIFTLINERIGHT} COPY FROM BUFFER");
            for (int x = 0; x < displayWidth; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    string VRAMAddress = "*VRAM " + x + "," + y;
                    string VRAMAddressBuffer = "*VRAMBUFFER " + x + "," + y;
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    AacFlBoolParameter ADDRESSBUFFER = FX.BoolParameter(VRAMAddressBuffer);
                    copyFromBuffer.DrivingCopies(ADDRESSBUFFER, ADDRESS);
                }
            }

            //link the states together
            copyToBuffer.AutomaticallyMovesTo(copyFromBuffer);

            return ConcatArrays(copyToBuffer, copyFromBuffer);
        }

        public AacFlState[] SHIFTSCREENDOWN(int pixels, AacFlLayer FX)
        {
            AacFlBoolParameter[] VRAMBUFFER = new AacFlBoolParameter[displayWidth * displayHeight];
            //make a state to copy from the screen to the buffer, offset by the number of pixels
            AacFlState copyToBuffer = FX.NewState("{SHIFTSCREENDOWN} COPY TO BUFFER");
            for (int x = 0; x < displayWidth; x++)
            {
                for (int y = 0; y < displayHeight; y++)
                {
                    string VRAMAddress = "*VRAM " + x + "," + y;
                    string VRAMAddressBuffer = "*VRAMBUFFER " + x + "," + (y + pixels);
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    AacFlBoolParameter ADDRESSBUFFER = FX.BoolParameter(VRAMAddressBuffer);
                    copyToBuffer.DrivingCopies(ADDRESS, ADDRESSBUFFER);
                }
            }

            //make a state to copy from the buffer to the screen
            AacFlState copyFromBuffer = FX.NewState("{SHIFTSCREENDOWN} COPY FROM BUFFER");
            for (int x = 0; x < displayWidth; x++)
            {
                for (int y = 0; y < displayHeight; y++)
                {
                    string VRAMAddress = "*VRAM " + x + "," + y;
                    string VRAMAddressBuffer = "*VRAMBUFFER " + x + "," + y;
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    AacFlBoolParameter ADDRESSBUFFER = FX.BoolParameter(VRAMAddressBuffer);
                    copyFromBuffer.DrivingCopies(ADDRESSBUFFER, ADDRESS);
                }
            }

            //link the states together
            copyToBuffer.AutomaticallyMovesTo(copyFromBuffer);

            return ConcatArrays(copyToBuffer, copyFromBuffer);
        }

        public AacFlState[] WRITECHAR(string character, AacFlLayer FX)
        {
            //shift the first 5 rows right 4 pixels
            AacFlState[] shiftRight = new AacFlState[displayWidth];
            for (int x = displayWidth; x > 0; x--)
            {
                shiftRight[x - 1] = FX.NewState("{WRITECHAR} SHIFT PIXEL " + x);
                for (int y = 0; y < 5; y++)
                {
                    string VRAMAddress = "*VRAM " + x + "," + y;
                    string VRAMAddressLeft = "*VRAM " + (x - 4) + "," + y;
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    AacFlBoolParameter ADDRESSLEFT = FX.BoolParameter(VRAMAddressLeft);
                    shiftRight[x - 1].DrivingCopies(ADDRESSLEFT, ADDRESS);
                }
            }

            //reverse the array
            Array.Reverse(shiftRight);

            //link all of the states together
            for (int i = 0; i < shiftRight.Length - 1; i++)
            {
                shiftRight[i].AutomaticallyMovesTo(shiftRight[i + 1]);
            }

            //make a final state that clears the leftmost pixel
            AacFlState clearLeftmostPixels = FX.NewState("{WRITECHAR} CLEAR LEFTMOST PIXEL");
            for (int y = 0; y < 5; y++)
            {
                string VRAMAddress = "*VRAM 0," + y;
                AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                clearLeftmostPixels.Drives(ADDRESS, false);
            }

            //link the last state to the clear state
            shiftRight[shiftRight.Length - 1].AutomaticallyMovesTo(clearLeftmostPixels);

            //make a state that writes the character
            AacFlState[] drawChar = DRAWCHAR(character, FX);

            //link the clear state to the draw state
            clearLeftmostPixels.AutomaticallyMovesTo(drawChar[0]);

            return ConcatArrays(shiftRight, clearLeftmostPixels, drawChar);
        }

        public AacFlState[] DRAWSTRING(string text, AacFlLayer FX)
        {
            //create a font dictionary for a 3x5 font, left to right, top to bottom
            var font = AnimatorAsAssemblyFont.GetFont();

            //create a state that drives the correct VRAM addresses
            AacFlState driveVRAM = FX.NewState("{DRAWCHAR} DRIVE VRAM");
            for (int t = 0; t < text.Length; t++)
            {
                string character = text.Substring(t, 1);
                for (int i = 0; i < 15; i++)
                {
                    bool value = false;
                    try {
                        value = font[character[0]][i];
                    }
                    catch (KeyNotFoundException)
                    {
                        //remove the progress bar
                        EditorUtility.ClearProgressBar();
                        throw new Exception("Character " + character + " not found in font provided");
                    }
                    int VRAMx = (i % 3) + (t * 4);
                    int VRAMy = i / 3;
                    string VRAMAddress = "*VRAM " + VRAMx + "," + VRAMy;
                    AacFlBoolParameter ADDRESS = FX.BoolParameter(VRAMAddress);
                    driveVRAM.Drives(ADDRESS, value);
                }
            }

            return new AacFlState[] { driveVRAM };
        }

        //this takes in a register and a digit place and returns the digit
        //@param {rIN} the register to get the digit from
        //@param {rOUT} the register to put the digit in
        //@param {digit_place} the digit place to get (0 is the ones place, 1 is the tens place, etc)
        public AacFlState[] GETDIGIT(Register rIN, Register rOUT, int digit_place, AacFlLayer FX)
        {
            Register GETD = Register.CreateRegister("&GETD", FX);
            AacFlState movToRegister = FX.NewState("{GETDIGIT} MOV TO REGISTER").DrivingRemaps(rIN.param, 0, int.MaxValue, GETD.param, 0, int.MaxValue / (int)Math.Pow(10, digit_place));
            AacFlState CalculationBranch = FX.NewState("{GETDIGIT} CALCULATION BRANCH");
            AacFlState Calculation10x = FX.NewState("{GETDIGIT} CALCULATION 10x").DrivingDecreases(GETD.param, 10);
            AacFlState Calculation100x = FX.NewState("{GETDIGIT} CALCULATION 100x").DrivingDecreases(GETD.param, 100);
            AacFlState Calculation1000x = FX.NewState("{GETDIGIT} CALCULATION 1000x").DrivingDecreases(GETD.param, 1000);
            AacFlState Calculation10000x = FX.NewState("{GETDIGIT} CALCULATION 10000x").DrivingDecreases(GETD.param, 10000);
            AacFlState Calculation100000x = FX.NewState("{GETDIGIT} CALCULATION 100000x").DrivingDecreases(GETD.param, 100000);
            AacFlState Calculation1000000x = FX.NewState("{GETDIGIT} CALCULATION 1000000x").DrivingDecreases(GETD.param, 1000000);
            AacFlState Calculation10000000x = FX.NewState("{GETDIGIT} CALCULATION 10000000x").DrivingDecreases(GETD.param, 10000000);

            AacFlState movToOutput = FX.NewState("{GETDIGIT} MOV TO OUTPUT").DrivingCopies(GETD.param, rOUT.param);

            movToRegister.AutomaticallyMovesTo(CalculationBranch);
            //check if the number is less than the minimum number that can be displayed at this digit place
            AacFlTransition CalcDone = CalculationBranch.TransitionsTo(movToOutput);
            CalcDone.When(GETD.param.IsLessThan(10));
            AacFlTransition Calc10x = CalculationBranch.TransitionsTo(Calculation10x);
            Calc10x.When(GETD.param.IsLessThan(100));
            AacFlTransition Calc100x = CalculationBranch.TransitionsTo(Calculation100x);
            Calc100x.When(GETD.param.IsLessThan(1000));
            AacFlTransition Calc1000x = CalculationBranch.TransitionsTo(Calculation1000x);
            Calc1000x.When(GETD.param.IsLessThan(10000));
            AacFlTransition Calc10000x = CalculationBranch.TransitionsTo(Calculation10000x);
            Calc10000x.When(GETD.param.IsLessThan(100000));
            AacFlTransition Calc100000x = CalculationBranch.TransitionsTo(Calculation100000x);
            Calc100000x.When(GETD.param.IsLessThan(1000000));
            AacFlTransition Calc1000000x = CalculationBranch.TransitionsTo(Calculation1000000x);
            Calc1000000x.When(GETD.param.IsLessThan(10000000));
            CalculationBranch.AutomaticallyMovesTo(Calculation10000000x);
            Calculation10x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation100000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation1000000x.AutomaticallyMovesTo(CalculationBranch);
            Calculation10000000x.AutomaticallyMovesTo(CalculationBranch);
            return ConcatArrays(movToRegister, CalculationBranch, Calculation10x, Calculation100x, Calculation1000x, Calculation10000x, Calculation100000x, Calculation1000000x, Calculation10000000x, movToOutput);
        }

        //displays a register on the screen
        public AacFlState[] DRAWREGISTER(Register r, AacFlLayer FX)
        {
            AacFlState start = FX.NewState("{DRAWREGISTER} START");
            AacFlState end = FX.NewState("{DRAWREGISTER} END");
            AacFlState[,] digitWrites = new AacFlState[10, 1];
            for (int i = 0; i < 10; i++)
            {
                digitWrites[i,0] = DRAWCHAR(i.ToString(), FX)[0];
                digitWrites[i,0].AutomaticallyMovesTo(end);
                AacFlTransition t = start.TransitionsTo(digitWrites[i,0]);
                t.When(r.param.IsEqualTo(i));
            }

            //convert the array to single dimension, linking the states together
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    if (j == 0)
                    {
                        continue;
                    }
                    digitWrites[i, j - 1].AutomaticallyMovesTo(digitWrites[i, j]);
                }
            }
            AacFlState[] states = new AacFlState[10 * digitWrites.GetLength(1)];
            for (int i = 0; i < 10 * digitWrites.GetLength(1); i++)
            {
                states[i] = digitWrites[i / digitWrites.GetLength(1), i % digitWrites.GetLength(1)];
            }

            return ConcatArrays(start, states, end);
        }

        //draws an entire 8 digit register
        public AacFlState[] DRAWCOMPLETEREGISTER(Register r, AacFlLayer FX)
        {
            //do this process for each digit
            //get the digit from the register using GETDIGIT and store it in a temporary register
            //draw the digit using DRAWREGISTER
            //shift the line to the right 4 units
            //repeat for each digit

            Register DCRDIG = Register.CreateRegister("&DCRDIG", FX); //stores the current working digit

            AacFlState[] ShiftState1 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState2 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState3 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState4 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState5 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState6 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState7 = SHIFTLINERIGHT(4, FX);
            AacFlState[] ShiftState8 = SHIFTLINERIGHT(4, FX);

            AacFlState[] GetDigit0 = GETDIGIT(r, DCRDIG, 0, FX);
            AacFlState[] GetDigit1 = GETDIGIT(r, DCRDIG, 1, FX);
            AacFlState[] GetDigit2 = GETDIGIT(r, DCRDIG, 2, FX);
            AacFlState[] GetDigit3 = GETDIGIT(r, DCRDIG, 3, FX);
            AacFlState[] GetDigit4 = GETDIGIT(r, DCRDIG, 4, FX);
            AacFlState[] GetDigit5 = GETDIGIT(r, DCRDIG, 5, FX);
            AacFlState[] GetDigit6 = GETDIGIT(r, DCRDIG, 6, FX);
            AacFlState[] GetDigit7 = GETDIGIT(r, DCRDIG, 7, FX);

            AacFlState[] DrawDigit0 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit1 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit2 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit3 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit4 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit5 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit6 = DRAWREGISTER(DCRDIG, FX);
            AacFlState[] DrawDigit7 = DRAWREGISTER(DCRDIG, FX);

            //link everything together
            ShiftState1[ShiftState1.Length - 1].AutomaticallyMovesTo(GetDigit0[0]);
            GetDigit0[GetDigit0.Length - 1].AutomaticallyMovesTo(DrawDigit0[0]);
            DrawDigit0[DrawDigit0.Length - 1].AutomaticallyMovesTo(ShiftState2[0]);

            ShiftState2[ShiftState2.Length - 1].AutomaticallyMovesTo(GetDigit1[0]);
            GetDigit1[GetDigit1.Length - 1].AutomaticallyMovesTo(DrawDigit1[0]);
            DrawDigit1[DrawDigit1.Length - 1].AutomaticallyMovesTo(ShiftState3[0]);

            ShiftState3[ShiftState3.Length - 1].AutomaticallyMovesTo(GetDigit2[0]);
            GetDigit2[GetDigit2.Length - 1].AutomaticallyMovesTo(DrawDigit2[0]);
            DrawDigit2[DrawDigit2.Length - 1].AutomaticallyMovesTo(ShiftState4[0]);

            ShiftState4[ShiftState4.Length - 1].AutomaticallyMovesTo(GetDigit3[0]);
            GetDigit3[GetDigit3.Length - 1].AutomaticallyMovesTo(DrawDigit3[0]);
            DrawDigit3[DrawDigit3.Length - 1].AutomaticallyMovesTo(ShiftState5[0]);

            ShiftState5[ShiftState5.Length - 1].AutomaticallyMovesTo(GetDigit4[0]);
            GetDigit4[GetDigit4.Length - 1].AutomaticallyMovesTo(DrawDigit4[0]);
            DrawDigit4[DrawDigit4.Length - 1].AutomaticallyMovesTo(ShiftState6[0]);

            ShiftState6[ShiftState6.Length - 1].AutomaticallyMovesTo(GetDigit5[0]);
            GetDigit5[GetDigit5.Length - 1].AutomaticallyMovesTo(DrawDigit5[0]);
            DrawDigit5[DrawDigit5.Length - 1].AutomaticallyMovesTo(ShiftState7[0]);

            ShiftState7[ShiftState7.Length - 1].AutomaticallyMovesTo(GetDigit6[0]);
            GetDigit6[GetDigit6.Length - 1].AutomaticallyMovesTo(DrawDigit6[0]);
            DrawDigit6[DrawDigit6.Length - 1].AutomaticallyMovesTo(ShiftState8[0]);

            ShiftState8[ShiftState8.Length - 1].AutomaticallyMovesTo(GetDigit7[0]);
            GetDigit7[GetDigit7.Length - 1].AutomaticallyMovesTo(DrawDigit7[0]);

            return ConcatArrays(ShiftState1, ShiftState2, ShiftState3, ShiftState4, ShiftState5, ShiftState6, ShiftState7, ShiftState8, GetDigit0, GetDigit1, GetDigit2, GetDigit3, GetDigit4, GetDigit5, GetDigit6, GetDigit7, DrawDigit0, DrawDigit1, DrawDigit2, DrawDigit3, DrawDigit4, DrawDigit5, DrawDigit6, DrawDigit7);
        }

        public AacFlState[] UPDATESCREEN(AacFlLayer FX)
        {
            Register X = Register.CreateRegister("*X", FX);
            //create a animation to turn off the screen particle system for a few frames, then turn it back on and set the GPU *x and *y to -1
            AacFlState TurnOffScreen = FX.NewState("TurnOffScreen").WithAnimation(aac.NewClip().Animating(clip =>
            {
                clip.Animates(displayXDrive.gameObject).WithFrameCountUnit(keyframes =>
                    keyframes.Bool(0, false).Bool(10, true)
                );
            }));

            AacFlState TurnOnScreen = FX.NewState("TurnOnScreen").Drives(X.param, -1);
            
            TurnOffScreen.AutomaticallyMovesTo(TurnOnScreen);

            return ConcatArrays(TurnOffScreen, TurnOnScreen);
        }

        //integer to binary is done by dividing the number by two, then using the remainder as the bit, and the quotient as the next number to divide by, till the quotient is 0
        //remember, internally binary is stored as either 1 or 4, not 0 or 1 due to the way unity truncates 0 from the front of integers and floating point imprecision from the copy
        //Due to how this calculation is done, the result is stored least significant bit first
        public AacFlState[] INTTOBINARY(Register rin, Register rout, AacFlLayer FX)
        {
            //create internal register
            Register INTTOBINARYAC1 = Register.CreateRegister("&INTTOBINARYAC1", FX); //quotient
            Register INTTOBINARYAC2 = Register.CreateRegister("&INTTOBINARYAC2", FX); //dividend
            Register INTTOBINARYACR = Register.CreateRegister("&INTTOBINARYACR", FX); //result
            Register DAC2 = Register.CreateRegister("&DAC2", FX); //used to determine if to add a bit or not
            //override INTTOBINARYAC2 with 2 (this is used for the divide by 2)
            FX.OverrideValue(INTTOBINARYAC2.param, 2);
            AacFlState movToRegister = FX.NewState("{INTTOBINARY} movToRegister").DrivingCopies(rin.param, INTTOBINARYAC1.param).Drives(INTTOBINARYACR.param, 0);
            AacFlState CalculationBranch = FX.NewState("{INTTOBINARY} CalculationBranch"); //used to determine if we are done calculating
            AacFlState[] Calculation = DIV(INTTOBINARYAC1, INTTOBINARYAC2, INTTOBINARYAC1, FX); //do the divide
            //append the remainder to the result register. do this by shifting the result register left by 1, then incrementing by 1. skip this step if DAC2 is 0
            AacFlState[] ShiftState = SHL(INTTOBINARYACR, FX);
            ShiftState[ShiftState.Length - 1].DrivingIncreases(INTTOBINARYACR.param, 1); //increase to 1 to represent a 0
            AacFlState BitIsOne = FX.NewState("{INTTOBINARY} BitIsOne").DrivingIncreases(INTTOBINARYACR.param, 3); //increase to 4 to represent a 1
            BitIsOne.AutomaticallyMovesTo(CalculationBranch);
            AacFlState movToOutput = FX.NewState("{INTTOBINARY} movToOutput").DrivingCopies(INTTOBINARYACR.param, rout.param);

            //everything above does the conversion, now we need to add the remaining 0s to the front of the number to make it 8 bits. shift left, increment by 1, till the number is larger than 10000000
            AacFlState AddZeroes = FX.NewState("{INTTOBINARY} AddZeroes");
            AacFlState[] ShiftState2 = SHL(INTTOBINARYACR, FX);
            ShiftState2[ShiftState2.Length - 1].DrivingIncreases(INTTOBINARYACR.param, 1);
            ShiftState2[ShiftState2.Length - 1].AutomaticallyMovesTo(AddZeroes);
            AacFlTransition AddZeroesDone = AddZeroes.TransitionsTo(movToOutput);
            AddZeroesDone.When(INTTOBINARYACR.param.IsGreaterThan(10000000));
            AddZeroes.AutomaticallyMovesTo(ShiftState2[0]);

            //handle 0 as a special case, since it wont calulate correctly. just set the result to 1
            AacFlState setOutputToZero = FX.NewState("{INTTOBINARY} setOutputToZero").Drives(INTTOBINARYACR.param, 1);
            setOutputToZero.AutomaticallyMovesTo(AddZeroes);
            AacFlTransition WhenZero = movToRegister.TransitionsTo(setOutputToZero);
            WhenZero.When(rin.param.IsEqualTo(0));
            movToRegister.AutomaticallyMovesTo(CalculationBranch);
            AacFlTransition CalculationDone = CalculationBranch.TransitionsTo(AddZeroes);
            CalculationDone.When(INTTOBINARYAC1.param.IsEqualTo(0));
            CalculationBranch.AutomaticallyMovesTo(Calculation[0]);
            AacFlTransition CalculationBranchToShiftState = ShiftState[ShiftState.Length - 1].TransitionsTo(BitIsOne);
            CalculationBranchToShiftState.When(DAC2.param.IsNotEqualTo(0));
            Calculation[Calculation.Length - 1].AutomaticallyMovesTo(ShiftState[0]);
            ShiftState[ShiftState.Length - 1].AutomaticallyMovesTo(CalculationBranch);

            return ConcatArrays(movToRegister, CalculationBranch, Calculation, ShiftState, BitIsOne, AddZeroes, ShiftState2, setOutputToZero, movToOutput);
        }

        //binary to integer is done by multiplying the number by 2 to the power of the bit, then adding it to the result. 1 is 0 and 4 is 1
        //little endian form means the least significant bit is first
        public AacFlState[] BINARYTOINT(Register rin, Register rout, AacFlLayer FX)
        {
            Register BINARYTOINTAC1 = Register.CreateRegister("&BINARYTOINTAC1", FX); //result
            Register BINARYTOINTAC2 = Register.CreateRegister("&BINARYTOINTAC2", FX); //used to store the working bit

            //sequence of operations is get the first bit, multiply it by 2^0, add it to the result, repeat for the next bit
            //7 is LSB since we are using little endian form
            AacFlState[] GetBit = GETDIGIT(rin, BINARYTOINTAC2, 7, FX);
            //make the last state reset the result register since it might have a value from a previous run
            GetBit[GetBit.Length - 1].Drives(BINARYTOINTAC1.param, 0);
            //add 1 to the result if the bit is 4
            AacFlState BIT_PLACE_ONE = FX.NewState("{BINARYTOINT} BIT_PLACE_ONE").DrivingIncreases(BINARYTOINTAC1.param, 1);
            //determine if get bit moves to BIT_PLACE_ONE if BINARYTOINTAC2 is 4
            AacFlTransition GetBitToBIT_PLACE_ONE = GetBit[GetBit.Length - 1].TransitionsTo(BIT_PLACE_ONE);
            GetBitToBIT_PLACE_ONE.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //LSB + 1   
            AacFlState[] GetBit2 = GETDIGIT(rin, BINARYTOINTAC2, 6, FX);
            AacFlState BIT_PLACE_TWO = FX.NewState("{BINARYTOINT} BIT_PLACE_TWO").DrivingIncreases(BINARYTOINTAC1.param, 2);
            AacFlTransition GetBitToBIT_PLACE_TWO = GetBit2[GetBit2.Length - 1].TransitionsTo(BIT_PLACE_TWO);
            GetBitToBIT_PLACE_TWO.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //LSB + 2
            AacFlState[] GetBit3 = GETDIGIT(rin, BINARYTOINTAC2, 5, FX);
            AacFlState BIT_PLACE_THREE = FX.NewState("{BINARYTOINT} BIT_PLACE_THREE").DrivingIncreases(BINARYTOINTAC1.param, 4);
            AacFlTransition GetBitToBIT_PLACE_THREE = GetBit3[GetBit3.Length - 1].TransitionsTo(BIT_PLACE_THREE);
            GetBitToBIT_PLACE_THREE.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //LSB + 3
            AacFlState[] GetBit4 = GETDIGIT(rin, BINARYTOINTAC2, 4, FX);
            AacFlState BIT_PLACE_FOUR = FX.NewState("{BINARYTOINT} BIT_PLACE_FOUR").DrivingIncreases(BINARYTOINTAC1.param, 8);
            AacFlTransition GetBitToBIT_PLACE_FOUR = GetBit4[GetBit4.Length - 1].TransitionsTo(BIT_PLACE_FOUR);
            GetBitToBIT_PLACE_FOUR.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //LSB + 4
            AacFlState[] GetBit5 = GETDIGIT(rin, BINARYTOINTAC2, 3, FX);
            AacFlState BIT_PLACE_FIVE = FX.NewState("{BINARYTOINT} BIT_PLACE_FIVE").DrivingIncreases(BINARYTOINTAC1.param, 16);
            AacFlTransition GetBitToBIT_PLACE_FIVE = GetBit5[GetBit5.Length - 1].TransitionsTo(BIT_PLACE_FIVE);
            GetBitToBIT_PLACE_FIVE.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //LSB + 5
            AacFlState[] GetBit6 = GETDIGIT(rin, BINARYTOINTAC2, 2, FX);
            AacFlState BIT_PLACE_SIX = FX.NewState("{BINARYTOINT} BIT_PLACE_SIX").DrivingIncreases(BINARYTOINTAC1.param, 32);
            AacFlTransition GetBitToBIT_PLACE_SIX = GetBit6[GetBit6.Length - 1].TransitionsTo(BIT_PLACE_SIX);
            GetBitToBIT_PLACE_SIX.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //LSB + 6
            AacFlState[] GetBit7 = GETDIGIT(rin, BINARYTOINTAC2, 1, FX);
            AacFlState BIT_PLACE_SEVEN = FX.NewState("{BINARYTOINT} BIT_PLACE_SEVEN").DrivingIncreases(BINARYTOINTAC1.param, 64);
            AacFlTransition GetBitToBIT_PLACE_SEVEN = GetBit7[GetBit7.Length - 1].TransitionsTo(BIT_PLACE_SEVEN);
            GetBitToBIT_PLACE_SEVEN.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //MSB
            AacFlState[] GetBit8 = GETDIGIT(rin, BINARYTOINTAC2, 0, FX);
            AacFlState BIT_PLACE_EIGHT = FX.NewState("{BINARYTOINT} BIT_PLACE_EIGHT").DrivingIncreases(BINARYTOINTAC1.param, 128);
            AacFlTransition GetBitToBIT_PLACE_EIGHT = GetBit8[GetBit8.Length - 1].TransitionsTo(BIT_PLACE_EIGHT);
            GetBitToBIT_PLACE_EIGHT.When(BINARYTOINTAC2.param.IsNotEqualTo(1));

            //final state
            AacFlState FINAL = FX.NewState("{BINARYTOINT} FINAL").DrivingCopies(BINARYTOINTAC1.param, rout.param);

            //make all the automatic transitions
            GetBit[GetBit.Length - 1].AutomaticallyMovesTo(GetBit2[0]);
            BIT_PLACE_ONE.AutomaticallyMovesTo(GetBit2[0]);
            GetBit2[GetBit.Length - 1].AutomaticallyMovesTo(GetBit3[0]);
            BIT_PLACE_TWO.AutomaticallyMovesTo(GetBit3[0]);
            GetBit3[GetBit.Length - 1].AutomaticallyMovesTo(GetBit4[0]);
            BIT_PLACE_THREE.AutomaticallyMovesTo(GetBit4[0]);
            GetBit4[GetBit.Length - 1].AutomaticallyMovesTo(GetBit5[0]);
            BIT_PLACE_FOUR.AutomaticallyMovesTo(GetBit5[0]);
            GetBit5[GetBit.Length - 1].AutomaticallyMovesTo(GetBit6[0]);
            BIT_PLACE_FIVE.AutomaticallyMovesTo(GetBit6[0]);
            GetBit6[GetBit.Length - 1].AutomaticallyMovesTo(GetBit7[0]);
            BIT_PLACE_SIX.AutomaticallyMovesTo(GetBit7[0]);
            GetBit7[GetBit.Length - 1].AutomaticallyMovesTo(GetBit8[0]);
            BIT_PLACE_SEVEN.AutomaticallyMovesTo(GetBit8[0]);
            GetBit8[GetBit.Length - 1].AutomaticallyMovesTo(FINAL);
            BIT_PLACE_EIGHT.AutomaticallyMovesTo(FINAL);
            return ConcatArrays(GetBit, GetBit2, GetBit3, GetBit4, GetBit5, GetBit6, GetBit7, GetBit8, BIT_PLACE_ONE, BIT_PLACE_TWO, BIT_PLACE_THREE, BIT_PLACE_FOUR, BIT_PLACE_FIVE, BIT_PLACE_SIX, BIT_PLACE_SEVEN, BIT_PLACE_EIGHT, FINAL);
        }

        //generates a random number between 0 and 255
        public AacFlState[] RAND8(Register rout, AacFlLayer FX)
        {
            AacFlState randomize = FX.NewState("{RAND8} randomize").DrivingRandomizesLocally(rout.param, 0, 255);

            return ConcatArrays(randomize);
        }
    }

    public class Register
    {
        public AacFlIntParameter param;
        public string name;

        public Register(string name, AacFlIntParameter param)
        {
            this.name = name;
            this.param = param;
        }

        public static Register CreateRegister(string name, AacFlLayer FX)
        {
            return new Register(name, FX.IntParameter(name));
        }

        public static Register FindRegisterInArray(string name, Register[] registers)
        {
            Profiler.BeginSample("FindRegisterInArray");
            for (int i = 0; i < registers.Length; i++)
            {
                if (registers[i] == null)
                {
                    continue;
                }

                if (registers[i].name == name)
                {
                    Profiler.EndSample();
                    return registers[i];
                }
            }
            Profiler.EndSample();
            return null;
        }
    }

    public class LBL
    {
        public string Name;
        public int Line;

        public LBL(string name, int LineNumber)
        {
            this.Name = name;
            this.Line = LineNumber;
        }
    }

    //create a simple inspector window that has a button for init and a button for ReadInProgram
    [CustomEditor(typeof(AnimatorAsAssembly))]
    public class AnimatorAsAssemblyEditor : Editor
    {
        double lastCompileTime = 0;
        public override void OnInspectorGUI()
        {
            AnimatorAsAssembly myScript = (AnimatorAsAssembly)target;
            GameObject myObject = myScript.gameObject;

            //record the current time
            double currentTime = Time.realtimeSinceStartup;
            //display a label with the time taken to compile the program last time
            EditorGUILayout.LabelField("Time taken to compile: " + lastCompileTime + "s");
            //display the ammount of registers used
            EditorGUILayout.LabelField("Registers used: " + myScript.RegistersUsed);
            if (GUILayout.Button("Create"))
            {
                //run the create function outside of OnInspectorGUI
                myScript.Create();
                //calculate the time taken to compile the program, rounded to 3 decimal places
                lastCompileTime = Math.Round((Time.realtimeSinceStartup - currentTime), 3);
            }

            DrawDefaultInspector();
        }
    }
}
#endif
