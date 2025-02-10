using BepInEx;
using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using UnityEngine.SceneManagement;

namespace Stratagem
{
    // TODO: Change 'YourPlugin' to the name of your plugin
    [BepInPlugin("Packer.StratagemPlugin", "StratagemPlugin", "1.0.0")]
    [BepInProcess("h3vr.exe")]
    public partial class StratagemPlugin : BaseUnityPlugin
    {
        public static StratagemPlugin instance;
        public static List<Sosig> sosigs = new List<Sosig>();

        private void Awake()
        {
            Logger = base.Logger;

            instance = this;

            // Your plugin's ID, Name, and Version are available here.
            //Logger.LogMessage($"Hello, world! Sent from {Id} {Name} {Version}");
        }

        private void Start()
        {
            StartCoroutine(ModLoader.LoadModPackages());
            SceneManager.activeSceneChanged += ReregisterSosigKillEvent;
        }

        private void OnDestroy()
        {
            GM.CurrentSceneSettings.SosigKillEvent -= RemoveSosig;
        }

        private void ReregisterSosigKillEvent(Scene current, Scene next)
        {
            if (GM.CurrentSceneSettings == null)
                return;

            GM.CurrentSceneSettings.SosigKillEvent -= RemoveSosig;
            GM.CurrentSceneSettings.SosigKillEvent += RemoveSosig;
        }

        // The line below allows access to your plugin's logger from anywhere in your code, including outside of this file.
        // Use it with 'YourPlugin.Logger.LogInfo(message)' (or any of the other Log* methods)
        internal new static ManualLogSource Logger { get; private set; }

        public static void AddSosig(Sosig s)
        {
            //Clean up list every time we add new sosigs incase of scene change
            for (int i = sosigs.Count - 1; i >= 0; i--)
            {
                if (sosigs[i] == null)
                    sosigs.RemoveAt(i);
            }
            sosigs.Add(s);
        }

        public static void RemoveSosig(Sosig s)
        {
            if (sosigs.Contains(s))
            {
                instance.StartCoroutine(DelayKillSosig(s));
                sosigs.Remove(s);
            }
        }

        protected static IEnumerator DelayKillSosig(Sosig s)
        {
            yield return new WaitForSeconds(5);

            s.ClearSosig();
        }

        public static void DestroyObject(float time, GameObject gameObj)
        {
            instance.StartCoroutine(DelayDestroy(time, gameObj));
        }

        protected static IEnumerator DelayDestroy(float time, GameObject gameObj)
        {
            yield return new WaitForSeconds(time);
            if(gameObj != null)
                Destroy(gameObj);
        }
    }
}
