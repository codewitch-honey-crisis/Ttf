using System;
using System.Collections.Generic;
using System.IO;

namespace Ttf
{
    partial class OpenFont
    {
        private abstract class CharacterMap
        {
            //https://www.microsoft.com/typography/otspec/cmap.htm
            public abstract ushort Format { get; }
            public ushort PlatformId { get; set; }
            public ushort EncodingId { get; set; }

            public ushort CharacterToGlyphIndex(int codepoint)
            {
                return GetGlyphIndex(codepoint);
            }
            public abstract ushort GetGlyphIndex(int codepoint);
            public abstract void CollectUnicodeChars(List<uint> unicodes);
        }
        private class CharMapFormat4 : CharacterMap
        {
            public override ushort Format => 4;

            internal readonly ushort[] _startCode; //Starting character code for each segment
            internal readonly ushort[] _endCode;//Ending character code for each segment, last = 0xFFFF.      
            internal readonly ushort[] _idDelta; //Delta for all character codes in segment
            internal readonly ushort[] _idRangeOffset; //Offset in bytes to glyph indexArray, or 0 (not offset in bytes unit)
            internal readonly ushort[] _glyphIdArray;
            public CharMapFormat4(ushort[] startCode, ushort[] endCode, ushort[] idDelta, ushort[] idRangeOffset, ushort[] glyphIdArray)
            {
                _startCode = startCode;
                _endCode = endCode;
                _idDelta = idDelta;
                _idRangeOffset = idRangeOffset;
                _glyphIdArray = glyphIdArray;
            }

            public override ushort GetGlyphIndex(int codepoint)
            {
                // This lookup table only supports 16-bit codepoints
                if (codepoint > ushort.MaxValue)
                {
                    return 0;
                }

                // https://www.microsoft.com/typography/otspec/cmap.htm#format4
                // "You search for the first endCode that is greater than or equal to the character code you want to map"
                // "The segments are sorted in order of increasing endCode values"
                // -> binary search is valid here
                int i = Array.BinarySearch(_endCode, (ushort)codepoint);
                i = i < 0 ? ~i : i;

                // https://www.microsoft.com/typography/otspec/cmap.htm#format4
                // "If the corresponding startCode is [not] less than or equal to the character code,
                // then [...] the missingGlyph is returned"
                // Index i should never be out of range, because the list ends with a
                // 0xFFFF value. However, we also use this charmap for format 0, which
                // does not have that final endcode, so there is a chance to overflow.
                if (i >= _endCode.Length || _startCode[i] > codepoint)
                {
                    return 0;
                }

                if (_idRangeOffset[i] == 0)
                {
                    //TODO: review 65536 => use bitflags
                    return (ushort)((codepoint + _idDelta[i]) % 65536);
                }
                else
                {
                    //If the idRangeOffset value for the segment is not 0,
                    //the mapping of character codes relies on glyphIdArray.
                    //The character code offset from startCode is added to the idRangeOffset value.
                    //This sum is used as an offset from the current location within idRangeOffset itself to index out the correct glyphIdArray value.
                    //This obscure indexing trick works because glyphIdArray immediately follows idRangeOffset in the font file.
                    //The C expression that yields the glyph index is:

                    //*(idRangeOffset[i]/2
                    //+ (c - startCount[i])
                    //+ &idRangeOffset[i])

                    int offset = _idRangeOffset[i] / 2 + (codepoint - _startCode[i]);
                    // I want to thank Microsoft for this clever pointer trick
                    // TODO: What if the value fetched is inside the _idRangeOffset table?
                    // TODO: e.g. (offset - _idRangeOffset.Length + i < 0)
                    return _glyphIdArray[offset - _idRangeOffset.Length + i];
                }
            }
            public override void CollectUnicodeChars(List<uint> unicodes)
            {
                for (int i = 0; i < _startCode.Length; ++i)
                {
                    uint start = _startCode[i];
                    uint stop = _endCode[i];
                    for (uint u = start; u <= stop; ++u)
                    {
                        unicodes.Add(u);
                    }
                }
            }
        }

        private class CharMapFormat12 : CharacterMap
        {
            public override ushort Format => 12;

            uint[] _startCharCodes, _endCharCodes, _startGlyphIds;
            internal CharMapFormat12(uint[] startCharCodes, uint[] endCharCodes, uint[] startGlyphIds)
            {
                _startCharCodes = startCharCodes;
                _endCharCodes = endCharCodes;
                _startGlyphIds = startGlyphIds;
            }

