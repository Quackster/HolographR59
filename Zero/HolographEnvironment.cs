using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Zero.Core;
using Zero.HabboHotel;
using Zero.Net;
using Zero.Storage;

namespace Zero;

internal class HolographEnvironment
{
	private static Logging Logging;

	private static ConfigurationData Configuration;

	private static DatabaseManager DatabaseManager;

	private static Encoding DefaultEncoding;

	private static TcpConnectionManager ConnectionManager;

	private static MusSocket MusSocket;

	private static Game Game;

    // public static string Titulo = Console.Title;

    public static string Version => "Holograph Emulator R59 +";

    public static void Initialize()
    {
        Console.Title = Version;
        DefaultEncoding = Encoding.Default;
        Logging = new Logging();
        Logging.MinimumLogLevel = LogLevel.Debug;
        GetLogging().WriteLine("HOLOGRAPH EMULATOR");
        GetLogging().WriteLine("FREE EMULATOR BASED ON HABBO HOTEL EMULATOR");
        GetLogging().WriteLine("COPYRIGHT (C) 2007-2010 BY HOLOGRAPH TEAM");
        GetLogging().WriteLine("");
        GetLogging().WriteLine("VERSION:");
        GetLogging().WriteLine(" CORE: C#.NET");
        GetLogging().WriteLine(" CLIENT: R59 +");
        GetLogging().WriteLine(" STABLE CLIENT: R59+");
        GetLogging().WriteLine("Marlon Colhado & Shine-Away's R59 Emulator (Holograph BR)");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[.NET] » Starting Holograph Emulator");
        Console.WriteLine("[.NET] » Phase 1 Complete - www.holograph-emulator.com");
        try
        {
            Configuration = new ConfigurationData("mysql.ini");
            if (GetConfig().data["db.password"].Length == 0)
            {
                throw new Exception("For security reasons, your MySQL password cannot be left blank. Please change your password to start the server.");
            }
            if (GetConfig().data["db.password"] == "change-me")
            {
                throw new Exception("Your MySQL password cannot be 'change-me'.\nPlease change your password to start the server.");
            }
            DatabaseServer dbServer = new DatabaseServer(GetConfig().data["db.hostname"], uint.Parse(GetConfig().data["db.port"]), GetConfig().data["db.username"], GetConfig().data["db.password"]);
            Database db = new Database(GetConfig().data["db.name"], uint.Parse(GetConfig().data["db.pool.minsize"]), uint.Parse(GetConfig().data["db.pool.maxsize"]));
            DatabaseManager = new DatabaseManager(dbServer, db);
            MusSocket = new MusSocket(GetConfig().data["mus.tcp.bindip"], int.Parse(GetConfig().data["mus.tcp.port"]), GetConfig().data["mus.tcp.allowedaddr"].Split(';'), 20);
            Game = new Game();
            ConnectionManager = new TcpConnectionManager(GetConfig().data["game.tcp.bindip"], int.Parse(GetConfig().data["game.tcp.port"]), int.Parse(GetConfig().data["game.tcp.conlimit"]));
            ConnectionManager.GetListener().Start();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Holograph Emulator started. Status: active");
            Console.Beep();
            MainTimer();
        }
        catch (KeyNotFoundException)
        {
            Logging.WriteLine("Please check your configuration file - some values seem to be missing.", LogLevel.Error);
            Logging.WriteLine("Press any key to shut down ..", LogLevel.Error);
            Console.ReadKey(intercept: true);
            Destroy();
        }
        catch (InvalidOperationException ex2)
        {
            Logging.WriteLine("Failed to initialize ZeroEmulator " + ex2.Message, LogLevel.Error);
            Logging.WriteLine("Press any key to shut down ...", LogLevel.Error);
            Console.ReadKey(intercept: true);
            Destroy();
        }
    }


    public static bool EnumToBool(string Enum)
	{
		if (Enum == "1")
		{
			return true;
		}
		return false;
	}

	public static string BoolToEnum(bool Bool)
	{
		if (Bool)
		{
			return "1";
		}
		return "0";
	}

	public static int GetRandomNumber(int Min, int Max)
	{
		RandomBase Quick = new Quick();
		return Quick.Next(Min, Max);
	}

	public static double GetUnixTimestamp()
	{
		return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
	}

	public static void MainTimer()
	{
		Timer aTimer = new Timer();
		aTimer.Elapsed += OnTimedEvent;
		aTimer.Interval = 30000.0;
		aTimer.Enabled = true;
		Console.ReadLine();
		GC.KeepAlive(aTimer);
	}

