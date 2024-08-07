﻿using Google.Protobuf.Protocol;
using Server.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    public class Zone
    {
        public int IndexY {  get; private set; }
        public int IndexX {  get; private set; }

        public HashSet<Player> Players { get; set; } = new HashSet<Player>();
        public HashSet<Monster> Monsters { get; set; } = new HashSet<Monster>();
        public HashSet<Projectile> Projectiles { get; set; } = new HashSet<Projectile>();
        public HashSet<Npc> Npcs { get; set; } = new HashSet<Npc>();

        public Zone(int y, int x)
        {
            IndexY = y;
            IndexX = x;
        }

        public void Remove(GameObject gameObject)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            switch(type)
            {
                case GameObjectType.Player:
                    Players.Remove((Player)gameObject);
                    break;
                case GameObjectType.Monster:
                    Monsters.Remove((Monster)gameObject);
                    break;
                case GameObjectType.Projectile:
                    Projectiles.Remove((Projectile)gameObject);
                    break;
                case GameObjectType.Npc:
                    Npcs.Remove((Npc)gameObject);
                    break;
            }
        }

        public Player FindOnePlayer(Func<Player, bool> condition)
        {            
            foreach (Player player in Players)
            {
                if (condition.Invoke(player))
                {
                    return player;
                }
            }
            return null;
        }

        public List<Player> FindAllPlayers(Func<Player, bool> condition)
        {
            List<Player> findlist = new List<Player>();
            foreach (Player player in Players)
            {
                if(condition.Invoke(player))
                {
                    findlist.Add(player); 
                }
            }
            return findlist;
        }
    }
}