            public override ushort GetGlyphIndex(int codepoint)
            {
                // https://www.microsoft.com/typography/otspec/cmap.htm#format12
                // "Groups must be sorted by increasing startCharCode."
                // -> binary search is valid here
                int i = Array.BinarySearch(_startCharCodes, (uint)codepoint);
                i = i < 0 ? ~i - 1 : i;

                if (i >= 0 && codepoint <= _endCharCodes[i])
                {
                    return (ushort)(_startGlyphIds[i] + codepoint - _startCharCodes[i]);
                }
                return 0;
            }
            public override void CollectUnicodeChars(List<uint> unicodes)
            {
                for (int i = 0; i < _startCharCodes.Length; ++i)
                {
                    uint start = _startCharCodes[i];
                    uint stop = _endCharCodes[i];
                    for (uint u = start; u <= stop; ++u)
                    {
                        unicodes.Add(u);
                    }
                }
            }
        }

        private class CharMapFormat6 : CharacterMap
        {
            public override ushort Format => 6;

            internal CharMapFormat6(ushort startCode, ushort[] glyphIdArray)
            {
                _glyphIdArray = glyphIdArray;
                _startCode = startCode;
            }

            public override ushort GetGlyphIndex(int codepoint)
            {
                // The firstCode and entryCount values specify a subrange (beginning at firstCode,
                // length = entryCount) within the range of possible character codes.
                // Codes outside of this subrange are mapped to glyph index 0.
                // The offset of the code (from the first code) within this subrange is used as
                // index to the glyphIdArray, which provides the glyph index value.
                int i = codepoint - _startCode;
                return i >= 0 && i < _glyphIdArray.Length ? _glyphIdArray[i] : (ushort)0;
            }


            internal readonly ushort _startCode;
            internal readonly ushort[] _glyphIdArray;
            public override void CollectUnicodeChars(List<uint> unicodes)
            {
                ushort u = _startCode;
                for (uint i = 0; i < _glyphIdArray.Length; ++i)
                {
                    unicodes.Add(u + i);
                }
            }
        }
        //https://www.microsoft.com/typography/otspec/cmap.htm#format14
        // Subtable format 14 specifies the Unicode Variation Sequences(UVSes) supported by the font.
        // A Variation Sequence, according to the Unicode Standard, comprises a base character followed
        // by a variation selector; e.g. <U+82A6, U+E0101>.
        //
        // The subtable partitions the UVSes supported by the font into two categories: “default” and
        // “non-default” UVSes.Given a UVS, if the glyph obtained by looking up the base character of
        // that sequence in the Unicode cmap subtable(i.e.the UCS-4 or the BMP cmap subtable) is the
        // glyph to use for that sequence, then the sequence is a “default” UVS; otherwise it is a
        // “non-default” UVS, and the glyph to use for that sequence is specified in the format 14
        // subtable itself.
        private class CharMapFormat14 : CharacterMap
        {
            public override ushort Format => 14;
            public override ushort GetGlyphIndex(int character) => 0;
            public ushort CharacterPairToGlyphIndex(int codepoint, ushort defaultGlyphIndex, int nextCodepoint)
            {
                // Only check codepoint if nextCodepoint is a variation selector

                if (_variationSelectors.TryGetValue(nextCodepoint, out VariationSelector sel))
                {

                    // If the sequence is a non-default UVS, return the mapped glyph

                    if (sel.UVSMappings.TryGetValue(codepoint, out ushort ret))
                    {
                        return ret;
                    }

                    // If the sequence is a default UVS, return the default glyph
                    for (int i = 0; i < sel.DefaultStartCodes.Count; ++i)
                    {
                        if (codepoint >= sel.DefaultStartCodes[i] && codepoint < sel.DefaultEndCodes[i])
                        {
                            return defaultGlyphIndex;
                        }
                    }

                    // At this point we are neither a non-default UVS nor a default UVS,
                    // but we know the nextCodepoint is a variation selector. Unicode says
                    // this glyph should be invisible: “no visible rendering for the VS”
                    // (http://unicode.org/faq/unsup_char.html#4)
                    return defaultGlyphIndex;
                }

                // In all other cases, return 0
                return 0;
            }

