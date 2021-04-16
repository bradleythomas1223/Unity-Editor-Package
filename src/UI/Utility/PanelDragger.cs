﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using System.IO;
using System.Diagnostics;

namespace UnityExplorer.UI.Utility
{
    public class PanelDragger
    {
        internal static List<PanelDragger> Instances = new List<PanelDragger>();

        public static void UpdateInstances()
        {
            foreach (var instance in Instances)
                instance.Update();
        }

        public RectTransform Panel { get; set; }
        public event Action<RectTransform> OnFinishResize;
        public event Action<RectTransform> OnFinishDrag;

        private readonly RectTransform refCanvasTransform;

        // Dragging
        public RectTransform DragableArea { get; set; }
        public bool WasDragging { get; set; }
        private Vector3 m_lastDragPosition;

        // Resizing
        private const int RESIZE_THICKNESS = 10;

        public GameObject m_resizeCursorObj;

        internal readonly Vector2 minResize = new Vector2(200, 50);

        private bool WasResizing { get; set; }
        private ResizeTypes m_currentResizeType = ResizeTypes.NONE;
        private Vector2 m_lastResizePos;

        private bool WasHoveringResize { get; set; }
        private ResizeTypes m_lastResizeHoverType;

        private Rect m_totalResizeRect;

        public PanelDragger(RectTransform dragArea, RectTransform panelToDrag)
        {
            Instances.Add(this);
            DragableArea = dragArea;
            Panel = panelToDrag;
            refCanvasTransform = Panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

            UpdateResizeCache();
        }

        public void Destroy()
        {
            if (m_resizeCursorObj)
                GameObject.Destroy(m_resizeCursorObj);

            if (Instances.Contains(this))
                Instances.Remove(this);
        }

        public void Update()
        {
            if (!m_resizeCursorObj)
                this.CreateCursorUI();

            Vector3 rawMousePos = InputManager.MousePosition;

            ResizeTypes type;
            Vector3 resizePos = Panel.InverseTransformPoint(rawMousePos);

            Vector3 dragPos = DragableArea.InverseTransformPoint(rawMousePos);
            bool inDragPos = DragableArea.rect.Contains(dragPos);

            if (WasHoveringResize && m_resizeCursorObj)
                UpdateHoverImagePos();

            // If Mouse pressed this frame
            if (InputManager.GetMouseButtonDown(0))
            {
                if (inDragPos)
                {
                    OnBeginDrag();
                    return;
                }
                else if (MouseInResizeArea(resizePos))
                {
                    type = GetResizeType(resizePos);
                    if (type != ResizeTypes.NONE)
                    {
                        OnBeginResize(type);
                    }
                }
            }
            // If mouse still pressed from last frame
            else if (InputManager.GetMouseButton(0))
            {
                if (WasDragging)
                {
                    OnDrag();
                }
                else if (WasResizing)
                {
                    OnResize();
                }
            }
            // If mouse not pressed
            else
            {
                if (WasDragging)
                {
                    OnEndDrag();
                }
                else if (WasResizing)
                {
                    OnEndResize();
                }
                else if (!inDragPos && MouseInResizeArea(resizePos) && (type = GetResizeType(resizePos)) != ResizeTypes.NONE)
                {
                    OnHoverResize(type);
                }
                else if (WasHoveringResize)
                {
                    OnHoverResizeEnd();
                }
            }

            return;
        }

        #region DRAGGING

        public void OnBeginDrag()
        {
            WasDragging = true;
            m_lastDragPosition = InputManager.MousePosition;
        }

        public void OnDrag()
        {
            Vector3 diff = InputManager.MousePosition - m_lastDragPosition;
            m_lastDragPosition = InputManager.MousePosition;

            // update position while preserving the z value
            Vector3 pos = Panel.localPosition;
            float z = pos.z;
            pos += diff;
            pos.z = z;
            Panel.localPosition = pos;
        }

        public void OnEndDrag()
        {
            WasDragging = false;

            OnFinishDrag?.Invoke(Panel);
        }

