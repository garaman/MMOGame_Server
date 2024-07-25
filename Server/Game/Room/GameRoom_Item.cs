using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {  
        public void HandleEquipItem(Player player, C_EquipItem equipPacket)
        {
            if (player == null) { return; }
                       
            player.HandleEquipItem(equipPacket);
        }
        
    }
}
