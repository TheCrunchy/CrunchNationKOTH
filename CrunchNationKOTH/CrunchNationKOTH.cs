using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.API.Managers;
using Torch.Managers.ChatManager;
using Torch.Session;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;

namespace CrunchNationKOTH
{
    public class CrunchNationKOTH : TorchPluginBase
    {
        public static List<KothConfig> KOTHs = new List<KothConfig>();
        private static List<DateTime> captureIntervals = new List<DateTime>();
        private static Dictionary<String, int> amountCaptured = new Dictionary<String, int>();
       

        public static Logger Log = LogManager.GetCurrentClassLogger();

        public static Dictionary<string, DenialPoint> denials = new Dictionary<string, DenialPoint>();
      

        private TorchSessionManager sessionManager;
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Log.Info("Loading Crunch Koth");
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            SetupConfig();
        }



        public static string GetNationTag(IMyFaction fac)
        {
            if (fac.Description.Contains("UNIN"))
                return "UNIN";

            if (fac.Description.Contains("FEDR"))
                return "FEDR";

            if (fac.Description.Contains("CONS"))
                return "CONS";
            return null;

        }

        public static Boolean DoesGridHaveCaptureBlock(MyCubeGrid grid, KothConfig koth)
        {
            foreach (MyCubeBlock block in grid.GetFatBlocks())
            {
                if (block != null && block.BlockDefinition != null)
                {
                    Log.Info(block.BlockDefinition.Id.TypeId + " " + block.BlockDefinition.Id.SubtypeName);

                }
                else
                {
                    Log.Info("Null id for capture block");
                }

                if (block.OwnerId > 0 && block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "").Equals(koth.captureBlockType) && block.BlockDefinition.Id.SubtypeName.Equals(koth.captureBlockSubtype))
                {
                    if (block.IsFunctional && block.IsWorking)
                    {
                        if (block is Sandbox.ModAPI.IMyFunctionalBlock bl)
                        {
                            bl.Enabled = true;
                        }

                        if (block is Sandbox.ModAPI.IMyBeacon beacon)
                        {
                            beacon.Radius = koth.captureBlockBroadcastDistance;
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        public static Boolean IsContested(IMyFaction fac, KothConfig koth, string capturingNation)
        {

            if (GetNationTag(fac) != null)
            {
                if (capturingNation.Equals(GetNationTag(fac)) || capturingNation.Equals(""))
                    capturingNation = GetNationTag(fac);
                else
                {
                    return true;

                }
            }
            else
            {
                //unaff cant capture
                return true;
            }
            return false;
        }


        private static TorchSessionState state1;
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            if (state == TorchSessionState.Loaded)
            {
                state1 = state;
                if (!System.IO.File.Exists(this.StoragePath + "//CrunchKOTH"))
                {
                    System.IO.Directory.CreateDirectory(this.StoragePath + "//CrunchKOTH");
                }

                //if (System.IO.File.Exists(this.StoragePath + "//CrunchKOTH//koths.csv"))
                //{
                //    String[] line = File.ReadAllLines(this.StoragePath + "//CrunchKOTH//koths.csv");

                //    for (int i = 1; i < line.Length; i++)
                //    {

                //        String[] split = line[i].Split(',');

                //    }
                //}
                //else
                //{
                // //   StringBuilder easy = new StringBuilder();
                //  //  easy.AppendLine("Name, X, Y, Z, Koth Building Fac Tag, LootBoxType, LootBoxSubtype, Minutes Warmup Before Cap, Capture Radius Metres, Seconds Between Core Spawn, Seconds Between Capture Check, Points //Per Cap, Points To Cap, Mins Per Broadcast, Owner, Capture Block Type, Capture Block Subtype, Capture Block Needs To Be Turned On, Capture Block Needs To Broadcast, Fail Cooldown, Hours Lock //Aftercap, capturemessage, capturemessagecomplete, dochatmessages, dodiscordmessages, discordid");
                //  //  easy.AppendLine("EasyIngot1,Ingot,Iron,1,10,20,50,50,3");



                //    if (!System.IO.File.Exists(this.StoragePath + "//CrunchKOTH//koths.csv"))
                //    {
                //        File.WriteAllText(this.StoragePath + "//CrunchKOTH//koths.csv", easy.ToString());
                //    }

                //}
            }
        }
        int tick = 0;
        private static string path = "";
        public static void LoadConfig()
        {
            FileUtils utils = new FileUtils();
            foreach (String s in Directory.GetFiles(Path.Combine(path + "//CrunchKOTH//")))
            {
                KOTHs.Add(utils.ReadFromXmlFile<KothConfig>(path + "//CrunchKOTH//" + s + ".xml"));
            }
        }

        public static KothConfig SaveConfig(String name, KothConfig config)
        {
            FileUtils utils = new FileUtils();
            utils.WriteToXmlFile<KothConfig>(path + "//CrunchKOTH" + name+".xml", config);

            return config;
        }
        private void SetupConfig()
        {
            path = this.StoragePath;
            KothConfig config = new KothConfig();
            FileUtils utils = new FileUtils();
            if (File.Exists(this.StoragePath + "//CrunchKOTH//example.xml"))
            {
                config = utils.ReadFromXmlFile<KothConfig>(this.StoragePath + "//CrunchKOTH//config.xml");
                utils.WriteToXmlFile<KothConfig>(this.StoragePath + "//CrunchKOTH//example.xml", config, false);
            }
            else
            {
                config = new KothConfig();
                utils.WriteToXmlFile<KothConfig>(this.StoragePath + "//CrunchKOTH//example.xml", config, false);
            }
        }
        public static MyCubeGrid GetLootboxGrid(Vector3 position, KothConfig config)
        {
            if (MyAPIGateway.Entities.GetEntityById(config.LootboxGridEntityId) != null)
            {
                if (MyAPIGateway.Entities.GetEntityById(config.LootboxGridEntityId) is MyCubeGrid grid)
                    return grid;
            }
            BoundingSphereD sphere = new BoundingSphereD(position, config.CaptureRadiusInMetre + 5000);
            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if (fac != null && fac.Tag.Equals(config.KothBuildingOwner))
                {

                    Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    Sandbox.ModAPI.IMyTerminalBlock block = gridTerminalSys.GetBlockWithName(config.LootBoxTerminalName);
                    if (block != null)
                    {
                        return grid;
                    }

                }

            }
             return null;
        }
        public override void Update()
        {
            try
            {

                if (state1 != TorchSessionState.Loaded)
                {
                    return;
                }
                foreach (KothConfig config in KOTHs)
                {
                    if (!config.enabled)
                        continue;

                    bool contested = false;
                    Boolean hasActiveCaptureBlock = false;
                    // Log.Info("We capping?");
                    Vector3 position = new Vector3(config.x, config.y, config.z);
                    BoundingSphereD sphere = new BoundingSphereD(position, config.CaptureRadiusInMetre);

                    if (DateTime.Now >= config.nextCaptureInterval)
                    {

                        //setup a time check for capture time
                        String capturingNation = "";

                        Boolean locked = false;

                        Log.Info("Yeah we capping");
                        //check grids first
                        List<MyCubeGrid> acmeGrids = new List<MyCubeGrid>();
                        List<MyCubeGrid> NotAcmeGrids = new List<MyCubeGrid>();


                        int entitiesInCapPoint = 0;
                        foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                        {
                            entitiesInCapPoint++;
                            if (!contested)
                            {
                                IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                                if (fac != null && !fac.Tag.Equals(config.KothBuildingOwner))
                                {

                                    if (IsContested(fac, config, capturingNation))
                                    {
                                        contested = true;
                                        break;
                                    }
                                    else
                                    {
                                        capturingNation = GetNationTag(fac);
                                    }

                                }
                                hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, config);
                            }
                        }

                        if (!contested)
                        {
                            //now check characters
                            foreach (MyCharacter character in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCharacter>())
                            {
                                entitiesInCapPoint++;
                                IMyFaction fac = FacUtils.GetPlayersFaction(character.GetPlayerIdentityId());
                                if (fac != null)
                                {
                                    float distance = Vector3.Distance(position, character.PositionComp.GetPosition());
                                    if (IsContested(fac, config, capturingNation))
                                    {
                                        contested = true;
                                        break;
                                    }
                                    else
                                    {
                                        capturingNation = GetNationTag(fac);
                                    }
                                }
                                else
                                {
                                    contested = true;
                                }
                            }
                        }

                        if (entitiesInCapPoint == 0 && config.IsDenialPoint)
                        {
                            if (denials.TryGetValue(config.DeniedKoth, out DenialPoint den))
                            {
                                den.RemoveCap(config.KothName);
                                SaveConfig(config.KothName, config);
                            }
                        }
                        if (!contested && hasActiveCaptureBlock && !config.CaptureStarted && !capturingNation.Equals(""))
                        {
                            config.CaptureStarted = true;
                            config.nextCaptureAvailable = DateTime.Now.AddMinutes(config.MinutesBeforeCaptureStarts);
                            Log.Info("Can cap in 10 minutes");
                            config.capturingNation = capturingNation;
                            SendChatMessage("Can cap in however many minutes");
                        }
                        else
                        {
                            if (!contested && !capturingNation.Equals(""))
                            {
                                Log.Info("Got to the capping check and not contested");
                                if (DateTime.Now >= config.nextCaptureAvailable && config.CaptureStarted)
                                {
                                    if (config.capturingNation.Equals(capturingNation) && !config.capturingNation.Equals(""))
                                    {
                                        Log.Info("Is the same nation as whats capping");
                                        if (!hasActiveCaptureBlock)
                                        {
                                            Log.Info("Locking because no active cap block");
                                            config.capturingNation = config.owner;
                                            config.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                            //broadcast that its locked
                                            config.capturingNation = "";
                                            config.amountCaptured = 0;
                                            SendChatMessage("Locked because capture blocks are dead");
                                        }
                                        else
                                        {
                                            config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                                            if (config.IsDenialPoint)
                                            {
                                                if (denials.TryGetValue(config.DeniedKoth, out DenialPoint den))
                                                {
                                                    den.AddCap(config.KothName);
                                                }
                                                else
                                                {
                                                    DenialPoint denial = new DenialPoint();
                                                    denial.AddCap(config.KothName);
                                                    denials.Add(config.DeniedKoth, denial);
                                                }
                                                //exit this one because its a denial point and continue to the next config
                                                continue;
                                            }
                                            config.amountCaptured += config.PointsPerCap;
                               
                                            if (config.amountCaptured >= config.PointsToCap)
                                            {
                                                //lock
                                                Log.Info("Locking because points went over the threshold");
                                                locked = true;
                                                config.nextCaptureInterval = DateTime.Now.AddHours(config.hoursToLockAfterCap);
                                                config.capturingNation = capturingNation;
                                                config.owner = capturingNation;
                                                config.amountCaptured = 0;
                                                SendChatMessage(config.captureMessage.Replace("%NATION%", config.owner));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log.Info("Locking because the capturing nation changed");
                                        config.capturingNation = config.owner;
                                        config.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                        //broadcast that its locked
                                        SendChatMessage("Locked because capturing nation has changed.");
                                        config.amountCaptured = 0;
                      
                                    }
                                }
                                else
                                {
                                    SendChatMessage("Waiting to cap");
                                    Log.Info("Waiting to cap");
                                }
                            }
                            else
                            {
                                Log.Info("Its contested or the fuckers trying to cap have no nation");
                                //send contested message
                                SendChatMessage("Contested or unaff trying to cap");
                            }


                        }

                        if (!locked)
                        {
                            config.nextCaptureInterval = DateTime.Now.AddSeconds(config.SecondsBetweenCaptureCheck);
                        }
                        SaveConfig(config.KothName, config);
                    }
               

                //if its not locked, check again for capture in a minute



                if (DateTime.Now > config.nextCoreSpawn && !config.IsDenialPoint)
                {
                    MyCubeGrid lootgrid = GetLootboxGrid(position, config);
                    //spawn the cores
                    foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                    {
                        IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                        if (fac != null)
                        {
                            if (GetNationTag(fac) != null && GetNationTag(fac).Equals(config.owner))
                            {
                                hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, config);
                            }

                        }

                    }
                    if (denials.TryGetValue(config.KothName, out DenialPoint den))
                    {
                        if (den.IsDenied())
                            SendChatMessage("Denied point, no core spawn");
                        continue;
                    }
                    if (!config.owner.Equals("NOBODY"))
                    {

                        if (hasActiveCaptureBlock)
                        {
                            Log.Info("The owner has an active block so reducing time between spawning");
                            SpawnCores(lootgrid, config);
                            config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn / 2);
                            SendChatMessage("Capture block and owned, half spawn time");
                        }
                        else
                        {
                            Log.Info("No block");
                            SpawnCores(lootgrid, config);
                            SendChatMessage("No capture block and owned, normal spawn time");
                            config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                        }
                    }
                    else
                    {
                        Log.Info("No owner, normal spawn time");
                        SpawnCores(lootgrid, config);
                        config.nextCoreSpawn = DateTime.Now.AddSeconds(config.SecondsBetweenCoreSpawn);
                        SendChatMessage("No owner, normal spawn time");
                    }

                }

                contested = false;
                hasActiveCaptureBlock = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("koth error " + ex.ToString());
            }
        }
        public static void SpawnCores(MyCubeGrid grid, KothConfig config)
        {
            if (grid != null)
            {
                Sandbox.ModAPI.IMyGridTerminalSystem gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                MyDefinitionId rewardItem = getRewardItem(config);
                Sandbox.ModAPI.IMyTerminalBlock block = gridTerminalSys.GetBlockWithName(config.LootBoxTerminalName);
                if (block != null && rewardItem != null)
                {
                    Log.Info("Should spawn item");
                    MyItemType itemType = new MyInventoryItemFilter(rewardItem.TypeId + "/" + rewardItem.SubtypeName).ItemType;
                    block.GetInventory().AddItems((MyFixedPoint) config.RewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(rewardItem));

                }
                else
                {
                    Log.Info("Cant spawn item");
                }
                return;
            }



        }

        public static MyDefinitionId getRewardItem(KothConfig config)
        {
            MyDefinitionId.TryParse("MyObjectBuilder_" + config.RewardTypeId, config.RewardSubTypeId, out MyDefinitionId id);
            return id;
        }
        public static void SendChatMessage(String message, ulong steamID = 0)
        {
            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = "KOTH";
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = Color.OrangeRed;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId(steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
    }
}
