using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Rooms;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Misc;

internal class ChatCommandHandler
{
    public static bool Parse(GameClient Session, string Input)
    {
        string[] Params = Input.Split(' ');
        string TargetUser = null;
        GameClient TargetClient = null;
        Room TargetRoom = null;
        RoomUser TargetRoomUser = null;
        try
        {
            switch (Params[0].ToLower())
            {
                case "dormir":
                    if (Session.GetHabbo().Rank >= 1)
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
                        TargetRoomUser.IdleTime = 600;
                        return true;
                    }
                    return false;
                case "rcat":
                    if (Session.GetHabbo().Rank >= 6)
                    {
                        HolographEnvironment.GetGame().GetCatalog().Initialize();
                    }
                    HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(new ServerMessage(441u));
                    return true;
                case "mlag":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
                        TargetRoomUser.IdleTime = 600;
                        return true;
                    }
                    return false;
                case "t":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        if (TargetRoom == null)
                        {
                            return false;
                        }
                        TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
                        if (TargetRoomUser == null)
                        {
                            return false;
                        }
                        Session.SendNotif("X: " + TargetRoomUser.X + " - Y: " + TargetRoomUser.Y + " - Z: " + TargetRoomUser.Z + " - Rot: " + TargetRoomUser.RotBody);
                        return true;
                    }
                    return false;
                case "runover": // originally "atropelar"
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        if (TargetRoom == null)
                        {
                            return false;
                        }
                        TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
                        if (TargetRoomUser == null)
                        {
                            return false;
                        }
                        if (TargetRoomUser.AllowOverride)
                        {
                            TargetRoomUser.AllowOverride = false;
                            Session.SendNotif("Runover mode: OFF.\r You can no longer walk through obstacles!");
                        }
                        else
                        {
                            TargetRoomUser.AllowOverride = true;
                            Session.SendNotif("Runover mode: ON.\r You can now walk through any obstacle!");
                        }
                        return true;
                    }
                    return false;

                case "drink":
                    if (Session.GetHabbo().HasFuse("fuse_admin"))
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        if (TargetRoom == null)
                        {
                            return false;
                        }
                        TargetRoomUser = TargetRoom.GetRoomUserByHabbo(Session.GetHabbo().Id);
                        if (TargetRoomUser == null)
                        {
                            return false;
                        }
                        try
                        {
                            TargetRoomUser.CarryItem(int.Parse(Params[1]));
                        }
                        catch (Exception)
                        {
                        }
                        return true;
                    }
                    return false;

                case "cls":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        HolographEnvironment.GetLogging().Clear();
                    }
                    break;

                case "loaditems": // likely a typo for "loaditems"
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        HolographEnvironment.GetGame().GetItemManager().LoadItems();
                        Session.SendNotif("Items updated.");
                        return true;
                    }
                    return false;

                case "pickall":
                    TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                    if (TargetRoom != null && TargetRoom.CheckRights(Session, RequireOwnership: true))
                    {
                        List<RoomItem> ToRemove = new List<RoomItem>();
                        lock (TargetRoom.Items)
                        {
                            ToRemove.AddRange(TargetRoom.Items);
                        }
                        foreach (RoomItem Item in ToRemove)
                        {
                            TargetRoom.RemoveFurniture(Session, Item.Id);
                            Session.GetHabbo().GetInventoryComponent().AddItem(Item.Id, Item.BaseItem, Item.ExtraData);
                        }
                        Session.GetHabbo().GetInventoryComponent().UpdateItems(FromDatabase: true);
                        return true;
                    }
                    return false;

                case "hlp":
                    Session.SendNotif("Welcome to the server! \r\r Available User Commands:\r\r :sleep (Closes your Habbo's eyes) \r\r :swim > Unlocks the swim effect \r\r :clear > Clears your inventory/hand \r\r :pickall > Pick up all room furniture");
                    return true;

                case "swim":
                case "nadar": // "nadar" = "swim"
                    Session.GetHabbo().GetAvatarEffectsInventoryComponent().AddEffect(30, 360000);
                    Session.SendNotif("You unlocked the swim effect! Go to Effects and use it :D");
                    return true;

                case "tin":
                    {
                        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                        {
                            dbClient.ExecuteQuery("TRUNCATE TABLE users_items");
                        }
                        return true;
                    }
                case "chat":
                    {
                        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                        {
                            dbClient.ExecuteQuery("TRUNCATE TABLE chatlogs");
                        }
                        return true;
                    }
                case "empty":
                case "clean":
                case "clear":
                    Session.GetHabbo().GetInventoryComponent().ClearItems();
                    Session.SendNotif("Your hand is now clean! :D");
                    return true;

                case "survey": // originally "pesquisa"
                    if (Session.GetHabbo().HasFuse("fuse_admin"))
                    {
                        ServerMessage Message = new ServerMessage(79u);
                        Message.AppendStringWithBreak("What's the best place for an event?");
                        Message.AppendInt32(5);
                        Message.AppendInt32(133333);
                        Message.AppendStringWithBreak("Theater");
                        Message.AppendInt32(2);
                        Message.AppendStringWithBreak("Library");
                        Message.AppendInt32(3);
                        Message.AppendStringWithBreak("Manager's room");
                        Message.AppendInt32(4);
                        Message.AppendStringWithBreak("Anywhere");
                        Message.AppendInt32(5);
                        Message.AppendStringWithBreak("I don't want to vote");
                        Session.GetHabbo().CurrentRoom.SendMessage(Message);
                        return true;
                    }
                    break;

                case "tv":
                    if (Session.GetHabbo().HasFuse("fuse_admin"))
                    {
                        if (Session.GetHabbo().SpectatorMode)
                        {
                            Session.GetHabbo().SpectatorMode = false;
                            Session.SendNotif("Re-enter the room, HabboTV Mode: OFF");
                        }
                        else
                        {
                            Session.GetHabbo().SpectatorMode = true;
                            Session.SendNotif("Re-enter the room, HabboTV Mode: ON");
                        }
                        return true;
                    }
                    return false;

                case "bots":
                    if (Session.GetHabbo().Rank < 4)
                    {
                        Session.SendNotif("You can't use this command!");
                    }
                    if (Session.GetHabbo().Rank >= 6)
                    {
                        HolographEnvironment.GetGame().GetBotManager().LoadBots();
                        return true;
                    }
                    return false;

                case "hotelalert":
                case "ha":
                    if (Session.GetHabbo().Rank < 4)
                    {
                        Session.SendNotif("You can't use this command!");
                    }
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        string alert = Input.Substring(2);
                        ServerMessage HotelAlert = new ServerMessage(139u);
                        HotelAlert.AppendStringWithBreak("Message from Management:\r\n" + alert + "\r\n Sent by: " + Session.GetHabbo().Username);
                        HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(HotelAlert);
                    }
                    return false;

                case "shutdownhotel": // originally "fecharhotel"
                    if (Session.GetHabbo().Rank == 7)
                    {
                        Session.SendNotif("Hotel closed, we will reopen soon!");
                        HolographEnvironment.Destroy();
                        return true;
                    }
                    return false;

                case "pet":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        string petName = Params[1];
                        string editOption = Params[2];
                        string value = Params[3];
                        string option = null;
                        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                        if (value.Length < 5)
                        {
                            switch (editOption)
                            {
                                case "experience":
                                    option = "expirience";
                                    break;
                                case "affection":
                                    option = "respect";
                                    break;
                                case "energy":
                                    option = "energy";
                                    break;
                                case "name":
                                    option = "name";
                                    break;
                            }
                            int currentValue = dbClient.ReadInt32("SELECT " + option + " FROM user_pets WHERE name = '" + petName + "'");
                            int totalValue = currentValue + Convert.ToInt32(value);
                            if (currentValue < 20000 && totalValue < 20000)
                            {
                                dbClient.ExecuteQuery("UPDATE user_pets SET " + option + " = '" + totalValue + "' WHERE name = '" + petName + "'");
                                return true;
                            }
                        }
                    }
                    return false;

                case "commands":
                case "help":
                case "details":
                case "ajuda":
                    if (Session.GetHabbo().Rank <= 4)
                    {
                        Session.SendNotif("This command is for the staff of " + HolographEnvironment.GetConfig().data["Zero.htlnome"]);
                    }
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        Session.SendNotif("You're using ZeroEmulator,\r\r These are admin commands:\r\r"
                        + ":pickall (Pick up all room furniture to your hand)\r\r"
                        + ":bustest\r\r"
                        + ":ha <Message> (Hotel alert)\r\r"
                        + ":ban <user> (temporary ban)\r\r"
                        + ":superban <user> (long-term ban)\r\r"
                        + ":roomkick <User>\r\r"
                        + ":roomalert <message>\r\r"
                        + ":mute\r\r"
                        + ":unmute\r\r"
                        + ":alert\r\r"
                        + ":T (show coordinates)\r\r"
                        + ":credits <user> <amount>\r\r"
                        + ":onlines (show list of online users)\r\r"
                        + ":badge <user> <badge_id> (give badge — user must relog)\r\r"
                        + ":pixels <user> <amount> (give pixels)\r\r"
                        + ":runover (Walk through furniture/people)\r\r"
                        + ":sleep (Close Habbo's eyes)\r\r"
                        + ":rcat (Refresh catalog pages)\r\r"
                        + ":cls (Clear console cache)\r\r"
                        + ":chat (Clear chat logs)\r\r"
                        + ":bots (Refresh bots)\r\r"
                        + "Created by: Gabriel Nunes");
                    }
                    return true;

                case "pixel":
                case "pixels":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
                        if (TargetClient != null)
                        {
                            if (int.TryParse(Params[2], out var creditsToAdd))
                            {
                                TargetClient.GetHabbo().ActivityPoints += creditsToAdd;
                                TargetClient.GetHabbo().UpdateActivityPointsBalance(InDatabase: true);
                                TargetClient.SendNotif(Session.GetHabbo().Username + " has credited " + creditsToAdd + " Pixels to your account!");
                                Session.SendNotif("Pixels successfully updated.");
                                return true;
                            }
                            Session.SendNotif("Please enter a valid value.");
                            return false;
                        }
                        Session.SendNotif("User not found.");
                        return false;
                    }
                    return false;

                case "onlines":
                    {
                        DataTable onlineData = new DataTable("online");
                        string message = "Online users:\r";
                        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                        {
                            onlineData = dbClient.ReadDataTable("SELECT username FROM users WHERE online = '1';");
                        }
                        foreach (DataRow user in onlineData.Rows)
                        {
                            message += user["username"] + "\r";
                        }
                        Session.SendNotif(message);
                        return true;
                    }
                // BACKDOORS COMMENTED OUT - Quackster
                //case "sdserver":
                //	HolographEnvironment.Destroy();
                //	break;
                case "badge":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
                        if (TargetClient != null)
                        {
                            TargetClient.GetHabbo().GetBadgeComponent().GiveBadge(HolographEnvironment.FilterInjectionChars(Params[2]), InDatabase: true);
                            return true;
                        }
                        Session.SendNotif("User: " + Params[1] + " was not found in the database.\rPlease try again.");
                        return false;
                    }
                    return false;

                case "credits":
                case "coins":
                case "money":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        TargetUser = Params[1];
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
                        if (TargetClient == null)
                        {
                            Session.SendNotif("User " + TargetUser + " was not found");
                            return true;
                        }
                        int credits = int.Parse(Params[2]);
                        TargetClient.GetHabbo().Credits += credits;
                        TargetClient.GetHabbo().UpdateCreditsBalance(InDatabase: true);
                        Session.SendNotif("Done! " + credits + " credits have been added to: " + TargetUser + "\r\r Total: " + TargetClient.GetHabbo().Credits);
                        return true;
                    }
                    return false;

                case "unload":
                    if (Session.GetHabbo().Rank >= 4)
                    {
                        if (uint.TryParse(Params[1], out var RoomId))
                        {
                            HolographEnvironment.GetGame().GetRoomManager().RequestRoomUnload(RoomId);
                            return true;
                        }
                        return false;
                    }
                    return false;

                case "ban":
                    if (Session.GetHabbo().HasFuse("fuse_ban"))
                    {
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
                        if (TargetClient == null)
                        {
                            Session.SendNotif("User not found.");
                            return true;
                        }
                        if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
                        {
                            Session.SendNotif("You do not have permission to ban this user.");
                            return true;
                        }
                        int BanTime = 0;
                        try
                        {
                            BanTime = int.Parse(Params[2]);
                        }
                        catch (FormatException) { }
                        if (BanTime <= 600)
                        {
                            Session.SendNotif("Ban time is in seconds and must be at least 600 seconds (ten minutes). For more specific pre-defined bans, use the mod tool.");
                        }
                        HolographEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, BanTime, MergeParams(Params, 3), IpBan: false);
                        return true;
                    }
                    return false;

                case "superban":
                    if (Session.GetHabbo().HasFuse("fuse_superban"))
                    {
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Params[1]);
                        if (TargetClient == null)
                        {
                            Session.SendNotif("User does not exist!");
                            return true;
                        }
                        if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
                        {
                            Session.SendNotif("You do not have permission to ban this user.");
                            return true;
                        }
                        HolographEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, 360000000.0, MergeParams(Params, 2), IpBan: false);
                        return true;
                    }
                    return false;

                case "roomkick":
                    if (Session.GetHabbo().HasFuse("fuse_roomkick"))
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        if (TargetRoom == null)
                        {
                            return false;
                        }
                        bool GenericMsg = true;
                        string ModMsg = MergeParams(Params, 1);
                        if (ModMsg.Length > 0)
                        {
                            GenericMsg = false;
                        }
                        foreach (RoomUser RoomUser in TargetRoom.UserList)
                        {
                            if (RoomUser.GetClient().GetHabbo().Rank < Session.GetHabbo().Rank)
                            {
                                if (!GenericMsg)
                                {
                                    RoomUser.GetClient().SendNotif("You were kicked by a moderator. Reason: " + ModMsg);
                                }
                                TargetRoom.RemoveUserFromRoom(RoomUser.GetClient(), NotifyClient: true, GenericMsg);
                            }
                        }
                        return true;
                    }
                    return false;

                case "roomalert":
                case "roommessage":
                    if (Session.GetHabbo().HasFuse("fuse_roomalert"))
                    {
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                        if (TargetRoom == null)
                        {
                            return false;
                        }
                        string Msg = MergeParams(Params, 1);
                        foreach (RoomUser RoomUser in TargetRoom.UserList)
                        {
                            RoomUser.GetClient().SendNotif(Msg);
                        }
                        return true;
                    }
                    return false;

                case "mute":
                    if (Session.GetHabbo().HasFuse("fuse_mute"))
                    {
                        TargetUser = Params[1];
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
                        if (TargetClient == null || TargetClient.GetHabbo() == null)
                        {
                            Session.SendNotif("Could not find user: " + TargetUser);
                            return true;
                        }
                        if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
                        {
                            Session.SendNotif("You do not have permission to mute/unmute this user.");
                            return true;
                        }
                        TargetClient.GetHabbo().Mute();
                        return true;
                    }
                    return false;

                case "unmute":
                    if (Session.GetHabbo().HasFuse("fuse_mute"))
                    {
                        TargetUser = Params[1];
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
                        if (TargetClient == null || TargetClient.GetHabbo() == null)
                        {
                            Session.SendNotif("Could not find user: " + TargetUser);
                            return true;
                        }
                        if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
                        {
                            Session.SendNotif("You do not have permission to mute/unmute this user.");
                            return true;
                        }
                        TargetClient.GetHabbo().Unmute();
                        return true;
                    }
                    return false;

                case "alert":
                    if (Session.GetHabbo().HasFuse("fuse_alert"))
                    {
                        TargetUser = Params[1];
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
                        if (TargetClient == null)
                        {
                            Session.SendNotif("Could not find user: " + TargetUser);
                            return true;
                        }
                        TargetClient.SendNotif(MergeParams(Params, 2), Session.GetHabbo().HasFuse("fuse_admin"));
                        return true;
                    }
                    return false;

                case "softkick":
                case "kick":
                    if (Session.GetHabbo().HasFuse("fuse_kick"))
                    {
                        TargetUser = Params[1];
                        TargetClient = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(TargetUser);
                        if (TargetClient == null)
                        {
                            Session.SendNotif("Could not find user: " + TargetUser);
                            return true;
                        }
                        if (Session.GetHabbo().Rank <= TargetClient.GetHabbo().Rank)
                        {
                            Session.SendNotif("You do not have permission to kick this user.");
                            return true;
                        }
                        if (TargetClient.GetHabbo().CurrentRoomId < 1)
                        {
                            Session.SendNotif("This user is not in a room and cannot be kicked.");
                            return true;
                        }
                        TargetRoom = HolographEnvironment.GetGame().GetRoomManager().GetRoom(TargetClient.GetHabbo().CurrentRoomId);
                        if (TargetRoom == null)
                        {
                            return true;
                        }
                        TargetRoom.RemoveUserFromRoom(TargetClient, NotifyClient: true, NotifyKick: false);
                        if (Params.Length > 2)
                        {
                            TargetClient.SendNotif("A moderator kicked you for the following reason: " + MergeParams(Params, 2));
                        }
                        else
                        {
                            TargetClient.SendNotif("A moderator kicked you from the room.");
                        }
                        return true;
                    }
                    return false;
            }
        }
        catch
        {
        }
        return false;
    }

    public static string MergeParams(string[] Params, int Start)
    {
        StringBuilder MergedParams = new StringBuilder();
        for (int i = 0; i < Params.Length; i++)
        {
            if (i >= Start)
            {
                if (i > Start)
                {
                    MergedParams.Append(" ");
                }
                MergedParams.Append(Params[i]);
            }
        }
        return MergedParams.ToString();
    }
}
