﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using db;
using db.data;
using wServer.cliPackets;
using wServer.realm.entities;
using wServer.realm.entities.player;
using wServer.realm.worlds;
using wServer.svrPackets;

#endregion

namespace wServer.realm.commands
{
    internal class TutorialCommand : ICommand
    {
        public string Command
        {
            get { return "tutorial"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            player.Client.Reconnect(new ReconnectPacket
            {
                Host = "",
                Port = 2050,
                GameId = World.TUT_ID,
                Name = "Tutorial",
                Key = Empty<byte>.Array,
            });
        }
    }

    internal class WhoCommand : ICommand
    {
        public string Command
        {
            get { return "who"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            var sb = new StringBuilder("Players online: ");
            var copy = player.Owner.Players.Values.ToArray();
            for (var i = 0; i < copy.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(copy[i].Name);
            }

            player.SendInfo(sb.ToString());
        }
    }

    internal class ServerCommand : ICommand
    {
        public string Command
        {
            get { return "server"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            player.SendInfo(player.Owner.Name);
        }
    }

    internal class PauseCommand : ICommand
    {
        public string Command
        {
            get { return "pause"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.Owner.Name != "Battle Arena" && player.Owner.Name != "Free Battle Arena")
            {
                if (player.HasConditionEffect(ConditionEffects.Paused))
                {
                    player.ApplyConditionEffect(new ConditionEffect
                    {
                        Effect = ConditionEffectIndex.Paused,
                        DurationMS = 0
                    });
                    player.SendInfo("Game resumed.");
                }
                else
                {
                    foreach (var i in player.Owner.EnemiesCollision.HitTest(player.X, player.Y, 6).OfType<Enemy>())
                    {
                        if (i.ObjectDesc.Enemy)
                        {
                            player.SendInfo("Not safe to pause.");
                            return;
                        }
                    }
                    player.ApplyConditionEffect(new ConditionEffect
                    {
                        Effect = ConditionEffectIndex.Paused,
                        DurationMS = -1
                    });
                    player.SendInfo("Game paused.");
                }
            }
            else
            {
                player.SendError("You cannot pause in the arena");
            }
        }
    }

    internal class TeleportCommand : ICommand
    {
        public string Command
        {
            get { return "teleport"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            try
            {
                if (player.nName.ToLower() == args[0].ToLower())
                {
                    player.SendInfo("You are already at yourself, and always will be!");
                    return;
                }
                if (player.Owner.AllowTeleport == false)
                {
                    player.SendInfo("You are not allowed to teleport in this area!");
                    return;
                }
                foreach (var i in player.Owner.Players)
                {
                    if (i.Value.nName.ToLower() == args[0].ToLower().Trim())
                    {
                        if (i.Value.HasConditionEffect(ConditionEffects.Invisible))
                        {
                            player.SendInfo("Could not teleport to this player.");
                            return;
                        }
                        player.Teleport(new RealmTime(), new TeleportPacket
                        {
                            ObjectId = i.Value.Id
                        });
                        return;
                    }
                }
                player.SendInfo(string.Format("Cannot teleport, {0} not found!", args[0].Trim()));
            }
            catch
            {
                player.SendHelp("Usage: /teleport <player name>");
            }
        }
    }

    internal class TellCommand : ICommand
    {
        public string Command
        {
            get { return "tell"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            try
            {
                var sindex = 1;

                if (!(player.NameChosen))
                {
                    player.SendInfo(string.Format("Choose a name!"));
                    return;
                }

                string[] tags =
                {
                    "[Hero]", "[Elite]", "[Legend]", "[Titan]", "[Doomed]", "[QA]", "[Mod]", "[Dev]", "[HMod]", "[HDev]", "[HCDev]", "[HDDev]", "[Founder]",
                    "[CM]", "[Player]", "[Temp Adm]", "[Member]", "[Super]", "[GM]", "[QA]", "[Admin]", "[Owner]"
                };
                var playername = args[0].Trim();
                if (tags.Contains(playername))
                {
                    playername = args[1];
                    sindex = 2;
                }

                if (player.nName.ToLower() == playername.ToLower())
                {
                    player.SendInfo(string.Format("You canot tell yourself!"));
                    return;
                }

                var saytext = string.Join(" ", args, sindex, args.Length - sindex);

                foreach (var w in RealmManager.Worlds)
                {
                    var world = w.Value;
                    if (w.Key != 0) // 0 is limbo??
                    {
                        foreach (var i in world.Players)
                        {
                            if (i.Value.nName.ToLower() == args[sindex - 1].ToLower().Trim() && i.Value.NameChosen)
                            {
                                if (saytext == "" || saytext == null)
                                {
                                    player.SendHelp("Usage: /tell <player name> <text>");
                                    return;
                                }
                                else
                                {
                                    player.Client.SendPacket(new TextPacket //echo to self
                                    {
                                        BubbleTime = 10,
                                        Stars = player.Stars,
                                        Name = player.Name,
                                        Recipient = i.Value.Name,
                                        Text = " " + saytext.ToSafeText()
                                    });

                                    i.Value.Client.SendPacket(new TextPacket //echo to /tell player
                                    {
                                        BubbleTime = 10,
                                        Stars = player.Stars,
                                        Recipient = i.Value.nName,
                                        Name = player.Name,
                                        Text = " " + saytext.ToSafeText()
                                    });
                                    return;
                                }
                            }
                        }
                    }
                }
                player.SendInfo(string.Format("Cannot tell, {0} not found!", args[sindex - 1].Trim()));
            }
            catch
            {
                player.SendInfo("Cannot tell!");
            }
        }
    }

