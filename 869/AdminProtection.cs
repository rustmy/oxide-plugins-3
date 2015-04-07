using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Text;


namespace Oxide.Plugins
{
    [Info("AdminProtection", "4seti [aka Lunatiq] for Rust Planet", "0.3.1", ResourceId = 869)]
	public class AdminProtection : RustPlugin
    {
        #region Utility Methods

        private void Log(string message)
        {
            Puts("{0}: {1}", Title, message);
        }

        private void Warn(string message)
        {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message)
        {
            PrintError("{0}: {1}", Title, message);
        }

        #endregion

        private Dictionary<string, ProtectionStatus> protData;
        private Dictionary<string, DateTime> antiSpam;
        private Dictionary<string, string> APHelper = new Dictionary<string,string>();

        Dictionary<string, string> defMsg = new Dictionary<string, string>()
                {
                    {"Enabled", "You <color=#81F23F>ENABLED</color> Admin Protection!"},
                    {"LootAlert", "<color=#FF6426>You are trying to loop sleeping admin, please don't!</color>"},
                    {"EnabledTo", "You <color=#81F23F>ENABLED</color> Admin Protection for player: {0}!"},
                    {"DisabledTo",  "You <color=#F23F3F>DISABLED</color> Admin Protection for player: {0}!"},
                    {"TooMuch",  "More than one match!"},
                    {"Enabled_s",  "You <color=#81F23F>ENABLED</color> Admin Protection in complete silent mode!"},
                    {"Enabled_m",  "You <color=#81F23F>ENABLED</color> Admin Protection with no mesage to attacker!"},
                    {"Disabled",  "You <color=#F23F3F>DISABLED</color> Admin Protection!"},
                    {"HelpMessage",  "/ap - This command will toggle Admin Protection on or off."},
                    {"NoAPDamageAttacker",  "{0} is admin, you can't kill him."},
                    {"NoAPDamagePlayer",  "{0} is trying to kill you."},
                    {"ChatName",  "Admin Protection"},
                    {"Error",  "Error!"},
                    {"LootMessageLog",  "{0} - is trying to loot admin - {1}"},
					{"APListByAdmin",  "<color=#007BFF>{0}</color>[{1}], Mode: <color=#FFBF00>{2}</color>, Enabled By: <color=#81F23F>{3}</color>"},
					{"APListAdmin",  "<color=#81F23F>{0}</color>[{1}], Mode: <color=#FFBF00>{2}</color>"},
					{"APListHeader",  "<color=#81F23F>List of active AdminProtections</color>"}
                };

        void Loaded()
        {            
            Log("Loaded");
            LoadData();
            SaveData();
        }

