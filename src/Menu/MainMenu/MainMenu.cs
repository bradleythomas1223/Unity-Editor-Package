﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MelonLoader;

namespace Explorer
{
    public class MainMenu
    {
        public static MainMenu Instance;

        public MainMenu()
        {
            Instance = this;

            Pages.Add(new ScenePage());
            Pages.Add(new SearchPage());
            Pages.Add(new ConsolePage());

            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                page.Init();
            }
        }

        public const int MainWindowID = 5000;
        public static Rect MainRect = new Rect(5,5, ModConfig.Instance.Default_Window_Size.x,ModConfig.Instance.Default_Window_Size.y);

        public static readonly List<WindowPage> Pages = new List<WindowPage>();
        private static int m_currentPage = 0;

        public static void SetCurrentPage(int index)
        {
            if (index < 0 || Pages.Count <= index)
            {
                MelonLogger.Log("cannot set page " + index);
                return;
            }
            m_currentPage = index;
            GUI.BringWindowToFront(MainWindowID);
            GUI.FocusWindow(MainWindowID);
        }

        public void Update()
        {
            Pages[m_currentPage].Update();
        }

        public void OnGUI()
        {
            MainRect = GUI.Window(MainWindowID, MainRect, (GUI.WindowFunction)MainWindow, CppExplorer.NAME);
        }

        private void MainWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, MainRect.width - 90, 20));

            if (GUI.Button(new Rect(MainRect.width - 90, 2, 80, 20), $"Hide ({ModConfig.Instance.Main_Menu_Toggle})"))
            {
                CppExplorer.ShowMenu = false;
                return;
            }

            GUIUnstrip.BeginArea(new Rect(5, 25, MainRect.width - 10, MainRect.height - 35), GUI.skin.box);

            MainHeader();

            var page = Pages[m_currentPage];

            page.scroll = GUIUnstrip.BeginScrollView(page.scroll);

            page.DrawWindow();

            GUIUnstrip.EndScrollView();

            MainRect = ResizeDrag.ResizeWindow(MainRect, MainWindowID);

            GUIUnstrip.EndArea();
        }

        private void MainHeader()
        {
            GUIUnstrip.BeginHorizontal();
            for (int i = 0; i < Pages.Count; i++)
            {
                if (m_currentPage == i)
                    GUI.color = Color.green;
                else
                    GUI.color = Color.white;

                if (GUIUnstrip.Button(Pages[i].Name))
                {
                    m_currentPage = i;
                }
            }
            GUIUnstrip.EndHorizontal();

            GUIUnstrip.BeginHorizontal();
            GUI.color = Color.white;
            InspectUnderMouse.EnableInspect = GUIUnstrip.Toggle(InspectUnderMouse.EnableInspect, "Inspect Under Mouse (Shift + RMB)");

            bool mouseState = CursorControl.ForceUnlockMouse;
            bool setMouse = GUIUnstrip.Toggle(mouseState, "Force Unlock Mouse (Left Alt)");
            if (setMouse != mouseState) CursorControl.ForceUnlockMouse = setMouse;

            WindowManager.TabView = GUIUnstrip.Toggle(WindowManager.TabView, "Tab View");
            GUIUnstrip.EndHorizontal();

            //GUIUnstrip.Space(10);
            GUIUnstrip.Space(10);

            GUI.color = Color.white;
        }
    }
}
