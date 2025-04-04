using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Zero.Hotel.Advertisements;
using Zero.Hotel.Catalogs;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Misc;
using Zero.Hotel.Navigators;
using Zero.Hotel.Pathfinding;
using Zero.Hotel.Pets;
using Zero.Hotel.RoomBots;
using Zero.Hotel.Rooms;
using Zero.Hotel.Support;
using Zero.Hotel.Users.Badges;
using Zero.Hotel.Users.Messenger;
using Zero.Storage;

namespace Zero.Messages;

internal class GameClientMessageHandler
{
    private delegate void RequestHandler();

    private const int HIGHEST_MESSAGE_ID = 4004;

    private GameClient Session;

    private ClientMessage Request;

    private ServerMessage Response;

    private RequestHandler[] RequestHandlers;

    private void GuardarLookVestuario()
    {
        using (HolographEnvironment.GetDatabase().GetClient())
        {
            HolographEnvironment.GetLogging().WriteLine("Recibidos datos de un Vestuario");
        }
    }

    public void RegisterVestuario()
    {
        RequestHandlers[88] = GuardarLookVestuario;
    }

    private void AddFavorite()
    {
        uint Id = Request.PopWiredUInt();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id);
        if (Data == null || Session.GetHabbo().FavoriteRooms.Count >= 30 || Session.GetHabbo().FavoriteRooms.Contains(Id) || Data.Type == "public")
        {
            GetResponse().Init(33u);
            GetResponse().AppendInt32(-9001);
            SendResponse();
            return;
        }
        GetResponse().Init(459u);
        GetResponse().AppendUInt(Id);
        GetResponse().AppendBoolean(Bool: true);
        SendResponse();
        Session.GetHabbo().FavoriteRooms.Add(Id);
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.ExecuteQuery("INSERT INTO user_favorites (user_id,room_id) VALUES ('" + Session.GetHabbo().Id + "','" + Id + "')");
    }

    private void RemoveFavorite()
    {
        uint Id = Request.PopWiredUInt();
        Session.GetHabbo().FavoriteRooms.Remove(Id);
        GetResponse().Init(459u);
        GetResponse().AppendUInt(Id);
        GetResponse().AppendBoolean(Bool: false);
        SendResponse();
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.ExecuteQuery("DELETE FROM user_favorites WHERE user_id = '" + Session.GetHabbo().Id + "' AND room_id = '" + Id + "' LIMIT 1");
    }

    private void GoToHotelView()
    {
        if (Session.GetHabbo().InRoom)
        {
            HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId)
                .RemoveUserFromRoom(Session, NotifyClient: true, NotifyKick: false);
        }
    }

    private void GetFlatCats()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeFlatCategories());
    }

    private void EnterInquiredRoom()
    {
    }

    private void GetPubs()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializePublicRooms());
    }

    private void GetRoomInfo()
    {
        uint RoomId = Request.PopWiredUInt();
        bool unk = Request.PopWiredBoolean();
        bool unk2 = Request.PopWiredBoolean();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(RoomId);
        if (Data != null)
        {
            GetResponse().Init(454u);
            GetResponse().AppendInt32(0);
            Data.Serialize(GetResponse(), ShowEvents: false);
            SendResponse();
        }
    }

    private void GetPopularRooms()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRoomListing(Session, int.Parse(Request.PopFixedString())));
    }

    private void GetHighRatedRooms()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRoomListing(Session, -2));
    }

    private void GetFriendsRooms()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRoomListing(Session, -4));
    }

    private void GetRoomsWithFriends()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRoomListing(Session, -5));
    }

    private void GetOwnRooms()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRoomListing(Session, -3));
    }

    private void GetFavoriteRooms()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeFavoriteRooms(Session));
    }

    private void GetRecentRooms()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRecentRooms(Session));
    }

    private void GetEvents()
    {
        int Category = int.Parse(Request.PopFixedString());
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeEventListing(Session, Category));
    }

    private void GetPopularTags()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializePopularRoomTags());
    }

    private void PerformSearch()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeSearchResults(Request.PopFixedString()));
    }

    private void PerformSearch2()
    {
        int junk = Request.PopWiredInt32();
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeSearchResults(Request.PopFixedString()));
    }

    public void RegisterNavigator()
    {
        RequestHandlers[19] = AddFavorite;
        RequestHandlers[20] = RemoveFavorite;
        RequestHandlers[53] = GoToHotelView;
        RequestHandlers[151] = GetFlatCats;
        RequestHandlers[233] = EnterInquiredRoom;
        RequestHandlers[380] = GetPubs;
        RequestHandlers[385] = GetRoomInfo;
        RequestHandlers[430] = GetPopularRooms;
        RequestHandlers[431] = GetHighRatedRooms;
        RequestHandlers[432] = GetFriendsRooms;
        RequestHandlers[433] = GetRoomsWithFriends;
        RequestHandlers[434] = GetOwnRooms;
        RequestHandlers[435] = GetFavoriteRooms;
        RequestHandlers[436] = GetRecentRooms;
        RequestHandlers[439] = GetEvents;
        RequestHandlers[382] = GetPopularTags;
        RequestHandlers[437] = PerformSearch;
        RequestHandlers[438] = PerformSearch2;
    }

    private void InitHelpTool()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetHelpTool().SerializeFrontpage());
    }

    private void GetHelpCategories()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetHelpTool().SerializeIndex());
    }

    private void ViewHelpTopic()
    {
        uint TopicId = Request.PopWiredUInt();
        HelpTopic Topic = HolographEnvironment.GetGame().GetHelpTool().GetTopic(TopicId);
        if (Topic != null)
        {
            Session.SendMessage(HolographEnvironment.GetGame().GetHelpTool().SerializeTopic(Topic));
        }
    }

    private void SearchHelpTopics()
    {
        string SearchQuery = Request.PopFixedString();
        if (SearchQuery.Length >= 3)
        {
            Session.SendMessage(HolographEnvironment.GetGame().GetHelpTool().SerializeSearchResults(SearchQuery));
        }
    }

    private void GetTopicsInCategory()
    {
        uint Id = Request.PopWiredUInt();
        HelpCategory Category = HolographEnvironment.GetGame().GetHelpTool().GetCategory(Id);
        if (Category != null)
        {
            Session.SendMessage(HolographEnvironment.GetGame().GetHelpTool().SerializeCategory(Category));
        }
    }

    private void SubmitHelpTicket()
    {
        bool errorOccured = false;
        if (HolographEnvironment.GetGame().GetModerationTool().UsersHasPendingTicket(Session.GetHabbo().Id))
        {
            errorOccured = true;
        }
        if (!errorOccured)
        {
            string Message = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
            int Junk = Request.PopWiredInt32();
            int Type = Request.PopWiredInt32();
            uint ReportedUser = Request.PopWiredUInt();
            HolographEnvironment.GetGame().GetModerationTool().SendNewTicket(Session, Type, ReportedUser, Message);
        }
        GetResponse().Init(321u);
        GetResponse().AppendBoolean(errorOccured);
        SendResponse();
    }

    private void DeletePendingCFH()
    {
        if (HolographEnvironment.GetGame().GetModerationTool().UsersHasPendingTicket(Session.GetHabbo().Id))
        {
            HolographEnvironment.GetGame().GetModerationTool().DeletePendingTicketForUser(Session.GetHabbo().Id);
            GetResponse().Init(320u);
            SendResponse();
        }
    }

    private void ModGetUserInfo()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            uint UserId = Request.PopWiredUInt();
            if (HolographEnvironment.GetGame().GetClientManager().GetNameById(UserId) != "Unknown User")
            {
                Session.SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeUserInfo(UserId));
            }
            else
            {
                Session.SendNotif("Não foi possivel encontrar info's, usuário inexistente");
            }
        }
    }

    private void ModGetUserChatlog()
    {
        if (Session.GetHabbo().HasFuse("fuse_chatlogs"))
        {
            Session.SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeUserChatlog(Request.PopWiredUInt()));
        }
    }

    private void ModGetRoomChatlog()
    {
        if (Session.GetHabbo().HasFuse("fuse_chatlogs"))
        {
            int Junk = Request.PopWiredInt32();
            uint RoomId = Request.PopWiredUInt();
            if (HolographEnvironment.GetGame().GetRoomManager().GetRoom(RoomId) != null)
            {
                Session.SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeRoomChatlog(RoomId));
            }
        }
    }

    private void ModGetRoomTool()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            uint RoomId = Request.PopWiredUInt();
            RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateNullableRoomData(RoomId);
            Session.SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeRoomTool(Data));
        }
    }

    private void ModPickTicket()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            int Junk = Request.PopWiredInt32();
            uint TicketId = Request.PopWiredUInt();
            HolographEnvironment.GetGame().GetModerationTool().PickTicket(Session, TicketId);
        }
    }

    private void ModReleaseTicket()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            int amount = Request.PopWiredInt32();
            for (int i = 0; i < amount; i++)
            {
                uint TicketId = Request.PopWiredUInt();
                HolographEnvironment.GetGame().GetModerationTool().ReleaseTicket(Session, TicketId);
            }
        }
    }

    private void ModCloseTicket()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            int Result = Request.PopWiredInt32();
            int Junk = Request.PopWiredInt32();
            uint TicketId = Request.PopWiredUInt();
            HolographEnvironment.GetGame().GetModerationTool().CloseTicket(Session, TicketId, Result);
        }
    }

    private void ModGetTicketChatlog()
    {
        if (!Session.GetHabbo().HasFuse("fuse_mod"))
        {
            return;
        }
        SupportTicket Ticket = HolographEnvironment.GetGame().GetModerationTool().GetTicket(Request.PopWiredUInt());
        if (Ticket != null)
        {
            RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateNullableRoomData(Ticket.RoomId);
            if (Data != null)
            {
                Session.SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeTicketChatlog(Ticket, Data, Ticket.Timestamp));
            }
        }
    }

    private void ModGetRoomVisits()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            uint UserId = Request.PopWiredUInt();
            Session.SendMessage(HolographEnvironment.GetGame().GetModerationTool().SerializeRoomVisits(UserId));
        }
    }

    private void ModSendRoomAlert()
    {
        if (Session.GetHabbo().HasFuse("fuse_alert"))
        {
            int One = Request.PopWiredInt32();
            int Two = Request.PopWiredInt32();
            string Message = Request.PopFixedString();
            HolographEnvironment.GetGame().GetModerationTool().RoomAlert(Session.GetHabbo().CurrentRoomId, !Two.Equals(3), Message);
        }
    }

    private void ModPerformRoomAction()
    {
        if (Session.GetHabbo().HasFuse("fuse_mod"))
        {
            uint RoomId = Request.PopWiredUInt();
            bool ActOne = Request.PopWiredBoolean();
            bool ActTwo = Request.PopWiredBoolean();
            bool ActThree = Request.PopWiredBoolean();
            HolographEnvironment.GetGame().GetModerationTool().PerformRoomAction(Session, RoomId, ActThree, ActOne, ActTwo);
        }
    }

    private void ModSendUserCaution()
    {
        if (Session.GetHabbo().HasFuse("fuse_alert"))
        {
            uint UserId = Request.PopWiredUInt();
            string Message = Request.PopFixedString();
            HolographEnvironment.GetGame().GetModerationTool().AlertUser(Session, UserId, Message, Caution: true);
        }
    }

    private void ModSendUserMessage()
    {
        if (Session.GetHabbo().HasFuse("fuse_alert"))
        {
            uint UserId = Request.PopWiredUInt();
            string Message = Request.PopFixedString();
            HolographEnvironment.GetGame().GetModerationTool().AlertUser(Session, UserId, Message, Caution: false);
        }
    }

    private void ModKickUser()
    {
        if (Session.GetHabbo().HasFuse("fuse_kick"))
        {
            uint UserId = Request.PopWiredUInt();
            string Message = Request.PopFixedString();
            HolographEnvironment.GetGame().GetModerationTool().KickUser(Session, UserId, Message, Soft: false);
        }
    }

    private void ModBanUser()
    {
        if (Session.GetHabbo().HasFuse("fuse_ban"))
        {
            uint UserId = Request.PopWiredUInt();
            string Message = Request.PopFixedString();
            int Length = Request.PopWiredInt32() * 3600;
            HolographEnvironment.GetGame().GetModerationTool().BanUser(Session, UserId, Length, Message);
        }
    }

    private void CallGuideBot()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        lock (Room.UserList)
        {
            List<RoomUser>.Enumerator Users = Room.UserList.GetEnumerator();
            while (Users.MoveNext())
            {
                RoomUser User = Users.Current;
                if (User.IsBot && User.BotData.AiType == "guide")
                {
                    Session.GetMessageHandler().GetResponse().Init(33u);
                    Session.GetMessageHandler().GetResponse().AppendInt32(4009);
                    Session.GetMessageHandler().SendResponse();
                    return;
                }
            }
        }
        if (Session.GetHabbo().CalledGuideBot)
        {
            Session.GetMessageHandler().GetResponse().Init(33u);
            Session.GetMessageHandler().GetResponse().AppendInt32(4010);
            Session.GetMessageHandler().SendResponse();
            return;
        }
        RoomUser NewUser = Room.DeployBot(HolographEnvironment.GetGame().GetBotManager().GetBot(55u));
        NewUser.SetPos(Room.Model.DoorX, Room.Model.DoorY, Room.Model.DoorZ);
        NewUser.UpdateNeeded = true;
        RoomUser RoomOwner = Room.GetRoomUserByHabbo(Room.Owner);
        if (RoomOwner != null)
        {
            NewUser.MoveTo(RoomOwner.Coordinate);
            NewUser.SetRot(Rotation.Calculate(NewUser.X, NewUser.Y, RoomOwner.X, RoomOwner.Y));
        }
        HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(Session, 6u, 1);
        Session.GetHabbo().CalledGuideBot = true;
    }

    public void RegisterHelp()
    {
        RequestHandlers[416] = InitHelpTool;
        RequestHandlers[417] = GetHelpCategories;
        RequestHandlers[418] = ViewHelpTopic;
        RequestHandlers[419] = SearchHelpTopics;
        RequestHandlers[420] = GetTopicsInCategory;
        RequestHandlers[453] = SubmitHelpTicket;
        RequestHandlers[238] = DeletePendingCFH;
        RequestHandlers[440] = CallGuideBot;
        RequestHandlers[200] = ModSendRoomAlert;
        RequestHandlers[450] = ModPickTicket;
        RequestHandlers[451] = ModReleaseTicket;
        RequestHandlers[452] = ModCloseTicket;
        RequestHandlers[454] = ModGetUserInfo;
        RequestHandlers[455] = ModGetUserChatlog;
        RequestHandlers[456] = ModGetRoomChatlog;
        RequestHandlers[457] = ModGetTicketChatlog;
        RequestHandlers[458] = ModGetRoomVisits;
        RequestHandlers[459] = ModGetRoomTool;
        RequestHandlers[460] = ModPerformRoomAction;
        RequestHandlers[461] = ModSendUserCaution;
        RequestHandlers[462] = ModSendUserMessage;
        RequestHandlers[463] = ModKickUser;
        RequestHandlers[464] = ModBanUser;
    }

    private void GetCatalogIndex()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetCatalog().SerializeIndex(Session));
    }

    private void GetCatalogPage()
    {
        CatalogPage Page = HolographEnvironment.GetGame().GetCatalog().GetPage(Request.PopWiredInt32());
        if (Page == null || !Page.Enabled || !Page.Visible || Page.ComingSoon || Page.MinRank > Session.GetHabbo().Rank)
        {
            return;
        }
        if (Page.ClubOnly && !Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_club"))
        {
            Session.SendNotif("This page is for Zero Club members only!");
            return;
        }
        Session.SendMessage(HolographEnvironment.GetGame().GetCatalog().SerializePage(Page));
        if (Page.Layout == "recycler")
        {
            GetResponse().Init(507u);
            GetResponse().AppendBoolean(Bool: true);
            GetResponse().AppendBoolean(Bool: false);
            SendResponse();
        }
    }

    private void RedeemVoucher()
    {
        HolographEnvironment.GetGame().GetCatalog().GetVoucherHandler()
            .TryRedeemVoucher(Session, Request.PopFixedString());
    }

    private void HandlePurchase()
    {
        int PageId = Request.PopWiredInt32();
        uint ItemId = Request.PopWiredUInt();
        string ExtraData = Request.PopFixedString();
        HolographEnvironment.GetGame().GetCatalog().HandlePurchase(Session, PageId, ItemId, ExtraData, IsGift: false, "", "");
    }

    private void PurchaseGift()
    {
        int PageId = Request.PopWiredInt32();
        uint ItemId = Request.PopWiredUInt();
        string ExtraData = Request.PopFixedString();
        string GiftUser = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        string GiftMessage = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        HolographEnvironment.GetGame().GetCatalog().HandlePurchase(Session, PageId, ItemId, ExtraData, IsGift: true, GiftUser, GiftMessage);
    }

    private void GetRecyclerRewards()
    {
        GetResponse().Init(506u);
        GetResponse().AppendInt32(5);
        for (uint i = 5u; i >= 1; i--)
        {
            GetResponse().AppendUInt(i);
            switch (i)
            {
                case 0u:
                case 1u:
                    GetResponse().AppendInt32(0);
                    break;
                case 2u:
                    GetResponse().AppendInt32(4);
                    break;
                case 3u:
                    GetResponse().AppendInt32(40);
                    break;
                case 4u:
                    GetResponse().AppendInt32(200);
                    break;
                default:
                    if (i >= 5)
                    {
                        GetResponse().AppendInt32(2000);
                    }
                    break;
            }
            List<EcotronReward> Rewards = HolographEnvironment.GetGame().GetCatalog().GetEcotronRewardsForLevel(i);
            GetResponse().AppendInt32(Rewards.Count);
            foreach (EcotronReward Reward in Rewards)
            {
                GetResponse().AppendStringWithBreak(Reward.GetBaseItem().Type.ToLower());
                GetResponse().AppendUInt(Reward.DisplayId);
            }
        }
        SendResponse();
    }

    private void CanGift()
    {
        uint Id = Request.PopWiredUInt();
        CatalogItem Item = HolographEnvironment.GetGame().GetCatalog().FindItem(Id);
        if (Item != null)
        {
            GetResponse().Init(622u);
            GetResponse().AppendUInt(Item.Id);
            GetResponse().AppendBoolean(Item.GetBaseItem().AllowGift);
            SendResponse();
        }
    }

    private void GetCataData1()
    {
        GetResponse().Init(612u);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(5);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(10000);
        GetResponse().AppendInt32(48);
        GetResponse().AppendInt32(7);
        SendResponse();
    }

    private void GetCataData2()
    {
        GetResponse().Init(620u);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(10);
        GetResponse().AppendInt32(3064);
        GetResponse().AppendInt32(3065);
        GetResponse().AppendInt32(3066);
        GetResponse().AppendInt32(3067);
        GetResponse().AppendInt32(3068);
        GetResponse().AppendInt32(3069);
        GetResponse().AppendInt32(3070);
        GetResponse().AppendInt32(3071);
        GetResponse().AppendInt32(3072);
        GetResponse().AppendInt32(3073);
        GetResponse().AppendInt32(7);
        GetResponse().AppendInt32(0);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(2);
        GetResponse().AppendInt32(3);
        GetResponse().AppendInt32(4);
        GetResponse().AppendInt32(5);
        GetResponse().AppendInt32(6);
        GetResponse().AppendInt32(11);
        GetResponse().AppendInt32(0);
        GetResponse().AppendInt32(1);
        GetResponse().AppendInt32(2);
        GetResponse().AppendInt32(3);
        GetResponse().AppendInt32(4);
        GetResponse().AppendInt32(5);
        GetResponse().AppendInt32(6);
        GetResponse().AppendInt32(7);
        GetResponse().AppendInt32(8);
        GetResponse().AppendInt32(9);
        GetResponse().AppendInt32(10);
        GetResponse().AppendInt32(1);
        SendResponse();
    }

    private void MarketplaceCanSell()
    {
        GetResponse().Init(611u);
        GetResponse().AppendBoolean(Bool: true);
        GetResponse().AppendInt32(99999);
        SendResponse();
    }

    private void MarketplacePostItem()
    {
        if (Session.GetHabbo().GetInventoryComponent() != null)
        {
            int sellingPrice = Request.PopWiredInt32();
            int junk = Request.PopWiredInt32();
            uint itemId = Request.PopWiredUInt();
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(itemId);
            if (Item != null && Item.GetBaseItem().AllowTrade)
            {
                HolographEnvironment.GetGame().GetCatalog().GetMarketplace()
                    .SellItem(Session, Item.Id, sellingPrice);
            }
        }
    }

    private void MarketplaceGetOwnOffers()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetCatalog().GetMarketplace()
            .SerializeOwnOffers(Session.GetHabbo().Id));
    }

    private void MarketplaceTakeBack()
    {
        uint ItemId = Request.PopWiredUInt();
        DataRow Row = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Row = dbClient.ReadDataRow("SELECT * FROM catalog_marketplace_offers WHERE offer_id = '" + ItemId + "' LIMIT 1");
        }
        if (Row == null || (uint)Row["user_id"] != Session.GetHabbo().Id || (string)Row["state"] != "1")
        {
            return;
        }
        Item Item = HolographEnvironment.GetGame().GetItemManager().GetItem((uint)Row["item_id"]);
        if (Item != null)
        {
            HolographEnvironment.GetGame().GetCatalog().DeliverItems(Session, Item, 1, (string)Row["extra_data"]);
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("DELETE FROM catalog_marketplace_offers WHERE offer_id = '" + ItemId + "' LIMIT 1");
            }
            GetResponse().Init(614u);
            GetResponse().AppendUInt((uint)Row["offer_id"]);
            GetResponse().AppendBoolean(Bool: true);
            SendResponse();
        }
    }

    private void MarketplaceClaimCredits()
    {
        DataTable Results = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Results = dbClient.ReadDataTable("SELECT asking_price FROM catalog_marketplace_offers WHERE user_id = '" + Session.GetHabbo().Id + "' AND state = '2'");
        }
        if (Results == null)
        {
            return;
        }
        int Profit = 0;
        foreach (DataRow Row in Results.Rows)
        {
            Profit += (int)Row["asking_price"];
        }
        if (Profit >= 1)
        {
            Session.GetHabbo().Credits += Profit;
            Session.GetHabbo().UpdateCreditsBalance(InDatabase: true);
        }

        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("DELETE FROM catalog_marketplace_offers WHERE user_id = '" + Session.GetHabbo().Id + "' AND state = '2'");
        }
    }

    private void MarketplaceGetOffers()
    {
        int MinPrice = Request.PopWiredInt32();
        int MaxPrice = Request.PopWiredInt32();
        string SearchQuery = Request.PopFixedString();
        int FilterMode = Request.PopWiredInt32();
        Session.SendMessage(HolographEnvironment.GetGame().GetCatalog().GetMarketplace()
            .SerializeOffers(MinPrice, MaxPrice, SearchQuery, FilterMode));
    }

    private void MarketplacePurchase()
    {
        uint ItemId = Request.PopWiredUInt();
        DataRow Row = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Row = dbClient.ReadDataRow("SELECT * FROM catalog_marketplace_offers WHERE offer_id = '" + ItemId + "' LIMIT 1");
        }
        if (Row == null || (string)Row["state"] != "1" || (double)Row["timestamp"] <= HolographEnvironment.GetGame().GetCatalog().GetMarketplace()
            .FormatTimestamp())
        {
            Session.SendNotif("Sorry, this offer has expired.");
            return;
        }
        Item Item = HolographEnvironment.GetGame().GetItemManager().GetItem((uint)Row["item_id"]);
        if (Item != null)
        {
            if ((int)Row["total_price"] >= 1)
            {
                Session.GetHabbo().Credits -= (int)Row["total_price"];
                Session.GetHabbo().UpdateCreditsBalance(InDatabase: true);
            }
            HolographEnvironment.GetGame().GetCatalog().DeliverItems(Session, Item, 1, (string)Row["extra_data"]);
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update catalog_marketplace_offers SET state = '2' WHERE offer_id = '" + ItemId + "' LIMIT 1");
            }
            Session.GetMessageHandler().GetResponse().Init(67u);
            Session.GetMessageHandler().GetResponse().AppendUInt(Item.ItemId);
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Item.Name);
            Session.GetMessageHandler().GetResponse().AppendInt32(0);
            Session.GetMessageHandler().GetResponse().AppendInt32(0);
            Session.GetMessageHandler().GetResponse().AppendInt32(1);
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Item.Type.ToLower());
            Session.GetMessageHandler().GetResponse().AppendInt32(Item.SpriteId);
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak("");
            Session.GetMessageHandler().GetResponse().AppendInt32(1);
            Session.GetMessageHandler().GetResponse().AppendInt32(-1);
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak("");
            Session.GetMessageHandler().SendResponse();
            Session.SendMessage(HolographEnvironment.GetGame().GetCatalog().GetMarketplace()
                .SerializeOffers(-1, -1, "", 1));
        }
    }

    private void CheckPetName()
    {
        Session.GetMessageHandler().GetResponse().Init(36u);
        Session.GetMessageHandler().GetResponse().AppendInt32((!HolographEnvironment.GetGame().GetCatalog().CheckPetName(Request.PopFixedString())) ? 2 : 0);
        Session.GetMessageHandler().SendResponse();
    }

    public void RegisterCatalog()
    {
        RequestHandlers[101] = GetCatalogIndex;
        RequestHandlers[102] = GetCatalogPage;
        RequestHandlers[129] = RedeemVoucher;
        RequestHandlers[100] = HandlePurchase;
        RequestHandlers[472] = PurchaseGift;
        RequestHandlers[412] = GetRecyclerRewards;
        RequestHandlers[3030] = CanGift;
        RequestHandlers[3011] = GetCataData1;
        RequestHandlers[473] = GetCataData2;
        RequestHandlers[3012] = MarketplaceCanSell;
        RequestHandlers[3010] = MarketplacePostItem;
        RequestHandlers[3019] = MarketplaceGetOwnOffers;
        RequestHandlers[3015] = MarketplaceTakeBack;
        RequestHandlers[3016] = MarketplaceClaimCredits;
        RequestHandlers[3018] = MarketplaceGetOffers;
        RequestHandlers[3014] = MarketplacePurchase;
        RequestHandlers[42] = CheckPetName;
    }

    public GameClientMessageHandler(GameClient Session)
    {
        this.Session = Session;
        RequestHandlers = new RequestHandler[4004];
        Response = new ServerMessage(0u);
    }

    public ServerMessage GetResponse()
    {
        return Response;
    }

    public void Destroy()
    {
        Session = null;
        RequestHandlers = null;
        Request = null;
        Response = null;
    }

    public void HandleRequest(ClientMessage Request)
    {
        if (Request.Id >= 0 && Request.Id <= 4004 && RequestHandlers[Request.Id] != null)
        {
            this.Request = Request;
            RequestHandlers[Request.Id]();
            this.Request = null;
        }
    }

    public void SendResponse()
    {
        if (Response.Id != 0)
        {
            Session.GetConnection().SendMessage(Response);
        }
    }

    private void SendSessionParams()
    {
        Response.Init(257u);
        Response.AppendInt32(9);
        Response.AppendInt32(0);
        Response.AppendInt32(0);
        Response.AppendInt32(1);
        Response.AppendInt32(1);
        Response.AppendInt32(3);
        Response.AppendInt32(0);
        Response.AppendInt32(2);
        Response.AppendInt32(1);
        Response.AppendInt32(4);
        Response.AppendInt32(0);
        Response.AppendInt32(5);
        Response.AppendStringWithBreak("dd-MM-yyyy");
        Response.AppendInt32(7);
        Response.AppendBoolean(Bool: false);
        Response.AppendInt32(8);
        Response.AppendStringWithBreak("hotel-co.uk");
        Response.AppendInt32(9);
        Response.AppendBoolean(Bool: false);
        SendResponse();
    }

    private void SSOLogin()
    {
        if (Session.GetHabbo() == null)
        {
            Session.Login(Request.PopFixedString());
        }
        else
        {
            Session.SendNotif("You are already logged in!");
        }
    }

    public void RegisterHandshake()
    {
        RequestHandlers[206] = SendSessionParams;
        RequestHandlers[415] = SSOLogin;
    }

    private void GetAdvertisement()
    {
        RoomAdvertisement Ad = HolographEnvironment.GetGame().GetAdvertisementManager().GetRandomRoomAdvertisement();
        GetResponse().Init(258u);
        if (Ad == null)
        {
            GetResponse().AppendStringWithBreak("");
            GetResponse().AppendStringWithBreak("");
        }
        else
        {
            GetResponse().AppendStringWithBreak(Ad.AdImage);
            GetResponse().AppendStringWithBreak(Ad.AdLink);
            Ad.OnView();
        }
        SendResponse();
    }

    private void GetPub()
    {
        uint Id = Request.PopWiredUInt();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id);
        if (Data != null && !(Data.Type != "public"))
        {
            GetResponse().Init(453u);
            GetResponse().AppendUInt(Data.Id);
            GetResponse().AppendStringWithBreak(Data.CCTs);
            GetResponse().AppendUInt(Data.Id);
            SendResponse();
        }
    }

    private void OpenFlat()
    {
        uint Id = Request.PopWiredUInt();
        string Password = Request.PopFixedString();
        int Junk = Request.PopWiredInt32();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id);
        if (Data != null && !(Data.Type != "private"))
        {
            PrepareRoomForUser(Id, Password);
        }
    }

    private void OpenPub()
    {
        int Junk = Request.PopWiredInt32();
        uint Id = Request.PopWiredUInt();
        int Junk2 = Request.PopWiredInt32();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id);
        if (Data != null && !(Data.Type != "public"))
        {
            PrepareRoomForUser(Data.Id, "");
        }
    }

    private void GetGroupBadges()
    {
        GetResponse().Init(309u);
        GetResponse().AppendStringWithBreak("IcIrDs43103s19014d5a1dc291574a508bc80a64663e61a00");
        SendResponse();
    }

    private void GetInventory()
    {
        Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeItemInventory());
    }

    private void GetRoomData1()
    {
        if (Session.GetHabbo().LoadingRoom != 0)
        {
            GetResponse().Init(297u);
            GetResponse().AppendInt32(0);
            SendResponse();
        }
    }

    private void GetRoomData2()
    {
        if (Session.GetHabbo().LoadingRoom == 0)
        {
            return;
        }
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Session.GetHabbo().LoadingRoom);
        if (Data != null)
        {
            if (Data.Model == null)
            {
                Session.SendNotif("Sorry, model data is missing from this room and therefore cannot be loaded.");
                Session.SendMessage(new ServerMessage(18u));
                ClearRoomLoading();
            }
            else
            {
                Session.SendMessage(Data.Model.SerializeHeightmap());
                Session.SendMessage(Data.Model.SerializeRelativeHeightmap());
            }
        }
    }

    private void GetRoomData3()
    {
        if (Session.GetHabbo().LoadingRoom == 0 || !Session.GetHabbo().LoadingChecksPassed)
        {
            return;
        }
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().LoadingRoom);
        if (Room == null)
        {
            return;
        }
        ClearRoomLoading();
        GetResponse().Init(30u);
        if (Room.Model.StaticFurniMap != "")
        {
            GetResponse().AppendStringWithBreak(Room.Model.StaticFurniMap);
        }
        else
        {
            GetResponse().AppendInt32(0);
        }
        SendResponse();
        if (Room.Type == "private")
        {
            List<RoomItem> FloorItems = Room.FloorItems;
            List<RoomItem> WallItems = Room.WallItems;
            GetResponse().Init(32u);
            GetResponse().AppendInt32(FloorItems.Count);
            foreach (RoomItem Item in FloorItems)
            {
                Item.Serialize(GetResponse());
            }
            SendResponse();
            GetResponse().Init(45u);
            GetResponse().AppendInt32(WallItems.Count);
            foreach (RoomItem Item in WallItems)
            {
                Item.Serialize(GetResponse());
            }
            SendResponse();
        }
        Room.AddUserToRoom(Session, Session.GetHabbo().SpectatorMode);
        List<RoomUser> UsersToDisplay = new List<RoomUser>();
        foreach (RoomUser User in Room.UserList)
        {
            if (!User.IsSpectator)
            {
                UsersToDisplay.Add(User);
            }
        }
        GetResponse().Init(28u);
        GetResponse().AppendInt32(UsersToDisplay.Count);
        foreach (RoomUser User in UsersToDisplay)
        {
            User.Serialize(GetResponse());
        }
        SendResponse();
        GetResponse().Init(472u);
        GetResponse().AppendBoolean(Room.Hidewall);
        SendResponse();
        if (Room.Type == "public")
        {
            GetResponse().Init(471u);
            GetResponse().AppendBoolean(Bool: false);
            GetResponse().AppendStringWithBreak(Room.ModelName);
            GetResponse().AppendBoolean(Bool: false);
            SendResponse();
        }
        else if (Room.Type == "private")
        {
            GetResponse().Init(471u);
            GetResponse().AppendBoolean(Bool: true);
            GetResponse().AppendUInt(Room.RoomId);
            if (Room.CheckRights(Session, RequireOwnership: true))
            {
                GetResponse().AppendBoolean(Bool: true);
            }
            else
            {
                GetResponse().AppendBoolean(Bool: false);
            }
            SendResponse();
            GetResponse().Init(454u);
            GetResponse().AppendInt32(1);
            GetResponse().AppendUInt(Room.RoomId);
            GetResponse().AppendInt32(0);
            GetResponse().AppendStringWithBreak(Room.Name);
            GetResponse().AppendStringWithBreak(Room.Owner);
            GetResponse().AppendInt32(Room.State);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(25);
            GetResponse().AppendStringWithBreak(Room.Description);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(8228);
            GetResponse().AppendInt32(Room.Category);
            GetResponse().AppendStringWithBreak("");
            GetResponse().AppendInt32(Room.TagCount);
            foreach (string Tag in Room.Tags)
            {
                GetResponse().AppendStringWithBreak(Tag);
            }
            Room.Icon.Serialize(GetResponse());
            GetResponse().AppendBoolean(Bool: false);
            SendResponse();
        }
        ServerMessage Updates = Room.SerializeStatusUpdates(All: true);
        if (Updates != null)
        {
            Session.SendMessage(Updates);
        }
        foreach (RoomUser User in Room.UserList)
        {
            if (!User.IsSpectator)
            {
                if (User.IsDancing)
                {
                    GetResponse().Init(480u);
                    GetResponse().AppendInt32(User.VirtualId);
                    GetResponse().AppendInt32(User.DanceId);
                    SendResponse();
                }
                if (User.IsAsleep)
                {
                    GetResponse().Init(486u);
                    GetResponse().AppendInt32(User.VirtualId);
                    GetResponse().AppendBoolean(Bool: true);
                    SendResponse();
                }
                if (User.CarryItemID > 0 && User.CarryTimer > 0)
                {
                    GetResponse().Init(482u);
                    GetResponse().AppendInt32(User.VirtualId);
                    GetResponse().AppendInt32(User.CarryTimer);
                    SendResponse();
                }
                if (!User.IsBot && User.GetClient().GetHabbo() != null && User.GetClient().GetHabbo().GetAvatarEffectsInventoryComponent() != null && User.GetClient().GetHabbo().GetAvatarEffectsInventoryComponent()
                    .CurrentEffect >= 1)
                {
                    GetResponse().Init(485u);
                    GetResponse().AppendInt32(User.VirtualId);
                    GetResponse().AppendInt32(User.GetClient().GetHabbo().GetAvatarEffectsInventoryComponent()
                        .CurrentEffect);
                    SendResponse();
                }
            }
        }
    }

    public void PrepareRoomForUser(uint Id, string Password)
    {
        ClearRoomLoading();
        if (HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id) == null)
        {
            return;
        }
        if (Session.GetHabbo().InRoom)
        {
            HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId)?.RemoveUserFromRoom(Session, NotifyClient: false, NotifyKick: false);
        }
        if (!HolographEnvironment.GetGame().GetRoomManager().IsRoomLoaded(Id))
        {
            HolographEnvironment.GetGame().GetRoomManager().LoadRoom(Id);
        }
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Id);
        if (Room == null)
        {
            return;
        }
        Session.GetHabbo().LoadingRoom = Id;
        if (Room.UserIsBanned(Session.GetHabbo().Id))
        {
            if (!Room.HasBanExpired(Session.GetHabbo().Id))
            {
                GetResponse().Init(224u);
                GetResponse().AppendInt32(4);
                SendResponse();
                GetResponse().Init(18u);
                SendResponse();
                return;
            }
            Room.RemoveBan(Session.GetHabbo().Id);
        }
        if (Room.UsersNow >= Room.UsersMax && !HolographEnvironment.GetGame().GetRoleManager().RankHasRight(Session.GetHabbo().Rank, "fuse_enter_full_rooms"))
        {
            GetResponse().Init(224u);
            GetResponse().AppendInt32(1);
            SendResponse();
            GetResponse().Init(18u);
            SendResponse();
            return;
        }
        if (Room.Type == "public")
        {
            if (Room.State > 0 && !Session.GetHabbo().HasFuse("fuse_mod"))
            {
                Session.SendNotif("This public room is accessible to Zero staff only.");
                GetResponse().Init(18u);
                SendResponse();
                return;
            }
            GetResponse().Init(166u);
            GetResponse().AppendStringWithBreak("/client/public/" + Room.ModelName + "/0");
            SendResponse();
        }
        else if (Room.Type == "private")
        {
            GetResponse().Init(19u);
            SendResponse();
            if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && !Room.CheckRights(Session, RequireOwnership: true) && !Session.GetHabbo().IsTeleporting)
            {
                if (Room.State == 1)
                {
                    if (Room.UserCount == 0)
                    {
                        GetResponse().Init(131u);
                        SendResponse();
                        return;
                    }
                    GetResponse().Init(91u);
                    GetResponse().AppendStringWithBreak("");
                    SendResponse();
                    ServerMessage RingMessage = new ServerMessage(91u);
                    RingMessage.AppendStringWithBreak(Session.GetHabbo().Username);
                    Room.SendMessageToUsersWithRights(RingMessage);
                    return;
                }
                if (Room.State == 2 && Password.ToLower() != Room.Password.ToLower())
                {
                    GetResponse().Init(33u);
                    GetResponse().AppendInt32(-100002);
                    SendResponse();
                    GetResponse().Init(18u);
                    SendResponse();
                    return;
                }
            }
            GetResponse().Init(166u);
            GetResponse().AppendStringWithBreak("/client/private/" + Room.RoomId + "/id");
            SendResponse();
        }
        Session.GetHabbo().LoadingChecksPassed = true;
        LoadRoomForUser();
    }

    private void ReqLoadRoomForUser()
    {
        LoadRoomForUser();
    }

    public void LoadRoomForUser()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().LoadingRoom);
        if (Room == null || !Session.GetHabbo().LoadingChecksPassed)
        {
            return;
        }
        GetResponse().Init(309u);
        GetResponse().AppendStringWithBreak("IcIrDs43103s19014d5a1dc291574a508bc80a64663e61a00");
        SendResponse();
        GetResponse().Init(69u);
        GetResponse().AppendStringWithBreak(Room.ModelName);
        GetResponse().AppendUInt(Room.RoomId);
        SendResponse();
        if (Session.GetHabbo().SpectatorMode)
        {
            GetResponse().Init(254u);
            SendResponse();
        }
        if (Room.Type == "private")
        {
            if (Room.Wallpaper != "0.0")
            {
                GetResponse().Init(46u);
                GetResponse().AppendStringWithBreak("wallpaper");
                GetResponse().AppendStringWithBreak(Room.Wallpaper);
                SendResponse();
            }
            if (Room.Floor != "0.0")
            {
                GetResponse().Init(46u);
                GetResponse().AppendStringWithBreak("floor");
                GetResponse().AppendStringWithBreak(Room.Floor);
                SendResponse();
            }
            GetResponse().Init(46u);
            GetResponse().AppendStringWithBreak("landscape");
            GetResponse().AppendStringWithBreak(Room.Landscape);
            SendResponse();
            if (Room.CheckRights(Session, RequireOwnership: true))
            {
                GetResponse().Init(42u);
                SendResponse();
                GetResponse().Init(47u);
                SendResponse();
            }
            else if (Room.CheckRights(Session))
            {
                GetResponse().Init(42u);
                SendResponse();
            }
            GetResponse().Init(345u);
            if (Session.GetHabbo().RatedRooms.Contains(Room.RoomId) || Room.CheckRights(Session, RequireOwnership: true))
            {
                GetResponse().AppendInt32(Room.Score);
            }
            else
            {
                GetResponse().AppendInt32(-1);
            }
            SendResponse();
            if (Room.HasOngoingEvent)
            {
                Session.SendMessage(Room.Event.Serialize(Session));
                return;
            }
            GetResponse().Init(370u);
            GetResponse().AppendStringWithBreak("-1");
            SendResponse();
        }
    }

    public void ClearRoomLoading()
    {
        Session.GetHabbo().LoadingRoom = 0u;
        Session.GetHabbo().LoadingChecksPassed = false;
    }

    private void Talk()
    {
        HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId)?.GetRoomUserByHabbo(Session.GetHabbo().Id)?.Chat(Session, HolographEnvironment.FilterInjectionChars(Request.PopFixedString()), Shout: false);
    }

    private void Shout()
    {
        HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId)?.GetRoomUserByHabbo(Session.GetHabbo().Id)?.Chat(Session, HolographEnvironment.FilterInjectionChars(Request.PopFixedString()), Shout: true);
    }

    private void Whisper()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        if (Session.GetHabbo().Muted)
        {
            Session.SendNotif("You are muted.");
            return;
        }
        string Params = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        string ToUser = Params.Split(' ')[0];
        string Message = Params.Substring(ToUser.Length + 1);
        RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
        RoomUser User2 = Room.GetRoomUserByHabbo(ToUser);
        ServerMessage TellMsg = new ServerMessage();
        TellMsg.Init(25u);
        TellMsg.AppendInt32(User.VirtualId);
        TellMsg.AppendStringWithBreak(Message);
        TellMsg.AppendBoolean(Bool: false);
        if (User != null && !User.IsBot)
        {
            User.GetClient().SendMessage(TellMsg);
        }
        User.Unidle();
        if (User2 == null || User2.IsBot)
        {
            return;
        }
        if (!User2.GetClient().GetHabbo().MutedUsers.Contains(Session.GetHabbo().Id))
        {
            User2.GetClient().SendMessage(TellMsg);
        }
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.AddParamWithValue("message", "<Whisper to " + User2.GetClient().GetHabbo().Username + ">: " + Message);
        dbClient.ExecuteQuery("INSERT INTO chatlogs (user_id,room_id,hour,minute,timestamp,message,user_name,full_date) VALUES ('" + Session.GetHabbo().Id + "','" + Room.RoomId + "','" + DateTime.Now.Hour + "','" + DateTime.Now.Minute + "','" + HolographEnvironment.GetUnixTimestamp() + "',@message,'" + Session.GetHabbo().Username + "','" + DateTime.Now.ToLongDateString() + "')");
    }

    private void Move()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (User != null && User.CanWalk)
        {
            int MoveX = Request.PopWiredInt32();
            int MoveY = Request.PopWiredInt32();
            if (MoveX != User.X || MoveY != User.Y)
            {
                User.MoveTo(MoveX, MoveY);
            }
        }
    }

    private void CanCreateRoom()
    {
        GetResponse().Init(512u);
        GetResponse().AppendBoolean(Bool: false);
        GetResponse().AppendInt32(99999);
        SendResponse();
    }

    private void CreateRoom()
    {
        string RoomName = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        string ModelName = Request.PopFixedString();
        string RoomState = Request.PopFixedString();
        RoomData NewRoom = HolographEnvironment.GetGame().GetRoomManager().CreateRoom(Session, RoomName, ModelName);
        if (NewRoom != null)
        {
            GetResponse().Init(59u);
            GetResponse().AppendUInt(NewRoom.Id);
            GetResponse().AppendStringWithBreak(NewRoom.Name);
            SendResponse();
        }
    }

    private void GetRoomEditData()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        GetResponse().Init(465u);
        GetResponse().AppendUInt(Room.RoomId);
        GetResponse().AppendStringWithBreak(Room.Name);
        GetResponse().AppendStringWithBreak(Room.Description);
        GetResponse().AppendInt32(Room.State);
        GetResponse().AppendInt32(Room.Category);
        GetResponse().AppendInt32(Room.UsersMax);
        GetResponse().AppendInt32(25);
        GetResponse().AppendInt32(Room.TagCount);
        foreach (string Tag in Room.Tags)
        {
            GetResponse().AppendStringWithBreak(Tag);
        }
        GetResponse().AppendInt32(Room.UsersWithRights.Count);
        foreach (uint UserId in Room.UsersWithRights)
        {
            GetResponse().AppendUInt(UserId);
            GetResponse().AppendStringWithBreak(HolographEnvironment.GetGame().GetClientManager().GetNameById(UserId));
        }
        GetResponse().AppendInt32(Room.UsersWithRights.Count);
        GetResponse().AppendBoolean(Room.AllowPets);
        GetResponse().AppendBoolean(Room.AllowPetsEating);
        GetResponse().AppendBoolean(Room.AllowWalkthrough);
        GetResponse().AppendBoolean(Room.Hidewall);
        SendResponse();
    }

    private void SaveRoomIcon()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        int Junk = Request.PopWiredInt32();
        Dictionary<int, int> Items = new Dictionary<int, int>();
        int Background = Request.PopWiredInt32();
        int TopLayer = Request.PopWiredInt32();
        int AmountOfItems = Request.PopWiredInt32();
        for (int i = 0; i < AmountOfItems; i++)
        {
            int Pos = Request.PopWiredInt32();
            int Item = Request.PopWiredInt32();
            if (Pos < 0 || Pos > 10 || Item < 1 || Item > 27 || Items.ContainsKey(Pos))
            {
                return;
            }
            Items.Add(Pos, Item);
        }
        if (Background < 1 || Background > 24 || TopLayer < 0 || TopLayer > 11)
        {
            return;
        }
        StringBuilder FormattedItems = new StringBuilder();
        int j = 0;
        foreach (KeyValuePair<int, int> Item2 in Items)
        {
            if (j > 0)
            {
                FormattedItems.Append("|");
            }
            FormattedItems.Append(Item2.Key + "," + Item2.Value);
            j++;
        }
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("Update rooms SET icon_bg = '" + Background + "', icon_fg = '" + TopLayer + "', icon_items = '" + FormattedItems.ToString() + "' WHERE id = '" + Room.RoomId + "' LIMIT 1");
        }
        Room.Icon = new RoomIcon(Background, TopLayer, Items);
        GetResponse().Init(457u);
        GetResponse().AppendUInt(Room.RoomId);
        GetResponse().AppendBoolean(Bool: true);
        SendResponse();
        GetResponse().Init(456u);
        GetResponse().AppendUInt(Room.RoomId);
        SendResponse();
        RoomData Data = new RoomData();
        Data.Fill(Room);
        GetResponse().Init(454u);
        GetResponse().AppendBoolean(Bool: false);
        Data.Serialize(GetResponse(), ShowEvents: false);
        SendResponse();
    }

    private void SaveRoomData()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        int Id = Request.PopWiredInt32();
        string Name = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        string Description = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        int State = Request.PopWiredInt32();
        string Password = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        int MaxUsers = Request.PopWiredInt32();
        int CategoryId = Request.PopWiredInt32();
        int TagCount = Request.PopWiredInt32();
        List<string> Tags = new List<string>();
        StringBuilder formattedTags = new StringBuilder();
        for (int i = 0; i < TagCount; i++)
        {
            if (i > 0)
            {
                formattedTags.Append(",");
            }
            string tag = HolographEnvironment.FilterInjectionChars(Request.PopFixedString().ToLower());
            Tags.Add(tag);
            formattedTags.Append(tag);
        }
        int AllowPets = 0;
        int AllowPetsEat = 0;
        int AllowWalkthrough = 0;
        int Hidewall = 0;
        string _AllowPets = Request.PlainReadBytes(1)[0].ToString();
        Request.AdvancePointer(1);
        string _AllowPetsEat = Request.PlainReadBytes(1)[0].ToString();
        Request.AdvancePointer(1);
        string _AllowWalkthrough = Request.PlainReadBytes(1)[0].ToString();
        Request.AdvancePointer(1);
        string _Hidewall = Request.PlainReadBytes(1)[0].ToString();
        Request.AdvancePointer(1);
        if (Name.Length < 1 || State < 0 || State > 2 || (MaxUsers != 10 && MaxUsers != 15 && MaxUsers != 20 && MaxUsers != 25))
        {
            return;
        }
        FlatCat FlatCat = HolographEnvironment.GetGame().GetNavigator().GetFlatCat(CategoryId);
        if (FlatCat == null)
        {
            return;
        }
        if (FlatCat.MinRank > Session.GetHabbo().Rank)
        {
            Session.SendNotif("You are not allowed to use this category. Your room has been moved to no category instead.");
            CategoryId = 0;
        }
        if (TagCount <= 2 && State >= 0 && State <= 2)
        {
            if (_AllowPets == "65")
            {
                AllowPets = 1;
                Room.AllowPets = true;
            }
            else
            {
                Room.AllowPets = false;
            }
            if (_AllowPetsEat == "65")
            {
                AllowPetsEat = 1;
                Room.AllowPetsEating = true;
            }
            else
            {
                Room.AllowPetsEating = false;
            }
            if (_AllowWalkthrough == "65")
            {
                AllowWalkthrough = 1;
                Room.AllowWalkthrough = true;
            }
            else
            {
                Room.AllowWalkthrough = false;
            }
            if (_Hidewall == "65")
            {
                Hidewall = 1;
                Room.Hidewall = true;
            }
            else
            {
                Room.Hidewall = false;
            }
            Room.Name = Name;
            Room.State = State;
            Room.Description = Description;
            Room.Category = CategoryId;
            Room.Password = Password;
            Room.Tags = Tags;
            Room.UsersMax = MaxUsers;
            string formattedState = "open";
            if (Room.State == 1)
            {
                formattedState = "locked";
            }
            else if (Room.State > 1)
            {
                formattedState = "password";
            }
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.AddParamWithValue("caption", Room.Name);
                dbClient.AddParamWithValue("description", Room.Description);
                dbClient.AddParamWithValue("password", Room.Password);
                dbClient.AddParamWithValue("tags", formattedTags.ToString());
                dbClient.ExecuteQuery("Update rooms SET caption = @caption, description = @description, password = @password, category = '" + CategoryId + "', state = '" + formattedState + "', tags = @tags, users_max = '" + MaxUsers + "', allow_pets = '" + AllowPets + "', allow_pets_eat = '" + AllowPetsEat + "', allow_walkthrough = '" + AllowWalkthrough + "', allow_hidewall = '" + Hidewall + "' WHERE id = '" + Room.RoomId + "' LIMIT 1;");
            }
            GetResponse().Init(467u);
            GetResponse().AppendUInt(Room.RoomId);
            SendResponse();
            GetResponse().Init(456u);
            GetResponse().AppendUInt(Room.RoomId);
            SendResponse();
            GetResponse().Init(472u);
            GetResponse().AppendBoolean(Room.Hidewall);
            HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId)
                .SendMessage(Response);
            RoomData Data = new RoomData();
            Data.Fill(Room);
            GetResponse().Init(454u);
            GetResponse().AppendBoolean(Bool: false);
            Data.Serialize(GetResponse(), ShowEvents: false);
            SendResponse();
        }
    }

    private void GiveRights()
    {
        uint UserId = Request.PopWiredUInt();
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        RoomUser RoomUser = Room.GetRoomUserByHabbo(UserId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true) || RoomUser == null || RoomUser.IsBot)
        {
            return;
        }
        if (Room.UsersWithRights.Contains(UserId))
        {
            Session.SendNotif("User already has rights! (There appears to be a bug with the rights button, we are looking into it - for now rely on 'Advanced settings')");
            return;
        }
        Room.UsersWithRights.Add(UserId);
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("INSERT INTO room_rights (room_id,user_id) VALUES ('" + Room.RoomId + "','" + UserId + "')");
        }
        GetResponse().Init(510u);
        GetResponse().AppendUInt(Room.RoomId);
        GetResponse().AppendUInt(UserId);
        GetResponse().AppendStringWithBreak(RoomUser.GetClient().GetHabbo().Username);
        SendResponse();
        RoomUser.AddStatus("flatcrtl", "");
        RoomUser.UpdateNeeded = true;
        RoomUser.GetClient().SendMessage(new ServerMessage(42u));
    }

    private void TakeRights()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        StringBuilder DeleteParams = new StringBuilder();
        int Amount = Request.PopWiredInt32();
        for (int i = 0; i < Amount; i++)
        {
            if (i > 0)
            {
                DeleteParams.Append(" OR ");
            }
            uint UserId = Request.PopWiredUInt();
            Room.UsersWithRights.Remove(UserId);
            DeleteParams.Append("room_id = '" + Room.RoomId + "' AND user_id = '" + UserId + "'");
            RoomUser User = Room.GetRoomUserByHabbo(UserId);
            if (User != null && !User.IsBot)
            {
                User.GetClient().SendMessage(new ServerMessage(43u));
            }
            GetResponse().Init(511u);
            GetResponse().AppendUInt(Room.RoomId);
            GetResponse().AppendUInt(UserId);
            SendResponse();
        }
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.ExecuteQuery("DELETE FROM room_rights WHERE " + DeleteParams.ToString());
    }

    private void TakeAllRights()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        foreach (uint UserId in Room.UsersWithRights)
        {
            RoomUser User = Room.GetRoomUserByHabbo(UserId);
            if (User != null && !User.IsBot)
            {
                User.GetClient().SendMessage(new ServerMessage(43u));
            }
            GetResponse().Init(511u);
            GetResponse().AppendUInt(Room.RoomId);
            GetResponse().AppendUInt(UserId);
            SendResponse();
        }
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("DELETE FROM room_rights WHERE room_id = '" + Room.RoomId + "'");
        }
        Room.UsersWithRights.Clear();
    }

    private void KickUser()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session))
        {
            uint UserId = Request.PopWiredUInt();
            RoomUser User = Room.GetRoomUserByHabbo(UserId);
            if (User != null && !User.IsBot && !Room.CheckRights(User.GetClient(), RequireOwnership: true) && !User.GetClient().GetHabbo().HasFuse("fuse_mod"))
            {
                Room.RemoveUserFromRoom(User.GetClient(), NotifyClient: true, NotifyKick: true);
            }
        }
    }

    private void BanUser()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true))
        {
            uint UserId = Request.PopWiredUInt();
            RoomUser User = Room.GetRoomUserByHabbo(UserId);
            if (User != null && !User.IsBot && !User.GetClient().GetHabbo().HasFuse("fuse_mod"))
            {
                Room.AddBan(UserId);
                Room.RemoveUserFromRoom(User.GetClient(), NotifyClient: true, NotifyKick: true);
            }
        }
    }

    private void SetHomeRoom()
    {
        uint RoomId = Request.PopWiredUInt();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(RoomId);
        if (RoomId == 0 || (Data != null && !(Data.Owner.ToLower() != Session.GetHabbo().Username.ToLower())))
        {
            Session.GetHabbo().HomeRoom = RoomId;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update users SET home_room = '" + RoomId + "' WHERE id = '" + Session.GetHabbo().Id + "' LIMIT 1");
            }
            GetResponse().Init(455u);
            GetResponse().AppendUInt(RoomId);
            SendResponse();
        }
    }

    private void DeleteRoom()
    {
        uint RoomId = Request.PopWiredUInt();
        RoomData Data = HolographEnvironment.GetGame().GetRoomManager().GenerateRoomData(RoomId);
        if (Data == null || Data.Owner.ToLower() != Session.GetHabbo().Username.ToLower())
        {
            return;
        }
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("DELETE FROM rooms WHERE id = '" + Data.Id + "' LIMIT 1");
            dbClient.ExecuteQuery("DELETE FROM user_favorites WHERE room_id = '" + Data.Id + "'");
            dbClient.ExecuteQuery("DELETE FROM room_items WHERE room_id = '" + Data.Id + "'");
            dbClient.ExecuteQuery("DELETE FROM room_rights WHERE room_id = '" + Data.Id + "'");
            dbClient.ExecuteQuery("Update users SET home_room = '0' WHERE home_room = '" + Data.Id + "'");
        }
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Data.Id);
        if (Room != null)
        {
            foreach (RoomUser User in Room.UserList)
            {
                if (!User.IsBot)
                {
                    User.GetClient().SendMessage(new ServerMessage(18u));
                    User.GetClient().GetHabbo().OnLeaveRoom();
                }
            }
            HolographEnvironment.GetGame().GetRoomManager().UnloadRoom(Data.Id);
        }
        GetResponse().Init(101u);
        SendResponse();
        Session.SendMessage(HolographEnvironment.GetGame().GetNavigator().SerializeRoomListing(Session, -3));
    }

    private void LookAt()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (User != null)
        {
            User.Unidle();
            int X = Request.PopWiredInt32();
            int Y = Request.PopWiredInt32();
            if (X != User.X || Y != User.Y)
            {
                int Rot = Rotation.Calculate(User.X, User.Y, X, Y);
                User.SetRot(Rot);
            }
        }
    }

    private void StartTyping()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null)
        {
            RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User != null)
            {
                ServerMessage Message = new ServerMessage(361u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendBoolean(Bool: true);
                Room.SendMessage(Message);
            }
        }
    }

    private void StopTyping()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null)
        {
            RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User != null)
            {
                ServerMessage Message = new ServerMessage(361u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendBoolean(Bool: false);
                Room.SendMessage(Message);
            }
        }
    }

    private void IgnoreUser()
    {
    }

    private void UnignoreUser()
    {
    }

    private void CanCreateRoomEvent()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true))
        {
            bool Allow = true;
            int ErrorCode = 0;
            if (Room.State != 0)
            {
                Allow = false;
                ErrorCode = 3;
            }
            GetResponse().Init(367u);
            GetResponse().AppendBoolean(Allow);
            GetResponse().AppendInt32(ErrorCode);
            SendResponse();
        }
    }

    private void StartEvent()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true) && Room.Event == null && Room.State == 0)
        {
            int category = Request.PopWiredInt32();
            string name = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
            string descr = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
            int tagCount = Request.PopWiredInt32();
            Room.Event = new RoomEvent(Room.RoomId, name, descr, category, null);
            Room.Event.Tags = new List<string>();
            for (int i = 0; i < tagCount; i++)
            {
                Room.Event.Tags.Add(HolographEnvironment.FilterInjectionChars(Request.PopFixedString()));
            }
            Room.SendMessage(Room.Event.Serialize(Session));
        }
    }

    private void StopEvent()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true) && Room.Event != null)
        {
            Room.Event = null;
            ServerMessage Message = new ServerMessage(370u);
            Message.AppendStringWithBreak("-1");
            Room.SendMessage(Message);
        }
    }

    private void EditEvent()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true) && Room.Event != null)
        {
            int category = Request.PopWiredInt32();
            string name = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
            string descr = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
            int tagCount = Request.PopWiredInt32();
            Room.Event.Category = category;
            Room.Event.Name = name;
            Room.Event.Description = descr;
            Room.Event.Tags = new List<string>();
            for (int i = 0; i < tagCount; i++)
            {
                Room.Event.Tags.Add(HolographEnvironment.FilterInjectionChars(Request.PopFixedString()));
            }
            Room.SendMessage(Room.Event.Serialize(Session));
        }
    }

    private void Wave()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null)
        {
            RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User != null)
            {
                User.Unidle();
                User.DanceId = 0;
                ServerMessage Message = new ServerMessage(481u);
                Message.AppendInt32(User.VirtualId);
                Room.SendMessage(Message);
            }
        }
    }

    private void GetUserTags()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomUser User = Room.GetRoomUserByHabbo(Request.PopWiredUInt());
        if (User == null || User.IsBot)
        {
            return;
        }
        GetResponse().Init(350u);
        GetResponse().AppendUInt(User.GetClient().GetHabbo().Id);
        GetResponse().AppendInt32(User.GetClient().GetHabbo().Tags.Count);
        foreach (string Tag in User.GetClient().GetHabbo().Tags)
        {
            GetResponse().AppendStringWithBreak(Tag);
        }
        SendResponse();
    }

    private void GetUserBadges()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomUser User = Room.GetRoomUserByHabbo(Request.PopWiredUInt());
        if (User == null || User.IsBot)
        {
            return;
        }
        GetResponse().Init(228u);
        GetResponse().AppendUInt(User.GetClient().GetHabbo().Id);
        GetResponse().AppendInt32(User.GetClient().GetHabbo().GetBadgeComponent()
            .EquippedCount);
        foreach (Badge Badge in User.GetClient().GetHabbo().GetBadgeComponent()
            .BadgeList)
        {
            if (Badge.Slot > 0)
            {
                GetResponse().AppendInt32(Badge.Slot);
                GetResponse().AppendStringWithBreak(Badge.Code);
            }
        }
        SendResponse();
    }

    private void RateRoom()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && !Session.GetHabbo().RatedRooms.Contains(Room.RoomId) && !Room.CheckRights(Session, RequireOwnership: true))
        {
            switch (Request.PopWiredInt32())
            {
                default:
                    return;
                case -1:
                    Room.Score--;
                    break;
                case 1:
                    Room.Score++;
                    break;
                case 0:
                    return;
            }
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update rooms SET score = '" + Room.Score + "' WHERE id = '" + Room.RoomId + "' LIMIT 1");
            }
            Session.GetHabbo().RatedRooms.Add(Room.RoomId);
            GetResponse().Init(345u);
            GetResponse().AppendInt32(Room.Score);
            SendResponse();
        }
    }

    private void Dance()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
        if (User != null)
        {
            User.Unidle();
            int DanceId = Request.PopWiredInt32();
            if (DanceId < 0 || DanceId > 4 || (!Session.GetHabbo().HasFuse("fuse_use_club_dance") && DanceId > 1))
            {
                DanceId = 0;
            }
            if (DanceId > 0 && User.CarryItemID > 0)
            {
                User.CarryItem(0);
            }
            User.DanceId = DanceId;
            ServerMessage DanceMessage = new ServerMessage(480u);
            DanceMessage.AppendInt32(User.VirtualId);
            DanceMessage.AppendInt32(DanceId);
            Room.SendMessage(DanceMessage);
        }
    }

    private void AnswerDoorbell()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session))
        {
            return;
        }
        string Name = Request.PopFixedString();
        byte[] Result = Request.ReadBytes(1);
        GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Name);
        if (Client != null)
        {
            if (Result[0] == Convert.ToByte(65))
            {
                Client.GetHabbo().LoadingChecksPassed = true;
                Client.GetMessageHandler().GetResponse().Init(41u);
                Client.GetMessageHandler().SendResponse();
            }
            else
            {
                Client.GetMessageHandler().GetResponse().Init(131u);
                Client.GetMessageHandler().SendResponse();
            }
        }
    }

    private void ApplyRoomEffect()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.PopWiredUInt());
        if (Item != null)
        {
            string type = "floor";
            if (Item.GetBaseItem().Name.ToLower().Contains("wallpaper"))
            {
                type = "wallpaper";
            }
            else if (Item.GetBaseItem().Name.ToLower().Contains("landscape"))
            {
                type = "landscape";
            }
            switch (type)
            {
                case "floor":
                    Room.Floor = Item.ExtraData;
                    break;
                case "wallpaper":
                    Room.Wallpaper = Item.ExtraData;
                    break;
                case "landscape":
                    Room.Landscape = Item.ExtraData;
                    break;
            }
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update rooms SET " + type + " = '" + Item.ExtraData + "' WHERE id = '" + Room.RoomId + "' LIMIT 1");
            }
            Session.GetHabbo().GetInventoryComponent().RemoveItem(Item.Id);
            ServerMessage Message = new ServerMessage(46u);
            Message.AppendStringWithBreak(type);
            Message.AppendStringWithBreak(Item.ExtraData);
            Room.SendMessage(Message);
        }
    }

    private void PlaceItem()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session))
        {
            return;
        }
        string PlacementData = Request.PopFixedString();
        string[] DataBits = PlacementData.Split(' ');
        uint ItemId = uint.Parse(DataBits[0]);
        UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(ItemId);
        if (Item == null)
        {
            return;
        }
        string text = Item.GetBaseItem().InteractionType.ToLower();
        if (text != null && text == "dimmer" && Room.ItemCountByType("dimmer") >= 1)
        {
            Session.SendNotif("You can only have one moodlight in a room.");
        }
        else if (DataBits[1].StartsWith(":"))
        {
            string WallPos = Room.WallPositionCheck(":" + PlacementData.Split(':')[1]);
            if (WallPos == null)
            {
                GetResponse().Init(516u);
                GetResponse().AppendInt32(11);
                SendResponse();
                return;
            }
            RoomItem RoomItem = new RoomItem(Item.Id, Room.RoomId, Item.BaseItem, Item.ExtraData, 0, 0, 0.0, 0, WallPos);
            if (Room.SetWallItem(Session, RoomItem))
            {
                Session.GetHabbo().GetInventoryComponent().RemoveItem(ItemId);
            }
        }
        else
        {
            int X = int.Parse(DataBits[1]);
            int Y = int.Parse(DataBits[2]);
            int Rot = int.Parse(DataBits[3]);
            RoomItem RoomItem = new RoomItem(Item.Id, Room.RoomId, Item.BaseItem, Item.ExtraData, 0, 0, 0.0, 0, "");
            if (Room.SetFloorItem(Session, RoomItem, X, Y, Rot, newItem: true))
            {
                Session.GetHabbo().GetInventoryComponent().RemoveItem(ItemId);
            }
        }
    }

    private void TakeItem()
    {
        int junk = Request.PopWiredInt32();
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        RoomItem Item = Room.GetItem(Request.PopWiredUInt());
        if (Item != null)
        {
            string text = Item.GetBaseItem().InteractionType.ToLower();
            if (text == null || !(text == "postit"))
            {
                Room.RemoveFurniture(Session, Item.Id);
                Session.GetHabbo().GetInventoryComponent().AddItem(Item.Id, Item.BaseItem, Item.ExtraData);
                Session.GetHabbo().GetInventoryComponent().UpdateItems(FromDatabase: false);
            }
        }
    }

    private void MoveItem()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session))
        {
            RoomItem Item = Room.GetItem(Request.PopWiredUInt());
            if (Item != null)
            {
                int x = Request.PopWiredInt32();
                int y = Request.PopWiredInt32();
                int Rotation = Request.PopWiredInt32();
                int Junk = Request.PopWiredInt32();
                Room.SetFloorItem(Session, Item, x, y, Rotation, newItem: false);
            }
        }
    }

    private void TriggerItem()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomItem Item = Room.GetItem(Request.PopWiredUInt());
        if (Item != null)
        {
            bool hasRights = false;
            if (Room.CheckRights(Session))
            {
                hasRights = true;
            }
            Item.Interactor.OnTrigger(Session, Item, Request.PopWiredInt32(), hasRights);
        }
    }

    private void TriggerItemDiceSpecial()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomItem Item = Room.GetItem(Request.PopWiredUInt());
        if (Item != null)
        {
            bool hasRights = false;
            if (Room.CheckRights(Session))
            {
                hasRights = true;
            }
            Item.Interactor.OnTrigger(Session, Item, -1, hasRights);
        }
    }

    private void OpenPostit()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null)
        {
            RoomItem Item = Room.GetItem(Request.PopWiredUInt());
            if (Item != null && !(Item.GetBaseItem().InteractionType.ToLower() != "postit"))
            {
                GetResponse().Init(48u);
                GetResponse().AppendStringWithBreak(Item.Id.ToString());
                GetResponse().AppendStringWithBreak(Item.ExtraData);
                SendResponse();
            }
        }
    }

    private void SavePostit()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null)
        {
            return;
        }
        RoomItem Item = Room.GetItem(Request.PopWiredUInt());
        if (Item == null || Item.GetBaseItem().InteractionType.ToLower() != "postit")
        {
            return;
        }
        string Data = Request.PopFixedString();
        string Color = Data.Split(' ')[0];
        string Text = HolographEnvironment.FilterInjectionChars(Data.Substring(Color.Length + 1), AllowLinebreaks: true);
        if (Room.CheckRights(Session) || Data.StartsWith(Item.ExtraData))
        {
            switch (Color)
            {
                case "FFFF33":
                case "FF9CFF":
                case "9CCEFF":
                case "9CFF9C":
                    Item.ExtraData = Color + " " + Text;
                    Item.UpdateState(inDb: true, inRoom: true);
                    break;
            }
        }
    }

    private void DeletePostit()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true))
        {
            RoomItem Item = Room.GetItem(Request.PopWiredUInt());
            if (Item != null && !(Item.GetBaseItem().InteractionType.ToLower() != "postit"))
            {
                Room.RemoveFurniture(Session, Item.Id);
            }
        }
    }

    private void OpenPresent()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        RoomItem Present = Room.GetItem(Request.PopWiredUInt());
        if (Present == null)
        {
            return;
        }
        DataRow Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataRow("SELECT SQL_NO_CACHE base_id,amount,extra_data FROM user_presents WHERE item_id = '" + Present.Id + "' LIMIT 1");
        }
        if (Data == null)
        {
            return;
        }
        Item BaseItem = HolographEnvironment.GetGame().GetItemManager().GetItem((uint)Data["base_id"]);
        if (BaseItem != null)
        {
            Room.RemoveFurniture(Session, Present.Id);
            GetResponse().Init(219u);
            GetResponse().AppendUInt(Present.Id);
            SendResponse();
            GetResponse().Init(129u);
            GetResponse().AppendStringWithBreak(BaseItem.Type);
            GetResponse().AppendInt32(BaseItem.SpriteId);
            GetResponse().AppendStringWithBreak(BaseItem.Name);
            SendResponse();
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("DELETE FROM user_presents WHERE item_id = '" + Present.Id + "' LIMIT 1");
            }
            HolographEnvironment.GetGame().GetCatalog().DeliverItems(Session, BaseItem, (int)Data["amount"], (string)Data["extra_data"]);
        }
    }

    private void GetMoodlight()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true) || Room.MoodlightData == null)
        {
            return;
        }
        GetResponse().Init(365u);
        GetResponse().AppendInt32(Room.MoodlightData.Presets.Count);
        GetResponse().AppendInt32(Room.MoodlightData.CurrentPreset);
        int i = 0;
        foreach (MoodlightPreset Preset in Room.MoodlightData.Presets)
        {
            i++;
            GetResponse().AppendInt32(i);
            GetResponse().AppendInt32(int.Parse(HolographEnvironment.BoolToEnum(Preset.BackgroundOnly)) + 1);
            GetResponse().AppendStringWithBreak(Preset.ColorCode);
            GetResponse().AppendInt32(Preset.ColorIntensity);
        }
        SendResponse();
    }

    private void UpdateMoodlight()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true) || Room.MoodlightData == null)
        {
            return;
        }
        RoomItem Item = null;
        foreach (RoomItem I in Room.Items)
        {
            if (I.GetBaseItem().InteractionType.ToLower() == "dimmer")
            {
                Item = I;
                break;
            }
        }
        if (Item != null)
        {
            int Preset = Request.PopWiredInt32();
            int BackgroundMode = Request.PopWiredInt32();
            string ColorCode = Request.PopFixedString();
            int Intensity = Request.PopWiredInt32();
            bool BackgroundOnly = false;
            if (BackgroundMode >= 2)
            {
                BackgroundOnly = true;
            }
            Room.MoodlightData.Enabled = true;
            Room.MoodlightData.CurrentPreset = Preset;
            Room.MoodlightData.UpdatePreset(Preset, ColorCode, Intensity, BackgroundOnly);
            Item.ExtraData = Room.MoodlightData.GenerateExtraData();
            Item.UpdateState();
        }
    }

    private void SwitchMoodlightStatus()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true) || Room.MoodlightData == null)
        {
            return;
        }
        RoomItem Item = null;
        foreach (RoomItem I in Room.Items)
        {
            if (I.GetBaseItem().InteractionType.ToLower() == "dimmer")
            {
                Item = I;
                break;
            }
        }
        if (Item != null)
        {
            if (Room.MoodlightData.Enabled)
            {
                Room.MoodlightData.Disable();
            }
            else
            {
                Room.MoodlightData.Enable();
            }
            Item.ExtraData = Room.MoodlightData.GenerateExtraData();
            Item.UpdateState();
        }
    }

    private void InitTrade()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
            RoomUser User2 = Room.GetRoomUserByVirtualId(Request.PopWiredInt32());
            Room.TryStartTrade(User, User2);
        }
    }

    private void OfferTradeItem()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.PopWiredUInt());
            if (Trade != null && Item != null)
            {
                Trade.OfferItem(Session.GetHabbo().Id, Item);
            }
        }
    }

    private void TakeBackTradeItem()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.PopWiredUInt());
            if (Trade != null && Item != null)
            {
                Trade.TakeBackItem(Session.GetHabbo().Id, Item);
            }
        }
    }

    private void StopTrade()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            Room.TryStopTrade(Session.GetHabbo().Id);
        }
    }

    private void AcceptTrade()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            Room.GetUserTrade(Session.GetHabbo().Id)?.Accept(Session.GetHabbo().Id);
        }
    }

    private void UnacceptTrade()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            Room.GetUserTrade(Session.GetHabbo().Id)?.Unaccept(Session.GetHabbo().Id);
        }
    }

    private void CompleteTrade()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CanTradeInRoom)
        {
            Room.GetUserTrade(Session.GetHabbo().Id)?.CompleteTrade(Session.GetHabbo().Id);
        }
    }

    private void GiveRespect()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || Session.GetHabbo().DailyRespectPoints <= 0)
        {
            return;
        }
        RoomUser User = Room.GetRoomUserByHabbo(Request.PopWiredUInt());
        if (User != null && User.GetClient().GetHabbo().Id != Session.GetHabbo().Id && !User.IsBot)
        {
            Session.GetHabbo().DailyRespectPoints--;
            User.GetClient().GetHabbo().Respect++;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update users SET respect = respect + 1 WHERE id = '" + User.GetClient().GetHabbo().Id + "' LIMIT 1");
                dbClient.ExecuteQuery("Update users SET daily_respect_points = daily_respect_points - 1 WHERE id = '" + Session.GetHabbo().Id + "' LIMIT 1");
            }
            ServerMessage Message = new ServerMessage(440u);
            Message.AppendUInt(User.GetClient().GetHabbo().Id);
            Message.AppendInt32(User.GetClient().GetHabbo().Respect);
            Room.SendMessage(Message);
            if (User.GetClient().GetHabbo().Look != "hd-180-1.ch-210-66.lg-270-82.sh-290-91.hr-100-")
            {
                HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(User.GetClient(), 1u, 1);
            }
            if (User.GetClient().GetHabbo().Tags != null)
            {
                HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(User.GetClient(), 5u, 1);
            }
            if (User.GetClient().GetHabbo().Motto != null)
            {
                HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(User.GetClient(), 5u, 1);
            }
        }
    }

    private void ApplyEffect()
    {
        Session.GetHabbo().GetAvatarEffectsInventoryComponent().ApplyEffect(Request.PopWiredInt32());
    }

    private void EnableEffect()
    {
        Session.GetHabbo().GetAvatarEffectsInventoryComponent().EnableEffect(Request.PopWiredInt32());
    }

    private void RecycleItems()
    {
        if (!Session.GetHabbo().InRoom)
        {
            return;
        }
        int itemCount = Request.PopWiredInt32();
        if (itemCount != 5)
        {
            return;
        }
        for (int i = 0; i < itemCount; i++)
        {
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.PopWiredUInt());
            if (Item != null && Item.GetBaseItem().AllowRecycle)
            {
                Session.GetHabbo().GetInventoryComponent().RemoveItem(Item.Id);
                continue;
            }
            return;
        }
        uint newItemId = HolographEnvironment.GetGame().GetCatalog().GenerateItemId();
        EcotronReward Reward = HolographEnvironment.GetGame().GetCatalog().GetRandomEcotronReward();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("INSERT INTO user_items (id,user_id,base_item,extra_data) VALUES ('" + newItemId + "','" + Session.GetHabbo().Id + "','1478','" + DateTime.Now.ToLongDateString() + "')");
            dbClient.ExecuteQuery("INSERT INTO user_presents (item_id,base_id,amount,extra_data) VALUES ('" + newItemId + "','" + Reward.BaseId + "','1','')");
        }
        Session.GetHabbo().GetInventoryComponent().UpdateItems(FromDatabase: true);
        GetResponse().Init(508u);
        GetResponse().AppendBoolean(Bool: true);
        GetResponse().AppendUInt(newItemId);
        SendResponse();
    }

    private void RedeemExchangeFurni()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || !Room.CheckRights(Session, RequireOwnership: true))
        {
            return;
        }
        RoomItem Exchange = Room.GetItem(Request.PopWiredUInt());
        if (Exchange != null && (Exchange.GetBaseItem().Name.StartsWith("CF_") || Exchange.GetBaseItem().Name.StartsWith("CFC_")))
        {
            string[] Split = Exchange.GetBaseItem().Name.Split('_');
            int Value = int.Parse(Split[1]);
            if (Value > 0)
            {
                Session.GetHabbo().Credits += Value;
                Session.GetHabbo().UpdateCreditsBalance(InDatabase: true);
            }
            Room.RemoveFurniture(null, Exchange.Id);
            GetResponse().Init(219u);
            SendResponse();
        }
    }

    private void EnterInfobus()
    {
        GetResponse().Init(81u);
        GetResponse().AppendStringWithBreak("The Zero Infobus is not yet in use.");
        SendResponse();
    }

    private void KickBot()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && Room.CheckRights(Session, RequireOwnership: true))
        {
            RoomUser Bot = Room.GetRoomUserByVirtualId(Request.PopWiredInt32());
            if (Bot != null && Bot.IsBot)
            {
                Room.RemoveBot(Bot.VirtualId, Kicked: true);
            }
        }
    }

    private void PlacePet()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || (!Room.AllowPets && !Room.CheckRights(Session, RequireOwnership: true)))
        {
            return;
        }
        uint PetId = Request.PopWiredUInt();
        Pet Pet = Session.GetHabbo().GetInventoryComponent().GetPet(PetId);
        if (Pet == null || Pet.PlacedInRoom)
        {
            return;
        }
        int X = Request.PopWiredInt32();
        int Y = Request.PopWiredInt32();
        if (!Room.CanWalk(X, Y, 0.0, LastStep: true))
        {
            return;
        }
        if (Room.PetCount >= HolographEnvironment.GetGame().GetRoomManager().MAX_PETS_PER_ROOM)
        {
            Session.SendNotif("There are too many pets in this room. A room may only contain up to " + HolographEnvironment.GetGame().GetRoomManager().MAX_PETS_PER_ROOM + " pets.");
            return;
        }
        Pet.PlacedInRoom = true;
        Pet.RoomId = Room.RoomId;
        RoomUser PetUser = Room.DeployBot(new RoomBot(Pet.PetId, Pet.RoomId, "pet", "freeroam", Pet.Name, "", Pet.Look, X, Y, 0, 0, 0, 0, 0, 0), Pet);
        if (Room.CheckRights(Session, RequireOwnership: true))
        {
            Session.GetHabbo().GetInventoryComponent().MovePetToRoom(Pet.PetId, Room.RoomId);
        }
    }

    private void GetPetInfo()
    {
        uint PetId = Request.PopWiredUInt();
        DataRow Row = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.AddParamWithValue("petid", PetId);
            Row = dbClient.ReadDataRow("SELECT SQL_NO_CACHE * FROM user_pets WHERE id = @petid LIMIT 1");
        }
        if (Row != null)
        {
            Session.SendMessage(HolographEnvironment.GetGame().GetCatalog().GeneratePetFromRow(Row)
                .SerializeInfo());
        }
    }

    private void PickUpPet()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room != null && !Room.IsPublic && (Room.AllowPets || Room.CheckRights(Session, RequireOwnership: true)))
        {
            uint PetId = Request.PopWiredUInt();
            RoomUser PetUser = Room.GetPet(PetId);
            if (PetUser != null && PetUser.PetData != null && PetUser.PetData.OwnerId == Session.GetHabbo().Id)
            {
                Session.GetHabbo().GetInventoryComponent().AddPet(PetUser.PetData);
                Room.RemoveBot(PetUser.VirtualId, Kicked: false);
            }
        }
    }

    private void GetTrainerPanel()
    {
        uint PetID = Request.PopWiredUInt();
        Pet PetData = Session.GetHabbo().GetInventoryComponent().GetPet(PetID);
        GetResponse().Init(605u);
        GetResponse().AppendUInt(PetID);
        GetResponse().AppendString("PBHIJKPAQARAPSA");
        SendResponse();
    }

    private void RespectPet()
    {
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
        if (Room == null || Room.IsPublic || (!Room.AllowPets && !Room.CheckRights(Session, RequireOwnership: true)))
        {
            return;
        }
        uint PetId = Request.PopWiredUInt();
        RoomUser PetUser = Room.GetPet(PetId);
        if (PetUser == null || PetUser.PetData == null || PetUser.PetData.OwnerId != Session.GetHabbo().Id)
        {
            return;
        }
        PetUser.PetData.OnRespect();
        Session.GetHabbo().DailyPetRespectPoints--;
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.AddParamWithValue("userid", Session.GetHabbo().Id);
        dbClient.ExecuteQuery("Update users SET daily_pet_respect_points = daily_pet_respect_points - 1 WHERE id = @userid LIMIT 1");
    }

    private void SerializeWired()
    {
        GetResponse().Init(650u);
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM wiredtrigger WHERE roomid = '" + Session.GetHabbo().CurrentRoomId + "'");
        }
        if (Data.Rows.Count == 0)
        {
            GetResponse().AppendStringWithBreak("H");
        }
        else
        {
            GetResponse().AppendInt32(Data.Rows.Count);
            foreach (DataRow Row in Data.Rows)
            {
                GetResponse().AppendInt32(int.Parse(Row["slotid"].ToString()));
                if (Row["triggertype"].ToString() == "say")
                {
                    GetResponse().AppendStringWithBreak("HIH");
                    GetResponse().AppendStringWithBreak(Row["whattrigger"].ToString());
                    using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                    if (dbClient.findsResult("SELECT SQL_NO_CACHE * from wiredaction where slotid = '" + Row["slotid"].ToString() + "'"))
                    {
                        DataRow Action = null;
                        Action = dbClient.ReadDataRow("SELECT SQL_NO_CACHE * from wiredaction where slotid = '" + Row["slotid"].ToString() + "'");
                        if (Action["typeaction"].ToString() == "status")
                        {
                            GetResponse().AppendString("HHHI");
                            GetResponse().AppendInt32(int.Parse(Action["itemid"].ToString()));
                            Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                            RoomItem Item = Room.GetItem(uint.Parse(Action["itemid"].ToString()));
                            GetResponse().AppendString(Item.GetBaseItem().PublicName);
                            GetResponse().AppendStringWithBreak("");
                            GetResponse().AppendStringWithBreak(Action["whataction"].ToString());
                            GetResponse().AppendString("HHK");
                        }
                        else if (Action["typeaction"].ToString() == "kick")
                        {
                            GetResponse().AppendStringWithBreak("HHHIH");
                            GetResponse().AppendStringWithBreak("");
                            GetResponse().AppendString("HHJ");
                        }
                    }
                    else
                    {
                        GetResponse().AppendString("HHHH");
                    }
                }
                else if (Row["triggertype"].ToString() == "walkon")
                {
                    GetResponse().AppendString("HI");
                    GetResponse().AppendInt32(int.Parse(Row["whattrigger"].ToString()));
                    Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                    RoomItem Item = Room.GetItem(uint.Parse(Row["whattrigger"].ToString()));
                    GetResponse().AppendStringWithBreak(Item.GetBaseItem().PublicName);
                    GetResponse().AppendStringWithBreak("");
                    using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                    if (dbClient.findsResult("SELECT SQL_NO_CACHE * from wiredaction where slotid = '" + Row["slotid"].ToString() + "'"))
                    {
                        DataRow Action = null;
                        Action = dbClient.ReadDataRow("SELECT SQL_NO_CACHE * from wiredaction where slotid = '" + Row["slotid"].ToString() + "'");
                        if (Action["typeaction"].ToString() == "status")
                        {
                            GetResponse().AppendString("HHII");
                            GetResponse().AppendInt32(int.Parse(Action["itemid"].ToString()));
                            Item = Room.GetItem(uint.Parse(Action["itemid"].ToString()));
                            GetResponse().AppendString(Item.GetBaseItem().PublicName);
                            GetResponse().AppendStringWithBreak("");
                            GetResponse().AppendStringWithBreak(Action["whataction"].ToString());
                            GetResponse().AppendString("HHK");
                        }
                        else if (Action["typeaction"].ToString() == "kick")
                        {
                            GetResponse().AppendStringWithBreak("HHIIH");
                            GetResponse().AppendStringWithBreak("");
                            GetResponse().AppendString("HHJ");
                        }
                    }
                    else
                    {
                        GetResponse().AppendString("HHIH");
                    }
                }
                else
                {
                    GetResponse().AppendString("HHH");
                }
            }
            GetResponse().AppendStringWithBreak("");
        }
        SendResponse();
    }

    private void InitializeWired()
    {
        SerializeWired();
    }

    private void RequestAddWired()
    {
        GetResponse().Init(650u);
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("INSERT INTO wiredtrigger values ('','" + Session.GetHabbo().CurrentRoomId + "','','')");
        }
        SerializeWired();
    }

    private void RequestAddTrigger()
    {
        uint SlotID = Request.PopWiredUInt();
        int Chose = Request.PopWiredInt32();
        string ExtraData = null;
        string Type = "";
        if (Chose == 0)
        {
            Request.AdvancePointer(3);
            Type = "say";
            ExtraData = Request.PopFixedString();
        }
        if (Chose == 1)
        {
            ExtraData = Request.PopWiredInt32().ToString();
            Request.AdvancePointer(3);
            Type = "walkon";
        }
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("Update wiredtrigger set triggertype = '" + Type + "',whattrigger = '" + ExtraData + "' where slotid = '" + SlotID + "'");
        }
        SerializeWired();
    }

    private void GetFurniStates()
    {
        GetResponse().Init(651u);
        GetResponse().AppendStringWithBreak("JHI");
        SendResponse();
    }

    private void AddTriggerStatus()
    {
        int slotID = Request.PopWiredInt32();
        int Type = Request.PopWiredInt32();
        int ItemID = 0;
        string ItemName = "";
        int status = 0;
        if (Type == 3)
        {
            ItemID = Request.PopWiredInt32();
            ItemName = Request.PopFixedString();
            status = Request.PopFixedInt32();
            using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
            dbClient.ExecuteQuery("INSERT INTO wiredaction values ('" + slotID + "','status','" + status + "','" + ItemID + "')");
        }
        if (Type == 2)
        {
            ItemID = Request.PopWiredInt32();
            ItemName = Request.PopFixedString();
            status = Request.PopFixedInt32();
            using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
            dbClient.ExecuteQuery("INSERT INTO wiredaction values ('" + slotID + "','kick','','')");
        }
        SerializeWired();
    }

    private void DeleteWired()
    {
        int slotID = Request.PopWiredInt32();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("DELETE from wiredtrigger where slotid= '" + slotID + "'");
            dbClient.ExecuteQuery("DELETE from wiredaction where slotid= '" + slotID + "'");
        }
        SerializeWired();
    }

    private void RefreshWired()
    {
        SerializeWired();
    }

    private void DeleteWiredTrigger()
    {
        int slotID = Request.PopWiredInt32();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("DELETE from wiredaction where slotid= '" + slotID + "'");
            dbClient.ExecuteQuery("DELETE from wiredtrigger where slotid= '" + slotID + "'");
        }
        SerializeWired();
    }

    private void DeleteWiredAction()
    {
        int slotID = Request.PopWiredInt32();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("DELETE from wiredaction where slotid= '" + slotID + "'");
        }
        SerializeWired();
    }

    public void RegisterRooms()
    {
        RequestHandlers[391] = OpenFlat;
        RequestHandlers[182] = GetAdvertisement;
        RequestHandlers[388] = GetPub;
        RequestHandlers[2] = OpenPub;
        RequestHandlers[230] = GetGroupBadges;
        RequestHandlers[215] = GetRoomData1;
        RequestHandlers[390] = GetRoomData2;
        RequestHandlers[126] = GetRoomData3;
        RequestHandlers[52] = Talk;
        RequestHandlers[55] = Shout;
        RequestHandlers[56] = Whisper;
        RequestHandlers[75] = Move;
        RequestHandlers[387] = CanCreateRoom;
        RequestHandlers[29] = CreateRoom;
        RequestHandlers[400] = GetRoomEditData;
        RequestHandlers[386] = SaveRoomIcon;
        RequestHandlers[401] = SaveRoomData;
        RequestHandlers[96] = GiveRights;
        RequestHandlers[97] = TakeRights;
        RequestHandlers[155] = TakeAllRights;
        RequestHandlers[95] = KickUser;
        RequestHandlers[320] = BanUser;
        RequestHandlers[71] = InitTrade;
        RequestHandlers[384] = SetHomeRoom;
        RequestHandlers[23] = DeleteRoom;
        RequestHandlers[79] = LookAt;
        RequestHandlers[317] = StartTyping;
        RequestHandlers[318] = StopTyping;
        RequestHandlers[319] = IgnoreUser;
        RequestHandlers[322] = UnignoreUser;
        RequestHandlers[345] = CanCreateRoomEvent;
        RequestHandlers[346] = StartEvent;
        RequestHandlers[347] = StopEvent;
        RequestHandlers[348] = EditEvent;
        RequestHandlers[94] = Wave;
        RequestHandlers[263] = GetUserTags;
        RequestHandlers[159] = GetUserBadges;
        RequestHandlers[261] = RateRoom;
        RequestHandlers[93] = Dance;
        RequestHandlers[98] = AnswerDoorbell;
        RequestHandlers[59] = ReqLoadRoomForUser;
        RequestHandlers[66] = ApplyRoomEffect;
        RequestHandlers[90] = PlaceItem;
        RequestHandlers[67] = TakeItem;
        RequestHandlers[73] = MoveItem;
        RequestHandlers[392] = TriggerItem;
        RequestHandlers[393] = TriggerItem;
        RequestHandlers[83] = OpenPostit;
        RequestHandlers[84] = SavePostit;
        RequestHandlers[85] = DeletePostit;
        RequestHandlers[78] = OpenPresent;
        RequestHandlers[341] = GetMoodlight;
        RequestHandlers[342] = UpdateMoodlight;
        RequestHandlers[343] = SwitchMoodlightStatus;
        RequestHandlers[72] = OfferTradeItem;
        RequestHandlers[405] = TakeBackTradeItem;
        RequestHandlers[70] = StopTrade;
        RequestHandlers[403] = StopTrade;
        RequestHandlers[69] = AcceptTrade;
        RequestHandlers[68] = UnacceptTrade;
        RequestHandlers[402] = CompleteTrade;
        RequestHandlers[371] = GiveRespect;
        RequestHandlers[372] = ApplyEffect;
        RequestHandlers[373] = EnableEffect;
        RequestHandlers[232] = TriggerItem;
        RequestHandlers[314] = TriggerItem;
        RequestHandlers[247] = TriggerItem;
        RequestHandlers[76] = TriggerItem;
        RequestHandlers[77] = TriggerItemDiceSpecial;
        RequestHandlers[414] = RecycleItems;
        RequestHandlers[183] = RedeemExchangeFurni;
        RequestHandlers[113] = EnterInfobus;
        RequestHandlers[441] = KickBot;
        RequestHandlers[3002] = PlacePet;
        RequestHandlers[3001] = GetPetInfo;
        RequestHandlers[3003] = PickUpPet;
        RequestHandlers[3004] = GetTrainerPanel;
        RequestHandlers[3005] = RespectPet;
        RequestHandlers[3056] = InitializeWired;
        RequestHandlers[3050] = RequestAddWired;
        RequestHandlers[3051] = RequestAddTrigger;
        RequestHandlers[3058] = GetFurniStates;
        RequestHandlers[3052] = AddTriggerStatus;
        RequestHandlers[3057] = RefreshWired;
        RequestHandlers[3053] = DeleteWired;
        RequestHandlers[3054] = DeleteWiredTrigger;
        RequestHandlers[3055] = DeleteWiredAction;
    }

    private void GetUserInfo()
    {
        GetResponse().Init(5u);
        GetResponse().AppendStringWithBreak(Session.GetHabbo().Id.ToString());
        GetResponse().AppendStringWithBreak(Session.GetHabbo().Username);
        GetResponse().AppendStringWithBreak(Session.GetHabbo().Look);
        GetResponse().AppendStringWithBreak(Session.GetHabbo().Gender.ToUpper());
        GetResponse().AppendStringWithBreak(Session.GetHabbo().Motto);
        GetResponse().AppendStringWithBreak(Session.GetHabbo().RealName);
        GetResponse().AppendInt32(0);
        GetResponse().AppendStringWithBreak("");
        GetResponse().AppendInt32(0);
        GetResponse().AppendInt32(0);
        GetResponse().AppendInt32(Session.GetHabbo().Respect);
        GetResponse().AppendInt32(Session.GetHabbo().DailyRespectPoints);
        GetResponse().AppendInt32(Session.GetHabbo().DailyPetRespectPoints);
        SendResponse();
    }

    private void GetBalance()
    {
        Session.GetHabbo().UpdateCreditsBalance(InDatabase: false);
        Session.GetHabbo().UpdateActivityPointsBalance(InDatabase: false);
    }

    private void GetSubscriptionData()
    {
        string SubscriptionId = Request.PopFixedString();
        GetResponse().Init(7u);
        GetResponse().AppendStringWithBreak(SubscriptionId.ToLower());
        if (Session.GetHabbo().GetSubscriptionManager().HasSubscription(SubscriptionId))
        {
            double Expire = Session.GetHabbo().GetSubscriptionManager().GetSubscription(SubscriptionId)
                .ExpireTime;
            double TimeLeft = Expire - HolographEnvironment.GetUnixTimestamp();
            int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400.0);
            int MonthsLeft = TotalDaysLeft / 31;
            if (MonthsLeft >= 1)
            {
                MonthsLeft--;
            }
            GetResponse().AppendInt32(TotalDaysLeft - MonthsLeft * 31);
            GetResponse().AppendBoolean(Bool: true);
            GetResponse().AppendInt32(MonthsLeft);
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                GetResponse().AppendInt32(0);
            }
        }
        SendResponse();
    }

    private void GetBadges()
    {
        Session.SendMessage(Session.GetHabbo().GetBadgeComponent().Serialize());
    }

    private void UpdateBadges()
    {
        Session.GetHabbo().GetBadgeComponent().ResetSlots();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("Update user_badges SET badge_slot = '0' WHERE user_id = '" + Session.GetHabbo().Id + "'");
        }
        while (Request.RemainingLength > 0)
        {
            int Slot = Request.PopWiredInt32();
            string Badge = Request.PopFixedString();
            if (Badge.Length != 0)
            {
                if (!Session.GetHabbo().GetBadgeComponent().HasBadge(Badge) || Slot < 1 || Slot > 5)
                {
                    return;
                }
                Session.GetHabbo().GetBadgeComponent().GetBadge(Badge)
                    .Slot = Slot;
                using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                dbClient.AddParamWithValue("slotid", Slot);
                dbClient.AddParamWithValue("badge", Badge);
                dbClient.AddParamWithValue("userid", Session.GetHabbo().Id);
                dbClient.ExecuteQuery("Update user_badges SET badge_slot = @slotid WHERE badge_id = @badge AND user_id = @userid LIMIT 1");
            }
        }
        ServerMessage Message = new ServerMessage(228u);
        Message.AppendUInt(Session.GetHabbo().Id);
        Message.AppendInt32(Session.GetHabbo().GetBadgeComponent().EquippedCount);
        foreach (Badge Badge2 in Session.GetHabbo().GetBadgeComponent().BadgeList)
        {
            if (Badge2.Slot > 0)
            {
                Message.AppendInt32(Badge2.Slot);
                Message.AppendStringWithBreak(Badge2.Code);
            }
        }
        if (Session.GetHabbo().InRoom && HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId) != null)
        {
            HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId)
                .SendMessage(Message);
        }
        else
        {
            Session.SendMessage(Message);
        }
    }

    private void GetAchievements()
    {
        Session.SendMessage(HolographEnvironment.GetGame().GetAchievementManager().SerializeAchievementList(Session));
    }

    private void ChangeLook()
    {
        if (Session.GetHabbo().MutantPenalty)
        {
            Session.SendNotif("Because of a penalty or restriction on your account, you are not allowed to change your look.");
            return;
        }
        string Gender = Request.PopFixedString().ToUpper();
        string Look = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        if (!AntiMutant.ValidateLook(Look, Gender))
        {
            return;
        }
        Session.GetHabbo().Look = Look;
        Session.GetHabbo().Gender = Gender.ToLower();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.AddParamWithValue("look", Look);
            dbClient.AddParamWithValue("gender", Gender);
            dbClient.ExecuteQuery("Update users SET look = @look, gender = @gender WHERE id = '" + Session.GetHabbo().Id + "' LIMIT 1");
        }
        Session.GetMessageHandler().GetResponse().Init(266u);
        Session.GetMessageHandler().GetResponse().AppendInt32(-1);
        Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Look);
        Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
        Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Motto);
        Session.GetMessageHandler().SendResponse();
        if (!Session.GetHabbo().InRoom)
        {
            return;
        }
        Room Room = Session.GetHabbo().CurrentRoom;
        if (Room != null)
        {
            RoomUser User = Room.GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User != null)
            {
                ServerMessage RoomUpdate = new ServerMessage(266u);
                RoomUpdate.AppendInt32(User.VirtualId);
                RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Look);
                RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
                RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Motto);
                Room.SendMessage(RoomUpdate);
            }
        }
    }

    private void GetWardrobe()
    {
        GetResponse().Init(267u);
        GetResponse().AppendBoolean(Session.GetHabbo().HasFuse("fuse_use_wardrobe"));
        if (Session.GetHabbo().HasFuse("fuse_use_wardrobe"))
        {
            DataTable WardrobeData = null;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.AddParamWithValue("userid", Session.GetHabbo().Id);
                WardrobeData = dbClient.ReadDataTable("SELECT * FROM user_wardrobe WHERE user_id = @userid");
            }
            if (WardrobeData == null)
            {
                GetResponse().AppendInt32(0);
            }
            else
            {
                GetResponse().AppendInt32(WardrobeData.Rows.Count);
                foreach (DataRow Row in WardrobeData.Rows)
                {
                    GetResponse().AppendUInt((uint)Row["slot_id"]);
                    GetResponse().AppendStringWithBreak((string)Row["look"]);
                    GetResponse().AppendStringWithBreak((string)Row["gender"]);
                }
            }
        }
        SendResponse();
    }

    private void SaveWardrobe()
    {
        uint SlotId = Request.PopWiredUInt();
        string Look = Request.PopFixedString();
        string Gender = Request.PopFixedString();
        if (!AntiMutant.ValidateLook(Look, Gender))
        {
            return;
        }
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.AddParamWithValue("userid", Session.GetHabbo().Id);
        dbClient.AddParamWithValue("slotid", SlotId);
        dbClient.AddParamWithValue("look", Look);
        dbClient.AddParamWithValue("gender", Gender.ToUpper());
        if (dbClient.ReadDataRow("SELECT null FROM user_wardrobe WHERE user_id = @userid AND slot_id = @slotid LIMIT 1") != null)
        {
            dbClient.ExecuteQuery("Update user_wardrobe SET look = @look, gender = @gender WHERE user_id = @userid AND slot_id = @slotid LIMIT 1");
        }
        else
        {
            dbClient.ExecuteQuery("INSERT INTO user_wardrobe (user_id,slot_id,look,gender) VALUES (@userid,@slotid,@look,@gender)");
        }
    }

    private void GetPetsInventory()
    {
        if (Session.GetHabbo().GetInventoryComponent() != null)
        {
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }
    }

    public void RegisterUsers()
    {
        RequestHandlers[7] = GetUserInfo;
        RequestHandlers[8] = GetBalance;
        RequestHandlers[26] = GetSubscriptionData;
        RequestHandlers[157] = GetBadges;
        RequestHandlers[158] = UpdateBadges;
        RequestHandlers[370] = GetAchievements;
        RequestHandlers[44] = ChangeLook;
        RequestHandlers[375] = GetWardrobe;
        RequestHandlers[376] = SaveWardrobe;
        RequestHandlers[404] = GetInventory;
        RequestHandlers[3000] = GetPetsInventory;
    }

    private void InitMessenger()
    {
        Session.GetHabbo().InitMessenger();
    }

    private void FriendsListUpdate()
    {
        if (Session.GetHabbo().GetMessenger() != null)
        {
            Session.SendMessage(Session.GetHabbo().GetMessenger().SerializeUpdates());
        }
    }

    private void RemoveBuddy()
    {
        if (Session.GetHabbo().GetMessenger() != null)
        {
            int Requests = Request.PopWiredInt32();
            for (int i = 0; i < Requests; i++)
            {
                Session.GetHabbo().GetMessenger().DestroyFriendship(Request.PopWiredUInt());
            }
        }
    }

    private void SearchHabbo()
    {
        if (Session.GetHabbo().GetMessenger() != null)
        {
            Session.SendMessage(Session.GetHabbo().GetMessenger().PerformSearch(Request.PopFixedString()));
        }
    }

    private void AcceptRequest()
    {
        if (Session.GetHabbo().GetMessenger() == null)
        {
            return;
        }
        int Amount = Request.PopWiredInt32();
        for (int i = 0; i < Amount; i++)
        {
            uint RequestId = Request.PopWiredUInt();
            MessengerRequest MessRequest = Session.GetHabbo().GetMessenger().GetRequest(RequestId);
            if (MessRequest != null)
            {
                if (MessRequest.To != Session.GetHabbo().Id)
                {
                    break;
                }
                if (!Session.GetHabbo().GetMessenger().FriendshipExists(MessRequest.To, MessRequest.From))
                {
                    Session.GetHabbo().GetMessenger().CreateFriendship(MessRequest.From);
                }
                Session.GetHabbo().GetMessenger().HandleRequest(RequestId);
            }
        }
    }

    private void DeclineRequest()
    {
        if (Session.GetHabbo().GetMessenger() != null)
        {
            int Mode = Request.PopWiredInt32();
            int Amount = Request.PopWiredInt32();
            if (Mode == 0 && Amount == 1)
            {
                uint RequestId = Request.PopWiredUInt();
                Session.GetHabbo().GetMessenger().HandleRequest(RequestId);
            }
            else if (Mode == 1)
            {
                Session.GetHabbo().GetMessenger().HandleAllRequests();
            }
        }
    }

    private void RequestBuddy()
    {
        if (Session.GetHabbo().GetMessenger() != null)
        {
            Session.GetHabbo().GetMessenger().RequestBuddy(Request.PopFixedString());
        }
    }

    private void SendInstantMessenger()
    {
        uint userId = Request.PopWiredUInt();
        string message = HolographEnvironment.FilterInjectionChars(Request.PopFixedString());
        if (Session.GetHabbo().GetMessenger() != null)
        {
            Session.GetHabbo().GetMessenger().SendInstantMessage(userId, message);
        }
    }

    private void FollowBuddy()
    {
        uint BuddyId = Request.PopWiredUInt();
        GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(BuddyId);
        if (Client == null || Client.GetHabbo() == null || !Client.GetHabbo().InRoom)
        {
            return;
        }
        Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Client.GetHabbo().CurrentRoomId);
        if (Room != null)
        {
            GetResponse().Init(286u);
            GetResponse().AppendBoolean(Room.IsPublic);
            GetResponse().AppendUInt(Client.GetHabbo().CurrentRoomId);
            SendResponse();
            if (!Room.IsPublic)
            {
                PrepareRoomForUser(Room.RoomId, "");
            }
        }
    }

    private void SendInstantInvite()
    {
        int count = Request.PopWiredInt32();
        List<uint> UserIds = new List<uint>();
        for (int i = 0; i < count; i++)
        {
            UserIds.Add(Request.PopWiredUInt());
        }
        string message = HolographEnvironment.FilterInjectionChars(Request.PopFixedString(), AllowLinebreaks: true);
        ServerMessage Message = new ServerMessage(135u);
        Message.AppendUInt(Session.GetHabbo().Id);
        Message.AppendStringWithBreak(message);
        foreach (uint Id in UserIds)
        {
            if (Session.GetHabbo().GetMessenger().FriendshipExists(Session.GetHabbo().Id, Id))
            {
                GameClient Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(Id);
                if (Client == null)
                {
                    break;
                }
                Client.SendMessage(Message);
            }
        }
    }

    public void RegisterMessenger()
    {
        RequestHandlers[12] = InitMessenger;
        RequestHandlers[15] = FriendsListUpdate;
        RequestHandlers[40] = RemoveBuddy;
        RequestHandlers[41] = SearchHabbo;
        RequestHandlers[33] = SendInstantMessenger;
        RequestHandlers[37] = AcceptRequest;
        RequestHandlers[38] = DeclineRequest;
        RequestHandlers[39] = RequestBuddy;
        RequestHandlers[262] = FollowBuddy;
        RequestHandlers[34] = SendInstantInvite;
    }

    private void Pong()
    {
        Session.PongOK = true;
    }

    public void RegisterGlobal()
    {
        RequestHandlers[196] = Pong;
    }
}
