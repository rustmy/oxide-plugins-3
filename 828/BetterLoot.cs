// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: UnityEngine

using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("BetterLoot", "playrust.io / dcode", "1.7.1", ResourceId = 828)]
    public class BetterLoot : RustPlugin
    {

        // To configure, use config/BetterLoot.json instead
        #region Configuration

        private double        defaultBlueprintProbability = 0.11;
        private int           defaultMinItemsPerBarrel    = 1;
        private int           defaultMaxItemsPerBarrel    = 3;
        private int           defaultMinItemsPerCrate     = 3;
        private int           defaultMaxItemsPerCrate     = 6;
        private double        defaultBaseItemRarity       = 2;
        private double        defaultBaseBlueprintRarity  = 2.5;
        private int           defaultRefreshMinutes       = 15;
        private bool          defaultEnforceBlacklist     = false;
        private bool          defaultDropWeaponsWithAmmo  = true;
        private Dictionary<string, int> defaultDropLimits = new Dictionary<string, int>() {
            // C4
            { "explosives", 50 },
            { "explosive.timed", 2 },
            // Ammunition
            { "arrow_wooden", 16 },
            { "ammo_handmade_shell", 32 },
            { "ammo_shotgun", 32 },
            { "ammo_pistol", 32 },
            { "ammo_rifle", 32 },
            { "gunpowder", 200 },
            // From animals
            { "fat_animal", 200 },
            { "cloth", 200 },
            { "lowgradefuel", 200 },
            { "bone_fragments", 200 },
            // Medical
            { "antiradpills", 5 },
            { "bandage", 5 },
            { "largemedkit", 2 },
            { "blood", 200 },
            // Resources
            { "wood", 1000 },
            { "stones", 1000 },
            { "metal_ore", 1000 },
            { "sulfur_ore", 1000 },
            { "metal_fragments", 1000 },
            { "sulfur", 1000 },
            // Food
            { "apple", 10 },
            { "blueberries", 10 },
            { "black raspberries", 10 },
            { "wolfmeat_cooked", 5 },
            { "chicken_cooked", 5 },
            { "can_beans", 5 },
            { "can_tuna", 5 },
            { "granolabar", 5 },
            { "chocholate", 5 },
            { "smallwaterbottle", 5}
        };

        private double       blueprintProbability;
        private int          minItemsPerBarrel;
        private int          maxItemsPerBarrel;
        private int          minItemsPerCrate;
        private int          maxItemsPerCrate;
        private double       baseItemRarity;
        private double       baseBlueprintRarity;
        private int          refreshMinutes;
        private List<string> itemBlacklist;
        private List<string> blueprintBlacklist;
        private bool         enforceBlacklist;
        private bool         dropWeaponsWithAmmo;
        private Dictionary<string, int> dropLimits;

        #endregion

        // Stuff that will never drop as a blueprint (default craftable)
        private List<string> defaultBlueprints = new List<string>() {
            // Building
            "lock.key", "cupboard.tool", "building_planner", "door_key",
            // Items
            "campfire", "box_wooden", "sleepingbag", "furnace",
            // Resources
            "paper", "lowgradefuel", "gunpowder",
            // Attire
            "burlap_shirt", "burlap_shoes", "burlap_trousers",
            "attire.hide.boots", "attire.hide.pants", "attire.hide.poncho", "attire.hide.vest",
            "urban_pants", "urban_boots",
            // Tools
            "hammer", "torch", "stonehatchet", "stone_pickaxe", "box_repair_bench",
            // Medical
            "bandage",
            // Weapons
            "spear_wooden", "knife_bone", "bow_hunting", "pistol_eoka",
            // Ammunition
            "arrow_wooden", "ammo_handmade_shell"
            // Traps
        };

        // Stuff that will simply never drop
        private List<string> doesNotDrop = new List<string>() {
            // Food
            "apple_spoiled", "bearmeat", "chicken_burned", "chicken_spoiled", "wolfmeat_burned", "wolfmeat_spoiled",
            // Resources
            "blood", "battery_small", "paper", "skull_human", "skull_wolf"
        };

        // Ammunition used by the differnt kinds of weapons
        private Dictionary<string, string> weaponAmmunition = new Dictionary<string, string>() {
            { "bow_hunting", "arrow_wooden" },
            { "pistol_eoka", "ammo_handmade_shell" },
            { "pistol_revolver", "ammo_pistol" },
            { "shotgun_waterpipe", "ammo_shotgun" },
            { "shotgun_pump", "ammo_shotgun" },
            { "smg_thompson", "ammo_pistol" },
            { "rifle_bolt", "ammo_rifle" },
            { "rifle_ak", "ammo_rifle" }
        };

        // Regular expressions defining what to override
        private Regex barrelEx = new Regex("loot_barrel|loot_trash");
        private Regex crateEx = new Regex("crate");
        private Regex overrideLootOf = new Regex("(loot_barrel|loot_trash|crate)");

        // Items and blueprints data
        private List<string>[] items = new List<string>[4];
        private int totalItems;
        private List<string>[] blueprints = new List<string>[4];
        private int totalBlueprints;
        private int[] itemWeights = new int[4];
        private int[] blueprintWeights = new int[4];
        private int totalItemWeight;
        private int totalBlueprintWeight;

        // What the game says
        private List<ItemDefinition> originalItems;
        private List<ItemBlueprint> originalBlueprints;

        // Underlying random number generator
        private Random rng = new Random();

        // Whether the plugin has been correctly initialized
        private bool initialized = false;

        // Number of ticks until an internals update is scheduled
        private int updateScheduled = -1;

        // List of containers to refresh periodically
        private List<ContainerToRefresh> refreshList = new List<ContainerToRefresh>();

        // Last time containers have been refreshed
        private DateTime lastRefresh = DateTime.MinValue;

        // Loads the default configuration parameters into the config object
        protected override void LoadDefaultConfig() {
            Config["blueprintProbability"] = defaultBlueprintProbability;
            Config["minItemsPerBarrel"]    = defaultMinItemsPerBarrel;
            Config["maxItemsPerBarrel"]    = defaultMaxItemsPerBarrel;
            Config["minItemsPerCrate"]     = defaultMinItemsPerCrate;
            Config["maxItemsPerCrate"]     = defaultMaxItemsPerCrate;
            Config["baseItemRarity"]       = defaultBaseItemRarity;
            Config["baseBlueprintRarity"]  = defaultBaseBlueprintRarity;
            Config["refreshMinutes"]       = defaultRefreshMinutes;
            Config["itemBlacklist"]        = new List<string>();
            Config["blueprintBlacklist"]   = new List<string>();
            Config["enforceBlacklist"]     = defaultEnforceBlacklist;
            Config["dropWeaponsWithAmmo"]  = defaultDropWeaponsWithAmmo;
            Config["dropLimits"]           = defaultDropLimits;
        }

        // Gets a configuration value of a specific type
        T GetConfig<T>(string key, T defaultValue) {
            try {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>) {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String)) {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    } else if (t == typeof(int)) {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                } else if (val is Dictionary<string, object>) {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int)) {
                        var cval = new Dictionary<string,int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            } catch (Exception ex) {
                Warn("Invalid config value: " + key+" ("+ex.Message+")");
                return defaultValue;
            }
        }

        // Updates the internal probability matrix and optionally logs the result
        private void UpdateInternals(bool doLog) {
            Log("Updating internals ...");
            originalItems = UnityEngine.Resources.LoadAll<ItemDefinition>("items/").ToList<ItemDefinition>();
            originalBlueprints = UnityEngine.Resources.LoadAll<ItemBlueprint>("items/").ToList<ItemBlueprint>();
            if (originalItems.Count < 20 || originalBlueprints.Count < 10) {
                Error("Resources did not contain a sane amount of items and/or blueprints. Is the game broken?");
                return;
            }
            if (doLog)
                Log("There are " + originalItems.Count + " items and " + originalBlueprints.Count + " blueprints in the game.");

            for (var i = 0; i < 4; ++i) {
                items[i] = new List<string>();
                blueprints[i] = new List<string>();
            }
            totalItems = 0;
            totalBlueprints = 0;
            var allItems = ItemManager.GetItemDefinitions();
            if (allItems == null || allItems.Count < 20) {
                Error("ItemManager did not return a sane amount of items. Is the game broken?");
                return;
            }
            var notExistingItems = 0;
            var notExistingBlueprints = 0;
            var itemsWithNoRarity = 0;
            foreach (var item in allItems) {
                if (doesNotDrop.Contains(item.shortname))
                    continue;
                int index = RarityIndex(item.rarity);
                if (index >= 0) {
                    if (ItemExists(item.shortname)) {
                        if (!itemBlacklist.Contains(item.shortname)) {
                            items[index].Add(item.shortname);
                            ++totalItems;
                        }
                    } else ++notExistingItems;
                    if (BlueprintExists(item.shortname)) {
                        if (!blueprintBlacklist.Contains(item.shortname)) {
                            blueprints[index].Add(item.shortname);
                            ++totalBlueprints;
                        }
                    } else ++notExistingBlueprints;
                } else ++itemsWithNoRarity;
            }
            if (totalItems < 20 || totalBlueprints < 10) {
                Error("Failed to categorize items: "+notExistingItems+" items and "+notExistingBlueprints+" blueprints did not exist and "+itemsWithNoRarity+" items had no rarity");
                if (itemsWithNoRarity > 10)
                    Error("THIS IS MOST LIKELY CAUSED BY A MISCONFIGURED (OR BROKEN) PLUGIN THAT MODIFIES ITEMS!");
                else
                    Error("PLEASE REPORT THIS ON THE DEDICATED DISCUSSION THREAD! http://oxidemod.org/threads/betterloot.7063");
                return;
            }
            if (doLog)
                Log("We are going to use " + totalItems + " items and " + totalBlueprints + " blueprints of them.");

            totalItemWeight = 0;
            totalBlueprintWeight = 0;
            for (var i = 0; i < 4; ++i) {
                totalItemWeight += (itemWeights[i] = ItemWeight(baseItemRarity, i) * items[i].Count);
                totalBlueprintWeight += (blueprintWeights[i] = ItemWeight(baseBlueprintRarity, i) * blueprints[i].Count);
            }

            if (doLog) {
                Log(string.Format("Base item rarity is {0} and base blueprint rarity is {1}.", baseItemRarity, baseBlueprintRarity));
                Log(string.Format("With a {0:0.0}% chance that any drop is a blueprint we get:", 100 * blueprintProbability));
                double total = 0;
                for (var i = 0; i < 4; ++i) {
                    double prob = (1 - blueprintProbability) * 100d * itemWeights[i] / totalItemWeight;
                    Log(string.Format("There is a {0:0.000}% chance to get one of {1} " + RarityName(i) + " items (w={2}, {3}/{4}).", prob, items[i].Count, ItemWeight(baseItemRarity, i), itemWeights[i], totalItemWeight));
                    total += prob;
                }
                for (var i = 0; i < 4; ++i) {
                    double prob = blueprintProbability * 100d * blueprintWeights[i] / totalBlueprintWeight;
                    Log(string.Format("There is a {0:0.000}% chance to get one of {1} " + RarityName(i) + " blueprints (w={2}, {3}/{4}).", prob, blueprints[i].Count, ItemWeight(baseBlueprintRarity, i), blueprintWeights[i], totalBlueprintWeight));
                    total += prob;
                }
                // Log("Total chance: " + total + "% == 100%");
            }
            // Update containers accordingly
            var containers = UnityEngine.Object.FindObjectsOfType<LootContainer>();
            foreach (var container in containers) {
                try {
                    PopulateContainer(container);
                } catch (Exception ex) {
                    Warn("Failed to populate container " + ContainerName(container) + ": " + ex.Message + "\n" + ex.StackTrace);
                }
            }
            initialized = true;
            Log("Internals have been updated");
        }

        // Initializes our custom loot tables
        [HookMethod("OnServerInitialized")]
        void OnServerInitialized() {
            if (initialized)
                return;
            try {
                LoadConfig();

                blueprintProbability = GetConfig<double>("blueprintProbability", defaultBlueprintProbability);
                minItemsPerBarrel = GetConfig<int>("minItemsPerBarrel", defaultMinItemsPerBarrel);
                maxItemsPerBarrel = GetConfig<int>("maxItemsPerBarrel", defaultMaxItemsPerBarrel);
                minItemsPerCrate = GetConfig<int>("minItemsPerCrate", defaultMinItemsPerCrate);
                maxItemsPerCrate = GetConfig<int>("maxItemsPerCrate", defaultMaxItemsPerCrate);
                baseItemRarity = GetConfig<double>("baseItemRarity", defaultBaseItemRarity);
                baseBlueprintRarity = GetConfig<double>("baseBlueprintRarity", defaultBaseBlueprintRarity);
                refreshMinutes = GetConfig<int>("refreshMinutes", defaultRefreshMinutes);
                itemBlacklist = GetConfig<List<string>>("itemBlacklist", new List<string>()); /* ref */ Config["itemBlacklist"] = itemBlacklist;
                blueprintBlacklist = GetConfig<List<string>>("blueprintBlacklist", new List<string>()); /* ref */ Config["blueprintBlacklist"] = blueprintBlacklist;
                enforceBlacklist = GetConfig<bool>("enforceBlacklist", defaultEnforceBlacklist);
                dropWeaponsWithAmmo = GetConfig<bool>("dropWeaponsWithAmmo", defaultDropWeaponsWithAmmo);
                dropLimits = GetConfig<Dictionary<string, int>>("dropLimits", defaultDropLimits); /* ref */ Config["dropLimits"] = dropLimits;

                updateScheduled = 3;
                Log("Updating in T-" + updateScheduled + " ...");
                // ^ Wait a couple of ticks to give plugins that modify items a chance to do their thing prior to calculating loot tables.
            } catch (Exception ex) {
                Error("OnServerInitialized failed: " + ex.Message);
            }
        }

        // Asks the mighty RNG for an item
        private Item MightyRNG() {
            bool blueprint = rng.NextDouble() < blueprintProbability;
            List<string> selectFrom;
            int limit = 0;
            string itemName;
            Item item;
            int maxRetry = 20;
            do {
                selectFrom = null;
                item = null;
                if (blueprint) {
                    var r = rng.Next(totalBlueprintWeight);
                    for (var i=0; i<4; ++i) {
                        limit += blueprintWeights[i];
                        if (r < limit) {
                            selectFrom = blueprints[i];
                            break;
                        }
                    }
                } else {
                    var r = rng.Next(totalItemWeight);
                    for (var i=0; i<4; ++i) {
                        limit += itemWeights[i];
                        if (r < limit) {
                            selectFrom = items[i];
                            break;
                        }
                    }
                }
                if (selectFrom == null) {
                    if (--maxRetry <= 0) {
                        Error("Endless loop detected: ABORTING");
                        break;
                    }
                    Warn("Item list to select from is empty (trying another one)");
                    continue;
                }
                itemName = selectFrom[rng.Next(0, selectFrom.Count)];
                item = ItemManager.CreateByName(itemName, 1);
                if (item == null) {
                    Warn("Failed to create item: " + itemName + " (trying another one)");
                    continue;
                }
                if (item.info == null) {
                    Warn("Item has no definition: " + itemName+" (trying another one)");
                    continue;
                }
                break;
            } while (true);
            if (item == null)
                return null;
            if (blueprint) {
                item.isBlueprint = true;
            } else if (item.info.stackable > 1 && dropLimits.TryGetValue(item.info.shortname, out limit)) {
                item.amount = rng.Next(1, Math.Min(limit, item.info.stackable) + 1);
            }
            return item;
        }

        // Clears a loot container's contents
        private void ClearContainer(LootContainer container) {
            while (container.inventory.itemList.Count > 0) {
                var item = container.inventory.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }

        // Suppresses automatic refreshes of a container
        private void SuppressRefresh(LootContainer container) {
            container.minSecondsBetweenRefresh = -1;
            container.maxSecondsBetweenRefresh = 0;
            container.CancelInvoke("SpawnLoot");
        }

        // Populates a container with loot
        private void PopulateContainer(LootContainer container) {
            if (container.inventory == null) {
                // Log("Creating inventory for " + ContainerName(container) + " with " + container.inventorySlots + " slots");
                container.inventory = new ItemContainer();
                container.inventory.capacity = container.inventorySlots;
                container.inventory.itemList = new List<Item>();
            }
            int min = 1;
            int max = 0;
            bool refresh = false;
            if (container is SupplyDrop) {
                SuppressRefresh(container);
                var inv = container.inventory.itemList.ToArray();
                foreach (var item in inv) {
                    if (itemBlacklist.Contains(item.info.shortname)) {
                        item.RemoveFromContainer();
                        item.Remove(0f);
                        ++max;
                    }
                }
                if (max == 0)
                    return;
            } else if (barrelEx.IsMatch(container.gameObject.name)) {
                SuppressRefresh(container);
                ClearContainer(container);
                min = minItemsPerBarrel;
                max = maxItemsPerBarrel;
            } else if (crateEx.IsMatch(container.gameObject.name)) {
                SuppressRefresh(container);
                ClearContainer(container);
                min = minItemsPerCrate;
                max = maxItemsPerCrate;
                refresh = true; // In case someone puts trash in it
            } else
                return;

            var n = min + rng.Next(0, max - min + 1);
            var sb = new StringBuilder();
            var items = new List<Item>();
            bool hasAmmo = false;
            for (int i = 0; i < n; ++i) {
                var item = MightyRNG();
                if (item == null) {
                    Error("Failed to obtain item: Is the plugin initialized yet?");
                    return;
                }
                items.Add(item);
                if (sb.Length > 0)
                    sb.Append(", ");
                if (item.amount > 1)
                    sb.Append(item.amount).Append("x ");
                sb.Append(item.info.shortname);
                if (item.isBlueprint)
                    sb.Append(" (BP)");
                else if (dropWeaponsWithAmmo && !hasAmmo && items.Count < container.inventorySlots) { // Drop some ammunition with first weapon
                    string ammo;
                    int limit;
                    if (weaponAmmunition.TryGetValue(item.info.shortname, out ammo) && dropLimits.TryGetValue(ammo, out limit)) {
                        try {
                            item = ItemManager.CreateByName(ammo, rng.Next(2, limit + 1));
                            items.Add(item);
                            sb.Append(" + ");
                            if (item.amount > 1)
                                sb.Append(item.amount).Append("x ");
                            sb.Append(item.info.shortname);
                            hasAmmo = true;
                        } catch (Exception) {
                            Warn("Failed to obtain ammo item: "+ammo);
                        }
                    }
                }
            }
            // Log("Populating " + ContainerName(container) + " with " + sb.ToString());
            foreach (var item in items)
                item.MoveToContainer(container.inventory, -1, false);
            container.inventory.MarkDirty();
            if (refresh)
                refreshList.Add(new ContainerToRefresh() { container = container, time = DateTime.UtcNow.AddMinutes(refreshMinutes) });
        }

        [HookMethod("OnEntitySpawn")]
        private void OnEntitySpawn(BaseNetworkable entity) {
            if (!initialized)
                return;
            try {
                var container = entity as LootContainer;
                if (container == null || container.inventory == null || container.inventory.itemList == null)
                    return;
                PopulateContainer(container);
            } catch (Exception ex) {
                Error("OnEntitySpawn failed: " + ex.Message);
            }
        }

        [ChatCommand("loot")]
        private void cmdChatLoot(BasePlayer player, string command, string[] args) {
            if (!initialized)
                return;
            var sb = new StringBuilder();
            sb.Append("<size=22>BetterLoot</size> by <color=#ce422b>http://playrust.io</color>\n");
            sb.Append(string.Format("A barrel drops up to {0} items, a chest up to {1} items.\n", maxItemsPerBarrel, maxItemsPerCrate));
            sb.Append(string.Format("Base item rarity is {0:0.00} and base blueprint rarity is {1:0.00}.\n", baseItemRarity, baseBlueprintRarity));
            sb.Append(string.Format("There is a <color=#aef45b>{0:0.0}%</color> chance that any drop is a blueprint.\n", 100 * blueprintProbability));
            for (var i = 0; i < 4; ++i) {
                double prob = (1 - blueprintProbability) * 100d * itemWeights[i] / totalItemWeight;
                sb.Append(string.Format("There is a <color=#f4e75b>{0:0.000}%</color> chance to get one of {1} " + RarityName(i) + " items.\n", prob, items[i].Count));
            }
            for (var i = 0; i < 4; ++i) {
                double prob = blueprintProbability * 100d * blueprintWeights[i] / totalBlueprintWeight;
                sb.Append(string.Format("There is a <color=#5bbcf4>{0:0.000}%</color> chance to get one of {1} " + RarityName(i) + " blueprints.\n", prob, blueprints[i].Count));
            }
            SendReply(player, sb.ToString());
        }

        [ChatCommand("droplimit")]
        private void cmdChatDroplimit(BasePlayer player, string command, string[] args) {
            var usage = "Usage: /droplimit \"ITEMNAME\" [LIMIT]";
            if (!initialized)
                return;
            if (!ServerUsers.Is(player.userID, ServerUsers.UserGroup.Owner)) {
                SendReply(player, "You are not authorized to modify drop limits");
                return;
            }
            if (args.Length < 1) {
                SendReply(player, usage);
                return;
            }
            string name = args[0];
            int currentLimit;
            if (!dropLimits.TryGetValue(name, out currentLimit)) {
                SendReply(player, "No such item: "+name);
                return;
            }
            if (args.Length == 1) {
                SendReply(player, "Drop limit of '" + name + "' is " + currentLimit);
                return;
            }
            int limit = Convert.ToInt32(args[1]);
            dropLimits[name] = limit;
            SaveConfig();
            SendReply(player, "Drop limit of '" + name + "' has been changed from "+currentLimit+" to " + limit);
        }

        [ChatCommand("blacklist")]
        private void cmdChatBlacklist(BasePlayer player, string command, string[] args) {
            var usage = "Usage: /blacklist [additem|deleteitem|addbp|deletebp] \"ITEMNAME\"";
            if (!initialized)
                return;
            if (args.Length == 0) {
                if (itemBlacklist.Count == 0) {
                    SendReply(player, "There are no blacklisted items");
                } else {
                    var sb = new StringBuilder();
                    foreach (var item in itemBlacklist) {
                        if (sb.Length > 0)
                            sb.Append(", ");
                        sb.Append(item);
                    }
                    SendReply(player, "Blacklisted items: {0}", sb.ToString());
                }
                if (blueprintBlacklist.Count == 0) {
                    SendReply(player, "There are no blacklisted blueprints");
                } else {
                    var sb = new StringBuilder();
                    foreach (var item in blueprintBlacklist) {
                        if (sb.Length > 0)
                            sb.Append(", ");
                        sb.Append(item);
                    }
                    SendReply(player, "Blacklisted blueprints: {0}", sb.ToString());
                }
                return;
            }
            if (!ServerUsers.Is(player.userID, ServerUsers.UserGroup.Owner)) {
                SendReply(player, "You are not authorized to modify the blacklist");
                return;
            }
            if (args.Length != 2) {
                SendReply(player, usage);
                return;
            }
            if (args[0] == "additem") {
                if (!ItemExists(args[1])) {
                    SendReply(player, "Not a valid item: {0}", args[1]);
                    return;
                }
                if (!itemBlacklist.Contains(args[1])) {
                    itemBlacklist.Add(args[1]);
                    UpdateInternals(false);
                    SendReply(player, "The item '{0}' is now blacklisted", args[1]);
                    SaveConfig();
                    return;
                } else {
                    SendReply(player, "The item '{0}' is already blacklisted", args[1]);
                    return;
                }
            } else if (args[0] == "addbp") {
                if (!BlueprintExists(args[1])) {
                    SendReply(player, "Not a valid blueprint: {0}", args[1]);
                    return;
                }
                if (!blueprintBlacklist.Contains(args[1])) {
                    blueprintBlacklist.Add(args[1]);
                    UpdateInternals(false);
                    SendReply(player, "The blueprint '{0}' is now blacklisted", args[1]);
                    SaveConfig();
                    return;
                } else {
                    SendReply(player, "The blueprint '{0}' is already blacklisted", args[1]);
                    return;
                }
            } else if (args[0] == "deleteitem") {
                if (!ItemExists(args[1])) {
                    SendReply(player, "Not a valid item: {0}", args[1]);
                    return;
                }
                if (itemBlacklist.Contains(args[1])) {
                    itemBlacklist.Remove(args[1]);
                    UpdateInternals(false);
                    SendReply(player, "The item '{0}' is now no longer blacklisted", args[1]);
                    SaveConfig();
                    return;
                } else {
                    SendReply(player, "The item '{0}' is not blacklisted", args[1]);
                    return;
                }
            } else if (args[0] == "deletebp") {
                if (!BlueprintExists(args[1])) {
                    SendReply(player, "Not a valid blueprint: {0}", args[1]);
                    return;
                }
                if (blueprintBlacklist.Contains(args[1])) {
                    blueprintBlacklist.Remove(args[1]);
                    UpdateInternals(false);
                    SendReply(player, "The blueprint '{0}' is now no longer blacklisted", args[1]);
                    SaveConfig();
                    return;
                } else {
                    SendReply(player, "The blueprint '{0}' is not blacklisted", args[1]);
                    return;
                }
            } else {
                SendReply(player, usage);
                return;
            }
        }

        [HookMethod("OnItemAddedToContainer")]
        private void OnItemAddedToContainer(ItemContainer container, Item item) {
            if (!initialized || !enforceBlacklist)
                return;
            try {
                var owner = item.GetOwnerPlayer();
                if (owner != null && (ServerUsers.Is(owner.userID, ServerUsers.UserGroup.Owner) || ServerUsers.Is(owner.userID, ServerUsers.UserGroup.Moderator)))
                    return;
                if (!item.isBlueprint && itemBlacklist.Contains(item.info.shortname)) {
                    item.RemoveFromContainer();
                    item.Remove(0f);
                    Log(string.Format("An item instance of '{0}' has been destroyed", item.info.shortname));
                } else if (item.isBlueprint && blueprintBlacklist.Contains(item.info.shortname)) {
                    item.RemoveFromContainer();
                    item.Remove(0f);
                    Log(string.Format("A blueprint instance of '{0}' has been destroyed", item.info.shortname));
                }
            } catch (Exception ex) {
                Error("OnItemAddedToContainer failed: " + ex.Message);
            }
        }

        [HookMethod("OnTick")]
        private void OnTick() {
            try {
                if (updateScheduled == 0) {
                    updateScheduled = -1;
                    UpdateInternals(true);
                } else if (updateScheduled > 0) {
                    --updateScheduled;
                }
            } catch (Exception ex) {
                Error("OnTick scheduled update failed: " + ex.Message);
            }
            try {
                var now = DateTime.UtcNow;
                if (lastRefresh < now.AddMinutes(-1)) {
                    lastRefresh = now;
                    int n = 0;
                    int m = 0;
                    while (refreshList.Count > 0) {
                        var ctr = refreshList[0];
                        if (ctr.time < now) {
                            if (ctr.container.IsOpen())
                                continue; // Do not refresh while occupied
                            refreshList.RemoveAt(0);
                            if (!ctr.container.isDestroyed) {
                                try {
                                    PopulateContainer(ctr.container);
                                    ++n;
                                } catch (Exception ex) {
                                    Error("Failed to refresh container: " + ContainerName(ctr.container) + ": " + ex.Message + "\n" + ex.StackTrace);
                                }
                            } else ++m;
                        } else
                            break;
                    }
                    if (n > 0 || m > 0)
                        Log("Refreshed " + n + " containers (" + m + " destroyed)");
                }
            } catch (Exception ex) {
                Error("OnTick scheduled refresh failed: " + ex.Message);
            }
        }

        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist) {
            try {
                taglist.Add("betterloot");
            } catch (Exception ex) {
                Error("BuildServerTags failed: " + ex.Message);
            }
        }

        #region Utility Methods

        private void Log(string message) {
            Puts("{0}: {1}", Title, message);
        }

        private void Warn(string message) {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message) {
            PrintError("{0}: {1}", Title, message);
        }

        private static string ContainerName(LootContainer container) {
            var name = container.gameObject.name;
            name = name.Substring(name.LastIndexOf("/") + 1);
            name += "#" + container.gameObject.GetInstanceID();
            return name;
        }


        private static int RarityIndex(Rarity rarity) {
            switch (rarity) {
                case Rarity.Common: return 0;
                case Rarity.Uncommon: return 1;
                case Rarity.Rare: return 2;
                case Rarity.VeryRare: return 3;
            }
            return -1;
        }

        private static string RarityName(int index) {
            switch (index) {
                case 0: return "common";
                case 1: return "uncommon";
                case 2: return "rare";
                case 3: return "very rare";
            }
            return null;
        }

        private bool BlueprintExists(string name) {
            if (defaultBlueprints.Contains(name))
                return false;
            foreach (var def in originalBlueprints) {
                if (def.targetItem.shortname != name)
                    continue;
                var testItem = ItemManager.CreateByName(name, 1);
                if (testItem != null) {
                    testItem.Remove(0f);
                    return true;
                }
            }
            return false;
        }

        private bool ItemExists(string name) {
            foreach (var def in originalItems) {
                if (def.shortname != name)
                    continue;
                var testItem = ItemManager.CreateByName(name, 1);
                if (testItem != null) {
                    testItem.Remove(0f);
                    return true;
                }
            }
            return false;
        }

        private bool IsWeapon(string name) {
            return weaponAmmunition.ContainsKey(name);
        }

        private int ItemWeight(double baseRarity, int index) {
            return (int)(Math.Pow(baseRarity, 3 - index) * 1000); // Round to 3 decimals
        }

        #endregion

        private class ContainerToRefresh {
            public LootContainer container;
            public DateTime time;
        }
    }
}
