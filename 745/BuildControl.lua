PLUGIN.Name = "BuildControl"
PLUGIN.Title = "Build Controller"
PLUGIN.Description = "Control the buildings placement in your server."
PLUGIN.Version = V(1, 1, 5)
PLUGIN.Author = "SPooCK"
PLUGIN.HasConfig = true

local function IsAllowed(player)
    local playerAuthLevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
    if playerAuthLevel >= 2 then return true end
    return false
end

local function GetGround(pos, method, n)
local arr  = util.TableToArray( { pos, UnityEngine.Vector3.get_down() } )
local hits = UnityEngine.Physics.RaycastAll["methodarray"][5]:Invoke( nil, arr )
local info = tostring(hits)
if (not info or not info:find("UnityEngine.RaycastHit")) then return false end
	local it = hits:GetEnumerator()
	while (it:MoveNext()) do
		if (method == 1 and it.Current and tonumber(it.Current.point.y) ~= 0) then
			if (n > 0 and tonumber(it.Current.distance) > n or n < 0 and tonumber(it.Current.distance) < n) then return true end
		elseif (method == 2 and it.Current) then
			if (tostring(it.Current.transform):find("rock")) then return true end
		elseif (method == 3 and it.Current) then
			if (tonumber(it.Current.point.y)) then return tonumber(it.Current.point.y) end
		end						
	end
return false
end

local function GetSphere(object, radius)
local pos = object.transform.position
local arr = util.TableToArray( { pos , radius } )
util.ConvertAndSetOnArray(arr, 1, radius, System.Single._type)
local hits = UnityEngine.Physics.OverlapSphere["methodarray"][1]:Invoke(nil,arr)
local it = hits:GetEnumerator()
	while (it:MoveNext()) do
	if (it.Current:GetComponentInParent(global.BuildingBlock._type)) then
	local block = it.Current:GetComponentInParent(global.BuildingBlock._type) local name = tostring(block.blockDefinition.name)	
		if (name == "foundation" or name == "foundation.triangle" or name == "floor") then return true end
	end
	end
return false
end

local function Remove(player, object, msg)
if (object:GetComponent("BaseNetworkable").isDestroyed) then return end
local buildingblock = object:GetComponent("BuildingBlock")
	buildingblock:KillMessage()
	rust.SendChatMessage(player, "BuildControl", msg)
end

function PLUGIN:OnEntityBuilt(helditem, object)
if (not object:GetComponent("BuildingBlock")) then return end
local player = helditem.ownerPlayer
if (self.Config.AdminException and IsAllowed(player)) then return end
local buildingblock = object:GetComponent("BuildingBlock")
local blockname = tostring(buildingblock.blockDefinition.name)
local pos = buildingblock.transform.position

	if (not self.Config.WaterBuild[1]) then
	local place = GetGround(pos, 3)
		if (place and place < 0) then
		local msg = "Building in Water is forbidden in this Server !"
		Remove(player, object, msg)
		return
		end
	elseif (self.Config.WaterBuild[1] and self.Config.WaterBuild[2]) then
	local place = GetGround(pos, 3)
	local range = tonumber(self.Config.WaterBuild[2])
		if (place and range and place < range) then
		local msg = "You've reached MAX Water Depth <" ..range.. "m.> !"
		Remove(player, object, msg)
		return
		end
	end
	
	if (self.Config.BuildHeight) then
	local distance = GetGround(pos, 1, self.Config.BuildHeight)
	local place = GetGround(pos, 3)
		if (distance and place and place ~= 0) then
		local msg = "You've reached MAX Build Height <" ..self.Config.BuildHeight.. "m.> !"
		Remove(player, object, msg)
		return
		end
	end
	
	if (not self.Config.UndergroundBuild) then
	local distance = GetGround(pos, 1, -1.5)
	local collide = GetGround(pos, 2)
		if (collide and distance) then
		local msg = "Building Underground is forbidden in this Server !"
		Remove(player, object, msg)
		return
		end
	end
	
	if (not self.Config.RockBuild and blockname:find("foundation")) then
	local collide = GetGround(pos, 2)
		if (collide) then
		local msg = "Building on Rocks is forbidden in this Server !"
		Remove(player, object, msg)
		return
		end
	end
end

function PLUGIN:OnItemDeployed(helditem, item)
local player = helditem.ownerPlayer
if (self.Config.AdminException and IsAllowed(player)) then return end
local name = tostring(item.name)
if (self.Config.CupFoundation and name:find("cupboard")) then
if (item:GetComponent("BaseNetworkable").isDestroyed) then return end
	local ray = GetSphere(item, 0.5)
	if (not ray) then
	item:GetComponent("BaseEntity"):KillMessage()
	rust.SendChatMessage(player, "BuildControl", "Cupboard can be placed only in the center of a foundation/floor !")
	end
end
end

function PLUGIN:LoadDefaultConfig()
self.Config.RockBuild = false
self.Config.WaterBuild = { true, -3 }
self.Config.UndergroundBuild = false
self.Config.BuildHeight = 20
self.Config.CupFoundation = true
self.Config.AdminException = false
end