        #endregion

        #region RESIZE

        private readonly Dictionary<ResizeTypes, Rect> m_resizeMask = new Dictionary<ResizeTypes, Rect>
        {
            { ResizeTypes.Top,      default },
            { ResizeTypes.Left,     default },
            { ResizeTypes.Right,    default },
            { ResizeTypes.Bottom,   default },
        };

        [Flags]
        public enum ResizeTypes
        {
            NONE = 0,
            Top = 1,
            Left = 2,
            Right = 4,
            Bottom = 8,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
        }

        private const int HALF_THICKESS = RESIZE_THICKNESS / 2;
        private const int DBL_THICKESS = RESIZE_THICKNESS * 2;

        private void UpdateResizeCache()
        {
            m_totalResizeRect = new Rect(Panel.rect.x - RESIZE_THICKNESS + 1,
                Panel.rect.y - RESIZE_THICKNESS + 1,
                Panel.rect.width + DBL_THICKESS - 2,
                Panel.rect.height + DBL_THICKESS - 2);

            // calculate the four cross sections to use as flags

            m_resizeMask[ResizeTypes.Bottom] = new Rect(
                m_totalResizeRect.x, 
                m_totalResizeRect.y, 
                m_totalResizeRect.width,
                RESIZE_THICKNESS);

            m_resizeMask[ResizeTypes.Left] = new Rect(
                m_totalResizeRect.x, 
                m_totalResizeRect.y, 
                RESIZE_THICKNESS, 
                m_totalResizeRect.height);

            m_resizeMask[ResizeTypes.Top] = new Rect(
                m_totalResizeRect.x, 
                Panel.rect.y + Panel.rect.height - 2, 
                m_totalResizeRect.width, 
                RESIZE_THICKNESS);

            m_resizeMask[ResizeTypes.Right] = new Rect(
                m_totalResizeRect.x + Panel.rect.width + RESIZE_THICKNESS - 2, 
                m_totalResizeRect.y, 
                RESIZE_THICKNESS, 
                m_totalResizeRect.height);
        }

        private bool MouseInResizeArea(Vector2 mousePos)
        {
            return m_totalResizeRect.Contains(mousePos);
        }

        private ResizeTypes GetResizeType(Vector2 mousePos)
        {
            // Calculate which part of the resize area we're in, if any.
            // More readable method commented out below.

            int mask = 0;
            mask |= (int)ResizeTypes.Top * (m_resizeMask[ResizeTypes.Top].Contains(mousePos) ? 1 : 0);
            mask |= (int)ResizeTypes.Bottom * (m_resizeMask[ResizeTypes.Bottom].Contains(mousePos) ? 1 : 0);
            mask |= (int)ResizeTypes.Left * (m_resizeMask[ResizeTypes.Left].Contains(mousePos) ? 1 : 0);
            mask |= (int)ResizeTypes.Right * (m_resizeMask[ResizeTypes.Right].Contains(mousePos) ? 1 : 0);

            //if (m_resizeMask[ResizeTypes.Top].Contains(mousePos))
            //    mask |= ResizeTypes.Top;
            //else if (m_resizeMask[ResizeTypes.Bottom].Contains(mousePos))
            //    mask |= ResizeTypes.Bottom;

            //if (m_resizeMask[ResizeTypes.Left].Contains(mousePos))
            //    mask |= ResizeTypes.Left;
            //else if (m_resizeMask[ResizeTypes.Right].Contains(mousePos))
            //    mask |= ResizeTypes.Right;

            return (ResizeTypes)mask;
        }

