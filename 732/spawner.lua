PLUGIN.Title = "Spawner"
PLUGIN.Version = V(1, 2, 0)
PLUGIN.Description = ""
PLUGIN.Author = "Bombardir" 
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 732

local msgs, mode, nill_quaternion, nill_vector = {}, {}
local function SendMessage(player, msg)
	player:SendConsoleCommand("chat.add \"".. msgs.ChatName.."\" \"".. msg .."\"")
end
local function CreateEntity( prefab, pos, rot)
	return global.GameManager.server:CreateEntity( prefab, pos, rot )
end

local function Spawn(ent, pos, rot, player_)
	local spawned = CreateEntity(ent.name, pos or nill_vector, rot or nill_quaternion)
	if ent.hp then
		local max = spawned:MaxHealth()
		if ent.hp > max then
			spawned:InitializeHealth(ent.hp, ent.hp)
		else
			spawned:InitializeHealth(ent.hp, max)
		end
	end
	-- Thanks to Reneb (build plugin) --
	spawned:SendMessage("SetDeployedBy", player_, UnityEngine.SendMessageOptions.DontRequireReceiver ) 
	------------------------------------
	spawned:Spawn(true)
	if ent.player_name then
		local player = spawned:ToPlayer()
		player.displayName = ent.player_name
		if ent.sleeping then
			player:EndSleeping()
		end
	end
	spawned:SendNetworkUpdate(global.BasePlayer.NetworkQueue.Update)
	return msgs.Succes
end
function PLUGIN:Init()
	self.Config.Admin_Auth_LvL = self.Config.Admin_Auth_LvL or 2
	self.Config.Chat_Command   = self.Config.Chat_Command or "espawn"
	if self.Config.Spawn_List_Generate == nil then self.Config.Spawn_List_Generate = false end
	self.Config.Spawn_List_Name = self.Config.Spawn_List_Name or "Spawn List"
	msgs = self.Config.Messages or {}
	msgs.NoPerm = msgs.NoPerm or "No Permission!"
	msgs.ChatName = msgs.ChatName or "[Spawner]"
	msgs.SyntaxError = msgs.SyntaxError or "/%s [entity_name/false] [health] [true/false](sleeping?) [player_name]"
	msgs.EntityNo = msgs.EntityNo or "Entity with this name can't be created!"
	msgs.Succes = msgs.Succes or "Entity spawned!"
	msgs.ModeOn = msgs.ModeOn or "Spawner mode enabled!"
	msgs.ModeOff = msgs.ModeOff or "Spawner mode disabled!"
	self.Config.Messages = msgs
	command.AddChatCommand(self.Config.Chat_Command, self.Plugin, "C_Spawn")
end
function PLUGIN:OnServerInitialized()
    _,_ = pcall( new, UnityEngine.Quaternion._type, nil )
    _,_ = pcall( new, UnityEngine.Vector3._type, nil )
	nill_quaternion = new( UnityEngine.Quaternion._type, nil )
	nill_vector = new(UnityEngine.Vector3._type,nil)
	if self.Config.Spawn_List_Generate then
		self.Config.Spawn_List_Generate = false
		self:SaveConfig()
		local spawnfile = datafile.GetDataTable( self.Config.Spawn_List_Name )
		spawnfile.Can_Be_Spawned = {}
		spawnfile.Can_Not_Be_Spawned = {}
		local enum = global.GameManifest.Get().pooledStrings:GetEnumerator()
		while enum:MoveNext() do
			local prefab = enum.Current.str
			if CreateEntity(prefab, nill_vector, nill_quaternion) then
				table.insert(spawnfile.Can_Be_Spawned, prefab)
			else
				table.insert(spawnfile.Can_Not_Be_Spawned, prefab)
			end
			datafile.SaveDataTable( self.Config.Spawn_List_Name )
		end  
		print("------------ Spawn list + ------------")
	else
		self:SaveConfig()
	end
end
function PLUGIN:C_Spawn(player, _, args)
	if player:GetComponent("BaseNetworkable").net.connection.authLevel >= self.Config.Admin_Auth_LvL then
		local len = args.Length
		if len > 0 then
			local ent = args[0]
			if ent:lower() == "false" then
				mode[player] = nil
				SendMessage(player, msgs.ModeOff)
			else
				ent = { name = ent }
				if ent.name == "player/player" then
					ent.sleeping = false
					ent.player_name = ""
					if len > 2 and args[2]:lower() ~= "true" then ent.sleeping = true end
					if len > 3 then ent.player_name = args[3] end
				end
				local test_ent = CreateEntity(ent.name, player.transform.position, nill_quaternion)
				if test_ent then
					if test_ent:GetComponent("BaseCombatEntity") and not test_ent:GetComponent("BuildingBlock") then
						ent.hp = 100
						if len > 1 then ent.hp = tonumber(args[1]) or ent.hp end
					end					
					mode[player] = ent
					SendMessage(player, msgs.ModeOn)
					test_ent:Kill(ProtoBuf.EntityDestroy.Mode.None,0,0,nill_vector)
				else
					SendMessage(player, msgs.EntityNo)
				end
			end
		else
			SendMessage(player, msgs.SyntaxError:format(self.Config.Chat_Command))
		end
	else
		SendMessage(player, msgs.NoPerm)
	end
end

function PLUGIN:OnPlayerAttack(attacker, hitinfo)
	local pos = hitinfo.HitPositionWorld
	if pos then
		local ent = mode[attacker]
		if ent then
			SendMessage(attacker, Spawn(ent, pos, attacker.transform.rotation, attacker))
		end
	end
end
