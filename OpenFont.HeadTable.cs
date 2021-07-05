using System;
using System.IO;

namespace Ttf
{
    partial class OpenFont
    {
        private class HeadTable : Table
        {
            UInt16 _majorVersion;
            UInt16 _minorVersion;
            UInt32 _fontRevision;
            UInt32 _checkSumAdjustment;
            UInt32 _magicNumber;// 	Set to 0x5F0F3CF5.
            UInt16 _flags;
            UInt16 _unitsPerEm;
            Byte[] _created;
            Byte[] _modified;
            Int16 _xMin;
            Int16 _yMin;
            Int16 _xMax;
            Int16 _yMax;
            UInt16 _macStyle;
            UInt16 _lowestRecPPEM;
            Int16 _fontDirectionHint;
            Int16 _indexToLocFormat;
            Int16 _glyphDataFormat;

            //Type 	    Name 	        Description
            public UInt16 MajorVersion { get { return _majorVersion; } }// 	Major version number of the font header table — set to 1.
            public UInt16 MinorVersion { get { return _minorVersion; } }// 	Minor version number of the font header table — set to 0.
            public UInt32 FontRevision { get { return _fontRevision; } } // 	Set by font manufacturer.
            public UInt32 CheckSumAdjustment { get { return _checkSumAdjustment; } } // 	To compute: set it to 0, sum the entire font as uint32, then store 0xB1B0AFBA - sum.
                                                                                     //                          If the font is used as a component in a font collection file, 
                                                                                     //                          the value of this field will be invalidated by changes to the file structure and font table directory, and must be ignored.
            public UInt32 MagicNumber { get { return _magicNumber; } } // 	Set to 0x5F0F3CF5.
            public UInt16 Flags { get { return _flags; } } // 	        Bit 0: Baseline for font at y=0;

            //                          Bit 1: Left sidebearing point at x=0 (relevant only for TrueType rasterizers) — see the note below regarding variable fonts;

            //                          Bit 2: Instructions may depend on point size;

            //                          Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear;

            //                          Bit 4: Instructions may alter advance width (the advance widths might not scale linearly);

            //                          Bit 5: This bit is not used in OpenType, and should not be set in order to ensure compatible behavior on all platforms. If set, it may result in different behavior for vertical layout in some platforms. (See Apple’s specification for details regarding behavior in Apple platforms.)

            //                          Bits 6–10: These bits are not used in Opentype and should always be cleared. (See Apple’s specification for details regarding legacy used in Apple platforms.)

            //                          Bit 11: Font data is “lossless” as a result of having been subjected to optimizing transformation and/or compression (such as e.g. compression mechanisms defined by ISO/IEC 14496-18, MicroType Express, WOFF 2.0 or similar) where the original font functionality and features are retained but the binary compatibility between input and output font files is not guaranteed. As a result of the applied transform, the DSIG table may also be invalidated.

            //                          Bit 12: Font converted (produce compatible metrics)

            //                          Bit 13: Font optimized for ClearType™. Note, fonts that rely on embedded bitmaps (EBDT) for rendering should not be considered optimized for ClearType, and therefore should keep this bit cleared.

            //                          Bit 14: Last Resort font. If set, indicates that the glyphs encoded in the 'cmap' subtables are simply generic symbolic representations of code point ranges and don’t truly represent support for those code points. If unset, indicates that the glyphs encoded in the 'cmap' subtables represent proper support for those code points.

            //                          Bit 15: Reserved, set to 0
            public UInt16 UnitsPerEm { get { return _unitsPerEm; } } // 	Set to a value from 16 to 16384. Any value in this range is valid.
                                                                     //                          In fonts that have TrueType outlines, a power of 2 is recommended as this allows performance optimizations in some rasterizers.
                                                                     //LONGDATETIME 	created 	Number of seconds since 12:00 midnight that started January 1st 1904 in GMT/UTC time zone. 64-bit integer
                                                                     // can't use uint64_t since some platforms don't support it
            public Byte[] Created { get { return _created; } }
            //LONGDATETIME 	modified 	Number of seconds since 12:00 midnight that started January 1st 1904 in GMT/UTC time zone. 64-bit integer
            public Byte[] Modified { get { return _modified; } }
            public Int16 XMin { get { return _xMin; } } // 	        For all glyph bounding boxes.
            public Int16 YMin { get { return _yMin; } } // 	        For all glyph bounding boxes.
            public Int16 XMax { get { return _xMax; } } // 	        For all glyph bounding boxes.
            public Int16 YMax { get { return _yMax; } } // 	        For all glyph bounding boxes.
            public UInt16 MacStyle { get { return _macStyle; } } // 	    Bit 0: Bold (if set to 1);
                                                                 //                          Bit 1: Italic (if set to 1)
                                                                 //                          Bit 2: Underline (if set to 1)
                                                                 //                          Bit 3: Outline (if set to 1)
                                                                 //                          Bit 4: Shadow (if set to 1)
                                                                 //                          Bit 5: Condensed (if set to 1)
                                                                 //                          Bit 6: Extended (if set to 1)
                                                                 //                          Bits 7–15: Reserved (set to 0).
            public UInt16 LowestRecPPEM { get { return _lowestRecPPEM; } } // 	Smallest readable size in pixels.
            public Int16 FontDirectionHint { get { return _fontDirectionHint; } } // 	Deprecated (Set to 2).
                                                                                  //                          0: Fully mixed directional glyphs;
                                                                                  //                          1: Only strongly left to right;
                                                                                  //                          2: Like 1 but also contains neutrals;
                                                                                  //                          -1: Only strongly right to left;
                                                                                  //                          -2: Like -1 but also contains neutrals.

            //(A neutral character has no inherent directionality; it is not a character with zero (0) width. Spaces and punctuation are examples of neutral characters. Non-neutral characters are those with inherent directionality. For example, Roman letters (left-to-right) and Arabic letters (right-to-left) have directionality. In a “normal” Roman font where spaces and punctuation are present, the font direction hints should be set to two (2).)
            public Int16 IndexToLocFormat { get { return _indexToLocFormat; } } // 	0 for short offsets (Offset16), 1 for long (Offset32).
            public Int16 GlyphDataFormat { get { return _glyphDataFormat; } } // 	0 for current format.
            public void Fill(Header header, BinaryReader reader)
            {
                Header = header;
                _majorVersion = ReadUInt16(reader);
                _minorVersion = ReadUInt16(reader);
                _fontRevision = ReadUInt32(reader);
                _checkSumAdjustment = ReadUInt32(reader);
                _magicNumber = ReadUInt32(reader);
                if(0x5F0F3CF5!=_magicNumber)
                {
                    throw new InvalidDataException("The font file has a bad table");
                }
                _flags = ReadUInt16(reader);
                _unitsPerEm = ReadUInt16(reader);
                _created=reader.ReadBytes(8);
                if(8!=_created.Length)
                {
                    throw new InvalidDataException("The font file has a bad table");
                }
                _modified = reader.ReadBytes(8);
                if (8 != _modified.Length)
                {
                    throw new InvalidDataException("The font file has a bad table");
                }
                _xMin = ReadInt16(reader);
                _yMin = ReadInt16(reader);
                _xMax = ReadInt16(reader);
                _yMax = ReadInt16(reader);
                _macStyle = ReadUInt16(reader);
                _lowestRecPPEM = ReadUInt16(reader);
                _fontDirectionHint = ReadInt16(reader);
                _indexToLocFormat = ReadInt16(reader);
                _glyphDataFormat = ReadInt16(reader);
            }
        }
    }
}
