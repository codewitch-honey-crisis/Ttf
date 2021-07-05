using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ttf
{
    partial class OpenFont
    {
        Glyph[] _glyphs;
        CharacterMap[] _charMaps = null;
        IList<CharMapFormat14> _charMap14List;
        IDictionary<Int32, UInt16> _codepointToGlyphs = new Dictionary<Int32, UInt16>();
        void CollectUnicode(List<uint> unicodes)
        {
            for (int i = 0; i < _charMaps.Length; ++i)
            {
                _charMaps[i].CollectUnicodeChars(unicodes);
            }
        }
        internal UInt16 GetGlyphIndex(int codepoint, int nextCodepoint, out bool skipNextCodepoint)
        {
            // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
            // "character codes that do not correspond to any glyph in the font should be mapped to glyph index 0."

            skipNextCodepoint = false; //default

            if (!_codepointToGlyphs.TryGetValue(codepoint, out ushort found))
            {
                for (int i = 0; i < _charMaps.Length; ++i)
                {
                    CharacterMap cmap = _charMaps[i];



                    if (found == 0)
                    {
                        found = cmap.GetGlyphIndex(codepoint);
                    }
                    else if (cmap.PlatformId == 3 && cmap.EncodingId == 1)
                    {
                        //...When building a Unicode font for Windows, 
                        // the platform ID should be 3 and the encoding ID should be 1
                        ushort glyphIndex = cmap.GetGlyphIndex(codepoint); //glyphIndex=> gid
                        if (glyphIndex != 0)
                        {
                            found = glyphIndex;
                        }
                    }
                }
                _codepointToGlyphs[codepoint] = found;
            }

            // If there is a second codepoint, we are asked whether this is an UVS sequence
            //  -> if true, return a glyph ID
            //  -> otherwise, return 0
            if (nextCodepoint > 0 && _charMap14List != null)
            {
                foreach (CharMapFormat14 cmap14 in _charMap14List)
                {
                    ushort glyphIndex = cmap14.CharacterPairToGlyphIndex(codepoint, found, nextCodepoint);
                    if (glyphIndex > 0)
                    {
                        skipNextCodepoint = true;
                        return glyphIndex;
                    }
                }
            }
            return found;
        }
        internal Glyph GetGlyph(int codepoint, int nextCodepoint, out bool skipNextCodepoint)
        {
            return Glyphs[GetGlyphIndex(codepoint, nextCodepoint, out skipNextCodepoint)];
        }
        internal Glyph[] Glyphs { get { return _glyphs; } }
        public static OpenFont Read(Stream stream)
        {
            OpenFont result = new OpenFont();
            BinaryReader reader = new BinaryReader(stream);
            Byte[] fourCC = reader.ReadBytes(4);
            if(fourCC.Length!=4)
            {
                throw new InvalidDataException("Unexpected end of stream");
            }
            UInt16 tableCount = Table.ReadUInt16(reader);
            UInt16 searchRange = Table.ReadUInt16(reader);
            UInt16 entrySelector = Table.ReadUInt16(reader);
            UInt16 rangeShift = Table.ReadUInt16(reader);

            HeadTable head = null;
            MaxpTable maxp = null;
            LocaTable loca = null;
            GlyfTable glyf = null;
            CMapTable cmap = null; 
            long tableStartPos = reader.BaseStream.Seek(0, SeekOrigin.Current);
            long tmpPos;
            while(
                head==null ||
                maxp==null ||
                loca==null ||
                glyf==null ||
                cmap==null
                )
            {
                reader.BaseStream.Seek(tableStartPos, SeekOrigin.Begin);
                for(int i =0;i<tableCount;++i)
                {
                    Header header = new Header();
                    header.Fill(reader);
                    tmpPos = reader.BaseStream.Seek(0, SeekOrigin.Current);
                    reader.BaseStream.Seek(header.Offset,SeekOrigin.Begin);
                    switch (header.Id)
                    {
                        case 0x68656164: // head
                            head = new HeadTable();
                            head.Fill(header, reader);
                            break;
                        case 0x6D617870: //maxp
                            maxp = new MaxpTable();
                            maxp.Fill(header, reader);
                            break;
                        case 0x6C6F6361: // loca
                            if(head!=null && maxp!=null)
                            {
                                loca = new LocaTable();
                                loca.Fill(header, head.IndexToLocFormat != 0, maxp.NumGlyphs, reader);
                            }
                            break;
                        case 0x676C7966: // glyf
                            if(loca!=null)
                            {
                                glyf = new GlyfTable();
                                glyf.Fill(header, loca.Locations, reader);
                            }
                            break;
                        case 0x636D6170: // cmap
                            cmap = new CMapTable();
                            cmap.Fill(header, reader);
                            break;

                    }
                    reader.BaseStream.Seek(tmpPos, SeekOrigin.Begin);
                    
                }

            }
            result._glyphs = glyf.Glyphs;
            result._charMaps = cmap.CharacterMaps;
            result._charMap14List = cmap.CharacterMap14List;
            result._codepointToGlyphs = cmap.CodepointToGlyphs;
            
            return result;
        }
    }
}
