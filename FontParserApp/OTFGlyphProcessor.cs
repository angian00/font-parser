using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace FontParserApp
{
    public class OTFGlyphProcessor
    {
        private Int16 _globalXMin;
        private Int16 _globalYMin;
        private Int16 _globalXMax;
        private Int16 _globalYMax;

        public OTFGlyphProcessor(OpenTypeFont font)
        {
            //global metrics
            _globalXMin = font.XMin;
            _globalYMin = font.YMin;
            _globalXMax = font.XMax;
            _globalYMax = font.YMax;
        }


        public void ProcessGlyph(GlyphData glyphData)
        {
            Int16 lastX = (Int16)(-_globalXMin);
            Int16 lastY = (Int16)(-_globalYMin);

            //normalize metrics
            glyphData.XMin -= _globalXMin;
            glyphData.XMax -= _globalXMin;
            glyphData.YMin -= _globalYMin;
            glyphData.YMax -= _globalYMin;

            for (UInt16 iContour=0; iContour < glyphData.Contours.Length; iContour++)
            {
                for (UInt16 iPoint=0; iPoint < glyphData.Contours[iContour].Length; iPoint++)
                {
                    ContourPoint currPoint = glyphData.Contours[iContour][iPoint];
                    currPoint.X = (Int16)(lastX + currPoint.X);
                    currPoint.Y = (Int16)(lastY + currPoint.Y);
                    
                    lastX = currPoint.X;
                    lastY = currPoint.Y;
                }
            }
        }
    }
}