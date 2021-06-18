using BepInEx;
using Newtonsoft.Json;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace HolloFoxes
{
    [BepInPlugin(Guid, Name, Version)]
    public partial class BoardPersistence : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Board Persistence Plug-In";
        public const string Guid = "org.hollofox.plugins.boardpersistence";
        public const string Version = "1.0.0.0";

        // Prevent multiple sources from modifying data at once
        private static object exclusionLock = new object();

        // Holds callback subscriptions for message distribution
        private static Dictionary<Guid, Subscription> subscriptions = new Dictionary<Guid, Subscription>();

        private static string data = "";

        /// <summary>
        /// Class for holding callback subscriptions
        /// </summary>
        public class Subscription
        {
            public string key { get; set; }
            public Action<Change[]> callback { get; set; }
        }

        /// <summary>
        /// Enumeration to determine what type of change occured
        /// </summary>
        public enum ChangeType
        {
            added = 1,
            modified,
            removed
        }

        /// <summary>
        /// Class to define Stat Message block changes
        /// </summary>
        public class Change
        {
            public ChangeType action { get; set; }
            public string key { get; set; }
            public string previous { get; set; }
            public string value { get; set; }
        }

        // Variable to prevent overlapping checks in case the check is taking too long
        private static bool checkInProgress = false;

        private static bool ready = false;


        /// <summary>
        /// Method triggered when the plugin loads
        /// </summary>
        public void Awake()
        {
            Debug.Log("Board Persistence Plugin now active. Automatic message checks will being when the board loads.");
            BoardSessionManager.OnStateChange += (s) =>
            {
                if (s.ToString().Contains("+Active"))
                {
                    ready = true;
                }
                else
                {
                    ready = false;
                    Reset();
                    Debug.Log("Board Persistence stopped looking for messages.");
                }
            };
        }

        /// <summary>
        /// Method triggered periodically
        /// </summary>
        public void Update()
        {
            if (ready) { StatMessagingCheck(); }
        }

        /// <summary>
        /// Method to subscribe to Stat Messages of a certain key
        /// (Guids are used for subscription removal instead of the key so that multiple plugins can be looking at the same messages)
        /// </summary>
        /// <param name="key">The key of the messages for which changes should trigger callbacks</param>
        /// <param name="dataChangeCallback">Callback that receives the changes</param>
        /// <returns>Guid associated with the subscription which can be used to unsubscribe</returns>
        public static System.Guid Subscribe(string key, Action<Change[]> dataChangeCallback)
        {
            System.Guid guid = System.Guid.NewGuid();
            subscriptions.Add(guid, new Subscription() { key = key, callback = dataChangeCallback });
            return guid;
        }

        /// <summary>
        /// Method to remove a subscription associated with a specific Guid
        /// (Guids are used for subscription removal instead of the key so that multiple plugins can be looking at the same messages)
        /// </summary>
        /// <param name="subscriptionId">Guid of the subscription to be removed (provided by the Subscribe method)</param>
        public static void Unsubscribe(System.Guid subscriptionId)
        {
            if (subscriptions.ContainsKey(subscriptionId)) { subscriptions.Remove(subscriptionId); }
        }

        /// <summary>
        /// Method to set a new piece of information or modify an existing one in Board Persistence block
        /// </summary>
        /// <param name="cid">CreatureId whose block is to be changed</param>
        /// <param name="key">String key for which data is to be changed (e.g. plugin unique identifier)</param>
        /// <param name="value">String value of the key (e.g. value to be communicated)</param>
        public static void SetInfo(string key, string value)
        {
            // Minimize race conditions
            lock (exclusionLock)
            {
                // Extract the JSON portion of the name
                var boardName = BoardSessionManager.CurrentBoardInfo.BoardName;
                string json = boardName.Substring(boardName.IndexOf("<size=0>") + "<size=0>".Length);
                // Convert to a dictionary
                Dictionary<string, string> info = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                // Modify or add the specified value under the specified key
                if (info.ContainsKey(key)) { info[key] = value; } else { info.Add(key, value); }
                // Update character name
                BoardSessionManager.RenameBoard(boardName.Substring(0, boardName.IndexOf("<size=0>") + "<size=0>".Length) + JsonConvert.SerializeObject(info));
            }
        }

        /// <summary>
        /// Method to clear a piece of information in Board Persistence block
        /// </summary>
        /// <param name="cid">CreatureId whose block is to be changed</param>
        /// <param name="key">String key for which data is to be cleared (e.g. plugin unique identifier)</param>
        public static void ClearInfo(string key)
        {
            // Minimize race conditions
            lock (exclusionLock)
            {
                var boardName = BoardSessionManager.CurrentBoardInfo.BoardName;
                // Extract the JSON portion of the name
                string json = boardName.Substring(boardName.IndexOf("<size=0>") + "<size=0>".Length);
                    // Convert to a dictionary
                Dictionary<string, string> info = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                // Remove the key if it exists
                if (info.ContainsKey(key)) { info.Remove(key); }
                // Update character name
                BoardSessionManager.RenameBoard(boardName.Substring(0, boardName.IndexOf("<size=0>") + "<size=0>".Length) + JsonConvert.SerializeObject(info));
            }
        }

        /// <summary>
        /// Method used to read the last recorded value for a particular key on a particular creature
        /// (typically used to get current values for things like inputs)
        /// </summary>
        /// <param name="cid">Identification of the creature whose key is to bb read</param>
        /// <param name="key">Identification of the key to be read</param>
        /// <returns>Value of the key or an empty string if the key is not set or the cid is invalid</returns>
        public static string ReadInfo(string key)
        {
            // Minimize race conditions
            lock (exclusionLock)
            { 
               string json = data;
               json = json.Substring(json.IndexOf("<size=0>") + "<size=0>".Length);
               Dictionary<string, string> keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
               if (keys.ContainsKey(key))
               {
                   return keys[key];
               }
               else
               {
                    Debug.LogWarning("Key '" + key + "' not defined in data dictionary");
               }
               return "";
            }
        }

        /// <summary>
        /// Method used to reset the data dictionary and thus reprocess all Stat Message changes.
        /// Typically used on a new board load to dump old board data and also to re-process it if the board is reloaded.
        /// </summary>
        public static void Reset()
        {
            Debug.Log("Board Persistence data dictionary reset");
            data = "";
        }


        /// <summary>
        /// Method to get Creature Name
        /// </summary>
        /// <param name="asset">CreatureBoardAsset</param>
        /// <returns>String representation of the creature name</returns>
        public static string GetBoardName()
        {
            string name = BoardSessionManager.CurrentBoardInfo.BoardName;
            if (name.Contains("<size=0>")) { name = name.Substring(0, name.IndexOf("<size=0>")).Trim(); }
            return name;
        }

        /// <summary>
        /// Method that performs actual checks for stat messages
        /// </summary>
        private static void StatMessagingCheck()
        {
            if (string.IsNullOrWhiteSpace(data)) data = BoardSessionManager.CurrentBoardInfo.BoardName;
            if (checkInProgress) { return; }

            // Prevent overlapping checks (in case checks are taking too long)
            checkInProgress = true;

            try
            {
                // Check all creatures
                // Read the creature name into a string. Routine will use this because setting name takes time (i.e. is not reflected immediately).
                string boardName = BoardSessionManager.CurrentBoardInfo.BoardName;

                // Ensure creature has a JSON Block
                if (!boardName.Contains("<size=0>"))
                {
                    Debug.Log("Appending size because '" + GetBoardName() + "' This is probably a new board.");
                    BoardSessionManager.RenameBoard(boardName + "<size=0>{}");
                    boardName = boardName + "<size=0>{}";
                    data = boardName;
                }

                // Check to see if the creature name has changed
                if (boardName != data)
                {
                    // Extract JSON ending
                    string lastJson = data.Substring(data.IndexOf("<size=0>") + "<size=0>".Length);
                    string currentJson = boardName.Substring(boardName.IndexOf("<size=0>") + "<size=0>".Length);
                    // Compare entries
                    Dictionary<string, string> last = JsonConvert.DeserializeObject<Dictionary<string, string>>(lastJson);
                    Dictionary<string, string> current = JsonConvert.DeserializeObject<Dictionary<string, string>>(currentJson);
                    // Update data dictionary with current info
                    data = boardName;
                    List<Change> changes = new List<Change>();
                    // Compare entries in the last data to current data
                    foreach (KeyValuePair<string, string> entry in last)
                    {
                        // If last data does not appear in current data then the data was removed
                        if (!current.ContainsKey(entry.Key))
                        {
                            changes.Add(new Change() { action = ChangeType.removed, key = entry.Key, previous = entry.Value, value = "" });
                        }
                        else
                        {
                            // If last data does not match current data then the data has been modified
                            if (entry.Value != current[entry.Key])
                            {
                                changes.Add(new Change() { action = ChangeType.modified, key = entry.Key, previous = entry.Value, value = current[entry.Key] });
                            };
                        }
                    }
                    // Compare entries in current data to last data
                    foreach (KeyValuePair<string, string> entry in current)
                    {
                        // If current data does not exist in last data then a new entry has been added
                        if (!last.ContainsKey(entry.Key))
                        {
                            changes.Add(new Change() { action = ChangeType.added, key = entry.Key, previous = "", value = entry.Value });
                        };
                    }
                    Debug.Log($"Json {JsonConvert.SerializeObject(changes)}");
                    // Process callback if there were any changes
                    if (changes.Count > 0)
                    {
                        // Run through each change
                        foreach (Change change in changes)
                        {
                            Debug.Log("Board Persistence Change, Type: " + change.action.ToString() + ", Key: " + change.key + ", Previous: " + change.previous + ", Current: " + change.value);
                            // Check each subscription
                            foreach (Subscription subscription in subscriptions.Values)
                            {
                                // Trigger a callback for anyone subscription matching the key
                                if (subscription.key == change.key)
                                {
                                    subscription.callback(new Change[] { change });
                                }
                            }
                        }
                        // Check for legacy wild card subscriptions
                        foreach (Subscription subscription in subscriptions.Values)
                        {
                            if (subscription.key == "*") { subscription.callback(changes.ToArray()); }
                        }
                    }
                }
            }
            catch (Exception x) { Debug.LogWarning(x); }

            // Indicated that next check is allowed
            checkInProgress = false;
        }
    }
}
