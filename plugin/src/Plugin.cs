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
    [BepInPlugin("Packer.StratagemPlugin", "StratagemPlugin", "1.0.1")]
    [BepInProcess("h3vr.exe")]
    public partial class StratagemPlugin : BaseUnityPlugin
    {
        public static StratagemPlugin instance;
        public static List<Sosig> sosigs = new List<Sosig>();
        private static bool sosigClearUpdate = false;

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
            instance.StartCoroutine(SosigUpdate());
            //SceneManager.activeSceneChanged += ReregisterSosigKillEvent;
        }

        private void OnDestroy()
        {
            //GM.CurrentSceneSettings.SosigKillEvent -= RemoveSosig;
            sosigs = null;
        }

        private void ReregisterSosigKillEvent(Scene current, Scene next)
        {
            if (GM.CurrentSceneSettings == null)
                return;

            //GM.CurrentSceneSettings.SosigKillEvent -= RemoveSosig;
            //GM.CurrentSceneSettings.SosigKillEvent += RemoveSosig;
        }

        // The line below allows access to your plugin's logger from anywhere in your code, including outside of this file.
        // Use it with 'YourPlugin.Logger.LogInfo(message)' (or any of the other Log* methods)
        internal new static ManualLogSource Logger { get; private set; }

        //Manuelly check for added sosigs to remove
        protected static IEnumerator SosigUpdate()
        {
            yield return new WaitForSeconds(Random.Range(4f, 6f));

            if (sosigs != null)
            {
                for (int i = sosigs.Count - 1; i >= 0; i--)
                {
                    ClearSosig(sosigs[i]);
                }
                instance.StartCoroutine(SosigUpdate());
                yield break;
            }
        }

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

        public static void ClearSosig(Sosig s)
        {
            if (sosigs.Contains(s) && s.BodyState == Sosig.SosigBodyState.Dead)
            {
                s.ClearSosig();
                sosigs.Remove(s);
            }
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
