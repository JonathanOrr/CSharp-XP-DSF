// See https://aka.ms/new-console-template for more information
using CSDsf;

//Console.WriteLine("Hello, World!");

XPLNEDSF dsf = new XPLNEDSF();
dsf.Read("D:/SteamLibrary/steamapps/common/X-Plane 12/Global Scenery/X-Plane 12 Global Scenery/Earth nav data/+40+010/+47+011.dsf");

const int AREA_W = 0;
const int AREA_E = 1;
const int AREA_S = 0;
const int AREA_N = 1;
const int SCALING = 1000;

Console.WriteLine("------------ Starting to transform DSF ------------------");

int gridWest = int.Parse(dsf.Properties["sim/west"]);
int gridSouth = int.Parse(dsf.Properties["sim/south"]);
Console.WriteLine($"Importing Mesh and setting west={gridWest} and south={gridSouth} to origin.");

int areaWest = AREA_W;
int areaEast = AREA_E;
int areaSouth = AREA_S;
int areaNorth = AREA_N;

if (0 <= AREA_W && AREA_W <= 1 && 0 <= AREA_S && AREA_S <= 1)
{
    areaWest += gridWest;
    areaEast += gridWest;
    areaSouth += gridSouth;
    areaNorth += gridSouth;
}

Console.WriteLine($"But extracting just from west {areaWest} to east {areaEast} and south {areaSouth} to north {areaNorth}");

// Sorting mesh patches
var terLayers = new Dictionary<(int?, int?, float?, float?), List<XPLNEpatch>>();
foreach (var p in dsf.Patches)
{
    var terType = (p.Flag, p.DefIndex, p.Near, p.Far);
    if (!terLayers.ContainsKey(terType))
        terLayers[terType] = new List<XPLNEpatch>();
    terLayers[terType].Add(p);
}

Console.WriteLine($"Sorted {dsf.Patches.Count} mesh patches into {terLayers.Count} different types");

var verts = new List<List<double>>();
var edges = new List<int>();  // Not filled as Blender takes in case of empty edges the edges from the faces
var faces = new List<List<int>>();
var uvs = new List<(double, double)>();
var coords = new Dictionary<(double, double), int>();
var trisIsWater = new List<bool>();

foreach (var terLayerId in terLayers.Keys.OrderBy(k => k.Item1).ThenBy(k => k.Item2))
{
    if (terLayerId.Item1 != 1) // only read base mesh
        continue;  // skip all overlays

    bool projectedUv = false;
    bool water = dsf.DefTerrains[(int)terLayerId.Item2] == "terrain_Water";

    foreach (var p in terLayers[terLayerId])
    {
        var trias = p.Triangles();

        if (water && trias.Count > 0 && dsf.V[trias[0][0][0]][trias[0][0][1]].Count <= 5)
        {
            projectedUv = true;
        }
        else
        {
            projectedUv = false;
        }

        foreach (var t in trias)
        {
            if (!(areaWest <= dsf.V[t[0][0]][t[0][1]][0] && dsf.V[t[0][0]][t[0][1]][0] <= areaEast &&
                  areaSouth <= dsf.V[t[0][0]][t[0][1]][1] && dsf.V[t[0][0]][t[0][1]][1] <= areaNorth) &&
                !(areaWest <= dsf.V[t[1][0]][t[1][1]][0] && dsf.V[t[1][0]][t[1][1]][0] <= areaEast &&
                  areaSouth <= dsf.V[t[1][0]][t[1][1]][1] && dsf.V[t[1][0]][t[1][1]][1] <= areaNorth) &&
                !(areaWest <= dsf.V[t[2][0]][t[2][1]][0] && dsf.V[t[2][0]][t[2][1]][0] <= areaEast &&
                  areaSouth <= dsf.V[t[2][0]][t[2][1]][1] && dsf.V[t[2][0]][t[2][1]][1] <= areaNorth))
            {
                continue;
            }

            var ti = new List<int>();  // index list of vertices of tria that will be added to faces
            var tuvs = new List<(double, double)>();  // uvs for that triangle

            foreach (var v in t)
            {
                double vx = Math.Round((dsf.V[v[0]][v[1]][0] - gridWest) * SCALING, 3);
                double vy = Math.Round((dsf.V[v[0]][v[1]][1] - gridSouth) * SCALING, 3);
                double vz = (double)dsf.GetVertexElevation(dsf.V[v[0]][v[1]][0], dsf.V[v[0]][v[1]][1], dsf.V[v[0]][v[1]][2]);
                vz = Math.Round(vz / (100000.0 / SCALING), 3);

                int vi;
                if (coords.ContainsKey((vx, vy)))
                {
                    vi = coords[(vx, vy)];
                }
                else
                {
                    vi = coords.Count;
                    coords[(vx, vy)] = vi;
                    verts.Add(new List<double> { vx, vy, vz });
                }

                ti.Insert(0, vi);

                if (dsf.V[v[0]][v[1]].Count == 7)
                {
                    if (!projectedUv && p.Flag == 1)
                    {
                        tuvs.Insert(0, (dsf.V[v[0]][v[1]][5], dsf.V[v[0]][v[1]][6]));
                    }
                    else
                    {
                        tuvs.Insert(0, (vx / 100, vy / 100));
                    }
                }
                else if (dsf.V[v[0]][v[1]].Count == 9)
                {
                    tuvs.Insert(0, (dsf.V[v[0]][v[1]][5], dsf.V[v[0]][v[1]][6]));
                }
                else
                {
                    tuvs.Insert(0, (vx / 100, vy / 100));
                }
            }

            faces.Add(ti);
            uvs.AddRange(tuvs);
            trisIsWater.Add(water);
        }
    }
}

using (StreamWriter writer = new StreamWriter("C:/Users/Jonathan/Desktop/test.stl"))
{
    writer.WriteLine("solid model");

    foreach (var face in faces)
    {
        // Get the vertices for this face
        var v1 = verts[face[0]];
        var v2 = verts[face[1]];
        var v3 = verts[face[2]];

        // Calculate the normal vector (not normalized for simplicity)
        var nx = (v2[1] - v1[1]) * (v3[2] - v1[2]) - (v2[2] - v1[2]) * (v3[1] - v1[1]);
        var ny = (v2[2] - v1[2]) * (v3[0] - v1[0]) - (v2[0] - v1[0]) * (v3[2] - v1[2]);
        var nz = (v2[0] - v1[0]) * (v3[1] - v1[1]) - (v2[1] - v1[1]) * (v3[0] - v1[0]);

        writer.WriteLine($"facet normal {nx} {ny} {nz}");
        writer.WriteLine("  outer loop");
        writer.WriteLine($"    vertex {v1[0]} {v1[1]} {v1[2]}");
        writer.WriteLine($"    vertex {v2[0]} {v2[1]} {v2[2]}");
        writer.WriteLine($"    vertex {v3[0]} {v3[1]} {v3[2]}");
        writer.WriteLine("  endloop");
        writer.WriteLine("endfacet");
    }

    writer.WriteLine("endsolid model");
}

using (StreamWriter outputFile = new StreamWriter(Path.Combine("C:/Users/Jonathan/Desktop/", "testcs.txt")))
{
    foreach (var f in verts)
    {
        outputFile.WriteLine($"{f[0]}, {f[1]}, {f[2]}");
    }
}

Console.WriteLine($"Loaded mesh with {verts.Count} vertices");
Console.WriteLine($"Loaded mesh with {faces.Count} faces");
Console.WriteLine("Finished");
