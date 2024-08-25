using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics; // For sin, cos, sqrt, atan2, radians
using System.Reflection;
using System.Text;

namespace CSDsf;


public class XPLNEDSF
{
    private const bool DEBUG = false;
    private int[] Progress { get; set; } = [0, 0, 0]; // Progress as an array [bytes read/written in percent and shown, read/written but not yet shown as number of bytes, total bytes to be processed]
    private Dictionary<string, List<byte[]>> Atoms { get; set; } = []; // Dictionary containing every atom in file with corresponding strings
    private readonly Dictionary<string, List<string>> AtomStructure = new()
    {
        { "DAEH", new List<string> { "PORP" } },
        { "NFED", new List<string> { "TRET", "TJBO", "YLOP", "WTEN", "NMED" } },
        { "DOEG", new List<string> { "LOOP", "LACS", "23OP", "23CS" } },
        { "SMED", new List<string> { "IMED", "DMED" } },
        { "SDMC", new List<string>() }
    };
    private readonly List<string> AtomList = ["DAEH", "NFED", "DOEG", "SMED", "SDMC", "PORP", "TRET", "TJBO", "YLOP", "WTEN", "NMED", "LOOP", "LACS", "23OP", "23CS", "IMED", "DMED"];
    private readonly List<string> AtomOfAtoms = ["DAEH", "NFED", "DOEG", "SMED"];
    private readonly List<string> MultiAtoms = ["LOOP", "LACS", "23OP", "23CS", "IMED", "DMED"]; // These Atoms can occur several times and are stored as lists in Atoms
    private readonly Dictionary<int, List<string>> CMDStructure = new()
    {
        { 1, new List<string> { "H" } },
        { 2, new List<string> { "L" } },
        { 3, new List<string> { "B" } },
        { 4, new List<string> { "H" } },
        { 5, new List<string> { "L" } },
        { 6, new List<string> { "B" } },
        { 7, new List<string> { "H" } },
        { 8, new List<string> { "HH" } },
        { 9, new List<string> { "", "B", "H" } },
        { 10, new List<string> { "HH" } },
        { 11, new List<string> { "", "B", "L" } },
        { 12, new List<string> { "H", "B", "H" } },
        { 13, new List<string> { "HHH" } },
        { 15, new List<string> { "H", "B", "H" } },
        { 16, new List<string> {""} },
        { 17, new List<string> { "B" } },
        { 18, new List<string> { "Bff" } },
        { 23, new List<string> { "", "B", "H" } },
        { 24, new List<string> { "", "B", "HH" } },
        { 25, new List<string> { "HH" } },
        { 26, new List<string> { "", "B", "H" } },
        { 27, new List<string> { "", "B", "HH" } },
        { 28, new List<string> { "HH" } },
        { 29, new List<string> { "", "B", "H" } },
        { 30, new List<string> { "", "B", "HH" } },
        { 31, new List<string> { "HH" } },
        { 32, new List<string> { "", "B", "c" } },
        { 33, new List<string> { "", "H", "c" } },
        { 34, new List<string> { "", "L", "c" } }
    };
    private readonly Dictionary<int, List<int>> CMDStructLen = new()
    {
        { 1, new List<int> { 2 } },
        { 2, new List<int> { 4 } },
        { 3, new List<int> { 1 } },
        { 4, new List<int> { 2 } },
        { 5, new List<int> { 4 } },
        { 6, new List<int> { 1 } },
        { 7, new List<int> { 2 } },
        { 8, new List<int> { 4 } },
        { 9, new List<int> { 0, 1, 2 } },
        { 10, new List<int> { 4 } },
        { 11, new List<int> { 0, 1, 4 } },
        { 12, new List<int> { 2, 1, 2 } },
        { 13, new List<int> { 6 } },
        { 15, new List<int> { 2, 1, 2 } },
        { 16, new List<int> { 0 } },
        { 17, new List<int> { 1 } },
        { 18, new List<int> { 9 } },
        { 23, new List<int> { 0, 1, 2 } },
        { 24, new List<int> { 0, 1, 4 } },
        { 25, new List<int> { 4 } },
        { 26, new List<int> { 0, 1, 2 } },
        { 27, new List<int> { 0, 1, 4 } },
        { 28, new List<int> { 4 } },
        { 29, new List<int> { 0, 1, 2 } },
        { 30, new List<int> { 0, 1, 4 } },
        { 31, new List<int> { 4 } },
        { 32, new List<int> { 0, 1, 1 } },
        { 33, new List<int> { 0, 2, 1 } },
        { 34, new List<int> { 0, 4, 1 } }
    };
    public string FileHash { get; set; } = ""; // Hash value of dsf file read
    public List<List<object>> CMDS { get; set; } // Unpacked commands
    public List<XPLNEpatch> Patches { get; set; } // Mesh patches, list of objects of class XPLNEpatch
    public List<List<List<double>>> V { get; set; } // 3D list of all vertices V[PoolID][Vertex][xyz etc coordinates]
    public List<List<List<double>>> V32 { get; set; } // Same as V but for 32-bit coordinates
    public List<List<List<float>>> Scalings { get; set; } // 3D list of all scale multipliers and offsets for all pools and planes
    public List<List<List<float>>> Scal32 { get; set; } // Same as Scalings but for vertices with 32-bit coordinates
    public List<XPLNEraster> Raster { get; set; } // Raster layers of file
    public List<List<List<object>>> Polygons { get; set; } // For each Polygon Definition a list of PoolId, CommandData values of Polygon
    public List<List<List<object>>> Objects { get; set; } // For each Object Definition a list of PoolId, CommandData values of Object
    public List<List<List<object>>> Networks { get; set; } // List of network junks with road subtype, Junction offset, PoolIndex, and commands
    public Dictionary<string, string> Properties { get; set; } // Properties of the dsf file stored in HEAD -> PROP
    public Dictionary<int, string> DefTerrains { get; set; } // Dictionary containing for each index number the name of Terrain definition file
    public Dictionary<int, string> DefObjects { get; set; } // Dictionary containing for each index number the name of Object definition file
    public Dictionary<int, string> DefPolygons { get; set; } // Dictionary containing for each index number the name of Polygon definition file
    public Dictionary<int, string> DefNetworks { get; set; } // Dictionary containing for each index number the name of Network definition file
    public Dictionary<int, string> DefRasters { get; set; } // Dictionary containing for each index number the name of Raster definition

