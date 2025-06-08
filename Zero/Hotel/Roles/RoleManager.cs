using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Zero.Hotel.Users;
using Zero.Storage;

namespace Zero.Hotel.Roles;

internal class RoleManager
{
    private ConcurrentDictionary<uint, Role> Roles;
    private ConcurrentDictionary<string, uint> Rights;
    private ConcurrentDictionary<string, string> SubRights;

    public RoleManager()
    {
        Roles = new ConcurrentDictionary<uint, Role>();
        Rights = new ConcurrentDictionary<string, uint>();
        SubRights = new ConcurrentDictionary<string, string>();
    }

    public void LoadRoles()
    {
        ClearRoles();
        DataTable Data = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT * FROM ranks ORDER BY id ASC");
        }
        if (Data == null)
        {
            return;
        }
        foreach (DataRow Row in Data.Rows)
        {
            Roles.TryAdd((uint)Row["id"], new Role((uint)Row["id"], (string)Row["name"]));
        }
    }

    public void LoadRights()
    {
        ClearRights();
        DataTable Data = null;
        DataTable SubData = null;
        using (DatabaseClient dbClient = HolographEnvironment.GetDatabase().GetClient())
        {
            Data = dbClient.ReadDataTable("SELECT * FROM fuserights");
            SubData = dbClient.ReadDataTable("SELECT * FROM fuserights_subs");
        }
        if (Data != null)
        {
            foreach (DataRow Row in Data.Rows)
            {
                Rights.TryAdd(Row["fuse"].ToString().ToLower(), (uint)Row["rank"]);
            }
        }
        if (SubData == null)
        {
            return;
        }
        foreach (DataRow Row in SubData.Rows)
        {
            SubRights.TryAdd((string)Row["fuse"], (string)Row["sub"]);
        }
    }

    public bool RankHasRight(uint RankId, string Fuse)
    {
        if (!ContainsRight(Fuse))
        {
            return false;
        }
        uint MinRank = Rights[Fuse];
        if (RankId >= MinRank)
        {
            return true;
        }
        return false;
    }

    public bool SubHasRight(string Sub, string Fuse)
    {
        if (SubRights.ContainsKey(Fuse) && SubRights[Fuse] == Sub)
        {
            return true;
        }
        return false;
    }

    public Role GetRole(uint Id)
    {
        if (!ContainsRole(Id))
        {
            return null;
        }
        return Roles[Id];
    }

    public List<string> GetRightsForHabbo(Habbo Habbo)
    {
        List<string> UserRights = new List<string>();
        UserRights.AddRange(GetRightsForRank(Habbo.Rank));
        foreach (string SubscriptionId in Habbo.GetSubscriptionManager().SubList)
        {
            UserRights.AddRange(GetRightsForSub(SubscriptionId));
        }
        return UserRights;
    }

    public List<string> GetRightsForRank(uint RankId)
    {
        List<string> UserRights = new List<string>();
        foreach (KeyValuePair<string, uint> Data in Rights)
        {
            if (RankId >= Data.Value && !UserRights.Contains(Data.Key))
            {
                UserRights.Add(Data.Key);
            }
        }
        return UserRights;
    }

    public List<string> GetRightsForSub(string SubId)
    {
        List<string> UserRights = new List<string>();

        foreach (KeyValuePair<string, string> Data in SubRights)
        {
            if (Data.Value == SubId)
            {
                UserRights.Add(Data.Key);
            }
        }

        return UserRights;
    }

    public bool ContainsRole(uint Id)
    {
        return Roles.ContainsKey(Id);
    }

    public bool ContainsRight(string Right)
    {
        return Rights.ContainsKey(Right);
    }

    public void ClearRoles()
    {
        Roles.Clear();
    }

    public void ClearRights()
    {
        Rights.Clear();
    }
}
