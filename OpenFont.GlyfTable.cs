using System;
using System.IO; 

namespace Ttf
{
    partial class OpenFont
    {
        private class GlyfTable : Table
        {
            enum GlyphFlags : Byte
            {
                sg_on_curve = 1,
                sg_xbyte = 1 << 1,
                sg_ybyte = 1 << 2,
                sg_repeat = 1 << 3,
                sg_xsign_or_same = 1 << 4,
                sg_ysign_or_same = 1 << 5
            };
            enum CompositeGlyphFlags : UInt16
            {
                //These are the constants for the flags field:
                //Bit   Flags 	 	            Description
                //0     ARG_1_AND_2_ARE_WORDS  	If this is set, the arguments are words; otherwise, they are bytes.
                //1     ARGS_ARE_XY_VALUES 	  	If this is set, the arguments are xy values; otherwise, they are points.
                //2     ROUND_XY_TO_GRID 	  	For the xy values if the preceding is true.
                //3     WE_HAVE_A_SCALE 	 	This indicates that there is a simple scale for the component. Otherwise, scale = 1.0.
                //4     RESERVED 	        	This bit is reserved. Set it to 0.
                //5     MORE_COMPONENTS 	    Indicates at least one more glyph after this one.
                //6     WE_HAVE_AN_X_AND_Y_SCALE 	The x direction will use a different scale from the y direction.
                //7     WE_HAVE_A_TWO_BY_TWO 	  	There is a 2 by 2 transformation that will be used to scale the component.
                //8     WE_HAVE_INSTRUCTIONS 	 	Following the last component are instructions for the composite character.
                //9     USE_MY_METRICS 	 	        If set, this forces the aw and lsb (and rsb) for the composite to be equal to those from this original glyph. This works for hinted and unhinted characters.
                //10    OVERLAP_COMPOUND 	 	    If set, the components of the compound glyph overlap. Use of this flag is not required in OpenType — that is, it is valid to have components overlap without having this flag set. It may affect behaviors in some platforms, however. (See Apple’s specification for details regarding behavior in Apple platforms.)
                //11    SCALED_COMPONENT_OFFSET 	The composite is designed to have the component offset scaled.
                //12    UNSCALED_COMPONENT_OFFSET 	The composite is designed not to have the component offset scaled.

                cg_arg_1_and_2_are_words = 1,
                cg_args_are_xy_values = 1 << 1,
                cg_round_xy_to_grid = 1 << 2,
                cg_we_have_a_scale = 1 << 3,
                cg_reserved = 1 << 4,
                cg_more_components = 1 << 5,
                cg_we_have_an_x_and_a_y_scale = 1 << 6,
                cg_we_have_a_two_by_two = 1 << 7,
                cg_we_have_instructions = 1 << 8,
                cg_use_my_metrics = 1 << 9,
                cg_overlap_compound = 1 << 10,
                cg_scaled_component_offset = 1 << 11,
                cg_unscaled_component_offset = 1 << 12
            };
            Glyph[] _glyphs;

