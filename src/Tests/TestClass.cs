﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Specialized;
using MelonLoader;

// used to test multiple generic constraints
public class TestGeneric : IComparable<string>
{
    public TestGeneric() { }

    public int CompareTo(string other) => throw new NotImplementedException();
}

[Flags]
public enum TestFlags
{
    Red,
    Green,
    Blue
}

// test non-flags weird enum
public enum WeirdEnum
{
    First = 1,
    Second,
    Third = 2,
    Fourth,
    Fifth
}

namespace Explorer.Tests
{
    public class TestClass
    {
        public static TestFlags testFlags = TestFlags.Blue | TestFlags.Green;
        public static WeirdEnum testWeird = WeirdEnum.First;

        public static int testBitmask;

        public static TestClass Instance => m_instance ?? (m_instance = new TestClass());
        private static TestClass m_instance;

        public TestClass()
        {
            ILHashSetTest = new Il2CppSystem.Collections.Generic.HashSet<string>();
            ILHashSetTest.Add("1");
            ILHashSetTest.Add("2");
            ILHashSetTest.Add("3");

            testBitmask = 1 | 2;
        }

        public static int StaticProperty => 5;
        public static int StaticField = 5;
        public int NonStaticField;

        public static string TestGeneric<C, T>(string arg0) where C : Component where T : TestGeneric, IComparable<string>
        {
            return $"C: '{typeof(C).FullName}', T: '{typeof(T).FullName}', arg0: '{arg0}'";
        }

        public static string TestRefInOutGeneric<T>(ref string arg0, in int arg1, out string arg2)
        {
            arg2 = "this is arg2";

            return $"T: '{typeof(T).FullName}', ref arg0: '{arg0}', in arg1: '{arg1}', out arg2: '{arg2}'";
        }

        //// this type of generic is not supported, due to requiring a non-primitive argument.
        //public static T TestDifferentGeneric<T>(T obj) where T : Component
        //{
        //    return obj;
        //}

        // test a non-generic dictionary

        public Hashtable TestNonGenericDict()
        {
            return new Hashtable
            {
                { "One",   1 },
                { "Two",   2 },
                { "Three", 3 },
            };
        }

        // IL2CPP HASHTABLE NOT SUPPORTED! Cannot assign Il2CppSystem.Object from primitive struct / string.
        // Technically they are "supported" but if they contain System types they will not work.

        //public Il2CppSystem.Collections.Hashtable TestIl2CppNonGenericDict()
        //{
        //    var table = new Il2CppSystem.Collections.Hashtable();
        //    table.Add("One", 1);
        //    table.Add("One", 2);
        //    table.Add("One", 3);
        //    return table;
        //}

        // test HashSets

        public static HashSet<string> HashSetTest = new HashSet<string>
        {
            "One",
            "Two",
            "Three"
        };

        public static Il2CppSystem.Collections.Generic.HashSet<string> ILHashSetTest;

        // Test indexed parameter

        public string this[int arg0, string arg1]
        {
            get
            {
                return $"arg0: {arg0}, arg1: {arg1}";
            }
        }

        // Test basic list

        public static List<string> TestList = new List<string>
        {
            "1",
            "2",
            "3",
            "etc..."
        };

        // Test a nested dictionary

        public static Dictionary<int, Dictionary<string, int>> NestedDictionary = new Dictionary<int, Dictionary<string, int>>
        {
            {
                1,
                new  Dictionary<string, int>
                {
                    {
                        "Sub 1", 123
                    },
                    {
                        "Sub 2", 456
                    },
                }
            },
            {
                2,
                new  Dictionary<string, int>
                {
                    {
                        "Sub 3", 789
                    },
                    {
                        "Sub 4", 000
                    },
                }
            },
        };

        // Test a basic method

        public static Color TestMethod(float r, float g, float b, float a)
        {
            return new Color(r, g, b, a);
        }

        // A method with default arguments

        public static Vector3 TestDefaultArgs(float arg0, float arg1, float arg2 = 5.0f)
        {
            return new Vector3(arg0, arg1, arg2);
        }
    }
}
