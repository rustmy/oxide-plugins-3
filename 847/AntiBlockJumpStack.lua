
PLUGIN.Title       = "AntiBlockJumpStack"
PLUGIN.Description = "Stop players from building while jumping and placing objects on their own location."
PLUGIN.Version     = V( 1, 0, 0 )
PLUGIN.HasConfig   = false
PLUGIN.Author      = "Mughisi"

local BasePlayerModelState = global.BasePlayer._type:GetField("modelState", rust.PrivateBindingFlag())

function PLUGIN:OnEntityBuilt(planner, gameObject)
	local player = planner.ownerPlayer
	local buildingBlock = gameObject:GetComponent("BuildingBlock")
	local position = player.transform.position

	if buildingBlock then
		local modelState = BasePlayerModelState:GetValue(player)
		if not modelState.onground then
			self:SendMessage(player, "You aren't allowed to build while jumping or falling.")
			buildingBlock:KillMessage()
			return
		end
		
		local playerCollider = player.gameObject:GetComponentInChildren( UnityEngine.Collider._type )
		local blockCollider = buildingBlock.gameObject:GetComponentInChildren( UnityEngine.Collider._type )
		if playerCollider and blockCollider then
			local blockBounds = blockCollider.bounds
			local playerBounds = playerCollider.bounds

			if playerBounds:Intersects(blockBounds) then
				self:SendMessage(player, "You aren't allowed to build this close to yourself, take a step backwards.")
				buildingBlock:KillMessage()
				return
			end
		end
	end
end

function PLUGIN:SendMessage( target, message )
    if not target then return end
    if not target:IsConnected() then return end

    message = UnityEngine.StringExtensions.QuoteSafe( message )

    target:SendConsoleCommand( "chat.add \"0\""  .. message );
end