            public Glyph[] Glyphs { get { return _glyphs; } }
            public void Fill(Header header, UInt32[] glyphLocations, BinaryReader reader)
            {
                Header = header;
                _glyphs = new Glyph[glyphLocations.Length - 1];
                for (int i = 0; i < _glyphs.Length; ++i)
                {
                    reader.BaseStream.Seek(Header.Offset + glyphLocations[i], SeekOrigin.Begin);
                    UInt32 length = glyphLocations[i + 1] - glyphLocations[i];
                    if (length > 0)
                    {
                        _glyphs[i] = ReadGlyph((Int16)i, reader);
                    }
                    else
                    {
                        _glyphs[i] = Glyph.Empty;
                    }
                }
                for (int i = 0; i < _glyphs.Length; ++i)
                {
                    if (_glyphs[i].Index < 0)
                    {
                        _glyphs[i].Index = (Int16)~_glyphs[i].Index;
                        BuildCompositeGlyph(_glyphs, Header.Offset, glyphLocations, _glyphs[i].Index, reader);
                    }
                }
            }
            static bool HasFlag(GlyphFlags flags,GlyphFlags test)
            {
                return test == (flags & test);
            }
            static bool HasFlag(CompositeGlyphFlags flags, CompositeGlyphFlags test)
            {
                return test == (flags & test);
            }
            GlyphFlags[] ReadGlyphFlags(int count, BinaryReader reader)
            {
                GlyphFlags[] result = new GlyphFlags[count];
                int i = 0;
                Byte repeat = 0;
                GlyphFlags flag = default(GlyphFlags);
                while (i < count)
                {
                    if (repeat > 0)
                    {
                        --repeat;
                    }
                    else
                    {
                        flag = (GlyphFlags)ReadByte(reader);
                        if (HasFlag(flag, GlyphFlags.sg_repeat))
                        {
                            repeat = ReadByte(reader);
                        }
                    }
                    result[i++] = flag;
                }

                return result;
            }
            void FillGlyphPoints(GlyphPoint[] points,GlyphFlags[] flags,GlyphFlags isByte,GlyphFlags signOrSame, BinaryReader reader)
            {
                int x = 0;
                for (int i = 0; i < points.Length; ++i)
                {
                    int dx;
                    if (HasFlag(flags[i], isByte))
                    {
                        Byte b=ReadByte(reader);
                        dx = HasFlag(flags[i], signOrSame) ? b : -b;
                    }
                    else
                    {
                        if (HasFlag(flags[i], signOrSame))
                        {
                            dx = 0;
                        }
                        else
                        {
                            dx = ReadUInt16(reader);
                        }
                    }
                    x += dx;
                    if (isByte == GlyphFlags.sg_ybyte)
                    {
                        points[i].Location = new PointF(points[i].Location.X,(short)x);
                    }
                    else
                    {
                        points[i].Location = new PointF((short)x,points[i].Location.Y);
                    }
                }
            }
            Glyph ReadGlyph(Int16 index, BinaryReader reader)
            {
                Glyph result=default(Glyph);
                result.Index = index;
                Int16 contoursSize = ReadInt16(reader);
                result.Bounds = ReadSRect16(reader);
                if (contoursSize>0)
                {
                    result.ContourEndpoints = new UInt16[contoursSize];
                    for(int i =0;i<contoursSize;++i)
                    {
                        result.ContourEndpoints[i] = ReadUInt16(reader);
                    }
                    int instSize = ReadUInt16(reader);
                    result.Instructions = reader.ReadBytes(instSize);
                    if(result.Instructions.Length!=instSize)
                    {
                        throw new InvalidDataException("Unexpected end of file");
                    }
                    int pointsSize = result.ContourEndpoints[contoursSize - 1] + 1;
                    if(pointsSize>0)
                    {
                        result.Points = new GlyphPoint[pointsSize];
                        GlyphFlags[] flags = ReadGlyphFlags(pointsSize, reader);
                        FillGlyphPoints(result.Points, flags, GlyphFlags.sg_xbyte, GlyphFlags.sg_xsign_or_same, reader);
                        FillGlyphPoints(result.Points, flags, GlyphFlags.sg_ybyte, GlyphFlags.sg_ysign_or_same, reader);
                        for(int i = 0;i<result.Points.Length;++i)
                        {
                            result.Points[i].Kind = HasFlag(flags[i], GlyphFlags.sg_on_curve)?GlyphPointKind.Curve:GlyphPointKind.Normal;
                        }
                    }

                } else
                {
                    result.Index = (Int16)~result.Index;
                }
                return result; 
            }
            Glyph BuildCompositeGlyph(Glyph[] glyphs,UInt32 tableOffset,UInt32[] glyfLocations, Int16 compositeIndex,BinaryReader reader)
            {
                Glyph glyph=Glyph.Empty;
                //https://www.microsoft.com/typography/OTSPEC/glyf.htm
                //Composite Glyph Description

                //This is the table information needed for composite glyphs (numberOfContours is -1). 
                //A composite glyph starts with two USHORT values (“flags” and “glyphIndex,” i.e. the index of the first contour in this composite glyph); 
                //the data then varies according to “flags”).
                //Type 	    Name 	    Description
                //uint16 	flags 	    component flag
                //uint16 	glyphIndex 	glyph index of component
                //VARIABLE 	argument1 	x-offset for component or point number; type depends on bits 0 and 1 in component flags
                //VARIABLE 	argument2 	y-offset for component or point number; type depends on bits 0 and 1 in component flags
                //---------
                //note: VARIABLE => may be uint8,int8,uint16 or int16
                //see more at https://fontforge.github.io/assets/old/Composites/index.html
                //---------
                reader.BaseStream.Seek(tableOffset+ glyfLocations[glyph.Index], SeekOrigin.Begin);
                //------------------------
                UInt16 contoursSize = ReadUInt16(reader); // ignored
                SRect16 bounds = ReadSRect16(reader);
                Glyph finalGlyph=Glyph.Empty;
                bool first = true;
                CompositeGlyphFlags flags;
                do
                {
                    flags = (CompositeGlyphFlags)ReadUInt16(reader);
                    UInt16 glyphIndex = ReadUInt16(reader);
                    if (glyphs[glyphIndex].Index < 0)
                    {
                        // This glyph is not read yet, resolve it first!
                        long storedOffset = reader.BaseStream.Seek(0, SeekOrigin.Current);
                        glyphs[glyphIndex]=BuildCompositeGlyph(glyphs,tableOffset,glyfLocations, (Int16)glyphIndex,reader);
                        reader.BaseStream.Seek(storedOffset, SeekOrigin.Begin);
                    }
                    Glyph newGlyph=glyphs[glyphIndex].Clone();
                    newGlyph.Index = compositeIndex;
                    Int32 arg1 = 0;//arg1, arg2 may be int8,uint8,int16,uint 16 
                    Int32 arg2 = 0;//arg1, arg2 may be int8,uint8,int16,uint 16

                    if (HasFlag(flags, CompositeGlyphFlags.cg_arg_1_and_2_are_words))
                    {

                        //0x0002  ARGS_ARE_XY_VALUES Bit 1: If this is set,
                        //the arguments are **signed xy values**
                        //otherwise, they are unsigned point numbers.
                        if (HasFlag(flags, CompositeGlyphFlags.cg_args_are_xy_values))
                        {
                            //signed
                            arg1 = ReadInt16(reader);
                            arg2 = ReadInt16(reader);
                        }
                        else
                        {
                            //unsigned
                            arg1 = ReadUInt16(reader);
                            arg2 = ReadUInt16(reader);
                        }
                    }
                    else
                    {
                        //0x0002  ARGS_ARE_XY_VALUES Bit 1: If this is set,
                        //the arguments are **signed xy values**
                        //otherwise, they are unsigned point numbers.
                        if (HasFlag(flags, CompositeGlyphFlags.cg_args_are_xy_values))
                        {
                            arg1 = ReadSByte(reader);
                            arg2 = ReadSByte(reader);
                        }
                        else
                        {
                            arg1 = ReadByte(reader);
                            arg2 = ReadByte(reader);
                        }
                    }

                    //-----------------------------------------
                    float xscale = 1;
                    float scale01 = 0;
                    float scale10 = 0;
                    float yscale = 1;

                    bool useMatrix = false;
                    //-----------------------------------------
                    bool hasScale = false;
                    if (HasFlag(flags, CompositeGlyphFlags.cg_we_have_a_scale))
                    {
                        //If the bit WE_HAVE_A_SCALE is set,
                        //the scale value is read in 2.14 format-the value can be between -2 to almost +2.
                        //The glyph will be scaled by this value before grid-fitting. 
                        xscale = ReadF214(reader);
                        yscale = xscale;
                        hasScale = true;
                    }
                    else if (HasFlag(flags, CompositeGlyphFlags.cg_we_have_an_x_and_a_y_scale))
                    {
                        xscale= ReadF214(reader);
                        yscale= ReadF214(reader);
                        hasScale = true;
                    }
                    else if (HasFlag(flags,CompositeGlyphFlags.cg_we_have_a_two_by_two))
                    {

                        //The bit WE_HAVE_A_TWO_BY_TWO allows for linear transformation of the X and Y coordinates by specifying a 2 × 2 matrix.
                        //This could be used for scaling and 90-degree*** rotations of the glyph components, for example.

                        //2x2 matrix

                        //The purpose of USE_MY_METRICS is to force the lsb and rsb to take on a desired value.
                        //For example, an i-circumflex (U+00EF) is often composed of the circumflex and a dotless-i. 
                        //In order to force the composite to have the same metrics as the dotless-i,
                        //set USE_MY_METRICS for the dotless-i component of the composite. 
                        //Without this bit, the rsb and lsb would be calculated from the hmtx entry for the composite 
                        //(or would need to be explicitly set with TrueType instructions).

                        //Note that the behavior of the USE_MY_METRICS operation is undefined for rotated composite components. 
                        useMatrix = true;
                        hasScale = true;
                        xscale = ReadF214(reader);
                        scale01 = ReadF214(reader);
                        scale10 = ReadF214(reader);
                        yscale = ReadF214(reader);
                    }

                    //Argument1 and argument2 can be either...
                    //   x and y offsets to be added to the glyph(the ARGS_ARE_XY_VALUES flag is set), 
                    //or 
                    //   two point numbers(the ARGS_ARE_XY_VALUES flag is **not** set)

                    //When arguments 1 and 2 are an x and a y offset instead of points and the bit ROUND_XY_TO_GRID is set to 1,
                    //the values are rounded to those of the closest grid lines before they are added to the glyph.
                    //X and Y offsets are described in FUnits. 


                    //--------------------------------------------------------------------
                    if (HasFlag(flags, CompositeGlyphFlags.cg_args_are_xy_values))
                    {
                        if (useMatrix)
                        {
                            //use this matrix  
                            newGlyph.Transform2x2(xscale, scale01, scale10, yscale);
                            newGlyph.Offset((Int16)arg1, (Int16)arg2);
                        }
                        else
                        {
                            if (hasScale)
                            {
                                if (xscale != 1.0 || yscale != 1.0)
                                {
                                    newGlyph.Transform2x2(xscale, 0, 0, yscale);
                                }
                                newGlyph.Offset((Int16)arg1, (Int16)arg2);
                            }
                            else
                            {
                                if (HasFlag(flags, CompositeGlyphFlags.cg_round_xy_to_grid))
                                {
                                    //TODO: implement round xy to grid***
                                    //----------------------------
                                }
                                //just offset***
                                newGlyph.Offset((Int16)arg1, (Int16)arg2);
                            }
                        }
                    }
                    else
                    {
                        //two point numbers. 
                        //the first point number indicates the point that is to be matched to the new glyph. 
                        //The second number indicates the new glyph's “matched” point. 
                        //Once a glyph is added,its point numbers begin directly after the last glyphs (endpoint of first glyph + 1)

                        //TODO: implement this...

                    }

                    //
                    if (first)
                    {

                        first = false;
                        finalGlyph = new Glyph();
                        finalGlyph.Index = newGlyph.Index;
                        finalGlyph.Bounds = newGlyph.Bounds;
                        finalGlyph.ContourEndpoints = newGlyph.ContourEndpoints;
                        finalGlyph.Points = newGlyph.Points;
                        finalGlyph.Instructions = newGlyph.Instructions;
                    }
                    else
                    {
                        finalGlyph.Append(newGlyph);
                    }
                } while (HasFlag(flags, CompositeGlyphFlags.cg_more_components));
                //
                if (HasFlag(flags, CompositeGlyphFlags.cg_we_have_instructions))
                {
                    UInt16 instrSize = ReadUInt16(reader);
                    finalGlyph.Instructions = reader.ReadBytes(instrSize);
                    if(finalGlyph.Instructions.Length!=instrSize)
                    {
                        throw new InvalidDataException("Unexpected end of stream");
                    }
                }
                //F2DOT14 	16-bit signed fixed number with the low 14 bits of fraction (2.14).
                //Transformation Option
                //
                //The C pseudo-code fragment below shows how the composite glyph information is stored and parsed; definitions for “flags” bits follow this fragment:
                //  do {
                //    USHORT flags;
                //    USHORT glyphIndex;
                //    if ( flags & ARG_1_AND_2_ARE_WORDS) {
                //    (SHORT or FWord) argument1;
                //    (SHORT or FWord) argument2;
                //    } else {
                //        USHORT arg1and2; /* (arg1 << 8) | arg2 */
                //    }
                //    if ( flags & WE_HAVE_A_SCALE ) {
                //        F2Dot14  scale;    /* Format 2.14 */
                //    } else if ( flags & WE_HAVE_AN_X_AND_Y_SCALE ) {
                //        F2Dot14  xscale;    /* Format 2.14 */
                //        F2Dot14  yscale;    /* Format 2.14 */
                //    } else if ( flags & WE_HAVE_A_TWO_BY_TWO ) {
                //        F2Dot14  xscale;    /* Format 2.14 */
                //        F2Dot14  scale01;   /* Format 2.14 */
                //        F2Dot14  scale10;   /* Format 2.14 */
                //        F2Dot14  yscale;    /* Format 2.14 */
                //    }
                //} while ( flags & MORE_COMPONENTS ) 
                //if (flags & WE_HAVE_INSTR){
                //    USHORT numInstr
                //    BYTE instr[numInstr]
                //------------------------------------------------------------ 
                if (first)
                {
                    glyph = Glyph.Empty;
                }
                else
                {
                    glyph = finalGlyph;
                }
                return glyph;
            }
        }
        
    }
}
