using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace CrunchNationKOTH
{
    public class Commands : CommandModule
    {
        [Command("koth reload", "reload koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnlockKoth()
        {
            CrunchNationKOTH.LoadConfig();
            Context.Respond("Reloaded");
        }

        [Command("koth unlock", "unlock koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void UnlockKoth(string name)
        {
            foreach (KothConfig koth in CrunchNationKOTH.KOTHs)
            {
                if (koth.KothName.Equals(name))
                {
                    koth.nextCaptureAvailable = DateTime.Now;
                  koth.nextCaptureInterval = DateTime.Now;
                    Context.Respond("Unlocked the koth");
                }
              
            }
        }
        [Command("koth output", "unlock koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputAllKothNames()
        {
            foreach (KothConfig koth in CrunchNationKOTH.KOTHs)
            {
                Context.Respond(koth.KothName);

            }
        }
    }
}
