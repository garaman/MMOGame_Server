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
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        public const int VisionCells = 10;
        public int RoomID { get; set; }

        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();
        Dictionary<int, Npc> _npcs = new Dictionary<int, Npc>();

        public Zone[,] Zones { get; set; }
        public int ZoneCells { get; set; }
        
        public Map Map { get; private set; } = new Map();

        public Zone GetZone(Vector2Int cellPos)
        {
            int x = (cellPos.x - Map.MinX) / ZoneCells;
            int y = (Map.MaxY - cellPos.y) / ZoneCells;
            
            return GetZone(y,x);
        }

        public Zone GetZone(int indexY, int indexX)
        {
            if (indexX < 0 || indexX >= Zones.GetLength(1)) { return null; }
            if (indexY < 0 || indexY >= Zones.GetLength(0)) { return null; }

            return Zones[indexY, indexX];
        }

        public void Init(int mapId, int zoneCells)
        {
            Map.LoadMap(mapId);

            // Zone 로딩.
            ZoneCells = zoneCells;
            int countY = ( Map.SizeY + zoneCells - 1 ) / zoneCells;
            int countX = ( Map.SizeX + zoneCells - 1 ) / zoneCells;
            Zones = new Zone[countY, countX];
            for(int y = 0; y < countY; y++)
            {
                for(int x = 0; x < countX; x++)
                {
                    Zones[y,x] = new Zone(y,x);
                }
            }

            RoomSetting(mapId);                              
        }

        public void Update()
        {
            Flush();
        }

        Random _rand = new Random();
        public void EnterGame(GameObject gameObject, bool randomPos)
        {
            if (gameObject == null) { return; }

            if (randomPos) 
            { 
                Vector2Int respawnPos;
                while (true)
                {
                    respawnPos.x = _rand.Next(Map.MinX, Map.MaxX);
                    respawnPos.y = _rand.Next(Map.MinY, Map.MaxY);

                    if (Map.Find(respawnPos) == null)
                    {
                        gameObject.CellPos = respawnPos;
                        break;
                    }
                }
            }
            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            if (type == GameObjectType.Player)
            {
                Player player = (Player)gameObject;
                _players.Add(gameObject.Id, player);
                gameObject.TemplateId = player.TemplateId;
                player.Room = this;
                player.RefreshAddionalStat();

                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));
                GetZone(player.CellPos).Players.Add(player);

                // 본인한테 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

                    player.Vision.Update();
                }
                
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = (Monster)gameObject;
                _monsters.Add(gameObject.Id, monster);
                gameObject.TemplateId = monster.TemplateId;
                monster.Room = this;
                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
                GetZone(monster.CellPos).Monsters.Add(monster);

                monster.Update();
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = (Projectile)gameObject;                
                _projectiles.Add(gameObject.Id, projectile);
                gameObject.TemplateId = projectile.TemplateId;
                projectile.Room = this;
                GetZone(projectile.CellPos).Projectiles.Add(projectile);

                projectile.Update();
            }
            else if (type == GameObjectType.Npc)
            {
                Npc npc = (Npc)gameObject;
                _npcs.Add(gameObject.Id, npc);
                gameObject.TemplateId = npc.TemplateId;
                npc.Room = this;
                Map.ApplyMove(npc, new Vector2Int(npc.CellPos.x, npc.CellPos.y));
                GetZone(npc.CellPos).Npcs.Add(npc);

                npc.Update();
            }

            // 타인한테 정보 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);
                Broadcast(gameObject.CellPos, spawnPacket);
            }
        }
    
        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);
            Vector2Int cellPos;
            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false) { return; }

                cellPos = player.CellPos;                

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

                cellPos = monster.CellPos;
                
                Map.ApplyLeave(monster);
                monster.Room = null;
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if(_projectiles.Remove(objectId, out projectile) == false) { return; }

                cellPos = projectile.CellPos;
                Map.ApplyLeave(projectile);
                projectile.Room = null;
            }
            else if (type == GameObjectType.Npc)
            {
                Npc npc = null;
                if (_npcs.Remove(objectId, out npc) == false) { return; }

                cellPos = npc.CellPos;

                Map.ApplyLeave(npc);
                npc.Room = null;
            }
            else
            {
                return;
            }

            // 타인한테 정보 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);
                Broadcast(cellPos, despawnPacket);
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
                
        public Player FindClosestPlayer(Vector2Int pos, int range)
        {
            List<Player> players = GetAdiacentPlayer(pos, range);
            players.Sort((left, right) =>
            {
                int leftDist = (left.CellPos - pos).CellDistFromZero;
                int rightDist = (right.CellPos - pos).CellDistFromZero;
                return leftDist - rightDist;
            });

            foreach (Player player in players)
            {
                List<Vector2Int> path = Map.FindPath(pos, player.CellPos, checkObjects: true);
                if (path.Count < 2 || path.Count > range) { continue; }
                return player;
            }

            return null;
        }

        public void Broadcast(Vector2Int pos, IMessage packet)
        {
            List<Zone> zones = GetAdiacentZones(pos);
                        
            foreach(Player player in zones.SelectMany(z => z.Players))
            {
                int dx = player.CellPos.x - pos.x;
                int dy = player.CellPos.y - pos.y;
                if (Math.Abs(dx) > VisionCells) { continue; }
                if (Math.Abs(dy) > VisionCells) { continue; }

                player.Session.Send(packet); 
            }
        }

        public List<Player> GetAdiacentPlayer(Vector2Int pos, int range = VisionCells)
        {
            List<Zone> zones = GetAdiacentZones(pos, range);
            return zones.SelectMany(z => z.Players).ToList();
        }

        public List<Zone> GetAdiacentZones(Vector2Int cellPos, int range = VisionCells)
        {
            HashSet<Zone> zones = new HashSet<Zone>();

            int maxY = cellPos.y + range;
            int minY = cellPos.y - range;
            int maxX = cellPos.x + range;
            int minX = cellPos.x - range;

            // 좌측 상단
            Vector2Int leftTop = new Vector2Int(minX, maxY);
            int minIndexY = (Map.MaxY - leftTop.y) / ZoneCells;
            int minIndexX = (leftTop.x - Map.MinX) / ZoneCells;        

            // 우측 하단
            Vector2Int rightBottom = new Vector2Int(maxX, minY);
            int maxIndexY = (Map.MaxY - rightBottom.y) / ZoneCells;
            int maxIndexX = (rightBottom.x - Map.MinX) / ZoneCells;

            for(int x = minIndexX; x <= maxIndexX; x++)
            {
                for(int y = minIndexY; y <= maxIndexY; y++)
                {
                    Zone zone = GetZone(y,x);
                    if(zone == null) { continue; }

                    zones.Add(zone);
                }
            }

            return zones.ToList();
        }

        void RoomSetting(int mapId)
        {
            switch (mapId)
            {
                case 1:
                    {
                        Npc npc = ObjectManager.Instance.Add<Npc>();
                        npc.init(1);
                        this.EnterGame(npc, randomPos: false);
                    }                    
                    break;
                case 2:
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Monster monster = ObjectManager.Instance.Add<Monster>();
                            monster.init(1);
                            this.EnterGame(monster, randomPos: true);
                        }
                    }
                    break;
            }
        }
    }
}
