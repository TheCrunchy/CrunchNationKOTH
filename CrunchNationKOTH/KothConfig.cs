﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchNationKOTH
{
    public class KothConfig
    {
        public double x = 100000;
        public double y = 100000;
        public double z = 100000;
        public string KothName = "Thannian";

        public string KothBuildingOwner = "BOB";
        public long LootboxGridEntityId = 0;
        public string LootBoxTerminalName = "LOOT BOX";
        public string capturingNation = "";
        public int amountCaptured = 0;
        public int MinutesBeforeCaptureStarts = 10;
        public int CaptureRadiusInMetre = 20;
        public int SecondsBetweenCoreSpawn = 180;
        public int SecondsBetweenCaptureCheck = 60;
        public int PointsPerCap = 10;
        public int PointsToCap = 100;
        public  int MinsPerCaptureBroadcast = 5;
        public string owner = "NOBODY";
        public string captureBlockType = "Beacon";
        public string captureBlockSubtype = "LargeBlockBeacon";
        public Boolean captureBlockNeedsToBeTurnedOn = true;
        public Boolean captureBlockNeedsToBroadcast = true;
        public int captureBlockBroadcastDistance = 10000;
        public int hourCooldownAfterFail = 1;
        public int hoursToLockAfterCap = 12;
       
        public DateTime nextCaptureAvailable = DateTime.Now;
        public string captureMessage = "%NATION% is capturing the moooooooooon";
        public string captureCompleteMessage = "%NATION% has captured the mooooooon it is now locked for %HOURS%";
        public Boolean doChatMessages = true;
        public Boolean doDiscordMessages = true;
        public string DiscordChannelId = "";

        public DateTime nextCaptureInterval = DateTime.Now;
        public DateTime nextCoreSpawn = DateTime.Now;
        public DateTime nextBroadcast = DateTime.Now;
        public Boolean CaptureStarted = false;

    }
}
