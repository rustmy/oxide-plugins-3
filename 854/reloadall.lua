PLUGIN.Title        = "ReloadAll"
PLUGIN.Description  = "Reloads all Plugins"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,0,0)
PLUGIN.HasConfig    = false^

function PLUGIN:Init()	
 	command.AddChatCommand("reload", self.Object, "cmdReloadAll")
end

function PLUGIN:cmdReloadAll(player)
	if player.net.connection.authLevel > 1 then
		rust.RunServerCommand("oxide.reload *")
		rust.SendChatMessage(player, "RELOAD", "All plugins reloaded!")
	else
		rust.SendChatMessage(player, "RELOAD", "You have no permission to use this command!")
	end
end