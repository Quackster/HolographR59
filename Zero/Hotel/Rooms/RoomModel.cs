using System;
using System.Globalization;
using System.Text;
using Zero.Messages;
using Zero.Util;

namespace Zero.Hotel.Rooms;

internal class RoomModel
{
    public string Name;

    public int DoorX;

    public int DoorY;

    public double DoorZ;

    public int DoorOrientation;

    public string Heightmap;

    public SquareState[,] SqState;

    public double[,] SqFloorHeight;

    public int[,] SqSeatRot;

    public int MapSizeX;

    public int MapSizeY;

    public string StaticFurniMap;

    public bool ClubOnly;

    public RoomModel(string Name, int DoorX, int DoorY, double DoorZ, int DoorOrientation, string Heightmap, string StaticFurniMap, bool ClubOnly)
    {
        this.Name = Name;
        this.DoorX = DoorX;
        this.DoorY = DoorY;
        this.DoorZ = DoorZ;
        this.DoorOrientation = DoorOrientation;
        this.Heightmap = Heightmap.ToLower();
        this.StaticFurniMap = StaticFurniMap;
        string[] tmpHeightmap = Heightmap.Split(Convert.ToChar(13));
        MapSizeX = tmpHeightmap[0].Length;
        MapSizeY = tmpHeightmap.Length;
        this.ClubOnly = ClubOnly;
        SqState = new SquareState[MapSizeX, MapSizeY];
        SqFloorHeight = new double[MapSizeX, MapSizeY];
        SqSeatRot = new int[MapSizeX, MapSizeY];
        for (int y = 0; y < MapSizeY; y++)
        {
            if (y > 0)
            {
                tmpHeightmap[y] = tmpHeightmap[y].Substring(1);
            }
            for (int x = 0; x < MapSizeX; x++)
            {
                string Square = tmpHeightmap[y].Substring(x, 1).Trim().ToLower();
                if (Square == "x")
                {
                    SqState[x, y] = SquareState.BLOCKED;
                }
                else if (isNumeric(Square, NumberStyles.Integer))
                {
                    SqState[x, y] = SquareState.OPEN;
                    SqFloorHeight[x, y] = double.Parse(Square);
                }
            }
        }
        SqFloorHeight[DoorX, DoorY] = DoorZ;
        int pointer = 0;
        int num = OldEncoding.decodeVL64(StaticFurniMap);
        pointer += OldEncoding.encodeVL64(num).Length;
        for (int i = 0; i < num; i++)
        {
            string thisss = StaticFurniMap.Substring(pointer);
            int junk = OldEncoding.decodeVL64(StaticFurniMap.Substring(pointer));
            pointer += OldEncoding.encodeVL64(junk).Length;
            string junk2 = StaticFurniMap.Substring(pointer, 1);
            pointer++;
            int junk3 = int.Parse(StaticFurniMap.Substring(pointer).Split(Convert.ToChar(2))[0]);
            pointer += StaticFurniMap.Substring(pointer).Split(Convert.ToChar(2))[0].Length;
            pointer++;
            string name = StaticFurniMap.Substring(pointer).Split(Convert.ToChar(2))[0];
            pointer += StaticFurniMap.Substring(pointer).Split(Convert.ToChar(2))[0].Length;
            pointer++;
            int x = OldEncoding.decodeVL64(StaticFurniMap.Substring(pointer));
            pointer += OldEncoding.encodeVL64(x).Length;
            int y = OldEncoding.decodeVL64(StaticFurniMap.Substring(pointer));
            pointer += OldEncoding.encodeVL64(y).Length;
            int junk4 = OldEncoding.decodeVL64(StaticFurniMap.Substring(pointer));
            pointer += OldEncoding.encodeVL64(junk4).Length;
            int junk5 = OldEncoding.decodeVL64(StaticFurniMap.Substring(pointer));
            pointer += OldEncoding.encodeVL64(junk5).Length;
            SqState[x, y] = SquareState.BLOCKED;
            if (name.Contains("bench") || name.Contains("chair") || name.Contains("stool") || name.Contains("seat") || name.Contains("sofa"))
            {
                SqState[x, y] = SquareState.SEAT;
                SqSeatRot[x, y] = junk5;
            }
        }
    }

    public bool isNumeric(string val, NumberStyles NumberStyle)
    {
        double result;
        return double.TryParse(val, NumberStyle, CultureInfo.CurrentCulture, out result);
    }

    public ServerMessage SerializeHeightmap()
    {
        StringBuilder HeightMap = new StringBuilder();
        string[] array = Heightmap.Split("\r\n".ToCharArray());
        foreach (string MapBit in array)
        {
            if (!(MapBit == ""))
            {
                HeightMap.Append(MapBit);
                HeightMap.Append(Convert.ToChar(13));
            }
        }
        ServerMessage Message = new ServerMessage(31u);
        Message.AppendStringWithBreak(HeightMap.ToString());
        return Message;
    }

    public ServerMessage SerializeRelativeHeightmap()
    {
        ServerMessage Message = new ServerMessage(470u);
        string[] tmpHeightmap = Heightmap.Split(Convert.ToChar(13));
        for (int y = 0; y < MapSizeY; y++)
        {
            if (y > 0)
            {
                tmpHeightmap[y] = tmpHeightmap[y].Substring(1);
            }
            for (int x = 0; x < MapSizeX; x++)
            {
                string Square = tmpHeightmap[y].Substring(x, 1).Trim().ToLower();
                if (DoorX == x && DoorY == y)
                {
                    Square = string.Concat((int)DoorZ);
                }
                Message.AppendString(Square);
            }
            Message.AppendString(string.Concat(Convert.ToChar(13)));
        }
        return Message;
    }
}
