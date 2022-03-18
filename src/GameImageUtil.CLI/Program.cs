
using GameImageUtil.Assets;
using GameImageUtil.Converters;
using GameImageUtil.Imaging.DirectXTex;
using GameImageUtil.ShaderCode;
using GameImageUtil.Shaders;
using Vortice.Direct3D11;

namespace GameImageUtil.CLI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var instance = new Instance();

            using var processor = new CoDNOGProcessor();

            processor.Initialize(instance);

            processor.Process(
                @"G:\Tools\MMW\exported_files\xmodels\head_mp_rus_s4_polina_01_dmn\_images\mtl_c_s4_diamond_mask_mask_lens_01\c_s4_diamond_mask_mask_01_n&c_s4_diamond_mask_mask_01_g~3293891893872388404.png", new());
        }
    }
}