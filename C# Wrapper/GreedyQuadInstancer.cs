using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class GreedyQuadInstancer
{
    /// <summary>The keys for <see cref="instanceBuckets"/>. Grouping compatible <see cref="GridAlignedQuadInstance"/>s.</summary>
    /// <param name="Face">The cube face this bucket contains.</param>
    /// <param name="Slice">
    /// The depth or slice this bucket contains.<br/>
    /// E.g if <paramref name="Face"/> is <see cref="Direction.Back"/> instances at Z position 0 would be one slice, 1 would be another, ect.
    /// </param>
    /// <param name="TextureIndex">The repeating or wrapping texture ID this bucket contains.</param>
    public readonly record struct BucketKey(Direction Face, byte Slice, ushort TextureIndex);
    /// <summary>A 2D position on the plane of an unkown slice axis.</summary>
    public readonly record struct UV(byte U, byte V);
    public readonly record struct PositionTint(byte PosX, byte PosY, byte PosZ, Vector3 Tint);
    /// <summary>
    /// <see cref="GridAlignedQuadInstance"/>s bucketed by other compatible <see cref="GridAlignedQuadInstance"/> faces.<br/>
    /// Use <see cref="Add"/> or <see cref="AddRange"/> to automatically bucket your <see cref="GridAlignedQuadInstance"/> faces.
    /// </summary>
    public readonly Dictionary<BucketKey, Dictionary<UV, PositionTint>> instanceBuckets = [];

    /// <summary>Add a <see cref="GridAlignedQuadInstance"/> to be greedy meshed.</summary>
    public void Add(GridAlignedQuadInstanceAndTints toAdd)
    {
        if (toAdd.Tints.Length != 1)
            throw new Exception($"Tints must be of length 1.");
        (byte slice, UV uv) = (Direction)toAdd.QuadInstance.normal switch
        {
            Direction.Back or Direction.Front => (toAdd.QuadInstance.posZ, new UV(toAdd.QuadInstance.posX, toAdd.QuadInstance.posY)),
            Direction.Top or Direction.Bottom => (toAdd.QuadInstance.posY, new UV(toAdd.QuadInstance.posX, toAdd.QuadInstance.posZ)),
            Direction.Left or Direction.Right => (toAdd.QuadInstance.posX, new UV(toAdd.QuadInstance.posZ, toAdd.QuadInstance.posY)),
            _ => throw new Exception ($"{toAdd.QuadInstance.normal} is not a valid direction")
        };
        BucketKey bucketKey = new((Direction)toAdd.QuadInstance.normal, slice, toAdd.QuadInstance.texture);
        if (!instanceBuckets.TryGetValue(bucketKey, out Dictionary<UV, PositionTint>? instanceByUV))
        {
            instanceByUV = [];
            instanceBuckets.Add(bucketKey, instanceByUV);
        }
        instanceByUV[uv] = new(toAdd.QuadInstance.posX, toAdd.QuadInstance.posY, toAdd.QuadInstance.posZ, toAdd.Tints[0]);
    }

    /// <summary>Add multiple <see cref="GridAlignedQuadInstance"/>s to be greedy meshed.</summary>
    public void AddRange(IEnumerable<GridAlignedQuadInstanceAndTints> toAdd)
    {
        foreach (GridAlignedQuadInstanceAndTints instance in toAdd)
            Add(instance);
    }

    /// <summary>Merge all compatible <see cref="GridAlignedQuadInstanceAndTints"/> faces that have been added.</summary>
    public GridAlignedQuadInstanceAndTints[] Merge()
    {
        List<GridAlignedQuadInstanceAndTints> toReturn = [];
        HashSet<UV> merged = [];
        List<Vector3> groupedTints = [];
        uint tintIndex = 0;
        foreach ((BucketKey key, Dictionary<UV, PositionTint> instanceBucket) in instanceBuckets)
        {
            merged.Clear();
            foreach ((UV uv, PositionTint instance) in instanceBucket)
            {
                if (!merged.Add(uv))
                    continue;
                groupedTints.Clear();
                groupedTints.Add(instance.Tint);
                UV finalScale = new(0, 0);
                for (int offsetU = 1; uv.U + offsetU < 256; offsetU++)
                {
                    UV testing = new((byte)(uv.U + offsetU), uv.V);
                    if (!instanceBucket.TryGetValue(testing, out PositionTint PT) || !merged.Add(testing))
                        break;
                    groupedTints.Add(PT.Tint);
                    finalScale = finalScale with { U = (byte)offsetU };
                }
                for (int offsetV = 1; uv.V + offsetV < 256; offsetV++)
                {
                    (UV UV, Vector3 tint)[] row = new (UV UV, Vector3 tint)[finalScale.U + 1];
                    bool rowCompatible = true;
                    for (int offsetU = 0; offsetU <= finalScale.U; offsetU++)
                    {
                        UV testing = new((byte)(uv.U + offsetU), (byte)(uv.V + offsetV));
                        if (!instanceBucket.TryGetValue(testing, out PositionTint PT) || merged.Contains(testing))
                        {
                            rowCompatible = false;
                            break;
                        }
                        row[offsetU] = new(testing, PT.Tint);
                    }
                    if (!rowCompatible)
                        break;
                    for (int offsetU = 0; offsetU <= finalScale.U; offsetU++)
                    {
                        merged.Add(row[offsetU].UV);
                        groupedTints.Add(row[offsetU].tint);
                    }
                    finalScale = finalScale with { V = (byte)offsetV };
                }
                Vector3[] finalTints = [.. groupedTints];
                int width = finalScale.U + 1;
                // Quads always scale in the positive direction.
                // Where as texturing and tints scale relative to the normal (to the right when looking at the face).
                // Meaning we need to mirror the tint rows for these faces.
                if (key.Face is Direction.Front or Direction.Bottom or Direction.Left)
                    for (int v = 0; v <= finalScale.V; v++)
                        for (int u = 0; u <= finalScale.U; u++)
                            finalTints[v * width + u] = groupedTints[v * width + (finalScale.U - u)];
                GridAlignedQuadInstance quadInstance = new
                (
                    instance.PosX, instance.PosY, instance.PosZ,
                    key.Face,
                    key.TextureIndex,
                    finalScale.U, finalScale.V,
                    tintIndex
                );
                tintIndex += (uint)groupedTints.Count;
                toReturn.Add(new(quadInstance, finalTints));
            }
        }
        return [.. toReturn];
    }
}
