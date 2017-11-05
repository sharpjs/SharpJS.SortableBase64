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
using FluentAssertions;
using NUnit.Framework;

namespace SharpJS.SortableBase64
{
    [TestFixture]
    [TestOf(typeof(SortableBase64))]
    public class SortableBase64Tests
    {
        private readonly Random _random = new Random();

        [Test]
        public void Encode_Empty()
        {
            SortableBase64.Encode(new byte[] { }).Should().Be("");
        }

        [Test]
        public void Encode_1()
        {
            SortableBase64.Encode(new byte[] { 0x00 }).Should().Be(".."); // 000000|00 (0000)
            SortableBase64.Encode(new byte[] { 0x04 }).Should().Be("0."); // 000001|00 (0000)
            SortableBase64.Encode(new byte[] { 0xA5 }).Should().Be("dF"); // 101001|01 (0000)
        }

        [Test]
        public void Encode_2()
        {
            SortableBase64.Encode(new byte[] { 0x00, 0x00 }).Should().Be("..."); // 000000|00 0000|0000 (00)
            SortableBase64.Encode(new byte[] { 0x04, 0x10 }).Should().Be("00."); // 000001|00 0001|0000 (00)
            SortableBase64.Encode(new byte[] { 0xA5, 0xA5 }).Should().Be("dPJ"); // 101001|01 1010|0101 (00)
        }

        [Test]
        public void Encode_3()
        {
            SortableBase64.Encode(new byte[] { 0x00, 0x00, 0x00 }).Should().Be("...."); // 000000|00 0000|0000 00|000000
            SortableBase64.Encode(new byte[] { 0x04, 0x10, 0x41 }).Should().Be("0000"); // 000001|00 0001|0000 01|000001
            SortableBase64.Encode(new byte[] { 0xA5, 0xA5, 0xA5 }).Should().Be("dPL_"); // 101001|01 1010|0101 10|100101
        }

        [Test]
        public void Decode_InvalidDigit1()
        {
            byte[] decoded;
            var result = SortableBase64.TryDecode("?", out decoded);

            result.Should().BeFalse();
            decoded.Should().BeNull();
        }

        [Test]
        public void Decode_InvalidDigit2()
        {
            byte[] decoded;
            var result = SortableBase64.TryDecode("\u2368", out decoded);

            result.Should().BeFalse();
            decoded.Should().BeNull();
        }

        [Test]
        public void RoundTrip([Range(0, 32, 1)] int length)
        {
            var original = new byte[length];

            lock (_random)
                _random.NextBytes(original);

            byte[] decoded;
            var code64 = SortableBase64.Encode(original);
            var result = SortableBase64.TryDecode(code64, out decoded);

            result.Should().BeTrue();
            decoded.Should().Equal(original);
        }
    }
}
