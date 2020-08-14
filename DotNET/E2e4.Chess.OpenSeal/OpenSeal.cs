using E2e4.Chess.OpenSeal.Helpers;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace E2e4.Chess.OpenSeal
{
    public static class OpenSeal
    {
        private const string key = "Timestamp (FICS) v1.0 - programmed by Henrik Gram.";

        public static string Encrypt(string text, long systemTime = 0)
        {
            StringBuilder sb = new StringBuilder(text);

            long timestamp = TimeHelper.GetTimestamp(systemTime);
            Debug.WriteLine($"{timestamp:n0} ");

            sb.AppendFormat("\x18{0}\x19", timestamp);

            for (int n = sb.Length; n % 12 != 0; n++)
            {
                sb.Append('1');
            }

            for (int n = 0; n < sb.Length; n += 12)
            {
                sb[n + 11] ^= sb[n] ^= sb[n + 11];
                sb[n] ^= sb[n + 11];

                sb[n + 9] ^= sb[n + 2] ^= sb[n + 9];
                sb[n + 2] ^= sb[n + 9];

                sb[n + 7] ^= sb[n + 4] ^= sb[n + 7];
                sb[n + 4] ^= sb[n + 7];
            }

            for (int n = 0; n < sb.Length; n++)
            {
                sb[n] = (char)(((sb[n] | 0x80) ^ key[n % 50]) - 32);
            }

            sb.AppendFormat("\x80\n");

            return sb.ToString();
        }

        public static string Decrypt(string s)
        {
            try
            {
                if (String.IsNullOrEmpty(s))
                {
                    throw new Exception("Malformed message - message is null or empty");
                }

                StringBuilder sb = new StringBuilder(s);
                //        int n,offset,l=strlen(s);
                //        char *tmp_time,*tmp_end,*num_end;
                //        static char tmp[BSIZE];

                //        // first a consistency check
                //        if(l%12!=1) goto malformed_message;
                if (sb[sb.Length - 1] != '\n')
                {
                    throw new Exception("Malformed message - final linefeed character is missing");
                }

                sb.Length--;
                if (sb.Length % 12 != 1)
                {
                    throw new Exception("Malformed message - message length failed mod 12 check");
                }

                //        // we need this one...
                //        offset=(unsigned char)s[l-1];

                //        // decode the string
                //        strcpy(tmp,s);
                //        for(n=0;n<l;n++) {
                //                tmp[n]=(tmp[n]+32)^key[(n+offset-0x80)%50];
                //                if(!tmp[n]&0x80) goto malformed_message;
                //                tmp[n]^=0x80;
                //        }
                byte offset = (byte)sb[sb.Length - 1];
                for (int n = 0; n < sb.Length; n++)
                {
                    sb[n] = (char)((sb[n] + 32) ^ key[(n + offset - 0x80) % 50]);

                    if ((((byte)sb[n]) & ((byte)0x80)) == 0)
                    {
                        throw new Exception("Malformed message - message failed bitwise AND check");
                    }

                    sb[n] ^= '\x80';
                }


                //#define SC(A,B) tmp[B]^=tmp[A]^=tmp[B],tmp[A]^=tmp[B]
                //        for(n=0;n<l;n+=12)
                //                SC(n,n+11), SC(n+2,n+9), SC(n+4,n+7);
                for (int n = 0; n < sb.Length - 11; n += 12)
                {
                    sb[n + 11] ^= sb[n] ^= sb[n + 11];
                    sb[n] ^= sb[n + 11];

                    sb[n + 9] ^= sb[n + 2] ^= sb[n + 9];
                    sb[n + 2] ^= sb[n + 9];

                    sb[n + 7] ^= sb[n + 4] ^= sb[n + 7];
                    sb[n + 4] ^= sb[n + 7];
                }

                //        // find the timestamp beginning
                //        tmp_time=strchr(tmp,'\x18');
                //        if(!tmp_time) goto malformed_message;
                //        *tmp_time=0;
                //        tmp_time++;

                //        // find the timestamp end
                //        tmp_end=strchr(tmp_time,'\x19');
                //        if(!tmp_end) goto malformed_message;
                //        *tmp_end=0;

                //        // check that every character in the message is printable
                //        for(n=0;tmp[n];n++)
                //                if(!tmp[n]>>5) goto malformed_message;

                //        // check that the timestamp actually is a number
                //        strtol(tmp_time,&num_end,10);
                //        if(num_end!=tmp_end) goto malformed_message;

                //Message must be made up only from printable characters and the timestamp is one or more digits
                Regex regex = new Regex(@"^(?<Message>[^\x00-\x1f\x7f]*)\x18(?<Timestamp>\d+)\x19.*$",
                    RegexOptions.Compiled |
                    RegexOptions.ExplicitCapture |
                    RegexOptions.CultureInvariant |
                    RegexOptions.Singleline);
                Match match = regex.Match(sb.ToString());
                if (match.Success)
                {
                    string message = match.Groups["Message"].Value;
                    string timestamp = match.Groups["Timestamp"].Value;
                    return $"{timestamp}: {message}\n";
                }
                else
                {
                    throw new Exception("Malformed message - message failed regex check");
                }

                //        // assume we have got a valid message, and print it out
                //        printf("%s: %s\n",tmp_time,tmp);
                //        return;

                //        // in case of a malformed message, just output 0: message\n
                //malformed_message:
                //        printf("0: %s\n",s);
                //        return;
            }
            catch
            {
                //Malformed message. Simply return 0
                return $"0: {s}\n";
            }
        }
    }
}