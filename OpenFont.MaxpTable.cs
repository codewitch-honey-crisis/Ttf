using System;
using System.IO;

namespace Ttf
{
    partial class OpenFont
    {
        private class MaxpTable : Table
        {
            UInt32 _version;            
            UInt16 _numGlyphs;          
            UInt16 _maxPoints;          
            UInt16 _maxContours;        
            UInt16 _maxCompositePoints; 
            UInt16 _maxCompositeContours;
            UInt16 _maxZones;           
            UInt16 _maxTwilightPoints;  
            UInt16 _maxStorage;         
            UInt16 _maxFunctionDefs;    
            UInt16 _maxInstructionDefs; 
            UInt16 _maxStackElements;   
            UInt16 _maxSizeOfInstructions;
            UInt16 _maxComponentElements;
            UInt16 _maxComponentDepth; 
            
            // 0x00010000 for version 1.0.
            public UInt32 Version { get { return _version; } }
            // The number of glyphs in the font.
            public UInt16 NumGlyphs {  get { return _numGlyphs; } }
            // Maximum points in a non-composite glyph.
            public UInt16 MaxPoints { get { return _maxPoints; } }
            // Maximum contours in a non-composite glyph.
            public UInt16 MaxContours { get { return _maxContours; } }
            // Maximum points in a composite glyph.
            public UInt16 MaxCompositePoints { get { return _maxCompositePoints; } }
            //Maximum contours in a composite glyph.
            public UInt16 MaxCompositeContours {  get { return _maxCompositeContours; } }
            //1 if instructions do not use the twilight zone (Z0), or 2 if instructions do use Z0; should be set to 2 in most cases.
            public UInt16 MaxZones {  get { return _maxZones; } }
            // Maximum points used in Z0.
            public UInt16 MaxTwilightPoints {  get { return _maxTwilightPoints; } }
            // Number of Storage Area locations.
            public UInt16 MaxStorage { get { return _maxStorage; } }
            // Number of FDEFs, equal to the highest function number + 1.
            public UInt16 MaxFunctionDefs { get { return _maxFunctionDefs; } }
            // Number of IDEFs.
            public UInt16 MaxInstructionDefs { get { return _maxInstructionDefs; } }
            // Maximum stack depth across Font Program ('fpgm' table), CVT Program('prep' table) and all glyph instructions(in the 'glyf' table).
            public UInt16 MaxStackElements { get { return _maxStackElements; } }
            //Maximum byte count for glyph instructions.
            public UInt16 MaxSizeOfInstructions { get { return _maxSizeOfInstructions; } }
            //Maximum number of components referenced at “top level” for any composite glyph.
            public UInt16 MaxComponentElements { get { return _maxComponentElements; } }
            // Maximum levels of recursion; 1 for simple components.
            public UInt16 MaxComponentDepth { get { return _maxComponentDepth; } }

            public void Fill(Header header,BinaryReader reader)
            {
                Header = header;
                _version = ReadUInt32(reader);
                _numGlyphs = ReadUInt16(reader);
                _maxPoints = ReadUInt16(reader);
                _maxContours = ReadUInt16(reader);
                _maxCompositePoints = ReadUInt16(reader);
                _maxCompositeContours = ReadUInt16(reader);
                _maxZones = ReadUInt16(reader);
                _maxTwilightPoints = ReadUInt16(reader);
                _maxStorage = ReadUInt16(reader);
                _maxFunctionDefs = ReadUInt16(reader);
                _maxInstructionDefs = ReadUInt16(reader);
                _maxStackElements = ReadUInt16(reader);
                _maxSizeOfInstructions = ReadUInt16(reader);
                _maxComponentElements = ReadUInt16(reader);
                _maxComponentDepth = ReadUInt16(reader);
            }

        }
    }
}
