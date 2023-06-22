#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AnimatorAsAssembly
{
    public class NestedProgressBar : EditorWindow
    {
        [MenuItem("Debug/Show Progress Bar")]
        public static void ShowProgressBar()
        {
            NestedProgressBar NPB = new NestedProgressBar("Test Window");
            ProgressBar PB1 = NPB.registerNewProgressBar(
                "Test Bar Title 1",
                "Test Bar Description 1"
            );
            ProgressBar PB2 = NPB.registerNewProgressBar(
                "Test Bar Title 2",
                "Test Bar Description 2"
            );
            PB1.progress = 0.5f;
            PB2.progress = 0.25f;
            NPB.ShowUtility();

            PB1.finish();
        }

        private List<ProgressBar> progressBars = new List<ProgressBar>();
        public string windowTitle;

        public NestedProgressBar(string windowTitle)
        {
            titleContent = new GUIContent(windowTitle);
        }

        public void OnGUI()
        {
            foreach (ProgressBar progressBar in progressBars)
            {
                progressBar.render();
            }

            //Auto close the window if there are no progress bars left
            if (progressBars.Count == 0)
            {
                Close();
            }
        }

        /// <summary> Registers a new progress bar and returns it </summary>
        /// <param name="title">The title of the progress bar</param>
        /// <param name="description">The description of the progress bar</param>
        /// <returns>The progress bar that was just created</returns>
        public ProgressBar registerNewProgressBar(string title, string description)
        {
            ProgressBar progressBar = new ProgressBar();
            progressBar.title = title;
            progressBar.description = description;
            progressBar.progress = 0;
            progressBar.parent = this;
            this.progressBars.Add(progressBar);
            return progressBar;
        }

        /// <summary> Removes a progress bar from the list of progress bars </summary>
        /// <param name="progressBar">The progress bar to remove</param>
        public void removeProgressBar(ProgressBar progressBar)
        {
            progressBars.Remove(progressBar);
        }
    }

    public class ProgressBar
    {
        public string title;
        public string description;
        public float progress;
        internal NestedProgressBar parent;

        public void finish()
        {
            parent.removeProgressBar(this);
        }

        public void render()
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description);
            //create the container rect
            Rect containerRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            //create the fill rect
            Rect fillRect = new Rect(containerRect);
            fillRect.width *= progress;
            //draw the empty rect
            EditorGUI.DrawRect(containerRect, Color.gray);
            //draw the fill rect
            EditorGUI.DrawRect(fillRect, Color.green);
            //end the horizontal layout
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            EditorGUILayout.LabelField((progress * 100).ToString() + "%", style);
            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif
