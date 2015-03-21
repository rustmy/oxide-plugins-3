// Reference: Oxide.Ext.Rust

using System;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{

	[Info("Fly", "Bombardir", 0.2)]
	class Fly : RustPlugin
	{
        private static FieldInfo serverinput;
        private static byte authLevel = 2;

		class FlyMode : MonoBehaviour 
		{
			public float MaxSpeed = 30;
			public float MinSpeed = 5;
			public float StandartSpeed = 10;
			private float speed;
            private Vector3 direction;
            private InputState input;
			private BasePlayer player;
		
			private void CheckParent()
			{
				BaseEntity parentEntity = player.GetParentEntity();
				if (parentEntity != null)
				{
					parentEntity.RemoveChild(player);
					Vector3 CurrPos = parentEntity.transform.position;
					player.parentEntity.Set(null);
					player.parentBone = 0U;
					transform.position = CurrPos;
					player.UpdateNetworkGroup();
					player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
				}
			}
		
			void Awake () 
			{
                player = GetComponent<BasePlayer>();
				input = serverinput.GetValue(player) as InputState;
				enabled = false;
			}
			

            void Update()
            {
                if (!player.IsSpectating())
                    player.ChangePlayerState(PlayerState.Type.Spectating, false);

				direction = Vector3.zero;
				if (input.IsDown(BUTTON.FORWARD))
					direction.z++;
				if (input.IsDown(BUTTON.RIGHT))
					direction.x++;
				if (input.IsDown(BUTTON.LEFT))
					direction.x--;
				if (input.IsDown(BUTTON.BACKWARD))
					direction.z--;

				if (direction != Vector3.zero)
				{
					CheckParent();
					
					if (input.IsDown(BUTTON.SPRINT))
						speed = MaxSpeed;
					else if (input.IsDown(BUTTON.LOOK_ALT))
						speed = MinSpeed;
					else
						speed = StandartSpeed;

                    MovePlayerToPos(player, transform.position, transform.position + Quaternion.Euler(input.current.aimAngles) * direction * Time.deltaTime * speed);
				}
            }

            void OnDisable()
            {
				CheckParent();
                player.ChangePlayerState(PlayerState.Type.Normal, false);
            }
		}

		private static void MovePlayerToPos(BasePlayer player, Vector3 oldpos, Vector3 newpos)
		{
			player.transform.position = newpos;
			if (Vector3.Distance(newpos, oldpos) > 25.0)
				player.ClientRPC(null, player, "ForcePositionTo", new object[] { newpos });
			else
				player.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
		}
		
		void LoadDefaultConfig()
		{
			Config["AuthLevel"] = 2;
			SaveConfig();
		}
    
        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
			if (Config["AuthLevel"] != null)
				authLevel = Convert.ToByte(Config["AuthLevel"]);
        }
		
		void Unload()
		{	
			var objects = GameObject.FindObjectsOfType(typeof(FlyMode));
			if (objects != null)
				foreach (var gameObj in objects)
					GameObject.Destroy(gameObj);
		} 

        [ChatCommand("fly")]
        void FlyCMD(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= authLevel)
			{
				FlyMode fly = player.GetComponent<FlyMode>();
				if (!fly)
					fly = player.gameObject.AddComponent<FlyMode>();
					
				if (args.Length > 1)
					switch (args[0])
					{
						case "standart":
							fly.StandartSpeed = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the standart speed = "+args[1]);
							break;
						case "max":
							fly.MaxSpeed = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the max speed = "+args[1]);
							break;
						case "min":
							fly.MinSpeed = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the min speed = "+args[1]);
							break;
						default:
							SendReply(player, "Variables: standart, max, min");
							break;
					}
				else
					if (fly.enabled)
					{
						fly.enabled = false;
						SendReply(player, "Fly deactivated!");	
					}
					else
					{
						fly.enabled = true;
						SendReply(player, "Fly activated!");
					}
			}
			else
				SendReply(player, "No Permission!");
        }
		
		[ChatCommand("land")]
        void Land(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= authLevel)
			{
				RaycastHit hit;
                if (Physics.Raycast(new Ray(player.transform.position, Vector3.down), out hit, 25000) || Physics.Raycast(new Ray(player.transform.position, Vector3.up), out hit, 25000))
					MovePlayerToPos(player, player.transform.position, hit.point);
                else
                    SendReply(player, "Can't find position to land!");
			}
			else
				SendReply(player, "No Permission!");
		}
	}
}