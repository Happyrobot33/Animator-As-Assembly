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
            NPB.Show();

            PB1.finish();
        }

        private List<ProgressBar> progressBars = new List<ProgressBar>();
        public string windowTitle;

        public NestedProgressBar(string windowTitle)
        {
            titleContent = new GUIContent(windowTitle);
        }

        void OnGUI()
        {
            Repaint();
            foreach (ProgressBar progressBar in progressBars)
            {
                progressBar.render();
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

        public void removeProgressBar(ProgressBar progressBar)
        {
            progressBars.Remove(progressBar);
        }
    }
}

#endif