        // Loads the default configuration
        protected override void LoadDefaultConfig()
        {
            Log("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void LoadVariables()
        {
            Config["messages"] = defMsg;
            Config["version"] = Version;
        }
        
        


        // Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
                return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();
                var version = GetConfig<Dictionary<string, object>>("version", null);
                VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
                var cfgMessages = GetConfig<Dictionary<string, object>>("messages", null);
                if (cfgMessages != null)
                    foreach (var pair in cfgMessages)
                        APHelper[pair.Key] = Convert.ToString(pair.Value);

                if (verNum < Version)
                {
                    foreach (var pair in defMsg)
                        if (!APHelper.ContainsKey(pair.Key))
                            APHelper[pair.Key] = pair.Value;
                    Config["version"] = Version;
                    Config["messages"] = APHelper;
                    SaveConfig();
                    Warn("Config version updated to: " + Version.ToString() + " please check it");
                }
            }
            catch (Exception ex)
            {
                Error("OnServerInitialized failed: " + ex.Message);
            }
            
        }
        void LoadData()
        {
			try
			{			
				protData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, ProtectionStatus>>("AP_Data");
			}
			catch
			{
				protData = new Dictionary<string, ProtectionStatus>();
				Warn("Old data removed! ReEnable your AdminProtection");
			}
            antiSpam = new Dictionary<string, DateTime>();
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject<Dictionary<string, ProtectionStatus>>("AP_Data", protData);
            Log("Data Saved");
        }
		[ChatCommand("aplist")]
		void cmdAPList(BasePlayer player, string cmd, string[] args)
		{
			// Check if the player is an admin.
            if (player.net.connection.authLevel == 0) return;
			if (protData.Count > 0)
			{
				player.ChatMessage(APHelper["APListHeader"]);
				foreach (var item in protData)
				{
					string mode = "Normal";
					if (item.Value.Silent) mode = "Silent";
					else if (item.Value.NoMsgToPlayer) mode = "No Msg to Attacker";
					if (item.Value.Enabler == null)
						player.ChatMessage(string.Format(APHelper["APListAdmin"], item.Value.Name, item.Key, mode));
					else
						player.ChatMessage(string.Format(APHelper["APListByAdmin"], item.Value.Name, item.Key, mode, item.Value.Enabler));
				}
			}		
		}
        [ChatCommand("ap")]
        void cmdToggleAP(BasePlayer player, string cmd, string[] args)
        {            
            // Check if the player is an admin.
            if (player.net.connection.authLevel == 0) return;
            // Grab the player is Steam ID.
            string userID = player.userID.ToString();

            // Check if the player is turning Admin Protection on or off.
            if (protData != null)
            {
                bool silent = false;
                bool noMsg = false;
                if (args.Length >= 2)
                {
                    if (args[0] == "p")
                    {
                        string targetPlayer = args[1];
                        string mode = "";
                        if (args.Length > 2)
                            mode = args[2];
                        if (mode == "s") silent = true;
                        else if (mode == "m") noMsg = true;
                        List<BasePlayer> bpList = FindPlayerByName(targetPlayer);
                        if (bpList.Count > 1)
                        {
                            player.ChatMessage(APHelper["TooMuch"]);
                        }
                        else if (bpList.Count == 1)
                        {
                            string targetUID = bpList[0].userID.ToString();
                            if (protData.ContainsKey(targetUID))
                            {
                                protData.Remove(targetUID);
                                player.ChatMessage(string.Format(APHelper["DisabledTo"], bpList[0].displayName));
                            }
                            else
                            {
                                protData.Add(targetUID, new ProtectionStatus(true, silent, noMsg, bpList[0].displayName, player.displayName));
                                player.ChatMessage(string.Format(APHelper["EnabledTo"], bpList[0].displayName) + " " + mode);
                            }
                        }
                        else
                        {
                            player.ChatMessage(APHelper["Error"]);
                        }
                    }     
					if (args[0] == "id")
                    {
                        string targetPlayer = args[1];
                        string mode = "";
                        if (args.Length > 2)
                            mode = args[2];
                        if (mode == "s") silent = true;
                        else if (mode == "m") noMsg = true;
                        
						string targetUID = args[1];
						if (protData.ContainsKey(targetUID))
						{							
							player.ChatMessage(string.Format(APHelper["DisabledTo"], protData[targetUID].Name));
							protData.Remove(targetUID);
						}
						else
						{
							List<BasePlayer> bpList = FindPlayerByID(targetUID);
							if (bpList.Count > 1)
							{
								player.ChatMessage(APHelper["TooMuch"]);
							}
							else if (bpList.Count == 1)
							{
								protData.Add(targetUID, new ProtectionStatus(true, silent, noMsg, bpList[0].displayName, player.displayName));
								player.ChatMessage(string.Format(APHelper["EnabledTo"], bpList[0].displayName) + " " + mode);
							}
						}                       
                    }  
                }
                else
                {
                    if (protData.ContainsKey(userID))
                    {
                        ProtectionStatus protInfo = protData[userID];
                        if (protInfo.Enabled)
                        {
                             protData.Remove(userID);
                            player.ChatMessage(APHelper["Disabled"]);
                        }
                    }
                    else
                    {
                        if (args.Length > 0 && args.Length < 2)
                        {
                            if (args[0] == "s") silent = true;
                            else if (args[0] == "m") noMsg = true;
                        }
                        protData.Add(userID, new ProtectionStatus(true, silent, noMsg, player.displayName));
                        if (!silent && !noMsg)
                            player.ChatMessage(APHelper["Enabled"]);
                        else if (silent)
                            player.ChatMessage(APHelper["Enabled_s"]);
                        else
                            player.ChatMessage(APHelper["Enabled_m"]);
                    }
                }
            }
            SaveData();
        }
        private List<BasePlayer>FindPlayerByName( string playerName = "" )
        {
            // Check if a player name was supplied.
            if (playerName == "" ) return null;

            // Set the player name to lowercase to be able to search case insensitive.
            playerName = playerName.ToLower();

            // Setup some variables to save the matching BasePlayers with that partial
            // name.
            List<BasePlayer> matches = new List<BasePlayer>();            
    
            // Iterate through the online player list and check for a match.
            foreach (var player in BasePlayer.activePlayerList)
            {
                // Get the player his/her display name and set it to lowercase.
                string displayName = player.displayName.ToLower();
        
                // Look for a match.
                if (displayName.Contains(playerName))
                {
                    matches.Add(player);
                }                
            }

            // Return all the matching players.
            return matches;
        }
		private List<BasePlayer>FindPlayerByID( string playerID = "" )
        {
            // Check if a player name was supplied.
            if (playerID == "" || IsAllDigits(playerID)) return null;

            // Setup some variables to save the matching BasePlayers with that partial
            // name.
            List<BasePlayer> matches = new List<BasePlayer>();            
    
            // Iterate through the online player list and check for a match.
            foreach (var player in BasePlayer.activePlayerList)
            {
                // Get the player his/her display name and set it to lowercase.
                string onlineID = player.userID.ToString();
        
                // Look for a match.
                if (onlineID.Contains(playerID))
                {
                    matches.Add(player);
                }                
            }

            // Return all the matching players.
            return matches;
        }
		
