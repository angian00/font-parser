using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;

namespace FontParserApp
{
    public partial class OpenTypeFont
    {

        //-------------------------------------------------------------------
        //---- Table specific parsing ---------------------------------------
        //-------------------------------------------------------------------

        private void ParseTableCMAP()
        {
            UInt32 tableOffset = _currOffset;

            UInt16 version = Read16();
            UInt16 numEncSubtables = Read16();

            for (int i = 0; i < numEncSubtables; i++)
            {
                UInt16 platformId = Read16();
                UInt16 encodingId = Read16();
                UInt32 subtableOffset = Read32();

                Console.WriteLine($"-- found CMAP subtable: platformId={platformId}, encodingId={encodingId}");
                ParseTableCMAPSubtable(tableOffset + subtableOffset);
            }
        }

        private void ParseTableCMAPSubtable(UInt32 subtableOffset)
        {
            UInt32 offsetBkp = _currOffset;
            _currOffset = subtableOffset;

            UInt16 format = Read16();
            UInt16 length;
            UInt16 language;

            switch (format)
            {
                case 0x00:
                    //old Macintosh format
                    length = Read16();
                    language = Read16();
                    for (UInt16 asciiCode = 0; asciiCode < 256; asciiCode++)
                    {
                        //glyphIdArray[iContour];
                        byte glyphIndex = ReadByte();
                        Char2GlyphIndex[(char)asciiCode] = glyphIndex;
                        //Console.WriteLine($" \t '{(Char)asciiCode}' --> {glyphIndex}");
                    }
                    break;

                case 0x04:
                    //TODO: format 4 CMAPs
                    ////segment mapping to delta values
                    //length = Read16();
                    //language = Read16(); //ignore
                    //UInt16 segCount = (UInt16)(Read16() >> 1);
                    
                    //UInt16 searchRange = Read16(); //ignore
                    //UInt16 entrySelector = Read16(); //ignore
                    //UInt16 rangeShift = Read16(); //ignore

                    //UInt16[] endCodes = new UInt16[segCount];
                    //for (int i=0; i < segCount; i++)
                    //    endCodes[i] = Read16();

                    //UInt16 reservedPad = Read16();

                    //UInt16[] startCodes = new UInt16[segCount];
                    //for (int i = 0; i < segCount; i++)
                    //    startCodes[i] = Read16();

                    //Int16[] idDelta = new Int16[segCount];
                    //for (int i = 0; i < segCount; i++)
                    //    idDelta[i] = Read16Signed();

                    //UInt16[] idRangeOffsets = new UInt16[segCount];
                    //for (int i = 0; i < segCount; i++)
                    //    idRangeOffsets[i] = Read16();

                    //UInt16 glyphIdArray[] = Read16();


                    //for (UInt16 asciiCode = 0; asciiCode < 256; asciiCode++)
                    //{
                    //    //glyphIdArray[iContour];
                    //    byte glyphIndex = ReadByte();
                    //    Char2GlyphIndex[(char)asciiCode] = glyphIndex;
                    //    //Console.WriteLine($" \t '{(Char)asciiCode}' --> {glyphIndex}");
                    //}
                    //break;

                default:
                    //ignore
                    Console.Error.WriteLine($"!! Unsupported CMAP format: {format}");
                    break;
            }


            _currOffset = offsetBkp;
        }


