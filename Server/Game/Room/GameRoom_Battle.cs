using Google.Protobuf;
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
      
    }
}
