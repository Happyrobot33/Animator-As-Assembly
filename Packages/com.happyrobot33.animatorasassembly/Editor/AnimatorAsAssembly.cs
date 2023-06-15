#if UNITY_EDITOR
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using UnityEditor.Animations;
using AnimatorAsCode.Framework;
using AnimatorAsCode.Framework.Examples;
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
CLEARSCREEN - Clears the screen
DRAWCHARCODE - Draws a character to the screen using a INT code {DRAWCHARCODE INT_CODE} (ASCII Table)
PIXEL - Draws a pixel to the screen {PIXEL X Y}

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
RANDOM: generates a random number between a min and max {RANDOM MIN MAX REGISTER_NUMBER}
*/

namespace AnimatorAsAssembly
{
    //this file converts assembly based commands into AAC commands to generate an avatar that can run CPU level instructions
    public class AnimatorAsAssembly : MonoBehaviour
    {
        public VRCAvatarDescriptor avatar;
        public AnimatorController assetContainer;
        public string assetKey;

        [HideInInspector]
        public int RegistersUsed = 0;

        //[ReadOnly]
        public LBL[] Labels = new LBL[0];

        [Header("Compiler Options")]
        [Tooltip(
            "This is used to determine the maximum size of the stack. If you are using a lot of PUSH, POP or subroutine commands, you may need to increase this."
        )]
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
        public GameObject screenRoot;
        public GameObject pixelPrefab;

        [Header("Contact Senders")]
        public GameObject[] contactSenders;

        [CodeArea]
        public string RAWINSTRUCTIONS;

        [CodeArea(true)]
        public string CompiledCode;

        private AacFlBase aac;

        public void Create()
        {
            try
            {
                /*
                //Attempt to modify where the animator controller window is looking at, in order to prevent a redraw callback happening every edit
                //This is not a perfect solution, but it does help SIGNFICANTLY
                //get a reference to AnimatorControllerTool
                Type animatorWindowType = Type.GetType("UnityEditor.Graphs.AnimatorControllerTool, UnityEditor.Graphs");
                var window = EditorWindow.GetWindow(animatorWindowType);
                foreach (var property in animatorWindowType.GetProperties())
                {
                    Debug.Log(property.Name + " " + property.PropertyType + " " + property.GetValue(window));
                }
                //change the selected layer to 0
                animatorWindowType.GetProperty("selectedLayerIndex").SetValue(window, 0);
                */

                //Place the Asset Database in a state where
                //importing is suspended for most APIs
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("Clearing Asset", "Clearing Asset", 0.1f);
                //list how many sub assets are in the controller
                var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(
                    AssetDatabase.GetAssetPath(assetContainer)
                );
                Debug.Log("Sub Assets: " + allSubAssets.Length);
                //remove all sub assets except for the controller
                for (int i = 0; i < allSubAssets.Length; i++)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "Clearing Asset",
                        "Clearing Asset",
                        ((float)i / (float)allSubAssets.Length)
                    );
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
                    if (
                        allSubAssets[i].GetType() != typeof(AnimatorController)
                        && allSubAssets[i].GetType() != typeof(AnimatorStateMachine)
                    )
                    {
                        DestroyImmediate(allSubAssets[i], true);
                    }

                    //if it is the animator controller, remove all parameters
                    if (allSubAssets[i].GetType() == typeof(AnimatorController))
                    {
                        //remove all parameters, which are objects in the animator controller AnimatorParameters array
                        var controller = (AnimatorController)allSubAssets[i];
                        controller.parameters = new AnimatorControllerParameter[0];
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

                aac = AacExample.AnimatorAsCode(
                    "COMPUTER",
                    avatar,
                    assetContainer,
                    assetKey,
                    AacExample.Options().WriteDefaultsOn()
                );

                var FX = aac.CreateMainFxLayer();

                Register[] registers = new Register[0];

                //create a list of states that will be used to store the program
                AacFlState[,] Instructions = new AacFlState[1, 1];

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

        /// <summary> Generates a display on the avatar for the CPU to interface with </summary>
        /// <remarks> This function is called by the Create() function.
        /// The display is a 2d array of boolean values.
        /// All GPU registers use a * prefix to differentiate them from the CPU registers.</remarks>
        /// <param name="FX"> The main FX layer </param>
        /// <param name="registers"> The registers that are used by the CPU </param>
        /// <param name="width"> The width of the display </param>
        /// <param name="height"> The height of the display </param>
        public void GenerateDisplay(AacFlLayer FX, Register[] registers, int width, int height)
        {
            //progress bar
            EditorUtility.DisplayProgressBar("Compiling", "Generating Display", 0.1f);

            Profiler.BeginSample("GenerateDisplay");

            //create the horizontal drive layer
            AacFlLayer GPU = aac.CreateSupportingFxLayer("GPU");

            //remove all previous pixel emitters
            var tempList = screenRoot.transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                DestroyImmediate(child.gameObject);
            }

            AacFlFloatParameter[] VRAM = new AacFlFloatParameter[width * height];

            //create the registers for the VRAM
            for (int i = 0; i < VRAM.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                VRAM[i] = FX.FloatParameter("*VRAM_" + x + "," + y);
            }

            //create a gameobject for each pixel in the display
            GameObject[] PE = new GameObject[width * height];
            for (int i = 0; i < PE.Length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Compiling",
                    "Generating Gameobjects",
                    ((float)i / (float)PE.Length)
                );
                int x = i % width;
                int y = i / width;
                PE[i] = Instantiate(pixelPrefab, screenRoot.transform);
                PE[i].transform.parent = screenRoot.transform;
                PE[i].transform.localPosition = new Vector3(x, -y, 0);
                //set rotation to point negative z towards local negative z
                PE[i].transform.localRotation = Quaternion.Euler(0, 0, 180);
                PE[i].name = "Pixel " + x + "," + y;
            }

            //create a blendtree to drive the screen
            BlendTree blendTree = aac.NewBlendTreeAsRaw();
            blendTree.blendType = BlendTreeType.Direct;

            //create a state with the blendtree in the GPU
            AacFlState GPUState = GPU.NewState("GPU State");
            //set the motion to the blendtree
            GPUState.State.motion = blendTree;

            //generate a toggle animation for every single pixel gameobject
            AacFlClip[] clips = new AacFlClip[PE.Length];
            for (int i = 0; i < PE.Length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Compiling",
                    "Generating Animations",
                    ((float)i / (float)PE.Length)
                );
                //clips[i] = aac.NewClip().Toggling(PE[i], true); //takes up 2 frames
                clips[i] = aac.NewClip()
                    .Animating(clip =>
                    {
                        clip.Animates(PE[i])
                            .WithFrameCountUnit(keyframes => keyframes.Bool(0, true));
                    });
                //disable the pixel gameobject
                PE[i].SetActive(false);
                //add the pixel gameobject animation to the blendtree
                blendTree.AddChild(clips[i].Clip);
            }

