using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Ttf
{
    partial class OpenFont
    {
        internal struct PointF
        {
            public PointF(float x,float y)
            {
                X = x;
                Y = y;
            }
            public float X { get; set; }
            public float Y { get; set; }
        }
        internal struct Rect16
        {
            public Rect16(UInt16 x1,UInt16 y1,UInt16 x2,UInt16 y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
            public UInt16 X1 { get; set; }
            public UInt16 Y1 { get; set; }
            public UInt16 X2 { get; set; }
            public UInt16 Y2 { get; set; }
        }
        internal struct SRect16
        {
            public SRect16(Int16 x1, Int16 y1, Int16 x2, Int16 y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
            public Int16 X1 { get; set; }
            public Int16 Y1 { get; set; }
            public Int16 X2 { get; set; }
            public Int16 Y2 { get; set; }
        }
        internal abstract class Table
        {
            Header _header;
            internal Header Header { get { return _header; } set { _header = value; } }

            internal static SByte FromBE(SByte value)
            {
                return value;
            }
            internal static Byte FromBE(Byte value)
            {
                return value;
            }
            internal static UInt16 FromBE(UInt16 value)
            {
                if (BitConverter.IsLittleEndian)
                {
                    return (UInt16)(((value & 0xFF00) >> 8) | ((value & 0x00FF) << 8));
                }
                return value;
            }
            internal static Int16 FromBE(Int16 value)
            {
                return unchecked((Int16)FromBE(unchecked((UInt16)value)));
            }
            internal static UInt32 FromBE(UInt32 value)
            {
                if (BitConverter.IsLittleEndian)
                {
                    UInt32 tmp = ((value << 8) & 0xFF00FF00) | ((value >> 8) & 0xFF00FF);
                    return (UInt32)((tmp << 16) | (tmp >> 16));
                }
                return value;
            }
            internal static Int32 FromBE(Int32 value)
            {
                return unchecked((Int32)FromBE(unchecked((UInt32)value)));
            }
            internal static SByte ReadSByte(BinaryReader reader)
            {
                return reader.ReadSByte();
            }
            internal static Byte ReadByte(BinaryReader reader)
            {
                return reader.ReadByte();
            }
            internal static Int16 ReadInt16(BinaryReader reader)
            {
                return FromBE(reader.ReadInt16());
            }
            internal static Int16[] ReadInt16Array(BinaryReader reader, int count)
            {
                Int16[] result = new Int16[count];
                for(int i = 0;i<count;++i)
                {
                    result[i] = ReadInt16(reader);
                }
                return result;
            }
            internal static UInt16 ReadUInt16(BinaryReader reader)
            {
                return FromBE(reader.ReadUInt16());
            }
            internal static UInt16[] ReadUInt16Array(BinaryReader reader, int count)
            {
                UInt16[] result = new UInt16[count];
                for (int i = 0; i < count; ++i)
                {
                    result[i] = ReadUInt16(reader);
                }
                return result;
            }
            internal static UInt32 ReadUInt24(BinaryReader reader)
            {
                return (UInt32)(reader.ReadByte() << 16 | FromBE(reader.ReadUInt16()));
            }
            internal static Int32 ReadInt32(BinaryReader reader)
            {
                return FromBE(reader.ReadInt32());
            }
            internal static UInt32 ReadUInt32(BinaryReader reader)
            {
                return FromBE(reader.ReadUInt32());
            }
            internal static float ReadF214(BinaryReader reader)
            {
                return ((float)ReadInt16(reader)) / (1 << 14); 
            }

            internal static SRect16 ReadSRect16(BinaryReader reader)
            {
                SRect16 result=default(SRect16);
                result.X1 = ReadInt16(reader);
                result.Y1 = ReadInt16(reader);
                result.X2 = ReadInt16(reader);
                result.Y2 = ReadInt16(reader);
                return result; 
            }
        }
        internal class Header
        {
            UInt32 _id; // stored in big endian
            UInt32 _checksum;
            UInt32 _offset;
            UInt32 _size;

            public UInt32 Id { get { return Table.FromBE(_id); } }
            public string Tag { get { return Encoding.ASCII.GetString(BitConverter.GetBytes(_id)); } }

            public UInt32 Checksum { get { return _checksum; } }
            public UInt32 Offset { get { return _offset; } }
            public UInt32 Size { get { return _size; } }

            
            public void Fill(BinaryReader reader)
            {
                _id = reader.ReadUInt32();
                _checksum = Table.ReadUInt32(reader);
                _offset = Table.ReadUInt32(reader);
                _size = Table.ReadUInt32(reader);
            }
        }
    }
}
