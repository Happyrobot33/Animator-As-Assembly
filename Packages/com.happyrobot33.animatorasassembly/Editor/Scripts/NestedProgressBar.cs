#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;

namespace AnimatorAsAssembly
{
    public class NestedProgressBar : EditorWindow
    {
        private List<ProgressBar> progressBars = new List<ProgressBar>();
        public string windowTitle;

        public NestedProgressBar(string windowTitle)
        {
            titleContent = new GUIContent(windowTitle);
        }

        public void OnGUI()
        {
            float height = 0;
            foreach (ProgressBar progressBar in progressBars)
            {
                height += progressBar.render();
            }

            //Auto close the window if there are no progress bars left
            if (progressBars.Count == 0)
            {
                Close();
            }

            //this is hardcoded because I don't know how to get the height of the title bar
            height += 20;

            this.maxSize = new Vector2(500, height);
            this.minSize = new Vector2(500, height);
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
            Repaint();
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
        public float height;
        public float progress;
        internal NestedProgressBar parent;

        public void finish()
        {
            parent.removeProgressBar(this);
        }

        public float render()
        {
            GUIContent titleContent = new GUIContent(title);
            GUIContent descriptionContent = new GUIContent(description);
            GUIContent progressContent = new GUIContent((progress * 100).ToString() + "%");
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.label);
            GUIStyle progressStyle = new GUIStyle(EditorStyles.label);
            progressStyle.alignment = TextAnchor.MiddleCenter;
            progressStyle.normal.textColor = Color.black;

            EditorGUILayout.LabelField(titleContent, titleStyle);
            EditorGUILayout.LabelField(descriptionContent, descriptionStyle);

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
            EditorGUILayout.LabelField(progressContent, progressStyle);
            EditorGUILayout.EndHorizontal();

            float height = 0;
            height = titleStyle.CalcHeight(titleContent, containerRect.width);
            height += descriptionStyle.CalcHeight(descriptionContent, containerRect.width);
            height += progressStyle.CalcHeight(progressContent, containerRect.width);
            height += 10;
            trySetHeight(height);
            return this.height;
        }

        private void trySetHeight(float height)
        {
            //check to see if layout event
            if (Event.current.type == EventType.Layout)
            {
                this.height = height;
            }
        }

        public EditorCoroutine setProgress(float progress)
        {
            this.progress = progress;
            parent.Repaint();
            return null;
        }
    }
}

#endif