            //get the children array
            var children = blendTree.children;
            for (int i = 0; i < PE.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                //set the blendtree child's parameter to the VRAM register
                children[i].directBlendParameter = "*VRAM_" + x + "," + y;
            }
            blendTree.children = children;

            EditorUtility.ClearProgressBar();
        }

        /// <summary> Generates a contact sender layer for each contact sender </summary>
        /// <remarks> This function is called by the Create() function.
        /// The contact sender layer is a layer that will turn the contact sender gameobject on and off depending on a boolean with the same name as the contact sender itself.</remarks>
        /// <param name="FX"> The main FX layer </param>
        public void GenerateContactSenderSystem(AacFlLayer FX)
        {
            //progress bar
            EditorUtility.DisplayProgressBar("Compiling", "Generating Contact Sender System", 0.1f);

            foreach (GameObject sender in contactSenders)
            {
                EditorUtility.DisplayProgressBar(
                    "Compiling",
                    "Generating Contact Sender System {Sender " + sender.name + "}",
                    0.5f
                );
                //create a new layer for the contact sender
                AacFlLayer ContactSenderLayer = aac.CreateSupportingFxLayer(
                    sender.name + " Contact Sender"
                );
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
                AacFlClip OnAnimation = aac.NewClip(sender.name + " On")
                    .Animating(clip =>
                    {
                        clip.Animates(sender)
                            .WithFrameCountUnit(keyframes => keyframes.Bool(0, true));
                    });

                On.WithAnimation(OnAnimation);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary> Cleans up the raw instructions </summary>
        /// <remarks> This function is called by the Create() function.
        /// Cleanup removes all comments from the raw instructions.
        /// Cleanup also removes all new lines from the raw instructions, unless a ; is present. </remarks>
        /// <param name="raw"> The raw instructions </param>
        /// <returns> The cleaned up instruction string</returns>
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
                                    output =
                                        output.Substring(0, output.IndexOf(line3))
                                        + "JSR "
                                        + j
                                        + output.Substring(
                                            output.IndexOf(line3) + 4 + line2.Length
                                        );
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
                    output =
                        output.Substring(0, output.IndexOf(line))
                        + "NOCONNECT"
                        + output.Substring(output.IndexOf(line) + line.Length);
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
                    Labels = Util.CopyIntoArray(Labels, newLabel);
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

                    if (
                        line.Contains("JEN ")
                        || line.Contains("JNEN ")
                        || line.Contains("JEQ ")
                        || line.Contains("JIG ")
                    )
                    {
                        string[] split = line.Split(' ');
                        //make sure this is the right label
                        if (split[3] == Labels[i].Name)
                        {
                            //dont use replace as it is messy
                            //output = output.Replace(line, split[0] + " " + split[1] + " " + split[2] + " " + Labels[i].Line);
                            output =
                                output.Substring(0, output.IndexOf(line))
                                + split[0]
                                + " "
                                + split[1]
                                + " "
                                + split[2]
                                + " "
                                + Labels[i].Line
                                + output.Substring(output.IndexOf(line) + line.Length);
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
                            output =
                                output.Substring(0, output.IndexOf(line))
                                + split[0]
                                + " "
                                + Labels[i].Line
                                + output.Substring(output.IndexOf(line) + line.Length);
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

        /// <summary> Correlates the paths in the graph to the instructions in the program </summary>
        /// <remarks> This organizes the graph if enabled, adding a dummy state above each instruction to show what line it is on
        /// this also handles connecting each instruction to the next instruction </remarks>
        /// <param name="Instructions"> The instructions to correlate. X in the 2D array is the instruction number, Y is the individual states that make up that instruction</param>
        /// <param name="FX"> The FX layer to correlate </param>
        /// <param name="raw"> The raw program </param>
        /// <param name="registers"> The registers to correlate </param>
        public void CorrelatePaths(
            AacFlState[,] Instructions,
            AacFlLayer FX,
            string raw,
            Register[] registers
        )
        {
            Profiler.BeginSample("Path Correlation");

            //progress bar
            EditorUtility.DisplayProgressBar("Correlating Paths", "Correlating Paths", 0);

            //split the instructions into an array
            string[] instructions = raw.Split('\n');

            //place every opcode based on their index in the array
            //[x, y]
            if (organizeGraph)
            {
                EditorUtility.DisplayProgressBar("Correlating Paths", "Organizing Graph", 0f);
                Vector2 zero = new Vector2(0, 1000);
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
                        Instructions[x, y].Shift(
                            zero,
                            (x * horizontalGraphScale),
                            y * verticalGraphScale
                        );
                        EditorUtility.DisplayProgressBar(
                            "Correlating Paths",
                            "Organizing Graph",
                            (float)x / (float)Instructions.GetLength(0)
                        );
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
                EditorUtility.DisplayProgressBar(
                    "Correlating Paths",
                    "Correlating Paths {" + instruction + "}",
                    (float)i / (float)instructions.Length
                );

                //try to connect the previous instruction to the current one
                //do this by connecting the Last node in the previous instruction to the first node in the current instruction
                AacFlState PreviousFirstNode = null;
                AacFlState PreviousLastNode = null;
                AacFlState CurrentFirstNode = null;
                AacFlState CurrentLastNode = null;

                try
                {
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
                    /*                     case "JMP":
                                            CurrentLastNode.AutomaticallyMovesTo(
                                                Instructions[int.Parse(instructionParts[1]), 0]
                                            );
                                            //modify PC
                                            CurrentLastNode.Drives(
                                                FX.IntParameter("&PC"),
                                                int.Parse(instructionParts[1])
                                            );
                                            break;
                                        case "JSR":
                                            CurrentLastNode.AutomaticallyMovesTo(
                                                Instructions[int.Parse(instructionParts[1]), 0]
                                            );
                                            //modify PC
                                            CurrentLastNode.Drives(
                                                FX.IntParameter("&PC"),
                                                int.Parse(instructionParts[1])
                                            );
                                            break;
                                        case "JEN": //JEN (Jump If Equal To Number) Jumps to the line in the program list if the register is equal to the number
                                            var JENPCMod = FX.NewState("{JEN} MODIFY PC");
                                            JENPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                                            JENPCMod.AutomaticallyMovesTo(
                                                Instructions[int.Parse(instructionParts[3]), 0]
                                            );
                                            trueCon = CurrentLastNode.TransitionsTo(JENPCMod);
                                            trueCon.When(
                                                Register
                                                    .FindRegisterInArray(instructionParts[1], registers)
                                                    .param.IsEqualTo(int.Parse(instructionParts[2]))
                                            );
                    
                                            //make sure if the condition is not met, that the PC is incremented
                                            CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                                            break;
                                        case "JNEN": //JNEN (Jump If Not Equal To Number) Jumps to the line in the program list if the register is not equal to the number
                                            var JNENPCMod = FX.NewState("{JNEN} MODIFY PC");
                                            //JNENPCMod is the state that modifies the PC if the JNEN condition is true, then we jump to the line in the program list
                                            JNENPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                                            JNENPCMod.AutomaticallyMovesTo(
                                                Instructions[int.Parse(instructionParts[3]), 0]
                                            );
                                            falseCon = CurrentLastNode.TransitionsTo(JNENPCMod);
                                            falseCon.When(
                                                Register
                                                    .FindRegisterInArray(instructionParts[1], registers)
                                                    .param.IsNotEqualTo(int.Parse(instructionParts[2]))
                                            );
                    
                                            //make sure if the condition is not met, that the PC is incremented
                                            CurrentFirstNode.DrivingIncreases(FX.IntParameter("&PC"), 1);
                                            break;
                                        case "JEQ": //JEQ (Jump If Equal To) Compares the first register to the second register, and if they are equal it jumps to the line in the program list
                                            //if the JEQR flag is true, then we jump to the line in the program list
                                            //if the JEQR flag is false, then we dont jump, letting the program continue as normal
                                            var JEQPCMod = FX.NewState("{JEQ} MODIFY PC");
                                            JEQPCMod.Drives(FX.IntParameter("&PC"), int.Parse(instructionParts[3]));
                                            JEQPCMod.AutomaticallyMovesTo(
                                                Instructions[int.Parse(instructionParts[3]), 0]
                                            );
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
                                            JIGPCMod.AutomaticallyMovesTo(
                                                Instructions[int.Parse(instructionParts[3]), 0]
                                            );
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
                                                var RTSState = FX.NewState("{RTS} " + rtsLines[j])
                                                    .DrivingIncreases(FX.IntParameter("&PC"), 1);
                                                //throw an exception if there is no state to go to after the JSR
                                                if (rtsLines[j] + 1 >= Instructions.GetLength(0))
                                                {
                                                    throw new Exception(
                                                        "RTS instruction at line "
                                                            + i
                                                            + " is trying to go to a line that does not exist. The JSR instruction at line "
                                                            + i
                                                            + " needs a opcode after it."
                                                    );
                                                }
                                                RTSState.AutomaticallyMovesTo(Instructions[rtsLines[j] + 1, 0]);
                                                trueCon = CurrentLastNode.TransitionsTo(RTSState);
                                                trueCon.When(FX.IntParameter("&PC").IsEqualTo(rtsLines[j]));
                                            }
                                            break; */
                    default:
                        try
                        {
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

        /// <summary> Parses the instructions. </summary>
        /// <param name="raw"> The raw instructions to parse. </param>
        /// <param name="FX"> The FX layer. </param>
        /// <param name="registers"> [out] The registers that were found in the instructions. </param>
        /// <returns> The parsed instructions. </returns>
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
                EditorUtility.DisplayProgressBar(
                    "Parsing Instructions",
                    "Parsing Instruction {" + instruction + "}",
                    (float)i / (float)instructions.Length
                );

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
                            Register newReg = new Register(part, FX);

                            //increment the ammount of registers used
                            RegistersUsed++;

                            //add it into the registers array
                            Registers = Util.CopyIntoArray(Registers, newReg);
                        }
                    }
                }

                //create the relevant state
                switch (instructionType)
                {
                    case "HALFADDER":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.HALFADDER(
                                FX.BoolParameter(instructionParts[1]),
                                FX.BoolParameter(instructionParts[2]),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "FULLADDER":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.FULLADDER(
                                FX.BoolParameter(instructionParts[1]),
                                FX.BoolParameter(instructionParts[2]),
                                FX.BoolParameter(instructionParts[3]),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "LD":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.LD(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                int.Parse(instructionParts[2]),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "ADD":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.ADD(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                Register.FindRegisterInArray(instructionParts[2], Registers),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "SUB":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.SUB(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                Register.FindRegisterInArray(instructionParts[2], Registers),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "INC":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.INC(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "FLIP":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.FLIP(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "COMPLEMENT":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.COMPLEMENT(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                FX
                            ).states,
                            i
                        );
                        break;
                    case "MOV":
                        Instructions = Util.CopyIntoArray(
                            Instructions,
                            new Commands.MOV(
                                Register.FindRegisterInArray(instructionParts[1], Registers),
                                Register.FindRegisterInArray(instructionParts[2], Registers),
                                FX
                            ).states,
                            i
                        );
                        break;
                    default:
                        //throw an exception if the instruction is not valid
                        throw new Exception("Invalid instruction: " + instruction);
                }
            }

            registers = Registers;
            Profiler.EndSample();
            //end the progress bar
            EditorUtility.ClearProgressBar();
            return Instructions;
        }
    }

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