            public override void CollectUnicodeChars(List<uint> unicodes)
            {
                //TODO: review here
#if DEBUG
                System.Diagnostics.Debug.WriteLine("not implemented");
#endif
            }


            public static CharMapFormat14 Create(BinaryReader reader)
            {
                // 'cmap' Subtable Format 14:
                // Type                 Name                                Description
                // uint16               format                              Subtable format.Set to 14.
                // uint32               length                              Byte length of this subtable (including this header)
                // uint32               numVarSelectorRecords               Number of variation Selector Records 
                // VariationSelector    varSelector[numVarSelectorRecords]  Array of VariationSelector records.
                // ---                       
                //
                // Each variation selector records specifies a variation selector character, and
                // offsets to “default” and “non-default” tables used to map variation sequences using
                // that variation selector.
                //
                // VariationSelector Record:
                // Type      Name                 Description
                // uint24    varSelector          Variation selector
                // Offset32  defaultUVSOffset     Offset from the start of the format 14 subtable to
                //                                Default UVS Table.May be 0.
                // Offset32  nonDefaultUVSOffset  Offset from the start of the format 14 subtable to
                //                                Non-Default UVS Table. May be 0.
                //
                // The Variation Selector Records are sorted in increasing order of ‘varSelector’. No
                // two records may have the same ‘varSelector’.
                // A Variation Selector Record and the data its offsets point to specify those UVSes
                // supported by the font for which the variation selector is the ‘varSelector’ value
                // of the record. The base characters of the UVSes are stored in the tables pointed
                // to by the offsets.The UVSes are partitioned by whether they are default or
                // non-default UVSes.
                // Glyph IDs to be used for non-default UVSes are specified in the Non-Default UVS table.

                long beginAt = reader.BaseStream.Position - 2; // account for header format entry 
                uint length = Table.ReadUInt32(reader); // Byte length of this subtable (including the header)
                uint numVarSelectorRecords = Table.ReadUInt32(reader);

                var variationSelectors = new Dictionary<int, VariationSelector>();
                int[] varSelectors = new int[numVarSelectorRecords];
                uint[] defaultUVSOffsets = new uint[numVarSelectorRecords];
                uint[] nonDefaultUVSOffsets = new uint[numVarSelectorRecords];
                for (int i = 0; i < numVarSelectorRecords; ++i)
                {
                    varSelectors[i] = (int)Table.ReadUInt24(reader);
                    defaultUVSOffsets[i] = Table.ReadUInt32(reader);
                    nonDefaultUVSOffsets[i] = Table.ReadUInt32(reader);
                }


                for (int i = 0; i < numVarSelectorRecords; ++i)
                {
                    var sel = new VariationSelector();

                    if (defaultUVSOffsets[i] != 0)
                    {
                        // Default UVS table
                        //
                        // A Default UVS Table is simply a range-compressed list of Unicode scalar
                        // values, representing the base characters of the default UVSes which use
                        // the ‘varSelector’ of the associated Variation Selector Record.
                        //
                        // DefaultUVS Table:
                        // Type          Name                           Description
                        // uint32        numUnicodeValueRanges          Number of Unicode character ranges.
                        // UnicodeRange  ranges[numUnicodeValueRanges]  Array of UnicodeRange records.
                        //
                        // Each Unicode range record specifies a contiguous range of Unicode values.
                        //
                        // UnicodeRange Record:
                        // Type    Name               Description
                        // uint24  startUnicodeValue  First value in this range
                        // uint8   additionalCount    Number of additional values in this range
                        //
                        // For example, the range U+4E4D&endash; U+4E4F (3 values) will set
                        // ‘startUnicodeValue’ to 0x004E4D and ‘additionalCount’ to 2. A singleton
                        // range will set ‘additionalCount’ to 0.
                        // (‘startUnicodeValue’ + ‘additionalCount’) must not exceed 0xFFFFFF.
                        // The Unicode Value Ranges are sorted in increasing order of
                        // ‘startUnicodeValue’. The ranges must not overlap; i.e.,
                        // (‘startUnicodeValue’ + ‘additionalCount’) must be less than the
                        // ‘startUnicodeValue’ of the following range (if any).

                        reader.BaseStream.Seek(beginAt + defaultUVSOffsets[i], SeekOrigin.Begin);
                        uint numUnicodeValueRanges = Table.ReadUInt32(reader);
                        for (int n = 0; n < numUnicodeValueRanges; ++n)
                        {
                            int startCode = (int)Table.ReadUInt24(reader);
                            sel.DefaultStartCodes.Add(startCode);
                            sel.DefaultEndCodes.Add(startCode + reader.ReadByte());
                        }
                    }

                    if (nonDefaultUVSOffsets[i] != 0)
                    {
                        // Non-Default UVS table
                        //
                        // A Non-Default UVS Table is a list of pairs of Unicode scalar values and
                        // glyph IDs.The Unicode values represent the base characters of all
                        // non -default UVSes which use the ‘varSelector’ of the associated Variation
                        // Selector Record, and the glyph IDs specify the glyph IDs to use for the
                        // UVSes.
                        //
                        // NonDefaultUVS Table:
                        // Type        Name                         Description
                        // uint32      numUVSMappings               Number of UVS Mappings that follow
                        // UVSMapping  uvsMappings[numUVSMappings]  Array of UVSMapping records.
                        //
                        // Each UVSMapping record provides a glyph ID mapping for one base Unicode
                        // character, when that base character is used in a variation sequence with
                        // the current variation selector.
                        //
                        // UVSMapping Record:
                        // Type    Name          Description
                        // uint24  unicodeValue  Base Unicode value of the UVS
                        // uint16  glyphID       Glyph ID of the UVS
                        //
                        // The UVS Mappings are sorted in increasing order of ‘unicodeValue’. No two
                        // mappings in this table may have the same ‘unicodeValue’ values.

                        reader.BaseStream.Seek(beginAt + nonDefaultUVSOffsets[i], SeekOrigin.Begin);
                        uint numUVSMappings = Table.ReadUInt32(reader);
                        for (int n = 0; n < numUVSMappings; ++n)
                        {
                            int unicodeValue = (int)Table.ReadUInt24(reader);
                            ushort glyphID = Table.ReadUInt16(reader);
                            sel.UVSMappings.Add(unicodeValue, glyphID);
                        }
                    }

                    variationSelectors.Add(varSelectors[i], sel);
                }

                return new CharMapFormat14 { _variationSelectors = variationSelectors };
            }

