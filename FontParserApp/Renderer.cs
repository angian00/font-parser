
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;


namespace FontParserApp
{
    public class Renderer
    {
        const int fUnitsPerPixel = 5;
        //const int DEBUG_MARK_SIZE = 5;
        const int DEBUG_MARK_SIZE = 0;

        public static void RenderAsImage(string filepath, GlyphData glyphData, int fUnitImgWidth, int fUnitImgHeight)
        {
            int pxImgWidth    = fUnitImgWidth / fUnitsPerPixel;
            int pxImgHeight   = fUnitImgHeight / fUnitsPerPixel;
            int pxXStart      = glyphData.XMin / fUnitsPerPixel;
            int pxYStart      = pxImgHeight - glyphData.YMax / fUnitsPerPixel;
            int pxGlyphWidth  = (glyphData.XMax - glyphData.XMin) / fUnitsPerPixel;
            int pxGlyphHeight = (glyphData.YMax - glyphData.YMin) / fUnitsPerPixel;

            Bitmap bmp = new Bitmap(pxImgWidth, pxImgHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                //var glyphSizeRect = new Rectangle(pxXStart, pxYStart, pxGlyphWidth, pxGlyphHeight);
                //g.DrawRectangle(Pens.Black, glyphSizeRect);


                for (int iContour = 0; iContour < glyphData.Contours.Length; iContour++)
                {
                    int startX = -1;
                    int startY = -1;
                    int firstX = -1;
                    int firstY = -1;
                    int nBezierControls = 0;


                    ContourPoint[] currContour = glyphData.Contours[iContour];
                    int nPoints = currContour.Length;

                    //from glyph metrics to image coords
                    int[] xPx = new int[currContour.Length];
                    int[] yPx = new int[currContour.Length];
                    for (int iPoint = 0; iPoint < currContour.Length; iPoint++)
                    {
                        xPx[iPoint] = currContour[iPoint].X / fUnitsPerPixel;
                        yPx[iPoint] = pxImgHeight - currContour[iPoint].Y / fUnitsPerPixel;
                    }

                    for (int iPoint = 0; iPoint < currContour.Length; iPoint++)
                    {
                        Color pxColor = currContour[iPoint].IsOnCurve ? Color.Black : Color.Red;

                        //bmp.SetPixel(xPx, yPx, pxColor);
                        if (DEBUG_MARK_SIZE > 0)
                        {
                            for (int i = 0; i < DEBUG_MARK_SIZE; i++)
                            {
                                for (int j = 0; j < DEBUG_MARK_SIZE; j++)
                                {
                                    bmp.SetPixel(xPx[iPoint] + i, yPx[iPoint] + j, pxColor);
                                }
                            }
                        }

                        if (iPoint == 0)
                            continue;

                        if (glyphData.Contours[iContour][iPoint].IsOnCurve)
                        {
                            switch (nBezierControls)
                            {
                                case 2:
                                    Debug.Assert(iPoint >= 3);
                                    g.DrawBezier(Pens.Black,
                                        new Point(xPx[iPoint - 3], yPx[iPoint - 3]),
                                        new Point(xPx[iPoint - 2], yPx[iPoint - 2]),
                                        new Point(xPx[iPoint - 1], yPx[iPoint - 1]),
                                        new Point(xPx[iPoint], yPx[iPoint])
                                    );
                                    break;

                                case 1:
                                    Debug.Assert(iPoint >= 2);
                                    g.DrawBezier(Pens.Black,
                                        new Point(xPx[iPoint - 2], yPx[iPoint - 2]),
                                        new Point(xPx[iPoint - 1], yPx[iPoint - 1]),
                                        new Point(xPx[iPoint - 1], yPx[iPoint - 1]),
                                        new Point(xPx[iPoint], yPx[iPoint])
                                    );
                                    break;

                                case 0:
                                    g.DrawLine(Pens.Black,
                                        new Point(xPx[iPoint - 1], yPx[iPoint - 1]),
                                        new Point(xPx[iPoint], yPx[iPoint])
                                    );
                                    break;

                                default:
                                    Debug.Assert(false);
                                    break;
                            }

                            nBezierControls = 0;
                        }
                        else
                        {
                            nBezierControls++;
                        }
                    }

                    switch (nBezierControls)
                    {
                        case 2:
                            g.DrawBezier(Pens.Black,
                                new Point(xPx[nPoints - 3], yPx[nPoints - 3]),
                                new Point(xPx[nPoints - 2], yPx[nPoints - 2]),
                                new Point(xPx[nPoints - 1], yPx[nPoints - 1]),
                                new Point(xPx[0], yPx[0])
                            );
                            break;

                        case 1:
                            g.DrawBezier(Pens.Black,
                                new Point(xPx[nPoints - 2], yPx[nPoints - 2]),
                                new Point(xPx[nPoints - 1], yPx[nPoints - 1]),
                                new Point(xPx[nPoints - 1], yPx[nPoints - 1]),
                                new Point(xPx[0], yPx[0])
                            );
                            break;

                        case 0:
                            g.DrawLine(Pens.Black,
                                new Point(xPx[nPoints - 1], yPx[nPoints - 1]),
                                new Point(xPx[0], yPx[0])
                            );
                            break;

                        default:
                            Debug.Assert(false);
                            break;
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