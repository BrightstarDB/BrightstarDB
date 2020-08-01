using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace BrightstarDB.Utils
{
    internal class Base32
    {
        private const int BASE32_INPUT = 5;
        private const int BASE32_OUTPUT = 8;
        private const int BASE32_MAX_PADDING = 6;
        private const char BASE32_MAX_VALUE = (char)31;

        private static readonly char[] EncodeSymbol = new[] {
            '0','1','2','3','4','5','6','7','8','9',
            'A','B','C','D','E','F','G','H','J','K',
            'M','N','P','Q','R','S','T','V','W','X',
            'Y','Z', '='
        };

        private static byte[] DecodeValue = new byte[] {
            /*00-07*/ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*08-0f*/ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*10-17*/ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*18-1f*/ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*20-27*/ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*28-2f*/ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            /*30-37*/ 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, /* '0' - '7' */
            /*38-3f*/ 0x08, 0x09, 0xFF, 0xFF, 0xFF, 0x20, 0xFF, 0xFF, /* '8', '9', '=' */
            /*40-47*/ 0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, /* 'A' - 'G' */
            /*48-4f*/ 0x11, 0x01, 0x12, 0x13, 0x01, 0x14, 0x15, 0x00, /* 'H' - 'O': I and L map to 1, O maps to 0 */
            /*50-57*/ 0x16, 0x17, 0x18, 0x19, 0x1A, 0xFF, 0x1B, 0x1C, /* 'P' - 'W': U is not mapped */
            /*58-5f*/ 0x1D, 0x1E, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, /* 'X' - 'Z' */
            /*60-67*/ 0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, /* 'a' - 'g': same as 'A' - 'G' */
            /*68-6f*/ 0x11, 0x01, 0x12, 0x13, 0x01, 0x14, 0x15, 0x00, /* 'h' - 'o': same as 'H' - 'O' */
            /*70-77*/ 0x16, 0x17, 0x18, 0x19, 0x1A, 0xFF, 0x1B, 0x1C, /* 'p' - 'w': same as 'P' - 'W' */
            /*78-7f*/ 0x1D, 0x1E, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, /* 'x' - 'z': same as 'X' - 'Z' */
        };

        public static string Encode(byte[] bytes, int start, int byteCount)
        {
            int srcSize = byteCount;
            StringBuilder dest = new StringBuilder();
            int blockSize;
            int blockStart = start;
            byte n1, n2, n3, n4, n5, n6, n7, n8;

            while (srcSize >= 1)
            {
                blockSize = srcSize < BASE32_INPUT ? srcSize : BASE32_INPUT;
                n1 = n2 = n3 = n4 = n5 = n6 = n7 = n8 = 0;

                if (blockSize < 1 || blockSize > 5) Debug.Assert(false);
                if (blockSize == 5)
                {
                    n8 = (byte)(bytes[blockStart + 4] & 0x1f);
                    n7 = (byte)((bytes[blockStart + 4] & 0xe0) >> 5);
                }
                if (blockSize >= 4)
                {
                    n7 |= (byte)((bytes[blockStart + 3] & 0x03) << 3);
                    n6 = (byte)((bytes[blockStart + 3] & 0x7c) >> 2);
                    n5 = (byte)((bytes[blockStart + 3] & 0x80) >> 7);
                }
                if (blockSize >= 3)
                {
                    n5 |= (byte)((bytes[blockStart + 2] & 0x0f) << 1);
                    n4 = (byte)((bytes[blockStart + 2] & 0xf0) >> 4);
                }
                if (blockSize >= 2)
                {
                    n4 |= (byte)((bytes[blockStart + 1] & 0x01) << 4);
                    n3 = (byte)((bytes[blockStart + 1] & 0x3e) >> 1);
                    n2 = (byte)((bytes[blockStart + 1] & 0xc0) >> 6);
                }
                if (blockSize >= 1)
                {
                    n2 |= (byte)((bytes[blockStart] & 0x07) << 2);
                    n1 = (byte)((bytes[blockStart] & 0xf8) >> 3);
                }
                blockStart += blockSize;
                srcSize -= blockSize;

                Debug.Assert(n1 <= 31);
                Debug.Assert(n2 <= 31);
                Debug.Assert(n3 <= 31);
                Debug.Assert(n4 <= 31);
                Debug.Assert(n5 <= 31);
                Debug.Assert(n6 <= 31);
                Debug.Assert(n7 <= 31);
                Debug.Assert(n8 <= 31);

                // Padding
                if (blockSize < 1 || blockSize > 5)
                {
                    Debug.Assert(false);
                }
                if (blockSize == 1) { n3 = n4 = 32; }
                if (blockSize <= 2) { n5 = 32; }
                if (blockSize <= 3) { n6 = n7 = 32; }
                if (blockSize <= 4) { n8 = 32; }

                // 8 outputs
                dest.Append(EncodeSymbol[n1]);
                dest.Append(EncodeSymbol[n2]);
                dest.Append(EncodeSymbol[n3]);
                dest.Append(EncodeSymbol[n4]);
                dest.Append(EncodeSymbol[n5]);
                dest.Append(EncodeSymbol[n6]);
                dest.Append(EncodeSymbol[n7]);
                dest.Append(EncodeSymbol[n8]);
            }
            return dest.ToString();
        }

        private static string GetDecodableString(string src)
        {
            string str = src.Replace("-", "");
            while (str.Length % 8 != 0)
            {
                // Pad with =
                str = str + "=";
            }
            return str;
        }

        public static int GetDecodedLength(string src)
        {
            string decodable = GetDecodableString(src);
            return ((decodable.Length + 7) / 8) * 5;
        }

        public static int Decode(string src, byte[] dest)
        {
            /*
             * output 5 bytes for every 8 input:
             *
             *               outputs: 1        2        3        4        5
             * inputs: 1 = ---11111 = 11111---
             *         2 = ---222XX = -----222 XX------
             *         3 = ---33333 =          --33333-
             *         4 = ---4XXXX =          -------4 XXXX----
             *         5 = ---5555X =                   ----5555 X-------
             *         6 = ---66666 =                            -66666--
             *         7 = ---77XXX =                            ------77 XXX-----
             *         8 = ---88888 =                                     ---88888
             */

            src = GetDecodableString(src);
            int destOffset = 0;
            int srcSize = src.Length;
            int srcOffset = 0;
            int destSize = 0;
            char in1, in2, in3, in4, in5, in6, in7, in8;

            while (srcSize >= 1)
            {
                // 8 inputs
                in1 = src[srcOffset++];
                in2 = src[srcOffset++];
                in3 = src[srcOffset++];
                in4 = src[srcOffset++];
                in5 = src[srcOffset++];
                in6 = src[srcOffset++];
                in7 = src[srcOffset++];
                in8 = src[srcOffset++];
                srcSize -= 8;

                // Validation
                if (in1 > 0x80 || in2 >= 0x80 || in3 >= 0x80 || in4 >= 0x80
                    || in5 > 0x80 || in6 >= 0x80 || in7 >= 0x80 || in8 >= 0x80)
                {
                    throw new FormatException("Invalid Base32 character in input string");
                }
                // Convert to base32 value
                in1 = (char)DecodeValue[in1];
                in2 = (char)DecodeValue[in2];
                in3 = (char)DecodeValue[in3];
                in4 = (char)DecodeValue[in4];
                in5 = (char)DecodeValue[in5];
                in6 = (char)DecodeValue[in6];
                in7 = (char)DecodeValue[in7];
                in8 = (char)DecodeValue[in8];
                // Validate base32
                if (in1 > BASE32_MAX_VALUE || in2 > BASE32_MAX_VALUE)
                {
                    throw new FormatException("Invalid Base32 character in input string");
                }
                // The following can be padding (0x20)
                if (in3 > BASE32_MAX_VALUE + 1 || in4 > BASE32_MAX_VALUE + 1
                    || in5 > BASE32_MAX_VALUE + 1 || in6 > BASE32_MAX_VALUE + 1
                    || in7 > BASE32_MAX_VALUE + 1 || in8 > BASE32_MAX_VALUE + 1)
                {
                    throw new FormatException("Invalid Base32 character in input string");
                }

                dest[destOffset++] = (byte)(((in1 & 0x1f) << 3) | ((in2 & 0x1c) >> 2));
                dest[destOffset++] = (byte)(((in2 & 0x03) << 6) | ((in3 & 0x1f) << 1) | ((in4 & 0x10) >> 4));
                dest[destOffset++] = (byte)(((in4 & 0x0f) << 4) | ((in5 & 0x1e) >> 1));
                dest[destOffset++] = (byte)(((in5 & 0x01) << 7) | ((in6 & 0x1f) << 2) | ((in7 & 0x18) >> 3));
                dest[destOffset++] = (byte)(((in7 & 0x07) << 5) | (in8 & 0x1f));
                destSize += 5;

                // Padding
                if (in8 == BASE32_MAX_VALUE + 1)
                {
                    destSize--;
                    Debug.Assert((in7 == BASE32_MAX_VALUE + 1 && in6 == BASE32_MAX_VALUE + 1) || (in7 != BASE32_MAX_VALUE + 1));
                    if (in6 == BASE32_MAX_VALUE + 1)
                    {
                        destSize--;
                        if (in5 == BASE32_MAX_VALUE + 1)
                        {
                            destSize--;
                            Debug.Assert((in4 == BASE32_MAX_VALUE + 1 && in3 == BASE32_MAX_VALUE + 1) || (in4 != BASE32_MAX_VALUE + 1));
                            if (in3 == BASE32_MAX_VALUE + 1)
                            {
                                destSize--;
                            }
                        }
                    }
                }

            }
            return destSize;
        }
    }
}
