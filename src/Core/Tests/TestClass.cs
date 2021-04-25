﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Tests
{
    public static class TestClass
    {
        static TestClass()
        {
            List = new List<string>();
            for (int i = 0; i < 10000; i++)
                List.Add(i.ToString());
        }

        public static List<string> List;

#if CPP
        public static string testStringOne = "Test";
        public static Il2CppSystem.Object testStringTwo = "string boxed as cpp object";
        public static Il2CppSystem.String testStringThree = "string boxed as cpp string";
        public static string nullString = null;

        public static Il2CppSystem.Collections.Hashtable testHashset;
        public static Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object> testList;

        static TestClass()
        {
            testHashset = new Il2CppSystem.Collections.Hashtable();
            testHashset.Add("key1", "itemOne");
            testHashset.Add("key2", "itemTwo");
            testHashset.Add("key3", "itemThree");

            testList = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>(3);
            testList.Add("One");
            testList.Add("Two");
            testList.Add("Three");
            //testIList = list.TryCast<Il2CppSystem.Collections.IList>();
        }
#endif
    }
}
