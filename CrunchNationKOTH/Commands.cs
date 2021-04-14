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
        [Command("koth unlock", "unlock koth")]
        [Permission(MyPromoteLevel.Admin)]
        public void ContractDetails()
        {
            CrunchNationKOTH.koth.nextCaptureAvailable = DateTime.Now;
            CrunchNationKOTH.koth.nextCaptureInterval = DateTime.Now;
          
        }
    }
}
