using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Zero.Core;
using Zero.Hotel.GameClients;
using Zero.Storage;

namespace Zero.Hotel.Rooms;

internal class RoomManager
{
    public readonly int MAX_PETS_PER_ROOM = 10;

    private ConcurrentDictionary<uint, Room> Rooms;

    private ConcurrentDictionary<string, RoomModel> Models;

    private SynchronizedCollection<TeleUserData> TeleActions;

    private Thread EngineThread;

    private SynchronizedCollection<uint> RoomsToUnload;

    public int LoadedRoomsCount => Rooms.Count;

    public RoomManager()
    {
        this.Rooms = new ConcurrentDictionary<uint, Room>();
        this.Models = new ConcurrentDictionary<string, RoomModel>();
        this.TeleActions = new SynchronizedCollection<TeleUserData>();
        this.EngineThread = new Thread(ProcessEngine);
        this.EngineThread.Name = "Room Engine";
        this.EngineThread.Priority = ThreadPriority.AboveNormal;
        this.EngineThread.Start();
        this.RoomsToUnload = new SynchronizedCollection<uint>();
    }

    public void AddTeleAction(TeleUserData Act)
    {
            TeleActions.Add(Act);
    }

    public List<Room> GetEventRoomsForCategory(int Category)
    {
        List<Room> EventRooms = new List<Room>();

        foreach (var kvp in this.Rooms)
        {
            Room Room = kvp.Value;

            if (Room.Event == null)
            {
                continue;
            }

            if (Category > 0 && Room.Event.Category != Category)
            {
                continue;
            }

            EventRooms.Add(Room);
        }

        return EventRooms;
    }

    public void ProcessEngine()
    {
        Thread.Sleep(5000);

        while (true)
        {
            DateTime ExecutionStart = DateTime.Now;

            try
            {
                /*
                foreach (var kvp in this.Rooms)
                {
                    Room Room = kvp.Value;

                    if (!Room.KeepAlive)
                    {
                        continue;
                    }

                    Room.ProcessRoom();
                }*/

                foreach (uint RoomId in this.RoomsToUnload)
                {
                    UnloadRoom(RoomId);
                }

                this.RoomsToUnload.Clear();


                foreach (var eTeleActions in this.TeleActions)
                {
                    eTeleActions.Execute();
                }

                this.TeleActions.Clear();
                /*
                badlock (this.TeleActions)
                {
                    ConcurrentDictionary<TeleUserData>.Enumerator eTeleActions = this.TeleActions.GetEnumerator();

                    while (eTeleActions.MoveNext())
                    {
                        eTeleActions.Current.Execute();
                    }

                    this.TeleActions.Clear();
                }*/
            }

            catch (InvalidOperationException)
            {
                HolographEnvironment.GetLogging().WriteLine("InvalidOpException in Room Manager..", Core.LogLevel.Error);
            }

            finally
            {
                DateTime ExecutionComplete = DateTime.Now;
                TimeSpan Diff = ExecutionComplete - ExecutionStart;

                double sleepTime = 500 - Diff.TotalMilliseconds;

                if (sleepTime < 0)
                {
                    sleepTime = 0;
                }

                if (sleepTime > 500)
                {
                    sleepTime = 500;
                }

                Thread.Sleep((int)Math.Floor(sleepTime));
            }
        }
    }

