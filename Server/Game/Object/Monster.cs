using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class Monster : GameObject
    {
        public Monster() 
        {
            ObjectType = GameObjectType.Monster;
        }
    }
}
