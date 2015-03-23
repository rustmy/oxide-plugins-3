// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: UnityEngine

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rust:IO FriendlyFire", "playrust.io / dcode", "1.1.0", ResourceId = 840)]
    public class FriendlyFire : RustPlugin
    {

        #region Rust:IO Bindings

        private Library lib;
        private MethodInfo isInstalled;
        private MethodInfo hasFriend;
        private MethodInfo addFriend;
        private MethodInfo deleteFriend;

        private void InitializeRustIO() {
            lib = Interface.GetMod().GetLibrary<Library>("RustIO");
            if (lib == null || (isInstalled = lib.GetFunction("IsInstalled")) == null || (hasFriend = lib.GetFunction("HasFriend")) == null || (addFriend = lib.GetFunction("AddFriend")) == null || (deleteFriend = lib.GetFunction("DeleteFriend")) == null) {
                lib = null;
                Puts("{0}: {1}", Title, "Rust:IO is not present. You need to install Rust:IO first in order to use this plugin!");
            }
        }

        private bool IsInstalled() {
            if (lib == null) return false;
            return (bool)isInstalled.Invoke(lib, new object[] {});
        }

        private bool HasFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)hasFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool AddFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)addFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool DeleteFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)deleteFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        #endregion

        private List<string> texts = new List<string>() {
            "%NAME% is your friend and cannot be hurt. To disable this, unshare your location with %NAME% on the live map.",
            "Usage: /ff or /friendlyfire",
            "Friendly fire is disabled for your friends:",
            "You do not have any friends currently.",
            "You may add or delete friends using the live map."
        };
        private Dictionary<string, string> messages = new Dictionary<string, string>();
        private Dictionary<string, DateTime> notificationTimes = new Dictionary<string, DateTime>();

        // Translates a string
        private string _(string text, Dictionary<string, string> replacements = null) {
            if (messages.ContainsKey(text) && messages[text] != null)
                text = messages[text];
            if (replacements != null)
                foreach (var replacement in replacements)
                    text = text.Replace("%" + replacement.Key + "%", replacement.Value);
            return text;
        }


        // Loads the default configuration
        protected override void LoadDefaultConfig() {
            var messages = new Dictionary<string, object>();
            foreach (var text in texts) {
                if (messages.ContainsKey(text))
                    Puts("{0}: {1}", Title, "Duplicate translation string: " + text);
                else
                    messages.Add(text, text);
            }
            Config["messages"] = messages;
        }

        // Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue) {
            if (Config[name] == null)
                return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized() {
            try {
                InitializeRustIO();
                LoadConfig();
                var customMessages = GetConfig<Dictionary<string, object>>("messages", null);
                if (customMessages != null)
                    foreach (var pair in customMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);
            } catch (Exception ex) {
                Error("OnServerInitialized failed: " + ex.Message);
            }
        }

        [HookMethod("OnEntityAttacked")]
        object OnEntityAttacked(MonoBehaviour entity, HitInfo hit) {
            try {
                if (lib == null || !(entity is BasePlayer) || !(hit.Initiator is BasePlayer))
                    return null;
                var victim = entity as BasePlayer;
                var victimId = victim.userID.ToString();
                var attacker = hit.Initiator as BasePlayer;
                var attackerId = attacker.userID.ToString();
                var key = attackerId + "-" + victimId;
                if (HasFriend(attackerId, victimId)) {
                    DateTime now = DateTime.UtcNow;
                    DateTime time;
                    if (!notificationTimes.TryGetValue(key, out time) || time < now.AddSeconds(-10)) {
                        attacker.SendConsoleCommand("chat.add", "", _("%NAME% is your friend and cannot be hurt. To disable this, unshare your location with %NAME% on the live map.", new Dictionary<string, string>() { { "NAME", victim.displayName } }));
                        notificationTimes[key] = now;
                    }
                    return false;
                }
            } catch (Exception ex) {
                Error("OnEntityAttacked failed: " + ex.Message);
            }
            return null;
        }

        private void cmdChatFriendlyfireImpl(BasePlayer player, string command, string[] args) {
            if (!IsInstalled())
                return;
            if (args.Length != 0) {
                SendReply(player, _("Usage: /ff or /friendlyfire"));
                return;
            }
            int n = 0;
            var sb = new StringBuilder();
            sb.Append("<size=22>FriendlyFire</size> by <color=#ce422b>http://playrust.io</color>\n");
            sb.Append(_("Friendly fire is disabled for your friends:")).Append("\n");
            var playerId = player.userID.ToString();
            foreach (var p in BasePlayer.activePlayerList) {
                var pId = p.userID.ToString();
                if (HasFriend(playerId, pId)) {
                    if (n > 0)
                        sb.Append(", ");
                    sb.Append(p.displayName);
                    ++n;
                }
            }
            foreach (var p in BasePlayer.sleepingPlayerList) {
                var pId = p.userID.ToString();
                if (HasFriend(playerId, pId)) {
                    if (n > 0)
                        sb.Append(", ");
                    sb.Append(p.displayName);
                    ++n;
                }
            }
            if (n == 0)
                sb.Append(_("You do not have any friends currently."));
            sb.Append("\n").Append(_("You may add or delete friends using the live map."));
            SendReply(player, sb.ToString());
        }

        [ChatCommand("ff")]
        private void cmdChatFF(BasePlayer player, string command, string[] args) {
            cmdChatFriendlyfireImpl(player, command, args);
        }

        [ChatCommand("friendlyfire")]
        private void cmdChatFriendlyfire(BasePlayer player, string command, string[] args) {
            cmdChatFriendlyfireImpl(player, command, args);
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

        #endregion
    }
}
