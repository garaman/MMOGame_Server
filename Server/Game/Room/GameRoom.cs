using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    public class GameRoom
    {
        object _lock = new object();
        public int RoomID { get; set; }

        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Map _map = new Map();

        public void Init(int mapId)
        {
            _map.LoadMap(mapId);
        }

        public void EnterGame(Player newPlayer)
        {
            if (newPlayer == null) { return; }

            lock (_lock)
            {
                _players.Add(newPlayer.Info.ObjectId, newPlayer);
                newPlayer.Room = this;

                // 본인한테 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
                    {
                        if (newPlayer != p)
                        {
                            spawnPacket.Objects.Add(p.Info);
                        }
                    }
                    newPlayer.Session.Send(spawnPacket);
                }
                // 타인한테 정보 전송
                {
                    S_Spawn spawnpacket = new S_Spawn();
                    spawnpacket.Objects.Add(newPlayer.Info);
                    foreach (Player p in _players.Values)
                    {
                        if (newPlayer != p)
                        {
                            p.Session.Send(spawnpacket);
                        }
                    }
                }
            }
        }

        public void LeaveGame(int playerId)
        {
            lock (_lock)
            {
                Player player = null;
                if (_players.Remove(playerId, out player) == false) { return; }

                player.Room = null;

                // 본인한테 정보 전송
                {
                    S_LeaveGame LeavePacket = new S_LeaveGame();
                    player.Session.Send(LeavePacket);
                }
                // 타인한테 정보 전송
                {
                    S_Despawn despawnpacket = new S_Despawn();
                    despawnpacket.ObjectId.Add(player.Info.ObjectId);
                    foreach (Player p in _players.Values)
                    {
                        if (player != p)
                        {
                            p.Session.Send(despawnpacket);
                        }
                    }
                }
            }
        }


        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null) { return; }

            lock (_lock)
            {
                // 서버에서 좌표이동.
                PositionInfo movePosInfo = movePacket.PosInfo;
                ObjectInfo info = player.Info;

                if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
                {
                    if (_map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false) { return; }
                }

                info.PosInfo.State = movePosInfo.State;
                info.PosInfo.MoveDir = movePosInfo.MoveDir;
                _map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

                // 다른 플레이어에게 이동정보 전송.
                S_Move resMovePacket = new S_Move();
                resMovePacket.ObjectId = player.Info.ObjectId;
                resMovePacket.PosInfo = player.Info.PosInfo;

                Broadcast(resMovePacket);
            }
        }

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null) { return; }

            lock (_lock)
            {
                // 서버에서 좌표이동.
                ObjectInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle) { return; }

                info.PosInfo.State = CreatureState.Skill;

                S_Skill skill = new S_Skill() { Info = new SkillInfo() };
                skill.PlayerId = info.ObjectId;
                skill.Info.SkillId = skillPacket.Info.SkillId;

                Broadcast(skill);

                if (skillPacket.Info.SkillId == 1)
                {
                    // TODO 데이지 판정.
                    Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                    Player target = _map.Find(skillPos);
                    if (target != null)
                    {
                        Console.WriteLine("Hit Player!!");
                    }
                }
                else if (skillPacket.Info.SkillId == 2)
                {

                }




            }
        }

        public void Broadcast(IMessage packet)
        {
            lock (_lock)
            {
                foreach (Player p in _players.Values)
                {
                    p.Session.Send(packet);
                }
            }
        }
    }
}