        public void OnHoverResize(ResizeTypes resizeType)
        {
            if (WasHoveringResize && m_lastResizeHoverType == resizeType)
                return;

            // we are entering resize, or the resize type has changed.

            WasHoveringResize = true;
            m_lastResizeHoverType = resizeType;

            m_resizeCursorObj.SetActive(true);

            // set the rotation for the resize icon
            float iconRotation = 0f;
            switch (resizeType)
            {
                case ResizeTypes.TopRight:
                case ResizeTypes.BottomLeft:
                    iconRotation = 45f; break;
                case ResizeTypes.Top:
                case ResizeTypes.Bottom:
                    iconRotation = 90f; break;
                case ResizeTypes.TopLeft:
                case ResizeTypes.BottomRight:
                    iconRotation = 135f; break;
            }

            Quaternion rot = m_resizeCursorObj.transform.rotation;
            rot.eulerAngles = new Vector3(0, 0, iconRotation);
            m_resizeCursorObj.transform.rotation = rot;

            UpdateHoverImagePos();
        }

        // update the resize icon position to be above the mouse
        private void UpdateHoverImagePos()
        {
            m_resizeCursorObj.transform.localPosition = refCanvasTransform.InverseTransformPoint(InputManager.MousePosition);
        }

        public void OnHoverResizeEnd()
        {
            WasHoveringResize = false;
            m_resizeCursorObj.SetActive(false);
        }

        public void OnBeginResize(ResizeTypes resizeType)
        {
            m_currentResizeType = resizeType;
            m_lastResizePos = InputManager.MousePosition;
            WasResizing = true;
        }

        public void OnResize()
        {
            Vector3 mousePos = InputManager.MousePosition;
            Vector2 diff = m_lastResizePos - (Vector2)mousePos;

            if ((Vector2)mousePos == m_lastResizePos)
                return;

            m_lastResizePos = mousePos;

            float diffX = (float)((decimal)diff.x / Screen.width);
            float diffY = (float)((decimal)diff.y / Screen.height);

            Vector2 anchorMin = Panel.anchorMin;
            Vector2 anchorMax = Panel.anchorMax;

            if (m_currentResizeType.HasFlag(ResizeTypes.Left))
                anchorMin.x -= diffX;
            else if (m_currentResizeType.HasFlag(ResizeTypes.Right))
                anchorMax.x -= diffX;

            if (m_currentResizeType.HasFlag(ResizeTypes.Top))
                anchorMax.y -= diffY;
            else if (m_currentResizeType.HasFlag(ResizeTypes.Bottom))
                anchorMin.y -= diffY;

            var prevMin = Panel.anchorMin;
            var prevMax = Panel.anchorMax;

            Panel.anchorMin = new Vector2(anchorMin.x, anchorMin.y);
            Panel.anchorMax = new Vector2(anchorMax.x, anchorMax.y);

            if (Panel.rect.width < minResize.x)
            {
                Panel.anchorMin = new Vector2(prevMin.x, Panel.anchorMin.y);
                Panel.anchorMax = new Vector2(prevMax.x, Panel.anchorMax.y);
            }
            if (Panel.rect.height < minResize.y)
            {
                Panel.anchorMin = new Vector2(Panel.anchorMin.x, prevMin.y);
                Panel.anchorMax = new Vector2(Panel.anchorMax.x, prevMax.y);
            }
        }

        public void OnEndResize()
        {
            WasResizing = false;
            UpdateResizeCache();
            OnFinishResize?.Invoke(Panel);
        }

        internal void CreateCursorUI()
        {
            try
            {
                var text = UIFactory.CreateLabel(refCanvasTransform.gameObject, "ResizeCursor", "↔", TextAnchor.MiddleCenter, Color.white, true, 35);
                m_resizeCursorObj = text.gameObject;

                RectTransform rect = m_resizeCursorObj.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);

                m_resizeCursorObj.SetActive(false);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning("Exception creating Resize Cursor UI!\r\n" + e.ToString());
            }
        }

        #endregion
    }

    // Just to allow Enum to do .HasFlag() in NET 3.5
    public static class Net35FlagsEx
    {
        public static bool HasFlag(this Enum flags, Enum value)
        {
            ulong num = Convert.ToUInt64(value);
            return (Convert.ToUInt64(flags) & num) == num;
        }
    }
}