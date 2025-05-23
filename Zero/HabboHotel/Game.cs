using System;
using System.Threading;
using Zero.Hotel.Achievements;
using Zero.Hotel.Advertisements;
using Zero.Hotel.Catalogs;
using Zero.Hotel.GameClients;
using Zero.Hotel.Items;
using Zero.Hotel.Misc;
using Zero.Hotel.Navigators;
using Zero.Hotel.Roles;
using Zero.Hotel.RoomBots;
using Zero.Hotel.Rooms;
using Zero.Hotel.Support;
using Zero.Storage;

namespace Zero.HabboHotel;

internal class Game
{
    private const string Version = "Release 58+";

    private GameClientManager ClientManager;

    private ModerationBanManager BanManager;

    private RoleManager RoleManager;

    private HelpTool HelpTool;

    private Catalog Catalog;

    private Navigator Navigator;

    private ItemManager ItemManager;

    private RoomManager RoomManager;

    private AdvertisementManager AdvertisementManager;

    private PixelManager PixelManager;

    private AchievementManager AchievementManager;

    private ModerationTool ModerationTool;

    private BotManager BotManager;

    private Thread StatisticsThread;

    public Game()
    {
        ClientManager = new GameClientManager();
        if (HolographEnvironment.GetConfig().data["client.ping.enabled"] == "1")
        {
            ClientManager.StartConnectionChecker();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[DB.openConnection] » Database connected successfully.");
        }
        if (HolographEnvironment.GetConfig().data["client.ping.enabled"] == "0")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[DB.openConnection] » Database connected successfully.");
            Console.WriteLine("");
        }
        BanManager = new ModerationBanManager();
        RoleManager = new RoleManager();
        HelpTool = new HelpTool();
        Catalog = new Catalog();
        Navigator = new Navigator();
        ItemManager = new ItemManager();
        RoomManager = new RoomManager();
        AdvertisementManager = new AdvertisementManager();
        PixelManager = new PixelManager();
        AchievementManager = new AchievementManager();
        ModerationTool = new ModerationTool();
        BotManager = new BotManager();
        BanManager.LoadBans();
        RoleManager.LoadRoles();
        RoleManager.LoadRights();
        HelpTool.LoadCategories();
        HelpTool.LoadTopics();
        Catalog.Initialize();
        Navigator.Initialize();
        ItemManager.LoadItems();
        RoomManager.LoadModels();
        AdvertisementManager.LoadRoomAdvertisements();
        PixelManager.Start();
        AchievementManager.LoadAchievements();
        ModerationTool.LoadMessagePresets();
        ModerationTool.LoadPendingTickets();
        BotManager.LoadBots();
        DatabaseCleanup(1);
        StatisticsThread = new Thread(LowPriorityWorker.Process);
        StatisticsThread.Name = "Low Priority Worker";
        StatisticsThread.Priority = ThreadPriority.Lowest;
        StatisticsThread.Start();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Starting - For updates visit www.holograph-emulator.com");
    }

    public void DatabaseCleanup(int serverStatus)
    {
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            dbClient.ExecuteQuery("Update users SET auth_ticket = '', online = '0'");
            dbClient.ExecuteQuery("Update rooms SET users_now = '0'");
            dbClient.ExecuteQuery("Update user_roomvisits SET exit_timestamp = '" + HolographEnvironment.GetUnixTimestamp() + "' WHERE exit_timestamp <= 0");
            dbClient.ExecuteQuery("Update server_status SET status = '" + serverStatus + "', users_online = '0', rooms_loaded = '0', server_ver = '" + HolographEnvironment.Version + "', stamp = '" + HolographEnvironment.GetUnixTimestamp() + "' LIMIT 1");
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[stringManager.antiHacker] » Anti-hacker filter activated...");
        Console.WriteLine("[stringManager.antiHacker] » 38 anti-hacker filters detected.");
        Console.WriteLine("");
        Console.WriteLine("[catalogueManager.Init] » Initializing catalog cache + items...");
        Console.WriteLine("[catalogueManager.Init] » Cache successfully initialized (active).");
        Console.WriteLine("");
        Console.WriteLine("[rankManager.Init] » Initializing Rank System...");
        Console.WriteLine("[rankManager.Init] » Initialized, 7 Ranks Detected.");
        Console.WriteLine("");
        Console.WriteLine("[gameSocketServer.Init] » Initializing Game Connect...");
        Console.WriteLine("[gameSocketServer.Init] » GameConnect initialized successfully.");
        Console.WriteLine("[gameSocketServer.Init] » Database reset for quality improvement.");
        Console.WriteLine("");
    }

    public void Destroy()
    {
        if (StatisticsThread != null)
        {
            try
            {
                // Migration to .NET 8
                // StatisticsThread.Abort(); 
                StatisticsThread.Interrupt();
            }
            catch (ThreadInterruptedException)
            {
            }
            StatisticsThread = null;
        }
        DatabaseCleanup(0);
        if (GetClientManager() != null)
        {
            GetClientManager().Clear();
            GetClientManager().StopConnectionChecker();
        }
        if (GetPixelManager() != null)
        {
            PixelManager.KeepAlive = false;
        }
        ClientManager = null;
        BanManager = null;
        RoleManager = null;
        HelpTool = null;
        Catalog = null;
        Navigator = null;
        ItemManager = null;
        RoomManager = null;
        AdvertisementManager = null;
        PixelManager = null;
        HolographEnvironment.GetLogging().WriteLine("Hotel Desligado.");
    }

    public GameClientManager GetClientManager()
    {
        return ClientManager;
    }

    public ModerationBanManager GetBanManager()
    {
        return BanManager;
    }

    public RoleManager GetRoleManager()
    {
        return RoleManager;
    }

    public HelpTool GetHelpTool()
    {
        return HelpTool;
    }

    public Catalog GetCatalog()
    {
        return Catalog;
    }

    public Navigator GetNavigator()
    {
        return Navigator;
    }

    public ItemManager GetItemManager()
    {
        return ItemManager;
    }

    public RoomManager GetRoomManager()
    {
        return RoomManager;
    }

    public AdvertisementManager GetAdvertisementManager()
    {
        return AdvertisementManager;
    }

    public PixelManager GetPixelManager()
    {
        return PixelManager;
    }

    public AchievementManager GetAchievementManager()
    {
        return AchievementManager;
    }

    public ModerationTool GetModerationTool()
    {
        return ModerationTool;
    }

    public BotManager GetBotManager()
    {
        return BotManager;
    }
}