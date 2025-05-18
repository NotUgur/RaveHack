using UnityEngine;
using System.IO;
using System;

namespace RaveHack
{
    public class Loader
    {
        private static GameObject _RaveHack;

        public static void Init()
        {
            {


                _RaveHack = new GameObject("RaveHack");
                _RaveHack.AddComponent<Hacks>();
                UnityEngine.Object.DontDestroyOnLoad(_RaveHack);
            }
        }
            

        public static void Unload()
        {
            GameObject.Destroy(_RaveHack);
        }
    }
}