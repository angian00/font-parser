using System.Diagnostics;

namespace FontParserApp
{
    public partial class OpenTypeFont
    {

        //-------------------------------------------------------------------
        //---- Basic read and write operations ------------------------------
        //-------------------------------------------------------------------

        private string ReadTag()
        {
            string res = Utils.BytesToStr(_rawData, _currOffset, 4);
            _currOffset += 4;

            return res;
        }

        private DateTime ReadDateTime()
        {
            Int64 epochTs = Read64Signed();
            return Utils.DateTimeFromMacTimestamp(epochTs);
        }

        private Int64 Read64Signed()
        {
            return (Int64)ReadNBytes(8); //CHECK cast works as expected
        }


        private UInt32 Read32()
        {
            return (UInt32)ReadNBytes(4);
        }

        private Int32 Read32Signed()
        {
            return (Int32)ReadNBytes(4); //CHECK cast works as expected
        }

        private Fixed16_16 ReadFixed16_16()
        {
            return new Fixed16_16(Read32());
        }

        private UInt16 Read16()
        {
            return (UInt16)ReadNBytes(2);
        }

        private Int16 Read16Signed()
        {
            return (Int16)ReadNBytes(2); //CHECK cast works as expected
        }

        private byte ReadByte()
        {
            return (byte)ReadNBytes(1);
        }


        private UInt64 ReadNBytes(int nBytes)
        {
            Debug.Assert(nBytes > 0 && nBytes <= 8);

            UInt64 val = 0x00;
            for (int i = 0; i < nBytes; i++)
            {
                if (i > 0)
                    val <<= 8;

                val += _rawData[_currOffset++];
            }

            return val;
        }

    }
}