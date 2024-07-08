using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    internal class GameRoom
    {
        object _lock = new object();
        public int RoomID { get; set; }

        List<Player> _players = new List<Player>();

        public void EnterGame(Player newPlayer)
        {
            if(newPlayer == null) { return; }

            lock (_lock)
            {
                _players.Add(newPlayer);
                newPlayer.Room = this;

                // 본인한테 정보 전송
                { 
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players)
                    {
                        if (newPlayer != p)
                        {
                            spawnPacket.Players.Add(p.Info);
                        }
                    }
                    newPlayer.Session.Send(spawnPacket);
                }
                // 타인한테 정보 전송
                { 
                    S_Spawn spawnpacket = new S_Spawn();
                    spawnpacket.Players.Add(newPlayer.Info);
                    foreach (Player p in _players)
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
                Player player = _players.Find(p => p.Info.PlayerId == playerId);
                if(player == null) { return;}

                _players.Remove(player);
                player.Room = null;

                // 본인한테 정보 전송
                {
                    S_LeaveGame LeavePacket = new S_LeaveGame();
                    player.Session.Send(LeavePacket);
                }
                // 타인한테 정보 전송
                {
                    S_Despawn despawnpacket = new S_Despawn();
                    despawnpacket.PlayerIds.Add(player.Info.PlayerId);
                    foreach (Player p in _players)
                    {
                        if(player != p)
                        {
                            p.Session.Send(despawnpacket);
                        }                        
                    }
                }
            }
        }

        public void Broadcast(IMessage packet)
        {
            lock (_lock)
            {
                foreach (Player p in _players)
                {
                    p.Session.Send(packet);
                }
            }
        }
    }
}
