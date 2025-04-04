using System;
using System.Data;
using System.Net.Sockets;
using System.Text;
using Zero.Core;
using Zero.Hotel.GameClients;
using Zero.Hotel.Rooms;
using Zero.Messages;
using Zero.Storage;

namespace Zero.Net;

internal class MusConnection
{
    private Socket socket;

    private byte[] buffer = new byte[1024];

    public MusConnection(Socket _socket)
    {
        socket = _socket;
        try
        {
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnEvent_RecieveData, null);
        }
        catch (Exception)
        {
            tryClose();
        }
    }

    public void tryClose()
    {
        try
        {
            socket.Close();
        }
        catch (Exception)
        {
        }
    }

    public void OnEvent_RecieveData(IAsyncResult iAr)
    {
        try
        {
            int bytes = socket.EndReceive(iAr);
            string data = Encoding.Default.GetString(buffer, 0, bytes);
            if (data.Length > 0)
            {
                processCommand(data);
            }
        }
        catch (Exception)
        {
        }
        tryClose();
    }

    public void processCommand(string data)
    {
        string header = data.Split(Convert.ToChar(1))[0];
        string param = data.Split(Convert.ToChar(1))[1];
        uint userId = 0u;
        GameClient Client = null;
        Room Room = null;
        RoomUser RoomUser = null;
        DataRow Row = null;
        ServerMessage Message = null;
        switch (header.ToLower())
        {
            case "Updatecredits":
                if (param == "ALL")
                {
                    HolographEnvironment.GetGame().GetClientManager().DeployHotelCreditsUpdate();
                    break;
                }
                Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(uint.Parse(param));
                if (Client != null)
                {
                    int newCredits = 0;
                    using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                    {
                        newCredits = (int)dbClient.ReadDataRow("SELECT credits FROM users WHERE id = '" + Client.GetHabbo().Id + "' LIMIT 1")[0];
                    }
                    Client.GetHabbo().Credits = newCredits;
                    Client.GetHabbo().UpdateCreditsBalance(InDatabase: false);
                }
                break;
            case "reloadbans":
                HolographEnvironment.GetGame().GetBanManager().LoadBans();
                HolographEnvironment.GetGame().GetClientManager().CheckForAllBanConflicts();
                break;
            case "signout":
                HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(uint.Parse(param))
                    .Disconnect();
                break;
            case "Updatetags":
                Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(uint.Parse(param));
                Client.GetHabbo().LoadTags();
                break;
            case "ha":
                {
                    ServerMessage HotelAlert = new ServerMessage(139u);
                    HotelAlert.AppendStringWithBreak("Mensagem da administração: " + param);
                    HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(HotelAlert);
                    break;
                }
            case "Updatemotto":
            case "Updatelook":
                {
                    userId = uint.Parse(param);
                    Client = HolographEnvironment.GetGame().GetClientManager().GetClientByHabbo(userId);
                    using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
                    {
                        Row = dbClient.ReadDataRow("SELECT look,gender,motto,mutant_penalty,block_newfriends FROM users WHERE id = '" + Client.GetHabbo().Id + "' LIMIT 1");
                    }
                    Client.GetHabbo().Look = (string)Row["look"];
                    Client.GetHabbo().Gender = Row["gender"].ToString().ToLower();
                    Client.GetHabbo().Motto = HolographEnvironment.FilterInjectionChars((string)Row["motto"]);
                    Client.GetHabbo().BlockNewFriends = HolographEnvironment.EnumToBool(Row["block_newfriends"].ToString());
                    if (Row["mutant_penalty"].ToString() != "0" && !Client.GetHabbo().MutantPenalty)
                    {
                        Client.SendNotif("For scripting and/or manipulating your look, we have decided to punish you, by changing and locking your look and motto for a week (or perhaps permanently, depending on our mood). Enjoy!");
                        Client.GetHabbo().MutantPenalty = true;
                    }
                    Client.GetMessageHandler().GetResponse().Init(266u);
                    Client.GetMessageHandler().GetResponse().AppendInt32(-1);
                    Client.GetMessageHandler().GetResponse().AppendStringWithBreak(Client.GetHabbo().Look);
                    Client.GetMessageHandler().GetResponse().AppendStringWithBreak(Client.GetHabbo().Gender.ToLower());
                    Client.GetMessageHandler().GetResponse().AppendStringWithBreak(Client.GetHabbo().Motto);
                    Client.GetMessageHandler().SendResponse();
                    if (Client.GetHabbo().InRoom)
                    {
                        Room = HolographEnvironment.GetGame().GetRoomManager().GetRoom(Client.GetHabbo().CurrentRoomId);
                        RoomUser = Room.GetRoomUserByHabbo(Client.GetHabbo().Id);
                        Message = new ServerMessage(266u);
                        Message.AppendInt32(RoomUser.VirtualId);
                        Message.AppendStringWithBreak(Client.GetHabbo().Look);
                        Message.AppendStringWithBreak(Client.GetHabbo().Gender.ToLower());
                        Message.AppendStringWithBreak(Client.GetHabbo().Motto);
                        Room.SendMessage(Message);
                    }
                    switch (header.ToLower())
                    {
                        case "Updatemotto":
                            HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(Client, 5u, 1);
                            break;
                        case "Updatelook":
                            if (!Client.GetHabbo().MutantPenalty)
                            {
                                HolographEnvironment.GetGame().GetAchievementManager().UnlockAchievement(Client, 1u, 1);
                            }
                            break;
                    }
                    break;
                }
            default:
                HolographEnvironment.GetLogging().WriteLine("Packet MUS Sem Classificar: " + data, LogLevel.Error);
                break;
        }
    }
}
