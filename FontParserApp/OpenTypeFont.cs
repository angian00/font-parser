
namespace FontParserApp
{
    public partial class OpenTypeFont
    {
        private byte[] _rawData;
        private UInt32 _currOffset;

        public UInt32 SfntVersion { get; private set; }
        public UInt16 NumTables { get; private set; }
        public List<TableRecord> TableRecords { get; private set; }
        public Int16 IndexToLocFormat;

        public Int16 XMin { get; private set; }
        public Int16 YMin { get; private set; }
        public Int16 XMax { get; private set; }
        public Int16 YMax { get; private set; }

        public UInt16 NumGlyphs { get; private set; }
        public UInt32[] GlyphOffsets { get; private set; }
        public Dictionary<char, UInt16> Char2GlyphIndex { get; private set; }

        private OTFGlyphProcessor _glyphProc;

        public OpenTypeFont(string filename)
        {
            _rawData = File.ReadAllBytes(filename);
            _currOffset = 0;

            TableRecords = new List<TableRecord>();
            ParseMainDirectory();

            if (!IsValid())
                throw new ArgumentException("!! Not a valid font file");

            GlyphOffsets = new UInt32[256];
            Char2GlyphIndex = new Dictionary<char, UInt16>();
            ParseTables();

            _glyphProc = new OTFGlyphProcessor(this);
        }


        //-------------------------------------------------------------------
        //---- Public methods -----------------------------------------------
        //-------------------------------------------------------------------

        public void PrintTableStats()
        {
            foreach (TableRecord tr in TableRecords)
            {
                Console.WriteLine($"Found table of type [{tr.Tag}]");
                Console.WriteLine($"Length: {tr.Length}");
                Console.WriteLine($"Offset: {tr.Offset}");
            }
        }

        public void PrintMainStats()
        {
            Console.WriteLine($"# glyphs: {NumGlyphs}");
            Console.WriteLine($"xMin: {XMin}");
            Console.WriteLine($"xMin: {XMax}");
            Console.WriteLine($"yMin: {YMin}");
            Console.WriteLine($"yMax: {YMax}");
        }

        public void PrintGlyphNames()
        {
            foreach (TableRecord tr in TableRecords)
            {
                Console.WriteLine($"Found table of type [{tr.Tag}]");
                Console.WriteLine($"Length: {tr.Length}");
                Console.WriteLine($"Offset: {tr.Offset}");
            }
        }


        public GlyphData GetGlyphData(char c)
        {
            if (!Char2GlyphIndex.ContainsKey(c))
            {
                Console.WriteLine($"!! Char [{c}] not supported");
                return null;
            }

            UInt16 glyphIndex = Char2GlyphIndex[c];
            Console.WriteLine($"Char [{c}] is mapped to glyphIndex [{glyphIndex}]");

            GlyphData glyphData = ParseGlyphData(glyphIndex);
            if (glyphData == null)
                return null;

            _glyphProc.ProcessGlyph(glyphData);

            return glyphData;
        }


        //-------------------------------------------------------------------
        //---- Private functions --------------------------------------------
        //-------------------------------------------------------------------

        public bool IsValid()
        {
            if (SfntVersion != 0x00010000 && SfntVersion != 0x4F54544F)
                return false;

            return true;
        }

        private TableRecord? GetTableRecord(string tableTag)
        {
            tableTag = tableTag.ToLower();

            //TODO: support repeated tags
            foreach (TableRecord tr in TableRecords)
                if (tr.Tag.ToLower() == tableTag)
                    return tr;

            return null;
        }

        private GlyphData ParseGlyphData(UInt16 glyphIndex)
        {
            TableRecord? tr = GetTableRecord("GLYF");
            if (tr == null)
                return null;

            UInt32 absGLYFOffset = tr.Offset + GlyphOffsets[glyphIndex];
            return ParseTableGLYFRecord(absGLYFOffset);
        }

        private void PrintGlyphContours(ContourPoint[][] contours)
        {
            for (int iContour = 0; iContour < contours.Length; iContour++)
            {
                Console.WriteLine($"\t contour {iContour}/{contours.Length}");
                for (int iPoint = 0; iPoint < contours[iContour].Length; iPoint++)
                {
                    Console.WriteLine($"\t {contours[iContour][iPoint]}");
                }
            }
        }

        //-------------------------------------------------------------------
        //---- Table directory parsing --------------------------------------
        //-------------------------------------------------------------------

        private void ParseMainDirectory()
        {
            SfntVersion = Read32();
            NumTables = Read16();

            UInt16 searchRange = Read16(); //ignore
            UInt16 entrySelector = Read16(); //ignore
            UInt16 rangeShift = Read16(); //ignore

            TableRecords.Clear();
            for (int i = 0; i < NumTables; i++)
            {
                ParseDirectoryRecord();
            }
        }

        private void ParseDirectoryRecord()
        {
            string tag = ReadTag();
            UInt32 checksum = Read32(); //ignore
            UInt32 offset = Read32();
            UInt32 length = Read32();

            TableRecords.Add(new TableRecord(tag, offset, length));
        }



        private void ParseTables()
        {
            //some tables must be parsed beforehand
            TableRecord? trBefore = GetTableRecord("maxp");
            if (trBefore == null)
                return;
            
            _currOffset = trBefore.Offset;
            ParseTableMAXP();

            foreach (TableRecord tr in TableRecords)
            {
                Console.WriteLine($"Processing table [{tr.Tag}]");
                _currOffset = tr.Offset;

                switch (tr.Tag.ToLower())
                {
                    case "cmap":
                        ParseTableCMAP();
                        break;

                    case "glyf":
                        //ParseTableGLYF(); //ignore: parse this data lazily at runtime
                        break;

                    case "head":
                        ParseTableHEAD();
                        break;

                    case "loca":
                        ParseTableLOCA();
                        break;

                    case "maxp":
                        ParseTableMAXP();
                        break;

                    case "name":
                        ParseTableNAME();
                        break;

                    default:
                        //Console.WriteLine($"Skipping table [{tr.Tag}]");
                        break;
                }
            }
        }

    }


    public record TableRecord
    {
        public string Tag { get; init; }
        public UInt32 Offset { get; init; }
        public UInt32 Length { get; init; }

        public TableRecord(string tag, UInt32 offset, UInt32 length)
        {
            Tag = tag;
            Offset = offset;
            Length = length;
        }
    }

    public record ContourPoint
    {
        public Int16 X { get; set; }
        public Int16 Y { get; set; }
        public bool IsOnCurve { get; set; }

        public ContourPoint(Int16 x, Int16 y, bool isOnCurve)
        {
            X = x;
            Y = y;
            IsOnCurve = isOnCurve;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }


    public class GlyphData
    {
        public ContourPoint[][] Contours { get; set; }
        public Int16 XMin { get; set; }
        public Int16 YMin { get; set; }
        public Int16 XMax { get; set; }
        public Int16 YMax { get; set; }
    }
}