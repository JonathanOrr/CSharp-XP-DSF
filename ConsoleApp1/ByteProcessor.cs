using System.Text;
using System.Text.RegularExpressions;

namespace CSDsf;

public class ByteProcessor
{
    public static object[] Unpack(string format, byte[] data, int startIndex, int length)
    {
        var result = new List<object>();
        int offset = startIndex;
        int endIndex = startIndex + length;

        // Use regex to match format specifiers with sizes (like "4s")
        var matches = Regex.Matches(format, @"(\d*)([bBhHiIlLfds])");

        foreach (Match match in matches)
        {
            string sizePart = match.Groups[1].Value;
            char specifier = match.Groups[2].Value[0];

            int size = string.IsNullOrEmpty(sizePart) ? 1 : int.Parse(sizePart);

            if (offset >= endIndex)
            {
                throw new ArgumentException("Insufficient data to unpack.");
            }

            switch (specifier)
            {
                case 'B': // Unsigned char (1 byte)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(data[offset]);
                        offset += 1;
                    }
                    break;

                case 'b': // Signed char (1 byte)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add((sbyte)data[offset]);
                        offset += 1;
                    }
                    break;

                case 'H': // Unsigned short (2 bytes)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToUInt16(data, offset));
                        offset += 2;
                    }
                    break;

                case 'h': // Signed short (2 bytes)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToInt16(data, offset));
                        offset += 2;
                    }
                    break;

                case 'I': // Unsigned int (4 bytes)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToUInt32(data, offset));
                        offset += 4;
                    }
                    break;

                case 'i': // Signed int (4 bytes)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToInt32(data, offset));
                        offset += 4;
                    }
                    break;

                case 'L': // Unsigned long (4 bytes) (same as 'I' in most cases)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToUInt32(data, offset));
                        offset += 4;
                    }
                    break;

                case 'l': // Signed long (4 bytes) (same as 'i' in most cases)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToInt32(data, offset));
                        offset += 4;
                    }
                    break;

                case 'f': // Float (4 bytes)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToSingle(data, offset));
                        offset += 4;
                    }
                    break;

                case 'd': // Double (8 bytes)
                    for (int i = 0; i < size; i++)
                    {
                        result.Add(BitConverter.ToDouble(data, offset));
                        offset += 8;
                    }
                    break;

                case 's': // String (size bytes)
                    int stringLength = Math.Min(size, endIndex - offset);
                    result.Add(Encoding.UTF8.GetString(data, offset, stringLength));
                    offset += stringLength;
                    break;

                default:
                    throw new ArgumentException($"Unknown format specifier: {specifier}");
            }
        }

        return result.ToArray();
    }

}