    internal class DyeCommand : ICommand
    {
        public string Command
        {
            get { return "dye"; }
        }

        public int RequiredRank
        {
            get { return 5; }
        }
      //  public int RequiredDonation { get { return 5; } }

        public void Execute(Player player, string[] args)
        {
            var name = string.Join(" ", args.ToArray()).Trim();
            short objType;
            if (!XmlDatas.IdToType.TryGetValue(name, out objType))
            {
                player.SendInfo("Unknown dye!");
                return;
            }
            try
            {
                if (XmlDatas.TypeToElement[objType].Element("Class").Value == "Dye")
                {
                    for (var i = 0; i < player.Inventory.Length; i++)
                        if (player.Inventory[i] == null)
                        {
                            player.Inventory[i] = XmlDatas.ItemDescs[objType];
                            player.UpdateCount++;
                            return;
                        }
                }
                else
                {
                    player.SendInfo("Unknown dye!");
                    return;
                }
            }
            catch
            {
                return;
            }
        }
    }
    /*
internal class VisitCommand : ICommand
{
    public string Command
    {
        get { return "visit"; }
    }

    public int RequiredRank
    {
        get { return 0; }
    }

    public void Execute(Player player, string[] args)
    {
        var name = string.Join(" ", args.ToArray()).Trim();
        try
        {
            var PlayerData = PlayerDataList.GetData(player.Client.Account.Name);
            foreach (var w in RealmManager.Worlds)
            {
                var world = w.Value;
                if (w.Key != 0)
                {
                    foreach (var i in world.Players)
                    {
                        if (i.Value.Client.Account.Name.ToLower() == name.ToLower())
                        {
                            var iPlayerData = PlayerDataList.GetData(i.Value.Client.Account.Name);
                            if (!(player.Client.Account.Rank > 9))
                            {
                                if (world.Name != "Vault")
                                {
                                    if (world.Name != "Guild Hall")
                                    {
                                        TryJoin(player, iPlayerData, world, i.Value);
                                        return;
                                    }
                                    else
                                    {
                                        if ((world as GuildHall).Guild == player.Guild)
                                        {
                                            TryJoin(player, iPlayerData, world, i.Value);
                                            return;
                                        }
                                        else
                                        {
                                            player.SendInfo("Player is in " + i.Value.Guild + "'s guild hall!");
                                            return;
                                        }
                                    }
                                }
                                else
                                {
                                    if (world.Name == "Vault")
                                    {
                                        player.SendInfo("Player is in Vault!");
                                        return;
                                    }
                                    else if (world.Name == "Guild Hall")
                                    {
                                        player.SendInfo("Player is in Guild Hall!");
                                        return;
                                    }
                                    else
                                    {
                                        if (!iPlayerData.UsingGroup)
                                        {
                                            player.Client.Reconnect(new ReconnectPacket
                                            {
                                                Host = "",
                                                Port = 2050,
                                                GameId = world.Id,
                                                Name = i.Value.Name + "'s Vault",
                                                Key = Empty<byte>.Array,
                                            });
                                            return;
                                        }
                                        else
                                        {
                                            foreach (var o in iPlayerData.JGroup)
                                            {
                                                if (o.Value == player.Client.Account.Name.ToLower())
                                                {
                                                    player.Client.Reconnect(new ReconnectPacket
                                                    {
                                                        Host = "",
                                                        Port = 2050,
                                                        GameId = world.Id,
                                                        Name = i.Value.Name + "'s Vault",
                                                        Key = Empty<byte>.Array,
                                                    });
                                                    return;
                                                }
                                            }
                                            player.SendInfo("Not in " + i.Value.Client.Account.Name + "'s group!");
                                            return;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                player.Client.Reconnect(new ReconnectPacket
                                {
                                    Host = "",
                                    Port = 2050,
                                    GameId = world.Id,
                                    Name = i.Value.Owner.Name,
                                    Key = Empty<byte>.Array,
                                });
                                return;
                            }
                        }
                    }
                }
            }
            player.SendHelp("Use /visit <playername>");
        }
        catch
        {
            player.SendInfo("Unexpected error in command!");
        }
    }

    public bool TryJoin(Player player, GlobalPlayerData iPlayerData, World world, Player i)
    {
        if (world.Id == -60)
        {
            player.SendInfo("Sorry but that user is in a restricted area!");
            return true;
        }
        if (world.Name == "Admin Room")
        {
            if (!iPlayerData.Solo || player.Client.Account.Rank > 9)
            {
                if (!iPlayerData.UsingGroup)
                {
                    player.Client.Reconnect(new ReconnectPacket
                    {
                        Host = "",
                        Port = 2050,
                        GameId = world.Id,
                        Name = world.Name,
                        Key = Empty<byte>.Array,
                    });
                    return true;
                }
                else
                {
                    foreach (var o in iPlayerData.JGroup)
                    {
                        if (o.Value == player.Client.Account.Name.ToLower())
                        {
                            player.Client.Reconnect(new ReconnectPacket
                            {
                                Host = "",
                                Port = 2050,
                                GameId = world.Id,
                                Name = world.Name,
                                Key = Empty<byte>.Array,
                            });
                            return true;
                        }
                    }
                    player.SendInfo("Not in " + i.Client.Account.Name + "'s group!");
                    return true;
                }
            }
            else
            {
                player.SendInfo("Player is going solo!");
                return true;
            }
        }
        else
        {
            player.SendInfo("Sorry but that user is in a restricted area!");
            return true;
        }
    }
}
    */
    class VisitCommand : ICommand
    {
        public string Command { get { return "visit"; } }
        public int RequiredRank { get { return 0; } }

