using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace PathBerserker2d
{

    static class SharedMaterials
    {
        static Material unlitVertexColorSolid;
        static Material unlitVertexColorTransparent;
        static Material unlitStripped;
        static Material unlitTransparentTinted;
        static Material unlitTexture;
        static int unlitTransparentTinted_ColorId;
        static int unlitStripped_ColorId;
        static int unlitStripped_XOffsetId;
        static int unlitStripped_PauseSizeId;
        static int unlitStripped_SegmentSizeId;

        public static Material UnlitVertexColorSolid
        {
            get
            {
                if (unlitVertexColorSolid == null)
                    unlitVertexColorSolid = new Material(Shader.Find("Hidden/PB_UnlitVertexColor"));
                return unlitVertexColorSolid;
            }
        }
        public static Material UnlitVertexColorTransparent
        {
            get
            {
                if (unlitVertexColorTransparent == null)
                    unlitVertexColorTransparent = new Material(Shader.Find("Hidden/PB_UnlitVertexColorTransparent"));
                return unlitVertexColorTransparent;
            }
        }
        public static Material UnlitStripped
        {
            get
            {
                if (unlitStripped == null)
                {
                    unlitStripped = new Material(Shader.Find("Hidden/PB_UnlitSegmented"));
                    unlitStripped.SetFloat("_SegmentSize", 0.21f);
                    unlitStripped.SetFloat("_PauseSize", 0.31f);
                    unlitStripped_ColorId = unlitStripped.shader.GetPropertyNameId(3);
                    unlitStripped_XOffsetId = unlitStripped.shader.GetPropertyNameId(2);
                    unlitStripped_PauseSizeId = unlitStripped.shader.GetPropertyNameId(1);
                    unlitStripped_SegmentSizeId = unlitStripped.shader.GetPropertyNameId(0);
                }
                return unlitStripped;
            }
        }
        public static int UnlitStripped_ColorId => unlitStripped_ColorId;
        public static int UnlitStripped_XOffsetId => unlitStripped_XOffsetId;
        public static int UnlitStripped_PauseSizeId => unlitStripped_PauseSizeId;
        public static int UnlitStripped_SegmentSizeId => unlitStripped_SegmentSizeId;

        public static Material UnlitTransparentTinted
        {
            get
            {
                if (unlitTransparentTinted == null)
                {
                    unlitTransparentTinted = new Material(Shader.Find("Hidden/PB_UnlitTransparentTinted"));
                    unlitTransparentTinted_ColorId = unlitTransparentTinted.shader.GetPropertyNameId(0);
                }
                return unlitTransparentTinted;
            }
        }
        public static int UnlitTransparentTinted_ColorId => unlitTransparentTinted_ColorId;

        public static Material UnlitTexture
        {
            get
            {
                if (unlitTexture == null)
                {
                    unlitTexture = new Material(Shader.Find("Unlit/Texture"));
                }
                return unlitTexture;
            }
        }
    }
}
