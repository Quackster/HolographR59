using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;
using Zero.Hotel.Users.Messenger;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Navigators;

internal class Navigator
{
    private Dictionary<int, string> PublicCategories;

    private List<FlatCat> PrivateCategories;

    private Dictionary<int, PublicItem> PublicItems;

    public void Initialize()
    {
        PublicCategories = new Dictionary<int, string>();
        PrivateCategories = new List<FlatCat>();
        PublicItems = new Dictionary<int, PublicItem>();
        DataTable dPubCats = null;
        DataTable dPrivCats = null;
        DataTable dPubItems = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dPubCats = dbClient.ReadDataTable("SELECT id,caption FROM navigator_pubcats WHERE enabled = '1'");
            dPrivCats = dbClient.ReadDataTable("SELECT id,caption,min_rank FROM navigator_flatcats WHERE enabled = '1'");
            dPubItems = dbClient.ReadDataTable("SELECT id,bannertype,caption,image,image_type,room_id,category_id,category_parent_id FROM navigator_publics ORDER BY ordernum ASC");
        }
        if (dPubCats != null)
        {
            foreach (DataRow Row in dPubCats.Rows)
            {
                PublicCategories.Add((int)Row["id"], (string)Row["caption"]);
            }
        }
        if (dPrivCats != null)
        {
            foreach (DataRow Row in dPrivCats.Rows)
            {
                PrivateCategories.Add(new FlatCat((int)Row["id"], (string)Row["caption"], (int)Row["min_rank"]));
            }
        }
        if (dPubItems == null)
        {
            return;
        }
        foreach (DataRow Row in dPubItems.Rows)
        {
            PublicItems.Add((int)Row["id"], new PublicItem((int)Row["id"], int.Parse(Row["bannertype"].ToString()), (string)Row["caption"], (string)Row["image"], (!(Row["image_type"].ToString().ToLower() == "internal")) ? PublicImageType.EXTERNAL : PublicImageType.INTERNAL, (uint)Row["room_id"], (int)Row["category_id"], (int)Row["category_parent_id"]));
        }
    }

    public int GetCountForParent(int ParentId)
    {
        int i = 0;
        lock (PublicItems)
        {
            foreach (PublicItem Item in PublicItems.Values)
            {
                if (Item.ParentId == ParentId || ParentId == -1)
                {
                    i++;
                }
            }
        }
        return i;
    }

    public FlatCat GetFlatCat(int Id)
    {
        lock (PrivateCategories)
        {
            foreach (FlatCat FlatCat in PrivateCategories)
            {
                if (FlatCat.Id == Id)
                {
                    return FlatCat;
                }
            }
        }
        return null;
    }

    public ServerMessage SerializeFlatCategories()
    {
        ServerMessage Cats = new ServerMessage(221u);
        Cats.AppendInt32(PrivateCategories.Count);
        foreach (FlatCat FlatCat in PrivateCategories)
        {
            if (FlatCat.Id > 0)
            {
                Cats.AppendBoolean(Bool: true);
            }
            Cats.AppendInt32(FlatCat.Id);
            Cats.AppendStringWithBreak(FlatCat.Caption);
        }
        Cats.AppendStringWithBreak("");
        return Cats;
    }

    public ServerMessage SerializePublicRooms()
    {
        ServerMessage Frontpage = new ServerMessage(450u);
        Frontpage.AppendInt32(GetCountForParent(-1));
        lock (PublicItems)
        {
            foreach (PublicItem Pub in PublicItems.Values)
            {
                Pub.Serialize(Frontpage);
            }
        }
        return Frontpage;
    }

    public ServerMessage SerializeFavoriteRooms(GameClient Session)
    {
        ServerMessage Rooms = new ServerMessage(451u);
        Rooms.AppendInt32(0);
        Rooms.AppendInt32(6);
        Rooms.AppendStringWithBreak("");
        Rooms.AppendInt32(Session.GetHabbo().FavoriteRooms.Count);
        lock (Session.GetHabbo().FavoriteRooms)
        {
            foreach (uint Id in Session.GetHabbo().FavoriteRooms)
            {
                HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id)
                    .Serialize(Rooms, ShowEvents: false);
            }
        }
        return Rooms;
    }

    public ServerMessage SerializeRecentRooms(GameClient Session)
    {
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT * FROM user_roomvisits ORDER BY entry_timestamp DESC LIMIT 50");
        }
        List<RoomData> ValidRecentRooms = new List<RoomData>();
        List<uint> RoomsListed = new List<uint>();
        if (Data != null)
        {
            foreach (DataRow Row in Data.Rows)
            {
                RoomData rData = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData((uint)Row["room_id"]);
                if (rData != null && !rData.IsPublicRoom && !RoomsListed.Contains(rData.Id))
                {
                    ValidRecentRooms.Add(rData);
                    RoomsListed.Add(rData.Id);
                }
            }
        }
        ServerMessage Rooms = new ServerMessage(451u);
        Rooms.AppendInt32(0);
        Rooms.AppendInt32(7);
        Rooms.AppendStringWithBreak("");
        Rooms.AppendInt32(ValidRecentRooms.Count);
        foreach (RoomData _Data in ValidRecentRooms)
        {
            _Data.Serialize(Rooms, ShowEvents: false);
        }
        return Rooms;
    }

    public ServerMessage SerializeEventListing(GameClient Session, int Category)
    {
        ServerMessage Message = new ServerMessage(451u);
        Message.AppendInt32(Category);
        Message.AppendInt32(12);
        Message.AppendStringWithBreak("");
        List<Room> EventRooms = HolographEnvironment.GetGame().GetRoomManager().GetEventRoomsForCategory(Category);
        Message.AppendInt32(EventRooms.Count);
        foreach (Room Room in EventRooms)
        {
            RoomData Data = new RoomData();
            Data.Fill(Room);
            Data.Serialize(Message, ShowEvents: true);
        }
        return Message;
    }

    public ServerMessage SerializePopularRoomTags()
    {
        Dictionary<string, int> Tags = new Dictionary<string, int>();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT tags,users_now FROM rooms WHERE roomtype = 'private' AND users_now > 0 ORDER BY users_now DESC LIMIT 50");
        }
        if (Data != null)
        {
            foreach (DataRow Row in Data.Rows)
            {
                List<string> RoomTags = new List<string>();
                string[] array = Row["tags"].ToString().Split(',');
                foreach (string Tag in array)
                {
                    RoomTags.Add(Tag);
                }
                foreach (string Tag in RoomTags)
                {
                    if (Tags.ContainsKey(Tag))
                    {
                        Tags[Tag] += (int)Row["users_now"];
                    }
                    else
                    {
                        Tags.Add(Tag, (int)Row["users_now"]);
                    }
                }
            }
        }
        List<KeyValuePair<string, int>> SortedTags = new List<KeyValuePair<string, int>>(Tags);
        SortedTags.Sort((KeyValuePair<string, int> firstPair, KeyValuePair<string, int> nextPair) => firstPair.Value.CompareTo(nextPair.Value));
        ServerMessage Message = new ServerMessage(452u);
        Message.AppendInt32(SortedTags.Count);
        foreach (KeyValuePair<string, int> TagData in SortedTags)
        {
            Message.AppendStringWithBreak(TagData.Key);
            Message.AppendInt32(TagData.Value);
        }
        return Message;
    }

    public ServerMessage SerializeSearchResults(string SearchQuery)
    {
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            SearchQuery = HolographEnvironment.FilterInjectionChars(SearchQuery.ToLower()).Trim();
            if (SearchQuery.Length > 0)
            {
                dbClient.AddParamWithValue("query", SearchQuery + "%");
                dbClient.AddParamWithValue("tags_query", "%" + SearchQuery + "%");
                Data = dbClient.ReadDataTable("SELECT * FROM rooms WHERE caption LIKE @query AND roomtype = 'private' OR tags LIKE @tags_query AND roomtype = 'private' OR owner LIKE @query AND roomtype = 'private' ORDER BY users_now DESC LIMIT 30");
            }
        }
        List<RoomData> Results = new List<RoomData>();
        if (Data != null)
        {
            foreach (DataRow Row in Data.Rows)
            {
                RoomData RData = new RoomData();
                RData.Fill(Row);
                Results.Add(RData);
            }
        }
        ServerMessage Message = new ServerMessage(451u);
        Message.AppendInt32(1);
        Message.AppendInt32(9);
        Message.AppendStringWithBreak(SearchQuery);
        Message.AppendInt32(Results.Count);
        foreach (RoomData Room in Results)
        {
            Room.Serialize(Message, ShowEvents: false);
        }
        return Message;
    }

    public ServerMessage SerializeRoomListing(GameClient Session, int Mode)
    {
        ServerMessage Rooms = new ServerMessage(451u);
        if (Mode >= -1)
        {
            Rooms.AppendInt32(Mode);
            Rooms.AppendInt32(1);
        }
        else
        {
            switch (Mode)
            {
                case -2:
                    Rooms.AppendInt32(0);
                    Rooms.AppendInt32(2);
                    break;
                case -3:
                    Rooms.AppendInt32(0);
                    Rooms.AppendInt32(5);
                    break;
                case -4:
                    Rooms.AppendInt32(0);
                    Rooms.AppendInt32(3);
                    break;
                case -5:
                    Rooms.AppendInt32(0);
                    Rooms.AppendInt32(4);
                    break;
            }
        }
        Rooms.AppendStringWithBreak("");
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            List<MessengerBuddy> buddies = default(List<MessengerBuddy>);
            switch (Mode)
            {
                case -5:
                    {
                        List<uint> FriendRooms = new List<uint>();
                        bool lockTaken2 = false;
                        try
                        {
                            Monitor.Enter(buddies = Session.GetHabbo().GetMessenger().GetBuddies(), ref lockTaken2);
                            foreach (MessengerBuddy Buddy in Session.GetHabbo().GetMessenger().GetBuddies())
                            {
                                GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Buddy.Id);
                                if (Client != null && Client.GetHabbo() != null && Client.GetHabbo().CurrentRoomId != 0)
                                {
                                    FriendRooms.Add(Client.GetHabbo().CurrentRoomId);
                                }
                            }
                        }
                        finally
                        {
                            if (lockTaken2)
                            {
                                Monitor.Exit(buddies);
                            }
                        }
                        StringBuilder _Query = new StringBuilder("SELECT * FROM rooms WHERE");
                        int _i = 0;
                        foreach (uint Room in FriendRooms)
                        {
                            if (_i > 0)
                            {
                                _Query.Append(" OR");
                            }
                            _Query.Append(" id = '" + Room + "'");
                            _i++;
                        }
                        _Query.Append(" ORDER BY users_now DESC LIMIT 40");
                        Data = ((_i <= 0) ? null : dbClient.ReadDataTable(_Query.ToString()));
                        break;
                    }
                case -4:
                    {
                        List<string> FriendsNames = new List<string>();
                        bool lockTaken = false;
                        try
                        {
                            Monitor.Enter(buddies = Session.GetHabbo().GetMessenger().GetBuddies(), ref lockTaken);
                            foreach (MessengerBuddy Buddy in Session.GetHabbo().GetMessenger().GetBuddies())
                            {
                                FriendsNames.Add(Buddy.Username);
                            }
                        }
                        finally
                        {
                            if (lockTaken)
                            {
                                Monitor.Exit(buddies);
                            }
                        }
                        StringBuilder Query = new StringBuilder("SELECT * FROM rooms WHERE");
                        int i = 0;
                        foreach (string Name in FriendsNames)
                        {
                            if (i > 0)
                            {
                                Query.Append(" OR");
                            }
                            Query.Append(" owner = '" + Name + "'");
                            i++;
                        }
                        Query.Append(" ORDER BY users_now DESC LIMIT 40");
                        Data = ((i <= 0) ? null : dbClient.ReadDataTable(Query.ToString()));
                        break;
                    }
                case -3:
                    Data = dbClient.ReadDataTable("SELECT * FROM rooms WHERE owner = '" + Session.GetHabbo().Username + "' ORDER BY id ASC");
                    break;
                case -2:
                    Data = dbClient.ReadDataTable("SELECT * FROM rooms WHERE score > 0 AND roomtype = 'private' ORDER BY score DESC LIMIT 40");
                    break;
                case -1:
                    Data = dbClient.ReadDataTable("SELECT * FROM rooms WHERE users_now > 0 AND roomtype = 'private' ORDER BY users_now DESC LIMIT 40");
                    break;
                default:
                    Data = dbClient.ReadDataTable("SELECT * FROM rooms WHERE category = '" + Mode + "' AND roomtype = 'private' ORDER BY users_now DESC LIMIT 40");
                    break;
            }
        }
        if (Data == null)
        {
            Rooms.AppendInt32(0);
        }
        else
        {
            Rooms.AppendInt32(Data.Rows.Count);
            foreach (DataRow Row in Data.Rows)
            {
                HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData((uint)Row["id"])
                    .Serialize(Rooms, ShowEvents: false);
            }
        }
        return Rooms;
    }
}
