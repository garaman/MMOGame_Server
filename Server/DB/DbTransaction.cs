using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game;
using Server.Game.Object;
using Server.Game.Room;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Server.DB
{
    public partial class DbTransaction : JobSerializer
    {
        public static DbTransaction Instance { get; } = new DbTransaction();
        
        /*
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
        */
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
            //Console.WriteLine($"SavePlayerHp : {hp}");
        }


        // 아이템
        public static void RewardPlayer(Player player, RewardData rewardData, GameRoom room)
        {
            if (player == null || rewardData == null || room == null) { return; }

            int? slot = player.Inven.GetEmptySlot();
            if(slot == null) { return; }

            ItemDb itemDb = new ItemDb()
            {
                TemplateId = rewardData.itemId,
                Count = rewardData.count,
                Slot = slot.Value,
                OwnerDbId = player.PlayerDbId
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Items.Add(itemDb);                   
                    bool success = db.SaveChangesEx();
                    if (success)
                    {
                        room.Push(() => 
                        {
                            Item newItem = Item.MakeItem(itemDb);
                            player.Inven.Add(newItem);

                            {
                                S_AddItem itemPacket = new S_AddItem();
                                ItemInfo itemInfo = new ItemInfo();
                                itemInfo.MergeFrom(newItem.Info);
                                itemPacket.Items.Add(itemInfo);

                                player.Session.Send(itemPacket);
                            }
                        });
                    }
                }
            });
        }

        public static void BuyItem(Player player, GameRoom room, C_BuyItem buyPacket)
        {
            if (player == null || buyPacket == null || room == null) { return; }

            S_BuyItem sbuyPacket = new S_BuyItem();
            int? slot = player.Inven.GetEmptySlot();
            if (slot == null)
            {
                sbuyPacket.Success = false;
                player.Session.Send(sbuyPacket);
                return;
            }

            ItemDb itemDb = new ItemDb()
            {
                TemplateId = buyPacket.TemplateId,
                Count = 1,
                Slot = slot.Value,
                OwnerDbId = player.PlayerDbId
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Items.Add(itemDb);
                    bool success = db.SaveChangesEx();
                    if (success)
                    {
                        room.Push(() =>
                        {
                            Item newItem = Item.MakeItem(itemDb);
                            player.Inven.Add(newItem);

                            {
                                S_AddItem itemPacket = new S_AddItem();
                                ItemInfo itemInfo = new ItemInfo();
                                itemInfo.MergeFrom(newItem.Info);
                                itemPacket.Items.Add(itemInfo);

                                player.Session.Send(itemPacket);
                            }

                            {
                                sbuyPacket.TemplateId = newItem.TemplateId;
                                sbuyPacket.Success = true;

                                player.Session.Send(sbuyPacket);
                            }
                        });
                    }
                }
            });
        }

        public static void SellItem(Player player, GameRoom room, C_SellItem sellPacket)
        {
            if (player == null || sellPacket == null || room == null) { return; }

            Item item = player.Inven.Find(i => i.ItemDbId == sellPacket.ItemDbId);
            if (item == null) { return; }

            ItemDb itemDb = new ItemDb()
            {
                ItemDbId = item.ItemDbId,                
                OwnerDbId = player.PlayerDbId
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Items.Remove(itemDb);
                    bool success = db.SaveChangesEx();
                    if (success)
                    {
                        room.Push(() =>
                        {
                            
                            player.Inven.Remove(itemDb.ItemDbId);
                            {
                                S_SellItem sellItemPacket = new S_SellItem();
                                sellItemPacket.ItemDbId = itemDb.ItemDbId;
                                sellItemPacket.Success = true;                                
                                player.Session.Send(sellItemPacket);
                            }
                        });
                    }
                }
            });
        }

    }
}
