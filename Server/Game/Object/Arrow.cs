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

        long _nextMoveTick = 0;

        public override void Update()
        {            
            if (Owner == null || Room == null) { return; }  

            if(_nextMoveTick >= Environment.TickCount64) { return; }

            _nextMoveTick = Environment.TickCount64 + 50;

            Vector2Int destPos = GetFrontCellPos();
            if (Room.Map.CanGo(destPos))
            {
                CellPos = destPos;

                S_Move movePacket = new S_Move();
                movePacket.ObjectId = Id;
                movePacket.PosInfo = PosInfo;
                Room.Broadcast(movePacket);

                Console.WriteLine("Move Arrow");
            }
            else
            {
                GameObject target = Room.Map.Find(destPos);
                if (target != null)
                {

                }
                
                Room.LeaveGame(Id);
            }

        }
    }
}
