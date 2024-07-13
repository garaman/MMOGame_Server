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
            Speed = 10.0f;
        
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            Console.WriteLine($"Attacker : {attacker.Info.Name} /Damage : {damage}");
            base.OnDamaged(attacker, damage);
        }

    }
}
