// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: UnityEngine

using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{

    [Info("Timed Explosives Modifier", "Mughisi", "1.0.0")]
    class TimedExplosivesModifier : RustPlugin
    {

        #region Configuration Data

        bool configChanged;

        // Explosive damage values.
        string defaultChatPrefix = "Bomb Squad";
        string defaultChatPrefixColor = "#008000ff";
        float defaultDamageModifier = 100;

        string chatPrefix;
        string chatPrefixColor;
        float damageModifier;

        // Messages
        string defaultHelpTextPlayer = "Timed Explosives deal {0}% of normal damage.";
        string defaultHelpTextAdmin = "Modify the amount of damage Timed Explosives deal with the command /explosivedamage <value>";
        string defaultModified = "Timed Explosives damage changed to {0}% of normal damage.";
        string defaultNotAllowed = "You are not allowed to use this command.";
        string defaultInvalidArgument = "Invalid arguments supplied!\r\nUse '/explosivedamage <value>' where value is the % of the original damage.";

        string helpTextPlayer;
        string helpTextAdmin;
        string modified;
        string notAllowed;
        string invalidArgument;

        #endregion

        protected override void LoadDefaultConfig()
        {
            Log("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void Loaded()
        {
            LoadVariables();

            // Save config changes when required
            if (configChanged)
            {
                Log("Config file was updated.");
                SaveConfig();
            }

        }

        void LoadVariables()
        {
            // Settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));
            chatPrefixColor = Convert.ToString(GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor));
            damageModifier = float.Parse(Convert.ToString(GetConfigValue("Settings", "DamageModifier", defaultDamageModifier)), System.Globalization.CultureInfo.InvariantCulture);

            // Messages
            helpTextPlayer = Convert.ToString(GetConfigValue("Messages", "HelpTextPlayer", defaultHelpTextPlayer));
            helpTextAdmin = Convert.ToString(GetConfigValue("Messages", "HelpTextAdmin", defaultHelpTextAdmin));
            modified = Convert.ToString(GetConfigValue("Messages", "Modified", defaultModified));
            notAllowed = Convert.ToString(GetConfigValue("Messages", "NotAllowed", defaultNotAllowed));
            invalidArgument = Convert.ToString(GetConfigValue("Messages", "InvalidArgument", defaultInvalidArgument));
        }

        [ChatCommand("explosivedamage")]
        void ChangeExplosivesDamage(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 2)
            {
                float newModifier;

                try
                {
                    newModifier = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    SendChatMessage(player, defaultInvalidArgument);
                    return;
                }

                SetConfigValue("Settings", "DamageModifier", newModifier);
                damageModifier = newModifier;
                SendChatMessage(player, modified, newModifier);
                return;
            }

            SendChatMessage(player, notAllowed);
        }

        #region Hooks

        void OnEntitySpawn(BaseEntity entity)
        {
            if (entity.GetType() == typeof(TimedExplosive))
            {
                var explosive = entity as TimedExplosive;
                explosive.damage *= damageModifier / 100;
            }
        }

        void SendHelpText(BasePlayer player)
        {
            SendChatMessage(player, helpTextPlayer, damageModifier);

            if (player.net.connection.authLevel == 2)
                SendChatMessage(player, helpTextAdmin);
        }

        #endregion

        #region Helper Methods

        void Log(string message)
        {
            Puts("{0} : {1}", Title, message);
        }

        void SendChatMessage(BasePlayer player, string message, params object[] arguments)
        {
            string chatMessage = $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}";
            SendReply(player, chatMessage, arguments);
        }

        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;

            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }

            if (!data.TryGetValue(setting, out value))
            {
                value = defaultValue;
                data[setting] = value;
                configChanged = true;
            }

            return value;
        }

        void SetConfigValue(string category, string setting, object newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;

            if (data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }

            SaveConfig();
        }

        #endregion

    }

}
