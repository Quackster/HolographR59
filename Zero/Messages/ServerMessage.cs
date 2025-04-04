using System;
using System.Collections.Generic;
using System.Text;
using Zero.Util;

namespace Zero.Messages;

internal class ServerMessage
{
    private uint MessageId;

    private List<byte> Body;

    public uint Id => MessageId;

    public string Header => HolographEnvironment.GetDefaultEncoding().GetString(Base64Encoding.Encodeuint(MessageId, 2));

    public int Length => Body.Count;

    public void appendChar(int charId)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Convert.ToChar(charId));
    }

    public ServerMessage()
    {
    }

    public ServerMessage(uint _MessageId)
    {
        Init(_MessageId);
    }

    public override string ToString()
    {
        return Header + HolographEnvironment.GetDefaultEncoding().GetString(Body.ToArray());
    }

    public string ToBodyString()
    {
        return HolographEnvironment.GetDefaultEncoding().GetString(Body.ToArray());
    }

    public void Clear()
    {
        Body.Clear();
    }

    public void Init(uint _MessageId)
    {
        MessageId = _MessageId;
        Body = new List<byte>();
    }

    public void AppendByte(byte b)
    {
        Body.Add(b);
    }

    public void AppendBytes(byte[] Data)
    {
        if (Data != null && Data.Length != 0)
        {
            Body.AddRange(Data);
        }
    }

    public void AppendString(string s, Encoding encoding)
    {
        if (s != null && s.Length != 0)
        {
            AppendBytes(encoding.GetBytes(s));
        }
    }

    public void AppendString(string s)
    {
        AppendString(s, HolographEnvironment.GetDefaultEncoding());
    }

    public void AppendStringWithBreak(string s)
    {
        AppendStringWithBreak(s, 2);
    }

    public void AppendStringWithBreak(string s, byte BreakChar)
    {
        AppendString(s);
        AppendByte(BreakChar);
    }

    public void AppendInt32(int i)
    {
        AppendBytes(WireEncoding.EncodeInt32(i));
    }

    public void AppendRawInt32(int i)
    {
        AppendString(i.ToString(), Encoding.ASCII);
    }

    public void AppendUInt(uint i)
    {
        AppendInt32((int)i);
    }

    public void AppendRawUInt(uint i)
    {
        AppendRawInt32((int)i);
    }

    public void AppendBoolean(bool Bool)
    {
        if (Bool)
        {
            Body.Add(73);
        }
        else
        {
            Body.Add(72);
        }
    }

    public byte[] GetBytes()
    {
        byte[] Data = new byte[Length + 3];
        byte[] Header = Base64Encoding.Encodeuint(MessageId, 2);
        Data[0] = Header[0];
        Data[1] = Header[1];
        for (int i = 0; i < Length; i++)
        {
            Data[i + 2] = Body[i];
        }
        Data[Data.Length - 1] = 1;
        return Data;
    }
}
