using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using Zero.Core;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Pathfinding;
using Zero.Hotel.Pets;
using Zero.Hotel.RoomBots;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Hotel.Rooms;

internal class Room
{
    private uint Id;

    public string Name;

    public string Description;

    public string Type;

    public string Owner;

    public string Password;

    public int Category;

    public int State;

    public int UsersNow;

    public int UsersMax;

    public string ModelName;

    public string CCTs;

    public int Score;

    public List<string> Tags;

    public bool AllowPets;

    public bool AllowPetsEating;

    public bool AllowWalkthrough;

    public bool Hidewall;

    private StringBuilder mRollerBroadcast = new StringBuilder();

    public List<RoomUser> UserList;

    public int UserCounter = 0;

    private int IdleTime;

    public RoomIcon myIcon;

    public List<uint> UsersWithRights;

    private Dictionary<uint, double> Bans;

    public List<uint> HasWaterEffect;

    public Dictionary<uint, Thread> HasThread;

    public Dictionary<uint, Thread> BallThread;

    public RoomEvent Event;

    public string Wallpaper;

    public string Floor;

    public string Landscape;

    public List<RoomItem> Items;

    public MoodlightData MoodlightData;

    public List<Trade> ActiveTrades;

    public bool KeepAlive;

    public List<uint> HasBlueBattleBallEffect;

    public List<uint> HasYellowBattleBallEffect;

    public List<uint> HasGreenBattleBallEffect;

    public List<uint> HasRedBattleBallEffect;

    public MatrixState[,] Matrix;

    public bool[,] UserMatrix;

    public Coord[,] BedMatrix;

    public double[,] HeightMatrix;

    public double[,] TopStackHeight;

    public bool HasOngoingEvent
    {
        get
        {
            if (Event != null)
            {
                return true;
            }
            return false;
        }
    }

    public RoomIcon Icon
    {
        get
        {
            return myIcon;
        }
        set
        {
            myIcon = value;
        }
    }

    public int UserCount
    {
        get
        {
            int i = 0;
            foreach (RoomUser User in UserList)
            {
                if (!User.IsBot)
                {
                    i++;
                }
            }
            return i;
        }
    }

    public int TagCount => Tags.Count;

    public RoomModel Model => HolographEnvironment.GetGame().GetRoomManager().GetModel(ModelName);

    public uint RoomId => Id;

    public List<RoomItem> FloorItems
    {
        get
        {
            List<RoomItem> FloorItems = new List<RoomItem>();
            foreach (RoomItem Item in Items)
            {
                if (Item.IsFloorItem)
                {
                    FloorItems.Add(Item);
                }
            }
            return FloorItems;
        }
    }

    public List<RoomItem> WallItems
    {
        get
        {
            List<RoomItem> WallItems = new List<RoomItem>();
            foreach (RoomItem Item in Items)
            {
                if (Item.IsWallItem)
                {
                    WallItems.Add(Item);
                }
            }
            return WallItems;
        }
    }

    public bool CanTradeInRoom
    {
        get
        {
            if (IsPublic)
            {
                return false;
            }
            return true;
        }
    }

    public bool IsPublic
    {
        get
        {
            if (Type == "public")
            {
                return true;
            }
            return false;
        }
    }

    public int PetCount
    {
        get
        {
            int c = 0;
            List<RoomUser>.Enumerator Users = UserList.GetEnumerator();
            while (Users.MoveNext())
            {
                if (Users.Current.IsPet)
                {
                    c++;
                }
            }
            return c;
        }
    }