            class VariationSelector
            {
                public List<int> DefaultStartCodes = new List<int>();
                public List<int> DefaultEndCodes = new List<int>();
                public Dictionary<int, ushort> UVSMappings = new Dictionary<int, ushort>();
            }

            private Dictionary<int, VariationSelector> _variationSelectors;
        }

        /// <summary>
        /// An empty character map that maps all characters to glyph 0
        /// </summary>
        private class NullCharMap : CharacterMap
        {
            public override ushort Format => 0;
            public override ushort GetGlyphIndex(int character) => 0;
            public override void CollectUnicodeChars(List<uint> unicodes) {  /*nothing*/}

        }
        static CharacterMap ReadFormat_0(BinaryReader reader)
        {
            ushort length = Table.ReadUInt16(reader);
            //Format 0: Byte encoding table
            //This is the Apple standard character to glyph index mapping table.
            //Type  	Name 	        Description
            //uint16 	format 	        Format number is set to 0.
            //uint16 	length 	        This is the length in bytes of the subtable.
            //uint16 	language 	    Please see “Note on the language field in 'cmap' subtables“ in this document.
            //uint8 	glyphIdArray[256] 	An array that maps character codes to glyph index values.
            //-----------
            //This is a simple 1 to 1 mapping of character codes to glyph indices. 
            //The glyph set is limited to 256. Note that if this format is used to index into a larger glyph set,
            //only the first 256 glyphs will be accessible. 

            ushort language = Table.ReadUInt16(reader);
            byte[] only256Glyphs = reader.ReadBytes(256);
            ushort[] only256UInt16Glyphs = new ushort[256];
            for (int i = 255; i >= 0; --i)
            {
                //expand
                only256UInt16Glyphs[i] = only256Glyphs[i];
            }
            //convert to format4 cmap table
            ushort[] startArray = new ushort[] { 0, 0xFFFF };
            ushort[] endArray = new ushort[] { 255, 0xFFFF };
            ushort[] deltaArray = new ushort[] { 0, 1 };
            ushort[] offsetArray = new ushort[] { 4, 0 };
            return new CharMapFormat4(startArray, endArray, deltaArray, offsetArray, only256UInt16Glyphs);
        }

