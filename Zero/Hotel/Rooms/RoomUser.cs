using System;
using System.Collections.Generic;
using System.Data;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Misc;
using Zero.Hotel.Pathfinding;
using Zero.Hotel.Pets;
using Zero.Hotel.RoomBots;
using Zero.Messages;
using Zero.Storage;
using ZeroEmu.Util;

namespace Zero.Hotel.Rooms;

internal class RoomUser
{
    public uint HabboId;

    public int VirtualId;

    public uint RoomId;

    public int IdleTime;

    public int X;

    public int Y;

    public double Z;

    public int CarryItemID;

    public int CarryTimer;

    public int RotHead;

    public int RotBody;

    public bool CanWalk;

    public bool AllowOverride;

    public int GoalX;

    public int GoalY;

    public bool SetStep;

    public int SetX;

    public int SetY;

    public double SetZ;

    public RoomBot BotData;

    public BotAI BotAI;

    public Pet PetData;

    public bool IsWalking;

    public bool UpdateNeeded;

    public bool IsAsleep;

    public Dictionary<string, string> Statusses;

    public int DanceId;

    public List<Coord> Path;

    public int PathStep;

    public bool PathRecalcNeeded;

    public int PathRecalcX;

    public int PathRecalcY;

    public int TeleDelay;

    public bool IsSpectator;

    public Coord Coordinate => new Coord(X, Y);

    public bool IsPet => IsBot && BotData.IsPet;

    public bool IsDancing
    {
        get
        {
            if (DanceId >= 1)
            {
                return true;
            }
            return false;
        }
    }

    public bool NeedsAutokick
    {
        get
        {
            if (IsBot)
            {
                return false;
            }
            if (IdleTime >= 1800)
            {
                return true;
            }
            return false;
        }
    }

    public bool IsTrading
    {
        get
        {
            if (IsBot)
            {
                return false;
            }
            if (Statusses.ContainsKey("trd"))
            {
                return true;
            }
            return false;
        }
    }

    public bool IsBot
    {
        get
        {
            if (BotData != null)
            {
                return true;
            }
            return false;
        }
    }

    public RoomUser(uint HabboId, uint RoomId, int VirtualId)
    {
        this.HabboId = HabboId;
        this.RoomId = RoomId;
        this.VirtualId = VirtualId;
        IdleTime = 0;
        X = 0;
        Y = 0;
        Z = 0.0;
        RotHead = 0;
        RotBody = 0;
        UpdateNeeded = true;
        Statusses = new Dictionary<string, string>();
        Path = new List<Coord>();
        PathStep = 0;
        TeleDelay = -1;
        AllowOverride = false;
        CanWalk = true;
        IsSpectator = false;
    }

    public void Unidle()
    {
        IdleTime = 0;
        if (IsAsleep)
        {
            IsAsleep = false;
            ServerMessage Message = new ServerMessage(486u);
            Message.AppendInt32(VirtualId);
            Message.AppendBoolean(Bool: false);
            GetRoom().SendMessage(Message);
        }
    }