	private static void OnTimedEvent(object source, ElapsedEventArgs e)
	{
		using DatabaseClient dbClient = GetDatabase().GetClient();
		dbClient.ExecuteQuery("OPTIMIZE TABLE `achievements`, `bans`, `bans_appeals`, `bots`, `bots_responses`, `bots_speech`, `catalog_items`, `catalog_marketplace_offers`, `catalog_pages`, `chatlogs`, `credit_vouchers`, `ecotron_rewards`, `external_texts`, `external_variables`, `furniture`, `fuserights`, `fuserights_subs`, `help_subjects`, `help_topics`, `homes`, `homes_catalog`, `homes_categories`, `homes_inventory`, `homes_items`, `ipn_requests`, `item_id_generator`, `messenger_friendships`, `messenger_requests`, `moderation_forum_replies`, `moderation_forum_threads`, `moderation_presets`, `moderation_tickets`, `navigator_flatcats`, `navigator_pubcats`, `navigator_publics`, `notes`, `ranks`, `rooms`, `room_ads`, `room_items`, `room_items_moodlight`, `room_models`, `room_rights`, `server_status`, `site_app_form`, `site_app_openings`, `site_config`, `site_cron`, `site_hotcampaigns`, `site_minimail`, `site_navi`, `site_news`, `site_news_categories`, `tele_links`, `users`, `user_achievements`, `user_badges`, `user_effects`, `user_favorites` ,  `user_ignores` ,  `user_info` ,  `user_items` ,  `user_pets` ,  `user_presents` ,  `user_roomvisits` ,  `user_subscriptions` ,  `user_tags` ,  `user_wardrobe` ,`user_vip_wardrobe`,  `vip_items`");
		dbClient.ExecuteQuery("REPAIR TABLE `achievements`, `bans`, `bans_appeals`, `bots`, `bots_responses`, `bots_speech`, `catalog_items`, `catalog_marketplace_offers`, `catalog_pages`, `chatlogs`, `credit_vouchers`, `ecotron_rewards`, `external_texts`, `external_variables`, `furniture`, `fuserights`, `fuserights_subs`, `help_subjects`, `help_topics`, `homes`, `homes_catalog`, `homes_categories`, `homes_inventory`, `homes_items`, `ipn_requests`, `item_id_generator`, `messenger_friendships`, `messenger_requests`, `moderation_forum_replies`, `moderation_forum_threads`, `moderation_presets`, `moderation_tickets`, `navigator_flatcats`, `navigator_pubcats`, `navigator_publics`, `notes`, `ranks`, `rooms`, `room_ads`, `room_items`, `room_items_moodlight`, `room_models`, `room_rights`, `server_status`, `site_app_form`, `site_app_openings`, `site_config`, `site_cron`, `site_hotcampaigns`, `site_minimail`, `site_navi`, `site_news`, `site_news_categories`, `tele_links`, `users`, `user_achievements`, `user_badges`, `user_effects`, `user_favorites` ,  `user_ignores` ,  `user_info` ,  `user_items` ,  `user_pets` ,  `user_presents` ,  `user_roomvisits` ,  `user_subscriptions` ,  `user_tags` ,  `user_wardrobe` , `user_vip_wardrobe`,  `vip_items`");
		dbClient.ExecuteQuery("ANALYZE TABLE `achievements`, `bans`, `bans_appeals`, `bots`, `bots_responses`, `bots_speech`, `catalog_items`, `catalog_marketplace_offers`, `catalog_pages`, `chatlogs`, `credit_vouchers`, `ecotron_rewards`, `external_texts`, `external_variables`, `furniture`, `fuserights`, `fuserights_subs`, `help_subjects`, `help_topics`, `homes`, `homes_catalog`, `homes_categories`, `homes_inventory`, `homes_items`, `ipn_requests`, `item_id_generator`, `messenger_friendships`, `messenger_requests`, `moderation_forum_replies`, `moderation_forum_threads`, `moderation_presets`, `moderation_tickets`, `navigator_flatcats`, `navigator_pubcats`, `navigator_publics`, `notes`, `ranks`, `rooms`, `room_ads`, `room_items`, `room_items_moodlight`, `room_models`, `room_rights`, `server_status`, `site_app_form`, `site_app_openings`, `site_config`, `site_cron`, `site_hotcampaigns`, `site_minimail`, `site_navi`, `site_news`, `site_news_categories`, `tele_links`, `users`, `user_achievements`, `user_badges`, `user_effects`, `user_favorites` ,  `user_ignores` ,  `user_info` ,  `user_items` ,  `user_pets` ,  `user_presents` ,  `user_roomvisits` ,  `user_subscriptions` ,  `user_tags` ,  `user_wardrobe` , `user_vip_wardrobe`,  `vip_items`");
		dbClient.ExecuteQuery("CHECK TABLE `achievements`, `bans`, `bans_appeals`, `bots`, `bots_responses`, `bots_speech`, `catalog_items`, `catalog_marketplace_offers`, `catalog_pages`, `chatlogs`, `credit_vouchers`, `ecotron_rewards`, `external_texts`, `external_variables`, `furniture`, `fuserights`, `fuserights_subs`, `help_subjects`, `help_topics`, `homes`, `homes_catalog`, `homes_categories`, `homes_inventory`, `homes_items`, `ipn_requests`, `item_id_generator`, `messenger_friendships`, `messenger_requests`, `moderation_forum_replies`, `moderation_forum_threads`, `moderation_presets`, `moderation_tickets`, `navigator_flatcats`, `navigator_pubcats`, `navigator_publics`, `notes`, `ranks`, `rooms`, `room_ads`, `room_items`, `room_items_moodlight`, `room_models`, `room_rights`, `server_status`, `site_app_form`, `site_app_openings`, `site_config`, `site_cron`, `site_hotcampaigns`, `site_minimail`, `site_navi`, `site_news`, `site_news_categories`, `tele_links`, `users`, `user_achievements`, `user_badges`, `user_effects`, `user_favorites` ,  `user_ignores` ,  `user_info` ,  `user_items` ,  `user_pets` ,  `user_presents` ,  `user_roomvisits` ,  `user_subscriptions` ,  `user_tags` ,  `user_wardrobe` , `user_vip_wardrobe`,  `vip_items`");
		dbClient.ExecuteQuery("TRUNCATE TABLE  `messenger_requests`");
	}

