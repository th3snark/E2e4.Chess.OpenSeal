using System;
using System.Text;
using Xunit;

namespace E2e4.Chess.OpenSeal.Test
{
    public class OpenSealWireFormatUnitTest
    {
        // Golden vectors produced by an independent port of crypt() in C/FatICS/timeseal/openseal.c
        // (the port also reproduces OpenSealTestData.Buffer1 exactly), for the fixed timestamp
        // OpenSealTestData.Timestamp1. They cover every padding remainder around a block boundary.
        public static TheoryData<string, string> GoldenVectors() => new()
        {
            { "", "c5bebcb2a7a5b4b4c97279be800a" },
            { "a", "c5d1d4b7a6adb0baa4777987800a" },
            { "hello", "c16cb469a1ccb662a7ac798ed8bac27871a7607f61717c74800a" },
            { "1234567890", "c3bbbdb1aba2b6b8a97390d7d8d4c27871a2887b61797c72800a" },
            { "12345678901", "acbbbdb1aba2b6b8a97379d7d8d1c27099a3657f61777c77800a" },
            { "abcdefghijkl", "986b67617b72666879a3a387d8d4c27e74a7647789727c98800a" },
            { "abcdefghijklm", "986b67617b72666879a3a387d8bbaa7b75af607964777cad800a" },
            { "tell 1 hello there", "9b6c61697ba5a1ad75aca492d897c4ac777389bc62a87460a1a6beb6a3b0bcbcb4dd71b7800a" },
            { "caf\u00E9", "c068bc6ca4a3b3d5a9a67d85d8d2c27871a7607f61717c99800a" },
        };

        private static string Wire(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return Encoding.Latin1.GetString(bytes);
        }

        [Theory]
        [MemberData(nameof(GoldenVectors))]
        public void EncodeMatchesTheCReferenceForEveryPaddingRemainder(string text, string expectedHex)
        {
            Assert.Equal(Wire(expectedHex), OpenSeal.Encode(text, OpenSealTestData.SystemTime1));
        }

        [Theory]
        [InlineData("", "c5bebcb2a7a5b4b4c97279be800a")]
        [InlineData("hello", "c16cb469a1ccb662a7ac798ed8bac27871a7607f61717c74800a")]
        [InlineData("12345678901", "acbbbdb1aba2b6b8a97379d7d8d1c27099a3657f61777c77800a")]
        [InlineData("tell 1 hello there", "9b6c61697ba5a1ad75aca492d897c4ac777389bc62a87460a1a6beb6a3b0bcbcb4dd71b7800a")]
        public void DecodeOfTheCReferenceVectorsRecoversTimestampAndText(string text, string expectedHex)
        {
            Assert.Equal($"{OpenSealTestData.Timestamp1}: {text}\n", OpenSeal.Decode(Wire(expectedHex)));
        }

        [Fact]
        public void RoundTripsEveryMessageLengthUpToSixty()
        {
            for (int length = 0; length <= 60; length++)
            {
                string text = new string((char)('!' + length % 90), length);

                string sealedText = OpenSeal.Encode(text, OpenSealTestData.SystemTime1);

                Assert.Equal($"{OpenSealTestData.Timestamp1}: {text}\n", OpenSeal.Decode(sealedText));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(30)]
        public void SealedMessagesHaveTheWireShape(int length)
        {
            string sealedText = OpenSeal.Encode(new string('x', length), OpenSealTestData.SystemTime1);

            Assert.EndsWith("\u0080\n", sealedText);
            Assert.Equal(2, sealedText.Length % 12);
            Assert.All(sealedText, c => Assert.True(c <= 0xFF));
            Assert.DoesNotContain('\n', sealedText.Substring(0, sealedText.Length - 1));
        }

        [Fact]
        public void LiveTimestampsRoundTripThroughTheSystemClock()
        {
            string decoded = OpenSeal.Decode(OpenSeal.Encode("ping"));

            Assert.Matches(@"^[0-9]+: ping\n$", decoded);
        }

        [Fact]
        public void CharactersWithTheHighBitSetLoseItWhenSealed()
        {
            // The scheme is 7-bit: Encode ORs 0x80 into every character, so the decoder cannot
            // tell 0xE9 from 0x69. This is inherited from the C reference and pinned here.
            string sealedText = OpenSeal.Encode("caf\u00E9", OpenSealTestData.SystemTime1);

            Assert.Equal($"{OpenSealTestData.Timestamp1}: cafi\n", OpenSeal.Decode(sealedText));
        }

        [Fact]
        public void EncodeReplacesCharactersAboveLatin1WithQuestionMarks()
        {
            string sealedText = OpenSeal.Encode("a\u20ACb", OpenSealTestData.SystemTime1);

            Assert.Equal($"{OpenSealTestData.Timestamp1}: a?b\n", OpenSeal.Decode(sealedText));
        }

        [Fact]
        public void EncodeRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => OpenSeal.Encode(null!));
        }

        [Theory]
        [InlineData("a\u0001b")]        // control character in the message
        [InlineData("x\u0018y")]        // stray timestamp marker in the message
        public void MessagesContainingControlCharactersAreNotReadBackAsValid(string text)
        {
            string sealedText = OpenSeal.Encode(text, OpenSealTestData.SystemTime1);

            Assert.False(OpenSeal.TryDecode(sealedText, out _, out _));
            Assert.Equal($"0: {sealedText.TrimEnd('\n')}\n", OpenSeal.Decode(sealedText));
        }

        [Fact]
        public void TryDecodeReturnsTimestampAndMessage()
        {
            string sealedText = OpenSeal.Encode("tell 1 hi", OpenSealTestData.SystemTime1);

            Assert.True(OpenSeal.TryDecode(sealedText, out string? timestamp, out string? message));
            Assert.Equal(OpenSealTestData.Timestamp1.ToString(), timestamp);
            Assert.Equal("tell 1 hi", message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\n")]
        [InlineData("no terminator")]
        [InlineData("abcde\n")]                       // wrong length
        [InlineData("aaaaaaaaaaaaA\n")]               // right length, key offset below 0x80
        [InlineData("aaaaaaaaaaaa\u20AC\n")]        // not a byte stream
        public void TryDecodeRejectsMalformedInputWithoutThrowing(string? input)
        {
            Assert.False(OpenSeal.TryDecode(input!, out string? timestamp, out string? message));
            Assert.Null(timestamp);
            Assert.Null(message);
        }

        [Theory]
        [InlineData("abcde\n", "0: abcde\n")]
        [InlineData("no terminator", "0: no terminator\n")]
        [InlineData("\n", "0: \n")]
        [InlineData("", "0: \n")]
        public void DecodeFallbackEchoesTheLineWithASingleTerminator(string input, string expected)
        {
            Assert.Equal(expected, OpenSeal.Decode(input));
        }
    }
}
