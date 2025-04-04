using System;

namespace Zero.Hotel.Misc;

internal class AntiMutant
{
    public static bool ValidateLook(string Look, string Gender)
    {
        bool HasHead = false;
        bool HasLegs = false;
        bool HasCheated = false;
        if (Look.Length < 1)
        {
            return false;
        }
        try
        {
            string[] Sets = Look.Split('.');
            if (Sets.Length < 4)
            {
                return false;
            }
            string[] array = Sets;
            foreach (string Set in array)
            {
                string[] Parts = Set.Split('-');
                if (Parts.Length < 3)
                {
                    return false;
                }
                string Name = Parts[0];
                int Type = int.Parse(Parts[1]);
                int Color = int.Parse(Parts[1]);
                string Color2 = Parts[1];
                if (Type <= 0 || Color < 0)
                {
                    return false;
                }
                if (Name.Length != 2)
                {
                    return false;
                }
                if (Name == "hd")
                {
                    HasHead = true;
                }
                if (Name == "lg")
                {
                    HasLegs = true;
                }
                if (Color2 == "999")
                {
                    HasCheated = true;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
        if (!HasHead || !HasLegs || HasCheated || (Gender != "M" && Gender != "F"))
        {
            return false;
        }
        return true;
    }
}
