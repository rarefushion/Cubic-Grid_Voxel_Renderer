using System.Numerics;

namespace GalensUnified.CubicGrid.Renderer.NET;
/// <summary>Settings for lighting amongst a cubic grid.</summary>
/// <param name="lightDirection">The direction the light points.</param>
/// <param name="setBase">If the brightiness is reassigned. Without this lighting will accumulate.</param>
/// <param name="lightBase">If <paramref name="setBase"/> the light value a face will defualt to. aka ambient lighting.</param>
/// <param name="lightHit">The light amount to add if a face is visable to the light source.</param>
/// <param name="lightMiss">The light amount to remove if a face is not visiable to the light source.</param>
/// <param name="diffuseShading">Light hit is multiplied by the dot product of normal and <paramref name="lightDirection"/>.</param>
/// <param name="lightMin">Clamps the final level to this minimum.</param>
/// <param name="lightMax">Clamps the final level to this maximum.</param>
/// <param name="maxRaydistance">The max a ray originating from a face can travel before assuming it CAN see the light.</param>
public struct DirectionalLightingSettings
(
    Vector3 lightDirection,
    bool setBase,
    float lightBase,
    float lightHit,
    float lightMiss,
    bool diffuseShading,
    float lightMin,
    float lightMax,
    int maxRaydistance
)
{
    public Vector3 lightDirection = lightDirection;
    public bool setBase = setBase;
    public float lightBase = lightBase;
    public float lightHit = lightHit;
    public float lightMiss = lightMiss;
    public bool diffuseShading = diffuseShading;
    public float lightMin = lightMin;
    public float lightMax = lightMax;
    public int maxRaydistance = maxRaydistance;
}