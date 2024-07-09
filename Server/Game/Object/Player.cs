using Google.Protobuf.Protocol;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class Player : GameObject
    {       
        public ClientSession Session { get; set; }

        public Player() 
        {
            ObjectType = GameObjectType.Player;
        
        }
        
    }
}
