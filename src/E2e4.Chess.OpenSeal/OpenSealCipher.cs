using E2e4.Chess.OpenSeal.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace E2e4.Chess.OpenSeal;

/// <summary>
/// The FICS "timeseal" obfuscation as implemented by openseal by Marcello Mamino (GPL v2 or later), as
/// shipped in FatICS: https://github.com/gbtami/FatICS/blob/577f5ccf62352de721f3f7a57632059150fe7506/timeseal/openseal.c
/// and openseal_decoder.c beside it (see REFERENCES.md). This is not cryptography: it is a fixed-key
/// XOR/shuffle that carries a client timestamp so the server can measure lag.
/// </summary>
public static class OpenSealCipher
{
    private const string Key = "Timestamp (FICS) v1.0 - programmed by Henrik Gram.";

    /// <summary>The message plus its timestamp is padded to, and shuffled within, blocks of this size.</summary>
    private const int BlockSize = 12;

    private const char TimestampStart = '\u0018';
    private const char TimestampEnd = '\u0019';
    private const char PaddingChar = '1';

    /// <summary>
    /// The last character of a sealed message before its line feed. The decoder uses it as the
    /// offset into the key, so valid messages always end with it.
    /// </summary>
    private const char KeyOffsetChar = '\u0080';

    /// <summary>
    /// A decoded message is printable text, then the timestamp between the two marker
    /// characters. The timestamp must be one or more ASCII digits (the C decoder's strtol check
    /// also accepts an empty one).
    /// </summary>
    private static readonly Regex DecodedMessage = new Regex(
        @"^(?<Message>[^\x00-\x1f\x7f]*)\x18(?<Timestamp>[0-9]+)\x19.*$",
        RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    /// <summary>
    /// Seals a line of text for sending to a FICS server. The result is a Latin-1 string (every
    /// character is at most U+00FF) ending with the line feed that terminates the message.
    /// Characters above U+00FF cannot be represented on the wire and are sent as '?'.
    /// The cipher also sets the high bit of every character, so a character from U+0080 to U+00FF
    /// is not recovered intact by the server (it arrives with the bit cleared: U+00E9 becomes 'i');
    /// the protocol is meant for 7-bit ASCII text.
    /// </summary>
    /// <param name="text">The text to seal, without its line terminator.</param>
    public static string Encode(string text)
    {
        return Encode(text, 0);
    }

    /// <summary>
    /// Seals a line of text like <see cref="Encode(string)"/>, taking the timestamp from a fixed time so tests are deterministic.
    /// </summary>
    /// <param name="text">The text to seal, without its line terminator.</param>
    /// <param name="systemTime">A Windows file time to take the timestamp from, or 0 for now.</param>
    internal static string Encode(string text, long systemTime)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        long timestamp = TimeHelper.GetTimestamp(systemTime);
        string plain = text + TimestampStart + timestamp.ToString(CultureInfo.InvariantCulture) + TimestampEnd;

        int paddedLength = (plain.Length + BlockSize - 1) / BlockSize * BlockSize;
        char[] buffer = new char[paddedLength + 2];
        for (int n = 0; n < paddedLength; n++)
        {
            char c = n < plain.Length ? plain[n] : PaddingChar;
            buffer[n] = c > 0xFF ? '?' : c;
        }

        for (int n = 0; n < paddedLength; n += BlockSize)
        {
            ShuffleBlock(buffer, n);
        }

        for (int n = 0; n < paddedLength; n++)
        {
            buffer[n] = (char)((((buffer[n] | 0x80) ^ Key[n % Key.Length]) - 32) & 0xFF);
        }

        buffer[paddedLength] = KeyOffsetChar;
        buffer[paddedLength + 1] = '\n';
        return new string(buffer);
    }

    /// <summary>
    /// Unseals a line received in the openseal wire format, returning "&lt;timestamp&gt;: &lt;message&gt;\n".
    /// Like openseal_decoder.c, a malformed line comes back as "0: &lt;line&gt;\n".
    /// </summary>
    /// <param name="s">The sealed line including its terminating line feed.</param>
    public static string Decode(string? s)
    {
        if (TryDecode(s, out string? timestamp, out string? message))
        {
            return timestamp + ": " + message + "\n";
        }

        string line = s ?? string.Empty;
        if (line.Length > 0 && line[line.Length - 1] == '\n')
        {
            line = line.Substring(0, line.Length - 1);
        }
        return "0: " + line + "\n";
    }

    /// <summary>
    /// Attempts to unseal a line received in the openseal wire format.
    /// </summary>
    /// <param name="s">The sealed line including its terminating line feed.</param>
    /// <param name="timestamp">The decimal timestamp the sender embedded, when successful.</param>
    /// <param name="message">The message text, when successful.</param>
    /// <returns>False if the line is not a well-formed sealed message.</returns>
    /// <remarks>
    /// This is slightly stricter than the C decoder, whose two "printable" and "high bit"
    /// checks are no-ops because of operator precedence (<c>!x&amp;0x80</c> is <c>(!x)&amp;0x80</c>).
    /// Genuine messages satisfy the intended checks implemented here.
    /// </remarks>
    public static bool TryDecode(string? s, [NotNullWhen(true)] out string? timestamp, [NotNullWhen(true)] out string? message)
    {
        timestamp = null;
        message = null;

        if (s == null || s.Length == 0 || s[s.Length - 1] != '\n')
        {
            return false;
        }

        int length = s.Length - 1;
        if (length % BlockSize != 1)
        {
            return false;
        }

        for (int n = 0; n < length; n++)
        {
            if (s[n] > 0xFF)
            {
                return false;
            }
        }

        int offset = s[length - 1];
        if (offset < 0x80)
        {
            return false;
        }

        char[] buffer = new char[length];
        for (int n = 0; n < length; n++)
        {
            int c = ((s[n] + 32) ^ Key[(n + offset - 0x80) % Key.Length]) & 0xFF;
            if ((c & 0x80) == 0)
            {
                return false;
            }
            buffer[n] = (char)(c ^ 0x80);
        }

        for (int n = 0; n + BlockSize - 1 < length; n += BlockSize)
        {
            ShuffleBlock(buffer, n);
        }

        Match match = DecodedMessage.Match(new string(buffer));
        if (!match.Success)
        {
            return false;
        }

        message = match.Groups["Message"].Value;
        timestamp = match.Groups["Timestamp"].Value;
        return true;
    }

    /// <summary>The shuffle is its own inverse: swap three pairs of characters in a block.</summary>
    private static void ShuffleBlock(char[] buffer, int start)
    {
        Swap(buffer, start, start + 11);
        Swap(buffer, start + 2, start + 9);
        Swap(buffer, start + 4, start + 7);
    }

    private static void Swap(char[] buffer, int i, int j)
    {
        char temp = buffer[i];
        buffer[i] = buffer[j];
        buffer[j] = temp;
    }
}