    public XPLNEDSF()
    {
        CMDS = [];
        Patches = [];
        V = [];
        V32 = [];
        Scalings = [];
        Scal32 = [];
        Raster = [];
        Polygons = [];
        Objects = [];
        Networks = [];
        Properties = [];
        DefTerrains = [];
        DefObjects = [];
        DefPolygons = [];
        DefNetworks = [];
        DefRasters = [];
        Console.WriteLine("Class XPLNEDSF initialized.");
    }

    private void UpdateProgress(long bytes)
    {
        Progress[1] += (int)bytes;
        if (Progress[2] <= 0) return; // Size to be reached not set; no sense to track progress
        int currentProgress = 100 * Progress[1] / Progress[2];
        if (currentProgress > 0)
        {
            Progress[0] += currentProgress;
            if (Progress[0] > 100)
            {
                Progress[0] = 100; // Stop at 100%
                Progress[1] = 0;
            }
            else
            {
                Progress[1] -= currentProgress * Progress[2] / 100;
            }

            Console.Write($"[{Progress[0]}%]");
        }
    }

    private int TopAtomLength(string id)
    {
        int length = 0;

        foreach (var subId in AtomStructure[id])
        {
            if (MultiAtoms.Contains(subId))
            {
                if (Atoms.TryGetValue(subId, out List<byte[]>? atom))
                {
                    foreach (var atomStr in atom)
                    {
                        length += atomStr.Length + 8; // Add 8 bytes for each AtomID + Length header
                    }
                }
            }
            else
            {
                if (Atoms.TryGetValue(subId, out List<byte[]>? atom))
                {
                    length += atom.First().Length + 8; // Add 8 bytes for AtomID + Length header
                }
            }
        }


        return length + 8; // Add 8 bytes for the header of the TopAtom itself
    }

    private static List<string> GetStrings(List<byte[]> atom)
    {
        Debug.Assert(atom.Count == 1, "something went wrong this type of atom shouldn't summon this method");
        byte[] atomByteData = atom[0];

        var strings = new List<string>();
        int i = 0;

        while (i < atomByteData.Length)
        {
            int j = i;
            while (j < atomByteData.Length && atomByteData[j] != 0)
            {
                j++;
            }
            strings.Add(Encoding.UTF8.GetString(atomByteData, i, j - i));
            i = j + 1;
        }

        return strings;
    }

