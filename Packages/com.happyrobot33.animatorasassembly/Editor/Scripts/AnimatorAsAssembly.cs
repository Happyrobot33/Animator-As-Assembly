#if UNITY_EDITOR
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using UnityEditor.Animations;
using AnimatorAsCode.Framework;
using AnimatorAsCode.Framework.Examples;
using UnityEngine.Profiling;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

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

        /// <summary> This is a string list of all the instructions in the compiled code. index it with the program counter to get the current instruction </summary>
        [SerializeField]
        public List<string> instructionStringList = new List<string>();

        private AacFlBase aac;

        public void Create()
        {
            try
            {
                //Place the Asset Database in a state where
                //importing is suspended for most APIs
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("Clearing Asset", "Clearing Asset", 0.1f);
                //list how many sub assets are in the controller
                var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(
                    AssetDatabase.GetAssetPath(assetContainer)
                );
                Debug.Log("Sub assets before cleanup: " + allSubAssets.Length);
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
                //list how many sub assets are in the controller
                allSubAssets = AssetDatabase.LoadAllAssetsAtPath(
                    AssetDatabase.GetAssetPath(assetContainer)
                );
                Debug.Log("Sub assets after cleanup: " + allSubAssets.Length);

                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Resources.UnloadUnusedAssets();
                EditorUtility.ClearProgressBar();
                AssetDatabase.StartAssetEditing();

                Profiler.BeginSample("Compile");
                RegistersUsed = 0;
                instructionStringList = new List<string>();

                aac = AacExample.AnimatorAsCode(
                    "COMPUTER",
                    avatar,
                    assetContainer,
                    assetKey,
                    AacExample.Options().WriteDefaultsOn()
                );

                var ControllerLayer = aac.CreateMainFxLayer();

                Register[] registers = new Register[0];

                //create a dummy default state
                AacFlState DefaultState = ControllerLayer.NewState("Default");

                //remove comments
                CompiledCode = Cleanup(RAWINSTRUCTIONS);

                //run the display generation code if the user wants it
                if (useDisplay)
                {
                    GenerateDisplay(ControllerLayer, registers, displayWidth, displayHeight);
                }

                GenerateContactSenderSystem(ControllerLayer);

                //read in the instructions
                List<Commands.OPCODE> Instructions = CompileMicroCode(
                    CompiledCode,
                    ControllerLayer
                );

                //correlate all of the instructions with their paths
                OrganizeGraph(Instructions, ControllerLayer, CompiledCode);

                //create final connection between default state and the first instruction
                DefaultState.AutomaticallyMovesTo(Instructions[0].states[0]);
                Profiler.EndSample();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError(e);
                //show a dialog box with the error
                EditorUtility.DisplayDialog(
                    "Error",
                    "An internal error occured while compiling the code. Please check the console for more information.",
                    "OK"
                );
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
        /// <param name="ControllerLayer"> The main FX layer </param>
        /// <param name="registers"> The registers that are used by the CPU </param>
        /// <param name="width"> The width of the display </param>
        /// <param name="height"> The height of the display </param>
        void GenerateDisplay(
            AacFlLayer ControllerLayer,
            Register[] registers,
            int width,
            int height
        )
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
                VRAM[i] = ControllerLayer.FloatParameter("*VRAM_" + x + "," + y);
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
        /// <param name="ControllerLayer"> The main FX layer </param>
        void GenerateContactSenderSystem(AacFlLayer ControllerLayer)
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
                AacFlIntParameter ContactSenderBool = ControllerLayer.IntParameter(sender.name);

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
        string Cleanup(string raw)
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

        /// <summary> Organizes the graph of the program </summary>
        /// <remarks> This organizes the graph if enabled, adding a dummy state above each instruction to show what line it is on
        /// this also handles connecting each instruction to the next instruction </remarks>
        /// <param name="Instructions"> The instructions to correlate. X in the 2D array is the instruction number, Y is the individual states that make up that instruction</param>
        /// <param name="ControllerLayer"> The FX layer to correlate </param>
        /// <param name="raw"> The raw program </param>
        void OrganizeGraph(
            List<Commands.OPCODE> Instructions,
            AacFlLayer ControllerLayer,
            string raw
        )
        {
            Profiler.BeginSample("Path Correlation");

            //progress bar
            EditorUtility.DisplayProgressBar("Organizing Graph", "Organizing Graph", 0);

            //split the instructions into an array
            string[] instructions = raw.Split('\n');

            //place every opcode based on their index in the array
            //[x, y]
            if (organizeGraph)
            {
                EditorUtility.DisplayProgressBar("Organizing Graph", "Organizing Graph", 0f);
                Vector2 zero = new Vector2(0, 1000);
                for (int x = 0; x < Instructions.Count; x++)
                {
                    //the Y may not be the same for every X, so we need to check for null
                    for (int y = 0; y < Instructions[x].Length; y++)
                    {
                        if (Instructions[x][y] == null)
                        {
                            //go to the next X
                            break;
                        }
                        //shift the instruction to the correct position
                        Instructions[x][y].Shift(
                            zero,
                            (x * horizontalGraphScale),
                            y * verticalGraphScale
                        );
                        EditorUtility.DisplayProgressBar(
                            "Correlating Paths",
                            "Organizing Graph",
                            (float)x / (float)Instructions.Count
                        );
                    }
                    //create a empty state above the instruction to denote what line it is on
                    AacFlState LineIndicator = ControllerLayer.NewState("Line: " + x);
                    LineIndicator.Over(Instructions[x][0]);
                }
                EditorUtility.ClearProgressBar();
            }

            //end the progress bar
            EditorUtility.ClearProgressBar();
            Profiler.EndSample();
        }

        /// <summary> Parses the instructions. </summary>
        /// <param name="raw"> The raw instructions to parse. </param>
        /// <param name="ControllerLayer"> The FX layer. </param>
        /// <returns> The parsed instructions. </returns>
        List<Commands.OPCODE> CompileMicroCode(string raw, AacFlLayer ControllerLayer)
        {
            Profiler.BeginSample("CompileMicroCode");

            //begin a progress bar
            EditorUtility.DisplayProgressBar("Compiling MicroCode", "Compiling MicroCode", 0);

            //split the instructions into an array
            string[] instructions = raw.Split('\n');

            //create a list of states that will be used to store the program
            //the max sub states a opcode can have is 20
            List<Commands.OPCODE> Instructions = new List<Commands.OPCODE>();

            //loop through the instructions, making the relevant states based on the instruction
            for (int i = 0; i < instructions.Length; i++)
            {
                string instruction = instructions[i];

                //split the instruction into an array
                string[] instructionParts = instruction.Split(' ');

                //create a array of everything but the instruction
                string[] instructionArgs = new string[instructionParts.Length - 1];
                for (int j = 1; j < instructionParts.Length; j++)
                {
                    instructionArgs[j - 1] = instructionParts[j];
                }

                //get the instruction type
                string instructionType = instructionParts[0];

                //progress bar
                EditorUtility.DisplayProgressBar(
                    "Compiling MicroCode",
                    "Compiling MicroCode {" + instruction + "}",
                    (float)i / (float)instructions.Length
                );

                //Initialize Global Variables
                new Globals(ControllerLayer);

                //get the Commands namespace
                string nameSpace = "AnimatorAsAssembly.Commands.";

                //create the relevant states based on the instruction type using reflection
                Type type = Type.GetType(nameSpace + instructionType);

                //create a new instance of the type
                if (type != null)
                {
                    object[] args = { instructionArgs, ControllerLayer };
                    Commands.OPCODE instance =
                        Activator.CreateInstance(type, args: args) as Commands.OPCODE;

                    //add the states to the list
                    Instructions.Add(instance);

                    instructionStringList.Add(instruction);
                }
                else
                {
                    Debug.LogError("Instruction type " + instructionType + " not found");
                }
            }

            Profiler.EndSample();
            //end the progress bar
            EditorUtility.ClearProgressBar();

            //Link the microcode
            LinkMicroCode(Instructions);

            return Instructions;
        }

        /// <summary> Links the micro code by running each microcodes linker function. </summary>
        /// <param name="Instructions"> The instructions to link. </param>
        void LinkMicroCode(List<Commands.OPCODE> Instructions)
        {
            Profiler.BeginSample("LinkMicroCode");

            //begin a progress bar
            EditorUtility.DisplayProgressBar("Linking MicroCode", "Linking MicroCode", 0);

            //loop through the instructions, linking the relevant states based on the instruction
            for (int i = 0; i < Instructions.Count; i++)
            {
                //progress bar
                EditorUtility.DisplayProgressBar(
                    "Linking MicroCode",
                    "Linking MicroCode",
                    (float)i / (float)Instructions.Count
                );

                //link the states
                Instructions[i].Link(Instructions);
            }

            Profiler.EndSample();
            //end the progress bar
            EditorUtility.ClearProgressBar();
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
