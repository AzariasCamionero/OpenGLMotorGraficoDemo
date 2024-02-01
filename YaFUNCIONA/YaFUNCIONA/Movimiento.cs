using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Runtime.InteropServices;

namespace YaFUNCIONA
{
    internal class Movimiento : GameWindow
    {
        private int shaderProgram;
        private int vertexShader;
        private int fragmentShader;
        private int vbo;
        private int vao;

        private Matrix4 modelview;
        private Matrix4 projection;

        float xPosition = 0.0f;
        float yPosition = 0.0f;
        float zPosition = -5.0f;

        float stepSize = 0.005f;
        float rotationAngle = 0.0005f;
        float rotationSpeed = 0.5f;
        float rotationX = 0.0f;

        float cameraRotationX = 0.0f;
        float cameraRotationY = 0.0f;
        float cameraRotationSpeed = 0.1f;

        float velocity = 0.0001f;
        float acceleration = 0.0001f;

        // Cone
        private int coneVbo;
        private int coneVao;
        private int coneShaderProgram;
        private int coneVertexShader;
        private int coneFragmentShader;
        private Matrix4 coneModelview;

        public Movimiento(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            Vector3 lightColor = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 lightPos = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 objectColor = new Vector3(1.0f, 0.0f, 0.0f);

            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightColor"), lightColor);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), lightPos);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "objectColor"), objectColor);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);

            // Habilita mensajes de depuración de OpenGL
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                string errorMessage = Marshal.PtrToStringAnsi(message);
                Console.WriteLine($"OpenGL Debug Message ({severity}): {errorMessage}");
            }, IntPtr.Zero);

