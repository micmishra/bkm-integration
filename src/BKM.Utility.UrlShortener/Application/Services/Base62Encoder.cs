namespace BKM.Utility.Application.Features.UrlShortener.Services;

/// <summary>
/// Converts a long integer ID to a base62 string and back.
/// Characters: 0-9 A-Z a-z  (62 symbols)
/// 6 chars covers ~56 billion IDs; 8 chars covers ~218 trillion.
/// </summary>
public static class Base62Encoder
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Base = 62;

    /// <summary>Encodes a positive ID to a base62 string.</summary>
    public static string Encode(long id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "ID must be positive.");

        Span<char> buffer = stackalloc char[12];
        int pos = buffer.Length;

        while (id > 0)
        {
            buffer[--pos] = Alphabet[(int)(id % Base)];
            id /= Base;
        }

        return new string(buffer[pos..]);
    }

    /// <summary>Decodes a base62 string back to a long ID.</summary>
    public static long Decode(string code)
    {
        long result = 0;
        foreach (char c in code)
        {
            int digit = Alphabet.IndexOf(c);
            if (digit < 0) throw new FormatException($"Invalid base62 character: '{c}'");
            result = result * Base + digit;
        }
        return result;
    }
}
