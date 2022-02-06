using System;
using System.Collections.Generic;
using Bounce.Unmanaged;
using HarmonyLib;
using Newtonsoft.Json;

using UnityEngine;

namespace BoardPersistence
{
    public partial class BoardPersistencePlugin
    {
        internal static Dictionary<(NGuid,string), string> BoardData = new Dictionary<(NGuid, string), string>();
        internal static Dictionary<string, string> CampaignData = new Dictionary<string, string>();
        
        internal static Dictionary<string, Action<string>> Callbacks = new Dictionary<string, Action<string>>();

        internal static NGuid BoardId => BoardSessionManager.CurrentBoardInfo.Id.Value;

        public void SetBoardData(string key, string data)
        {
            BoardData[(BoardId,key)] = data;
            Api3CampaignGetInventoryReadPatch._instance.SaveData();
        }

        public string GetBoardData(string key)
        {
            Api3CampaignGetInventoryReadPatch._instance.LoadData(CampaignSessionManager.Id);
            return BoardData[(BoardId, key)];
        }

        public void SetCampaignData(string key, string data)
        {
            CampaignData[key] = data;
            Api3CampaignGetInventoryReadPatch._instance.SaveData();
        }

        public string GetCampaignData(string key)
        {
            Api3CampaignGetInventoryReadPatch._instance.LoadData(CampaignSessionManager.Id);
            return CampaignData[key];
        }

        internal static string GetBoardJson() => JsonConvert.SerializeObject(BoardData);
        internal static string GetCampaignJson() => JsonConvert.SerializeObject(CampaignData);
        internal static void SetBoardJson(string data) => BoardData = JsonConvert.DeserializeObject<Dictionary<(NGuid, string), string>>(data);
        internal static void SetCampaignJson(string data) => CampaignData = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
    }

    [HarmonyPatch(typeof(UI_InventoryPanel), "SetFromJson")]
    internal class Api3CampaignGetInventoryReadPatch
    {
        internal static UI_InventoryPanel _instance;
        static void Prefix(ref string json, ref UI_InventoryPanel __instance)
        {
            _instance = __instance;
            UI_InventoryPanel.InventoryPanelData inventoryPanelData = JsonUtility.FromJson<UI_InventoryPanel.InventoryPanelData>(json);
            for (int i = 0; i < inventoryPanelData.entries.Count; i++)
            {
                switch (inventoryPanelData.entries[i].location)
                {
                    case 100:
                        BoardPersistencePlugin.SetBoardJson(inventoryPanelData.entries[i].guid);
                        inventoryPanelData.entries.RemoveAt(i);
                        i--;
                        break;
                    case 101:
                        BoardPersistencePlugin.SetCampaignJson(inventoryPanelData.entries[i].guid);
                        inventoryPanelData.entries.RemoveAt(i);
                        i--;
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(UI_InventoryPanel), "SaveData")]
    internal class Api3CampaignGetInventoryWritePatch
    {
        static bool Prefix(ref Transform ____contentParent,ref string ____syncData,ref float ____syncTimer)
        {
            CampaignGuid id = CampaignSessionManager.Id;
            if (id == new CampaignGuid())
                return false;
            UI_InventoryPanel.InventoryPanelData inventoryPanelData = new UI_InventoryPanel.InventoryPanelData();
            UI_InventorySlot[] componentsInChildren = ____contentParent.GetComponentsInChildren<UI_InventorySlot>();
            for (int index = 0; index < componentsInChildren.Length; ++index)
            {
                UI_InventorySlot uiInventorySlot = componentsInChildren[index];
                if (uiInventorySlot.GetContent() != null)
                    inventoryPanelData.entries.Add(new UI_InventoryPanel.InventoryPanelData.InventoryEntry()
                    {
                        guid = uiInventorySlot.GetContent()._boardAssetGuid.ToString(),
                        location = index
                    });
            }
            string json = JsonUtility.ToJson(inventoryPanelData);
            if (json != ____syncData)
            {
                ____syncData = json;
                ____syncTimer = 5f;
            }
            inventoryPanelData.entries.Add(new UI_InventoryPanel.InventoryPanelData.InventoryEntry { guid = BoardPersistencePlugin.GetBoardJson(), location = 100 });
            inventoryPanelData.entries.Add(new UI_InventoryPanel.InventoryPanelData.InventoryEntry { guid = BoardPersistencePlugin.GetCampaignJson(), location = 101 });
            json = JsonUtility.ToJson(inventoryPanelData);

            PlayerPrefs.SetString("inventory_" + BackendManager.TSUserID + CampaignSessionManager.Id, json);
            return false;
        }
    }
}
