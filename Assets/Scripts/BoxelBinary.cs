using System;
using System.Collections.Generic;
using System.IO;

public static class BoxelBinary
{
    // Formato:
    // [int width][int height][byte[] dataBits]
    // dataBits contiene width*height bits en orden fila-major (x aumenta primero en tu código: x=i, y=j).
    public static void WriteBitPacked(string path, int width, int height, IList<int> types)
    {
        if (types == null) throw new ArgumentNullException(nameof(types));
        int total = width * height;
        int byteCount = (total + 7) / 8;
        byte[] bytes = new byte[byteCount];

        for (int idx = 0; idx < total; idx++)
        {
            int value = (idx < types.Count) ? (types[idx] & 1) : 0;
            if (value != 0)
            {
                int b = idx >> 3;
                int bit = idx & 7;
                bytes[b] |= (byte)(1 << bit);
            }
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);
        bw.Write(width);
        bw.Write(height);
        bw.Write(bytes.Length);
        bw.Write(bytes);
    }

    public static List<int> ReadBitPacked(string path, out int width, out int height)
    {
        width = 0;
        height = 0;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        width = br.ReadInt32();
        height = br.ReadInt32();
        int byteCount = br.ReadInt32();
        byte[] bytes = br.ReadBytes(byteCount);

        int total = width * height;
        var types = new List<int>(total);
        for (int idx = 0; idx < total; idx++)
        {
            int b = idx >> 3;
            int bit = idx & 7;
            bool bitSet = (bytes[b] & (1 << bit)) != 0;
            types.Add(bitSet ? 1 : 0);
        }

        return types;
    }
}