        private GlyphData ParseTableGLYFRecord(UInt32 glyphDataOffset)
        {
            GlyphData glyphData = new ();

            UInt32 bkpOffset = _currOffset;
            _currOffset = glyphDataOffset;

            Int16 nContours = Read16Signed();
            glyphData.XMin = Read16Signed();
            glyphData.YMin = Read16Signed();
            glyphData.XMax = Read16Signed();
            glyphData.YMax = Read16Signed();

            if (nContours < 0)
            {
                Console.WriteLine("\t Composite glyph");
                //TODO
            }
            else
            {
                Console.WriteLine("\t Simple glyph");
                Console.WriteLine($"\t nCountours = {nContours}");
                Console.WriteLine($"\t x from {glyphData.XMin} to {glyphData.XMax}, y from {glyphData.YMin} to {glyphData.YMax}");

                UInt16[] nPointsPerContour = new UInt16[nContours];
                for (int iContour = 0; iContour < nContours; iContour++)
                {
                    if (iContour == 0)
                        nPointsPerContour[iContour] = (UInt16)(Read16() + 1);
                    else
                        nPointsPerContour[iContour] = (UInt16)(Read16() - nPointsPerContour[iContour-1] + 1);
                }

                UInt16 instructionLength = Read16();
                UInt32 instructionStart = _currOffset;
                //byte instructions[instructionLength]
                _currOffset += instructionLength;


                byte[][] contoursFlags = new byte[nContours][];
                for (int iContour = 0; iContour < nContours; iContour++)
                {
                    UInt16 nPoints = nPointsPerContour[iContour];
                    byte[] contourFlags = new byte[nPoints];

                    int iPoint = 0;
                    while (iPoint < nPoints)
                    {
                        byte currFlags = ReadByte();
                        contourFlags[iPoint++] = currFlags;
                        if ((currFlags & GlyphContourFlags.REPEAT_FLAG) > 0)
                        {
                            byte nRepeats = ReadByte();
                            for (int iRepeat = 0; iRepeat < nRepeats; iRepeat++)
                            {
                                contourFlags[iPoint++] = currFlags;
                            }
                        }
                    }

                    contoursFlags[iContour] = contourFlags;
                }


                Int16[][] xContoursCoords = new Int16[nContours][];
                for (int iContour = 0; iContour < nContours; iContour++)
                {
                    UInt16 nPoints = nPointsPerContour[iContour];
                    Int16[] xContourCoords = new Int16[nPoints];

                    UInt16 iPoint = 0;
                    while (iPoint < nPoints)
                    {
                        byte currFlags = contoursFlags[iContour][iPoint];

                        bool isXShort = ((currFlags & GlyphContourFlags.X_SHORT_VECTOR) > 0);
                        bool isXSameOrPositive = ((currFlags & GlyphContourFlags.X_IS_SAME_OR_POSITIVE_X_SHORT_VECTOR) > 0);

                        if (isXShort)
                        {
                            if (isXSameOrPositive)
                                xContourCoords[iPoint++] = ReadByte();
                            else
                                xContourCoords[iPoint++] = (Int16)(-ReadByte());

                        }
                        else
                        {
                            if (isXSameOrPositive)
                            {
                                xContourCoords[iPoint] = 0;
                                iPoint++;
                            }
                            else
                            {
                                xContourCoords[iPoint++] = Read16Signed();
                            }
                        }
                    }

                    xContoursCoords[iContour] = xContourCoords;
                }


                Int16[][] yContoursCoords = new Int16[nContours][];
                for (int iContour = 0; iContour < nContours; iContour++)
                {
                    UInt16 nPoints = nPointsPerContour[iContour];
                    Int16[] yContourCoords = new Int16[nPoints];

                    UInt16 iPoint = 0;
                    while (iPoint < nPoints)
                    {
                        byte currFlags = contoursFlags[iContour][iPoint];

                        bool isYShort = ((currFlags & GlyphContourFlags.Y_SHORT_VECTOR) > 0);
                        bool isYSameOrPositive = ((currFlags & GlyphContourFlags.Y_IS_SAME_OR_POSITIVE_Y_SHORT_VECTOR) > 0);

                        if (isYShort)
                        {
                            if (isYSameOrPositive)
                                yContourCoords[iPoint++] = ReadByte();
                            else
                                yContourCoords[iPoint++] = (Int16)(-ReadByte());

                        }
                        else
                        {
                            if (isYSameOrPositive)
                            {
                                yContourCoords[iPoint] = 0;
                                iPoint++;
                            }
                            else
                            {
                                yContourCoords[iPoint++] = Read16Signed();
                            }
                        }
                    }

                    yContoursCoords[iContour] = yContourCoords;
                }


                ContourPoint[][] contours = new ContourPoint[nContours][];
                for (int iContour = 0; iContour < nContours; iContour++)
                {
                    UInt16 nPoints = nPointsPerContour[iContour];

                    ContourPoint[] contour = new ContourPoint[nPoints];
                    for (int iPoint = 0; iPoint < nPoints; iPoint++)
                    {
                        bool isOnCurve = ((contoursFlags[iContour][iPoint] & GlyphContourFlags.ON_CURVE_POINT) > 0);
                        contour[iPoint] = new ContourPoint(xContoursCoords[iContour][iPoint], yContoursCoords[iContour][iPoint], isOnCurve);
                        //bool isOverlap = ((currFlags & GlyphContourFlags.OVERLAP_SIMPLE) > 0); //ignore
                    }
                    contours[iContour] = contour;
                }

                glyphData.Contours = contours;
            }

            _currOffset = bkpOffset;

            return glyphData;
        }

