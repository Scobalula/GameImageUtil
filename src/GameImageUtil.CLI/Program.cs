
namespace GameImageUtil.CLI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var device = new GraphicsDevice();

            Console.WriteLine(device.Adapter.DebugName);

            var shader = ShaderCompiler.Compile(File.ReadAllText(@"C:\Prj\VS\Aether\src\x64\Debug\Shaders\cubearray2equ.hlsl"), ShaderType.ComputShader, null);

            File.WriteAllBytes("test.dat", shader.AsBytes());
        }
    }
}