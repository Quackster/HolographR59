using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Zero.Core;
using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;
using Zero.Hotel.Users.Badges;
using Zero.Hotel.Users.Inventory;
using Zero.Hotel.Users.Messenger;
using Zero.Hotel.Users.Subscriptions;
using Zero.Storage;

namespace Zero.Hotel.Users;

internal class Habbo
{
    public uint Id;

    public string Username;

    public string RealName;

    public string AuthTicket;

    public uint Rank;

    public string Motto;

    public string Look;

    public string Gender;

    public int Credits;

    public int ActivityPoints;

    public double LastActivityPointsUpdate;

    public bool Muted;

    public int Respect;

    public int DailyRespectPoints;

    public int DailyPetRespectPoints;

    public uint LoadingRoom;

    public bool LoadingChecksPassed;

    public uint CurrentRoomId;

    public uint HomeRoom;

    public bool IsTeleporting;

    public uint TeleporterId;

    public SynchronizedCollection<uint> FavoriteRooms;

    public SynchronizedCollection<uint> MutedUsers;

    public SynchronizedCollection<string> Tags;

    public ConcurrentDictionary<uint, int> Achievements;

    public SynchronizedCollection<uint> RatedRooms;

    private SubscriptionManager SubscriptionManager;

    private HabboMessenger Messenger;

    private BadgeComponent BadgeComponent;

    private InventoryComponent InventoryComponent;

    private AvatarEffectsInventoryComponent AvatarEffectsInventoryComponent;

    public int NewbieStatus;

    public bool SpectatorMode;

    public bool Disconnected;

    public bool CalledGuideBot;

    public bool MutantPenalty;

    public bool BlockNewFriends;

    public bool InRoom
    {
        get
        {
            if (CurrentRoomId >= 1)
            {
                return true;
            }
            return false;
        }
    }

    public Room CurrentRoom
    {
        get
        {
            if (CurrentRoomId == 0)
            {
                return null;
            }
            return HolographEnvironment.GetGame().GetRoomManager().GetRoom(CurrentRoomId);
        }
    }

    public Habbo(uint Id, string Username, string RealName, string AuthTicket, uint Rank, string Motto, string Look, string Gender, int Credits, int ActivityPoints, double LastActivityPointsUpdate, bool Muted, uint HomeRoom, int Respect, int DailyRespectPoints, int DailyPetRespectPoints, int NewbieStatus, bool MutantPenalty, bool BlockNewFriends)
    {
        this.Id = Id;
        this.Username = Username;
        this.RealName = RealName;
        this.AuthTicket = AuthTicket;
        this.Rank = Rank;
        this.Motto = Motto;
        this.Look = Look.ToLower();
        this.Gender = Gender.ToLower();
        this.Credits = Credits;
        this.ActivityPoints = ActivityPoints;
        this.LastActivityPointsUpdate = LastActivityPointsUpdate;
        this.Muted = Muted;
        LoadingRoom = 0u;
        LoadingChecksPassed = false;
        CurrentRoomId = 0u;
        this.HomeRoom = HomeRoom;
        FavoriteRooms = new SynchronizedCollection<uint>();
        MutedUsers = new SynchronizedCollection<uint>();
        Tags = new SynchronizedCollection<string>();
        Achievements = new ConcurrentDictionary<uint, int>();
        RatedRooms = new SynchronizedCollection<uint>();
        this.Respect = Respect;
        this.DailyRespectPoints = DailyRespectPoints;
        this.DailyPetRespectPoints = DailyPetRespectPoints;
        this.NewbieStatus = NewbieStatus;
        CalledGuideBot = false;
        this.MutantPenalty = MutantPenalty;
        this.BlockNewFriends = BlockNewFriends;
        IsTeleporting = false;
        TeleporterId = 0u;
        SubscriptionManager = new SubscriptionManager(Id);
        BadgeComponent = new BadgeComponent(Id);
        InventoryComponent = new InventoryComponent(Id);
        AvatarEffectsInventoryComponent = new AvatarEffectsInventoryComponent(Id);
        SpectatorMode = false;
        Disconnected = false;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(Username + " has connected.", LogLevel.novouser);
    }

    public void LoadData()
    {
        SubscriptionManager.LoadSubscriptions();
        BadgeComponent.LoadBadges();
        InventoryComponent.LoadInventory();
        AvatarEffectsInventoryComponent.LoadEffects();
        LoadAchievements();
        LoadFavorites();
        LoadMutedUsers();
        LoadTags();
    }

    public bool HasFuse(string Fuse)
    {
        if (HolographEnvironment.GetGame().GetRoleManager().RankHasRight(Rank, Fuse))
        {
            return true;
        }
        foreach (string SubscriptionId in GetSubscriptionManager().SubList)
        {
            if (HolographEnvironment.GetGame().GetRoleManager().SubHasRight(SubscriptionId, Fuse))
            {
                return true;
            }
        }
        return false;
    }

