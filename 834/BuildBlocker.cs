// Reference: Oxide.Ext.Rust
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BuildBlocker", "Bombardir", "1.0.1" )]
    class BuildBlocker : RustPlugin
    {
        #region Config

        private bool OnRock = false;
        private bool InRock = true;
        private bool InCave = false;
        private bool InWarehouse = true;
        private bool InMetalBuilding = true;
        private bool InHangar = true;
        private bool InBase = true;
        private bool UnTerrain = true;
        private bool UnBridge = false;
        private bool UnRadio = false;
        private int AuthLVL = 2;
        private string Msg = "Hey! You can't build here!";

        void LoadDefaultConfig() {}  

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
				Config[Key] = var;
        }

        void Init() 
        {
            CheckCfg<bool>("Block On Rock", ref OnRock);
            CheckCfg<bool>("Block In Rock", ref InRock);
            CheckCfg<bool>("Block In Rock Cave", ref InCave);
            CheckCfg<bool>("Block In Base", ref InBase);
            CheckCfg<bool>("Block In Warehouse", ref InWarehouse);
            CheckCfg<bool>("Block In Metal Building", ref InMetalBuilding);
            CheckCfg<bool>("Block In Hangar", ref InHangar);
            CheckCfg<bool>("Block Under Terrain", ref UnTerrain);
            CheckCfg<bool>("Block Under Bridge", ref UnBridge);
            CheckCfg<bool>("Block Under Radar", ref UnRadio);
            CheckCfg<string>("Message", ref Msg);
            CheckCfg<int>("Ignore Auth Lvl", ref AuthLVL);
            SaveConfig(); 
        }  
        #endregion
         
        private void CheckBlock(BaseNetworkable StartBlock, BasePlayer sender)
        {
            if (StartBlock && sender.net.connection.authLevel < AuthLVL)
            {
                Vector3 Pos = StartBlock.transform.position;
				if (StartBlock.name == "foundation.steps(Clone)")
					Pos.y++;
                Pos.y = Pos.y + 100;
                RaycastHit[] hits = Physics.RaycastAll(Pos, Vector3.down, 103f);
                Pos.y = Pos.y - 100;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (StartBlock.isDestroyed)
                        break;
                    RaycastHit hit = hits[i];
                    if (hit.collider)
                    {
                        string ColName = hit.collider.name;
                        if ((((UnTerrain && ColName == "Terrain") || (InMetalBuilding && ColName == "Metal_building_COL") || (UnBridge && ColName == "Bridge_top") || (InBase && ColName.StartsWith("base", StringComparison.CurrentCultureIgnoreCase)) || (UnRadio && ColName.StartsWith("dish"))) && hit.point.y > Pos.y) || (((InWarehouse && ColName.StartsWith("Warehouse")) || (InHangar && ColName.StartsWith("Hangar"))) && hit.point.y + 3 > Pos.y) || (ColName.StartsWith("rock") && (hit.point.y < Pos.y ? OnRock : hit.collider.bounds.Contains(Pos) ? InRock : InCave)))
                        {
                            SendReply(sender, Msg);
                            StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                            break;
                        }
                        if (ColName == "Terrain")
                            break;
                    }
                }
            }
        }

        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            CheckBlock(obj.GetComponent<BaseNetworkable>(), plan.ownerPlayer);
        }

        void OnItemDeployed(Deployer deployer, BaseEntity deployedentity)
        {
            if (!(deployedentity is BaseLock))
                CheckBlock(deployedentity.GetComponent<BaseNetworkable>(), deployer.ownerPlayer);
        }
    }
}