    public Room(uint Id, string Name, string Description, string Type, string Owner, int Category, int State, int UsersMax, string ModelName, string CCTs, int Score, List<string> Tags, bool AllowPets, bool AllowPetsEating, bool AllowWalkthrough, bool Hidewall, RoomIcon Icon, string Password, string Wallpaper, string Floor, string Landscape)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.Owner = Owner;
        this.Category = Category;
        this.Type = Type;
        this.State = State;
        UsersNow = 0;
        this.UsersMax = UsersMax;
        this.ModelName = ModelName;
        this.CCTs = CCTs;
        this.Score = Score;
        this.Tags = Tags;
        this.AllowPets = AllowPets;
        this.AllowPetsEating = AllowPetsEating;
        this.AllowWalkthrough = AllowWalkthrough;
        this.Hidewall = Hidewall;
        UserCounter = 0;
        UserList = new List<RoomUser>();
        myIcon = Icon;
        this.Password = Password;
        Bans = new Dictionary<uint, double>();
        Event = null;
        this.Wallpaper = Wallpaper;
        this.Floor = Floor;
        this.Landscape = Landscape;
        Items = new List<RoomItem>();
        ActiveTrades = new List<Trade>();
        UserMatrix = new bool[Model.MapSizeX, Model.MapSizeY];
        HasBlueBattleBallEffect = new List<uint>();
        HasYellowBattleBallEffect = new List<uint>();
        HasGreenBattleBallEffect = new List<uint>();
        HasRedBattleBallEffect = new List<uint>();
        HasWaterEffect = new List<uint>();
        HasThread = new Dictionary<uint, Thread>();
        BallThread = new Dictionary<uint, Thread>();
        IdleTime = 0;
        KeepAlive = true;
        LoadRights();
        LoadFurniture();
        GenerateMaps();
    }

    public void InitBots()
    {
        List<RoomBot> Bots = HolographEnvironment.GetGame().GetBotManager().GetBotsForRoom(RoomId);
        foreach (RoomBot Bot in Bots)
        {
            DeployBot(Bot);
        }
    }

    public void InitPets()
    {
        List<Pet> Pets = new List<Pet>();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.AddParamWithValue("roomid", RoomId);
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM user_pets WHERE room_id = @roomid");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            Pet Pet = HolographEnvironment.GetGame().GetCatalog().GeneratePetFromRow(Row);
            DeployBot(new RoomBot(Pet.PetId, RoomId, "pet", "freeroam", Pet.Name, "", Pet.Look, Pet.X, Pet.Y, (int)Pet.Z, 0, 0, 0, 0, 0), Pet);
        }
    }

    public RoomUser DeployBot(RoomBot Bot)
    {
        return DeployBot(Bot, null);
    }

    public RoomUser DeployBot(RoomBot Bot, Pet PetData)
    {
        RoomUser BotUser = new RoomUser(0u, RoomId, UserCounter++);
        if (Bot.X > 0 && Bot.Y > 0 && Bot.X < Model.MapSizeX && Bot.Y < Model.MapSizeY)
        {
            BotUser.SetPos(Bot.X, Bot.Y, Bot.Z);
            BotUser.SetRot(Bot.Rot);
        }
        else
        {
            Bot.X = Model.DoorX;
            Bot.Y = Model.DoorY;
            BotUser.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
            BotUser.SetRot(Model.DoorOrientation);
        }
        UserMatrix[Bot.X, Bot.Y] = true;
        BotUser.BotData = Bot;
        BotUser.BotAI = Bot.GenerateBotAI(BotUser.VirtualId);
        if (BotUser.IsPet)
        {
            BotUser.BotAI.Init((int)Bot.BotId, BotUser.VirtualId, RoomId);
            BotUser.PetData = PetData;
            BotUser.PetData.VirtualId = BotUser.VirtualId;
        }
        else
        {
            BotUser.BotAI.Init(-1, BotUser.VirtualId, RoomId);
        }
        UserList.Add(BotUser);
        UpdateUserStatus(BotUser);
        BotUser.UpdateNeeded = true;
        ServerMessage EnterMessage = new ServerMessage(28u);
        EnterMessage.AppendInt32(1);
        BotUser.Serialize(EnterMessage);
        SendMessage(EnterMessage);
        BotUser.BotAI.OnSelfEnterRoom();
        return BotUser;
    }

    public void RemoveBot(int VirtualId, bool Kicked)
    {
        RoomUser User = GetRoomUserByVirtualId(VirtualId);
        if (User != null && User.IsBot)
        {
            User.BotAI.OnSelfLeaveRoom(Kicked);
            ServerMessage LeaveMessage = new ServerMessage(29u);
            LeaveMessage.AppendRawInt32(User.VirtualId);
            SendMessage(LeaveMessage);
            UserMatrix[User.X, User.Y] = false;
            UserList.Remove(User);
        }
    }

    public void OnUserSay(RoomUser User, string Message, bool Shout)
    {
        foreach (RoomUser Usr in UserList)
        {
            if (Usr.IsBot)
            {
                if (Shout)
                {
                    Usr.BotAI.OnUserShout(User, Message);
                }
                else
                {
                    Usr.BotAI.OnUserSay(User, Message);
                }
            }
        }
    }

    public void RegenerateUserMatrix()
    {
        UserMatrix = new bool[Model.MapSizeX, Model.MapSizeY];
        List<RoomUser>.Enumerator eUsers = UserList.GetEnumerator();
        while (eUsers.MoveNext())
        {
            RoomUser User = eUsers.Current;
            UserMatrix[User.X, User.Y] = true;
        }
    }

    public void GenerateMaps()
    {
        Matrix = new MatrixState[Model.MapSizeX, Model.MapSizeY];
        BedMatrix = new Coord[Model.MapSizeX, Model.MapSizeY];
        HeightMatrix = new double[Model.MapSizeX, Model.MapSizeY];
        TopStackHeight = new double[Model.MapSizeX, Model.MapSizeY];
        for (int line = 0; line < Model.MapSizeY; line++)
        {
            for (int chr = 0; chr < Model.MapSizeX; chr++)
            {
                Matrix[chr, line] = MatrixState.BLOCKED;
                ref Coord reference = ref BedMatrix[chr, line];
                reference = new Coord(chr, line);
                HeightMatrix[chr, line] = 0.0;
                TopStackHeight[chr, line] = 0.0;
                if (chr == Model.DoorX && line == Model.DoorY)
                {
                    Matrix[chr, line] = MatrixState.WALKABLE_LASTSTEP;
                }
                else if (Model.SqState[chr, line] == SquareState.OPEN)
                {
                    Matrix[chr, line] = MatrixState.WALKABLE;
                }
                else if (Model.SqState[chr, line] == SquareState.SEAT)
                {
                    Matrix[chr, line] = MatrixState.WALKABLE_LASTSTEP;
                }
            }
        }
        foreach (RoomItem Item in Items)
        {
            if (Item.GetBaseItem().Type.ToLower() != "s" || Item.GetBaseItem().Height <= 0.0)
            {
                continue;
            }
            if (TopStackHeight[Item.X, Item.Y] <= Item.Z)
            {
                TopStackHeight[Item.X, Item.Y] = Item.Z;
                if (Item.GetBaseItem().Walkable)
                {
                    Matrix[Item.X, Item.Y] = MatrixState.WALKABLE;
                    HeightMatrix[Item.X, Item.Y] = Item.GetBaseItem().Height;
                }
                else if (Item.Z <= Model.SqFloorHeight[Item.X, Item.Y] + 0.1 && Item.GetBaseItem().InteractionType.ToLower() == "gate" && Item.ExtraData == "1")
                {
                    Matrix[Item.X, Item.Y] = MatrixState.WALKABLE;
                }
                else if (Item.GetBaseItem().IsSeat || Item.GetBaseItem().InteractionType.ToLower() == "bed")
                {
                    Matrix[Item.X, Item.Y] = MatrixState.WALKABLE_LASTSTEP;
                }
                else
                {
                    Matrix[Item.X, Item.Y] = MatrixState.BLOCKED;
                }
            }
            Dictionary<int, AffectedTile> Points = GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, Item.X, Item.Y, Item.Rot);
            if (Points == null)
            {
                Points = new Dictionary<int, AffectedTile>();
            }
            foreach (AffectedTile Tile in Points.Values)
            {
                if (TopStackHeight[Tile.X, Tile.Y] <= Item.Z)
                {
                    TopStackHeight[Tile.X, Tile.Y] = Item.Z;
                    if (Item.GetBaseItem().Walkable)
                    {
                        Matrix[Tile.X, Tile.Y] = MatrixState.WALKABLE;
                        HeightMatrix[Tile.X, Tile.Y] = Item.GetBaseItem().Height;
                    }
                    else if (Item.Z <= Model.SqFloorHeight[Item.X, Item.Y] + 0.1 && Item.GetBaseItem().InteractionType.ToLower() == "gate" && Item.ExtraData == "1")
                    {
                        Matrix[Tile.X, Tile.Y] = MatrixState.WALKABLE;
                    }
                    else if (Item.GetBaseItem().IsSeat || Item.GetBaseItem().InteractionType.ToLower() == "bed")
                    {
                        Matrix[Tile.X, Tile.Y] = MatrixState.WALKABLE_LASTSTEP;
                    }
                    else
                    {
                        Matrix[Tile.X, Tile.Y] = MatrixState.BLOCKED;
                    }
                }
                if (Item.GetBaseItem().InteractionType.ToLower() == "bed")
                {
                    if (Item.Rot == 0 || Item.Rot == 4)
                    {
                        BedMatrix[Tile.X, Tile.Y].y = Item.Y;
                    }
                    if (Item.Rot == 2 || Item.Rot == 6)
                    {
                        BedMatrix[Tile.X, Tile.Y].x = Item.X;
                    }
                }
            }
        }
    }

    public void LoadRights()
    {
        UsersWithRights = new List<uint>();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE user_id FROM room_rights WHERE room_id = '" + Id + "'");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            UsersWithRights.Add((uint)Row["user_id"]);
        }
    }

    public void LoadFurniture()
    {
        Items.Clear();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM room_items WHERE room_id = '" + Id + "'");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            RoomItem Item = new RoomItem((uint)Row["id"], RoomId, (uint)Row["base_item"], (string)Row["extra_data"], (int)Row["x"], (int)Row["y"], (double)Row["z"], (int)Row["rot"], (string)Row["wall_pos"]);
            string text = Item.GetBaseItem().InteractionType.ToLower();
            if (text != null && text == "dimmer" && MoodlightData == null)
            {
                MoodlightData = new MoodlightData(Item.Id);
            }
            Items.Add(Item);
        }
    }

    public bool CheckRights(GameClient Session)
    {
        return CheckRights(Session, RequireOwnership: false);
    }

    public bool CheckRights(GameClient Session, bool RequireOwnership)
    {
        if (Session.GetHabbo().Username.ToLower() == Owner.ToLower())
        {
            return true;
        }
        if (Session.GetHabbo().HasFuse("fuse_admin") || Session.GetHabbo().HasFuse("fuse_any_room_controller"))
        {
            return true;
        }
        if (!RequireOwnership)
        {
            if (Session.GetHabbo().HasFuse("fuse_any_room_rights"))
            {
                return true;
            }
            if (UsersWithRights.Contains(Session.GetHabbo().Id))
            {
                return true;
            }
        }
        return false;
    }

    public RoomItem GetItem(uint Id)
    {
        foreach (RoomItem Item in Items)
        {
            if (Item.Id == Id)
            {
                return Item;
            }
        }
        return null;
    }

    public void RemoveFurniture(GameClient Session, uint Id)
    {
        RoomItem Item = GetItem(Id);
        if (Item != null)
        {
            Item.Interactor.OnRemove(Session, Item);
            if (Item.IsWallItem)
            {
                ServerMessage Message = new ServerMessage(84u);
                Message.AppendRawUInt(Item.Id);
                Message.AppendStringWithBreak("");
                Message.AppendBoolean(Bool: false);
                SendMessage(Message);
            }
            else if (Item.IsFloorItem)
            {
                ServerMessage Message = new ServerMessage(94u);
                Message.AppendRawUInt(Item.Id);
                Message.AppendStringWithBreak("");
                Message.AppendBoolean(Bool: false);
                SendMessage(Message);
            }
            Items.Remove(Item);
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("DELETE FROM room_items WHERE id = '" + Id + "' AND room_id = '" + RoomId + "' LIMIT 1");
            }
            GenerateMaps();
            UpdateUserStatusses();
        }
    }

    public bool CanWalk(int X, int Y, double Z, bool LastStep)
    {
        if (X < 0 || X >= Model.MapSizeX || Y >= Model.MapSizeY || Y < 0)
        {
            return false;
        }
        if (SquareHasUsers(X, Y, LastStep))
        {
            return false;
        }
        if (Matrix[X, Y] == MatrixState.BLOCKED)
        {
            return false;
        }
        if (Matrix[X, Y] == MatrixState.WALKABLE_LASTSTEP && !LastStep)
        {
            return false;
        }
        return true;
    }

    public void ProcessRoom()
    {
        int i = 0;
        List<RoomItem>.Enumerator eItems = Items.GetEnumerator();
        while (eItems.MoveNext())
        {
            try
            {
                RoomItem Item = eItems.Current;
                if (Item.UpdateNeeded)
                {
                    Item.ProcessUpdates();
                }
            }
            catch (NullReferenceException ex)
            {
                HolographEnvironment.GetLogging().WriteLine("NullReferenceException at ProcessRoom: " + ex.ToString(), LogLevel.Error);
            }
        }
        List<uint> ToRemove = new List<uint>();
        List<RoomUser>.Enumerator eUsers = UserList.GetEnumerator();
        while (eUsers.MoveNext())
        {
            RoomUser User = eUsers.Current;
            User.IdleTime++;
            if (!User.IsAsleep && User.IdleTime >= 600)
            {
                User.IsAsleep = true;
                ServerMessage FallAsleep = new ServerMessage(486u);
                FallAsleep.AppendInt32(User.VirtualId);
                FallAsleep.AppendBoolean(Bool: true);
                SendMessage(FallAsleep);
            }
            if (User.NeedsAutokick && !ToRemove.Contains(User.HabboId))
            {
                ToRemove.Add(User.HabboId);
            }
            if (User.CarryItemID > 0)
            {
                User.CarryTimer--;
                if (User.CarryTimer <= 0)
                {
                    User.CarryItem(0);
                }
            }
            bool invalidSetStep = false;
            if (User.SetStep)
            {
                if (CanWalk(User.SetX, User.SetY, 0.0, LastStep: true) || User.AllowOverride || AllowWalkthrough)
                {
                    UserMatrix[User.X, User.Y] = false;
                    User.X = User.SetX;
                    User.Y = User.SetY;
                    User.Z = User.SetZ;
                    UserMatrix[User.X, User.Y] = true;
                    UpdateUserStatus(User);
                }
                else
                {
                    invalidSetStep = true;
                }
                User.SetStep = false;
            }
            if (User.PathRecalcNeeded)
            {
                Pathfinder Pathfinder = new Pathfinder(this, User);
                User.GoalX = User.PathRecalcX;
                User.GoalY = User.PathRecalcY;
                User.Path.Clear();
                User.Path = Pathfinder.FindPath();
                if (User.Path.Count > 1)
                {
                    User.PathStep = 1;
                    User.IsWalking = true;
                    User.PathRecalcNeeded = false;
                }
                else
                {
                    User.PathRecalcNeeded = false;
                    User.Path.Clear();
                }
            }
            if (User.IsWalking)
            {
                if (invalidSetStep || User.PathStep >= User.Path.Count || (User.GoalX == User.X && User.Y == User.GoalY))
                {
                    User.Path.Clear();
                    User.IsWalking = false;
                    User.RemoveStatus("mv");
                    User.PathRecalcNeeded = false;
                    if (User.X == Model.DoorX && User.Y == Model.DoorY && !ToRemove.Contains(User.HabboId) && !User.IsBot)
                    {
                        ToRemove.Add(User.HabboId);
                    }
                    UpdateUserStatus(User);
                }
                else
                {
                    int k = User.Path.Count - User.PathStep - 1;
                    Coord NextStep = User.Path[k];
                    User.PathStep++;
                    int nextX = NextStep.x;
                    int nextY = NextStep.y;
                    User.RemoveStatus("mv");
                    bool LastStep = false;
                    if (nextX == User.GoalX && nextY == User.GoalY)
                    {
                        LastStep = true;
                    }
                    if (CanWalk(nextX, nextY, 0.0, LastStep) || User.AllowOverride)
                    {
                        double nextZ = SqAbsoluteHeight(nextX, nextY);
                        User.Statusses.Remove("lay");
                        User.Statusses.Remove("sit");
                        User.AddStatus("mv", nextX + "," + nextY + "," + nextZ.ToString().Replace(',', '.'));
                        User.RotHead = (User.RotBody = Rotation.Calculate(User.X, User.Y, nextX, nextY));
                        User.SetStep = true;
                        User.SetX = BedMatrix[nextX, nextY].x;
                        User.SetY = BedMatrix[nextX, nextY].y;
                        User.SetZ = nextZ;
                    }
                    else
                    {
                        User.IsWalking = false;
                    }
                }
                User.UpdateNeeded = true;
            }
            else if (User.Statusses.ContainsKey("mv"))
            {
                User.RemoveStatus("mv");
                User.UpdateNeeded = true;
            }
            if (User.IsBot)
            {
                User.BotAI.OnTimerTick();
            }
            else
            {
                i++;
            }
        }
        foreach (uint toRemove in ToRemove)
        {
            RemoveUserFromRoom(HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(toRemove), NotifyClient: true, NotifyKick: false);
        }
        if (i >= 1)
        {
            IdleTime = 0;
        }
        else
        {
            IdleTime++;
        }
        if (IdleTime >= 60)
        {
            HolographEnvironment.GetGame().GetRoomManager().RequestRoomUnload(Id);
        }
        ServerMessage Updates = SerializeStatusUpdates(All: false);
        if (Updates != null)
        {
            SendMessage(Updates);
        }
    }

    public void AddUserToRoom(GameClient Session, bool Spectator)
    {
        RoomUser User = new RoomUser(Session.GetHabbo().Id, RoomId, UserCounter++);
        if (Spectator)
        {
            User.IsSpectator = true;
        }
        else
        {
            User.SetPos(Model.DoorX, Model.DoorY, Model.DoorZ);
            User.SetRot(Model.DoorOrientation);
            if (CheckRights(Session, RequireOwnership: true))
            {
                User.AddStatus("flatcrtl", "useradmin");
            }
            else if (CheckRights(Session))
            {
                User.AddStatus("flatcrtl", "");
            }
            if (!User.IsBot && User.GetClient().GetHabbo().IsTeleporting)
            {
                RoomItem Item = GetItem(User.GetClient().GetHabbo().TeleporterId);
                if (Item != null)
                {
                    User.SetPos(Item.X, Item.Y, Item.Z);
                    User.SetRot(Item.Rot);
                    Item.InteractingUser2 = Session.GetHabbo().Id;
                    Item.ExtraData = "2";
                    Item.UpdateState(inDb: false, inRoom: true);
                }
            }
            User.GetClient().GetHabbo().IsTeleporting = false;
            User.GetClient().GetHabbo().TeleporterId = 0u;
            ServerMessage EnterMessage = new ServerMessage(28u);
            EnterMessage.AppendInt32(1);
            User.Serialize(EnterMessage);
            SendMessage(EnterMessage);
        }
        UserList.Add(User);
        Session.GetHabbo().OnEnterRoom(Id);
        if (Spectator)
        {
            return;
        }
        UpdateUserCount();
        foreach (RoomUser Usr in UserList)
        {
            if (Usr.IsBot)
            {
                Usr.BotAI.OnUserEnterRoom(User);
            }
        }
    }

    public void RemoveUserFromRoom(GameClient Session, bool NotifyClient, bool NotifyKick)
    {
        try
        {
            if (Session == null)
            {
                return;
            }
            RoomUser User = GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null || !UserList.Remove(GetRoomUserByHabbo(Session.GetHabbo().Id)))
            {
                return;
            }
            if (NotifyClient)
            {
                if (NotifyKick)
                {
                    Session.GetMessageHandler().GetResponse().Init(33u);
                    Session.GetMessageHandler().GetResponse().AppendInt32(4008);
                    Session.GetMessageHandler().SendResponse();
                }
                Session.GetMessageHandler().GetResponse().Init(18u);
                Session.GetMessageHandler().SendResponse();
            }
            List<RoomUser> PetsToRemove = new List<RoomUser>();
            if (!User.IsSpectator)
            {
                if (User != null)
                {
                    UserMatrix[User.X, User.Y] = false;
                    ServerMessage LeaveMessage = new ServerMessage(29u);
                    LeaveMessage.AppendRawInt32(User.VirtualId);
                    SendMessage(LeaveMessage);
                }
                if (Session.GetHabbo() != null)
                {
                    if (HasActiveTrade(Session.GetHabbo().Id))
                    {
                        TryStopTrade(Session.GetHabbo().Id);
                    }
                    if (Session.GetHabbo().Username.ToLower() == Owner.ToLower() && HasOngoingEvent)
                    {
                        Event = null;
                        ServerMessage Message = new ServerMessage(370u);
                        Message.AppendStringWithBreak("-1");
                        SendMessage(Message);
                    }
                    Session.GetHabbo().OnLeaveRoom();
                }
            }
            if (!User.IsSpectator)
            {
                UpdateUserCount();
                List<RoomUser> Bots = new List<RoomUser>();
                foreach (RoomUser Usr in UserList)
                {
                    if (Usr.IsBot)
                    {
                        Bots.Add(Usr);
                    }
                }
                foreach (RoomUser Bot in Bots)
                {
                    Bot.BotAI.OnUserLeaveRoom(Session);
                    if (Bot.IsPet && Bot.PetData.OwnerId == Session.GetHabbo().Id && !CheckRights(Session, RequireOwnership: true))
                    {
                        PetsToRemove.Add(Bot);
                    }
                }
            }
            foreach (RoomUser toRemove in PetsToRemove)
            {
                Session.GetHabbo().GetInventoryComponent().AddPet(toRemove.PetData);
                RemoveBot(toRemove.VirtualId, Kicked: false);
            }
        }
        catch (NullReferenceException ex)
        {
            HolographEnvironment.GetLogging().WriteLine("NullReferenceException" + ex.ToString(), LogLevel.Error);
        }
    }

    public RoomUser GetPet(uint PetId)
    {
        List<RoomUser>.Enumerator Users = UserList.GetEnumerator();
        while (Users.MoveNext())
        {
            RoomUser User = Users.Current;
            if (User.IsBot && User.IsPet && User.PetData != null && User.PetData.PetId == PetId)
            {
                return User;
            }
        }
        return null;
    }

    public bool RoomContainsPet(uint PetId)
    {
        return GetPet(PetId) != null;
    }

    public void UpdateUserCount()
    {
        UsersNow = UserCount;
        using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
        dbClient.ExecuteQuery("Update rooms SET users_now = '" + UsersNow + "' WHERE id = '" + Id + "' LIMIT 1");
    }

    public RoomUser GetRoomUserByVirtualId(int VirtualId)
    {
        foreach (RoomUser User in UserList)
        {
            if (User.VirtualId == VirtualId)
            {
                return User;
            }
        }
        return null;
    }

    public RoomUser GetRoomUserByHabbo(uint Id)
    {
        foreach (RoomUser User in UserList)
        {
            if (User.IsBot || User.HabboId != Id)
            {
                continue;
            }
            return User;
        }
        return null;
    }

    public RoomUser GetRoomUserByHabbo(string Name)
    {
        foreach (RoomUser User in UserList)
        {
            if (User.IsBot || User.GetClient().GetHabbo() == null || !(User.GetClient().GetHabbo().Username.ToLower() == Name.ToLower()))
            {
                continue;
            }
            return User;
        }
        return null;
    }

    public void SendMessage(ServerMessage Message)
    {
        try
        {
            foreach (RoomUser User in UserList)
            {
                if (!User.IsBot && User.GetClient() != null)
                {
                    User.GetClient().SendMessage(Message);
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    public void SendMessageToUsersWithRights(ServerMessage Message)
    {
        foreach (RoomUser User in UserList)
        {
            if (!User.IsBot && CheckRights(User.GetClient()))
            {
                User.GetClient().SendMessage(Message);
            }
        }
    }

    public void Destroy()
    {
        SendMessage(new ServerMessage(18u));
        IdleTime = 0;
        KeepAlive = false;
        UserList.Clear();
    }

    public ServerMessage SerializeStatusUpdates(bool All)
    {
        List<RoomUser> Users = new List<RoomUser>();
        foreach (RoomUser User in UserList)
        {
            if (!All)
            {
                if (!User.UpdateNeeded)
                {
                    continue;
                }
                User.UpdateNeeded = false;
            }
            Users.Add(User);
        }
        if (Users.Count == 0)
        {
            return null;
        }
        ServerMessage Message = new ServerMessage(34u);
        Message.AppendInt32(Users.Count);
        foreach (RoomUser User in Users)
        {
            User.SerializeStatus(Message);
        }
        return Message;
    }

    public bool UserIsBanned(uint Id)
    {
        return Bans.ContainsKey(Id);
    }

    public void RemoveBan(uint Id)
    {
        Bans.Remove(Id);
    }

    public void AddBan(uint Id)
    {
        Bans.Add(Id, HolographEnvironment.GetUnixTimestamp());
    }

    public bool HasBanExpired(uint Id)
    {
        if (!UserIsBanned(Id))
        {
            return true;
        }
        double diff = HolographEnvironment.GetUnixTimestamp() - Bans[Id];
        if (diff > 900.0)
        {
            return true;
        }
        return false;
    }

    public int ItemCountByType(string InteractionType)
    {
        int i = 0;
        foreach (RoomItem Item in Items)
        {
            if (Item.GetBaseItem().InteractionType.ToLower() == InteractionType.ToLower())
            {
                i++;
            }
        }
        return i;
    }

    public bool HasActiveTrade(RoomUser User)
    {
        if (User.IsBot)
        {
            return false;
        }
        return HasActiveTrade(User.GetClient().GetHabbo().Id);
    }

    public bool HasActiveTrade(uint UserId)
    {
        foreach (Trade Trade in ActiveTrades)
        {
            if (Trade.ContainsUser(UserId))
            {
                return true;
            }
        }
        return false;
    }

    public Trade GetUserTrade(RoomUser User)
    {
        if (User.IsBot)
        {
            return null;
        }
        return GetUserTrade(User.GetClient().GetHabbo().Id);
    }

    public Trade GetUserTrade(uint UserId)
    {
        foreach (Trade Trade in ActiveTrades)
        {
            if (Trade.ContainsUser(UserId))
            {
                return Trade;
            }
        }
        return null;
    }

    public void TryStartTrade(RoomUser UserOne, RoomUser UserTwo)
    {
        if (UserOne != null && UserTwo != null && !UserOne.IsBot && !UserTwo.IsBot && !UserOne.IsTrading && !UserTwo.IsTrading && !HasActiveTrade(UserOne) && !HasActiveTrade(UserTwo))
        {
            ActiveTrades.Add(new Trade(UserOne.GetClient().GetHabbo().Id, UserTwo.GetClient().GetHabbo().Id, RoomId));
        }
    }

    public void TryStopTrade(uint UserId)
    {
        Trade Trade = GetUserTrade(UserId);
        if (Trade != null)
        {
            Trade.CloseTrade(UserId);
            ActiveTrades.Remove(Trade);
        }
    }

    public bool SetFloorItem(GameClient Session, RoomItem Item, int newX, int newY, int newRot, bool newItem)
    {
        Dictionary<int, AffectedTile> AffectedTiles = GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, newX, newY, newRot);
        if (!ValidTile(newX, newY))
        {
            return false;
        }
        foreach (AffectedTile Tile in AffectedTiles.Values)
        {
            if (!ValidTile(Tile.X, Tile.Y))
            {
                return false;
            }
        }
        double newZ = Model.SqFloorHeight[newX, newY];
        if (Item.Rot == newRot && Item.X == newX && Item.Y == newY && Item.Z != newZ)
        {
            return false;
        }
        if (Model.SqState[newX, newY] != 0)
        {
            return false;
        }
        foreach (AffectedTile Tile in AffectedTiles.Values)
        {
            if (Model.SqState[Tile.X, Tile.Y] != 0)
            {
                return false;
            }
        }
        if (!Item.GetBaseItem().IsSeat)
        {
            if (SquareHasUsers(newX, newY))
            {
                return false;
            }
            foreach (AffectedTile Tile in AffectedTiles.Values)
            {
                if (SquareHasUsers(Tile.X, Tile.Y))
                {
                    return false;
                }
            }
        }
        List<RoomItem> ItemsOnTile = GetFurniObjects(newX, newY);
        List<RoomItem> ItemsAffected = new List<RoomItem>();
        List<RoomItem> ItemsComplete = new List<RoomItem>();
        foreach (AffectedTile Tile in AffectedTiles.Values)
        {
            List<RoomItem> Temp = GetFurniObjects(Tile.X, Tile.Y);
            if (Temp != null)
            {
                ItemsAffected.AddRange(Temp);
            }
        }
        if (ItemsOnTile == null)
        {
            ItemsOnTile = new List<RoomItem>();
        }
        ItemsComplete.AddRange(ItemsOnTile);
        ItemsComplete.AddRange(ItemsAffected);
        foreach (RoomItem I in ItemsComplete)
        {
            if (I.Id == Item.Id || I.GetBaseItem().Stackable)
            {
                continue;
            }
            return false;
        }
        if (Item.Rot != newRot && Item.X == newX && Item.Y == newY)
        {
            newZ = Item.Z;
        }
        foreach (RoomItem I in ItemsComplete)
        {
            if (I.Id != Item.Id && I.TotalHeight > newZ)
            {
                newZ = I.TotalHeight;
            }
        }
        if (newRot != 0 && newRot != 2 && newRot != 4 && newRot != 6 && newRot != 8)
        {
            newRot = 0;
        }
        Item.X = newX;
        Item.Y = newY;
        Item.Z = newZ;
        Item.Rot = newRot;
        Item.Interactor.OnPlace(Session, Item);
        if (newItem)
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.AddParamWithValue("extra_data", Item.ExtraData);
                dbClient.ExecuteQuery("INSERT INTO room_items (id,room_id,base_item,extra_data,x,y,z,rot,wall_pos) VALUES ('" + Item.Id + "','" + RoomId + "','" + Item.BaseItem + "',@extra_data,'" + Item.X + "','" + Item.Y + "','" + Item.Z + "','" + Item.Rot + "','')");
            }
            Items.Add(Item);
            ServerMessage Message = new ServerMessage(93u);
            Item.Serialize(Message);
            SendMessage(Message);
        }
        else
        {
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                dbClient.ExecuteQuery("Update room_items SET x = '" + Item.X + "', y = '" + Item.Y + "', z = '" + Item.Z + "', rot = '" + Item.Rot + "', wall_pos = '' WHERE id = '" + Item.Id + "' LIMIT 1");
            }
            ServerMessage Message = new ServerMessage(95u);
            Item.Serialize(Message);
            SendMessage(Message);
        }
        GenerateMaps();
        UpdateUserStatusses();
        return true;
    }

    public bool SetWallItem(GameClient Session, RoomItem Item)
    {
        Item.Interactor.OnPlace(Session, Item);
        string text = Item.GetBaseItem().InteractionType.ToLower();
        if (text != null && text == "dimmer" && MoodlightData == null)
        {
            MoodlightData = new MoodlightData(Item.Id);
            Item.ExtraData = MoodlightData.GenerateExtraData();
        }
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.AddParamWithValue("extra_data", Item.ExtraData);
            dbClient.ExecuteQuery("INSERT INTO room_items (id,room_id,base_item,extra_data,x,y,z,rot,wall_pos) VALUES ('" + Item.Id + "','" + RoomId + "','" + Item.BaseItem + "',@extra_data,'0','0','0','0','" + Item.WallPos + "')");
        }
        Items.Add(Item);
        ServerMessage Message = new ServerMessage(83u);
        Item.Serialize(Message);
        SendMessage(Message);
        return true;
    }

    public void UpdateUserStatusses()
    {
        List<RoomUser>.Enumerator Users = UserList.GetEnumerator();
        while (Users.MoveNext())
        {
            UpdateUserStatus(Users.Current);
        }
    }

    public double SqAbsoluteHeight(int X, int Y)
    {
        List<RoomItem> ItemsOnSquare = GetFurniObjects(X, Y);
        double HighestStack = 0.0;
        bool deduct = false;
        double deductable = 0.0;
        if (ItemsOnSquare == null)
        {
            ItemsOnSquare = new List<RoomItem>();
        }
        if (ItemsOnSquare != null)
        {
            foreach (RoomItem Item in ItemsOnSquare)
            {
                if (Item.TotalHeight > HighestStack)
                {
                    if (Item.GetBaseItem().IsSeat || Item.GetBaseItem().InteractionType.ToLower() == "bed")
                    {
                        deduct = true;
                        deductable = Item.GetBaseItem().Height;
                    }
                    else
                    {
                        deduct = false;
                    }
                    HighestStack = Item.TotalHeight;
                }
            }
        }
        double floorHeight = Model.SqFloorHeight[X, Y];
        double stackHeight = HighestStack - Model.SqFloorHeight[X, Y];
        if (deduct)
        {
            stackHeight -= deductable;
        }
        if (stackHeight < 0.0)
        {
            stackHeight = 0.0;
        }
        return floorHeight + stackHeight;
    }

    public void UpdateUserStatus(RoomUser User)
    {
        if (HasThread.ContainsKey(User.HabboId))
        {
            // Migration to .NET 8
            // HasThread[User.HabboId].Abort();
            BallThread[User.HabboId].Interrupt();
            HasThread.Remove(User.HabboId);
        }
        if (BallThread.ContainsKey(User.HabboId))
        {
            // Migration to .NET 8
            // BallThread[User.HabboId].Abort();
            BallThread[User.HabboId].Interrupt();
            BallThread.Remove(User.HabboId);
        }
        if (User.Statusses.ContainsKey("lay") || User.Statusses.ContainsKey("sit"))
        {
            User.Statusses.Remove("lay");
            User.Statusses.Remove("sit");
            User.UpdateNeeded = true;
        }
        double newZ = SqAbsoluteHeight(User.X, User.Y);
        if (newZ != User.Z)
        {
            User.Z = newZ;
            User.UpdateNeeded = true;
        }
        if (Model.SqState[User.X, User.Y] == SquareState.SEAT)
        {
            if (!User.Statusses.ContainsKey("sit"))
            {
                User.Statusses.Add("sit", "1.0");
            }
            User.Z = Model.SqFloorHeight[User.X, User.Y];
            User.RotHead = Model.SqSeatRot[User.X, User.Y];
            User.RotBody = Model.SqSeatRot[User.X, User.Y];
            User.UpdateNeeded = true;
        }
        List<RoomItem> ItemsOnSquare = GetFurniObjects(User.X, User.Y);
        int BallX = 0;
        int BallY = 0;
        int Rot = User.RotBody;
        if (Rot == 3)
        {
            BallX = User.X + 1;
            BallY = User.Y + 1;
        }
        if (Rot == 4)
        {
            BallX = User.X;
            BallY = User.Y + 1;
        }
        if (Rot == 1)
        {
            BallX = User.X + 1;
            BallY = User.Y - 1;
        }
        if (Rot == 2)
        {
            BallX = User.X + 1;
            BallY = User.Y;
        }
        if (Rot == 0)
        {
            BallX = User.X;
            BallY = User.Y - 1;
        }
        if (Rot == 5)
        {
            BallX = User.X - 1;
            BallY = User.Y + 1;
        }
        if (Rot == 6)
        {
            BallX = User.X - 1;
            BallY = User.Y;
        }
        if (Rot == 7)
        {
            BallX = User.X - 1;
            BallY = User.Y - 1;
        }
        List<RoomItem> Ball = new List<RoomItem>();
        Ball = GetFurniObjects(BallX, BallY);
        if (Ball == null)
        {
            Ball = new List<RoomItem>();
        }
        RoomItem Item;
        foreach (RoomItem item in Ball)
        {
            Item = item;
            if (Item.GetBaseItem().InteractionType.ToLower() == "fball")
            {
                Thread BallT = new Thread((ThreadStart)delegate
                {
                    BallProcess(Item, User);
                });
                BallT.Start();
                BallThread.Add(User.HabboId, BallT);
                Item.ExtraData = "1";
                Item.UpdateState(inDb: false, inRoom: true);
            }
        }
        RoomItem Item2;
        foreach (RoomItem item2 in Ball)
        {
            Item2 = item2;
            if (Item2.GetBaseItem().InteractionType.ToLower() == "bb_puck")
            {
                Thread BallT = new Thread((ThreadStart)delegate
                {
                    BallProcess(Item2, User);
                });
                BallT.Start();
                BallThread.Add(User.HabboId, BallT);
                Item2.ExtraData = "1";
                Item2.UpdateState(inDb: false, inRoom: true);
            }
        }
        if (ItemsOnSquare == null)
        {
            ItemsOnSquare = new List<RoomItem>();
            if (HasWaterEffect.Contains(User.HabboId))
            {
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(0);
                SendMessage(Message);
                HasWaterEffect.Remove(User.HabboId);
                User.UpdateNeeded = true;
            }
            if (HasBlueBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
            {
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(0);
                SendMessage(Message);
                HasBlueBattleBallEffect.Remove(User.HabboId);
                User.UpdateNeeded = true;
            }
            if (HasYellowBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
            {
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(0);
                SendMessage(Message);
                HasYellowBattleBallEffect.Remove(User.HabboId);
                User.UpdateNeeded = true;
            }
            if (HasGreenBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
            {
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(0);
                SendMessage(Message);
                HasGreenBattleBallEffect.Remove(User.HabboId);
                User.UpdateNeeded = true;
            }
            if (HasRedBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
            {
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(0);
                SendMessage(Message);
                HasRedBattleBallEffect.Remove(User.HabboId);
                User.UpdateNeeded = true;
            }
        }
        if (HasBlueBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
        {
            ServerMessage Message = new ServerMessage(485u);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(0);
            SendMessage(Message);
            HasBlueBattleBallEffect.Remove(User.HabboId);
            User.UpdateNeeded = true;
        }
        if (HasYellowBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
        {
            ServerMessage Message = new ServerMessage(485u);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(0);
            SendMessage(Message);
            HasYellowBattleBallEffect.Remove(User.HabboId);
            User.UpdateNeeded = true;
        }
        if (HasGreenBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
        {
            ServerMessage Message = new ServerMessage(485u);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(0);
            SendMessage(Message);
            HasGreenBattleBallEffect.Remove(User.HabboId);
            User.UpdateNeeded = true;
        }
        if (HasRedBattleBallEffect.Contains(User.HabboId) && !User.IsPet)
        {
            ServerMessage Message = new ServerMessage(485u);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(0);
            SendMessage(Message);
            HasRedBattleBallEffect.Remove(User.HabboId);
            User.UpdateNeeded = true;
        }
        foreach (RoomItem Item3 in ItemsOnSquare)
        {
            DataTable Data = null;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM wiredtrigger WHERE roomid = '" + RoomId + "'");
            }
            if (Data != null)
            {
                foreach (DataRow Row in Data.Rows)
                {
                    if (!(Row["triggertype"].ToString() == "walkon") || int.Parse(Row["whattrigger"].ToString()) != Item3.Id)
                    {
                        continue;
                    }
                    using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                    if (dbClient.findsResult("SELECT SQL_NO_CACHE * from wiredaction where slotid = '" + Row["slotid"].ToString() + "'"))
                    {
                        string type = dbClient.ReadString(string.Concat("SELECT SQL_NO_CACHE typeaction from wiredaction where slotid = '", Row["slotid"], "'"));
                        if (type == "status")
                        {
                            RoomItem ItemToChange = GetItem(uint.Parse(dbClient.ReadString(string.Concat("SELECT SQL_NO_CACHE itemid from wiredaction where slotid = '", Row["slotid"], "'"))));
                            ItemToChange.ExtraData = dbClient.ReadString(string.Concat("SELECT SQL_NO_CACHE whataction from wiredaction where slotid = '", Row["slotid"], "'"));
                            ItemToChange.UpdateState();
                        }
                        else if (type == "kick")
                        {
                            RemoveUserFromRoom(User.GetClient(), NotifyClient: true, NotifyKick: false);
                        }
                    }
                }
            }
            if (Item3.GetBaseItem().InteractionType.ToLower() != "water" && HasWaterEffect.Contains(User.HabboId))
            {
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(0);
                SendMessage(Message);
                User.UpdateNeeded = true;
                HasWaterEffect.Remove(User.HabboId);
            }
            if (Item3.GetBaseItem().InteractionType.ToLower() == "swim2" && !HasWaterEffect.Contains(User.HabboId))
            {
                int EffectId = ((!(Item3.GetBaseItem().Name == "bw_water_1")) ? 30 : 30);
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(EffectId);
                SendMessage(Message);
                User.UpdateNeeded = true;
                HasWaterEffect.Add(User.HabboId);
            }
            if (Item3.GetBaseItem().InteractionType.ToLower() == "swim" && !HasWaterEffect.Contains(User.HabboId))
            {
                int EffectId = ((!(Item3.GetBaseItem().Name == "bw_water_2")) ? 29 : 29);
                ServerMessage Message = new ServerMessage(485u);
                Message.AppendInt32(User.VirtualId);
                Message.AppendInt32(EffectId);
                SendMessage(Message);
                User.UpdateNeeded = true;
                HasWaterEffect.Add(User.HabboId);
            }
            if (Item3.GetBaseItem().Name == "bb_gate_r")
            {
                ServerMessage GateR = new ServerMessage(485u);
                GateR.AppendInt32(User.VirtualId);
                GateR.AppendInt32(33);
                SendMessage(GateR);
                using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                dbClient.ExecuteQuery("UPDATE users SET bb_ball = 'r' WHERE id = '" + User.HabboId + "' LIMIT 1");
            }
            if (Item3.GetBaseItem().Name == "bb_gate_y")
            {
                ServerMessage GateY = new ServerMessage(485u);
                GateY.AppendInt32(User.VirtualId);
                GateY.AppendInt32(36);
                SendMessage(GateY);
                using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                dbClient.ExecuteQuery("UPDATE users SET bb_ball = 'y' WHERE id = '" + User.HabboId + "' LIMIT 1");
            }
            if (Item3.GetBaseItem().Name == "bb_gate_g")
            {
                ServerMessage GateG = new ServerMessage(485u);
                GateG.AppendInt32(User.VirtualId);
                GateG.AppendInt32(34);
                SendMessage(GateG);
                using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                dbClient.ExecuteQuery("UPDATE users SET bb_ball = 'g' WHERE id = '" + User.HabboId + "' LIMIT 1");
            }
            if (Item3.GetBaseItem().Name == "bb_gate_b")
            {
                ServerMessage GateB = new ServerMessage(485u);
                GateB.AppendInt32(User.VirtualId);
                GateB.AppendInt32(35);
                SendMessage(GateB);
                using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                dbClient.ExecuteQuery("UPDATE users SET bb_ball = 'b' WHERE id = '" + User.HabboId + "' LIMIT 1");
            }
            if (Item3.GetBaseItem().InteractionType.ToLower() == "bb_patch")
            {
                string pallo;
                using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                {
                    pallo = dbClient.ReadString("SELECT bb_ball FROM users WHERE id = '" + User.HabboId + "' LIMIT 1");
                }
                if (pallo == "r")
                {
                    string Color1 = "5";
                    string Color1_2 = "5";
                    ServerMessage ColorPlate = new ServerMessage(88u);
                    ServerMessage ColorPlate1_2 = new ServerMessage(88u);
                    ColorPlate.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate1_2.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate.AppendStringWithBreak(Color1);
                    SendMessage(ColorPlate);
                    Item3.ExtraData = "5";
                    ColorPlate.AppendStringWithBreak(Color1_2);
                    SendMessage(ColorPlate1_2);
                    Item3.ExtraData = "5";
                    Item3.UpdateState();
                }
                if (pallo == "y")
                {
                    string Color2 = "14";
                    string Color2_2 = "14";
                    ServerMessage ColorPlate = new ServerMessage(88u);
                    ServerMessage ColorPlate2_2 = new ServerMessage(88u);
                    ColorPlate.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate2_2.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate.AppendStringWithBreak(Color2);
                    SendMessage(ColorPlate);
                    Item3.ExtraData = "14";
                    ColorPlate.AppendStringWithBreak(Color2_2);
                    SendMessage(ColorPlate2_2);
                    Item3.ExtraData = "14";
                    Item3.UpdateState();
                }
                if (pallo == "g")
                {
                    string Color3 = "8";
                    string Color3_2 = "8";
                    ServerMessage ColorPlate = new ServerMessage(88u);
                    ServerMessage ColorPlate3_2 = new ServerMessage(88u);
                    ColorPlate.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate3_2.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate.AppendStringWithBreak(Color3);
                    SendMessage(ColorPlate);
                    Item3.ExtraData = "8";
                    ColorPlate.AppendStringWithBreak(Color3_2);
                    SendMessage(ColorPlate3_2);
                    Item3.ExtraData = "8";
                    Item3.UpdateState();
                }
                if (pallo == "b")
                {
                    string Color4 = "11";
                    string Color4_2 = "11";
                    ServerMessage ColorPlate = new ServerMessage(88u);
                    ServerMessage ColorPlate4_2 = new ServerMessage(88u);
                    ColorPlate.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate4_2.AppendStringWithBreak(Item3.Id.ToString());
                    ColorPlate.AppendStringWithBreak(Color4);
                    SendMessage(ColorPlate);
                    Item3.ExtraData = "11";
                    ColorPlate.AppendStringWithBreak(Color4_2);
                    SendMessage(ColorPlate4_2);
                    Item3.ExtraData = "11";
                    Item3.UpdateState();
                }
            }
            else if (Item3.GetBaseItem().InteractionType.ToLower() == "roller")
            {
                int newX = 0;
                int newY = 0;
                Coord UserPos = new Coord(User.X, User.Y);
                Coord ItemPos = new Coord(Item3.X, Item3.Y);
                if (UserPos == ItemPos)
                {
                    User.RemoveStatus("mv");
                    User.UpdateNeeded = true;
                    if (Item3.Rot == 4)
                    {
                        Thread.Sleep(900);
                        newY = User.Y + 1;
                        Coord MoveTo = new Coord(User.X, newY);
                        waitRoller(300);
                        User.MoveTo(MoveTo);
                        User.RemoveStatus("mv");
                        User.UpdateNeeded = true;
                        SendMessage(Rolando(User, newY, User.X, Item3));
                    }
                    if (Item3.Rot == 0)
                    {
                        Thread.Sleep(900);
                        newY = User.Y - 1;
                        Coord MoveTo = new Coord(User.X, newY);
                        waitRoller(300);
                        User.MoveTo(MoveTo);
                        User.RemoveStatus("mv");
                        User.UpdateNeeded = true;
                        User.UpdateNeeded = true;
                        User.ResetStatus();
                        SendMessage(Rolando(User, newY, User.X, Item3));
                    }
                    if (Item3.Rot == 6)
                    {
                        Thread.Sleep(900);
                        newX = User.X - 1;
                        Coord MoveTo = new Coord(newX, User.Y);
                        waitRoller(300);
                        User.MoveTo(MoveTo);
                        User.RemoveStatus("mv");
                        User.UpdateNeeded = true;
                        User.ResetStatus();
                        SendMessage(Rolando(User, User.Y, newX, Item3));
                    }
                    if (Item3.Rot == 2)
                    {
                        Thread.Sleep(900);
                        newX = User.X + 1;
                        Coord MoveTo = new Coord(newX, User.Y);
                        waitRoller(300);
                        User.MoveTo(MoveTo);
                        User.RemoveStatus("mv");
                        User.UpdateNeeded = true;
                        User.ResetStatus();
                        SendMessage(Rolando(User, User.Y, newX, Item3));
                    }
                }
            }
            if (Item3.GetBaseItem().IsSeat)
            {
                if (!User.Statusses.ContainsKey("sit"))
                {
                    User.Statusses.Add("sit", Item3.GetBaseItem().Height.ToString().Replace(',', '.'));
                }
                User.Z = Item3.Z;
                User.RotHead = Item3.Rot;
                User.RotBody = Item3.Rot;
                User.UpdateNeeded = true;
            }
            if (Item3.GetBaseItem().InteractionType.ToLower() == "bed")
            {
                if (!User.Statusses.ContainsKey("lay"))
                {
                    User.Statusses.Add("lay", Item3.GetBaseItem().Height.ToString().Replace(',', '.') + " null");
                }
                User.Z = Item3.Z;
                User.RotHead = Item3.Rot;
                User.RotBody = Item3.Rot;
                User.UpdateNeeded = true;
            }
        }
    }

    public ServerMessage Rolando(RoomUser User, int newY, int newX, RoomItem Item)
    {
        ServerMessage Message = new ServerMessage(230u);
        Message.AppendInt32(User.X);
        Message.AppendInt32(User.Y);
        Message.AppendInt32(newX);
        Message.AppendInt32(newY);
        Message.AppendBoolean(Bool: false);
        Message.AppendUInt(Item.Id);
        Message.AppendInt32(2);
        Message.AppendBoolean(Bool: false);
        Message.AppendString("0.45" + Convert.ToChar(2) + "0.0" + Convert.ToChar(2) + Convert.ToChar(1));
        return Message;
    }

    public void BallProcess(RoomItem ItemNow, RoomUser UserNow)
    {
        Thread.Sleep(10);
        int NewX = 0;
        int NewY = 0;
        int Rot = UserNow.RotBody;
        if (Rot == 3)
        {
            NewX = UserNow.X + 2;
            NewY = UserNow.Y + 2;
        }
        if (Rot == 4)
        {
            NewX = UserNow.X;
            NewY = UserNow.Y + 2;
        }
        if (Rot == 1)
        {
            NewX = UserNow.X + 2;
            NewY = UserNow.Y - 2;
        }
        if (Rot == 2)
        {
            NewX = UserNow.X + 2;
            NewY = UserNow.Y;
        }
        if (Rot == 0)
        {
            NewX = UserNow.X;
            NewY = UserNow.Y - 2;
        }
        if (Rot == 5)
        {
            NewX = UserNow.X - 2;
            NewY = UserNow.Y + 2;
        }
        if (Rot == 6)
        {
            NewX = UserNow.X - 2;
            NewY = UserNow.Y;
        }
        if (Rot == 7)
        {
            NewX = UserNow.X - 2;
            NewY = UserNow.Y - 2;
        }
        SetFloorItem(UserNow.GetClient(), ItemNow, NewX, NewY, ItemNow.Rot, newItem: false);
    }

    public bool ValidTile(int X, int Y)
    {
        if (X < 0 || Y < 0 || X >= Model.MapSizeX || Y >= Model.MapSizeY)
        {
            return false;
        }
        return true;
    }

    public void TurnHeads(int X, int Y, uint SenderId)
    {
        foreach (RoomUser User in UserList)
        {
            if (User.HabboId != SenderId)
            {
                User.SetRot(Rotation.Calculate(User.X, User.Y, X, Y), HeadOnly: true);
            }
        }
    }

    public List<RoomItem> GetFurniObjects(int X, int Y)
    {
        List<RoomItem> Results = new List<RoomItem>();
        foreach (RoomItem Item in FloorItems)
        {
            if (Item.X == X && Item.Y == Y)
            {
                Results.Add(Item);
            }
            Dictionary<int, AffectedTile> PointList = GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, Item.X, Item.Y, Item.Rot);
            foreach (AffectedTile Tile in PointList.Values)
            {
                if (Tile.X == X && Tile.Y == Y)
                {
                    Results.Add(Item);
                }
            }
        }
        if (Results.Count > 0)
        {
            return Results;
        }
        return null;
    }

    public RoomItem FindItem(uint Id)
    {
        foreach (RoomItem Item in Items)
        {
            if (Item.Id == Id)
            {
                return Item;
            }
        }
        return null;
    }

    public Dictionary<int, AffectedTile> GetAffectedTiles(int Length, int Width, int PosX, int PosY, int Rotation)
    {
        int x = 0;
        Dictionary<int, AffectedTile> PointList = new Dictionary<int, AffectedTile>();
        if (Length > 1)
        {
            if (Rotation == 0 || Rotation == 4)
            {
                for (int i = 1; i < Length; i++)
                {
                    PointList.Add(x++, new AffectedTile(PosX, PosY + i, i));
                    for (int j = 1; j < Width; j++)
                    {
                        PointList.Add(x++, new AffectedTile(PosX + j, PosY + i, (i < j) ? j : i));
                    }
                }
            }
            else if (Rotation == 2 || Rotation == 6)
            {
                for (int i = 1; i < Length; i++)
                {
                    PointList.Add(x++, new AffectedTile(PosX + i, PosY, i));
                    for (int j = 1; j < Width; j++)
                    {
                        PointList.Add(x++, new AffectedTile(PosX + i, PosY + j, (i < j) ? j : i));
                    }
                }
            }
        }
        if (Width > 1)
        {
            if (Rotation == 0 || Rotation == 4)
            {
                for (int i = 1; i < Width; i++)
                {
                    PointList.Add(x++, new AffectedTile(PosX + i, PosY, i));
                    for (int j = 1; j < Length; j++)
                    {
                        PointList.Add(x++, new AffectedTile(PosX + i, PosY + j, (i < j) ? j : i));
                    }
                }
            }
            else if (Rotation == 2 || Rotation == 6)
            {
                for (int i = 1; i < Width; i++)
                {
                    PointList.Add(x++, new AffectedTile(PosX, PosY + i, i));
                    for (int j = 1; j < Length; j++)
                    {
                        PointList.Add(x++, new AffectedTile(PosX + j, PosY + i, (i < j) ? j : i));
                    }
                }
            }
        }
        return PointList;
    }

    public bool SquareHasUsers(int X, int Y, bool LastStep)
    {
        if (AllowWalkthrough && !LastStep)
        {
            return false;
        }
        return SquareHasUsers(X, Y);
    }

    private void waitRoller(int time)
    {
        for (int i = 0; time > i; i++)
        {
        }
    }

    public bool SquareHasUsers(int X, int Y)
    {
        Coord Coord = BedMatrix[X, Y];
        return UserMatrix[Coord.x, Coord.y];
    }

    public string WallPositionCheck(string wallPosition)
    {
        try
        {
            if (wallPosition.Contains(Convert.ToChar(13)))
            {
                return null;
            }
            if (wallPosition.Contains(Convert.ToChar(9)))
            {
                return null;
            }
            string[] posD = wallPosition.Split(' ');
            if (posD[2] != "l" && posD[2] != "r")
            {
                return null;
            }
            string[] widD = posD[0].Substring(3).Split(',');
            int widthX = int.Parse(widD[0]);
            int widthY = int.Parse(widD[1]);
            if (widthX < 0 || widthY < 0 || widthX > 200 || widthY > 200)
            {
                return null;
            }
            string[] lenD = posD[1].Substring(2).Split(',');
            int lengthX = int.Parse(lenD[0]);
            int lengthY = int.Parse(lenD[1]);
            if (lengthX < 0 || lengthY < 0 || lengthX > 200 || lengthY > 200)
            {
                return null;
            }
            return ":w=" + widthX + "," + widthY + " l=" + lengthX + "," + lengthY + " " + posD[2];
        }
        catch
        {
            return null;
        }
    }

    public bool TilesTouching(int X1, int Y1, int X2, int Y2)
    {
        if (Math.Abs(X1 - X2) <= 1 && Math.Abs(Y1 - Y2) <= 1)
        {
            return true;
        }
        if (X1 == X2 && Y1 == Y2)
        {
            return true;
        }
        return false;
    }

    public int TileDistance(int X1, int Y1, int X2, int Y2)
    {
        return Math.Abs(X1 - X2) + Math.Abs(Y1 - Y2);
    }
}
