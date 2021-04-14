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
                if (block.OwnerId > 0 && block.DefinitionId.Value.TypeId.Equals(koth.captureBlockType) && block.DefinitionId.Value.SubtypeName.Equals(koth.captureBlockSubtype))
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

        public static Boolean IsContested(IMyFaction fac, float distance, KothConfig koth, string capturingNation)
        {
            if (distance <= koth.CaptureRadiusInMetre)
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
                if (DateTime.Now >= koth.nextCaptureInterval)
                {
                    Vector3 position = new Vector3(koth.x, koth.y, koth.z);
                    //setup a time check for capture time


                    String capturingNation = "";
                    BoundingSphereD sphere = new BoundingSphereD(position, 100);
                    Boolean locked = false;
                    Log.Info("Yeah we capping");
                    //check grids first
                    List<MyCubeGrid> acmeGrids = new List<MyCubeGrid>();
                    List<MyCubeGrid> NotAcmeGrids = new List<MyCubeGrid>();
                    foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                    {
                        Log.Info("This is a grid");
                        IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                        if (fac != null)
                        {
                            if (fac.Tag.Equals("ACME"))
                            {
                                acmeGrids.Add(grid);
                            }
                            else
                            {
                                NotAcmeGrids.Add(grid);
                            }
                        }
                        else
                        {
                            NotAcmeGrids.Add(grid);
                        }
                    }

                    foreach (MyCubeGrid grid in NotAcmeGrids)
                    {
                        Log.Info("Not an acme grid");
                        if (!contested)
                        {
                            Log.Info("Not contested");
                            IMyFaction fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                            if (fac != null && fac.Tag.Equals("ACME"))
                            {

                                float distance = Vector3.Distance(position, grid.PositionComp.GetPosition());


                                if (IsContested(fac, distance, koth, capturingNation))
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
                        else
                        {
                            Log.Info("Contested");
                        }
                    }



                    if (!contested)
                    {
                        Log.Info("Characters not contested");
                        //now check characters
                        foreach (MyCharacter character in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCharacter>())
                        {
                            IMyFaction fac = FacUtils.GetPlayersFaction(character.GetPlayerIdentityId());
                            if (fac != null)
                            {
                                float distance = Vector3.Distance(position, character.PositionComp.GetPosition());
                                if (IsContested(fac, distance, koth, capturingNation))
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
                    else
                    {
                        Log.Info("Characters contested");
                    }
                    if (!contested && !capturingNation.Equals(""))
                    {
                        Log.Info("Got to the capping check and not contested");

                        if (koth.capturingNation.Equals("") || koth.capturingNation.Equals(capturingNation))
                        {
                            Log.Info("Is the same nation as whats capping");
                            if (!hasActiveCaptureBlock && DateTime.Now >= koth.nextCaptureAvailable)
                            {
                                Log.Info("Locking because no active cap block");
                                koth.capturingNation = koth.owner;
                                koth.nextCaptureAvailable = DateTime.Now.AddHours(1);
                                //broadcast that its locked
                                koth.capturingNation = "";
                                koth.amountCaptured = 0;
                            }
                            else
                            {
                                if (!contested && hasActiveCaptureBlock && DateTime.Now >= koth.nextCaptureAvailable)
                                {
                                    Log.Info("Adding points to capture time");
                                    koth.nextCaptureInterval = DateTime.Now.AddSeconds(koth.SecondsBetweenCaptureCheck);
                                    koth.amountCaptured += koth.PointsPerCap;
                                    if (koth.amountCaptured >= koth.PointsToCap)
                                    {
                                        //lock
                                        Log.Info("Locking because points went over the threshold");
                                        locked = true;
                                        koth.nextCaptureInterval = DateTime.Now.AddHours(koth.hoursToLockAfterCap);
                                        koth.capturingNation = "";
                                        koth.amountCaptured = 0;
                                    }
                                }
                                else
                                {
                                    koth.nextCaptureAvailable = DateTime.Now.AddMinutes(koth.MinutesBeforeCaptureStarts);
                                    Log.Info("Starting the cap");
                                    //broadcast capture starting in 10 minutes

                                }
                            }

                        }
                        else
                        {
                            Log.Info("Locking because the shit changed");
                            koth.capturingNation = koth.owner;
                            koth.nextCaptureAvailable = DateTime.Now.AddHours(1);
                            //broadcast that its locked
                            koth.capturingNation = "";
                            koth.amountCaptured = 0;
                        }
                    }
                    else
                    {
                        Log.Info("Its contested or the fuckers trying to cap have no nation");
                        //send contested message
                    }
                    if (!locked)
                    {
                        koth.nextCaptureInterval = DateTime.Now.AddSeconds(koth.SecondsBetweenCaptureCheck);
                    }
                }

                //if its not locked, check again for capture in a minute



                if (DateTime.Now > koth.nextCoreSpawn)
                {
                    //spawn the cores
                    if (contested)
                    {
                        Log.Info("Contested and spawning core");
                        koth.nextCoreSpawn = DateTime.Now.AddSeconds(koth.SecondsBetweenCoreSpawn);

                    }
                    else
                    {
                        if (koth.owner.Equals(koth.capturingNation) && hasActiveCaptureBlock)
                        {
                            Log.Info("The owner has an active block so reducing time between spawning");
                            koth.nextCoreSpawn = DateTime.Now.AddSeconds(koth.SecondsBetweenCoreSpawn / 2);

                        }
                        else
                        {
                            Log.Info("No block");
                            koth.nextCoreSpawn = DateTime.Now.AddSeconds(koth.SecondsBetweenCoreSpawn);
                        }
                    }
                }
                if (DateTime.Now > koth.nextBroadcast)
                {
                    koth.nextBroadcast = DateTime.Now.AddMinutes(koth.MinsPerCaptureBroadcast);
                    if (contested)
                    {
                        Log.Info("Should broadcast with contested");
                        //broadcast contested
                       

                    }
                    else
                    {
                        if (!koth.owner.Equals(koth.capturingNation))
                        {
                            Log.Info("Do message for being captured");
                           
                            //broadcast its being captured by someone else

                        }
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
    }
}
