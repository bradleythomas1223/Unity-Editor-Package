﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Inspectors.IValues;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public enum ValueState
    {
        NotEvaluated,
        Exception,
        NullValue,
        Boolean,
        Number,
        String,
        Enum,
        Collection,
        Dictionary,
        ValueStruct,
        Color,
        Unsupported
    }

    public abstract class CacheObjectBase
    {
        public CacheObjectCell CellView { get; internal set; }

        public InteractiveValue IValue { get; private set; }
        public Type CurrentIValueType { get; private set; }
        public bool SubContentState { get; private set; }

        public object Value { get; protected set; }
        public Type FallbackType { get; protected set; }

        public string NameLabelText { get; protected set; }
        //public string TypeLabelText { get; set; }
        public string ValueLabelText { get; protected set; }

        public abstract bool ShouldAutoEvaluate { get; }
        public abstract bool HasArguments { get; }
        public bool CanWrite { get; protected set; }
        public bool HadException { get; protected set; }
        public Exception LastException { get; protected set; }

        public virtual void Initialize(Type fallbackType)
        {
            this.FallbackType = fallbackType;
            //this.TypeLabelText = SignatureHighlighter.ParseFullType(FallbackType, false);
            this.ValueLabelText = GetValueLabel();
        }

        // internals

        private static readonly Dictionary<string, MethodInfo> numberParseMethods = new Dictionary<string, MethodInfo>();

        public ValueState State = ValueState.NotEvaluated;

        protected const string NOT_YET_EVAL = "<color=grey>Not yet evaluated</color>";

        internal static GameObject InactiveIValueHolder
        {
            get
            {
                if (!inactiveIValueHolder)
                {
                    inactiveIValueHolder = new GameObject("InactiveIValueHolder");
                    GameObject.DontDestroyOnLoad(inactiveIValueHolder);
                    inactiveIValueHolder.transform.parent = UIManager.PoolHolder.transform;
                    inactiveIValueHolder.SetActive(false);
                }
                return inactiveIValueHolder;
            }
        }
        private static GameObject inactiveIValueHolder;

        // On parent destroying this

        public virtual void ReleasePooledObjects()
        {
            if (this.IValue != null)
                ReleaseIValue();

            // TODO release Evaluate

            if (this.CellView != null)
            {
                this.CellView.Occupant = null;
                this.CellView.SubContentHolder.SetActive(false);
                this.CellView = null;
            }
            
        }

        // Updating and applying values

        public virtual void SetValueFromSource(object value)
        {
            this.Value = value;

            if (!Value.IsNullOrDestroyed())
                Value = Value.TryCast();

            var prevState = State;
            ProcessOnEvaluate();

            if (State != prevState)
            {
                // TODO handle if subcontent / evaluate shown, check type change, etc
            }

            if (this.IValue != null)
            {
                this.IValue.SetValue(Value);
            }
        }

        public abstract void SetUserValue(object value);

        /// <summary>
        /// Process the CacheMember state when the value has been evaluated (or re-evaluated)
        /// </summary>
        protected virtual void ProcessOnEvaluate()
        {

            if (HadException)
                State = ValueState.Exception;
            else if (Value.IsNullOrDestroyed())
                State = ValueState.NullValue;
            else
            {
                var type = Value.GetActualType();

                if (type == typeof(bool))
                    State = ValueState.Boolean;
                else if (type.IsPrimitive || type == typeof(decimal))
                    State = ValueState.Number;
                else if (type == typeof(string))
                    State = ValueState.String;
                else if (type.IsEnum)
                    State = ValueState.Enum;
                else if (type.IsEnumerable())
                    State = ValueState.Collection;
                else if (type.IsDictionary())
                    State = ValueState.Dictionary;
                // todo Color and ValueStruct
                else
                    State = ValueState.Unsupported;
            }

            // Set label text
            ValueLabelText = GetValueLabel();
        }

        protected string GetValueLabel()
        {
            switch (State)
            {
                case ValueState.NotEvaluated:
                    return $"<i>{NOT_YET_EVAL} ({SignatureHighlighter.ParseFullType(FallbackType, true)})</i>";
                case ValueState.Exception:
                    return $"<i><color=red>{ReflectionUtility.ReflectionExToString(LastException)}</color></i>";
                case ValueState.Boolean:
                case ValueState.Number:
                    return null;
                case ValueState.String:
                    string s = Value as string;
                    if (s.Length > 200)
                        s = $"{s.Substring(0, 200)}...";
                    return $"\"{s}\"";
                case ValueState.NullValue:
                    return $"<i>{ToStringUtility.ToStringWithType(Value, FallbackType, true)}</i>";
                case ValueState.Enum:
                case ValueState.Collection:
                case ValueState.ValueStruct:
                case ValueState.Unsupported:
                default:
                    return ToStringUtility.ToStringWithType(Value, FallbackType, true);
            }
        }

        /// <summary>Return true if SetCell should abort, false if it should continue.</summary>
        protected abstract bool SetCellEvaluateState(CacheObjectCell cell);

        public virtual void SetCell(CacheObjectCell cell)
        {
            cell.NameLabel.text = NameLabelText;
            cell.ValueLabel.gameObject.SetActive(true);

            cell.SubContentHolder.gameObject.SetActive(SubContentState);
            if (IValue != null)
                IValue.UIRoot.transform.SetParent(cell.SubContentHolder.transform, false);

            if (SetCellEvaluateState(cell))
                return;

            switch (State)
            {
                case ValueState.Exception:
                case ValueState.NullValue:
                    ReleaseIValue();
                    SetValueState(cell, ValueStateArgs.Default);
                    break;
                case ValueState.Boolean:
                    SetValueState(cell, new ValueStateArgs(false, toggleActive:true, applyActive: CanWrite));
                    break;
                case ValueState.Number:
                    SetValueState(cell, new ValueStateArgs(false, typeLabelActive: true, inputActive: true, applyActive: CanWrite));
                    break;
                case ValueState.String:
                    SetIValueState();
                    SetValueState(cell, new ValueStateArgs(true, false, SignatureHighlighter.StringOrange, subContentButtonActive: true));
                    break;
                case ValueState.Enum:
                    SetIValueState();
                    SetValueState(cell, new ValueStateArgs(true, subContentButtonActive: true));
                    break;
                case ValueState.Collection:
                case ValueState.ValueStruct:
                    SetIValueState();
                    SetValueState(cell, new ValueStateArgs(true, inspectActive: true, subContentButtonActive: true));
                    break;
                case ValueState.Unsupported:
                    SetValueState(cell, new ValueStateArgs(true, inspectActive: true));
                    break;
            }
        }

        protected virtual void SetValueState(CacheObjectCell cell, ValueStateArgs args)
        {
            if (args.valueActive)
            {
                cell.ValueLabel.text = ValueLabelText;
                cell.ValueLabel.supportRichText = args.valueRichText;
                cell.ValueLabel.color = args.valueColor;
            }
            else
                cell.ValueLabel.text = "";

            cell.TypeLabel.gameObject.SetActive(args.typeLabelActive);
            if (args.typeLabelActive)
                cell.TypeLabel.text = SignatureHighlighter.ParseFullType(Value.GetActualType(), false);

            cell.Toggle.gameObject.SetActive(args.toggleActive);
            if (args.toggleActive)
            {
                cell.Toggle.isOn = (bool)Value;
                cell.ToggleText.text = Value.ToString();
            }

            cell.InputField.gameObject.SetActive(args.inputActive);
            if (args.inputActive)
            {
                cell.InputField.text = Value.ToString();
                cell.InputField.readOnly = !CanWrite;
            }

            cell.ApplyButton.Button.gameObject.SetActive(args.applyActive);
            cell.InspectButton.Button.gameObject.SetActive(args.inspectActive);
            cell.SubContentButton.Button.gameObject.SetActive(args.subContentButtonActive);
        }

        // IValues

        /// <summary>Called from SetCellState if SubContent button is wanted.</summary>
        public void SetIValueState()
        {
            if (this.IValue == null)
                return;

            // TODO ?
        }

        // temp for testing
        public virtual void OnCellSubContentToggle()
        {
            if (this.IValue == null)
            {
                var ivalueType = InteractiveValue.GetIValueTypeForState(State);
                IValue = (InteractiveValue)Pool.Borrow(ivalueType);
                CurrentIValueType = ivalueType;

                IValue.SetOwner(this);
                IValue.SetValue(this.Value);
                IValue.UIRoot.transform.SetParent(CellView.SubContentHolder.transform, false);
                CellView.SubContentHolder.SetActive(true);
                SubContentState = true;

                // update our cell after creating the ivalue (the value may have updated, make sure its consistent)
                this.ProcessOnEvaluate();
                this.SetCell(this.CellView);
            }
            else
            {
                SubContentState = !SubContentState;
                CellView.SubContentHolder.SetActive(SubContentState); 
            }
        }

        public virtual void ReleaseIValue()
        {
            if (IValue == null)
                return;

            IValue.ReleaseFromOwner();
            Pool.Return(CurrentIValueType, IValue);

            IValue = null;
        }

        internal void HideIValue()
        {
            if (this.IValue == null)
                return;

            this.IValue.UIRoot.transform.SetParent(InactiveIValueHolder.transform, false);
        }

        // CacheObjectCell Apply

        public virtual void OnCellApplyClicked()
        {
            if (CellView == null)
            {
                ExplorerCore.LogWarning("Trying to apply CacheMember but current cell reference is null!");
                return;
            }

            if (State == ValueState.Boolean)
                SetUserValue(this.CellView.Toggle.isOn);
            else
            {
                if (!numberParseMethods.ContainsKey(FallbackType.AssemblyQualifiedName))
                {
                    var method = FallbackType.GetMethod("Parse", new Type[] { typeof(string) });
                    numberParseMethods.Add(FallbackType.AssemblyQualifiedName, method);
                }

                var val = numberParseMethods[FallbackType.AssemblyQualifiedName]
                    .Invoke(null, new object[] { CellView.InputField.text });
                SetUserValue(val);
            }

            SetCell(this.CellView);
        }

        public struct ValueStateArgs
        {
            public ValueStateArgs(bool valueActive = true, bool valueRichText = true, Color? valueColor = null,
                bool typeLabelActive = false, bool toggleActive = false, bool inputActive = false, bool applyActive = false,
                bool inspectActive = false, bool subContentButtonActive = false)
            {
                this.valueActive = valueActive;
                this.valueRichText = valueRichText;
                this.valueColor = valueColor == null ? Color.white : (Color)valueColor;
                this.typeLabelActive = typeLabelActive;
                this.toggleActive = toggleActive;
                this.inputActive = inputActive;
                this.applyActive = applyActive;
                this.inspectActive = inspectActive;
                this.subContentButtonActive = subContentButtonActive;
            }

            public static ValueStateArgs Default => _default;
            private static ValueStateArgs _default = new ValueStateArgs(true);

            public bool valueActive, valueRichText, typeLabelActive, toggleActive,
                inputActive, applyActive, inspectActive, subContentButtonActive;

            public Color valueColor;
        }
    }
}
