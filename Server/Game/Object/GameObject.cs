﻿using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class GameObject
    {
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
        public int Id
        {
            get { return Info.ObjectId;  } 
            set { Info.ObjectId = value; }
        }
        public int TemplateId 
        {
            get { return Info.TemplateId; }
            set { Info.TemplateId = value; }
        }

        public GameRoom Room { get; set; }
        public ObjectInfo Info { get; set; } = new ObjectInfo();
        public PositionInfo PosInfo { get; private set; } = new PositionInfo();
        public StatInfo Stat { get; private set; } = new StatInfo();

        public virtual int TotalAttack { get { return Stat.Attack; } }
        public virtual int TotalDefence { get { return 0; } }
        public float Speed
        {
            get { return Stat.Speed; }
            set { Stat.Speed = value; }
        }

        public int Hp
        {
            get { return Stat.Hp; }
            set { Stat.Hp = Math.Clamp(value, 0, Stat.Hp); }
        }

        public MoveDir Dir
        {
            get { return PosInfo.MoveDir; }
            set { PosInfo.MoveDir = value; }
        }

        public CreatureState State 
        {
            get { return PosInfo.State; }
            set { PosInfo.State = value; }
        }
        public Vector2Int CellPos
        {
            get
            {
                return new Vector2Int(PosInfo.PosX, PosInfo.PosY);
            }

            set
            {
                PosInfo.PosX = value.x;
                PosInfo.PosY = value.y;
            }
        }

        public GameObject()
        {
            Info.PosInfo = PosInfo;
            Info.StatInfo = Stat;
        }

        public virtual void init(int templateId)
        {
            TemplateId = templateId;
        }

        public Vector2Int GetFrontCellPos()
        {
            return GetFrontCellPos(PosInfo.MoveDir);
        }

        public Vector2Int GetFrontCellPos(MoveDir dir)
        {
            Vector2Int cellPos = CellPos;

            switch (dir)
            {
                case MoveDir.Up:
                    cellPos += Vector2Int.up;
                    break;
                case MoveDir.Down:
                    cellPos += Vector2Int.down;
                    break;
                case MoveDir.Left:
                    cellPos += Vector2Int.left;
                    break;
                case MoveDir.Right:
                    cellPos += Vector2Int.right;
                    break;
            }
            return cellPos;
        }
        public static MoveDir GetDirFromVec(Vector2Int dir)
        {
            if (dir.x > 0) { return MoveDir.Right; }
            else if (dir.x < 0) { return MoveDir.Left; }
            else if (dir.y > 0) { return MoveDir.Up; }
            else { return MoveDir.Down; }
        }

        public virtual void Update()
        {

        }

        public virtual void OnDamaged(GameObject attacker, int damage)
        {
            if (Room == null) { return; }

            damage = Math.Max(damage - TotalDefence,0);
            Stat.Hp = Math.Max(Stat.Hp - damage, 0);

            //Console.WriteLine($"{Info.Name} / {Stat.Hp}");

            S_ChangeHp chagePacket = new S_ChangeHp();
            chagePacket.ObjectId = Id;
            chagePacket.Hp = Stat.Hp;
            Room.Broadcast(CellPos, chagePacket);
            
            if (Stat.Hp <= 0)
            {                
                OnDead(attacker);
            }
        }

        public virtual void OnDead(GameObject attacker)
        {
            if (Room == null) { return; }

            S_Die diePacket = new S_Die();
            diePacket.ObjectId = Id;
            diePacket.AttackerId = attacker.Id;
            Room.Broadcast(CellPos, diePacket);

            GameRoom room = Room;
            room.LeaveGame(Id);

            Stat.Hp = Stat.MaxHp;
            PosInfo.State = CreatureState.Idle;
            PosInfo.MoveDir = MoveDir.Down;
            PosInfo.PosX = 0;
            PosInfo.PosY = 0;

            room.EnterGame(this, randomPos: true);
        }

        public virtual GameObject GetOwner()
        {
            return this;
        }


    }
}