        public bool TryJoin(Player player, GlobalPlayerData iPlayerData, World world, Player i)
        {
            if (!iPlayerData.Solo)
            {
                if (!iPlayerData.UsingGroup)
                {
                    player.Client.Reconnect(new ReconnectPacket()
                    {
                        Host = "",
                        Port = 2050,
                        GameId = world.Id,
                        Name = world.Name,
                        Key = Empty<byte>.Array,
                    });
                    return true;
                }
                else
                {
                    foreach (var o in iPlayerData.JGroup)
                    {
                        if (o.Value == player.Client.Account.Name.ToLower())
                        {
                            player.Client.Reconnect(new ReconnectPacket()
                            {
                                Host = "",
                                Port = 2050,
                                GameId = world.Id,
                                Name = world.Name,
                                Key = Empty<byte>.Array,
                            });
                            return true;
                        }
                    }
                    player.SendInfo("Not in " + i.Client.Account.Name + "'s group!");
                    return true;
                }
            }
            else
            {
                player.SendInfo("Player is going solo!");
                return true;
            }
        }

        public void Execute(Player player, string[] args)
        {
            string name = string.Join(" ", args.ToArray()).Trim();
            try
            {
                GlobalPlayerData PlayerData = PlayerDataList.GetData(player.Client.Account.Name);
                foreach (var w in RealmManager.Worlds)
                {
                    World world = w.Value;
                    if (w.Key != 0)
                    {
                        foreach (var i in world.Players)
                        {
                            if (i.Value.Client.Account.Name.ToLower() == name.ToLower())
                            {
                                GlobalPlayerData iPlayerData = PlayerDataList.GetData(i.Value.Client.Account.Name);
                                if (!(player.Client.Account.Rank > 12))
                                {
                                    if (world.Name != "Vault")
                                    {
                                        if (world.Name != "Guild Hall")
                                        {
                                            TryJoin(player, iPlayerData, world, i.Value); return;
                                        }
                                        else
                                        {
                                            if ((world as GuildHall).Guild == player.Guild)
                                            {
                                                TryJoin(player, iPlayerData, world, i.Value); return;
                                            }
                                            else
                                            {
                                                player.SendInfo("Player is in " + i.Value.Guild + "'s guild hall!");
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (world.Name == "Vault")
                                        {
                                            player.SendInfo("Player is in Vault!");
                                            return;
                                        }
                                        else if (world.Name == "Guild Hall")
                                        {
                                            player.SendInfo("Player is in Guild Hall!");
                                            return;
                                        }
                                        else
                                        {
                                            if (!iPlayerData.UsingGroup)
                                            {
                                                player.Client.Reconnect(new ReconnectPacket()
                                                {
                                                    Host = "",
                                                    Port = 2050,
                                                    GameId = world.Id,
                                                    Name = i.Value.Name + "'s Vault",
                                                    Key = Empty<byte>.Array,
                                                });
                                                return;
                                            }
                                            else
                                            {
                                                foreach (var o in iPlayerData.JGroup)
                                                {
                                                    if (o.Value == player.Client.Account.Name.ToLower())
                                                    {
                                                        player.Client.Reconnect(new ReconnectPacket()
                                                        {
                                                            Host = "",
                                                            Port = 2050,
                                                            GameId = world.Id,
                                                            Name = i.Value.Name + "'s Vault",
                                                            Key = Empty<byte>.Array,
                                                        });
                                                        return;
                                                    }
                                                }
                                                player.SendInfo("Not in " + i.Value.Client.Account.Name + "'s group!");
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    player.Client.Reconnect(new ReconnectPacket()
                                    {
                                        Host = "",
                                        Port = 2050,
                                        GameId = world.Id,
                                        Name = i.Value.Owner.Name,
                                        Key = Empty<byte>.Array,
                                    });
                                    return;
                                }
                            }
                        }
                    }
                }
                player.SendHelp("Use /visit <playername>");
            }
            catch
            {
                player.SendInfo("Unexpected error in command!");
            }
        }
    }

    internal class GroupCommand : ICommand
    {
        public string Command
        {
            get { return "group"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            try
            {
                var PlayerData = PlayerDataList.GetData(player.Client.Account.Name);
                if (args.Length > 0)
                {
                    var subcommand = args[0];
                    if (subcommand == "list")
                    {
                        var glist = "Players in your group: ";
                        foreach (var i in PlayerData.JGroup)
                        {
                            if (glist != "Players in your group: ")
                            {
                                glist = glist + ", " + i;
                            }
                            else
                            {
                                glist = glist + i;
                            }
                        }
                        player.SendInfo(glist);
                    }
                    else if (subcommand == "add" && args.Length > 1)
                    {
                        foreach (var i in PlayerData.JGroup)
                        {
                            if (i.Value == args[1].ToLower())
                            {
                                player.SendInfo("Player already added!");
                                return;
                            }
                        }
                        PlayerData.JGroup.TryAdd(PlayerData.JGroup.Count, args[1].ToLower());
                        player.SendInfo("Added " + args[1] + "!");
                    }
                    else if (subcommand == "del" && args.Length > 1)
                    {
                        var remc = 0;
                        foreach (var i in PlayerData.JGroup)
                        {
                            if (i.Value == args[1].ToLower())
                            {
                                string absolutelynothingdisregardthis;
                                player.SendInfo("Removed player " + i.Value + "!");
                                remc++;
                                PlayerData.JGroup.TryRemove(i.Key, out absolutelynothingdisregardthis);
                            }
                        }
                        if (remc < 1)
                        {
                            player.SendInfo("Player not found!");
                        }
                    }
                }
                else
                {
                    if (PlayerData.UsingGroup)
                    {
                        PlayerData.UsingGroup = false;
                        player.SendInfo("Group-only join disabled!");
                    }
                    else
                    {
                        PlayerData.UsingGroup = true;
                        player.SendInfo("Group-only join enabled!");
                    }
                }
            }
            catch
            {
                player.SendInfo("Unexpected error in command!");
            }
        }
    }

    internal class SoloCommand : ICommand
    {
        public string Command
        {
            get { return "solo"; }
        }

        public int RequiredRank
        {
            get { return 8; }
        }

        public void Execute(Player player, string[] args)
        {
            var PlayerData = PlayerDataList.GetData(player.Client.Account.Name);
            if (PlayerData.Solo)
            {
                PlayerData.Solo = false;
                player.SendInfo("Solo disabled! People can now join you!");
            }
            else
            {
                PlayerData.Solo = true;
                player.SendInfo("Solo enabled! People can no longer join you!");
            }
        }
    }

    internal class ShopCommand : ICommand
    {
        public string Command
        {
            get { return "shop"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            var evar = string.Join(" ", args.ToArray()).Trim();
            if (args.Length > 0)
            {
                var shop = RealmManager.AddWorld(new ShopMap(""));
                RealmManager.ShopWorlds.TryGetValue(evar, out shop);
                player.Client.Reconnect(new ReconnectPacket
                {
                    Host = "",
                    Port = 2050,
                    GameId = shop.Id,
                    Name = "Shop",
                    Key = Empty<byte>.Array
                });
            }
            else
            {
                var shopnames = "";
                var tname = "";
                foreach (var i in MerchantLists.shopLists)
                {
                    if (shopnames == "")
                    {
                        tname = i.Key;
                        tname.Insert(0, tname[0].ToString().ToUpper());
                        shopnames = i.Key;
                    }
                    else
                    {
                        tname = i.Key;
                        tname.Insert(0, tname[0].ToString().ToUpper());
                        shopnames += ", " + i.Key;
                    }
                }
                player.SendInfo("Shops: " + shopnames);
            }
        }
    }

    internal class ListCommands : ICommand
    {
        public string Command
        {
            get { return "commands"; }
        }

        public int RequiredRank
        {
            get { return 12; }
        }

        public void Execute(Player player, string[] args)
        {
            var cmds = new Dictionary<string, ICommand>();
            var t = typeof (ICommand);
            foreach (var i in t.Assembly.GetTypes())
                if (t.IsAssignableFrom(i) && i != t)
                {
                    var instance = (ICommand) Activator.CreateInstance(i);
                    cmds.Add(instance.Command, instance);
                }
            var sb = new StringBuilder("");
            var copy = cmds.Values.ToArray();
            for (var i = 0; i < copy.Length; i++)
            {
                if (i != 0) sb.Append("  |  ");
                sb.Append(copy[i].Command);
            }

            player.Client.SendPacket(new TextBoxPacket
            {
                Title = "Commands:",
                Message = (sb.ToString()),
                Button1 = "Ok"
            });
        }
    }

    //class ListCommands : ICommand
    //{
    //    public string Command { get { return "commands"; } }
    //    public int RequiredRank { get { return 0; } }

    //    public void Execute(Player player, string[] args)
    //    {
    //        Dictionary<string, ICommand> cmds = new Dictionary<string, ICommand>();
    //        var t = typeof(ICommand);
    //        foreach (var i in t.Assembly.GetTypes())
    //            if (t.IsAssignableFrom(i) && i != t)
    //            {
    //                var instance = (ICommand)Activator.CreateInstance(i);
    //                cmds.Add(instance.Command, instance);
    //            }
    //        StringBuilder sb = new StringBuilder("Commands: ");
    //        var copy = cmds.Values.ToArray();
    //        for (int i = 0; i < copy.Length; i++)
    //        {
    //            if (i != 0) sb.Append(", ");
    //            sb.Append(copy[i].Command);
    //        }

    //        player.SendInfo(sb.ToString());
    //    }
    //}


    internal class statsCommand : ICommand
    {
        public string Command
        {
            get { return "stats"; }
        }

        public int RequiredRank
        {
            get { return 12; }
        }
        public int RequiredDonation { get { return 5; } }

        public void Execute(Player player, string[] args)
        {
            foreach (var i in RealmManager.Clients.Values)
                i.SendPacket(new NotificationPacket
                {
                    Color = new ARGB(0xff00ff00),
                    ObjectId = player.Id,
                    Text = "HP:" + player.HP + " " + "MP:" + player.MP + " " + "Fame:" + " " + player.Fame
                });
            player.SendInfo("HP:" + player.HP + " " + "MP:" + player.MP + " " + "Att:" + " " + player.Stats[2] + " " +
                            "Def:" + " " + player.Stats[3] + " " + "Spd:" + " " + player.Stats[4] + " " + "Vit:" + " " +
                            player.Stats[5] + " " + "Wis:" + " " + player.Stats[6] + " " + "Dex:" + " " +
                            player.Stats[7]);
        }
    }

    internal class sayCommand : ICommand
    {
        public string Command
        {
            get { return "say"; }
        }

        public int RequiredRank
        {
            get { return 5; }
        }
   //     public int RequiredDonation { get { return 5; } }

        public void Execute(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.SendHelp("Usage: /say <saytext>");
            }
            else
            {
                var saytext = string.Join(" ", args);
                foreach (var i in RealmManager.Clients.Values)
                    i.SendPacket(new NotificationPacket
                    {
                        Color = new ARGB(0xff00ff00),
                        ObjectId = player.Id,
                        Text = saytext
                    });
            }
        }
    }

    internal class AFKCommand : ICommand
    {
        public string Command
        {
            get { return "afk"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.Owner.Name != "Battle Arena" && player.Owner.Name != "Free Battle Arena")
            {
                if (player.HasConditionEffect(ConditionEffects.Paused))
                {
                    foreach (var i in RealmManager.Clients.Values)
                        i.SendPacket(new NotificationPacket
                        {
                            Color = new ARGB(0xff00ff00),
                            ObjectId = player.Id,
                            Text = "Active"
                        });
                    player.ApplyConditionEffect(new ConditionEffect
                    {
                        Effect = ConditionEffectIndex.Paused,
                        DurationMS = 0
                    });
                    player.SendInfo("Active!");
                }
                else
                {
                    foreach (var i in player.Owner.EnemiesCollision.HitTest(player.X, player.Y, 8).OfType<Enemy>())
                    {
                        if (i.ObjectDesc.Enemy)
                        {
                            player.SendInfo("Not safe to go AFK.");
                            return;
                        }
                    }
                    foreach (var i in RealmManager.Clients.Values)
                        i.SendPacket(new NotificationPacket
                        {
                            Color = new ARGB(0xff00ff00),
                            ObjectId = player.Id,
                            Text = "AFK"
                        });
                    player.ApplyConditionEffect(new ConditionEffect
                    {
                        Effect = ConditionEffectIndex.Paused,
                        DurationMS = -1
                    });
                    player.SendInfo("AFK!");
                }
            }
            else
            {
                player.SendInfo("You cannot pause in the arena");
            }
        }
    }

    internal class ArenasCommand : ICommand
    {
        public string Command
        {
            get { return "arenas"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            var Arenas = new List<BattleArenaMap>();
            var ArenaTexts = new List<string>();

            ArenaTexts.Add("");

            foreach (var w in RealmManager.Worlds)
            {
                var world = w.Value;
                if (w.Value.Name == "Battle Arena" && w.Value.Players.Count > 0 ||
                    w.Value.Name == "Free Battle Arena" && w.Value.Players.Count > 0)
                {
                    Arenas.Add(w.Value as BattleArenaMap);
                }
            }
            if (Arenas.Count > 0)
            {
                foreach (var w in Arenas)
                {
                    var ctext = "Wave " + w.Wave + " - {0} {1}";
                    var players = new List<string>();
                    var solo = 0;
                    foreach (var p in w.Players)
                    {
                        players.Add(p.Value.Name);
                        if (PlayerDataList.GetData(p.Value.Client.Account.Name).Solo)
                            solo++;
                    }
                    if (players.Count > 0)
                    {
                        ArenaTexts.Add(string.Format(ctext, string.Join(", ", players.ToArray()),
                            solo == players.Count ? " (SOLO)" : ""));
                    }
                }
            }

            if (ArenaTexts.Count == 1)
                ArenaTexts.Add("None");

            //player.SendInfo(string.Join("\n", ArenaTexts.ToArray()));

            player.Client.SendPacket(new TextBoxPacket
            {
                Title = "Current Arenas:",
                Message = string.Join("\n", ArenaTexts.ToArray()),
                Button1 = "Ok"
            });
        }
    }

    //class ArenasCommand : ICommand 
    //{
    //    public string Command { get { return "arenas"; } }
    //    public int RequiredRank { get { return 0; } }

    //    public void Execute(Player player, string[] args)
    //    {
    //        List<BattleArenaMap> Arenas = new List<BattleArenaMap>();
    //        List<string> ArenaTexts = new List<string>();

    //        ArenaTexts.Add("Current arenas:");

    //        foreach (var w in RealmManager.Worlds)
    //        {
    //            World world = w.Value;
    //            if (w.Value.Name == "Battle Arena" && w.Value.Players.Count > 0)
    //            {
    //                Arenas.Add(w.Value as BattleArenaMap);
    //            }
    //        }
    //        if (Arenas.Count > 0)
    //        {
    //            foreach (var w in Arenas)
    //            {
    //                string ctext = "Wave "+ w.Wave +" - {0} {1}";
    //                List<string> players = new List<string>();
    //                int solo = 0;
    //                foreach (var p in w.Players)
    //                {
    //                    players.Add(p.Value.Name);
    //                    if (PlayerDataList.GetData(p.Value.Client.Account.Name).Solo)
    //                        solo++;
    //                }
    //                if (players.Count > 0)
    //                {
    //                    ArenaTexts.Add(string.Format(ctext, string.Join(", ", players.ToArray()), solo == players.Count ? " (SOLO)" : ""));
    //                }
    //            }
    //        }

    //        if(ArenaTexts.Count == 1)
    //            ArenaTexts.Add("None");

    //        player.SendInfo(string.Join("\n", ArenaTexts.ToArray()));
    //    }
    //}

    internal class LeaderboardCommand : ICommand
    {
        public string Command
        {
            get { return "leaderboard"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            var leaderboardInfo = new Database().GetArenaLeaderboards();
           
            player.Client.SendPacket(new TextBoxPacket
            {
                Title = "Arena Leaderboard",
                Message = string.Join("\n", leaderboardInfo),
                Button1 = "Ok"
            });
           
        }
    }

    internal class GuildLeaderboardCommand : ICommand
    {
        public string Command
        {
            get { return "gleaderboard"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            var leaderboardInfo = new Database().GetGuildLeaderboards();
            
            player.Client.SendPacket(new TextBoxPacket
            {
                Title = "Guilds",
                Message = string.Join("\n", leaderboardInfo),
                Button1 = "Ok"
            });
            
        }
    }

    internal class ForgeListCommand : ICommand
    {
        public string Command
        {
            get { return "forgelist"; }
        }

        public int RequiredRank
        {
            get { return 12; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.HasSlot(3) && player.Inventory[3].ObjectId == "Forge Amulet")
            {
                var advancedNames = new List<string>();
                var itemNames = new List<string>();
                var c = new Combinations();
                foreach (var i in c.advCombos)
                {
                    var addText = i.Value.Item1;
                    var requiredItemCount = i.Key.Length;
                    var sList = i.Key.ToList();
                    foreach (var e in player.Inventory)
                    {
                        try
                        {
                            if (sList.Contains(e.ObjectId))
                                requiredItemCount--;
                            sList.Remove(e.ObjectId);
                        }
                        catch
                        {
                        }
                    }
                    if (requiredItemCount <= 0 && i.Value.Item2 <= player.CurrentFame)
                        addText = "<b>" + addText + "</b>";

                    if (i.Value.Item3)
                        advancedNames.Add(addText);
                    else
                        itemNames.Add(addText);
                }

                player.Client.SendPacket(new TextBoxPacket
                {
                    Title = "Forge List",
                    Message =
                        "<u>ADVANCED COMBOS</u>\n" + string.Join(" | ", advancedNames.ToArray()) +
                        "\n\n<u>REGULAR COMBOS</u>\n" + string.Join(" | ", itemNames.ToArray()),
                    Button1 = "Ok"
                });
            }
            else
            {
                var itemNames = new List<string>();
                var c = new Combinations();
                foreach (var i in c.combos)
                {
                    var addText = i.Value.Item1;
                    var requiredItemCount = i.Key.Length;
                    var sList = i.Key.ToList();
                    foreach (var e in player.Inventory)
                    {
                        try
                        {
                            if (sList.Contains(e.ObjectId))
                                requiredItemCount--;
                            sList.Remove(e.ObjectId);
                        }
                        catch
                        {
                        }
                    }
                    if (requiredItemCount <= 0 && i.Value.Item2 <= player.CurrentFame)
                        addText = "<b>" + addText + "</b>";

                    itemNames.Add(addText);
                }

                player.Client.SendPacket(new TextBoxPacket
                {
                    Title = "Forge List",
                    Message = string.Join(" | ", itemNames.ToArray()),
                    Button1 = "Ok"
                });
            }
        }
    }

    internal class SellCommand : ICommand
    {
        public string Command
        {
            get { return "sell"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            try
            {
                player.Decision = 0;
                player.price = new Prices();
                var slotList = new List<int>();
                var slotList2 = new List<int>();
                for (var i = 0; i < args.Length; i++)
                    if (!slotList.Contains(Convert.ToInt32(args[i])))
                        slotList.Add(Convert.ToInt32(args[i]));
                if (slotList.Count < 1)
                    throw new Exception();
                foreach (var i in slotList)
                    if (!(i < 0) && !(i > 8))
                    {
                        var realslot = i + 3;
                        if (player.Inventory[realslot] != null)
                        {
                            slotList2.Add(realslot);
                        }
                    }
                player.price.SellSlots = slotList2;
                if (!player.price.HasPrices(player))
                {
                    player.SendInfo("No prices for specified items!");
                }
                else
                {
                    var msgSlots = new List<int>();
                    foreach (var i in player.price.SellSlots)
                        try
                        {
                            msgSlots.Add(i - 3);
                        }
                        catch
                        {
                        }
                    player.SendInfo("Slots being sold: [" + string.Join(", ", msgSlots) + "]");
                    player.SendInfo("You gain " + player.price.GetPrices(player) +
                                    " fame from these items. Sell them?\nType /yes or /no");
                    player.Decision = 2;
                }
            }
            catch
            {
                player.SendHelp("Usage: /sell <slot #1> <slot #2> etc.");
            }
        }
    }
    /*
    internal class ForgeCommand : ICommand
    {
        public string Command
        {
            get { return "forge"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            player.Decision = 0;
            player.combs = new Combinations();
            var slotList = new List<int>();
            var slotList2 = new List<int>();
            var nameList = new List<string>();
            string[] nameArray;
            for (var i = 0; i < args.Length; i++)
                slotList.Add(Convert.ToInt32(args[i]));
            if (slotList.Count < 2)
                throw new Exception();
            foreach (var x in slotList)
            {
                foreach (var y in slotList)
                {
                    if (slotList[x] == slotList[y] && x != y)
                    {
                        player.SendError("You cannot select the same slot twice!");
                        return;
                    }
                    else
                    {
                        foreach (var i in slotList)
                            if (!(i < 0) && !(i > 8))
                            {
                                var realslot = i + 3;
                                if (player.Inventory[realslot] != null)
                                {
                                    slotList2.Add(realslot);
                                    nameList.Add(player.Inventory[realslot].ObjectId);
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }

                        player.combs.SlotList = slotList2;
                        nameArray = nameList.ToArray();
                        player.SendInfo("Researching " +
                                        (player.HasSlot(3) && player.Inventory[3].ObjectId == "Forge Amulet"
                                            ? "advanced "
                                            : "") + "combinations...");
                        if (
                            !player.combs.SetComboAdv(nameArray,
                                (player.HasSlot(3) && player.Inventory[3].ObjectId == "Forge Amulet")))
                        {
                            player.SendInfo("No combination found!");
                        }
                        else
                        {
                            player.SendInfo("It costs " +
                                            (player.combs.Combo.Item2/
                                             (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? 2 : 1)) +
                                            " fame to forge these items. Are you sure?\nType /yes or /no");
                            player.Decision = 1;
                        }
                    }
                }
            }
        }
    }

    internal class YesCommand : ICommand
    {
        public string Command
        {
            get { return "yes"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.Decision == 1)
            {
                if (player.combs.SlotList.Count > 0)
                {
                    if (player.combs.Combo.Item2/(player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? 2 : 1) <=
                        player.CurrentFame)
                    {
                        short resultId;
                        if (XmlDatas.IdToType.TryGetValue(player.combs.Combo.Item1, out resultId))
                        {
                            Item resultItem;
                            if (XmlDatas.ItemDescs.TryGetValue(resultId, out resultItem))
                            {
                                foreach (var i in player.combs.SlotList)
                                    player.Inventory[i] = null;
                                player.Inventory[player.combs.SlotList[0]] = resultItem;
                                player.CurrentFame =
                                    player.Client.Account.Stats.Fame =
                                        new Database().UpdateFame(player.Client.Account,
                                            -(player.combs.Combo.Item2/
                                              (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? 2 : 1)));
                                player.SendInfo("Your items have been forged into a " + player.combs.Combo.Item1 +
                                                (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e
                                                    ? ", breaking your Forge Amulet."
                                                    : "!"));
                                if (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e)
                                    player.Inventory[3] = null;
                                player.UpdateCount++;
                            }
                        }
                    }
                    else
                    {
                        player.SendInfo("Not enough fame to forge these items!");
                    }
                }
            }
            else if (player.Decision == 2)
            {
                if (player.price.HasPrices(player) && player.price.SellSlots.Count > 0)
                {
                    player.CurrentFame =
                        player.Client.Account.Stats.Fame =
                            new Database().UpdateFame(player.Client.Account, player.price.GetPrices(player));
                    foreach (var i in player.price.SellSlots)
                        player.Inventory[i] = null;
                    player.UpdateCount++;
                    player.SendInfo("Items sold!");
                }
                else
                    player.SendInfo("Could not sell.");
            }
            player.Decision = 0;
        }
    }

    internal class NoCommand : ICommand
    {
        public string Command
        {
            get { return "no"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.Decision == 1)
            {
                player.SendInfo("Cancelled forging.");
            }
            else if (player.Decision == 2)
            {
                player.SendInfo("Cancelled selling.");
            }
            player.Decision = 0;
        }
    }
    */
    class ForgeCommand : ICommand
    {
        public string Command { get { return "forge"; } }
        public int RequiredRank { get { return 0; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                player.Decision = 0;
                player.combs = new Combinations();
                List<int> slotList = new List<int>();
                List<int> slotList2 = new List<int>();
                List<string> nameList = new List<string>();
                string[] nameArray;
                for (var i = 0; i < args.Length; i++)
                    slotList.Add(Convert.ToInt32(args[i]));
                if (slotList.Count < 2)
                    throw new Exception();
                foreach (var i in slotList)
                    if (!(i < 0) && !(i > 8))
                    {
                        int realslot = i + 3;
                        if (player.Inventory[realslot] != null)
                        {
                            slotList2.Add(realslot);
                            nameList.Add(player.Inventory[realslot].ObjectId);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                player.combs.SlotList = slotList2;
                nameArray = nameList.ToArray();
                player.SendInfo("Researching " + (player.HasSlot(3) && player.Inventory[3].ObjectId == "Forge Amulet" ? "advanced " : "") + "combinations...");
                if (!player.combs.SetComboAdv(nameArray, (player.HasSlot(3) && player.Inventory[3].ObjectId == "Forge Amulet")))
                {
                    player.SendInfo("No combination found!");
                }
                else
                {
                    player.SendInfo("It costs " + (player.combs.Combo.Item2 / (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? 2 : 1)).ToString() + " fame to forge these items. Are you sure?\nType /yes or /no");
                    player.Decision = 1;
                }
            }
            catch
            {
                player.SendHelp("Usage: /forge <slot #1> <slot #2> etc.");
            }
        }
    }

    class YesCommand : ICommand
    {
        public string Command { get { return "yes"; } }
        public int RequiredRank { get { return 0; } }

        public void Execute(Player player, string[] args)
        {
            if (player.Decision == 1)
            {
                if (player.combs.SlotList.Count > 0)
                {
                    if (player.combs.Combo.Item2 / (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? 2 : 1) <= player.CurrentFame)
                    {
                        short resultId;
                        if (XmlDatas.IdToType.TryGetValue(player.combs.Combo.Item1, out resultId))
                        {
                            Item resultItem;
                            if (XmlDatas.ItemDescs.TryGetValue(resultId, out resultItem))
                            {
                                foreach (var i in player.combs.SlotList)
                                    player.Inventory[i] = null;
                                player.Inventory[player.combs.SlotList[0]] = resultItem;
                                player.CurrentFame = player.Client.Account.Stats.Fame = new Database().UpdateFame(player.Client.Account, -(player.combs.Combo.Item2 / (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? 2 : 1)));
                                player.SendInfo("Your items have been forged into a " + player.combs.Combo.Item1 + (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e ? ", breaking your Forge Amulet." : "!"));
                                if (player.HasSlot(3) && player.Inventory[3].ObjectType == 0x193e)
                                    player.Inventory[3] = null;
                                player.UpdateCount++;
                            }
                        }
                    }
                    else
                    {
                        player.SendInfo("Not enough fame to forge these items!");
                    }
                }
            }
            else if (player.Decision == 2)
            {
                if (player.price.HasPrices(player) && player.price.SellSlots.Count > 0)
                {
                    player.CurrentFame = player.Client.Account.Stats.Fame = new Database().UpdateFame(player.Client.Account, player.price.GetPrices(player));
                    foreach (var i in player.price.SellSlots)
                        player.Inventory[i] = null;
                    player.UpdateCount++;
                    player.SendInfo("Items sold!");
                }
                else
                    player.SendInfo("Could not sell.");
            }
            player.Decision = 0;
        }
    }

    class NoCommand : ICommand
    {
        public string Command { get { return "no"; } }
        public int RequiredRank { get { return 0; } }

        public void Execute(Player player, string[] args)
        {
            if (player.Decision == 1)
            {
                player.SendInfo("Cancelled forging.");
            }
            else if (player.Decision == 2)
            {
                player.SendInfo("Cancelled selling.");
            }
            player.Decision = 0;
        }
    }
    //class ForgeListCommand : ICommand
    //{
    //    public string Command { get { return "forgelist"; } }
    //    public int RequiredRank { get { return 0; } }

    //    public void Execute(Player player, string[] args)
    //    {
    //        List<string> itemNames = new List<string>();
    //        Combinations c = new Combinations();
    //        foreach (var i in c.combos)
    //        {
    //            itemNames.Add(i.Value.Item1);
    //        }
    //        player.SendInfo("These are the current items that can be forged:\n" + string.Join(", ", itemNames.ToArray()));
    //    }
    //}

    internal class PremChat : ICommand
    {
        public string Command
        {
            get { return "d"; }
        }

        public int RequiredRank
        {
            get { return 5; }
        }

        public void Execute(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.SendHelp("Use /d <text>");
            }

            else if (player.Client.Account.Rank >= 5)
            {
                try
                {
                    var saytext = string.Join(" ", args);

                    foreach (var w in RealmManager.Worlds)
                    {
                        var world = w.Value;
                        if (w.Key != 0)
                        {
                            foreach (var i in world.Players)
                            {
                                if (i.Value.Client.Account.Rank >= 5)
                                {
                                    i.Value.Client.SendPacket(new TextPacket
                                    {
                                        BubbleTime = 10,
                                        ObjectId = player.Id,
                                        Stars = player.Stars,
                                        Name = "#[Doomed Chat] " + player.nName,
                                        Text = " " + saytext
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    player.SendInfo("Cannot premium chat!");
                }
            }
            else
                player.SendInfo("You need to be a donator to use this command.");
        }
    }

    internal class PackCommand : ICommand
    {
        public string Command
        {
            get { return "b"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.tradeTarget == null)
            {
                if (player.SwapBackpack(Convert.ToInt32(args[0])))
                {
                    player.SendInfo("Switched to backpack #" + args[0]);
                }
                else
                {
                    player.SendInfo("Backpack #" + args[0] + " does not exist!");
                }
            }
            else
            {
                player.SendError("Cannot switch backpack while trading!");
            }
        }
    }

    internal class Pack2Command : ICommand
    {
        public string Command
        {
            get { return "bp"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            if (player.tradeTarget == null)
            {
                if (player.SwapBackpack(Convert.ToInt32(args[0])))
                {
                    player.SendInfo("Switched to backpack #" + args[0]);
                }
                else
                {
                    player.SendInfo("Backpack #" + args[0] + " does not exist!");
                }
            }
            else
            {
                player.SendError("Cannot switch backpack while trading!");
            }
        }
    }
    internal class RulesCommand : ICommand
    {
        public string Command
        {
            get { return "rules"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            player.Client.SendPacket(new NotificationPacket()
            {
                Color = new ARGB(0xFFFFFF),
                ObjectId = player.Id,
                Text = "\n1: Show respect\n2: Duping, clearly a ban if caught\n3: Spam or Caps   No need\n4:  Item slot glitches  insta-ban\n5: No begging\n6: No selling items while trading"
            });
        }
    }
    internal class NothingCommand : ICommand
    {
        public string Command
        {
            get { return "nothing"; }
        }

        public int RequiredRank
        {
            get { return 0; }
        }

        public void Execute(Player player, string[] args)
        {
            player.Client.SendPacket(new NotificationPacket()
            {
                Color = new ARGB(0xFFFFFF),
                ObjectId = player.Id,
                Text = "Nothing"
            });
        }
    }
}