    private void ExtractProps()
    {
        List<string> AtomStrings = GetStrings(Atoms["PORP"]); // Assuming 'PROP' is the key for the properties
        for (int i = 0; i < AtomStrings.Count; i += 2)
        {
            Properties[AtomStrings[i]] = AtomStrings[i + 1]; // The list contains property (dictionary key) and values one after each other
        }
    }

    private void ExtractDefs()
    {
        if (Atoms.ContainsKey("TRET"))
        {
            var AtomStrings = GetStrings(Atoms["TRET"]);
            DefTerrains = AtomStrings.Select((value, index) => new { value, index })
                            .ToDictionary(x => x.index, x => x.value);
        }
        else
        {
            Console.WriteLine("This dsf file has no TRET atom (Terrain Definitions)!");
        }

        if (Atoms.ContainsKey("TJBO"))
        {
            var AtomStrings = GetStrings(Atoms["TJBO"]);
            DefObjects = AtomStrings.Select((value, index) => new { value, index })
                          .ToDictionary(x => x.index, x => x.value);
        }
        else
        {
            Console.WriteLine("This dsf file has no TJBO atom (Object Definitions)!");
        }

        if (Atoms.ContainsKey("YLOP"))
        {
            var AtomStrings = GetStrings(Atoms["YLOP"]);
            DefPolygons = AtomStrings.Select((value, index) => new { value, index })
                           .ToDictionary(x => x.index, x => x.value);
        }
        else
        {
            Console.WriteLine("This dsf file has no YLOP atom (Polygon Definitions)!");
        }

        if (Atoms.ContainsKey("WTEN"))
        {
            var AtomStrings = GetStrings(Atoms["WTEN"]);
            DefNetworks = AtomStrings.Select((value, index) => new { value, index })
                           .ToDictionary(x => x.index, x => x.value);
        }
        else
        {
            Console.WriteLine("This dsf file has no WTEN atom (Network Definitions)!");
        }

        if (Atoms.ContainsKey("NMED"))
        {
            var AtomStrings = GetStrings(Atoms["NMED"]);
            DefRasters = AtomStrings.Select((value, index) => new { value, index })
                          .ToDictionary(x => x.index, x => x.value);
        }
        else
        {
            Console.WriteLine("This dsf file has no NMED atom (Raster Definitions)!");
        }

        UpdateProgress(TopAtomLength("NFED"));
    }

    private void ExtractPools(int bit = 16)
    {
        List<byte[]> atomString;
        // Define a delegate that matches the signature
        Func<byte[], int, uint> byteConverter;
        int size;
        ulong maxInt;
        List<List<List<double>>> Vref;

        if (bit == 32)
        {
            Console.WriteLine($"Start to unpack and extract {Atoms["23OP"].Count} pools ({bit} bit)...");
            atomString = Atoms["23OP"];
            Vref = V32;
            byteConverter = BitConverter.ToUInt32;
            size = 4; // bytes read per coordinate in 32 bit pool
            maxInt = 4294967296;
        }
        else
        {
            Console.WriteLine($"Start to unpack and extract {Atoms["LOOP"].Count} pools ({bit} bit)...");
            atomString = Atoms["LOOP"];
            Vref = V;
            byteConverter = (bytes, startIndex) => BitConverter.ToUInt16(bytes, startIndex);
            size = 2; // bytes read per coordinate in 16 bit pool
            maxInt = 65536;
        }

        foreach (var s in atomString)
        {
            var nArrays = BitConverter.ToUInt32(s.Take(4).ToArray(), 0); // number of vertices to read
            var nPlanes = s[4]; // number of places to read
            if (DEBUG) Console.WriteLine($"Pool number {Vref.Count} has {nArrays} Arrays (vertices) with {nPlanes} Planes (coordinates per vertex)!");

            Vref.Add([]);
            for (int i = 0; i < nArrays; i++)
            {
                Vref[^1].Add([]);
            }

            int pos = 5; // position in string s
            for (int n = 0; n < nPlanes; n++)
            {
                var encType = s[pos];
                pos++;
                if (DEBUG) Console.WriteLine($"Plane {n} is encoded: {encType}");

                if (encType < 0 || encType > 3) // encoding not defined
                {
                    Debug.Assert(false, "Stop reading pool because unknown encoding of plane found!!!");
                }

                int i = 0; // counts how many arrays = vertices have been read in plane n
                while (i < nArrays)
                {
                    int runLength;
                    if (encType >= 2)
                    {
                        runLength = s[pos];
                        pos++;
                    }
                    else
                    {
                        runLength = 1; // just read single values until end of this plane
                    }

                    if (runLength > 127)
                    {
                        uint v = byteConverter(s.Skip(pos).Take(size).ToArray(), 0); // repeated value
                        pos += size;
                        runLength -= 128; // only value without 8th bit gives now the number of repetitions
                        while (runLength > 0)
                        {
                            Vref[^1][i].Add(v);
                            runLength--;
                            i++;
                        }
                    }
                    else
                    {
                        while (runLength > 0)
                        {
                            uint v = byteConverter(s.Skip(pos).Take(size).ToArray(), 0); // repeated value
                            pos += size;
                            Vref[^1][i].Add(v);
                            runLength--;
                            i++;
                        }
                    }
                }

                if (encType == 1 || encType == 3)
                {
                    for (int j = 1; j < nArrays; j++)
                    {
                        Vref[^1][j][n] = (Vref[^1][j][n] + Vref[^1][j - 1][n]) % maxInt;
                    }
                }
            }
            UpdateProgress(s.Length);
        }
    }

