﻿#if CPP
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;
using UnhollowerRuntimeLib;

namespace Explorer.UnstripInternals
{
    public class Internal
    {
        #region Properties
        public static int s_ScrollControlId;

        public static bool ScrollFailed = false;
        public static bool ManualUnstripFailed = false;        

        public static GenericStack ScrollStack => m_scrollStack ?? GetScrollStack();
        public static PropertyInfo m_scrollViewStatesInfo;
        public static GenericStack m_scrollStack;

        public static Dictionary<int, Il2CppSystem.Object> StateCache => m_stateCacheDict ?? GetStateCacheDict();
        public static Dictionary<int, Il2CppSystem.Object> m_stateCacheDict;

        public static GUIStyle SpaceStyle => m_spaceStyle ?? GetSpaceStyle();
        public static GUIStyle m_spaceStyle;

        public static DateTime nextScrollStepTime;

        public static MethodInfo ScreenToGuiPointMethod;
        public static bool m_screenToGuiAttemped;

        public static MethodInfo m_bringWindowToFrontMethod;
        public static bool m_bringWindowFrontAttempted;

        private static GenericStack GetScrollStack()
        {
            if (m_scrollViewStatesInfo == null)
            {
                if (typeof(GUI).GetProperty("scrollViewStates", ReflectionHelpers.CommonFlags) is PropertyInfo scrollStatesInfo)
                {
                    m_scrollViewStatesInfo = scrollStatesInfo;
                }
                else if (typeof(GUI).GetProperty("s_ScrollViewStates", ReflectionHelpers.CommonFlags) is PropertyInfo s_scrollStatesInfo)
                {
                    m_scrollViewStatesInfo = s_scrollStatesInfo;
                }
            }

            if (m_scrollViewStatesInfo?.GetValue(null, null) is GenericStack stack)
            {
                m_scrollStack = stack;
            }
            else
            {
                m_scrollStack = new GenericStack();
            }

            return m_scrollStack;
        }

        private static Dictionary<int, Il2CppSystem.Object> GetStateCacheDict()
        {
            if (m_stateCacheDict == null)
            {
                try
                {
                    var type = ReflectionHelpers.GetTypeByName("UnityEngine.GUIStateObjects");
                    m_stateCacheDict = type.GetProperty("s_StateCache")
                                            .GetValue(null, null)
                                            as Dictionary<int, Il2CppSystem.Object>;

                    if (m_stateCacheDict == null) throw new Exception();
                }
                catch 
                {
                    m_stateCacheDict = new Dictionary<int, Il2CppSystem.Object>();
                }
            }
            return m_stateCacheDict;
        }

        private static GUIStyle GetSpaceStyle()
        {
            try
            {
                m_spaceStyle = typeof(GUILayoutUtility)
                    .GetProperty("s_SpaceStyle")
                    .GetValue(null, null)
                    .Il2CppCast(typeof(GUIStyle))
                    as GUIStyle;

                if (m_spaceStyle == null) throw new Exception();
            }
            catch { }

            if (m_spaceStyle == null)
            {
                m_spaceStyle = new GUIStyle();
            }
            m_spaceStyle.stretchWidth = false;
            return m_spaceStyle;
        }

        #endregion

        #region GUILayout Methods

        public static string TextField(string text, GUILayoutOption[] options)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
            GUIContent guicontent = GUIContent.Temp(text);
            bool flag = GUIUtility.keyboardControl != controlID;
            if (flag)
            {
                guicontent = GUIContent.Temp(text);
            }
            else
            {
                guicontent = GUIContent.Temp(text);
                // guicontent = GUIContent.Temp(text + GUIUtility.compositionString);
            }
            Rect rect = Internal_LayoutUtility.GetRect(guicontent, GUI.skin.textField, options);
            bool flag2 = GUIUtility.keyboardControl == controlID;
            if (flag2)
            {
                guicontent = GUIContent.Temp(text);
            }
            DoTextField(rect, controlID, guicontent, false, -1, GUI.skin.textField);
            return guicontent.text;
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style)
        {
            if (GetStateObject(Il2CppType.Of<TextEditor>(), id).TryCast<TextEditor>() is TextEditor textEditor)
            {
                if (maxLength >= 0 && content.text.Length > maxLength)
                {
                    content.text = content.text.Substring(0, maxLength);
                }
                textEditor.m_Content.text = content.text;
                textEditor.SaveBackup();
                textEditor.position = position;
                textEditor.style = style;
                textEditor.multiline = multiline;
                textEditor.controlID = id;
                textEditor.DetectFocusChange();
                GUI.HandleTextFieldEventForDesktop(position, id, content, multiline, maxLength, style, textEditor);
                textEditor.UpdateScrollOffsetIfNeeded(Event.current);
            }
        }

