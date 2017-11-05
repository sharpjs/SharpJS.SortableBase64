/*
Copyright (C) 2017 Jeffrey Sharp

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Text;

namespace SharpJS.SortableBase64
{
    public static class SortableBase64
    {
        // Maps value to char
        private const string
            Digits = ".0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

        // Maps char to value
        private static readonly sbyte[] Values =
        {
          // 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 0x ........ .tn..r..
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 1x ........ ........
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, // 2x  !"#$%&' ()*+,-./
             1,  2,  3,  4,  5,  6,  7,  8,  9, 10, -1, -1, -1, -1, -1, -1, // 3x 01234567 89:;<=>?
            -1, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, // 4x @ABCDEFG HIJKLMNO
            26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, -1, -1, -1, -1, 37, // 5x PQRSTUVW XYZ[\]^_
            -1, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, // 6x `abcdefg hijklmno
            53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, -1, -1, -1, -1, -1, // 7x pqrstuvw xyz{|}~. <- DEL
        };

        public static int GetEncodedLength(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var length = bytes.Length;
            return length + (length + 2) / 3;

            // bytes bits => bits code64
            //     n      =>           e = n + (n+2)/3
            //
            //     0 ( 0) => ( 0)      0 = 0 + 0
            //     1 ( 8) => (12)      2 = 1 + 1
            //     2 (16) => (18)      3 = 2 + 1
            //     3 (24) => (24)      4 = 3 + 1
            //     4 (32) => (36)      6 = 4 + 2
            //     5 (40) => (42)      7 = 5 + 2
            //     6 (48) => (48)      8 = 6 + 2
            //     7 (56) => (60)     10 = 7 + 3
        }

        public static int GetDecodedLength(string code64)
        {
            if (code64 == null)
                throw new ArgumentNullException(nameof(code64));

            var length = code64.Length;
            return length - (length + 3) / 4;

            // n =  e - (e+3)/4
            //
            // 0 =  0 - 0
            // 0 =  1 - 1 (useless trailing byte)
            // 1 =  2 - 1
            // 2 =  3 - 1
            // 3 =  4 - 1
            // 3 =  5 - 2 (useless trailing byte)
            // 4 =  6 - 2
            // 5 =  7 - 2
            // 6 =  8 - 2
            // 6 =  9 - 3 (useless trailing byte)
            // 7 = 10 - 3
        }

        public static string Encode(byte[] bytes)
        {
            var code  = new StringBuilder(GetEncodedLength(bytes));
            var data  = 0;
            var state = 0;

            for (var i = 0; i < bytes.Length; i++)
            {
                data |= bytes[i];

                switch (state)
                {
                    case 0:
                        // ........ aaaaaaaa
                        //          ^^^^^^..
                        code.Append(Digits[data >> 2]);
                        data  = (data & 0x3) << 8;
                        state = 1;
                        break;
                    case 1:
                        // ......aa bbbbbbbb
                        //       ^^ ^^^^....
                        code.Append(Digits[data >> 4]);
                        data  = (data & 0xF) << 8;
                        state = 2;
                        break;
                    case 2:
                        // ....bbbb cccccccc
                        //     ^^^^ ^^
                        //            ^^^^^^
                        code.Append(Digits[data >> 6]);
                        code.Append(Digits[data & 0x3F]);
                        data  = 0;
                        state = 0;
                        break;
                }
            }

            switch (state)
            {
                case 1:
                    // ......aa 00000000
                    //       ^^ ^^^^
                    code.Append(Digits[data >> 4]);
                    break;

                case 2:
                    // ....bbbb 00000000
                    //     ^^^^ ^^
                    code.Append(Digits[data >> 6]);
                    break;
            }

            return code.ToString();
        }

        public static bool TryDecode(string code64, out byte[] bytes)
        {
            bytes = new byte[GetDecodedLength(code64)];

            var index = 0;
            var data  = 0;
            var state = 0;

            foreach (var digit in code64)
            {
                var value = digit < Values.Length ? Values[digit] : -1;
                if (value < 0)
                {
                    // Invalid digit
                    bytes = null;
                    return false;
                }

                data = data << 6 | value;

                switch (state)
                {
                    case 0:
                        // ........ ..aaaaaa
                        state = 1;
                        break;

                    case 1:
                        // ....aaaa aabbbbbb
                        //     ^^^^ ^^^^
                        bytes[index++] = (byte) (data >> 4);
                        data &= 0xF;
                        state = 2;
                        break;

                    case 2:
                        // ......bb bbcccccc
                        //       ^^ ^^^^^^
                        bytes[index++] = (byte) (data >> 2);
                        data &= 0x3;
                        state = 3;
                        break;

                    case 3:
                        // ........ ccdddddd
                        //          ^^^^^^^^
                        bytes[index++] = (byte) data;
                        data  = 0;
                        state = 0;
                        break;
                }
            }

            return true;
        }
    }
}
