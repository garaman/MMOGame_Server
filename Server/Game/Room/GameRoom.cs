using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Job;
using Server.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    public class GameRoom : JobSerializer
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
            monster.CellPos = new Vector2Int(5, 5);
            this.EnterGame(monster);
        }

        public void Update()
        {
            foreach (Monster m in _monsters.Values)
            {
                m.Update();
            }
            foreach (Projectile p in _projectiles.Values)
            {
                p.Update();
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


        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null) { return; }
           
            // 서버에서 좌표이동.
            PositionInfo movePosInfo = movePacket.PosInfo;
            ObjectInfo info = player.Info;

            if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
            {
                if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false) { return; }
            }

            info.PosInfo.State = movePosInfo.State;
            info.PosInfo.MoveDir = movePosInfo.MoveDir;
            Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

            // 다른 플레이어에게 이동정보 전송.
            S_Move resMovePacket = new S_Move();
            resMovePacket.ObjectId = player.Info.ObjectId;
            resMovePacket.PosInfo = player.Info.PosInfo;

            Broadcast(resMovePacket);
            
        }

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null) { return; }

            // 서버에서 좌표이동.
            ObjectInfo info = player.Info;
            if (info.PosInfo.State != CreatureState.Idle) { return; }

            info.PosInfo.State = CreatureState.Skill;

            S_Skill skill = new S_Skill() { Info = new SkillInfo() };
            skill.ObjectId = info.ObjectId;
            skill.Info.SkillId = skillPacket.Info.SkillId;

            Broadcast(skill);

            Data.Skill skillData = null;
            if(DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false) { return; }   

            switch(skillData.skillType)
            {
                case SkillType.SkillAuto:
                    {
                        // TODO 데미지 판정.
                        Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                        GameObject target = Map.Find(skillPos);
                        if (target != null)
                        {
                            Console.WriteLine("Hit GameObject!!");
                        }
                    }
                    break;
                case SkillType.SkillProjectile:
                    {
                        Arrow arrow = ObjectManager.Instance.Add<Arrow>();
                        if (arrow == null) { return; }

                        arrow.Owner = player;
                        arrow.Data = skillData;

                        arrow.PosInfo.State = CreatureState.Moving;
                        arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
                        arrow.PosInfo.PosX = player.PosInfo.PosX;
                        arrow.PosInfo.PosY = player.PosInfo.PosY;
                        arrow.Speed = skillData.projectileInfo.speed;

                        Push(EnterGame,arrow);
                    }
                    break;
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
