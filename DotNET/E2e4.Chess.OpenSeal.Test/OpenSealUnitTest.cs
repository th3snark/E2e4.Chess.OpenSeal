using E2e4.Chess.OpenSeal.Helpers;
using System.Text;
using Xunit;

namespace E2e4.Chess.OpenSeal.Test
{
    public class OpenSealUnitTest
    {
        [Fact]
        public void EncryptHelloMessageGivesCorrectByteArray()
        {
            string str = OpenSeal.Encrypt(OpenSealTestData.Hello, OpenSealTestData.SystemTime1);
            byte[] strBuffer = OpenSealTestData.Iso88591.GetBytes(str);

            Assert.Equal(OpenSealTestData.Buffer1, strBuffer);
        }

        [Fact]
        public void DecryptOfByteArrayGivesCorrectTimestampAndHelloMessage()
        {
            string strBuffer = OpenSealTestData.Iso88591.GetString(OpenSealTestData.Buffer1);
            string plainText = OpenSeal.Decrypt(strBuffer);

            string expectedText = $"{OpenSealTestData.Timestamp1}: {OpenSealTestData.Hello}\n";
            Assert.Equal(expectedText, plainText);
        }

        [Fact]
        public void EncodingByNameAndNumberAreTheSame()
        {
            Assert.Equal(Encoding.GetEncoding(28591), OpenSealTestData.Iso88591);
        }

        [Fact]
        public void TimestampForFixedSystemTimeMatchesTimeHelperGetTimestamp()
        {
            Assert.Equal(OpenSealTestData.Timestamp1, TimeHelper.GetTimestamp(OpenSealTestData.SystemTime1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DecryptOfNullOrEmptyInputReturnsZeroTimestampFallback(string input)
        {
            Assert.Equal($"0: {input}\n", OpenSeal.Decrypt(input));
        }

        [Fact]
        public void DecryptOfInputMissingTrailingLinefeedReturnsFallbackEchoingInput()
        {
            Assert.Equal("0: abc\n", OpenSeal.Decrypt("abc"));
        }

        [Fact]
        public void DecryptOfInputWithLengthNotCongruentToOneModTwelveReturnsFallbackEchoingInput()
        {
            // After trimming the trailing '\n', "abcde" has length 5, which fails
            // Decrypt's "length mod 12 == 1" check. The expected value has two
            // trailing newlines: the input's own, plus the one the fallback appends.
            Assert.Equal("0: abcde\n\n", OpenSeal.Decrypt("abcde\n"));
        }

        [Fact]
        public void DecryptOfCorruptedValidLengthInputReturnsFallbackEchoingInput()
        {
            // Flip every byte but the last (which encodes the key offset used to decode
            // every other byte) so the cipher's internal high-bit consistency check fails.
            string decoded = OpenSealTestData.Iso88591.GetString(OpenSealTestData.Buffer1);
            char[] chars = decoded.ToCharArray();
            for (int i = 0; i < chars.Length - 1; i++)
                chars[i] = (char)(chars[i] ^ 0xFF);
            string corrupted = new(chars);

            Assert.Equal($"0: {corrupted}\n", OpenSeal.Decrypt(corrupted));
        }
    }
}