        static CharacterMap ReadFormat_2(BinaryReader input)
        {
            //Format 2: High - byte mapping through table

            //This subtable is useful for the national character code standards used for Japanese, Chinese, and Korean characters.
            //These code standards use a mixed 8 / 16 - bit encoding, 
            //in which certain byte values signal the first byte of a 2 - byte character(but these values are also legal as the second byte of a 2 - byte character).
            //
            //In addition, even for the 2 - byte characters, the mapping of character codes to glyph index values depends heavily on the first byte.
            //Consequently, the table begins with an array that maps the first byte to a SubHeader record.
            //For 2 - byte character codes, the SubHeader is used to map the second byte's value through a subArray, as described below.
            //When processing mixed 8/16-bit text, SubHeader 0 is special: it is used for single-byte character codes. 
            //When SubHeader 0 is used, a second byte is not needed; the single byte value is mapped through the subArray.
            //-------------
            //  'cmap' Subtable Format 2:
            //-------------
            //  Type        Name        Description
            //  uint16      format      Format number is set to 2.
            //  uint16      length      This is the length in bytes of the subtable.
            //  uint16      language    Please see “Note on the language field in 'cmap' subtables“ in this document.
            //  uint16      subHeaderKeys[256]  Array that maps high bytes to subHeaders: value is subHeader index * 8.
            //  SubHeader   subHeaders[]   Variable - length array of SubHeader records.
            //  uint16  glyphIndexArray[]  Variable - length array containing subarrays used for mapping the low byte of 2 - byte characters.
            //------------------
            //  A SubHeader is structured as follows:
            //  SubHeader Record:
            //  Type    Name            Description
            //  uint16  firstCode       First valid low byte for this SubHeader.
            //  uint16  entryCount      Number of valid low bytes for this SubHeader.
            //  int16   idDelta See     text below.
            //  uint16  idRangeOffset   See text below.
            //
            //  The firstCode and entryCount values specify a subrange that begins at firstCode and has a length equal to the value of entryCount.
            //This subrange stays within the 0 - 255 range of the byte being mapped.
            //Bytes outside of this subrange are mapped to glyph index 0(missing glyph).
            //The offset of the byte within this subrange is then used as index into a corresponding subarray of glyphIndexArray.
            //This subarray is also of length entryCount.
            //The value of the idRangeOffset is the number of bytes past the actual location of the idRangeOffset word
            //where the glyphIndexArray element corresponding to firstCode appears.
            //  Finally, if the value obtained from the subarray is not 0(which indicates the missing glyph),
            //you should add idDelta to it in order to get the glyphIndex.
            //The value idDelta permits the same subarray to be used for several different subheaders.
            //The idDelta arithmetic is modulo 65536.

            //Utils.WarnUnimplemented("cmap subtable format 2");
            // not implemented
            return new NullCharMap();
        }

