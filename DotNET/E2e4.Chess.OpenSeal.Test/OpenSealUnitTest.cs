using E2e4.Chess.OpenSeal.Helpers;
using System.Text;
using Xunit;

namespace E2e4.Chess.OpenSeal.Test
{
    public class OpenSealUnitTest
    {
        [Fact]
        public void EncodeHelloMessageGivesCorrectByteArray()
        {
            string str = OpenSeal.Encode(OpenSealTestData.Hello, OpenSealTestData.SystemTime1);
            byte[] strBuffer = OpenSealTestData.Iso88591.GetBytes(str);

            Assert.Equal(OpenSealTestData.Buffer1, strBuffer);
        }

        [Fact]
        public void DecodeOfByteArrayGivesCorrectTimestampAndHelloMessage()
        {
            string strBuffer = OpenSealTestData.Iso88591.GetString(OpenSealTestData.Buffer1);
            string plainText = OpenSeal.Decode(strBuffer);

            string expectedText = $"{OpenSealTestData.Timestamp1}: {OpenSealTestData.Hello}\n";
            Assert.Equal(expectedText, plainText);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DecodeOfNullOrEmptyInputReturnsZeroTimestampFallback(string? input)
        {
            Assert.Equal($"0: {input}\n", OpenSeal.Decode(input));
        }

        [Fact]
        public void DecodeOfInputMissingTrailingLinefeedReturnsFallbackEchoingInput()
        {
            Assert.Equal("0: abc\n", OpenSeal.Decode("abc"));
        }

        [Fact]
        public void DecodeOfInputWithLengthNotCongruentToOneModTwelveReturnsFallbackEchoingInput()
        {
            // After trimming the trailing line feed, "abcde" has length 5, which fails the
            // "length mod 12 == 1" check. As in openseal_decoder.c, the fallback echoes the
            // line without its terminator and appends exactly one line feed.
            Assert.Equal("0: abcde\n", OpenSeal.Decode("abcde\n"));
        }

        [Fact]
        public void DecodeOfCorruptedValidLengthInputReturnsFallbackEchoingInput()
        {
            // Flip every byte but the last (which encodes the key offset used to decode
            // every other byte) so the cipher's internal high-bit consistency check fails.
            string decoded = OpenSealTestData.Iso88591.GetString(OpenSealTestData.Buffer1);
            char[] chars = decoded.ToCharArray();
            for (int i = 0; i < chars.Length - 1; i++)
                chars[i] = (char)(chars[i] ^ 0xFF);
            string corrupted = new(chars);

            Assert.Equal($"0: {corrupted.TrimEnd('\n')}\n", OpenSeal.Decode(corrupted));
        }
    }
}
