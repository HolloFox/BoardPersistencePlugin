using BepInEx;
using BepInEx.Logging;
using BRClient;
using UnityEngine;
using HarmonyLib;

namespace BoardPersistence
{
    [BepInPlugin(Guid, Name, Version)]
    public partial class BoardPersistencePlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "HolloFoxes' Board Persistence Plug-In";
        public const string Guid = "org.hollofox.plugins.boardpersistence";
        public const string Version = "3.0.0.0";
        internal static ManualLogSource BPLogger;

        /// <summary>
        /// Method triggered when the plugin loads
        /// </summary>
        public void Awake()
        {
            BPLogger = Logger;
            Logger.LogInfo($"In Awake for {Name}");
            var harmony = new Harmony(Guid);
            harmony.PatchAll();
            Debug.Log($"{Name} Patched");
        }
    }
}