        static CharMapFormat4 ReadFormat_4(BinaryReader reader)
        {
            ushort lenOfSubTable = Table.ReadUInt16(reader); //This is the length in bytes of the subtable. ****
            //This is the Microsoft standard character to glyph index mapping table for fonts that support Unicode ranges other than the range [U+D800 - U+DFFF] (defined as Surrogates Area, in Unicode v 3.0) 
            //which is used for UCS-4 characters.
            //If a font supports this character range (i.e. in turn supports the UCS-4 characters) a subtable in this format with a platform specific encoding ID 1 is yet needed,
            //in addition to a subtable in format 12 with a platform specific encoding ID 10. Please see details on format 12 below, for fonts that support UCS-4 characters on Windows.
            //  
            //This format is used when the character codes for the characters represented by a font fall into several contiguous ranges, 
            //possibly with holes in some or all of the ranges (that is, some of the codes in a range may not have a representation in the font). 
            //The format-dependent data is divided into three parts, which must occur in the following order:
            //    A four-word header gives parameters for an optimized search of the segment list;
            //    Four parallel arrays describe the segments (one segment for each contiguous range of codes);
            //    A variable-length array of glyph IDs (unsigned words).
            long tableStartEndAt = reader.BaseStream.Position + lenOfSubTable;

            ushort language = Table.ReadUInt16(reader);
            //Note on the language field in 'cmap' subtables: 
            //The language field must be set to zero for all cmap subtables whose platform IDs are other than Macintosh (platform ID 1).
            //For cmap subtables whose platform IDs are Macintosh, set this field to the Macintosh language ID of the cmap subtable plus one, 
            //or to zero if the cmap subtable is not language-specific.
            //For example, a Mac OS Turkish cmap subtable must set this field to 18, since the Macintosh language ID for Turkish is 17. 
            //A Mac OS Roman cmap subtable must set this field to 0, since Mac OS Roman is not a language-specific encoding.

            ushort segCountX2 = Table.ReadUInt16(reader); //2 * segCount
            ushort searchRange = Table.ReadUInt16(reader); //2 * (2**FLOOR(log2(segCount)))
            ushort entrySelector = Table.ReadUInt16(reader);//2 * (2**FLOOR(log2(segCount)))
            ushort rangeShift = Table.ReadUInt16(reader); //2 * (2**FLOOR(log2(segCount)))
            int segCount = segCountX2 / 2;
            ushort[] endCode = Table.ReadUInt16Array(reader, segCount);//Ending character code for each segment, last = 0xFFFF.            
                                                                      //>To ensure that the search will terminate, the final endCode value must be 0xFFFF.
                                                                      //>This segment need not contain any valid mappings. It can simply map the single character code 0xFFFF to the missing character glyph, glyph 0.

            ushort Reserved = Table.ReadUInt16(reader); // always 0
            ushort[] startCode = Table.ReadUInt16Array(reader, segCount); //Starting character code for each segment
            ushort[] idDelta = Table.ReadUInt16Array(reader, segCount); //Delta for all character codes in segment
            ushort[] idRangeOffset = Table.ReadUInt16Array(reader, segCount); //Offset in bytes to glyph indexArray, or 0   
                                                                             //------------------------------------------------------------------------------------ 
            long remainingLen = tableStartEndAt - reader.BaseStream.Position;
            int recordNum2 = (int)(remainingLen / 2);
            ushort[] glyphIdArray = Table.ReadUInt16Array(reader, recordNum2);//Glyph index array                          
            return new CharMapFormat4(startCode, endCode, idDelta, idRangeOffset, glyphIdArray);
        }

        static CharMapFormat6 ReadFormat_6(BinaryReader reader)
        {
            //Format 6: Trimmed table mapping
            //Type      Name        Description
            //uint16    format      Format number is set to 6.
            //uint16    length      This is the length in bytes of the subtable.
            //uint16    language    Please see “Note on the language field in 'cmap' subtables“ in this document.
            //uint16    firstCode   First character code of subrange.
            //uint16    entryCount  Number of character codes in subrange.
            //uint16    glyphIdArray[entryCount]   Array of glyph index values for character codes in the range.

            //The firstCode and entryCount values specify a subrange(beginning at firstCode, length = entryCount) within the range of possible character codes.
            //Codes outside of this subrange are mapped to glyph index 0.
            //The offset of the code(from the first code) within this subrange is used as index to the glyphIdArray, 
            //which provides the glyph index value.

            ushort length = Table.ReadUInt16(reader);
            ushort language = Table.ReadUInt16(reader);
            ushort firstCode = Table.ReadUInt16(reader);
            ushort entryCount = Table.ReadUInt16(reader);
            ushort[] glyphIdArray = Table.ReadUInt16Array(reader, entryCount);
            return new CharMapFormat6(firstCode, glyphIdArray);
        }

