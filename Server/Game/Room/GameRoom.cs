﻿using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
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
        public int RoomID { get; set; }

        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();
        
        public Map Map { get; private set; } = new Map();

        public void Init(int mapId)
        {
            Map.LoadMap(mapId);

            // 임시
            Monster monster = ObjectManager.Instance.Add<Monster>();
            monster.init(1);
            monster.CellPos = new Vector2Int(5, 5);
            this.EnterGame(monster);
        }

        public void Update()
        {
            foreach (Monster m in _monsters.Values)
            {
                m.Update();
            }

            Flush();
        }
        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null) { return; }

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
            
            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player);
                player.Room = this;
                player.RefreshAddionalStat();

                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));
                // 본인한테 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
                    {
                        if (player != p)
                        {
                            spawnPacket.Objects.Add(p.Info);
                        }
                    }
                    foreach (Monster m in _monsters.Values)
                    {                            
                        spawnPacket.Objects.Add(m.Info);
                         
                    }
                    foreach (Projectile p in _projectiles.Values)
                    {                          
                        spawnPacket.Objects.Add(p.Info);                        
                    }

                    player.Session.Send(spawnPacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;
                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;

                projectile.Update();
            }

            // 타인한테 정보 전송
            {
                S_Spawn spawnpacket = new S_Spawn();
                spawnpacket.Objects.Add(gameObject.Info);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != gameObject.Id)
                    {
                        p.Session.Send(spawnpacket);
                    }
                }
            }            
        }
        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);
            
            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false) { return; }

                player.OnLeavGame();
                Map.ApplyLeave(player);
                player.Room = null;
                // 본인한테 정보 전송
                {
                    S_LeaveGame LeavePacket = new S_LeaveGame();
                    player.Session.Send(LeavePacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = null;
                if(_monsters.Remove(objectId, out monster) == false) { return; }
                                
                Map.ApplyLeave(monster);
                monster.Room = null;
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if(_projectiles.Remove(objectId, out projectile) == false) { return; }

                projectile.Room = null;
            }
                

            // 타인한테 정보 전송
            {
                S_Despawn despawnpacket = new S_Despawn();
                despawnpacket.ObjectIds.Add(objectId);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != objectId)
                    {
                        p.Session.Send(despawnpacket);
                    }
                }
            }            
        }        
        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if(condition.Invoke(player))
                    return player;
            }
            return null;
        }
        public void Broadcast(IMessage packet)
        {           
            foreach (Player p in _players.Values)
            {
                p.Session.Send(packet);
            }            
        }
        
    }
}
