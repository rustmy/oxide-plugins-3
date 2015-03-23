// Reference: Oxide.Ext.Rust
// Reference: RustBuild

using System.Reflection;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("NpcController", "Bombardir", "0.0.1")]
    class NpcController : RustPlugin
	{
        private static FieldInfo serverinput;
        private static FieldInfo AiGetter;
        private int AuthLvL = 2;

        class NpcControl : MonoBehaviour 
		{
            private InputState input;
            private BasePlayer owner;
            private NpcAI npc;
            private float TimeSincePressed;
		
			void Awake() 
			{
                owner = GetComponent<BasePlayer>();
                input = serverinput.GetValue(owner) as InputState;
				enabled = false;
                TimeSincePressed = Time.realtimeSinceStartup;
			}

            void OnAttacked(HitInfo info)
            {
                if (npc && info.Initiator)
                {
                    npc.targetentity = info.Initiator.GetComponent<BaseCombatEntity>();
                    npc.ShouldAttack = true;
                }
            }

            void FixedUpdate()
            {
                if (input.WasJustPressed(BUTTON.USE) && Time.realtimeSinceStartup - TimeSincePressed > 0.1)
                {
                    TimeSincePressed = Time.realtimeSinceStartup;
                    RaycastHit hit;
                    Vector3 raypos = transform.position;
                    raypos.y = raypos.y + 1.5f;
                    if (Physics.Raycast(raypos, Quaternion.Euler(input.current.aimAngles) * Vector3.forward, out hit) && hit.transform != transform)
                    {
                        if (npc == null)
                        {
                            BaseNPC hited = hit.transform.GetComponent<BaseNPC>();
                            if (hited != null)
                            {
                                owner.ChatMessage("BaseNPC found!");
                                NPCAI ai = AiGetter.GetValue(hited) as NPCAI;
                                if (ai != null)
                                {
                                    ai.ServerDestroy();
                                    npc = hited.gameObject.AddComponent<NpcAI>();
                                    npc.Init(this);
                                }
                            }
                        }
                        else
                        {
                            npc.targetentity = hit.transform.GetComponent<BaseCombatEntity>();
                            if (npc.targetentity == null)
                            {
                                owner.ChatMessage("Move command!");
                                npc.targetpoint = hit.point;
                                npc.ShouldAttack = true;
                            }
                            else
                            {
                                if (npc.targetentity == npc.GetComponent<BaseCombatEntity>())
                                {
                                    npc.ShouldAttack = !npc.ShouldAttack;
                                    if (npc.ShouldAttack)
                                    {
                                        owner.ChatMessage("UnFollow command!");
                                        npc.targetentity = null;
                                    }
                                    else
                                    {
                                        owner.ChatMessage("Follow command!");
                                        npc.targetentity = owner.GetComponent<BaseCombatEntity>();
                                    }

                                }
                                else
                                {
                                    owner.ChatMessage("Attack!");
                                    npc.ShouldAttack = true;
                                }
                                npc.targetpoint = Vector3.zero;
                            }
                        }
                    }
                }
			}
		}

        class NpcAI : MonoBehaviour
        {
            private static float IgnoreTargetDistance = 100f;
            private static float AttackTargetDistance = 1.5f;
            private static float TargetMoveDistance = 3f;

            private BaseNPC Base;
            private NpcControl owner;
            internal bool ShouldAttack;
            internal Vector3 targetpoint;
            internal BaseCombatEntity targetentity;

            public void Init(NpcControl player)
            {
                owner = player;
                ShouldAttack = true;
                Base = GetComponent<BaseNPC>();
                targetpoint = Vector3.zero;
            }

            void FixedUpdate()
            {
                if (targetentity != null)
                {
                    float distance = Vector3.Distance(transform.position, targetentity.transform.position);
                    if (distance > IgnoreTargetDistance)
                        targetentity = null;
                    else if (ShouldAttack && distance < AttackTargetDistance)
                        Base.attack.Hit(targetentity, 2, false);
                    else if (ShouldAttack || distance > TargetMoveDistance)
                        Move(targetentity.transform.position);
                }
                else if (targetpoint != Vector3.zero && targetpoint != transform.position)
                    Move(targetpoint);
            }

            private void Move(Vector3 point)
            {
                Base.steering.Move(Vector3Ex.XZ3D(point - transform.position).normalized, point, NPCSpeed.Gallop);
            }
        }

        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", ( BindingFlags.Instance | BindingFlags.NonPublic ));
            AiGetter = typeof(BaseNPC).GetField("ai", (BindingFlags.Instance | BindingFlags.NonPublic));
        }
		
		void Unload()
		{
            var objects = GameObject.FindObjectsOfType(typeof(NpcControl));
			if (objects != null)
				foreach (var gameObj in objects)
					GameObject.Destroy(gameObj);
            var objects1 = GameObject.FindObjectsOfType(typeof(NpcAI));
            if (objects1 != null)
                foreach (var gameObj1 in objects1)
                    GameObject.Destroy(gameObj1);
		}   
 
        [ChatCommand("npc")]
        void Fly(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= AuthLvL)
			{
                NpcControl comp = player.GetComponent<NpcControl>();
                if (!comp)
                    comp = player.gameObject.AddComponent<NpcControl>();

                if (comp.enabled)
				{
                    comp.enabled = false;
					SendReply(player, "NPC Mode deactivated!");	
				}
				else
				{
                    comp.enabled = true;
					SendReply(player, "NPC Mode activated!");
				}
			}
			else
				SendReply(player, "No Permission!");
        }
	}
}