    public void LoadModels()
    {
        DataTable Data = null;
        Models.Clear();
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT id,door_x,door_y,door_z,door_dir,heightmap,public_items,club_only FROM room_models");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            Models.TryAdd((string)Row["id"], new RoomModel((string)Row["id"], (int)Row["door_x"], (int)Row["door_y"], (double)Row["door_z"], (int)Row["door_dir"], (string)Row["heightmap"], (string)Row["public_items"], HolographEnvironment.EnumToBool(Row["club_only"].ToString())));
        }
    }

    public RoomModel GetModel(string Model)
    {
        if (Models.ContainsKey(Model))
        {
            return Models[Model];
        }
        return null;
    }

    public RoomData GenerateNullableRoomData(uint RoomId)
    {
        if (GenerateRoomData(RoomId) != null)
        {
            return GenerateRoomData(RoomId);
        }
        RoomData Data = new RoomData();
        Data.FillNull(RoomId);
        return Data;
    }

    public RoomData GenerateRoomData(uint RoomId)
    {
        RoomData Data = new RoomData();
        if (IsRoomLoaded(RoomId))
        {
            Data.Fill(GetRoom(RoomId));
        }
        else
        {
            DataRow Row = null;
            using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
            {
                Row = dbClient.ReadDataRow("SELECT * FROM rooms WHERE id = '" + RoomId + "' LIMIT 1");
            }
            if (Row == null)
            {
                return null;
            }
            Data.Fill(Row);
        }
        return Data;
    }

    public bool IsRoomLoaded(uint RoomId)
    {
        if (GetRoom(RoomId) != null)
        {
            return true;
        }
        return false;
    }

    public void LoadRoom(uint Id)
    {
        if (IsRoomLoaded(Id))
        {
            return;
        }
        RoomData Data = GenerateRoomData(Id);
        if (Data != null)
        {
            Room Room = new Room(Data.Id, Data.Name, Data.Description, Data.Type, Data.Owner, Data.Category, Data.State, Data.UsersMax, Data.ModelName, Data.CCTs, Data.Score, Data.Tags, Data.AllowPets, Data.AllowPetsEating, Data.AllowWalkthrough, Data.Hidewall, Data.Icon, Data.Password, Data.Wallpaper, Data.Floor, Data.Landscape);
                Rooms.TryAdd(Room.RoomId, Room);
            Room.InitBots();
            Room.InitPets();
            Room.StartProcessRoutine();
        }
    }

    public void RequestRoomUnload(uint Id)
    {
        if (!IsRoomLoaded(Id))
        {
            return;
        }
            GetRoom(Id).KeepAlive = false;
        GetRoom(Id).StopProcessRoutine();
        RoomsToUnload.Add(Id);
    }

    public void UnloadRoom(uint Id)
    {
        Room Room = GetRoom(Id);
        if (Room == null)
        {
            return;
        }
        lock (Rooms)
        {
            Room.Destroy();
            Rooms.TryRemove(Id, out var _);
        }
    }

    public Room GetRoom(uint RoomId)
    {
        foreach (var kvp in this.Rooms)
        {
            Room Room = kvp.Value;

            if (Room.RoomId == RoomId)
            {
                return Room;
            }
        }

        return null;
    }

    public RoomData CreateRoom(GameClient Session, string Name, string Model)
    {
        Name = HolographEnvironment.FilterInjectionChars(Name);

        if (!Models.ContainsKey(Model))
        {
            Session.SendNotif("Sorry, this room model has not been added yet. Try again later.");
            return null;
        }

        if (Models[Model].ClubOnly && !Session.GetHabbo().HasFuse("fuse_use_special_room_layouts"))
        {
            Session.SendNotif("Club members only.");
            return null;
        }

        if (Name.Length < 3)
        {
            Session.SendNotif("Room name is too short.");
            return null;
        }

        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.AddParamWithValue("caption", Name);
            dbClient.AddParamWithValue("model", Model);
            dbClient.AddParamWithValue("username", Session.GetHabbo().Username);
            try
            {
                dbClient.ExecuteQuery("INSERT INTO rooms (roomtype,caption,owner,model_name) VALUES ('private',@caption,@username,@model)");
            } catch (Exception e)
            {
                Session.SendNotif("Couldn't create room. Error was logged.");
                Console.WriteLine(e);
                return null;
            }
        }

        uint RoomId = 0u;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.AddParamWithValue("caption", Name);
            dbClient.AddParamWithValue("username", Session.GetHabbo().Username);
            RoomId = (uint)dbClient.ReadDataRow("SELECT id FROM rooms WHERE owner = @username AND caption = @caption ORDER BY id DESC")[0];
        }

        return GenerateRoomData(RoomId);
    }
}