            SetupShaders();
            SetupBuffers();
            SetupCone();
        }

        private void SetupShaders()
        {
            string vertexShaderSource = VertexShader;
            string fragmentShaderSource = FragmentShader;

            vertexShader = LoadShader(ShaderType.VertexShader, vertexShaderSource);
            fragmentShader = LoadShader(ShaderType.FragmentShader, fragmentShaderSource);

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);

            // Verificar el estado del programa
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(shaderProgram);
                Console.WriteLine($"Error al vincular shaders: {infoLog}");
            }

            GL.UseProgram(shaderProgram);

            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "modelview"), false, ref modelview);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "projection"), false, ref projection);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "objectColor"), 1.0f, 0.0f, 0.0f);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightColor"), 1.0f, 1.0f, 1.0f);
            GL.Uniform3(GL.GetUniformLocation(shaderProgram, "lightPos"), 1.0f, 1.0f, 1.0f);
        }

        private void SetupBuffers()
        {
            float[] vertices =
            {
                // Front face
                -0.5f, -0.5f, 0.5f,
                 0.5f, -0.5f, 0.5f,
                 0.5f,  0.5f, 0.5f,

                -0.5f, -0.5f, 0.5f,
                 0.5f,  0.5f, 0.5f,
                -0.5f,  0.5f, 0.5f,

                // Back face
                -0.5f, -0.5f, -0.5f,
                 0.5f, -0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,

                -0.5f, -0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f,

                // Left face
                -0.5f, -0.5f,  0.5f,
                -0.5f,  0.5f,  0.5f,
                -0.5f,  0.5f, -0.5f,

                -0.5f, -0.5f,  0.5f,
                -0.5f,  0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,

                // Right face
                 0.5f, -0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f, -0.5f,

                 0.5f, -0.5f,  0.5f,
                 0.5f,  0.5f, -0.5f,
                 0.5f, -0.5f, -0.5f,

                // Top face
                -0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f, -0.5f,

                -0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f,

                // Bottom face
                -0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f, -0.5f,

                -0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f
            };

            GL.GenBuffers(1, out vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.GenVertexArrays(1, out vao);
            GL.BindVertexArray(vao);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var keyboard = KeyboardState;

            if (keyboard.IsKeyDown(Keys.Up))
                yPosition -= stepSize;
            if (keyboard.IsKeyDown(Keys.Down))
                yPosition += stepSize;
            if (keyboard.IsKeyDown(Keys.Left))
                xPosition += stepSize;
            if (keyboard.IsKeyDown(Keys.Right))
                xPosition -= stepSize;

            if (keyboard.IsKeyDown(Keys.W))
                velocity += acceleration;
            if (keyboard.IsKeyDown(Keys.S))
                velocity -= acceleration;

            zPosition += velocity;
            velocity *= 0.98f;

            Console.WriteLine($"W Key: {keyboard.IsKeyDown(Keys.W)}, S Key: {keyboard.IsKeyDown(Keys.S)}");

            if (keyboard.IsKeyDown(Keys.I))
                cameraRotationX += cameraRotationSpeed * (float)e.Time;
            if (keyboard.IsKeyDown(Keys.K))
                cameraRotationX -= cameraRotationSpeed * (float)e.Time;
            if (keyboard.IsKeyDown(Keys.J))
                cameraRotationY += cameraRotationSpeed * (float)e.Time;
            if (keyboard.IsKeyDown(Keys.L))
                cameraRotationY -= cameraRotationSpeed * (float)e.Time;

            rotationX += 0.01f;

            cameraRotationY -= cameraRotationSpeed * (float)e.Time;

            rotationAngle += (float)e.Time * rotationSpeed;
        }

        private void SetupCone()
        {
            string coneVertexShaderSource = ConeVertexShader;
            string coneFragmentShaderSource = ConeFragmentShader;

            coneVertexShader = LoadShader(ShaderType.VertexShader, coneVertexShaderSource);
            coneFragmentShader = LoadShader(ShaderType.FragmentShader, coneFragmentShaderSource);

            coneShaderProgram = GL.CreateProgram();
            GL.AttachShader(coneShaderProgram, coneVertexShader);
            GL.AttachShader(coneShaderProgram, coneFragmentShader);
            GL.LinkProgram(coneShaderProgram);

            // Verificar el estado del programa
            GL.GetProgram(coneShaderProgram, GetProgramParameterName.LinkStatus, out int coneSuccess);
            if (coneSuccess == 0)
            {
                string infoLog = GL.GetProgramInfoLog(coneShaderProgram);
                Console.WriteLine($"Error al vincular shaders del cono: {infoLog}");
            }

            GL.UseProgram(coneShaderProgram);

            // Configurar las ubicaciones de las variables uniformes en los shaders
            GL.UniformMatrix4(GL.GetUniformLocation(coneShaderProgram, "projection"), false, ref projection);


            // Puedes definir los vértices del cono de manera similar a cómo definiste estan definidos los  vértices del cubo
            float[] coneVertices =
            {
            // Posiciones (x, y, z) de los vértices
             0.0f,  1.0f, 0.0f,   // Vértice superior
             -0.5f,  0.0f, 0.5f,   // Vértice inferior izquierdo
             0.5f,  0.0f, 0.5f,   // Vértice inferior derecho
    };

            GL.GenBuffers(1, out coneVbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, coneVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, coneVertices.Length * sizeof(float), coneVertices, BufferUsageHint.StaticDraw);

            GL.GenVertexArrays(1, out coneVao);
            GL.BindVertexArray(coneVao);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Dibujar cubo
            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vao);

            Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 5 + zPosition), Vector3.Zero, Vector3.UnitY);
            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), (float)Size.X / Size.Y, 0.1f, 100.0f);

            modelview = Matrix4.CreateRotationY(rotationAngle) * view * Matrix4.CreateTranslation(xPosition, yPosition, zPosition);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "modelview"), false, ref modelview);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "projection"), false, ref projection);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);

            // Dibujar cono
            GL.UseProgram(coneShaderProgram);
            GL.BindVertexArray(coneVao);

            // Ajusta la posición y la escala según tus necesidades
            coneModelview = Matrix4.CreateTranslation(2.0f, 0.0f, 0.0f) * Matrix4.CreateScale(0.5f, 1.0f, 0.5f);
            GL.UniformMatrix4(GL.GetUniformLocation(coneShaderProgram, "modelview"), false, ref coneModelview);

            // Configurar la matriz de proyección también para el cono
            GL.UniformMatrix4(GL.GetUniformLocation(coneShaderProgram, "projection"), false, ref projection);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3); // 3 vértices para el cono

            GL.BindVertexArray(0);
            GL.UseProgram(0); // Desactiva el programa después de su uso

            SwapBuffers();
        }

        private int LoadShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"Error al compilar shader: {infoLog}");
            }

            return shader;
        }

        private void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string errorMessage = Marshal.PtrToStringAnsi(message);
            Console.WriteLine($"OpenGL Debug Message ({severity}): {errorMessage}");
        }

        static void Main()
        {
            var gameSettings = GameWindowSettings.Default;
            var nativeSettings = new NativeWindowSettings
            {
                ClientSize = new Vector2i(800, 600),
                Title = "AzariaasEngine"
            };

            using (var movimiento = new Movimiento(gameSettings, nativeSettings))
            {
                movimiento.Run();
            }
        }

        public static string VertexShader = @"
#version 330 core

layout (location = 0) in vec3 aPos;

uniform mat4 modelview;
uniform mat4 projection;

void main()
{
    gl_Position = projection * modelview * vec4(aPos, 1.0);
}
";

        public static string FragmentShader = @"
#version 330 core

out vec4 FragColor;

uniform vec3 objectColor;
uniform vec3 lightColor;
uniform vec3 lightPos;

void main()
{
    vec3 ambient = 0.1 * objectColor;
    vec3 norm = normalize(vec3(0.0, 0.0, 1.0));
    vec3 lightDir = normalize(lightPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse) * objectColor;
    FragColor = vec4(result, 1.0);
}
";
        public static string ConeVertexShader = @"
#version 330 core

layout (location = 0) in vec3 aPos;

uniform mat4 modelview;

void main()
{
    gl_Position = modelview * vec4(aPos, 1.0);
}
";

        public static string ConeFragmentShader = @"
#version 330 core

out vec4 FragColor;

void main()
{
    FragColor = vec4(0.0, 1.0, 0.0, 1.0);
}
";
    }
}