    private void ExtractScalings(int bit = 16)
    {
        Console.WriteLine($"Start to unpack and extract all scalings of {bit} bit pools.");

        List<List<List<float>>> scalingsRef;
        List<byte[]> atomString;

        if (bit == 32) // 32 bit scaling pool
        {
            atomString = Atoms["23CS"];
            scalingsRef = Scal32;
        }
        else // 16 bit scaling pool
        {
            atomString = Atoms["LACS"];
            scalingsRef = Scalings;
        }

        foreach (var s in atomString)
        {
            scalingsRef.Add([]);
            int length = s.Length;

            for (int i = 0; i < length / 8; i++)
            {
                float multiplier = BitConverter.ToSingle(s, i * 8);
                float offset = BitConverter.ToSingle(s, i * 8 + 4);
                scalingsRef[^1].Add([multiplier, offset]);
            }
        }
    }

    private int ScaleV(int bit = 16, bool reverse = false)
    {
        if (reverse)
        {
            Console.WriteLine($"Start to de-scale all {bit} bit pools.");
        }
        else
        {
            Console.WriteLine($"Start to scale all {bit} bit pools.");
        }

        List<List<List<double>>> Vref;
        List<List<List<float>>> scalingsRef;
        double maxInt;

        if (bit == 32)
        {
            Vref = V32;
            scalingsRef = Scal32;
            maxInt = 4294967295; // 2^32 - 1
        }
        else
        {
            Vref = V;
            scalingsRef = Scalings;
            maxInt = 65535; // 2^16 - 1
        }

        if (Vref.Count != scalingsRef.Count)
        {
            Console.WriteLine("Amount of Scale atoms does not equal amount of Pools!!");
            return 1;
        }

        for (int p = 0; p < Vref.Count; p++)
        {
            if (Vref[p].Count == 0)
            {
                Console.WriteLine($"Empty pool number {p} not scaled!");
                continue; // test break statment. seems like all after the first occurance is the same
            }

            if (Vref[p][0].Count != scalingsRef[p].Count)
            {
                Console.WriteLine($"Amount of scale values for pool {p} does not equal the number of coordinate planes!!!");
                return 2;
            }

            for (int n = 0; n < scalingsRef[p].Count; n++)
            {
                if (DEBUG)
                {
                    Console.WriteLine($"Will now scale pool {p} plane {n} with multiplier: {scalingsRef[p][n][0]} and offset: {scalingsRef[p][n][1]}");
                }

                if (scalingsRef[p][n][0] == 0.0)
                {
                    if (DEBUG)
                    {
                        Console.WriteLine("   Plane will not be scaled because scale is 0!");
                    }
                    continue; // test break statment. seems like all after the first occurance is the same
                }

                for (int v = 0; v < Vref[p].Count; v++)
                {
                    if (reverse)
                    {
                        Vref[p][v][n] = Math.Round((Vref[p][v][n] - scalingsRef[p][n][1]) * maxInt / scalingsRef[p][n][0]);
                    }
                    else
                    {
                        Vref[p][v][n] = (Vref[p][v][n] * scalingsRef[p][n][0] / maxInt) + scalingsRef[p][n][1];
                    }
                }
            }
        }

        return 0;
    }

