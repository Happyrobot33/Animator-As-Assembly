#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimatorAsAssemblyGraphVisualizer : MonoBehaviour
{
    public Animator animatorToVisualize;
    public float posScale = 0.1f;
    public int layerSeperation = 1;
    public GameObject statePrefab;
    public GameObject transitionPrefab;
    public void visualizeGraph()
    {

        //clear all children of the gameobject this script is attached to
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            try {
                //make sure it is a top level child
                if (child != transform && child.parent == transform && child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            catch (System.Exception e)
            {
                //Debug.Log(e);
                continue;
            }
        }

        //get every state in the animator controller (animatorToVisualize)
        AnimatorController ac = animatorToVisualize.runtimeAnimatorController as AnimatorController;
        AnimatorControllerLayer[] asmArray = ac.layers;
        for (int i = 0; i < asmArray.Length; i++)
        {
            AnimatorStateMachine asm = asmArray[i].stateMachine;
            ChildAnimatorState[] states = asm.states;
            foreach (ChildAnimatorState state in states)
            {
                //create a gameobject for each state
                GameObject stateGO = Instantiate(statePrefab, transform);
                stateGO.name = state.state.name;
                stateGO.transform.localPosition = new Vector3(-state.position.x * posScale, -state.position.y * posScale, i * layerSeperation);
                //set parent of the state to the gameobject this script is attached to
                stateGO.transform.SetParent(transform);
            }

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
                    //set the position of the transition gameobject to the middle of the source and destination states
                    transitionGO.transform.localPosition = (sourcePos + destPos) / 2;
                    //move the transitions behind the states
                    transitionGO.transform.localPosition = new Vector3(transitionGO.transform.localPosition.x, transitionGO.transform.localPosition.y, -0.2f + (i * layerSeperation));
                    //set the scale of the transition gameobject to the distance between the source and destination states
                    transitionGO.transform.localScale = new Vector3(transitionGO.transform.localScale.x, transitionGO.transform.localScale.y, Vector3.Distance(sourcePos, destPos));
                    //rotate the transition gameobject to face the destination state
                    transitionGO.transform.localRotation = Quaternion.LookRotation(destPos - sourcePos);
                }
            }
        }

        /*
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
            if (children[i].GetComponent<MeshFilter>() != null && children[i].GetComponent<MeshRenderer>() != null)
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

        //sample all of the children and create a single mesh from them
        MeshFilter[] meshFilters = new MeshFilter[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            meshFilters[i] = children[i].GetComponent<MeshFilter>();
        }
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }
        mergeGO.GetComponent<MeshFilter>().sharedMesh = new Mesh();
        mergeGO.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);

        //clear all children of the gameobject this script is attached to
        Transform[] childrenAll = GetComponentsInChildren<Transform>();
        foreach (Transform child in childrenAll)
        {
            try {
                //make sure it is a top level child
                if (child != transform && child.parent == transform && child != null && child.gameObject != mergeGO)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            catch (System.Exception e)
            {
                //Debug.Log(e);
                continue;
            }
        }
        */
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

#endif