        public static bool DoRepeatButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            return GUI.DoRepeatButton(Internal_LayoutUtility.GetRect(content, style, options), content, style, FocusType.Passive);
        }

        public static void Space(float pixels)
        {
            if (GUILayoutUtility.current.topLevel.isVertical)
                Internal_LayoutUtility.GetRect(0, pixels, SpaceStyle, new GUILayoutOption[] { GUILayout.Height(pixels) });
            else
                Internal_LayoutUtility.GetRect(pixels, 0, SpaceStyle, new GUILayoutOption[] { GUILayout.Width(pixels) });

            if (Event.current.type == EventType.Layout)
            {
                GUILayoutUtility.current.topLevel.entries[GUILayoutUtility.current.topLevel.entries.Count - 1].consideredForMargin = false;
            }
        }

        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
            if (!m_screenToGuiAttemped)
            {
                m_screenToGuiAttemped = true;
                ScreenToGuiPointMethod = typeof(GUIUtility).GetMethod("ScreenToGUIPoint");
            }
            if (ScreenToGuiPointMethod == null)
            {
                throw new Exception("Couldn't get method 'GUIUtility.ScreenToGUIPoint'!");
            }
            return (Vector2)ScreenToGuiPointMethod.Invoke(null, new object[] { screenPoint });
        }

        public static void BringWindowToFront(int id)
        {
            if (!m_bringWindowFrontAttempted)
            {
                m_bringWindowFrontAttempted = true;
                m_bringWindowToFrontMethod = typeof(GUI).GetMethod("BringWindowToFront");
            }
            if (m_bringWindowToFrontMethod == null)
            {
                throw new Exception("Couldn't get method 'GUIUtility.BringWindowToFront'!");
            }
            m_bringWindowToFrontMethod.Invoke(null, new object[] { id });
        }

        public static void BeginArea(Rect screenRect, GUIContent content, GUIStyle style)
        {
            var g = BeginLayoutArea(style, typeof(GUILayoutGroup));
            if (Event.current.type == EventType.Layout)
            {
                g.resetCoords = true;
                g.minWidth = g.maxWidth = screenRect.width;
                g.minHeight = g.maxHeight = screenRect.height;
                g.rect = Rect.MinMaxRect(screenRect.xMin, screenRect.yMin, g.rect.xMax, g.rect.yMax);
            }

            BeginGroup(g.rect, content, style);
        }

        internal static GUILayoutGroup BeginLayoutArea(GUIStyle style, Type layoutType)
        {
            EventType type = Event.current.type;
            GUILayoutGroup guilayoutGroup;
            if (type != EventType.Used && type != EventType.Layout)
            {
                guilayoutGroup = GUILayoutUtility.current.windows.GetNext().TryCast<GUILayoutGroup>();
                guilayoutGroup.ResetCursor();
            }
            else
            {
                guilayoutGroup = (GUILayoutGroup)Activator.CreateInstance(layoutType);
                guilayoutGroup.style = style;
                GUILayoutUtility.current.windows.Add(guilayoutGroup);
            }
            GUILayoutUtility.current.layoutGroups.Push(guilayoutGroup);
            GUILayoutUtility.current.topLevel = guilayoutGroup;
            return guilayoutGroup;
        }

        public static void BeginGroup(Rect position, GUIContent content, GUIStyle style)
        {
            BeginGroup(position, content, style, Vector2.zero);
        }

        internal static void BeginGroup(Rect position, GUIContent content, GUIStyle style, Vector2 scrollOffset)
        {
            int id = GUIUtility.GetControlID(GUI.s_BeginGroupHash, FocusType.Passive);

            if (content != GUIContent.none || style != GUIStyle.none)
            {
                switch (Event.current.type)
                {
                    case EventType.Repaint:
                        style.Draw(position, content, id);
                        break;
                    default:
                        if (position.Contains(Event.current.mousePosition))
                            GUIUtility.mouseUsed = true;
                        break;
                }
            }
            GUIClip.Push(position, scrollOffset, Vector2.zero, false);
        }

        public static void EndArea()
        {
            if (Event.current.type == EventType.Used)
                return;
            GUILayoutUtility.current.layoutGroups.Pop();
            GUILayoutUtility.current.topLevel = GUILayoutUtility.current.layoutGroups.Peek().TryCast<GUILayoutGroup>();
            GUI.EndGroup();
        }

