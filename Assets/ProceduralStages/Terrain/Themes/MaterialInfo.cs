using UnityEngine;

namespace ProceduralStages
{
    public class MaterialInfo
    {
        public ThemeColorPalettes colorPalette;
        public Texture2D colorGradiant;
        public Texture2D grassColorGradiant;
        public SurfaceTexture floorTexture;
        public SurfaceTexture wallTexture;
        public SurfaceTexture detailTexture;

        public Material ApplyTo(
            Material material,
            bool useUV = true)
        {
            material.SetTexture("_ColorTex", colorGradiant);

            material.mainTexture = wallTexture.texture;
            material.SetFloat("_WallBias", wallTexture.bias);
            material.SetColor("_WallColor", wallTexture.averageColor);
            material.SetFloat("_WallScale", wallTexture.scale);
            material.SetFloat("_WallContrast", wallTexture.constrast);
            material.SetFloat("_WallGlossiness", wallTexture.glossiness);
            material.SetFloat("_WallMetallic", wallTexture.metallic);

            material.SetTexture("_FloorTex", floorTexture.texture);
            material.SetFloat("_FloorBias", floorTexture.bias);
            material.SetColor("_FloorColor", floorTexture.averageColor);
            material.SetFloat("_FloorScale", floorTexture.scale);
            material.SetFloat("_FloorContrast", floorTexture.constrast);

            material.SetTexture("_DetailTex", detailTexture.texture);
            material.SetFloat("_DetailBias", detailTexture.bias);
            material.SetColor("_DetailColor", detailTexture.averageColor);
            material.SetFloat("_DetailScale", detailTexture.scale);
            material.SetFloat("_DetailContrast", detailTexture.constrast);

            material.SetInt("_UseUV", useUV ? 1 : 0);

            return material;
        }
    }
}
