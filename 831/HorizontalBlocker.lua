PLUGIN.Title       = "Horizontal Building Blocker"
PLUGIN.Description = "Stops a player from building horizontal."
PLUGIN.Version     = V(1, 0, 3)
PLUGIN.HasConfig   = false
PLUGIN.Author      = "Mughisi"
 
local GradeEnum = {
	Twigs   = 0,
	Wood    = 1,
	Stone   = 2,
	Metal   = 3,
	TopTier = 4
}
 
function PLUGIN:OnEntityBuilt(Planner, GameObject)
	local BuildingBlock = GameObject:GetComponent("BuildingBlock")
	local Angles = BuildingBlock.transform.rotation.eulerAngles
 
	if math.floor(Angles.x) > 0 or math.floor(Angles.z) > 0 then
		local Player = Planner.ownerPlayer
		local PlayerID = rust.UserIDFromPlayer(Player)

		BuildingBlock:KillMessage()

		print(Player.displayName .. "[" .. PlayerID .. "] tried to place a " .. BuildingBlock.blockDefinition.name .. " sideways.")
		
	end
 
end