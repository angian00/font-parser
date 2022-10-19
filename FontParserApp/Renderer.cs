
using System.Drawing;
using System.Drawing.Imaging;


namespace FontParserApp
{
    public class Renderer
    {
        const int fUnitsPerPixel = 2;
        const int DEBUG_MARK_SIZE = 5;

        public static void RenderAsImage(string filepath, GlyphData glyphData, int width, int height)
        {
            int pxWidth  = width / fUnitsPerPixel;
            int pxHeight = height / fUnitsPerPixel;

            var glyphSizeRect = new Rectangle(
                glyphData.XMin / fUnitsPerPixel,
                glyphData.YMin / fUnitsPerPixel,
                (glyphData.XMax - glyphData.XMin) / fUnitsPerPixel,
                (glyphData.YMax - glyphData.YMin) / fUnitsPerPixel
            );

            Bitmap bmp = new Bitmap(pxWidth, pxHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.Black, glyphSizeRect);
            }


            for (int iContour = 0; iContour < glyphData.Contours.Length; iContour++)
            {
                for (int iPoint = 0; iPoint < glyphData.Contours[iContour].Length; iPoint++)
                {
                    int xPx = glyphData.Contours[iContour][iPoint].X / fUnitsPerPixel;
                    int yPx = glyphData.Contours[iContour][iPoint].Y / fUnitsPerPixel;
                    Color pxColor = glyphData.Contours[iContour][iPoint].IsOnCurve ? Color.Black : Color.Red;

                    //bmp.SetPixel(xPx, yPx, Color.Black);
                    for (int i=0; i < DEBUG_MARK_SIZE; i++)
                    {
                        for (int j = 0; j < DEBUG_MARK_SIZE; j++)
                        {
                            bmp.SetPixel(xPx+i, yPx+j, pxColor);
                        }

                    }
                }
            }

            var encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, 1L);
            
            bmp.Save(filepath, GetEncoder(ImageFormat.Png), encParams);
        }


        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }

            return null;
        }
    }
}