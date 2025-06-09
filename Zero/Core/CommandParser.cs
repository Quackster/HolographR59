using System.Text;
using Zero.Messages;

namespace Zero.Core;

internal class CommandParser
{
    public static void Parse(string Input)
    {
        string[] Params = Input.Split(' ');
        switch (Params[0])
        {
            case "reload_models":
                HolographEnvironment.GetGame().GetRoomManager().LoadModels();
                break;
            case "reload_bans":
                HolographEnvironment.GetGame().GetBanManager().LoadBans();
                break;
            case "nav":
                HolographEnvironment.GetGame().GetNavigator().Initialize();
                HolographEnvironment.GetLogging().WriteLine("Navigator updated!", LogLevel.Warning);
                break;
            case "reload_items":
                HolographEnvironment.GetGame().GetItemManager().LoadItems();
                HolographEnvironment.GetLogging().WriteLine("Please note that changes may not be reflected immediatly in currently loaded rooms.");
                break;
            case "reload_help":
                HolographEnvironment.GetGame().GetHelpTool().LoadCategories();
                HolographEnvironment.GetGame().GetHelpTool().LoadTopics();
                HolographEnvironment.GetLogging().WriteLine("Reloaded help categories and topics successfully.");
                break;
            case "cat":
                HolographEnvironment.GetGame().GetCatalog().Initialize();
                HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(new ServerMessage(441u));
                HolographEnvironment.GetLogging().WriteLine("Catalog updated!.");
                break;
            case "rank":
                HolographEnvironment.GetGame().GetRoleManager().LoadRoles();
                HolographEnvironment.GetGame().GetRoleManager().LoadRights();
                HolographEnvironment.GetLogging().WriteLine("Ranks updated!");
                break;
            case "cls":
            case "clear":
                HolographEnvironment.GetLogging().Clear();
                HolographEnvironment.GetLogging().WriteLine("Clered!", LogLevel.Warning);
                break;
            case "svr":
                HolographEnvironment.GetLogging().WriteLine("Available commands are: cls, close, help, reload_catalog, reload_navigator, reload_roles, reload_help, reload_items, plugins, unload_all_plugins, unload_plugin [name]");
                break;
            case "ha":
                {
                    ServerMessage HotelAlert = new ServerMessage(139u);
                    string Msg = MergeParams(Params, 1);
                    HotelAlert.AppendStringWithBreak(Msg);
                    HolographEnvironment.GetGame().GetClientManager().BroadcastMessage(HotelAlert);
                    HolographEnvironment.GetLogging().WriteLine("Hotel alerted!.", LogLevel.Warning);
                    break;
                }
            case "close":
            case "shutdown":
                HolographEnvironment.Destroy();
                break;
            default:
                HolographEnvironment.GetLogging().WriteLine("Command not found!", LogLevel.Warning);
                break;
        }
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
