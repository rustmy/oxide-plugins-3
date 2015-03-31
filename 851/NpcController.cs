// Reference: RustBuild

using System;
using System.Reflection;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("NpcController", "Bombardir", "0.0.8", ResourceId = 851)]
    class NpcController : RustPlugin
	{
        private static FieldInfo serverinput;
        private static MethodInfo SetDeltaTimeMethod;
        private static NpcController PluginInstance;

        private bool HasPermission(BasePlayer player, string perm)
        {
            return !UsePermission || permission.UserHasPermission(player.userID.ToString(), perm);
        }

        private enum Act
        {
            Move,
            Attack,
            Follow,
            Eat,
            Sleep,
            None
        }

        #region NPC Controller Class

        class NpcControl : MonoBehaviour 
		{
            private static float ButtonReload = 0.3f;
            private static float DrawReload = 0.05f;
            internal static float ReloadControl = 60f;
            internal static float MaxControlDistance = 10f;

            internal bool DrawEnabled;
            private InputState input;
            private float NextTimeToPress;
            private float NextTimeToControl;
            private float NextTimeToDraw;

            public NpcAI npc;
            public BasePlayer owner;
		
			void Awake() 
			{
                owner = GetComponent<BasePlayer>();
                input = serverinput.GetValue(owner) as InputState;
				enabled = false;
                NextTimeToPress = 0f;
                NextTimeToControl = 0f;
                NextTimeToDraw = 0f;
                DrawEnabled = GlobalDraw;
			}

            void OnAttacked(HitInfo info)
            {
                if (npc && info.Initiator)
                    npc.Attack(info.Initiator.GetComponent<BaseCombatEntity>());
            }

            void FixedUpdate()
            {
                float time = Time.realtimeSinceStartup;
                if (input.WasJustPressed(BUTTON.USE) && NextTimeToPress < time)
                {
                    NextTimeToPress = time + ButtonReload;
                    UpdateAction();
                }
                if (DrawEnabled && npc != null && npc.action != Act.None && npc.action != Act.Follow && npc.action != Act.Sleep && NextTimeToDraw < time)
                {
                    NextTimeToDraw = time + DrawReload;
                    UpdateDraw();
                }
			}

            void UpdateDraw()
            {
                Vector3 drawpos = npc.action == Act.Move ? npc.targetpoint : npc.targetentity.transform.position;
                owner.SendConsoleCommand("ddraw.arrow", new object[] { DrawReload + 0.02f, npc.action == Act.Move ? Color.cyan : npc.action == Act.Attack ? Color.red : Color.yellow, drawpos + new Vector3(0, 5f, 0), drawpos, 1.5f });
            }

            void UpdateAction()
            {
                RaycastHit hit;
                if (Physics.SphereCast(owner.eyes.position, 0.7f, Quaternion.Euler(input.current.aimAngles) * Vector3.forward, out hit) && hit.transform != transform)
                {
                    if (npc == null)
                    {
                        BaseNPC hited = hit.transform.GetComponent<BaseNPC>();
                        if (hited != null)
                        {
                            NpcAI OwnedNpc = hited.GetComponent<NpcAI>();
                            if (OwnedNpc != null && OwnedNpc.owner != this)
                                owner.ChatMessage(NoOwn);
                            else if (NextTimeToControl < Time.realtimeSinceStartup)
                            {
                                if (PluginInstance.HasPermission(owner, "can" + hited.modelPrefab.Remove(0, 12).Replace("_skin", "")))
                                {
                                    if (hit.distance < MaxControlDistance)
                                    {
                                        NextTimeToControl = Time.realtimeSinceStartup + ReloadControl;
                                        owner.ChatMessage(NewPetMsg);
                                        npc = hited.gameObject.AddComponent<NpcAI>();
                                        npc.owner = this;
                                    }
                                    else
                                        owner.ChatMessage(CloserMsg);
                                }
                                else
                                    owner.ChatMessage(NoPermPetMsg);
                            }
                            else
                                owner.ChatMessage(ReloadMsg);
                        }
                    }
                    else
                    {
                        npc.targetentity = hit.transform.GetComponent<BaseCombatEntity>();
                        if (npc.targetentity == null)
                        {
                            npc.targetpoint = hit.point;
                            npc.action = Act.Move;
                        }
                        else
                        {
                            if (npc.targetentity == (BaseCombatEntity) npc.Base)
                            {
                                if (npc.action == Act.Follow)
                                {
                                    owner.ChatMessage(UnFollowMsg);
                                    npc.action = Act.None;
                                }
                                else
                                {
                                    owner.ChatMessage(FollowMsg);
                                    npc.targetentity = owner.GetComponent<BaseCombatEntity>();
                                    npc.action = Act.Follow;
                                }
                            }
                            else if (npc.targetentity is BaseCorpse)
                            {
                                owner.ChatMessage(EatMsg);
                                npc.action = Act.Eat;
                            }
                            else
                            {
                                owner.ChatMessage(AttackMsg);
                                npc.Attack(npc.targetentity);
                            }
                        }
                    }
                }
            }
		}

        #endregion
        #region NPC AI Class

        class NpcAI : MonoBehaviour
        {
            internal static float IgnoreTargetDistance = 70f;
            private static float PointMoveDistance = 1f;
            private static float TargetMoveDistance = 3f;

            private float lastTick;
            private float hungerLose;
            private float thristyLose;
            private float sleepLose;
            private double attackrange;
            internal Act action;
            internal Vector3 targetpoint;
            internal BaseCombatEntity targetentity;

            public NpcControl owner;
            public BaseNPC Base;
            public NPCAI RustAI;
            public NPCMetabolism RustMetabolism;

            private void Move(Vector3 point, Vector3 normal = default(Vector3) )
            {
                if (normal == default(Vector3))
                    normal = Vector3Ex.XZ3D(point - transform.position).normalized;
                Base.state = BaseNPC.State.Normal;
                RustAI.sense.Think();
                Base.steering.Move(normal, point, NPCSpeed.Gallop);
            }

            internal void Attack(BaseCombatEntity ent)
            {
                targetentity = ent;
                action = Act.Attack;
                attackrange = Math.Pow(Vector3Ex.Max(BoundsExtension.XZ3D(Base._collider.bounds).extents) + Base.attack.range + Vector3Ex.Max(BoundsExtension.XZ3D(targetentity._collider.bounds).extents), 2);
            }
            

            void Awake()
            {
               RustAI = GetComponent<NPCAI>();
               RustAI.ServerDestroy();
               RustMetabolism = GetComponent<NPCMetabolism>();
               Base = GetComponent<BaseNPC>();
               lastTick = Time.time;
               targetpoint = Vector3.zero;
               action = Act.None;
               Base.locomotion.turnSpeed++;
               hungerLose = RustMetabolism.calories.max*2 / 15000;
               thristyLose = RustMetabolism.hydration.max*3 / 15000;
               sleepLose = RustMetabolism.sleep.max / 15000;
            }

            void FixedUpdate()
            {
                SetDeltaTimeMethod.Invoke( RustAI, new object[] { Time.time - lastTick });
                if ((double)RustAI.deltaTime >= (double)server.NPCTickDelta())
                {
                    lastTick = Time.time;
                    if (!Base.IsStunned())
                    {
                        Base.Tick();
                        if (action != Act.Sleep)
                        {
                            RustMetabolism.sleep.MoveTowards(0.0f, RustAI.deltaTime * sleepLose);
                            RustMetabolism.hydration.MoveTowards(0.0f, RustAI.deltaTime * thristyLose);
                            RustMetabolism.calories.MoveTowards(0.0f, RustAI.deltaTime * hungerLose);
                        }

                        if (action != Act.None)
                            if (action == Act.Move)
                            {
                                float distance = Vector3.Distance(transform.position, targetpoint);
                                if (distance < PointMoveDistance)
                                    action = Act.None;
                                else
                                    Move(targetpoint);
                            }
                            else if (action == Act.Sleep)
                            {
                                Base.state = BaseNPC.State.Sleeping;
                                Base.sleep.Recover(2f);
                                RustMetabolism.stamina.Run(4f);
                                Base.StartCooldown(2f, true);
                            }
                            else if (targetentity == null)
                            {
                                action = Act.None;
                                Base.state = BaseNPC.State.Normal;
                            }
                            else
                            {
                                float distance = Vector3.Distance(transform.position, targetentity.transform.position);
                                if (distance < IgnoreTargetDistance)
                                {
                                    if (action != Act.Follow)
                                    {
                                        Vector3 normalized = Vector3Ex.XZ3D(targetentity.transform.position - transform.position).normalized;
                                        if (distance > attackrange)
                                            Move(targetentity.transform.position, normalized);
                                        else
                                        {
                                            if (action == Act.Eat)
                                            {
                                                if (Base.diet.Eat(targetentity))
                                                {
                                                    RustMetabolism.calories.Add(RustMetabolism.calories.max * 0.03f);
                                                    RustMetabolism.hydration.Add(RustMetabolism.hydration.max * 0.03f);
                                                }
                                            }
                                            else if (Base.attack.Hit(targetentity, targetentity is BaseNPC ? 1f : 2f, false))
                                                transform.rotation = Quaternion.LookRotation(normalized);
                                            Base.steering.Face(normalized);
                                        }
                                    }
                                    else if (distance > TargetMoveDistance)
                                        Move(targetentity.transform.position);
                                }
                            }
                    }
                }
            }

            void OnDestroy ()
            {
                Base.locomotion.turnSpeed--;
                RustAI.ServerInit();
            }
        }
        #endregion

        #region Config & Initialisation

        private static bool UsePermission = true;
        private static bool GlobalDraw = true;
        private static string ReloadMsg = "You can not tame so often! Wait!";
        private static string NewPetMsg = "Now you have a new pet!";
        private static string CloserMsg = "You need to get closer!";
        private static string NoPermPetMsg = "You don't have permission to take this NPC!";
        private static string FollowMsg = "Follow command!";
        private static string UnFollowMsg = "UnFollow command!";
        private static string SleepMsg = "Sleep command!";
        private static string AttackMsg = "Attack!";
        private static string NoPermMsg = "No Permission!";
        private static string ActivatedMsg = "NPC Mode activated!";
        private static string DeactivatedMsg = "NPC Mode deactivated!";
        private static string NotNpc = "You don't have a pet!";
        private static string NpcFree = "Now your per is free!";
        private static string NoOwn = "This npc is already tamed by other player!";
        private static string EatMsg = "Time to eat!";
        private static string DrawEn = "Draw enabled!";
        private static string DrawDis = "Draw disabled!";
        private static string DrawSysDis = "Draw system was disabled by administrator!";
        private static string InfoMsg = "<color=red>Health: {health}%</color>, <color=orange>Hunger: {hunger}%</color>, <color=cyan>Thirst: {thirst}%</color>, <color=teal>Sleepiness: {sleep}%</color>, <color=lightblue>Stamina: {stamina}%</color>";
        

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Instance | BindingFlags.NonPublic));
            SetDeltaTimeMethod = typeof(NPCAI).GetProperty("deltaTime", (BindingFlags.Public | BindingFlags.Instance)).GetSetMethod(true);
            PluginInstance = this;

            CheckCfg<bool>("Use permissions", ref UsePermission);
            CheckCfg<bool>("Enable draw system", ref GlobalDraw);
            CheckCfg<float>("Reload time to take new npc", ref NpcControl.ReloadControl);
            CheckCfg<float>("Max distance to take npc", ref NpcControl.MaxControlDistance);
            CheckCfg<float>("Distance when target will be ignored by NPC", ref NpcAI.IgnoreTargetDistance);
            CheckCfg<string>("New pet msg", ref NewPetMsg);
            CheckCfg<string>("Closer msg", ref CloserMsg);
            CheckCfg<string>("No take perm msg", ref NoPermPetMsg);
            CheckCfg<string>("Follow msg", ref FollowMsg);
            CheckCfg<string>("UnFollow msg", ref UnFollowMsg);
            CheckCfg<string>("Sleep msg", ref SleepMsg);
            CheckCfg<string>("Attack msg", ref AttackMsg);
            CheckCfg<string>("No command perm msg", ref NoPermMsg);
            CheckCfg<string>("Activated msg", ref ActivatedMsg);
            CheckCfg<string>("Deactivated msg", ref DeactivatedMsg);
            CheckCfg<string>("Reload msg", ref ReloadMsg);
            CheckCfg<string>("No pet msg", ref NotNpc);
            CheckCfg<string>("Free pet msg", ref NpcFree);
            CheckCfg<string>("Already tamed msg", ref NoOwn);
            CheckCfg<string>("Eat msg", ref EatMsg);
            CheckCfg<string>("Draw enabled msg", ref DrawEn);
            CheckCfg<string>("Draw disabled msg", ref DrawDis);
            CheckCfg<string>("Draw system disabled msg", ref DrawSysDis);
            CheckCfg<string>("Info msg", ref InfoMsg);
            SaveConfig();

            if (UsePermission)
            {
                permission.RegisterPermission("cannpc", this);
                permission.RegisterPermission("canstag", this);
                permission.RegisterPermission("canbear", this);
                permission.RegisterPermission("canwolf", this);
                permission.RegisterPermission("canchicken", this);
                permission.RegisterPermission("canboar", this);
            }
        }

        #endregion

        #region Unload Hook (destroy all npc controller objects)

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

        #endregion

        #region PET Command (activate/deactivate npc mode)

        [ChatCommand("pet")]
        void npc(BasePlayer player, string command, string[] args)
        {
            if (HasPermission(player, "cannpc"))
			{
                NpcControl comp = player.GetComponent<NpcControl>() ?? player.gameObject.AddComponent<NpcControl>();
                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "free":
                            if (comp.npc)
                            {
                                GameObject.Destroy(comp.npc);
                                SendReply(player, NpcFree);
                            }
                            else
                                SendReply(player, NotNpc);
                            break;
                        case "draw":
                            if (GlobalDraw)
                                if (comp.DrawEnabled)
                                {
                                    comp.DrawEnabled = false;
                                    SendReply(player, DrawDis);
                                }
                                else
                                {
                                    comp.DrawEnabled = true;
                                    SendReply(player, DrawEn);
                                }
                            else
                                SendReply(player, DrawSysDis);
                            break;
                        case "sleep":
                            if (comp.npc)
                            {
                                SendReply(player, SleepMsg);
                                comp.npc.action = Act.Sleep;
                            }
                            else
                                SendReply(player, NotNpc);
                            break;
                        case "info":
                            if (comp.npc)
                            {
                                NPCMetabolism meta = comp.npc.RustMetabolism;
                                SendReply(player, InfoMsg
                                    .Replace("{health}", Math.Round(comp.npc.Base.health*  100/comp.npc.Base.MaxHealth()).ToString())
                                    .Replace("{hunger}", Math.Round(meta.hydration.value * 100 / meta.hydration.max).ToString())
                                    .Replace("{thirst}", Math.Round(meta.calories.value * 100 / meta.calories.max).ToString())
                                    .Replace("{sleep}", Math.Round(meta.sleep.value * 100 / meta.sleep.max).ToString())
                                    .Replace("{stamina}", Math.Round(meta.stamina.value * 100 / meta.stamina.max).ToString()));
                            }
                            else
                                SendReply(player, NotNpc);
                            break;
                    }
                }
                else
                {
                    if (comp.enabled)
                    {
                        comp.enabled = false;
                        SendReply(player, DeactivatedMsg);
                    }
                    else
                    {
                        comp.enabled = true;
                        SendReply(player, ActivatedMsg);
                    }
                }
			}
			else
                SendReply(player, NoPermMsg);
        }

        #endregion
    }
}