		private bool IsAllDigits(string s)
		{
			foreach (char c in s)
			{
				if (!Char.IsDigit(c))
					return false;
			}
			return true;
		}
		
        [HookMethod("OnPlayerLoot")]
        void OnPlayerLoot(PlayerLoot lootInventory, UnityEngine.Object entry)
        {
            if (entry is BasePlayer)
            {
                BasePlayer looter = lootInventory.GetComponent("BasePlayer") as BasePlayer;
                BasePlayer target = entry as BasePlayer;
                string userID = target.userID.ToString();
                if (protData.ContainsKey(userID))
                {
                    looter.SendConsoleCommand("inventory.endloot");
                    looter.StartSleeping();
                    looter.UpdateNetworkGroup();
                    looter.SendFullSnapshot();
                    Log(string.Format(APHelper["LootMessageLog"], looter.displayName, target.displayName));
                    looter.ChatMessage(APHelper["LootAlert"]);
                }
            }
            
        } 
		
        [HookMethod("OnEntityAttacked")]
        private void OnEntityAttacked(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer)
            {
                var player = entity as BasePlayer;
                if (protData.ContainsKey(player.userID.ToString()))
                {
                    ProtectionStatus protInfo = protData[player.userID.ToString()] as ProtectionStatus;
                    if (protInfo.Enabled)
                    {
                        while (hitInfo.damageTypes.Total() > 0)
                        {
                            hitInfo.damageTypes.Set(hitInfo.damageTypes.GetMajorityDamageType(), 0);
                        }
                        hitInfo.HitMaterial = 0;
                        if (hitInfo.Initiator is BasePlayer && !protInfo.Silent && hitInfo.Initiator != player) // 
                        {
                            var attacker = hitInfo.Initiator as BasePlayer;
                            string attackerID = attacker.userID.ToString();
                            if (antiSpam.ContainsKey(attackerID))
                            {
                                if ((DateTime.Now - antiSpam[attackerID]).TotalSeconds > 30)
                                {
                                    if (!protInfo.NoMsgToPlayer)
                                        attacker.ChatMessage(string.Format(APHelper["NoAPDamageAttacker"], player.displayName));
                                    player.ChatMessage(string.Format(APHelper["NoAPDamagePlayer"], attacker.displayName));
                                    antiSpam[attackerID] = DateTime.Now;
                                }
                            }
                            else
                            {
                                antiSpam.Add(attackerID, DateTime.Now);
                                if (!protInfo.NoMsgToPlayer)
                                    attacker.ChatMessage(string.Format(APHelper["NoAPDamageAttacker"], player.displayName));
                                player.ChatMessage(string.Format(APHelper["NoAPDamagePlayer"], attacker.displayName));
                            }
                        }

                    }
                }
            }
        }

        void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            // Grab the Steam ID of the player.
            BasePlayer player = metabolism.GetComponent("BasePlayer") as BasePlayer;
            string userID = player.userID.ToString();
            // Check if the player has Godmode enable.
            if (protData != null)
            {
                if (protData.ContainsKey(userID))
                {
                    ProtectionStatus protInfo = protData[userID];
                    if (protInfo.Enabled)
                    {
                        // The player has Godmode enabled, change the metabolism values.
                        player.InitializeHealth(100, 100);
                        metabolism.oxygen.Add(metabolism.oxygen.max);
                        metabolism.wetness.Add(-metabolism.wetness.max);
                        metabolism.radiation_level.Add(-metabolism.radiation_level.max);
                        metabolism.radiation_poison.Add(-metabolism.radiation_poison.max);
                        metabolism.temperature.Reset();
                        metabolism.hydration.Add(metabolism.hydration.max);
                        metabolism.calories.Add(metabolism.calories.max);
                        metabolism.bleeding.Reset();
                    }
                }
            }
        }

        void SendHelpText(BasePlayer player)
        {
            if (player.net.connection.authLevel > 0) {
                player.SendMessage(APHelper["HelpMessage"]);
            }
        }        
        public class ProtectionStatus
        {
			public string Name = null;
            public bool Enabled = false;
            public bool NoMsgToPlayer = false;
            public bool Silent = false;
			public string Enabler = null;

            public ProtectionStatus(bool En, bool Sil, bool noMsg, string name, string admName = null)
            {
                Enabled = En;
                Silent = Sil;
                NoMsgToPlayer = noMsg;
				Name = name;
				Enabler = admName != null ? admName : null;
            }
        }
	}
	
}