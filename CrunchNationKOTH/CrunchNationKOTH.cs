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

namespace CrunchNationKOTH
{
    public class CrunchNationKOTH : TorchPluginBase
    {
        private static List<KothConfig> KOTHs = new List<KothConfig>();
        private static List<DateTime> captureIntervals = new List<DateTime>();
        private static Dictionary<String, int> amountCaptured = new Dictionary<String, int>();
        public static KothConfig koth = new KothConfig();
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
                        if (block is IMyFunctionalBlock bl)
                        {
                            bl.Enabled = true;
                        }

                        if (block is IMyBeacon beacon)
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

        public static Logger Log = LogManager.GetCurrentClassLogger();

        private TorchSessionManager sessionManager;
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Log.Info("Loading Crunch Koth");
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
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

        public static MyCubeGrid GetLootboxGrid(Vector3 position)
        {
            if (MyAPIGateway.Entities.GetEntityById(koth.LootboxGridEntityId) != null)
            {
                if (MyAPIGateway.Entities.GetEntityById(koth.LootboxGridEntityId) is MyCubeGrid grid)
                    return grid;
            }
            BoundingSphereD sphere = new BoundingSphereD(position, koth.CaptureRadiusInMetre + 5000);
            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if (fac != null && fac.Tag.Equals(koth.KothBuildingOwner))
                {
                    foreach (MyCubeBlock block in grid.GetFatBlocks())
                    {

                        Log.Info(block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + " " + block.BlockDefinition.Id.SubtypeName);
                        if (block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "").Equals(koth.lootBoxTypeId) && block.BlockDefinition.Id.SubtypeName.Equals(koth.captureBlockSubtype))
                        {
                            return grid;
                        }
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
                bool contested = false;
                Boolean hasActiveCaptureBlock = false;
                // Log.Info("We capping?");
                Vector3 position = new Vector3(koth.x, koth.y, koth.z);
                BoundingSphereD sphere = new BoundingSphereD(position, koth.CaptureRadiusInMetre);
             
                if (DateTime.Now >= koth.nextCaptureInterval)
                {

                    //setup a time check for capture time
                    String capturingNation = "";
                  
                    Boolean locked = false;

                    Log.Info("Yeah we capping");
                    //check grids first
                    List<MyCubeGrid> acmeGrids = new List<MyCubeGrid>();
                    List<MyCubeGrid> NotAcmeGrids = new List<MyCubeGrid>();



                    foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                    {
                        if (!contested)
                        {
                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            if (fac != null && !fac.Tag.Equals(koth.KothBuildingOwner))
                            {

                                if (IsContested(fac, koth, capturingNation))
                                {
                                    contested = true;
                                    break;
                                }
                                else
                                {
                                    capturingNation = GetNationTag(fac);
                                }

                            }
                            hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, koth);
                        }
                    }

                    if (!contested)
                    {
                        //now check characters
                        foreach (MyCharacter character in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCharacter>())
                        {

                            IMyFaction fac = FacUtils.GetPlayersFaction(character.GetPlayerIdentityId());
                            if (fac != null)
                            {
                                float distance = Vector3.Distance(position, character.PositionComp.GetPosition());
                                if (IsContested(fac, koth, capturingNation))
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

                    if (!contested && hasActiveCaptureBlock && !koth.CaptureStarted && !capturingNation.Equals(""))
                    {
                        koth.CaptureStarted = true;
                        koth.nextCaptureAvailable = DateTime.Now.AddMinutes(koth.MinutesBeforeCaptureStarts);
                        Log.Info("Can cap in 10 minutes");
                        koth.capturingNation = capturingNation;
                        SendChatMessage("Can cap in however many minutes");
                    }
                    else
                    {
                        if (!contested && !capturingNation.Equals(""))
                        {
                            Log.Info("Got to the capping check and not contested");
                            if (DateTime.Now >= koth.nextCaptureAvailable && koth.CaptureStarted)
                            {
                                if (koth.capturingNation.Equals(capturingNation) && !koth.capturingNation.Equals(""))
                                {
                                    Log.Info("Is the same nation as whats capping");
                                    if (!hasActiveCaptureBlock)
                                    {
                                        Log.Info("Locking because no active cap block");
                                        koth.capturingNation = koth.owner;
                                        koth.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                        //broadcast that its locked
                                        koth.capturingNation = "";
                                        koth.amountCaptured = 0;
                                        SendChatMessage("Locked because capture blocks are dead");
                                    }
                                    else
                                    {
                                        koth.nextCaptureInterval = DateTime.Now.AddSeconds(koth.SecondsBetweenCaptureCheck);
                                        koth.amountCaptured += koth.PointsPerCap;
                                        if (koth.amountCaptured >= koth.PointsToCap)
                                        {
                                            //lock
                                            Log.Info("Locking because points went over the threshold");
                                            locked = true;
                                            koth.nextCaptureInterval = DateTime.Now.AddHours(koth.hoursToLockAfterCap);
                                            koth.capturingNation = capturingNation;
                                            koth.owner = capturingNation;
                                            koth.amountCaptured = 0;
                                            SendChatMessage(koth.captureMessage.Replace("%NATION%", koth.owner));
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Info("Locking because the capturing nation changed");
                                    koth.capturingNation = koth.owner;
                                    koth.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                    //broadcast that its locked
                                    SendChatMessage("Locked because capturing nation has changed.");
                                    koth.amountCaptured = 0;
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
                        koth.nextCaptureInterval = DateTime.Now.AddSeconds(koth.SecondsBetweenCaptureCheck);
                    }
                }

                //if its not locked, check again for capture in a minute



                if (DateTime.Now > koth.nextCoreSpawn)
                {
                    MyCubeGrid lootgrid = GetLootboxGrid(position);
                    //spawn the cores
                    foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                    {
                        IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                        if (fac != null)
                        {
                            if (GetNationTag(fac) != null && GetNationTag(fac).Equals(koth.owner))
                            {
                                hasActiveCaptureBlock = DoesGridHaveCaptureBlock(grid, koth);
                            }

                        }

                    }
                    if (!koth.owner.Equals("NOBODY"))
                    {

                        if (hasActiveCaptureBlock)
                        {
                            Log.Info("The owner has an active block so reducing time between spawning");
                            koth.nextCoreSpawn = DateTime.Now.AddSeconds(koth.SecondsBetweenCoreSpawn / 2);
                            SendChatMessage("Capture block and owned, half spawn time");
                        }
                        else
                        {
                            Log.Info("No block");
                            SendChatMessage("No capture block and owned, normal spawn time");
                            koth.nextCoreSpawn = DateTime.Now.AddSeconds(koth.SecondsBetweenCoreSpawn);
                        }
                    }
                    else
                    {
                        Log.Info("No owner, normal spawn time");
                        koth.nextCoreSpawn = DateTime.Now.AddSeconds(koth.SecondsBetweenCoreSpawn);
                        SendChatMessage("No owner, normal spawn time");
                    }

                }

                contested = false;
                hasActiveCaptureBlock = false;

            }
            catch (Exception ex)
            {
                Log.Error("koth error " + ex.ToString());
            }
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
