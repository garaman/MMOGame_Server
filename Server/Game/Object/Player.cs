﻿using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Game;
using Server.Game.Room;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class Player : GameObject
    {
        public int PlayerDbId {  get; set; }
        public ClientSession Session { get; set; }
        public Inventory Inven { get; private set; } = new Inventory();

        public Player() 
        {
            ObjectType = GameObjectType.Player;
            Speed = 10.0f;
            if(Hp < 0) { Hp = 1; }

        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            base.OnDamaged(attacker, damage);          
            
        }

        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);
        }

        public void OnLeavGame()
        {
            DbTransaction.SavePalyerStatus(this, Room);
        }
    }
}
