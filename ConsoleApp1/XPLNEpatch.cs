using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics; // For sin, cos, sqrt, atan2, radians
using System.Reflection;
using System.Text;
using System.Xml.Linq;
//using Microsoft.Extensions.Logging;

namespace CSDsf;

public class XPLNEpatch
{
    public int? Flag { get; private set; }
    public float? Near { get; private set; }
    public float? Far { get; private set; }
    public int DefIndex { get; private set; }
    public List<List<object>> Cmds { get; private set; }

    public XPLNEpatch(int? flag, float? near, float? far, int defIndex)
    {
        Flag = flag;
        Near = near;
        Far = far;
        DefIndex = defIndex;
        Cmds = [];
    }

    public List<int[][]> Triangles()
    {
        var triangles = new List<int[][]>();
        int? poolIndex = null;

        foreach (dynamic command in Cmds)
        {
            switch ((int) command[0])
            {
                case 1: // COORDINATE POOL SELECT
                    poolIndex = (int?)command[1];
                    break;
                case 23: // PATCH TRIANGLE
                    for (int i = 1; i < command.Count; i += 3)
                    {
                        triangles.Add([
                            [poolIndex.Value, (int)command[i]],
                            [poolIndex.Value, (int)command[i + 1]],
                            [poolIndex.Value, (int)command[i + 2]]
                        ]);
                    }
                    break;
                case 24: // TRIANGLE PATCH CROSS-POOL
                    for (int i = 1; i < command.Count; i += 6)
                    {
                        triangles.Add([
                            [(int)command[i], (int)command[i + 1]],
                            [(int)command[i + 2], (int)command[i + 3]],
                            [(int)command[i + 4], (int)command[i + 5]]
                        ]);
                    }
                    break;
                case 25: // PATCH TRIANGLE RANGE
                    for (int i = (int)command[1]; i < (int)command[2] - 1; i += 3)
                    {
                        triangles.Add([
                            [poolIndex.Value, i],
                            [poolIndex.Value, i + 1],
                            [poolIndex.Value, i + 2]
                        ]);
                    }
                    break;
                case 26: // PATCH TRIANGLE STRIP
                    for (int i = 3; i < command.Count; i++)
                    {
                        if (i % 2 != 0)
                        {
                            triangles.Add([
                                [poolIndex.Value, (int)command[i - 2]],
                                [poolIndex.Value, (int)command[i - 1]],
                                [poolIndex.Value, (int)command[i]]
                            ]);
                        }
                        else
                        {
                            triangles.Add([
                                [poolIndex.Value, (int)command[i - 2]],
                                [poolIndex.Value, (int)command[i]],
                                [poolIndex.Value, (int)command[i - 1]]
                            ]);
                        }
                    }
                    break;
                case 27: // PATCH TRIANGLE STRIP CROSS POOL
                    for (int i = 6; i < command.Count; i += 2)
                    {
                        if (i % 4 != 0)
                        {
                            triangles.Add([
                                [(int)command[i - 5], (int)command[i - 4]],
                                [(int)command[i - 3], (int)command[i - 2]],
                                [(int)command[i - 1], (int)command[i]]
                            ]);
                        }
                        else
                        {
                            triangles.Add([
                                [(int)command[i - 5], (int)command[i - 4]],
                                [(int)command[i - 1], (int)command[i]],
                                [(int)command[i - 3], (int)command[i - 2]]
                            ]);
                        }
                    }
                    break;
                case 28: // PATCH TRIANGLE STRIP RANGE
                    for (int i = (int)command[1]; i < (int)command[2] - 2; i++)
                    {
                        if ((i - (int)command[1]) % 2 != 0)
                        {
                            triangles.Add([
                                [poolIndex.Value, i],
                                [poolIndex.Value, i + 2],
                                [poolIndex.Value, i + 1]
                            ]);
                        }
                        else
                        {
                            triangles.Add([
                                [poolIndex.Value, i],
                                [poolIndex.Value, i + 1],
                                [poolIndex.Value, i + 2]
                            ]);
                        }
                    }
                    break;
                case 29: // PATCH TRIANGLE FAN
                    for (int i = 3; i < command.Count; i++)
                    {
                        triangles.Add([
                            [poolIndex.Value, (int)command[1]],
                            [poolIndex.Value, (int)command[i - 1]],
                            [poolIndex.Value, (int)command[i]]
                        ]);
                    }
                    break;
                case 30: // PATCH TRIANGLE FAN CROSS-POOL
                    for (int i = 6; i < command.Count; i += 2)
                    {
                        triangles.Add([
                            [(int)command[1], (int)command[2]],
                            [(int)command[i - 3], (int)command[i - 2]],
                            [(int)command[i - 1], (int)command[i]]
                        ]);
                    }
                    break;
                case 31: // PATCH TRIANGLE FAN RANGE
                    for (int i = (int)command[1]; i < (int)command[2] - 2; i++)
                    {
                        triangles.Add([
                            [poolIndex.Value, (int)command[1]],
                            [poolIndex.Value, i + 1],
                            [poolIndex.Value, i + 2]
                        ]);
                    }
                    break;
            }
        }

        return triangles;
    }
}