#endregion

        #region Scrolling

        private static Il2CppSystem.Object GetStateObject(Il2CppSystem.Type type, int controlID)
        {
            Il2CppSystem.Object obj;
            if (StateCache.ContainsKey(controlID))
            {
                obj = StateCache[controlID];
            }
            else
            {
                obj = Il2CppSystem.Activator.CreateInstance(type);
                StateCache.Add(controlID, obj);
            }

            return obj;
        }

        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options)
        {
            // First, just try normal way, may not have been stripped or was unstripped successfully.
            if (!ScrollFailed)
            {
                try
                {
                    return GUILayout.BeginScrollView(scroll, options);
                }
                catch
                {
                    ScrollFailed = true;
                }
            }

            // Try manual implementation.
            if (!ManualUnstripFailed)
            {
                try
                {
                    return BeginScrollView_ImplLayout(scroll,
                        false,
                        false,
                        GUI.skin.horizontalScrollbar,
                        GUI.skin.verticalScrollbar,
                        GUI.skin.scrollView,
                        options);
                }
                catch (Exception e)
                {
                    ExplorerCore.Log("Exception on manual BeginScrollView: " + e.GetType() + ", " + e.Message + "\r\n" + e.StackTrace);
                    ManualUnstripFailed = true;
                }
            }

            // Sorry! No scrolling for you.
            return scroll;
        }

        internal static void EndScrollView(bool handleScrollWheel)
        {
            // Only end the scroll view for the relevant BeginScrollView option, if any.

            if (!ScrollFailed)
            {
                GUILayout.EndScrollView();
            }
            else if (!ManualUnstripFailed)
            {
                GUILayoutUtility.EndLayoutGroup();

                if (ScrollStack.Count <= 0) return;

                var state = ScrollStack.Peek().TryCast<ScrollViewState>();
                var scrollExt = Internal_ScrollViewState.FromPointer(state.Pointer);

                if (scrollExt == null) throw new Exception("Could not get scrollExt!");

                GUIClip.Pop();

                ScrollStack.Pop();

                var position = scrollExt.position;

                if (handleScrollWheel && Event.current.type == EventType.ScrollWheel && position.Contains(Event.current.mousePosition))
                {
                    var pos = scrollExt.scrollPosition;
                    pos.x = Mathf.Clamp(scrollExt.scrollPosition.x + Event.current.delta.x * 20f, 0f, scrollExt.viewRect.width - scrollExt.visibleRect.width);
                    pos.y = Mathf.Clamp(scrollExt.scrollPosition.y + Event.current.delta.y * 20f, 0f, scrollExt.viewRect.height - scrollExt.visibleRect.height);

                    if (scrollExt.scrollPosition.x < 0f)
                    {
                        pos.x = 0f;
                    }
                    if (pos.y < 0f)
                    {
                        pos.y = 0f;
                    }

                    scrollExt.apply = true;

                    Event.current.Use();
                }
            }
        }

        private static Vector2 BeginScrollView_ImplLayout(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical,
            GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            var guiscrollGroup = GUILayoutUtility.BeginLayoutGroup(background, null, Il2CppType.Of<GUIScrollGroup>())
                                                 .TryCast<GUIScrollGroup>();

            EventType type = Event.current.type;
            if (type == EventType.Layout)
            {
                guiscrollGroup.resetCoords = true;
                guiscrollGroup.isVertical = true;
                guiscrollGroup.stretchWidth = 1;
                guiscrollGroup.stretchHeight = 1;
                guiscrollGroup.verticalScrollbar = verticalScrollbar;
                guiscrollGroup.horizontalScrollbar = horizontalScrollbar;
                guiscrollGroup.needsVerticalScrollbar = alwaysShowVertical;
                guiscrollGroup.needsHorizontalScrollbar = alwaysShowHorizontal;
                guiscrollGroup.ApplyOptions(options);
            }

            return BeginScrollView_Impl(guiscrollGroup.rect,
                scrollPosition,
                new Rect(0f, 0f, guiscrollGroup.clientWidth, guiscrollGroup.clientHeight),
                alwaysShowHorizontal,
                alwaysShowVertical,
                horizontalScrollbar,
                verticalScrollbar,
                background
            );
        }

        private static Vector2 BeginScrollView_Impl(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal,
            bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            // GUIUtility.CheckOnGUI();

            int controlID = GUIUtility.GetControlID(GUI.s_ScrollviewHash, FocusType.Passive);

            var scrollViewState = GetStateObject(Il2CppType.Of<ScrollViewState>(), controlID)
                                    .TryCast<ScrollViewState>();

            if (scrollViewState == null) 
                return scrollPosition;

            var scrollExt = Internal_ScrollViewState.FromPointer(scrollViewState.Pointer);

            if (scrollExt == null)
                return scrollPosition;

            bool apply = scrollExt.apply;
            if (apply)
            {
                scrollPosition = scrollExt.scrollPosition;
                scrollExt.apply = false;
            }

            scrollExt.position = position;

            scrollExt.scrollPosition = scrollPosition;
            scrollExt.visibleRect = scrollExt.viewRect = viewRect;

            var rect = scrollExt.visibleRect;
            rect.width = position.width;
            rect.height = position.height;

            ScrollStack.Push(scrollViewState);

            Rect screenRect = new Rect(position.x, position.y, position.width, position.height);
            EventType type = Event.current.type;
            if (type != EventType.Layout)
            {
                if (type != EventType.Used)
                {
                    bool flag = alwaysShowVertical;
                    bool flag2 = alwaysShowHorizontal;
                    if (flag2 || viewRect.width > screenRect.width)
                    {
                        rect.height = position.height - horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;

                        screenRect.height -= horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;
                        flag2 = true;
                    }
                    if (flag || viewRect.height > screenRect.height)
                    {
                        rect.width = position.width - verticalScrollbar.fixedWidth + (float)verticalScrollbar.margin.left;

                        screenRect.width -= verticalScrollbar.fixedWidth + (float)verticalScrollbar.margin.left;
                        flag = true;
                        if (!flag2 && viewRect.width > screenRect.width)
                        {
                            rect.height = position.height - horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;
                            screenRect.height -= horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;
                            flag2 = true;
                        }
                    }
                    if (Event.current.type == EventType.Repaint && background != GUIStyle.none)
                    {
                        background.Draw(position, position.Contains(Event.current.mousePosition), false, flag2 && flag, false);
                    }
                    if (flag2 && horizontalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.x = HorizontalScroll(
                            new Rect(
                                position.x,
                                position.yMax - horizontalScrollbar.fixedHeight,
                                screenRect.width,
                                horizontalScrollbar.fixedHeight),
                            scrollPosition.x,
                            Mathf.Min(screenRect.width, viewRect.width),
                            0f,
                            viewRect.width,
                            horizontalScrollbar
                        );
                    }
                    else
                    {
                        GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        scrollPosition.x = ((horizontalScrollbar == GUIStyle.none)
                            ? Mathf.Clamp(scrollPosition.x, 0f, Mathf.Max(viewRect.width - position.width, 0f))
                            : 0f);
                    }
                    if (flag && verticalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.y = VerticalScroll(
                            new Rect(
                                screenRect.xMax + (float)verticalScrollbar.margin.left,
                                screenRect.y,
                                verticalScrollbar.fixedWidth,
                                screenRect.height),
                            scrollPosition.y,
                            Mathf.Min(screenRect.height, viewRect.height),
                            0f,
                            viewRect.height,
                            verticalScrollbar
                        );
                    }
                    else
                    {
                        GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        scrollPosition.y = ((verticalScrollbar == GUIStyle.none)
                            ? Mathf.Clamp(scrollPosition.y, 0f, Mathf.Max(viewRect.height - position.height, 0f))
                            : 0f);
                    }
                }
            }
            else
            {
                GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
            }
            GUIClip.Push(screenRect,
                new Vector2(
                    Mathf.Round(-scrollPosition.x - viewRect.x),
                    Mathf.Round(-scrollPosition.y - viewRect.y)),
                Vector2.zero,
                false
            );

            return scrollPosition;
        }

        public static float HorizontalScroll(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style)
        {
            return Scroller(position, value, size, leftValue, rightValue, style,
                GUI.skin.GetStyle(style.name + "thumb"),
                GUI.skin.GetStyle(style.name + "leftbutton"),
                GUI.skin.GetStyle(style.name + "rightbutton"),
                true);
        }

        public static float VerticalScroll(Rect position, float value, float size, float topValue, float bottomValue, GUIStyle style)
        {
            return Scroller(position, value, size, topValue, bottomValue, style,
                GUI.skin.GetStyle(style.name + "thumb"),
                GUI.skin.GetStyle(style.name + "upbutton"),
                GUI.skin.GetStyle(style.name + "downbutton"),
                false);
        }

        private static float Scroller(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider,
            GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            GUIUtility.CheckOnGUI();
            int controlID = GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive, position);
            Rect position2;
            Rect rect;
            Rect rect2;
            if (horiz)
            {
                position2 = new Rect(position.x + leftButton.fixedWidth,
                    position.y,
                    position.width - leftButton.fixedWidth - rightButton.fixedWidth,
                    position.height);

                rect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                rect2 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                position2 = new Rect(position.x,
                    position.y + leftButton.fixedHeight,
                    position.width,
                    position.height - leftButton.fixedHeight - rightButton.fixedHeight);

                rect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                rect2 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }

            value = Slider(position2, value, size, leftValue, rightValue, slider, thumb, horiz, controlID);

            bool flag = Event.current.type == EventType.MouseUp;
            if (ScrollerRepeatButton(controlID, rect, leftButton))
            {
                value -= 10f * ((leftValue >= rightValue) ? -1f : 1f);
            }
            if (ScrollerRepeatButton(controlID, rect2, rightButton))
            {
                value += 10f * ((leftValue >= rightValue) ? -1f : 1f);
            }
            if (flag && Event.current.type == EventType.Used)
            {
                s_ScrollControlId = 0;
            }
            if (leftValue < rightValue)
            {
                value = Mathf.Clamp(value, leftValue, rightValue - size);
            }
            else
            {
                value = Mathf.Clamp(value, rightValue, leftValue - size);
            }
            return value;
        }

        public static float Slider(Rect position, float value, float size, float start, float end, GUIStyle slider,
            GUIStyle thumb, bool horiz, int id)
        {
            if (id == 0)
            {
                id = GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive, position);
            }
            var sliderHandler = new Internal_SliderHandler(position, value, size, start, end, slider, thumb, horiz, id);
            return sliderHandler.Handle();
        }

        private static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
        {
            bool result = false;
            if (GUI.DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                bool flag = s_ScrollControlId != scrollerID;
                s_ScrollControlId = scrollerID;

                if (flag)
                {
                    result = true;
                    nextScrollStepTime = DateTime.Now.AddMilliseconds(250.0);
                }
                else if (DateTime.Now >= nextScrollStepTime)
                {
                    result = true;
                    nextScrollStepTime = DateTime.Now.AddMilliseconds(30.0);
                }
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.InternalRepaintEditorWindow();
                }
            }
            return result;
        }

#endregion
    }

    #region Extensions

    public static class Extensions
    {
        public static Rect Unstripped_GetLast(this GUILayoutGroup group)
        {
            Rect result;
            if (group.m_Cursor > 0 && group.m_Cursor <= group.entries.Count)
            {
                GUILayoutEntry guilayoutEntry = group.entries[group.m_Cursor - 1];
                result = guilayoutEntry.rect;
            }
            else
            {
                result = GUILayoutEntry.kDummyRect;
            }
            return result;
        }
    }

    #endregion
}
#endif
