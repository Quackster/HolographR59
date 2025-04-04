using System;
using System.Text;
using Zero.Util;

namespace Zero.Messages;

internal class ClientMessage
{
    private uint MessageId;

    private byte[] Body;

    private int Pointer;

    public uint Id => MessageId;

    public int Length => Body.Length;

    public int RemainingLength => Body.Length - Pointer;

    public string Header => Encoding.Default.GetString(Base64Encoding.Encodeuint(MessageId, 2));

    public ClientMessage(uint _MessageId, byte[] _Body)
    {
        if (_Body == null)
        {
            _Body = new byte[0];
        }
        MessageId = _MessageId;
        Body = _Body;
        Pointer = 0;
    }

    public override string ToString()
    {
        return Header + HolographEnvironment.GetDefaultEncoding().GetString(Body);
    }

    public void ResetPointer()
    {
        Pointer = 0;
    }

    public void AdvancePointer(int i)
    {
        Pointer += i;
    }

    public string GetBody()
    {
        return Encoding.Default.GetString(Body);
    }

    public byte[] ReadBytes(int Bytes)
    {
        if (Bytes > RemainingLength)
        {
            Bytes = RemainingLength;
        }
        byte[] data = new byte[Bytes];
        for (int i = 0; i < Bytes; i++)
        {
            data[i] = Body[Pointer++];
        }
        return data;
    }

    public byte[] PlainReadBytes(int Bytes)
    {
        if (Bytes > RemainingLength)
        {
            Bytes = RemainingLength;
        }
        byte[] data = new byte[Bytes];
        int x = 0;
        int y = Pointer;
        while (x < Bytes)
        {
            data[x] = Body[y];
            x++;
            y++;
        }
        return data;
    }

    public byte[] ReadFixedValue()
    {
        int len = Base64Encoding.DecodeInt32(ReadBytes(2));
        return ReadBytes(len);
    }

    public bool PopBase64Boolean()
    {
        if (RemainingLength > 0 && Body[Pointer++] == 65)
        {
            return true;
        }
        return false;
    }

    public int PopInt32()
    {
        return Base64Encoding.DecodeInt32(ReadBytes(2));
    }

    public uint PopUInt32()
    {
        return (uint)PopInt32();
    }

    public string PopFixedString()
    {
        return PopFixedString(HolographEnvironment.GetDefaultEncoding());
    }

    public string PopFixedString(Encoding encoding)
    {
        return encoding.GetString(ReadFixedValue()).Replace(Convert.ToChar(1), ' ');
    }

    public int PopFixedInt32()
    {
        int i = 0;
        string s = PopFixedString(Encoding.ASCII);
        int.TryParse(s, out i);
        return i;
    }

    public uint PopFixedUInt32()
    {
        return (uint)PopFixedInt32();
    }

    public bool PopWiredBoolean()
    {
        if (RemainingLength > 0 && Body[Pointer++] == 73)
        {
            return true;
        }
        return false;
    }

    public int PopWiredInt32()
    {
        if (RemainingLength < 1)
        {
            return 0;
        }
        byte[] Data = PlainReadBytes(6);
        int TotalBytes = 0;
        int i = WireEncoding.DecodeInt32(Data, out TotalBytes);
        Pointer += TotalBytes;
        return i;
    }

    public uint PopWiredUInt()
    {
        return (uint)PopWiredInt32();
    }
}
