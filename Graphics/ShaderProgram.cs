using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace EarthBound.Graphics
{
    public class ShaderProgram
    {
        public int ID { get; private set; }

        public ShaderProgram(string vertexShaderFilepath, string fragmentShaderFilepath)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Creating ShaderProgram");
            Console.WriteLine($"VERTEX   : {vertexShaderFilepath}");
            Console.WriteLine($"FRAGMENT : {fragmentShaderFilepath}");
            Console.WriteLine("----------------------------------------");

            ID = GL.CreateProgram();

            int vertexShader = CompileShader(
                ShaderType.VertexShader,
                vertexShaderFilepath
            );

            int fragmentShader = CompileShader(
                ShaderType.FragmentShader,
                fragmentShaderFilepath
            );

            if (vertexShader == -1 || fragmentShader == -1)
            {
                Console.WriteLine("[SHADER PROGRAM ERROR] Shader compilation failed");
                return;
            }

            GL.AttachShader(ID, vertexShader);
            GL.AttachShader(ID, fragmentShader);

            GL.LinkProgram(ID);

            GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string infoLog = GL.GetProgramInfoLog(ID);
                Console.WriteLine("[LINK ERROR]");
                Console.WriteLine(infoLog);
            }
            else
            {
                Console.WriteLine("[LINK OK] ShaderProgram linked successfully");
            }

            GL.DetachShader(ID, vertexShader);
            GL.DetachShader(ID, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            Console.WriteLine("========================================");
        }

        private int CompileShader(ShaderType type, string filepath)
        {
            Console.WriteLine($"Compiling {type} : {filepath}");

            string source = LoadShaderSource(filepath);
            if (string.IsNullOrWhiteSpace(source))
            {
                Console.WriteLine("----- SHADER SOURCE BEGIN -----");
                Console.WriteLine(source);
                Console.WriteLine("----- SHADER SOURCE END -----");

                Console.WriteLine($"[ERROR] Shader source empty: {filepath}");
                return -1;
            }

            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"[SHADER ERROR] {type}");
                Console.WriteLine(infoLog);
                return -1;
            }

            Console.WriteLine($"[OK] {type} compiled");
            return shader;
        }

        public void Bind()
        {
            GL.UseProgram(ID);
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        public void Delete()
        {
            GL.DeleteProgram(ID);
        }

        public static string LoadShaderSource(string filePath)
        {
            string fullPath = "../../../Shaders/" + filePath;
            Console.WriteLine($"Loading shader file: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("[FILE ERROR] Shader file not found");
                return string.Empty;
            }

            try
            {
                string source = File.ReadAllText(fullPath);
                Console.WriteLine("[FILE OK] Shader file loaded");
                return source;
            }
            catch (Exception e)
            {
                Console.WriteLine("[FILE ERROR] " + e.Message);
                return string.Empty;
            }
        }
    }
}
