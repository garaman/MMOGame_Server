using Server;
using Server.Session;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
	public static void C_LeaveGameHandler(PacketSession session, IPacket packet)
	{        
		ClientSession clientSession = session as ClientSession;

        if (clientSession.Room == null)
        {
            return;
        }

        GameRoom room = clientSession.Room;
        room.Push( () => room.Leave(clientSession) );

        //clientSession.Room.Broadcast(clientSession, chatPacket.chat);
    }

    public static void C_MoveHandler(PacketSession session, IPacket packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.Room == null)
        {
            return;
        }

        GameRoom room = clientSession.Room;
        room.Push(() => room.Move(clientSession, movePacket));

        //clientSession.Room.Broadcast(clientSession, chatPacket.chat);
    }
}
