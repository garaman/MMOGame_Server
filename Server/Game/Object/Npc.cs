using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using Server.Game.Room;
using ServerCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class Npc : GameObject
    {
        public NpcType npcType { get; private set; }
        public bool _active;

        public Npc() 
        {
            ObjectType = GameObjectType.Npc;
            npcType = NpcType.None;
        }

        public override void init(int templateId)
        {
            base.init(templateId);

            NpcData npcData = null;
            DataManager.NpcDict.TryGetValue(TemplateId, out npcData);
            Info.Name = $"{npcData.name}";
            npcType = npcData.npcType;
            Info.PosInfo.PosX = 3;
            Info.PosInfo.PosY = 3;

            State = CreatureState.Idle;
        }

        IJob _job;
        // FSM(Finite State Machine)
        public override void Update()
        {
            CheckPlayer();

            // 5프레임
            if (Room != null)
            {
                _job = Room.PushAfter(100, Update);
            }
        }
                
        int _searchCellDist = 1;
        
        protected virtual void CheckPlayer()
        {
            List<Player> targetPlayers = Room.GetAdiacentPlayer(CellPos, _searchCellDist);
            List<Player> visionPlayers = Room.GetAdiacentPlayer(CellPos);
            List<Player> players = visionPlayers.Except(targetPlayers).ToList();

            if (targetPlayers.Count > 0)
            {
                S_Interact interPacket = new S_Interact();
                interPacket.NpcId = Id;
                interPacket.Active = true;

                foreach (Player p in targetPlayers)
                {
                    interPacket.Player = p.Info;                  
                    p.Session.Send(interPacket);
                }
            }



            {
                S_Interact interPacket = new S_Interact();
                interPacket.NpcId = Id;
                interPacket.Active = false;

                foreach (Player p in players)
                {
                    interPacket.Player = p.Info;
                    p.Session.Send(interPacket);
                }
            }

        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            
        }
        public override void OnDead(GameObject attacker)
        {

        }
    }
}