	public static string FilterInjectionChars(string Input)
	{
		return FilterInjectionChars(Input, AllowLinebreaks: false);
	}

	public static string FilterInjectionChars(string Input, bool AllowLinebreaks)
	{
		Input = Input.Replace(Convert.ToChar(1), ' ');
		Input = Input.Replace(Convert.ToChar(2), ' ');
		Input = Input.Replace(Convert.ToChar(3), ' ');
		Input = Input.Replace(Convert.ToChar(9), ' ');
		if (!AllowLinebreaks)
		{
			Input = Input.Replace(Convert.ToChar(13), ' ');
		}
		return Input;
	}

	public static bool IsValidAlphaNumeric(string inputStr)
	{
		if (string.IsNullOrEmpty(inputStr))
		{
			return false;
		}
		for (int i = 0; i < inputStr.Length; i++)
		{
			if (!char.IsLetter(inputStr[i]) && !char.IsNumber(inputStr[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static ConfigurationData GetConfig()
	{
		return Configuration;
	}

	public static Logging GetLogging()
	{
		return Logging;
	}

	public static DatabaseManager GetDatabase()
	{
		return DatabaseManager;
	}

	public static Encoding GetDefaultEncoding()
	{
		return DefaultEncoding;
	}

	public static TcpConnectionManager GetConnectionManager()
	{
		return ConnectionManager;
	}

	public static Game GetGame()
	{
		return Game;
	}

    public static void Destroy()
    {
        GetLogging().WriteLine("Shutting down the ZeroEmulator");
        if (GetGame() != null)
        {
            GetGame().Destroy();
            Game = null;
        }
        if (GetConnectionManager() != null)
        {
            GetLogging().WriteLine("Shutting down Connection Manager");
            GetConnectionManager().GetListener().Stop();
            GetConnectionManager().GetListener().Destroy();
            GetConnectionManager().DestroyManager();
            ConnectionManager = null;
        }
        if (GetDatabase() != null)
        {
            GetLogging().WriteLine("Shutting down Database Manager");
            GetDatabase().StopClientMonitor();
            GetDatabase().DestroyClients();
            GetDatabase().DestroyDatabaseManager();
            DatabaseManager = null;
        }
        Logging.WriteLine("Shutdown complete. Closing...");
        Environment.Exit(0);
    }

}
