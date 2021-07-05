using System;
using System.IO;

namespace Ttf
{
    partial class OpenFont
    {
        private class LocaTable : Table
        {
            UInt32[] _locations;

            public UInt32[] Locations { get { return _locations; } }

            public void Fill(Header header,bool wide, int glyphsCount, BinaryReader reader)
            {
                Header = header;
                _locations = new UInt32[glyphsCount + 1];
                if(wide)
                {
                    for(int i = 0; i<glyphsCount;++i)
                    {
                        _locations[i] = ReadUInt32(reader);
                    }
                } else
                {
                    for (int i = 0; i < glyphsCount;++i)
                    {
                        _locations[i] = (UInt16)(ReadUInt16(reader) << 1);
                    }
                }
                _locations[glyphsCount] = 0;
            }
        }
    }
}
