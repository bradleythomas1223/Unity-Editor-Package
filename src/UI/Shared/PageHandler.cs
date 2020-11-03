﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityExplorer.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.Shared
{
    public enum Turn
    {
        Left,
        Right
    }

    public class PageHandler : IEnumerator
    {
        public PageHandler()
        {
            ItemsPerPage = ModConfig.Instance?.Default_Page_Limit ?? 20;
        }

        // For now this is just set when the PageHandler is created, based on config.
        // At some point I might make it possible to change this after creation again.
        public int ItemsPerPage { get; }

        // IEnumerator.Current
        public object Current => m_currentIndex;
        private int m_currentIndex = 0;

        private int m_currentPage;
        public event Action OnPageChanged;

        // ui
        private GameObject m_pageUIHolder;
        private Text m_currentPageLabel;

        // set and maintained by owner of list
        private int m_listCount;
        public int ListCount
        {
            get => m_listCount;
            set
            {
                m_listCount = value;

                if (LastPage <= 0 && m_pageUIHolder.activeSelf)
                {
                    m_pageUIHolder.SetActive(false);
                }
                else if (LastPage > 0 && !m_pageUIHolder.activeSelf)
                {
                    m_pageUIHolder.SetActive(true);
                }

                RefreshUI();
            }
        }

        public int LastPage => (int)Math.Ceiling(ListCount / (decimal)ItemsPerPage) - 1;

        // The index of the first element of the current page
        public int StartIndex
        {
            get
            {
                int offset = m_currentPage * ItemsPerPage;

                if (offset >= ListCount)
                {
                    offset = 0;
                    m_currentPage = 0;
                }

                return offset;
            }
        }

        public int EndIndex
        {
            get
            {
                int end = StartIndex + ItemsPerPage;
                if (end >= ListCount)
                    end = ListCount - 1;
                return end;
            }
        }

        // IEnumerator.MoveNext()
        public bool MoveNext()
        {
            m_currentIndex++;
            return m_currentIndex < StartIndex + ItemsPerPage;
        }

        // IEnumerator.Reset()
        public void Reset()
        {
            m_currentIndex = StartIndex - 1;
        }

        public IEnumerator<int> GetEnumerator()
        {
            Reset();
            while (MoveNext())
            {
                yield return m_currentIndex;
            }
        }

        public void TurnPage(Turn direction)
        {
            if (direction == Turn.Left)
            {
                if (m_currentPage > 0)
                {
                    m_currentPage--;
                    OnPageChanged?.Invoke();
                    RefreshUI();
                }
            }
            else
            {
                if (m_currentPage < LastPage)
                {
                    m_currentPage++;
                    OnPageChanged?.Invoke();
                    RefreshUI();
                }
            }
        }

        #region UI CONSTRUCTION

        public void Show() => m_pageUIHolder?.SetActive(true);

        public void Hide() => m_pageUIHolder?.SetActive(false);

        public void RefreshUI()
        {
            m_currentPageLabel.text = $"Page {m_currentPage + 1} / {LastPage + 1}";
        }

        public void ConstructUI(GameObject parent)
        {
            m_pageUIHolder = UIFactory.CreateHorizontalGroup(parent);

            Image image = m_pageUIHolder.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            HorizontalLayoutGroup parentGroup = m_pageUIHolder.GetComponent<HorizontalLayoutGroup>();
            parentGroup.childForceExpandHeight = true;
            parentGroup.childForceExpandWidth = false;
            parentGroup.childControlWidth = true;
            parentGroup.childControlHeight = true;

            LayoutElement parentLayout = m_pageUIHolder.AddComponent<LayoutElement>();
            parentLayout.minHeight = 20;
            parentLayout.flexibleHeight = 0;
            parentLayout.minWidth = 200;
            parentLayout.flexibleWidth = 30;

            GameObject leftBtnObj = UIFactory.CreateButton(m_pageUIHolder);
            Button leftBtn = leftBtnObj.GetComponent<Button>();
#if CPP
            leftBtn.onClick.AddListener(new Action(() => { TurnPage(Turn.Left); }));
#else
            leftBtn.onClick.AddListener(() => { TurnPage(Turn.Left); });
#endif
            Text leftBtnText = leftBtnObj.GetComponentInChildren<Text>();
            leftBtnText.text = "<";
            LayoutElement leftBtnLayout = leftBtnObj.AddComponent<LayoutElement>();
            leftBtnLayout.flexibleHeight = 0;
            leftBtnLayout.flexibleWidth = 0;
            leftBtnLayout.minWidth = 40;
            leftBtnLayout.minHeight = 20;

            GameObject labelObj = UIFactory.CreateLabel(m_pageUIHolder, TextAnchor.MiddleCenter);
            m_currentPageLabel = labelObj.GetComponent<Text>();
            m_currentPageLabel.text = "Page 1 / TODO";
            LayoutElement textLayout = labelObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1.5f;
            textLayout.preferredWidth = 120;

            GameObject rightBtnObj = UIFactory.CreateButton(m_pageUIHolder);
            Button rightBtn = rightBtnObj.GetComponent<Button>();
#if CPP
            rightBtn.onClick.AddListener(new Action(() => { TurnPage(Turn.Right); }));
#else
            rightBtn.onClick.AddListener(() => { TurnPage(Turn.Right); });
#endif
            Text rightBtnText = rightBtnObj.GetComponentInChildren<Text>();
            rightBtnText.text = ">";
            LayoutElement rightBtnLayout = rightBtnObj.AddComponent<LayoutElement>();
            rightBtnLayout.flexibleHeight = 0;
            rightBtnLayout.flexibleWidth = 0;
            rightBtnLayout.minWidth = 40;
            rightBtnLayout.minHeight = 20;

            ListCount = 0;
        }

        #endregion
    }
}
