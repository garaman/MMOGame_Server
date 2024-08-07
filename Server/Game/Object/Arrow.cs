﻿using Google.Protobuf.Protocol;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class Arrow : Projectile
    {
        public GameObject Owner {  get; set; }
                

        public override void Update()
        {            
            if (Data == null || Data.projectileInfo == null || Owner == null || Room == null) { return; }  
            
            int tick = (int)(1000 / Data.projectileInfo.speed);
            Room.PushAfter(tick, Update);
            

            Vector2Int destPos = GetFrontCellPos();
            if (Room.Map.ApplyMove(this, destPos, collision: false))
            {                
                S_Move movePacket = new S_Move();
                movePacket.ObjectId = Id;
                movePacket.PosInfo = PosInfo;
                Room.Broadcast(CellPos, movePacket);

                //Console.WriteLine("Move Arrow");
            }
            else
            {
                GameObject target = Room.Map.Find(destPos);
                if (target != null)
                {
                    target.OnDamaged(this, Data.damage + Owner.TotalAttack);                
                }
                
                Room.Push(Room.LeaveGame,Id);
            }

        }

        public override GameObject GetOwner()
        {
            return Owner;
        }
    }
}
