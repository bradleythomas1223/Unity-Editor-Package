﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplorerBeta.UI
{
	public static class UIFactory
	{
		private static Vector2 s_ThickElementSize = new Vector2(160f, 30f);
		private static Vector2 s_ThinElementSize = new Vector2(160f, 20f);
		//private static Vector2 s_ImageElementSize = new Vector2(100f, 100f);
		private static Color s_DefaultSelectableColor = new Color(1f, 1f, 1f, 1f);
		private static Color s_PanelColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
		private static Color s_TextColor = new Color(0.95f, 0.95f, 0.95f, 1f);

		public static Resources UIResources { get; set; }

		public struct Resources
		{
			public Sprite standard;
			public Sprite background;
			public Sprite inputField;
			public Sprite knob;
			public Sprite checkmark;
			public Sprite dropdown;
			public Sprite mask;
		}

		private static GameObject CreateUIObject(string name, GameObject parent, Vector2 size = default)
		{
			GameObject obj = new GameObject(name);

			var rect = obj.AddComponent<RectTransform>();
			if (size != default)
			{
				rect.sizeDelta = size;
			}

			SetParentAndAlign(obj, parent);

			return obj;
		}

		private static void SetDefaultTextValues(Text lbl)
		{
			lbl.color = s_TextColor;
			lbl.AssignDefaultFont();
			//lbl.alignment = alignment;
			//lbl.resizeTextForBestFit = true;
		}

		private static void SetDefaultColorTransitionValues(Selectable slider)
		{
			ColorBlock colors = slider.colors;
			colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
			colors.highlightedColor = new Color(0.45f, 0.45f, 0.45f);
			colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
			colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
			slider.colors = colors;
		}

		private static void SetParentAndAlign(GameObject child, GameObject parent)
		{
			if (parent == null)
			{
				return;
			}
			child.transform.SetParent(parent.transform, false);
			SetLayerRecursively(child);
		}

		public static void SetLayerRecursively(GameObject go)
		{
			go.layer = 5;
			Transform transform = go.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				SetLayerRecursively(transform.GetChild(i).gameObject);
			}
		}

		public static GameObject CreatePanel(GameObject parent, string name, out GameObject content)
		{
			GameObject panelObj = CreateUIObject($"Panel_{name}", parent, s_ThickElementSize);

			RectTransform rect = panelObj.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = Vector2.zero;

			Image image = panelObj.AddComponent<Image>();
			image.type = Image.Type.Filled;
			image.color = new Color(0.05f, 0.05f, 0.05f);

			var group = panelObj.AddComponent<VerticalLayoutGroup>();
			group.padding.left = 3;
			group.padding.right = 3;
			group.padding.bottom = 3;
			group.padding.top = 3;
			group.childControlHeight = true;
			group.childControlWidth = true;
			group.childForceExpandHeight = true;
			group.childForceExpandWidth = true;

			content = new GameObject("Content");
			content.transform.parent = panelObj.transform;

			Image image2 = content.AddComponent<Image>();
			image2.type = Image.Type.Filled;
			image2.color = new Color(0.1f, 0.1f, 0.1f);

			var group2 = content.AddComponent<VerticalLayoutGroup>();
			group2.padding.left = 5;
			group2.padding.right = 5;
			group2.padding.bottom = 5;
			group2.padding.top = 5;
			group2.spacing = 5;
			group2.childControlHeight = true;
			group2.childControlWidth = true;
			group2.childForceExpandHeight = false;
			group2.childForceExpandWidth = true;

			return panelObj;
		}

		public static GameObject CreateVerticalGroup(GameObject parent, Color color = default)
		{
			var groupObj = CreateUIObject("VerticalLayout", parent);

			var horiGroup = groupObj.AddComponent<VerticalLayoutGroup>();
			horiGroup.childAlignment = TextAnchor.UpperLeft;
			horiGroup.childControlWidth = false;

			var image = groupObj.AddComponent<Image>();
			if (color != default)
				image.color = color;
			else
				image.color = new Color(44f / 255f, 44f / 255f, 44f / 255f);

			return groupObj;
		}

		public static GameObject CreateHorizontalGroup(GameObject parent, Color color = default)
		{
			var groupObj = CreateUIObject("HorizontalLayout", parent);

			var horiGroup = groupObj.AddComponent<HorizontalLayoutGroup>();
			horiGroup.childAlignment = TextAnchor.UpperLeft;
			horiGroup.childControlWidth = false;

			var image = groupObj.AddComponent<Image>();
			if (color != default)
				image.color = color;
			else
				image.color = new Color(44f / 255f, 44f / 255f, 44f / 255f);

			return groupObj;
		}

		public static GameObject CreateLabel(GameObject parent, TextAnchor alignment)
		{
			GameObject labelObj = CreateUIObject("Label", parent, s_ThinElementSize);

			var text = labelObj.AddComponent<Text>();
			SetDefaultTextValues(text);
			text.alignment = alignment;
			text.supportRichText = true;

			return labelObj;
		}

		public static GameObject CreateButton(GameObject parent)
		{
			GameObject buttonObj = CreateUIObject("Button", parent, s_ThinElementSize);

			GameObject textObj = new GameObject("Text");
			textObj.AddComponent<RectTransform>();
			SetParentAndAlign(textObj, buttonObj);

			Image image = buttonObj.AddComponent<Image>();
			image.sprite = UIResources.standard;
			image.type = Image.Type.Sliced;
			image.color = s_DefaultSelectableColor;

			SetDefaultColorTransitionValues(buttonObj.AddComponent<Button>());

			Text text = textObj.AddComponent<Text>();
			text.text = "Button";
			SetDefaultTextValues(text);
			text.alignment = TextAnchor.MiddleCenter;

			RectTransform rect = textObj.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.sizeDelta = Vector2.zero;

			return buttonObj;
		}

		public static GameObject CreateSlider(GameObject parent)
		{
			GameObject sliderObj = CreateUIObject("Slider", parent, s_ThinElementSize);

			GameObject bgObj = CreateUIObject("Background", sliderObj);
			GameObject fillAreaObj = CreateUIObject("Fill Area", sliderObj);
			GameObject fillObj = CreateUIObject("Fill", fillAreaObj);
			GameObject handleSlideAreaObj = CreateUIObject("Handle Slide Area", sliderObj);
			GameObject handleObj = CreateUIObject("Handle", handleSlideAreaObj);

			Image bgImage = bgObj.AddComponent<Image>();
			bgImage.sprite = UIResources.background;
			bgImage.type = Image.Type.Sliced;
			bgImage.color = s_DefaultSelectableColor;

			RectTransform bgRect = bgObj.GetComponent<RectTransform>();
			bgRect.anchorMin = new Vector2(0f, 0.25f);
			bgRect.anchorMax = new Vector2(1f, 0.75f);
			bgRect.sizeDelta = new Vector2(0f, 0f);

			RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
			fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
			fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
			fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
			fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

			Image fillImage = fillObj.AddComponent<Image>();
			fillImage.sprite = UIResources.standard;
			fillImage.type = Image.Type.Sliced;
			fillImage.color = s_DefaultSelectableColor;

			fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 0f);

			RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
			handleSlideRect.sizeDelta = new Vector2(-20f, 0f);
			handleSlideRect.anchorMin = new Vector2(0f, 0f);
			handleSlideRect.anchorMax = new Vector2(1f, 1f);

			Image handleImage = handleObj.AddComponent<Image>();
			handleImage.sprite = UIResources.knob;
			handleImage.color = s_DefaultSelectableColor;

			handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 0f);

			Slider slider = sliderObj.AddComponent<Slider>();
			slider.fillRect = fillObj.GetComponent<RectTransform>();
			slider.handleRect = handleObj.GetComponent<RectTransform>();
			slider.targetGraphic = handleImage;
			slider.direction = Slider.Direction.LeftToRight;
			SetDefaultColorTransitionValues(slider);

			return sliderObj;
		}

		public static GameObject CreateScrollbar(GameObject parent)
		{
			GameObject scrollObj = CreateUIObject("Scrollbar", parent, s_ThinElementSize);

			GameObject slideAreaObj = CreateUIObject("Sliding Area", scrollObj);
			GameObject handleObj = CreateUIObject("Handle", slideAreaObj);

			Image scrollImage = scrollObj.AddComponent<Image>();
			scrollImage.type = Image.Type.Sliced;
			scrollImage.color = new Color(0.1f, 0.1f, 0.1f);

			Image handleImage = handleObj.AddComponent<Image>();
			handleImage.sprite = UIResources.standard;
			handleImage.type = Image.Type.Sliced;
			handleImage.color = new Color(0.4f, 0.4f, 0.4f);

			RectTransform slideAreaRect = slideAreaObj.GetComponent<RectTransform>();
			slideAreaRect.sizeDelta = new Vector2(-20f, -20f);
			slideAreaRect.anchorMin = Vector2.zero;
			slideAreaRect.anchorMax = Vector2.one;

			RectTransform handleRect = handleObj.GetComponent<RectTransform>();
			handleRect.sizeDelta = new Vector2(20f, 20f);

			Scrollbar scrollbar = scrollObj.AddComponent<Scrollbar>();
			scrollbar.handleRect = handleRect;
			scrollbar.targetGraphic = handleImage;
			SetDefaultColorTransitionValues(scrollbar);

			return scrollObj;
		}

		public static GameObject CreateToggle(GameObject parent)
		{
			GameObject toggleObj = CreateUIObject("Toggle", parent, s_ThinElementSize);

			GameObject bgObj = CreateUIObject("Background", toggleObj);
			GameObject checkObj = CreateUIObject("Checkmark", bgObj);
			GameObject labelObj = CreateUIObject("Label", toggleObj);

			Toggle toggle = toggleObj.AddComponent<Toggle>();
			toggle.isOn = true;

			Image bgImage = bgObj.AddComponent<Image>();
			bgImage.sprite = UIResources.standard;
			bgImage.type = Image.Type.Sliced;
			bgImage.color = s_DefaultSelectableColor;

			Image checkImage = checkObj.AddComponent<Image>();
			checkImage.sprite = UIResources.checkmark;

			Text text = labelObj.AddComponent<Text>();
			text.text = "Toggle";
			SetDefaultTextValues(text);

			toggle.graphic = checkImage;
			toggle.targetGraphic = bgImage;
			SetDefaultColorTransitionValues(toggle);

			RectTransform bgRect = bgObj.GetComponent<RectTransform>();
			bgRect.anchorMin = new Vector2(0f, 1f);
			bgRect.anchorMax = new Vector2(0f, 1f);
			bgRect.anchoredPosition = new Vector2(10f, -10f);
			bgRect.sizeDelta = new Vector2(20f, 20f);

			RectTransform checkRect = checkObj.GetComponent<RectTransform>();
			checkRect.anchorMin = new Vector2(0.5f, 0.5f);
			checkRect.anchorMax = new Vector2(0.5f, 0.5f);
			checkRect.anchoredPosition = Vector2.zero;
			checkRect.sizeDelta = new Vector2(20f, 20f);

			RectTransform labelRect = labelObj.GetComponent<RectTransform>();
			labelRect.anchorMin = new Vector2(0f, 0f);
			labelRect.anchorMax = new Vector2(1f, 1f);
			labelRect.offsetMin = new Vector2(23f, 1f);
			labelRect.offsetMax = new Vector2(-5f, -2f);
			return toggleObj;
		}

		public static GameObject CreateTMPInput(GameObject parent)
        {
			GameObject mainObj = CreateUIObject("InputField (TMP)", parent);

			Image mainImage = mainObj.AddComponent<Image>();
			mainImage.type = Image.Type.Sliced;
			mainImage.color = new Color(38f / 255f, 38f / 255f, 38f / 255f, 1.0f);

			var mainInput = mainObj.AddComponent<TMP_InputField>();
			mainInput.navigation.mode = Navigation.Mode.None;
			mainInput.richText = true;
			mainInput.isRichTextEditingAllowed = true;
			mainInput.lineType = TMP_InputField.LineType.MultiLineNewline;
			mainInput.interactable = true;
			mainInput.transition = Selectable.Transition.ColorTint;
			mainInput.onFocusSelectAll = false;

			var mainColors = mainInput.colors;
			mainColors.normalColor = new Color(1, 1, 1, 1);
			mainColors.highlightedColor = new Color(245f / 255f, 245f / 255f, 245f / 255f, 1.0f);
			mainColors.pressedColor = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
			mainColors.highlightedColor = new Color(245f / 255f, 245f / 255f, 245f / 255f, 1.0f);
			mainInput.colors = mainColors;

            var mainGroup = mainObj.AddComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childForceExpandHeight = true;

            var textArea = CreateUIObject("Text Area", mainObj);
            textArea.AddComponent<RectMask2D>();

            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0, 0);
            textAreaRect.anchorMax = new Vector2(1, 1);
			textAreaRect.offsetMin = new Vector2(10, 7);
			textAreaRect.offsetMax = new Vector2(10, 6);

            mainInput.textViewport = textArea.GetComponent<RectTransform>();

			var placeHolderObj = CreateUIObject("Placeholder", textArea);
			var placeholderText = placeHolderObj.AddComponent<TextMeshProUGUI>();
			placeholderText.fontSize = 16;
			placeholderText.text = "Nothing logged yet...";
			placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            var placeHolderRect = placeHolderObj.GetComponent<RectTransform>();
            placeHolderRect.anchorMin = Vector2.zero;
			placeHolderRect.anchorMax = Vector2.one;
			placeHolderRect.offsetMin = Vector2.zero;
            placeHolderRect.offsetMax = Vector2.zero;

			var placeholderLayout = placeHolderObj.AddComponent<LayoutElement>();
			placeholderLayout.preferredWidth = 990;
			placeholderLayout.flexibleWidth = 500;

			mainInput.placeholder = placeholderText;

			var inputTextObj = CreateUIObject("Text", textArea);
			var inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
			inputText.fontSize = 16;
			inputText.text = "";
			inputText.color = new Color(1f, 1f, 1f, 1f);

            var inputTextRect = inputTextObj.GetComponent<RectTransform>();
			inputTextRect.anchorMin = Vector2.zero;
			inputTextRect.anchorMax = Vector2.one;
			inputTextRect.offsetMin = Vector2.zero;
			inputTextRect.offsetMax = Vector2.zero;

			var test = inputTextObj.AddComponent<LayoutElement>();
			test.preferredWidth = 990;
			test.flexibleWidth = 500;

			mainInput.textComponent = inputText;

			return mainObj;
		}

		public static GameObject CreateInputField(GameObject parent)
		{
			GameObject inputObj = CreateUIObject("InputField", parent, s_ThickElementSize);

			GameObject placeholderObj = CreateUIObject("Placeholder", inputObj);
			GameObject textObj = CreateUIObject("Text", inputObj);

			Image inputImage = inputObj.AddComponent<Image>();
			inputImage.sprite = UIResources.inputField;
			inputImage.type = Image.Type.Sliced;
			inputImage.color = s_DefaultSelectableColor;

			InputField inputField = inputObj.AddComponent<InputField>();
			SetDefaultColorTransitionValues(inputField);

			Text text = textObj.AddComponent<Text>();
			text.text = "";
			text.supportRichText = false;
			SetDefaultTextValues(text);

			Text placeholderText = placeholderObj.AddComponent<Text>();
			placeholderText.text = "Enter text...";
			placeholderText.fontStyle = FontStyle.Italic;
			Color color = text.color;
			color.a *= 0.5f;
			placeholderText.color = color;

			RectTransform textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.sizeDelta = Vector2.zero;
			textRect.offsetMin = new Vector2(10f, 6f);
			textRect.offsetMax = new Vector2(-10f, -7f);

			RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
			placeholderRect.anchorMin = Vector2.zero;
			placeholderRect.anchorMax = Vector2.one;
			placeholderRect.sizeDelta = Vector2.zero;
			placeholderRect.offsetMin = new Vector2(10f, 6f);
			placeholderRect.offsetMax = new Vector2(-10f, -7f);
			inputField.textComponent = text;
			inputField.placeholder = placeholderText;

			return inputObj;
		}

		public static GameObject CreateDropdown(GameObject parent)
		{
			GameObject dropdownObj = CreateUIObject("Dropdown", parent, s_ThickElementSize);

			GameObject labelObj = CreateUIObject("Label", dropdownObj);
			GameObject arrowObj = CreateUIObject("Arrow", dropdownObj);
			GameObject templateObj = CreateUIObject("Template", dropdownObj);
			GameObject viewportObj = CreateUIObject("Viewport", templateObj);
			GameObject contentObj = CreateUIObject("Content", viewportObj);
			GameObject itemObj = CreateUIObject("Item", contentObj);
			GameObject itemBgObj = CreateUIObject("Item Background", itemObj);
			GameObject itemCheckObj = CreateUIObject("Item Checkmark", itemObj);
			GameObject itemLabelObj = CreateUIObject("Item Label", itemObj);

			GameObject scrollbarObj = CreateScrollbar(templateObj);
			scrollbarObj.name = "Scrollbar";
			Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
			scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

			RectTransform scrollRectTransform = scrollbarObj.GetComponent<RectTransform>();
			scrollRectTransform.anchorMin = Vector2.right;
			scrollRectTransform.anchorMax = Vector2.one;
			scrollRectTransform.pivot = Vector2.one;
			scrollRectTransform.sizeDelta = new Vector2(scrollRectTransform.sizeDelta.x, 0f);

			Text itemLabelText = itemLabelObj.AddComponent<Text>();
			SetDefaultTextValues(itemLabelText);
			itemLabelText.alignment = TextAnchor.MiddleLeft;

			Image itemBgImage = itemBgObj.AddComponent<Image>();
			itemBgImage.color = new Color32(245, 245, 245, byte.MaxValue);

			Image itemCheckImage = itemCheckObj.AddComponent<Image>();
			itemCheckImage.sprite = UIResources.checkmark;

			Toggle itemToggle = itemObj.AddComponent<Toggle>();
			itemToggle.targetGraphic = itemBgImage;
			itemToggle.graphic = itemCheckImage;
			itemToggle.isOn = true;

			Image templateImage = templateObj.AddComponent<Image>();
			templateImage.sprite = UIResources.standard;
			templateImage.type = Image.Type.Sliced;

			ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.content = contentObj.GetComponent<RectTransform>();
			scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
			scrollRect.horizontal = false;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.verticalScrollbar = scrollbar;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
			scrollRect.verticalScrollbarSpacing = -3f;

			viewportObj.AddComponent<Mask>().showMaskGraphic = false;

			Image viewportImage = viewportObj.AddComponent<Image>();
			viewportImage.sprite = UIResources.mask;
			viewportImage.type = Image.Type.Sliced;

			Text labelText = labelObj.AddComponent<Text>();
			SetDefaultTextValues(labelText);
			labelText.alignment = TextAnchor.MiddleLeft;

			arrowObj.AddComponent<Image>().sprite = UIResources.dropdown;
			Image dropdownImage = dropdownObj.AddComponent<Image>();
			dropdownImage.sprite = UIResources.standard;
			dropdownImage.color = s_DefaultSelectableColor;
			dropdownImage.type = Image.Type.Sliced;

			Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
			dropdown.targetGraphic = dropdownImage;
			SetDefaultColorTransitionValues(dropdown);
			dropdown.template = templateObj.GetComponent<RectTransform>();
			dropdown.captionText = labelText;
			dropdown.itemText = itemLabelText;
			itemLabelText.text = "Option A";
			dropdown.options.Add(new Dropdown.OptionData
			{
				text = "Option A"
			});
            dropdown.options.Add(new Dropdown.OptionData
            {
                text = "Option B"
            });
            dropdown.options.Add(new Dropdown.OptionData
            {
                text = "Option C"
            });

            dropdown.RefreshShownValue();

			RectTransform labelRect = labelObj.GetComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = new Vector2(10f, 6f);
			labelRect.offsetMax = new Vector2(-25f, -7f);

			RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
			arrowRect.anchorMin = new Vector2(1f, 0.5f);
			arrowRect.anchorMax = new Vector2(1f, 0.5f);
			arrowRect.sizeDelta = new Vector2(20f, 20f);
			arrowRect.anchoredPosition = new Vector2(-15f, 0f);

			RectTransform templateRect = templateObj.GetComponent<RectTransform>();
			templateRect.anchorMin = new Vector2(0f, 0f);
			templateRect.anchorMax = new Vector2(1f, 0f);
			templateRect.pivot = new Vector2(0.5f, 1f);
			templateRect.anchoredPosition = new Vector2(0f, 2f);
			templateRect.sizeDelta = new Vector2(0f, 150f);

			RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
			viewportRect.anchorMin = new Vector2(0f, 0f);
			viewportRect.anchorMax = new Vector2(1f, 1f);
			viewportRect.sizeDelta = new Vector2(-18f, 0f);
			viewportRect.pivot = new Vector2(0f, 1f);

			RectTransform contentRect = contentObj.GetComponent<RectTransform>();
			contentRect.anchorMin = new Vector2(0f, 1f);
			contentRect.anchorMax = new Vector2(1f, 1f);
			contentRect.pivot = new Vector2(0.5f, 1f);
			contentRect.anchoredPosition = new Vector2(0f, 0f);
			contentRect.sizeDelta = new Vector2(0f, 28f);

			RectTransform itemRect = itemObj.GetComponent<RectTransform>();
			itemRect.anchorMin = new Vector2(0f, 0.5f);
			itemRect.anchorMax = new Vector2(1f, 0.5f);
			itemRect.sizeDelta = new Vector2(0f, 20f);

			RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
			itemBgRect.anchorMin = Vector2.zero;
			itemBgRect.anchorMax = Vector2.one;
			itemBgRect.sizeDelta = Vector2.zero;

			RectTransform itemCheckRect = itemCheckObj.GetComponent<RectTransform>();
			itemCheckRect.anchorMin = new Vector2(0f, 0.5f);
			itemCheckRect.anchorMax = new Vector2(0f, 0.5f);
			itemCheckRect.sizeDelta = new Vector2(20f, 20f);
			itemCheckRect.anchoredPosition = new Vector2(10f, 0f);

			RectTransform itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
			itemLabelRect.anchorMin = Vector2.zero;
			itemLabelRect.anchorMax = Vector2.one;
			itemLabelRect.offsetMin = new Vector2(20f, 1f);
			itemLabelRect.offsetMax = new Vector2(-10f, -2f);
			templateObj.SetActive(false);

			return dropdownObj;
		}

		public static GameObject CreateScrollView(GameObject parent, out GameObject content, Color color = default)
		{
			GameObject scrollObj = CreateUIObject("Scroll View", parent);

			var mainLayout = scrollObj.AddComponent<LayoutElement>();
			mainLayout.flexibleWidth = 999;
			mainLayout.flexibleHeight = 999;
			mainLayout.preferredHeight = 200;
			mainLayout.preferredWidth = 200;

			GameObject viewportObj = CreateUIObject("Viewport", scrollObj);

			var viewportGroup = viewportObj.AddComponent<VerticalLayoutGroup>();
			viewportGroup.childControlHeight = true;
			viewportGroup.childControlWidth = true;
			viewportGroup.childForceExpandHeight = true;
			viewportGroup.childForceExpandWidth = true;

			content = CreateUIObject("Content", viewportObj);

			var contentGroup = content.AddComponent<VerticalLayoutGroup>();
			contentGroup.padding.left = 5;
			contentGroup.padding.right = 5;
			contentGroup.padding.top = 5;
			contentGroup.padding.bottom = 5;
			contentGroup.childControlHeight = false;
			contentGroup.childControlWidth = true;
			contentGroup.childForceExpandHeight = false;
			contentGroup.childForceExpandWidth = true;

			GameObject horiScroll = CreateScrollbar(scrollObj);
			horiScroll.name = "Scrollbar Horizontal";
			SetParentAndAlign(horiScroll, scrollObj);

            RectTransform horiRect = horiScroll.GetComponent<RectTransform>();
            horiRect.anchorMin = Vector2.zero;
            horiRect.anchorMax = Vector2.right;
            horiRect.pivot = Vector2.zero;
            horiRect.sizeDelta = new Vector2(0f, horiRect.sizeDelta.y);

            GameObject vertScroll = CreateScrollbar(scrollObj);
			vertScroll.name = "Scrollbar Vertical";
			SetParentAndAlign(vertScroll, scrollObj);
			vertScroll.GetComponent<Scrollbar>().SetDirection(Scrollbar.Direction.BottomToTop, true);

            RectTransform vertRect = vertScroll.GetComponent<RectTransform>();
            vertRect.anchorMin = Vector2.right;
            vertRect.anchorMax = Vector2.one;
            vertRect.pivot = Vector2.one;
            vertRect.sizeDelta = new Vector2(vertRect.sizeDelta.x, 0f);

			RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
			viewportRect.anchorMin = Vector2.zero;
			viewportRect.anchorMax = Vector2.one;
			viewportRect.sizeDelta = Vector2.zero;
			viewportRect.pivot = Vector2.up;

			RectTransform contentRect = content.GetComponent<RectTransform>();
			contentRect.anchorMin = Vector2.up;
			contentRect.anchorMax = Vector2.one;
			contentRect.pivot = Vector2.up;

			ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
			scrollRect.content = contentRect;
			scrollRect.viewport = viewportRect;
			scrollRect.horizontalScrollbar = horiScroll.GetComponent<Scrollbar>();
            scrollRect.verticalScrollbar = vertScroll.GetComponent<Scrollbar>();
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.horizontalScrollbarSpacing = -3f;
			scrollRect.verticalScrollbarSpacing = -3f;
			scrollRect.scrollSensitivity = 25;

			Image scrollImage = scrollObj.AddComponent<Image>();
			scrollImage.type = Image.Type.Filled;

			scrollImage.color = (color == default) ? new Color(0.3f, 0.3f, 0.3f, 1f) : color;

			Image viewportImage = viewportObj.AddComponent<Image>();
			viewportImage.sprite = UIResources.mask;
			viewportImage.type = Image.Type.Sliced;
			viewportImage.color = new Color(1, 1, 1, 1);

			var mask = viewportObj.AddComponent<Mask>();
			mask.showMaskGraphic = false;

			return scrollObj;
		}
	}
}
