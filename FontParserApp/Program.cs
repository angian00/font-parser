namespace FontParserApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("FontParser");
            Console.WriteLine("an OpenType file font by AnGian");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("");


            if (args.Length != 1)
            {
                Console.Error.WriteLine("  Usage: FontParser <fontfile.otf|fontfile.ttf>");
                Environment.Exit(1);
            }

            OpenTypeFont font = new(args[0]);

            font.PrintMainStats();
            //font.PrintTableStats();
            //font.PrintGlyphNames();

            //char ch = 'i';
            //char ch = 'c';
            char ch = 'b';

            GlyphData glyphData = font.GetGlyphData(ch);

            //font.PrintGlyphStats('€');

            string imagePath = $"test_image_{ch}.png";
            Renderer.RenderAsImage(imagePath, glyphData, font.XMax-font.XMin, font.YMax-font.YMin);
            Console.WriteLine($"Written image file: {imagePath}");
        }
    }
}