    public void LoadFavorites()
    {
        FavoriteRooms.Clear();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE room_id FROM user_favorites WHERE user_id = '" + Id + "'");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            FavoriteRooms.Add((uint)Row["room_id"]);
        }
    }

    public void LoadMutedUsers()
    {
        MutedUsers.Clear();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE ignore_id FROM user_ignores WHERE user_id = '" + Id + "'");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            MutedUsers.Add((uint)Row["ignore_id"]);
        }
    }

    public void LoadTags()
    {
        Tags.Clear();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE tag FROM user_tags WHERE user_id = '" + Id + "'");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            Tags.Add((string)Row["tag"]);
        }
        if (Tags.Count >= 5)
        {
            HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(GetClient(), 7u, 1);
        }
    }

    public void LoadAchievements()
    {
        Achievements.Clear();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE achievement_id,achievement_level FROM user_achievements WHERE user_id = '" + Id + "'");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            Achievements.TryAdd((uint)Row["achievement_id"], (int)Row["achievement_level"]);
        }
    }

    public void OnDisconnect()
    {
        try
        {
            if (!Disconnected)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Username + " has disconnected.", LogLevel.Error);
                Disconnected = true;
                DateTime Now = DateTime.Now;
                using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                {
                    dbClient.ExecuteQuery("Update users SET last_online = '" + Now.ToString() + "', online = '0' WHERE id = '" + Id + "' LIMIT 1");
                }
                if (InRoom)
                {
                    HolographEnvironment.GetGame().GetRoomManager().GetRoom(CurrentRoomId)
                        .RemoveUserFromRoom(GetClient(), NotifyClient: false, NotifyKick: false);
                }
                if (Messenger != null)
                {
                    Messenger.AppearOffline = true;
                    Messenger.OnStatusChanged(instantUpdate: true);
                    Messenger = null;
                }
                if (SubscriptionManager != null)
                {
                    SubscriptionManager.Clear();
                    SubscriptionManager = null;
                }
                HolographEnvironment.GetGame().GetClientManager().StopClient(Id);
            }
        }
        catch (NullReferenceException ex)
        {
            HolographEnvironment.GetLogging().WriteLine("NullReferenceException " + ex.ToString(), LogLevel.Error);
            HolographEnvironment.GetGame().GetClientManager().StopClient(Id);
            HolographEnvironment.GetLogging().WriteLine("Client was forcefully stopped " + ex.ToString(), LogLevel.Error);
        }
    }

    public void OnEnterRoom(uint RoomId)
    {
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("INSERT INTO user_roomvisits (user_id,room_id,entry_timestamp,exit_timestamp,hour,minute) VALUES ('" + Id + "','" + RoomId + "','" + HolographEnvironment.GetUnixTimestamp() + "','0','" + DateTime.Now.Hour + "','" + DateTime.Now.Minute + "')");
        }
        CurrentRoomId = RoomId;
        Messenger.OnStatusChanged(instantUpdate: false);
    }

    public void OnLeaveRoom()
    {
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("Update user_roomvisits SET exit_timestamp = '" + HolographEnvironment.GetUnixTimestamp() + "' WHERE room_id = '" + CurrentRoomId + "' AND user_id = '" + Id + "' ORDER BY entry_timestamp DESC LIMIT 1");
        }
        CurrentRoomId = 0u;
        if (Messenger != null)
        {
            Messenger.OnStatusChanged(instantUpdate: false);
        }
    }

    public void InitMessenger()
    {
        if (GetMessenger() == null)
        {
            Messenger = new HabboMessenger(Id);
            Messenger.LoadBuddies();
            Messenger.LoadRequests();
            GetClient().SendMessage(Messenger.SerializeFriends());
            GetClient().SendMessage(Messenger.SerializeRequests());
            Messenger.OnStatusChanged(instantUpdate: true);
        }
    }

    public void UpdateCreditsBalance(bool InDatabase)
    {
        GetClient().GetMessageHandler().GetResponse().Init(6u);
        GetClient().GetMessageHandler().GetResponse().AppendStringWithBreak(Credits + ".0");
        GetClient().GetMessageHandler().SendResponse();
        if (InDatabase)
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update users SET credits = '" + Credits + "' WHERE id = '" + Id + "' LIMIT 1");
            }
        }
    }

    public void UpdateActivityPointsBalance(bool InDatabase)
    {
        UpdateActivityPointsBalance(InDatabase, 0);
    }

    public void UpdateActivityPointsBalance(bool InDatabase, int NotifAmount)
    {
        GetClient().GetMessageHandler().GetResponse().Init(438u);
        GetClient().GetMessageHandler().GetResponse().AppendInt32(ActivityPoints);
        GetClient().GetMessageHandler().GetResponse().AppendInt32(NotifAmount);
        GetClient().GetMessageHandler().SendResponse();
        if (InDatabase)
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update users SET activity_points = '" + ActivityPoints + "', activity_points_lastUpdate = '" + LastActivityPointsUpdate + "' WHERE id = '" + Id + "' LIMIT 1");
            }
        }
    }

    public void Mute()
    {
        if (!Muted)
        {
            GetClient().SendNotif("You have been muted by an moderator.");
            Muted = true;
        }
    }

    public void Unmute()
    {
        if (Muted)
        {
            GetClient().SendNotif("You have been unmuted by an moderator.");
            Muted = false;
        }
    }

    private GameClient GetClient()
    {
        return HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Id);
    }

    public SubscriptionManager GetSubscriptionManager()
    {
        return SubscriptionManager;
    }

    public HabboMessenger GetMessenger()
    {
        return Messenger;
    }

    public BadgeComponent GetBadgeComponent()
    {
        return BadgeComponent;
    }

    public InventoryComponent GetInventoryComponent()
    {
        return InventoryComponent;
    }

    public AvatarEffectsInventoryComponent GetAvatarEffectsInventoryComponent()
    {
        return AvatarEffectsInventoryComponent;
    }
}