    private int ExtractRaster()
    {
        Console.WriteLine($"Extracting {Atoms["IMED"].Count} raster layers...");

        if (Atoms["IMED"].Count != Atoms["DMED"].Count)
        {
            Console.WriteLine("Number of raster info atoms not equal to number of raster data atoms!!!");
            return 1;
        }

        for (int rn = 0; rn < Atoms["IMED"].Count; rn++)
        {
            XPLNEraster R = new(Atoms["IMED"][rn]);
            Console.WriteLine($"Info of new raster layer: {R.Version} {R.BytesPerPixel} {R.Flags} {R.Width} {R.Height} {R.Scale} {R.Offset}");

            string ctype;
            if ((R.Flags & 1) != 0) // signed integers
            {
                ctype = R.BytesPerPixel switch
                {
                    1 => "<b",
                    2 => "<h",
                    4 => "<i",
                    _ => throw new Exception("Not allowed bytes per pixel in Raster Definition!!!")
                };
            }
            else if ((R.Flags & 2) != 0) // unsigned integers
            {
                ctype = R.BytesPerPixel switch
                {
                    1 => "<B",
                    2 => "<H",
                    4 => "<I",
                    _ => throw new Exception("Not allowed bytes per pixel in Raster Definition!!!")
                };
            }
            else if (R.BytesPerPixel == 4) // 4-byte float
            {
                ctype = "<f";
            }
            else
            {
                Console.WriteLine("Not allowed bytes per pixel in Raster Definition!!!");
                return 4;
            }

            for (int x = 0; x < R.BytesPerPixel * R.Width; x += R.BytesPerPixel)
            {
                var line = new List<double>();
                for (int y = 0; y < R.BytesPerPixel * R.Height * R.Width; y += R.BytesPerPixel * (int)R.Width)
                {
                    double v = UnpackValue(ctype, Atoms["DMED"][rn], y + x, R.BytesPerPixel);
                    v = v * R.Scale + R.Offset; // Apply scale and offset
                    line.Add(v);
                }
                R.Data.Add(line);
                UpdateProgress(R.BytesPerPixel * R.Width); // Update progress with number of bytes per raster line
            }

            Raster.Add(R);
        }

        Console.WriteLine("Finished extracting Rasters.");
        return 0;
    }


    private double UnpackValue(string ctype, byte[] data, int index, int length)
    {
        using (var ms = new MemoryStream(data, index, length))
        using (var br = new BinaryReader(ms))
        {
            return ctype switch
            {
                "<b" => br.ReadSByte(),
                "<h" => br.ReadInt16(),
                "<i" => br.ReadInt32(),
                "<B" => br.ReadByte(),
                "<H" => br.ReadUInt16(),
                "<I" => br.ReadUInt32(),
                "<f" => br.ReadSingle(),
                _ => throw new Exception("Unknown type in UnpackValue")
            };
        }
    }

    private void UnpackCMDS()
    {
        int i = 0; // position in CMDS string
        Console.WriteLine("Start unpacking of Commands.");
        int current100kBjunk = 1; // counts processed bytes in 100kB chunks

        while (i < Atoms["SDMC"][0].Length)
        {
            byte id = Atoms["SDMC"][0][i];
            CMDS.Add([id]);
            i++;

            if (CMDStructure.ContainsKey(id))
            {
                int l = CMDStructLen[id][0]; // length of bytes to read
                if (l > 0)
                {
                    var y = ByteProcessor.Unpack(CMDStructure[id][0], Atoms["SDMC"][0], i, l);
                    CMDS.Last().AddRange(y);
                    i += l;
                }

                if (CMDStructLen[id].Count == 3) // read command with variable length
                {
                    l = CMDStructLen[id][1]; // length of repeating value n to read
                    var n = ByteProcessor.Unpack(CMDStructure[id][1], Atoms["SDMC"][0], i, l).First();
                    int nint = Convert.ToInt32(n);
                    if (id == 15)
                    {
                        nint++; // id = 15 special case with one more index than windings
                    }
                    i += l;

                    l = CMDStructLen[id][2]; // length of repeated bytes
                    for (int j = 0; j < nint; j++)
                    {
                        var repeatedValue = ByteProcessor.Unpack(CMDStructure[id][2], Atoms["SDMC"][0], i, l);
                        CMDS.Last().AddRange(repeatedValue);
                        i += l;
                    }
                }
            }
            else if (id == 14) // special double packed case with lists inside commands
            {
                var parameters = ByteProcessor.Unpack("HB", Atoms["SDMC"][0], i, 3);
                i += 3;
                CMDS.Last().AddRange(parameters.Take(1)); // Add parameter only

                int windings = (int)parameters.ToList()[1];
                for (int w = 0; w < windings; w++)
                {
                    byte indices = Atoms["SDMC"][0][i];
                    i++;
                    var windingList = new List<int>();

                    for (int m = 0; m < indices; m++)
                    {
                        var y = ByteProcessor.Unpack("H", Atoms["SDMC"][0], i, 2).First();
                        i += 2;
                        windingList.Add((int)y);
                    }
                    CMDS.Last().Add(windingList);
                }
            }
            else
            {
                Console.WriteLine($"Unknown command ID {id} ignored!");
                CMDS.RemoveAt(CMDS.Count - 1); // delete already written id
            }

            if (DEBUG)
            {
                Console.WriteLine($"CMD id {CMDS.Last()[0]}: {string.Join(", ", CMDS.Last().Skip(1))} (string pos next cmd: {i})");
            }

            if (i > current100kBjunk * 100000)
            {
                UpdateProgress(50000); // count only half of the length, other half by extractCMDS
                current100kBjunk++;
            }
        }

        Console.WriteLine($"{CMDS.Count} commands have been unpacked.");
    }