    public void Chat(GameClient Session, string Message, bool Shout)
    {
        Unidle();
        if (!IsBot)
        {
            DataTable Data = null;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                Data = dbClient.ReadDataTable("SELECT SQL_NO_CACHE * FROM wiredtrigger WHERE roomid = '" + Session.GetHabbo().CurrentRoomId + "'");
            }
            if (Data != null)
            {
                foreach (DataRow Row in Data.Rows)
                {
                    if (!(Row["triggertype"].ToString() == "say") || !Message.Contains(Row["whattrigger"].ToString()))
                    {
                        continue;
                    }
                    using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
                    if (dbClient.findsResult("SELECT SQL_NO_CACHE * from wiredaction where slotid = '" + Row["slotid"].ToString() + "'"))
                    {
                        string type = dbClient.ReadString(string.Concat("SELECT SQL_NO_CACHE typeaction from wiredaction where slotid = '", Row["slotid"], "'"));
                        if (type == "status")
                        {
                            Room Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                            RoomItem Item = Room.GetItem(uint.Parse(dbClient.ReadString(string.Concat("SELECT SQL_NO_CACHE itemid from wiredaction where slotid = '", Row["slotid"], "'"))));
                            Item.ExtraData = dbClient.ReadString(string.Concat("SELECT SQL_NO_CACHE whataction from wiredaction where slotid = '", Row["slotid"], "'"));
                            Item.UpdateState();
                        }
                        else if (type == "kick")
                        {
                            GetRoom().RemoveUserFromRoom(Session, NotifyClient: true, NotifyKick: false);
                        }
                    }
                }
            }
        }
        if (!IsBot && GetClient().GetHabbo().Muted)
        {
            GetClient().SendNotif("Você está Mudo.");
        }
        else
        {
            if ((Message.StartsWith(":") && Session != null && ChatCommandHandler.Parse(Session, Message.Substring(1))) || Message == "www.holoscripter.ya.st" || Message == "www.Holoscripter.ya.st" || Message == "Hola, putos, cómo están?")
            {
                return;
            }
            switch (Message)
            {
                case "Hola, putos, cómo están?":
                    return;
                case "Visita [url=http://www.holoscripter.ya.st]www.holoscripter.ya.st[/url] o serás baneado":
                    return;
                case "Todos a bailar, o les baneamos del hotel!     ÂªÂªÂª _ ÂªÂªÂª":
                    return;
                case "Salgan de esta mierda de hotel, estamos infectados!! ÂªÂª":
                    return;
                case "Viva España           Yeahh!!":
                    return;
                case "Viva Al-Andalus       Yeahh      Âª":
                    return;
                case "Te meto el móvil por el culo y te llamo para que Vibre!":
                    return;
                case "Baneo = DDos <<<<<<<<< PiÃ©nsalo  ÂªÂª":
                    return;
                case "Inmortal ha ganado está batalla, Your loser":
                    return;
                case "Ayer le partí el culo a tu madre":
                    return;
                case "Estoy Sexy eh?":
                    return;
            }
            if (Message == "Sí, lo afirmo Inmortal es un troll y un spammer, quien lo iba a esperar?")
            {
                return;
            }
            switch (Message)
            {
                case "www.Holoscripter.mforos.com":
                    return;
                case "Hacker":
                    return;
                case "Puta":
                    return;
                case "Gay":
                    return;
                case "Viado":
                    return;
                case "Filho da Puta":
                    return;
                case "Habbinfo":
                    return;
                case "Habbinho":
                    return;
                case "Habbox":
                    return;
                case "Holo":
                    return;
                case "Sulkea":
                    return;
                case "Inmortal ha ganado está batalla, Your loser":
                    return;
                case "Ayer le partí el culo a tu madre":
                    return;
                case "Estoy Sexy eh?":
                    return;
            }
            if (Message == "Sí, lo afirmo Inmortal es un troll y un spammer, quien lo iba a esperar?")
            {
                return;
            }
            switch (Message)
            {
                case "www.Holoscripter.mforos.com":
                    return;
                case "Hacker":
                    return;
                case "Puta":
                    return;
                case "Gay":
                    return;
                case "Viado":
                    return;
                case "Filha da Puta":
                    return;
                case "Habbinfo":
                    return;
                case "Habbinho":
                    return;
                case "Habbox":
                    return;
                case "Holo":
                    return;
                case "Sulkea":
                    return;
                case "Inmortal ha ganado está batalla, Your loser":
                    return;
                case "Ayer le partí el culo a tu madre":
                    return;
                case "Estoy Sexy eh?":
                    return;
            }
            if (Message == "Sí, lo afirmo Inmortal es un troll y un spammer, quien lo iba a esperar?" || (Message.StartsWith("#") && Session != null && ZeroExperience.Parse(Session, Message.Substring(1))))
            {
                return;
            }
            uint ChatHeader = 24u;
            if (Shout)
            {
                ChatHeader = 26u;
            }
            ServerMessage ChatMessage = new ServerMessage(ChatHeader);
            ChatMessage.AppendInt32(VirtualId);
            ChatMessage.AppendStringWithBreak(Message);
            ChatMessage.AppendInt32(GetSpeechEmotion(Message));
            GetRoom().TurnHeads(X, Y, HabboId);
            GetRoom().SendMessage(ChatMessage);
            if (!IsBot)
            {
                GetRoom().OnUserSay(this, Message, Shout);
            }
            if (IsBot)
            {
                return;
            }
            using DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient();
            dbClient.AddParamWithValue("message", Message);
            dbClient.ExecuteQuery("INSERT INTO chatlogs (user_id,room_id,hour,minute,timestamp,message,user_name,full_date) VALUES ('" + Session.GetHabbo().Id + "','" + GetRoom().RoomId + "','" + DateTime.Now.Hour + "','" + DateTime.Now.Minute + "','" + HolographEnvironment.GetUnixTimestamp() + "',@message,'" + Session.GetHabbo().Username + "','" + DateTime.Now.ToLongDateString() + "')");
        }
    }

    public int GetSpeechEmotion(string Message)
    {
        Message = Message.ToLower();
        if (Message.Contains(":)") || Message.Contains(":d") || Message.Contains("=]") || Message.Contains("=d") || Message.Contains(":>"))
        {
            return 1;
        }
        if (Message.Contains(">:(") || Message.Contains(":@") || Message.Contains(":X"))
        {
            return 2;
        }
        if (Message.Contains(":o"))
        {
            return 3;
        }
        if (Message.Contains(":(") || Message.Contains("=[") || Message.Contains(":'(") || Message.Contains("='[") || Message.Contains(":T"))
        {
            return 4;
        }
        return 0;
    }

    public void ClearMovement(bool Update)
    {
        IsWalking = false;
        PathRecalcNeeded = false;
        Path = new List<Coord>();
        Statusses.Remove("mv");
        GoalX = 0;
        GoalY = 0;
        SetStep = false;
        SetX = 0;
        SetY = 0;
        SetZ = 0.0;
        if (Update)
        {
            UpdateNeeded = true;
        }
    }

    public void MoveTo(Coord c)
    {
        MoveTo(c.x, c.y);
    }

    public void MoveTo(int X, int Y)
    {
        Unidle();
        PathRecalcNeeded = true;
        PathRecalcX = X;
        PathRecalcY = Y;
    }

    public void Roll(Coord c)
    {
        MoveTo(c.x, c.y);
    }

    public void Roll(int X, int Y)
    {
        Unidle();
        ServerMessage RollPerson = new ServerMessage();
        RollPerson.Init(230u);
        GetRoom().SendMessage(RollPerson);
    }

    public void UnlockWalking()
    {
        AllowOverride = false;
        CanWalk = true;
    }

    public void SetPos(int X, int Y, double Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    public void CarryItem(int Item)
    {
        CarryItemID = Item;
        if (Item > 0)
        {
            CarryTimer = 240;
        }
        else
        {
            CarryTimer = 0;
        }
        ServerMessage Message = new ServerMessage(482u);
        Message.AppendInt32(VirtualId);
        Message.AppendInt32(Item);
        GetRoom().SendMessage(Message);
    }

    public void SetRot(int Rotation)
    {
        SetRot(Rotation, HeadOnly: false);
    }

    public void SetRot(int Rotation, bool HeadOnly)
    {
        if (Statusses.ContainsKey("lay") || IsWalking)
        {
            return;
        }
        int diff = RotBody - Rotation;
        RotHead = RotBody;
        if (Statusses.ContainsKey("sit") || HeadOnly)
        {
            if (RotBody == 2 || RotBody == 4)
            {
                if (diff > 0)
                {
                    RotHead = RotBody - 1;
                }
                else if (diff < 0)
                {
                    RotHead = RotBody + 1;
                }
            }
            else if (RotBody == 0 || RotBody == 6)
            {
                if (diff > 0)
                {
                    RotHead = RotBody - 1;
                }
                else if (diff < 0)
                {
                    RotHead = RotBody + 1;
                }
            }
        }
        else if (diff <= -2 || diff >= 2)
        {
            RotHead = Rotation;
            RotBody = Rotation;
        }
        else
        {
            RotHead = Rotation;
        }
        UpdateNeeded = true;
    }

    public void AddStatus(string Key, string Value)
    {
        Statusses[Key] = Value;
    }

    public void RemoveStatus(string Key)
    {
        if (Statusses.ContainsKey(Key))
        {
            Statusses.Remove(Key);
        }
    }

    public void ResetStatus()
    {
        Statusses = new Dictionary<string, string>();
    }

    public void Serialize(ServerMessage Message)
    {
        if (IsSpectator)
        {
            return;
        }
        if (!IsBot)
        {
            Message.AppendUInt(GetClient().GetHabbo().Id);
            Message.AppendStringWithBreak(GetClient().GetHabbo().Username);
            Message.AppendStringWithBreak(GetClient().GetHabbo().Motto);
            Message.AppendStringWithBreak(GetClient().GetHabbo().Look);
            Message.AppendInt32(VirtualId);
            Message.AppendInt32(X);
            Message.AppendInt32(Y);
            Message.AppendStringWithBreak(Z.ToString().Replace(',', '.'));
            Message.AppendInt32(2);
            Message.AppendInt32(1);
            Message.AppendStringWithBreak(GetClient().GetHabbo().Gender.ToLower());
            Message.AppendInt32(-1);
            Message.AppendInt32(-1);
            Message.AppendInt32(-1);
            Message.AppendStringWithBreak("");
        }
        else
        {
            Message.AppendInt32(BotAI.BaseId);
            Message.AppendStringWithBreak(BotData.Name);
            Message.AppendStringWithBreak(BotData.Motto);
            Message.AppendStringWithBreak(BotData.Look);
            Message.AppendInt32(VirtualId);
            Message.AppendInt32(X);
            Message.AppendInt32(Y);
            Message.AppendStringWithBreak(Z.ToString().Replace(',', '.'));
            Message.AppendInt32(4);
            Message.AppendInt32((BotData.AiType.ToLower() == "pet") ? 2 : 3);
            if (BotData.AiType.ToLower() == "pet")
            {
                Message.AppendInt32(0);
            }
        }
    }

    public void SerializeStatus(ServerMessage Message)
    {
        if (IsSpectator)
        {
            return;
        }
        Message.AppendInt32(VirtualId);
        Message.AppendInt32(X);
        Message.AppendInt32(Y);
        Message.AppendStringWithBreak(Z.ToString().Replace(',', '.'));
        Message.AppendInt32(RotHead);
        Message.AppendInt32(RotBody);
        Message.AppendString("/");
        foreach (KeyValuePair<string, string> Status in Statusses)
        {
            Message.AppendString(Status.Key);
            Message.AppendString(" ");
            Message.AppendString(Status.Value);
            Message.AppendString("/");
        }
        Message.AppendStringWithBreak("/");
    }

    public GameClient GetClient()
    {
        if (IsBot)
        {
            return null;
        }
        return HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(HabboId);
    }

    private Room GetRoom()
    {
        return HolographEnvironment.GetGame().GetRoomManager().GetRoom(RoomId);
    }
}
