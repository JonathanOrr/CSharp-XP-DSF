namespace CSDsf;

public class XPLNEraster
{
    public byte Version { get; set; }  // Version of Raster
    public byte BytesPerPixel { get; set; }  // Bytes per pixel in raster data
    public ushort Flags { get; set; }  // Flags about Raster centricity and data type used
    public uint Width { get; set; }  // Width of area in pixels
    public uint Height { get; set; }  // Height of area in pixels
    public float Scale { get; set; }  // Scale factor for height values
    public float Offset { get; set; }  // Offset for height values
    public List<List<double>> Data { get; set; }  // Stores final raster height values in a 2D list: [pixel x][pixel y]

    public XPLNEraster(byte[] info)
    {
        using (var ms = new MemoryStream(info))
        using (var br = new BinaryReader(ms))
        {
            Version = br.ReadByte();
            BytesPerPixel = br.ReadByte();
            Flags = br.ReadUInt16();
            Width = br.ReadUInt32();
            Height = br.ReadUInt32();
            Scale = br.ReadSingle();
            Offset = br.ReadSingle();
        }
        Data = [];
    }
}