    private void ExtractCMDS()
    {
        Console.WriteLine("Start to extract CMDS");

        // Initialize lists for polygons and objects based on their definitions
        for (int i = 0; i < DefPolygons.Count; i++)
        {
            Polygons.Add([]);
        }
        for (int i = 0; i < DefObjects.Count; i++)
        {
            Objects.Add([]);
        }

        int? patchPoolIndex = null;  // PoolIndex currently used in current patch
        int? flag_physical = null;   // 1 if physical, 2 if overlay
        float? nearLOD = null;
        float? farLOD = null;
        int poolIndex = 0;
        int defIndex = 0;
        int subroadtype = 0;
        int junctionoffset = 0;
        int counter = 0;
        int amount_of_two_percent_CMDS = Math.Max(CMDS.Count / 50, 1); // Avoid division by zero

        foreach (dynamic c in CMDS)
        {
            switch ((int)c[0])
            {
                case 1: // New pool selection
                    poolIndex = (int)c[1];
                    break;
                case 2: // New junction offset
                    junctionoffset = (int)c[1];
                    break;
                case int cmd when cmd >= 3 && cmd <= 5: // New definition index
                    defIndex = (int)c[1];
                    break;
                case 6: // New subtype for road
                    subroadtype = (int)c[1];
                    break;
                case int cmd when cmd >= 7 && cmd <= 8: // Object Command
                    Objects[defIndex].Add([poolIndex]);
                    Objects[defIndex].Last().AddRange(c);
                    break;
                case int cmd when cmd >= 9 && cmd <= 11: // Network Commands
                    if (Networks.Count == 0)
                    {
                        Networks.Add([[subroadtype, junctionoffset, poolIndex]]);
                    }
                    else if ((int)Networks.Last()[0][0] != subroadtype ||
                             (int)Networks.Last()[0][1] != junctionoffset ||
                             (int)Networks.Last()[0][2] != poolIndex)
                    {
                        Networks.Add([[subroadtype, junctionoffset, poolIndex]]);
                    }
                    Networks.Last().Add(c);
                    break;
                case int cmd when cmd >= 12 && cmd <= 15: // Polygon Commands
                    if (Polygons.Count > defIndex)
                    {
                        Polygons[defIndex].Add([poolIndex]);
                        Polygons[defIndex].Last().AddRange(c);
                    }
                    else
                    {
                        Console.WriteLine($"dsf file includes polygon with defindex {defIndex} that was not defined. Polygon is ignored.");
                    }
                    break;
                case int cmd when cmd >= 16 && cmd <= 18: // Add new Terrain Patch
                    patchPoolIndex = null;
                    if ((int)c[0] == 17) flag_physical = (int)c[1];
                    if ((int)c[0] == 18)
                    {
                        flag_physical = (int)c[1];
                        nearLOD = (float)c[2];
                        farLOD = (float)c[3];
                    }
                    var patch = new XPLNEpatch(flag_physical, nearLOD, farLOD, defIndex);
                    Patches.Add(patch);
                    break;
                case int cmd when cmd >= 23 && cmd <= 31: // Patch Command
                    if (patchPoolIndex != poolIndex)
                    {
                        Patches.Last().Cmds.Add([1, poolIndex]);
                        patchPoolIndex = poolIndex;
                    }
                    if (Patches.Last().DefIndex != defIndex)
                    {
                        Console.WriteLine("Definition Index changed within patch. Aborted command extraction!");
                        return;
                    }
                    Patches.Last().Cmds.Add(c);
                    break;
            }

            counter++;
            if (counter % amount_of_two_percent_CMDS == 0)
            {
                UpdateProgress((int)Math.Round((double)Atoms["SDMC"][0].Length / 100));
            }
        }

        Console.WriteLine($"{Patches.Count} patches extracted from commands.");
        Console.WriteLine($"{Polygons.Count} different Polygon types including their definitions extracted from commands.");
        Console.WriteLine($"{Objects.Count} different Objects with placement coordinates extracted from commands.");
        Console.WriteLine($"{Networks.Count} different Network subtypes extracted from commands (could include double count).");
    }


