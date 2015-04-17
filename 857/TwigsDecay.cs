// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: UnityEngine

using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using Rust;

namespace Oxide.Plugins
{
    [Info("TwigsDecay", "playrust.io / dcode", "1.3.0", ResourceId = 857)]
    public class TwigsDecay : RustPlugin
    {
        private Dictionary<string, int> damage = new Dictionary<string, int>();
        private int timespan;
        private DateTime lastUpdate = DateTime.Now;
        private List<string> blocks = new List<string>();
        private bool initialized = false;

        // A list of all translateable texts
        private List<string> texts = new List<string>() {
            "Twigs",
            "Wood",
            "Stone",
            "Metal",
            "TopTier",
            "%GRADE% buildings decay by %DAMAGE% HP per %TIMESPAN% minutes.",
            "%GRADE% buildings do not decay."
        };
        private Dictionary<string, string> messages = new Dictionary<string, string>();

        protected override void LoadDefaultConfig() {
            var damage = new Dictionary<string, object>() {
                {"Twigs", 1},
                {"Wood", 0},
                {"Stone", 0},
                {"Metal", 0},
                {"TopTier", 0}
            };
            Config["damage"] = damage;
            Config["timespan"] = 288;
            var blocks = new List<object>() {
                "block.halfheight",
                /* stairs */ "block.halfheight.slanted",
                "floor",
                "floor.triangle",
                "foundation",
                "foundation.steps",
                "foundation.triangle",
                // "pillar",
                "roof",
                "wall",
                "wall.doorway",
                // "door.hinged",
                "wall.low",
                "wall.window",
                "wall.window.bars"
            };
            Config["blocks"] = blocks;
            var messages = new Dictionary<string, object>();
            foreach (var text in texts) {
                if (messages.ContainsKey(text))
                    Puts("{0}: {1}", Title, "Duplicate translation string: " + text);
                else
                    messages.Add(text, text);
            }
            Config["messages"] = messages;
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized() {
            LoadConfig();
            try {
                var damageConfig = (Dictionary<string, object>)Config["damage"];
                int val;
                foreach (var cfg in damageConfig)
                    damage.Add(cfg.Key, (val = Convert.ToInt32(cfg.Value)) >= 0 ? val : 0);
                timespan = Convert.ToInt32(Config["timespan"]);
                if (timespan < 0)
                    timespan = 15;
                var blocksConfig = (List<object>)Config["blocks"];
                foreach (var cfg in blocksConfig)
                    blocks.Add(Convert.ToString(cfg));
                initialized = true;
                var customMessages = (Dictionary<string, object>)Config["messages"];
                if (customMessages != null)
                    foreach (var pair in customMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);
                Puts("{0}: {1}", Title, "Initialized");
            } catch (Exception ex) {
                PrintError("{0}: {1}", Title, "Failed to load configuration file: " + ex.Message);
            }
        }

        [HookMethod("OnTick")]
        private void OnTick() {
            if (!initialized)
                return;
            var now = DateTime.Now;
            if (lastUpdate > now.AddMinutes(-timespan))
                return;
            lastUpdate = now;
            int n = 0;
            int m = 0;
            var allBlocks = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
            foreach (var block in allBlocks) {
                string grade;
                string name;
                try {
                    grade = block.grade.ToString();
                    name = block.blockDefinition.fullName.Substring(6); // "build/foundation"
                } catch (Exception) {
                    continue;
                }
                if (!blocks.Contains(name))
                    continue;
                int amount;
                if (damage.TryGetValue(grade, out amount) && amount > 0) {
                    if (block.health <= amount) {
                        block.Kill(BaseNetworkable.DestroyMode.Gib);
                        ++n;
                    } else {
                        block.health -= amount;
                        ++m;
                    }
                }
            }
            Puts("{0}: {1}", Title, "Decayed " + m + " blocks, destroyed " + n + " blocks");
        }

        // Disable default decay
        [HookMethod("OnBaseCombatEntityHurt")]
        private object OnBaseCombatEntityHurt(BaseCombatEntity entity, HitInfo info) {
            if (!(entity is BuildingBlock))
                return null;
            if (!info.damageTypes.Has(DamageType.Decay))
                return null;
            info.damageTypes.types = new float[0];
            return false;
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player) {
            var sb = new StringBuilder()
               .Append("<size=18>TwigsDecay</size> by <color=#ce422b>http://playrust.io</color>\n");
            foreach (var dmg in damage) {
                if (dmg.Value > 0)
                    sb.Append("  ").Append(_("%GRADE% buildings decay by %DAMAGE% HP per %TIMESPAN% minutes.", new Dictionary<string, string> {
                        { "GRADE", _(dmg.Key) },
                        { "DAMAGE", dmg.Value.ToString() },
                        { "TIMESPAN", timespan.ToString() }
                    })).Append("\n");
                else
                    sb.Append("  ").Append(_("%GRADE% buildings do not decay.", new Dictionary<string, string>() {
                        { "GRADE", _(dmg.Key) }
                    })).Append("\n");
            }
            player.ChatMessage(sb.ToString().TrimEnd());
        }

        // Translates a string
        private string _(string text, Dictionary<string, string> replacements = null) {
            if (messages.ContainsKey(text) && messages[text] != null)
                text = messages[text];
            if (replacements != null)
                foreach (var replacement in replacements)
                    text = text.Replace("%" + replacement.Key + "%", replacement.Value);
            return text;
        }

    }
}
