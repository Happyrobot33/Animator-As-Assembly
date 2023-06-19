#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace AnimatorAsAssembly
{
    public class AnimatorAsAssemblyGraphVisualizer : MonoBehaviour
    {
        public Animator animatorToVisualize;
        public float posScale = 0.1f;
        public int layerSeperation = 1;
        public GameObject statePrefab;
        public GameObject transitionPrefab;
        public bool merge = true;
        public float lerpScale = 10;

        public void visualizeGraph()
        {
            //clear all children of the gameobject this script is attached to
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                try
                {
                    //make sure it is a top level child
                    if (child != transform && child.parent == transform && child != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log(e);
                    continue;
                }
            }

            //get every state in the animator controller (animatorToVisualize)
            AnimatorController ac =
                animatorToVisualize.runtimeAnimatorController as AnimatorController;
            AnimatorControllerLayer[] asmArray = ac.layers;
            //for (int i = 0; i < asmArray.Length; i++)
            for (int i = 0; i < 2; i++)
            {
                AnimatorStateMachine asm = asmArray[i].stateMachine;
                ChildAnimatorState[] states = asm.states;
                foreach (ChildAnimatorState state in states)
                {
                    //create a gameobject for each state
                    GameObject stateGO = Instantiate(statePrefab, transform);
                    stateGO.name = state.state.name;
                    stateGO.transform.localPosition = new Vector3(
                        -state.position.x * posScale,
                        -state.position.y * posScale,
                        i * layerSeperation
                    );
                    //set parent of the state to the gameobject this script is attached to
                    stateGO.transform.SetParent(transform);
                }
                /*
                //determine the maximum and minimum transition length
                float max = 0;
                float min = 100000;
                foreach (ChildAnimatorState state in states)
                {
                    AnimatorStateTransition[] transitions = state.state.transitions;
                    foreach (AnimatorStateTransition transition in transitions)
                    {
                        Vector3 sourcePos = new Vector3(-state.position.x * posScale, -state.position.y * posScale, i * layerSeperation);
                        AnimatorState destinationState = transition.destinationState;
                        //find the destination state in the list of states
                        ChildAnimatorState actualDestinationState = new ChildAnimatorState();
                        foreach (ChildAnimatorState state2 in states)
                        {
                            if (state2.state == destinationState)
                            {
                                actualDestinationState = state2;
                                break;
                            }
                        }
                        Vector3 destPos = new Vector3(-actualDestinationState.position.x * posScale, -actualDestinationState.position.y * posScale, i * layerSeperation);
                        float distance = Vector3.Distance(sourcePos, destPos);
                        if (distance > max)
                        {
                            max = distance;
                        }
                        if (distance < min)
                        {
                            min = distance;
                        }
                    }
                }
                */

                //visualize all the transitions, by stretching a line between the states
                foreach (ChildAnimatorState state in states)
                {
                    AnimatorStateTransition[] transitions = state.state.transitions;
                    foreach (AnimatorStateTransition transition in transitions)
                    {
                        //create a gameobject for each transition
                        GameObject transitionGO = Instantiate(transitionPrefab, transform);
                        transitionGO.name = transition.name;
                        //set parent of the transition to the gameobject this script is attached to
                        transitionGO.transform.SetParent(transform);
                        //get the position of the source and destination states
                        Vector3 sourcePos = new Vector3(
                            -state.position.x * posScale,
                            -state.position.y * posScale,
                            i * layerSeperation
                        );
                        AnimatorState destinationState = transition.destinationState;
                        //find the destination state in the list of states
                        ChildAnimatorState actualDestinationState = new ChildAnimatorState();
                        foreach (ChildAnimatorState state2 in states)
                        {
                            if (state2.state == destinationState)
                            {
                                actualDestinationState = state2;
                                break;
                            }
                        }
                        Vector3 destPos = new Vector3(
                            -actualDestinationState.position.x * posScale,
                            -actualDestinationState.position.y * posScale,
                            i * layerSeperation
                        );
                        //set the position of the transition gameobject to the middle of the source and destination states
                        transitionGO.transform.localPosition = (sourcePos + destPos) / 2;
                        //move the transitions behind the states
                        transitionGO.transform.localPosition = new Vector3(
                            transitionGO.transform.localPosition.x,
                            transitionGO.transform.localPosition.y,
                            -0.2f + (i * layerSeperation)
                        );
                        //make the smallest distance transitions 0 and the largest distance transitions 1
                        //float remapDistance = Mathf.InverseLerp(max, min, Vector3.Distance(sourcePos, destPos)) * lerpScale;
                        //move the transitions based on the depth of the transition
                        //transitionGO.transform.localPosition = new Vector3(transitionGO.transform.localPosition.x, transitionGO.transform.localPosition.y, transitionGO.transform.localPosition.z - remapDistance);
                        //set the scale of the transition gameobject to the distance between the source and destination states
                        transitionGO.transform.localScale = new Vector3(
                            transitionGO.transform.localScale.x,
                            transitionGO.transform.localScale.y,
                            Vector3.Distance(sourcePos, destPos)
                        );
                        //rotate the transition gameobject to face the destination state
                        transitionGO.transform.localRotation = Quaternion.LookRotation(
                            destPos - sourcePos
                        );
                    }
                }
            }

            if (merge)
            {
                //get all of the children again
                children = GetComponentsInChildren<Transform>();
                //remove the gameobject this script is attached to
                Transform[] children2 = new Transform[children.Length - 1];
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != transform)
                    {
                        children2[i - 1] = children[i];
                    }
                }
                children = children2;
                //remove any children that dont have a mesh filter or mesh renderer
                Transform[] children3 = new Transform[children.Length];
                int j = 0;
                for (int i = 0; i < children.Length; i++)
                {
                    if (
                        children[i].GetComponent<MeshFilter>() != null
                        && children[i].GetComponent<MeshRenderer>() != null
                    )
                    {
                        children3[j] = children[i];
                        j++;
                    }
                }
                children = children3;
                //trim the array to remove null values
                Transform[] children4 = new Transform[j];
                for (int i = 0; i < j; i++)
                {
                    children4[i] = children[i];
                }
                children = children4;

                //create a merge gameobject with a mesh filter and mesh renderer
                GameObject mergeGO = new GameObject("Merge");
                mergeGO.transform.SetParent(transform);
                mergeGO.AddComponent<MeshFilter>();
                mergeGO.AddComponent<MeshRenderer>();

                //for each different material in all the children, create a different Combine instance
                //get all unique materials
                Material[] materials = new Material[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    materials[i] = children[i].GetComponent<MeshRenderer>().sharedMaterial;
                }
                Material[] uniqueMaterials = new Material[0];
                //add each material to the unique materials array if it is not already in there
                foreach (Material material in materials)
                {
                    bool found = false;
                    foreach (Material material2 in uniqueMaterials)
                    {
                        if (material == material2)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Material[] uniqueMaterials2 = new Material[uniqueMaterials.Length + 1];
                        for (int i = 0; i < uniqueMaterials.Length; i++)
                        {
                            uniqueMaterials2[i] = uniqueMaterials[i];
                        }
                        uniqueMaterials2[uniqueMaterials.Length] = material;
                        uniqueMaterials = uniqueMaterials2;
                    }
                }
                Mesh[] submeshes = new Mesh[uniqueMaterials.Length];
                //create a combine instance for each unique material
                foreach (Material material in uniqueMaterials)
                {
                    CombineInstance[] combine = new CombineInstance[children.Length];
                    int k = 0;
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (children[i].GetComponent<MeshRenderer>().sharedMaterial == material)
                        {
                            combine[k].mesh = children[i].GetComponent<MeshFilter>().sharedMesh;
                            combine[k].transform = children[i].transform.localToWorldMatrix;
                            k++;
                        }
                    }
                    //trim the combine instance array to remove null values
                    CombineInstance[] combine2 = new CombineInstance[k];
                    for (int i = 0; i < k; i++)
                    {
                        combine2[i] = combine[i];
                    }
                    combine = combine2;
                    //create a mesh from the combine instance
                    Mesh mesh = new Mesh();
                    mesh.CombineMeshes(combine);
                    //add the mesh to the submeshes array
                    for (int i = 0; i < uniqueMaterials.Length; i++)
                    {
                        if (uniqueMaterials[i] == material)
                        {
                            submeshes[i] = mesh;
                        }
                    }
                }

                //create a mesh from the submeshes, combine
                CombineInstance[] combine3 = new CombineInstance[submeshes.Length];
                for (int i = 0; i < submeshes.Length; i++)
                {
                    combine3[i].mesh = submeshes[i];
                    combine3[i].transform = Matrix4x4.identity;
                    //set the submesh index
                    //combine3[i].subMeshIndex = i;
                    //combine3[i].mesh.subMeshCount = submeshes.Length;
                }
                Mesh mesh2 = new Mesh();
                //set the index format to the largest possible
                mesh2.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh2.subMeshCount = submeshes.Length;
                mesh2.CombineMeshes(combine3, false);
                mesh2.subMeshCount = submeshes.Length;
                //set the mesh of the merge gameobject
                mergeGO.GetComponent<MeshFilter>().sharedMesh = mesh2;
                //set the materials of the merge gameobject
                mergeGO.GetComponent<MeshRenderer>().sharedMaterials = uniqueMaterials;
                /*
                //sample all of the children and create a single mesh from them
                MeshFilter[] meshFilters = new MeshFilter[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    meshFilters[i] = children[i].GetComponent<MeshFilter>();
                }
                CombineInstance[] combine = new CombineInstance[meshFilters.Length];
                Material[] materials = new Material[2];
                materials[0] = statePrefab.GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial;
                materials[1] = transitionPrefab.GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial;
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    combine[i].mesh = meshFilters[i].sharedMesh;
                    combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                    //set the submesh index of the mesh to 0 or 1 depending on the material
                    if (meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial == materials[0])
                    {
                        combine[i].subMeshIndex = 0;
                    }
                    else if (meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial == materials[1])
                    {
                        combine[i].subMeshIndex = 1;
                    }
                }
                mergeGO.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                mergeGO.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, false);
                */

                //clear all children of the gameobject this script is attached to
                Transform[] childrenAll = GetComponentsInChildren<Transform>();
                foreach (Transform child in childrenAll)
                {
                    try
                    {
                        //make sure it is a top level child
                        if (
                            child != transform
                            && child.parent == transform
                            && child != null
                            && child.gameObject != mergeGO
                        )
                        {
                            DestroyImmediate(child.gameObject);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log(e);
                        continue;
                    }
                }
            }
        }
    }

    // create the inspection window
    [CustomEditor(typeof(AnimatorAsAssemblyGraphVisualizer))]
    public class AnimatorAsAssemblyGraphVisualizerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            AnimatorAsAssemblyGraphVisualizer myScript = (AnimatorAsAssemblyGraphVisualizer)target;
            if (GUILayout.Button("Visualize Graph"))
            {
                myScript.visualizeGraph();
            }

            DrawDefaultInspector();
        }
    }
}
#endif
