using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class WindowBase
    { 
        public WindowBase(int id, string title)
        {
            ID = id;
            Title = title;
        }

        public WindowBase(int id, string title, bool draggable, bool centered)
        {
            ID = id;
            Title = title;
            Draggable = draggable;
            CenterWindow = centered;
        }


        public int ID { get; private set; } = 8234;
        private Rect _windowRect = new Rect((Screen.width - 300) / 2, (Screen.height / 4), 300, 1);
        public Rect WindowRect
        {
            get { return _windowRect; }
            set { _windowRect = value; }
        }

        public string Title { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public bool CenterWindow { get; set; }
        public bool Draggable { get; set; } = true; //True by default

        /// <summary>
        /// Makes the window not visible
        /// </summary>
        public virtual void Close()
        {
            IsVisible = false;
        }

        /// <summary>
        /// Causes the window to be visible
        /// </summary>
        public virtual void Show()
        {
            IsVisible = true;
        }

        /// <summary>
        /// Draws the window
        /// </summary>
        /// <param name="windowID">The window's id</param>
        public virtual void Draw(int windowID)
        {
            if (Draggable)
            {
                dragWindow();
            }
        }



        /// <summary>
        /// Handles all drawing of the window and handling of the Rect object. Call from OnGUI and magic happens.
        /// </summary>
        /// <param name="id">The (preferably unique) int ID of the window.</param>
        /// <param name="title">The window's title.</param>
        public void OnGUIHandler()
        {
            if (IsVisible)
            {
                _windowRect = GUILayout.Window(ID, _windowRect, Draw, Title);
                if (CenterWindow)
                {
                    centerWindow();
                }
            }
        }

        /// <summary>
        /// Sets the size of the window
        /// </summary>
        /// <param name="left">The left edge of the window</param>
        /// <param name="top">The top edge of the window</param>
        /// <param name="width">The window width</param>
        /// <param name="height">The window height</param>
        public void SetSize(double left, double top, double width, double height)
        {
            //so we don't have to cast ourselves
            _windowRect = new Rect((float)left, (float)top, (float)width, (float)height);
        }


        /// <summary>
        /// Makes the window draggable
        /// </summary>
        protected void dragWindow()
        {
            if (!Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                GUI.DragWindow();
            }
        }

        /// <summary>
        /// Centers the window
        /// </summary>
        protected void centerWindow()
        {
            float newX = (Screen.width - WindowRect.width) / 2.0f;
            float newY = (Screen.height - WindowRect.height) / 2.0f;

            WindowRect = new Rect(newX, newY, WindowRect.width, WindowRect.height);
        }
    }
}