        private void ParseTableHEAD()
        {
            UInt16 majorVersion = Read16(); // ignore (set to 1)
            UInt16 minorVersion = Read16(); // ignore (set to 0)
            Fixed16_16 fontRevision = ReadFixed16_16();

            UInt32 checksumAdjustment = Read32(); //ignore
            UInt32 magicNumber = Read32(); //ignore (must be 0x5F0F3CF5)
            UInt16 flags = Read16();

            UInt16 unitsPerEm = Read16();
            DateTime created = ReadDateTime();
            DateTime modified = ReadDateTime();
            XMin = Read16Signed();
            YMin = Read16Signed();
            XMax = Read16Signed();
            YMax = Read16Signed();
            UInt16 macStyle = Read16(); //ignore

            UInt16 lowestRecPPEM = Read16();
            Int16 fontDirectionHint = Read16Signed();
            IndexToLocFormat = Read16Signed(); // used for parsing of other tables!!
            Int16 glyphDataFormat = Read16Signed(); // ignore (set to 0)
        }


        private void ParseTableLOCA()
        {
            GlyphOffsets = new UInt32[NumGlyphs + 1];

            for (int i = 0; i <= NumGlyphs; i++)
            {
                if (IndexToLocFormat == 0)
                    GlyphOffsets[i] = (UInt32)(Read16() * 2);
                else
                    GlyphOffsets[i] = Read32();
            }
        }


        private void ParseTableMAXP()
        {
            UInt32 version16_16 = Read32();
            NumGlyphs = Read16();

            if (version16_16 == 0x00000500)
                return;

            Debug.Assert(version16_16 == 0x00010000);
            //TODO: parse other fields
        }


        private void ParseTableNAME()
        {
            UInt32 tableOffset = _currOffset;

            UInt16 version = Read16();
            UInt16 recCount = Read16();
            UInt16 storageOffset = Read16();

            for (int i = 0; i < recCount; i++)
            {
                UInt16 platformId = Read16();
                UInt16 encodingId = Read16();
                UInt16 languageId = Read16();
                UInt16 nameId = Read16();
                UInt16 length = Read16();
                UInt16 offset = Read16();

                UInt32 totOffset = (UInt32)(tableOffset + storageOffset + offset);
                string strData = Utils.BytesToStr(_rawData, totOffset, length);

                Console.WriteLine($"-- from [name]: {platformId} {encodingId} {languageId} {nameId} {strData}");
            }

            if (version == 0x01)
            {
                UInt16 langTagCount = Read16();
                for (int i = 0; i < langTagCount; i++)
                {
                    //TODO: parse langTagRecord
                }

            }
        }
    }
}