    private int UnpackAtoms()
    {
        Console.WriteLine("Extracting properties and definitions.");

        if (Atoms.ContainsKey("PORP"))
        {
            ExtractProps();
        }
        else
        {
            Console.WriteLine("This DSF file has no properties defined!");
        }

        if (Atoms.ContainsKey("NFED"))
        {
            ExtractDefs();
        }
        else
        {
            Console.WriteLine("This DSF file has no definitions.");
        }

        if (Atoms.ContainsKey("IMED"))
        {
            ExtractRaster();
        }
        else
        {
            Console.WriteLine("This DSF file has no raster layers.");
        }

        if (Atoms.ContainsKey("LOOP"))
        {
            ExtractPools(16);
            ExtractScalings(16);
            ScaleV(16, false); // False means scaling is not reversed
            UpdateProgress(Atoms["LACS"].Count);
        }
        else
        {
            Console.WriteLine("This DSF file has no coordinate pools (16-bit) defined!");
        }

        if (Atoms.ContainsKey("23OP"))
        {
            ExtractPools(32);
            ExtractScalings(32);
            ScaleV(32, false); // False means scaling is not reversed
            UpdateProgress(Atoms["23CS"].Count);
        }
        else
        {
            Console.WriteLine("This DSF file has no 32-bit pools.");
        }

        if (Atoms.ContainsKey("SDMC"))
        {
            UnpackCMDS();
            ExtractCMDS();
        }
        else
        {
            Console.WriteLine("This DSF file has no commands defined.");
        }

        return 0;
    }

    public double? GetVertexElevation(double x, double y, double z = -32768)
    {
        if ((int)z != -32768) // If the z vertex is different from -32768, then this is the correct height and not taken from the raster
        {
            return z;
        }

        if (!Properties.ContainsKey("sim/west"))
        {
            Console.WriteLine("Cannot get elevation as properties like sim/west are not defined!!!");
            return null;
        }

        if (!(int.Parse(Properties["sim/west"]) <= x && x <= int.Parse(Properties["sim/east"])))
        {
            Console.WriteLine("Cannot get elevation as x coordinate is not within boundaries!!!");
            return null;
        }

        if (!(int.Parse(Properties["sim/south"]) <= y && y <= int.Parse(Properties["sim/north"])))
        {
            Console.WriteLine("Cannot get elevation as y coordinate is not within boundaries!!!");
            return null;
        }

        if (DefRasters.Count == 0) // No raster defined, use elevation from trias
        {
            Console.WriteLine("GetVertexElevation: dsf includes no raster, elevation returned is None");
            return null;
        }
        else // Use raster to get elevation; this version assumes that elevation raster is the first raster layer (index 0)
        {
            if (DefRasters[0] != "elevation")
            {
                Console.WriteLine("Warning: The first raster layer is not called elevation, but used to determine elevation!");
            }

            x = Math.Abs(x - int.Parse(Properties["sim/west"])) * (Raster[0].Width - 1); // -1 from width required because pixels cover the boundaries of dsf lon/lat grid
            y = Math.Abs(y - int.Parse(Properties["sim/south"])) * (Raster[0].Height - 1); // -1 from height required because pixels cover the boundaries of dsf lon/lat grid

            if ((Raster[0].Flags & 4) != 0) // When bit 4 is set, then the data is stored post-centric, meaning the center of the pixel lies on the dsf-boundaries, rounding should apply
            {
                x = Math.Round(x, 0);
                y = Math.Round(y, 0);
            }

            int ix = (int)x; // For point-centric, the outer edges of the pixels lie on the boundary of dsf, and just cutting to int should be right
            int iy = (int)y;

            return Raster[0].Data[ix][iy];
        }
    }

