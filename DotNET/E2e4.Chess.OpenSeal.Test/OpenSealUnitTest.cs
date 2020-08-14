using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace E2e4.Chess.OpenSeal.Test
{
    [TestClass]
    public class OpenSealUnitTest
    {
        private const string _hello = "TIMESTAMP|openseal|Running on an operating system|";

        private readonly long _timestamp1 = 7279154;
        private readonly long _systemTime1 = 130770708791546526;
        private readonly byte[] _buffer1 = new byte[]
        {
            0x84, 0x80, 0x71, 0x80, 0x9e, 0x80, 0x80, 0x9e, 0x80, 0xcd,
            0xa7, 0x72, 0x80, 0x8d, 0x9d, 0xac, 0xd2, 0x7a, 0xad, 0xaf,
            0xa5, 0xb3, 0xa3, 0xa5, 0x75, 0x75, 0x60, 0x68, 0x7c, 0xa1,
            0x6c, 0x63, 0xa5, 0xa4, 0xb0, 0x6c, 0x7c, 0xa1, 0x9b, 0x6c,
            0x7d, 0x75, 0xa9, 0x65, 0xb9, 0x93, 0x66, 0x73, 0xbc, 0xb2,
            0xc0, 0xbe, 0xbc, 0xb2, 0xaa, 0xa6, 0xb4, 0xd5, 0xc9, 0xad,
            0x80, 0x0a,
        };

        [TestMethod]
        public void EncryptHelloMessageGivesCorrectByteArray()
        {
            //Encoding encoding = Encoding.GetEncoding(28591);
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string str = OpenSeal.Encrypt(_hello, _systemTime1);
            byte[] strBuffer = encoding.GetBytes(str);
            CollectionAssert.AreEqual(_buffer1, strBuffer);
        }

        [TestMethod]
        public void DecryptOfByteArrayGivesCorrectTimestampAndHelloMessage()
        {
            //Encoding encoding = Encoding.GetEncoding(28591);
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string strBuffer = encoding.GetString(_buffer1);
            string plainText = OpenSeal.Decrypt(strBuffer);

            string expectedText = $"{_timestamp1}: {_hello}\n";
            Assert.AreEqual(expectedText, plainText);
        }

        [TestMethod]
        public void EncodingByNameAndNumberAreTheSame()
        {
            Encoding encoding1 = Encoding.GetEncoding(28591);
            Encoding encoding2 = Encoding.GetEncoding("iso-8859-1");
            Assert.AreEqual(encoding1, encoding2);
        }
    }
}
