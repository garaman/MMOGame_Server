using Microsoft.EntityFrameworkCore;
using Server.Game.Job;
using Server.Game.Object;
using Server.Game.Room;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Server.DB
{
    public class DbTransaction : JobSerializer
    {
        public static DbTransaction Instance { get; } = new DbTransaction();

        public static void SavePlayerStatus_AllinOne(Player player, GameRoom room)
        {
            if (player == null || room == null) {  return; }

            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;

            Instance.Push(() => 
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(playerDb.Hp)).IsModified = true;
                    bool success = db.SaveChangesEx();
                    if (success) 
                    {
                        room.Push(() => Console.WriteLine($"SavePlayerHp : {playerDb.Hp}"));
                    }
                }
            }); 
        }

        public static void SavePalyerStatus(Player player, GameRoom room)
        {
            if (player == null || room == null) { return; }

            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;
            Instance.Push<PlayerDb, GameRoom>(PlayerStatusToDb, playerDb, room);
        }

        public static void PlayerStatusToDb(PlayerDb playerDb, GameRoom room)
        {
            using (AppDbContext db = new AppDbContext())
            {
                db.Entry(playerDb).State = EntityState.Unchanged;
                db.Entry(playerDb).Property(nameof(playerDb.Hp)).IsModified = true;
                bool success = db.SaveChangesEx();
                if (success)
                {
                    room.Push(SaveResultFromDb, playerDb.Hp);
                }
            }
        }

        public static void SaveResultFromDb(int hp)
        {
            Console.WriteLine($"SavePlayerHp : {hp}");
        }
    }
}