        static CharacterMap ReadFormat_12(BinaryReader reader)
        {
            //TODO: test this again
            // Format 12: Segmented coverage
            //This is the Microsoft standard character to glyph index mapping table for fonts supporting the UCS - 4 characters 
            //in the Unicode Surrogates Area(U + D800 - U + DFFF).
            //It is a bit like format 4, in that it defines segments for sparse representation in 4 - byte character space.
            //Here's the subtable format:
            //'cmap' Subtable Format 12:
            //Type     Name      Description
            //uint16   format    Subtable format; set to 12.
            //uint16   reserved  Reserved; set to 0
            //uint32   length    Byte length of this subtable(including the header)
            //uint32   language  Please see “Note on the language field in 'cmap' subtables“ in this document.
            //uint32   numGroups Number of groupings which follow
            //SequentialMapGroup  groups[numGroups]   Array of SequentialMapGroup records.
            //
            //The sequential map group record is the same format as is used for the format 8 subtable.
            //The qualifications regarding 16 - bit character codes does not apply here, 
            //however, since characters codes are uniformly 32 - bit.
            //SequentialMapGroup Record:
            //Type    Name    Description
            //uint32  startCharCode   First character code in this group
            //uint32  endCharCode Last character code in this group
            //uint32  startGlyphID    Glyph index corresponding to the starting character code
            //
            //Groups must be sorted by increasing startCharCode.A group's endCharCode must be less than the startCharCode of the following group, 
            //if any. The endCharCode is used, rather than a count, because comparisons for group matching are usually done on an existing character code, 
            //and having the endCharCode be there explicitly saves the necessity of an addition per group.
            //
            //Fonts providing Unicode - encoded UCS - 4 character support for Windows 2000 and later, 
            //need to have a subtable with platform ID 3, platform specific encoding ID 1 in format 4;
            //and in addition, need to have a subtable for platform ID 3, platform specific encoding ID 10 in format 12.
            //Please note, that the content of format 12 subtable,
            //needs to be a super set of the content in the format 4 subtable.
            //The format 4 subtable needs to be in the cmap table to enable backward compatibility needs.

            ushort reserved = Table.ReadUInt16(reader);
#if DEBUG
            if (reserved != 0) { throw new NotSupportedException(); }
#endif

            uint length = Table.ReadUInt32(reader);// Byte length of this subtable(including the header)
            uint language = Table.ReadUInt32(reader);
            uint numGroups = Table.ReadUInt32(reader);

#if DEBUG
            if (numGroups > int.MaxValue) { throw new NotSupportedException(); }
#endif
            uint[] startCharCodes = new uint[(int)numGroups];
            uint[] endCharCodes = new uint[(int)numGroups];
            uint[] startGlyphIds = new uint[(int)numGroups];


            for (uint i = 0; i < numGroups; ++i)
            {
                //seq map group record
                startCharCodes[i] = Table.ReadUInt32(reader);
                endCharCodes[i] = Table.ReadUInt32(reader);
                startGlyphIds[i] = Table.ReadUInt32(reader);
            }
            return new CharMapFormat12(startCharCodes, endCharCodes, startGlyphIds);
        }

        private static CharacterMap ReadCharacterMap(BinaryReader input)
        {
            ushort format = input.ReadUInt16();
            switch (format)
            {
                default:
                    // not implemented
                    return new NullCharMap();
                case 0: return ReadFormat_0(input);
                case 2: return ReadFormat_2(input);
                case 4: return ReadFormat_4(input);
                case 6: return ReadFormat_6(input);
                case 12: return ReadFormat_12(input);
                case 14: return CharMapFormat14.Create(input);
            }
        }

        private class CMapTable : Table
        {
            CharacterMap[] _charMaps = null;
            List<CharMapFormat14> _charMap14List;
            Dictionary<Int32, UInt16> _codepointToGlyphs = new Dictionary<Int32, UInt16>();
            public CharacterMap[] CharacterMaps { get { return _charMaps; } }
            public IList<CharMapFormat14> CharacterMap14List { get { return _charMap14List; } }
            public IDictionary<Int32,UInt16> CodepointToGlyphs { get { return _codepointToGlyphs; } }
            public void Fill(Header header, BinaryReader reader)
            {
                Header = header;
                //https://www.microsoft.com/typography/otspec/cmap.htm
                long beginAt = reader.BaseStream.Position;
                //
                ushort version = Table.ReadUInt16(reader); // 0
                ushort tableCount = Table.ReadUInt16(reader);

                ushort[] platformIds = new ushort[tableCount];
                ushort[] encodingIds = new ushort[tableCount];
                uint[] offsets = new uint[tableCount];
                for (int i = 0; i < tableCount; i++)
                {
                    platformIds[i] = Table.ReadUInt16(reader);
                    encodingIds[i] = Table.ReadUInt16(reader);
                    offsets[i] = Table.ReadUInt32(reader);
                }

                _charMaps = new CharacterMap[tableCount];
                for (int i = 0; i < tableCount; i++)
                {
                    reader.BaseStream.Seek(beginAt + offsets[i], SeekOrigin.Begin);
                    CharacterMap cmap = ReadCharacterMap(reader);
                    cmap.PlatformId = platformIds[i];
                    cmap.EncodingId = encodingIds[i];
                    _charMaps[i] = cmap;

                    //
                    if (cmap is CharMapFormat14 cmap14)
                    {
                        if (_charMap14List == null) _charMap14List = new List<CharMapFormat14>();
                        //
                        _charMap14List.Add(cmap14);
                    }
                }
            }

        }
    }
}
