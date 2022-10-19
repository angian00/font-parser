namespace FontParserApp
{
    public class GlyphContourFlags
    {
        public const byte ON_CURVE_POINT                        = 0x01;
        public const byte X_SHORT_VECTOR                        = 0x02;
        public const byte Y_SHORT_VECTOR                        = 0x04;
        public const byte REPEAT_FLAG                           = 0x08;
        public const byte X_IS_SAME_OR_POSITIVE_X_SHORT_VECTOR  = 0x10;
        public const byte Y_IS_SAME_OR_POSITIVE_Y_SHORT_VECTOR  = 0x20;
        public const byte OVERLAP_SIMPLE                        = 0x40;
        //public const byte __RESERVED__ = 0x80;
    }
}