    public int Read(string file)
    {
        if (!File.Exists(file))
        {
            Console.WriteLine($"File does not exist: {file}");
            return 1;
        }

        long flength = new FileInfo(file).Length; // Length of dsf-file
        Progress = [0, 0, (int)flength]; // Initialize progress start for reading

        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
        {
            Console.WriteLine($"Opened file {file} with {flength} bytes.");

            // if (start.Take(7).SequenceEqual(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }))
            // {
            //     // Handle 7Zip file extraction
            //     Console.WriteLine("File is 7Zip archive.");
            //     return -1;
            // try
            // {
            //     using (var archive = new SevenZip.SevenZipArchive(fs))
            //     {
            //         var entry = archive.Entries.First();
            //         using (var entryStream = entry.Open())
            //         using (var memoryStream = new MemoryStream())
            //         {
            //             entryStream.CopyTo(memoryStream);
            //             byte[] fileData = memoryStream.ToArray();
            //             Console.WriteLine($"Extracted and read file {entry.FullName} from archive with decompressed length {fileData.Length}.");
            //             fs.Close(); // Close the original stream
            //             fs.Dispose();
            //             fs. = new FileStream(file, FileMode.Create, FileAccess.ReadWrite);
            //             fs.Write(fileData, 0, fileData.Length);
            //             fs.Position = 0; // Reset position to start reading from beginning
            //         }
            //     }
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"Failed to extract 7Zip archive: {ex.Message}");
            //     return 2;
            // }
            // }

            byte[] start = new byte[12];
            fs.Read(start, 0, 12);
            string identifier = Encoding.UTF8.GetString(start.Take(8).ToArray());
            uint version = BitConverter.ToUInt32(start.Skip(8).Take(4).ToArray(), 0);

            if (identifier != "XPLNEDSF" || version != 1)
            {
                Console.WriteLine(fs.Position);
                Console.WriteLine("File is not a valid X-Plane dsf-file Version 1!");
                return 3;
            }

            while (fs.Position < flength - 16)
            {
                byte[] bytes = new byte[8];
                fs.Read(bytes, 0, 8);
                string atomID = Encoding.UTF8.GetString(bytes.Take(4).ToArray());
                uint atomLength = BitConverter.ToUInt32(bytes.Skip(4).Take(4).ToArray(), 0);

                if (AtomStructure.ContainsKey(atomID))
                {
                    if (DEBUG) Console.WriteLine($"Reading top-level atom {atomID} with length of {atomLength} bytes.");
                }
                else
                {
                    if (DEBUG) Console.WriteLine($"Reading atom {atomID} with length of {atomLength} bytes.");
                }

                if (AtomOfAtoms.Contains(atomID))
                {
                    Atoms[atomID] = []; // Just keep notice in dictionary that atom of atoms was read
                }
                else if (AtomList.Contains(atomID))
                {
                    byte[] atomData = new byte[atomLength - 8];
                    fs.Read(atomData, 0, (int)(atomLength - 8)); // Length includes 8 bytes header
                    if (Atoms.ContainsKey(atomID))
                    {
                        Atoms[atomID].Add(atomData); // Append data to existing list
                    }
                    else
                    {
                        Atoms[atomID] = [atomData]; // Create new list entry for multiple atoms
                    }
                }
                else
                {
                    Console.WriteLine($"Jumping over unknown Atom ID (reversed): {atomID} with length {atomLength}!!");
                    byte[] skipBytes = new byte[atomLength - 8];
                    fs.Read(skipBytes, 0, (int)(atomLength - 8));
                }
            }

            byte[] FileHashBytes = new byte[16];
            fs.Read(FileHashBytes, 0, 16);
            FileHash = BitConverter.ToString(FileHashBytes);
            if (DEBUG) Console.WriteLine($"Reached FOOTER with Hash-Value: {FileHash}");

            Console.WriteLine("Finished pure file reading.");
        }

        UnpackAtoms();
        return 0; // File successfully read
    }
}