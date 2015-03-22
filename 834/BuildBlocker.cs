// Reference: Oxide.Ext.Rust
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BuildBlocker", "Bombardir", "1.1.0" )]
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
        private bool BlockStructuresHeight = false;
        private bool BlockDeployablesHeight = false;
        private int MaxHeight = 100;
        private bool BlockStructuresWater = false;
        private bool BlockDeployablesWater = false;
        private int MaxWater = -2;
        private int AuthLVL = 2;
        private string Msg = "Hey! You can't build here!";
        private string MsgHeight = "You can't build here! (Height limit 100m)";
        private string MsgWater = "You can't build here! (Water limit -2m)";

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
            CheckCfg<int>("Max Height Limit", ref MaxHeight);
            CheckCfg<bool>("Block Structures above the max height", ref BlockStructuresHeight);
            CheckCfg<bool>("Block Deployables above the max height", ref BlockDeployablesHeight);
            CheckCfg<int>("Max Under Water Height Limit", ref MaxWater);
            CheckCfg<bool>("Block Structures under water", ref BlockStructuresWater);
            CheckCfg<bool>("Block Deployables under water", ref BlockDeployablesWater);
            CheckCfg<string>("Block Water Message", ref MsgWater);
            CheckCfg<string>("Block Height Message", ref MsgHeight);
            CheckCfg<string>("Block Message", ref Msg); 
            CheckCfg<int>("Ignore Auth Lvl", ref AuthLVL);
            SaveConfig(); 
        }  
        #endregion 
         
        private void CheckBlock(BaseNetworkable StartBlock, BasePlayer sender, bool CheckHeight, bool CheckWater)
        {
            if (StartBlock && sender.net.connection.authLevel < AuthLVL && !StartBlock.isDestroyed)
            {
                Vector3 Pos = StartBlock.transform.position;
                float height = TerrainMeta.HeightMap.GetHeight(Pos);
                if (CheckHeight && Pos.y - height > MaxHeight)
                {
                    SendReply(sender, MsgHeight);
                    StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                else if (CheckWater && height < 0 && height < MaxWater && Pos.y < 2.8 )
                {
                    SendReply(sender, MsgWater);
                    StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                else
                {
                    if (StartBlock.name == "foundation.steps(Clone)")
                        Pos.y++;
                    Pos.y = Pos.y + 100;
                    RaycastHit[] hits = Physics.RaycastAll(Pos, Vector3.down, 102.8f);
                    Pos.y = Pos.y - 100;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit hit = hits[i];
                        if (hit.collider)
                        {
                            string ColName = hit.collider.name;
                            if ( (UnTerrain && ColName == "Terrain" || InMetalBuilding && ColName == "Metal_building_COL" || UnBridge && ColName == "Bridge_top" || InBase && ColName.StartsWith("base", StringComparison.CurrentCultureIgnoreCase) || UnRadio && ColName.StartsWith("dish")) && hit.point.y > Pos.y || (InWarehouse && ColName.StartsWith("Warehouse") || InHangar && ColName.StartsWith("Hangar")) && hit.point.y + 3 > Pos.y || ColName.StartsWith("rock") && (hit.point.y < Pos.y ? OnRock : hit.collider.bounds.Contains(Pos) ? InRock : InCave))
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
        }

        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            CheckBlock(obj.GetComponent<BaseNetworkable>(), plan.ownerPlayer, BlockStructuresHeight, BlockStructuresWater);
        }

        void OnItemDeployed(Deployer deployer, BaseEntity deployedentity)
        {
            if (!(deployedentity is BaseLock))
                CheckBlock(deployedentity.GetComponent<BaseNetworkable>(), deployer.ownerPlayer, BlockDeployablesHeight, BlockDeployablesWater);
        }
    }
}