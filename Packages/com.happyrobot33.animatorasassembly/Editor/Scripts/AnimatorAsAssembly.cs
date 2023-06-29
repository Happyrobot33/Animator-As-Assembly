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
using Unity.EditorCoroutines.Editor;
using System.Threading;

namespace AnimatorAsAssembly
{
    /// <summary>
    /// this file converts assembly based commands into AAC commands to generate an avatar that can run CPU level instructions
    /// </summary>
    public class AnimatorAsAssembly : MonoBehaviour
    {
        public VRCAvatarDescriptor avatar;
        public AnimatorController assetContainer;
        public string assetKey;
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

        /// <summary> This is a string list of all the instructions in the compiled code. index it with the program counter to get the current instruction </summary>
        [SerializeField]
        [HideInInspector]
        public List<string> instructionStringList = new List<string>();

        private AacFlBase aac;

        public IEnumerator<EditorCoroutine> Create()
        {
            try
            {
                ComplexProgressBar progressWindow = ScriptableObject.CreateInstance<ComplexProgressBar>();
                progressWindow.titleContent = new GUIContent("Animator As Assembly");
                ProgressBar mainProgressBar = progressWindow.RegisterNewProgressBar(
                    "Compiling",
                    "Compiling the code"
                );
                progressWindow.ShowUtility();
                yield return null;

                Util.EmptyController(assetContainer);

                AssetDatabase.StartAssetEditing();

                Profiler.BeginSample("Compile");
                instructionStringList = new List<string>();

                aac = AacExample.AnimatorAsCode(
                    "COMPUTER",
                    avatar,
                    assetContainer,
                    assetKey,
                    AacExample.Options().WriteDefaultsOn()
                );

                AacFlLayer ControllerLayer = aac.CreateMainFxLayer();
                yield return mainProgressBar.SetProgress(0.1f);

                //Initialize Global Variables
                new Globals(ControllerLayer);

                //create a dummy default state
                AacFlState DefaultState = ControllerLayer.NewState("Default");

                //remove comments
                string CleanedCode = Cleanup(RAWINSTRUCTIONS);
                yield return mainProgressBar.SetProgress(0.33f);

                //run the display generation code if the user wants it
                if (useDisplay)
                {
                    yield return EditorCoroutineUtility.StartCoroutine(GenerateDisplay(ControllerLayer, displayWidth, displayHeight, progressWindow), this);
                }

                GenerateContactSenderSystem(ControllerLayer);

                //read in the instructions
                List<Commands.OPCODE> Instructions = new List<Commands.OPCODE>();
                yield return EditorCoroutineUtility.StartCoroutine(
                    CompileMicroCode(
                        CleanedCode,
                        ControllerLayer,
                        progressWindow,
                        (List<Commands.OPCODE> instructions) => Instructions = instructions
                    ),
                    this
                );
                //List<Commands.OPCODE> Instructions = CompileMicroCode(CleanedCode, ControllerLayer);
                yield return mainProgressBar.SetProgress(0.66f);

                //correlate all of the instructions with their paths
                OrganizeGraph(Instructions);
                yield return mainProgressBar.SetProgress(1f);

                //create final connection between default state and the first instruction
                _ = DefaultState.AutomaticallyMovesTo(Instructions[0].States[0]);

                //remove all junk sub assets
                //Util.CleanAnimatorControllerAsset(AssetDatabase.GetAssetPath(assetContainer));

                //save the asset
                Profiler.EndSample();
                progressWindow.Close();
            }
            finally
            {
                //By adding a call to StopAssetEditing inside
                //a "finally" block, we ensure the AssetDatabase
                //state will be reset when leaving this function
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary> Generates a display on the avatar for the CPU to interface with </summary>
        /// <remarks> This function is called by the Create() function.
        /// The display is a 2d array of boolean values.
        /// All GPU registers use a * prefix to differentiate them from the CPU registers.</remarks>
        /// <param name="ControllerLayer"> The main FX layer </param>
        /// <param name="width"> The width of the display </param>
        /// <param name="height"> The height of the display </param>
        private IEnumerator<EditorCoroutine> GenerateDisplay(
            AacFlLayer ControllerLayer,
            int width,
            int height,
            ComplexProgressBar progressWindow
        )
        {
            //progress bar
            ProgressBar PB = progressWindow.RegisterNewProgressBar(
                "Compiling",
                "Generating Display"
            );
            yield return PB.SetProgress(0f);

            Profiler.BeginSample("GenerateDisplay");

            //create the horizontal drive layer
            AacFlLayer GPU = aac.CreateSupportingFxLayer("GPU");

            //remove all previous pixel emitters
            foreach (Transform child in screenRoot.transform.Cast<Transform>().ToList())
            {
                DestroyImmediate(child.gameObject);
            }

            Globals._PixelBuffer = new AacFlFloatParameter[width * height];
            Globals._PixelBufferSize = new Vector2Int(width, height);

            //create the registers for the VRAM
            for (int i = 0; i < Globals._PixelBuffer.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                Globals._PixelBuffer[i] = ControllerLayer.FloatParameter(Globals.PIXELBUFFERSTRINGPREFIX + x + "," + y);
            }

            //create a gameobject for each pixel in the display
            GameObject[] PE = new GameObject[width * height];
            for (int i = 0; i < PE.Length; i++)
            {
                yield return PB.SetProgress((float)i / PE.Length / 2);
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
                yield return PB.SetProgress(0.5f + ((float)i / PE.Length / 2));
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
            ChildMotion[] children = blendTree.children;
            for (int i = 0; i < PE.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                //set the blendtree child's parameter to the VRAM register
                children[i].directBlendParameter = Globals.PIXELBUFFERSTRINGPREFIX + x + "," + y;
            }
            blendTree.children = children;

            //finish the progress bar
            PB.Finish();
        }

        /// <summary> Generates a contact sender layer for each contact sender </summary>
        /// <remarks> This function is called by the Create() function.
        /// The contact sender layer is a layer that will turn the contact sender gameobject on and off depending on a boolean with the same name as the contact sender itself.</remarks>
        /// <param name="ControllerLayer"> The main FX layer </param>
        private void GenerateContactSenderSystem(AacFlLayer ControllerLayer)
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
                _ = OnTransition.When(ContactSenderBool.IsGreaterThan(0));
                //create a transition from the on state to the off state if the boolean is false
                AacFlTransition OffTransition = On.TransitionsTo(Off);
                _ = OffTransition.When(ContactSenderBool.IsEqualTo(0));

                //create the animation for the contact sender to be on
                AacFlClip OnAnimation = aac.NewClip(sender.name + " On")
                    .Animating(clip =>
                    {
                        clip.Animates(sender)
                            .WithFrameCountUnit(keyframes => keyframes.Bool(0, true));
                    });

                _ = On.WithAnimation(OnAnimation);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary> Cleans up the raw instructions </summary>
        /// <remarks> This function is called by the Create() function.
        /// Cleanup removes all comments from the raw instructions.
        /// Cleanup also removes all new lines from the raw instructions, unless a ; is present. </remarks>
        /// <param name="raw"> The raw instructions </param>
        /// <returns> The cleaned up instruction string</returns>
        private string Cleanup(string raw)
        {
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
                else if (line?.Length == 0 || line == "\r" || line == "\n" || line == " ")
                {
                    //do nothing
                }
                else
                {
                    //add the line to the output
                    output += line + "\n";
                }
            }

            //remove the last \n
            output = output.Substring(0, output.Length - 1);

            Profiler.EndSample();
            return output;
        }

        /// <summary> Organizes the graph of the program </summary>
        /// <remarks> This organizes the graph if enabled, adding a dummy state above each instruction to show what line it is on </remarks>
        /// <param name="Instructions"> The instructions to correlate. X in the 2D array is the instruction number, Y is the individual states that make up that instruction</param>
        private void OrganizeGraph(
            List<Commands.OPCODE> Instructions)
        {
            Profiler.BeginSample("Path Correlation");

            //place every opcode based on their index in the array
            //[x, y]
            if (organizeGraph)
            {
                Vector2 zero = new Vector2(2, 5);
                Vector2 offset = new Vector2(0, 0);
                for (int index = 0; index < Instructions.Count; index++)
                {
                    //detect if we hit a SBR instruction, and if so reset the x of the offset
                    if (Instructions[index] is Commands.SBR)
                    {
                        offset.x = 0;
                        offset.y += verticalGraphScale;
                    }

                    offset += new Vector2(horizontalGraphScale, 0);

                    Instructions[index]._layer.Position =
                        zero + offset;
                }
            }
            Profiler.EndSample();
        }

        /// <summary> Parses the instructions. </summary>
        /// <param name="raw"> The raw instructions to parse. </param>
        /// <param name="ControllerLayer"> The FX layer. </param>
        /// <returns> The parsed instructions. </returns>
        private IEnumerator<EditorCoroutine> CompileMicroCode(
            string raw,
            AacFlLayer ControllerLayer,
            ComplexProgressBar progressBar,
            Action<List<Commands.OPCODE>> callback
        )
        {
            Profiler.BeginSample("CompileMicroCode");

            //split the instructions into an array
            string[] instructions = raw.Split('\n');

            //create a list of instructions that make up the program
            List<Commands.OPCODE> Instructions = new List<Commands.OPCODE>();

            //create a progress bar for the total progress
            ProgressBar microcodeProgress = progressBar.RegisterNewProgressBar(
                "Compiling MicroCode",
                "Compiling instructions to state MicroCode"
            );

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

                yield return microcodeProgress.SetProgress(i / (float)instructions.Length);

                //get the Commands namespace
                const string nameSpace = "AnimatorAsAssembly.Commands.";

                //create the relevant states based on the instruction type using reflection
                Type type = Type.GetType(nameSpace + instructionType);

                if (type == null)
                {
                    //replace it with a NOP
                    type = Type.GetType(nameSpace + "NOP");
                    Debug.LogError("Instruction type " + instructionType + " not found, replaced with NOP");
                }

                //create a new instance of the type
                object[] args = { instructionArgs, ControllerLayer, progressBar };
                Commands.OPCODE instance =
                    Activator.CreateInstance(type, args: args) as Commands.OPCODE;
                instance._base = aac;

                //this calls GenerateStates() on the instance
                yield return instance;

                //make every state in the instance increase the clock
                foreach (AacFlState state in instance.States)
                {
                    state.DrivingIncreases(Globals._Clock, 1);
                }

                //add the states to the list
                Instructions.Add(instance);

                instructionStringList.Add(instruction);
            }

            //end the progress bar
            microcodeProgress.Finish();

            Profiler.EndSample();

            //Link the microcode
            LinkMicroCode(Instructions);

            callback(Instructions);
        }

        /// <summary> Links the micro code by running each microcodes linker function. </summary>
        /// <param name="Instructions"> The instructions to link. </param>
        private void LinkMicroCode(List<Commands.OPCODE> Instructions)
        {
            Profiler.BeginSample("LinkMicroCode");

            //give each instruction a reference to the instruction list
            foreach (Commands.OPCODE instruction in Instructions)
            {
                instruction.ReferenceProgram = Instructions;
            }

            //loop through the instructions, linking the relevant states based on the instruction
            for (int i = 0; i < Instructions.Count; i++)
            {
                //link the states
                Instructions[i].Linker();
            }

            Profiler.EndSample();
            //end the progress bar
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// create a simple inspector window that has a button for init and a button for ReadInProgram
    /// </summary>
    [CustomEditor(typeof(AnimatorAsAssembly))]
    public class AnimatorAsAssemblyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AnimatorAsAssembly myScript = (AnimatorAsAssembly)target;

            if (GUILayout.Button("Create"))
            {
                //run the create function outside of OnInspectorGUI
                //myScript.Create();
                EditorCoroutineUtility.StartCoroutineOwnerless(myScript.Create());
            }

            //create settings header
            GUILayout.Label("Global Compiler Settings", EditorStyles.boldLabel);

            //show the bit depth as a field
            GUIContent bitDepthLabel = new GUIContent("Bit Depth", "The bit depth of the register. This is static for compilers in the unity editor.");
            Register.SetBitDepth(EditorGUILayout.IntField(bitDepthLabel, Register.BitDepth));

            //show the stack size as a field
            GUIContent stackSizeLabel = new GUIContent("Stack Size", "The size of the stack. This is static for compilers in the unity editor.");
            Globals.SetStackSize(EditorGUILayout.IntField(stackSizeLabel, Globals.StackSize));

            //create horizontal divider
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawDefaultInspector();
        }
    }
}
#endif
