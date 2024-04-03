using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;



namespace RayTracer
{
    class Program
    {
        static GameWindow window;
        static Random rand = new Random();
        static void Main(string[] args)
        {
            // 창 생성 및 OpenGL 초기화
            window = new GameWindow(512, 512, GraphicsMode.Default, "Ray Tracer");
            window.Load += Window_Load;
            window.RenderFrame += Window_RenderFrame;
            window.Run();
        }

        private static void Window_Load(object sender, EventArgs e)
        {
            GL.ClearColor(0f, 0f, 0f, 0f);

        }

        private static void Window_RenderFrame(object sender, FrameEventArgs e)
        {


            GL.Clear(ClearBufferMask.ColorBufferBit);
            //int N = 64;
            int N = 1;
            Vector3 eyePoint = new Vector3(0, 0, 0);
            Vector3 u = new Vector3(1, 0, 0);
            Vector3 v = new Vector3(0, 1, 0);
            Vector3 w = new Vector3(0, 0, 1);
            float l = -0.1f;
            float r = 0.1f;
            float b = -0.1f;
            float t = 0.1f;
            float d = 0.1f;


            Plane plane = new Plane(new Vector3(0, -2, 0), new Vector3(0, 1, 0));
            Sphere sphere1 = new Sphere(new Vector3(-4, 0, -7), 1);
            Sphere sphere2 = new Sphere(new Vector3(0, 0, -7), 2);
            Sphere sphere3 = new Sphere(new Vector3(4, 0, -7), 1);

            Vector3 lightPos = new Vector3(-4, 4, -3);
            GL.Begin(PrimitiveType.Points);
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {

                    
                    Vector3 color = Vector3.Zero;
                    for (int s= 0; s<N; s++)
                    {
                        float rx = (float)rand.NextDouble();
                        float ry = (float)rand.NextDouble();

                        Vector3 rayDir = CalculateRayDirection(x, y, rx, ry, 512, 512, l, r, b, t, d, u, v, w);

                        float minDist = float.MaxValue;
                        Object closestObj = null;
                        if (plane.Intersect(eyePoint, rayDir, out t))
                        {
                            if (t < minDist)
                            {

                                minDist = t;
                                closestObj = plane;
                            }
                        }
                        if (sphere1.Intersect(eyePoint, rayDir, out t))
                        {
                            if (t < minDist)
                            {
                                minDist = t;
                                closestObj = sphere1;
                            }
                        }
                        if (sphere2.Intersect(eyePoint, rayDir, out t))
                        {
                            if (t < minDist)
                            {
                                minDist = t;
                                closestObj = sphere2;

                            }
                        }

                        if (sphere3.Intersect(eyePoint, rayDir, out t))
                        {
                            if (t < minDist)
                            {
                                minDist = t;
                                closestObj = sphere3;
                            }
                        }

                        // 교점에 따른 픽셀 색상 설정
                        if (closestObj != null)
                        {
                            Vector3 hitPoint = eyePoint + rayDir * minDist;
                            Vector3 normal = closestObj.GetNormal(hitPoint);
                            color = PhongShading(closestObj, hitPoint, normal, lightPos);

                            //Vector3 color = new Vector3(0.0f, 0.0f, 0.0f);
                            GL.Color3(color);

                        }
                        else
                        {

                            GL.Color3(1.0f, 1.0f, 1.0f);
                        }
                    }
                    

                    GL.Vertex2(x / 256.0f - 1.0f, y / 256.0f- 1.0f);
                }
            }
            GL.End();
           
            window.SwapBuffers();
        }

        private static Vector3 CalculateRayDirection(int x, int y, float rx, float ry, int width, int height, float l, float r, float b, float t, float d, Vector3 u, Vector3 v, Vector3 w)
        {
            float px = l + (r - l) * (x + rx) / width;
            float py = b + (t - b) * (y + ry) / height;
            
            Vector3 rayDir = -w * d + u * px + v * py;

            return rayDir.Normalized();
        }

        private static Vector3 PhongShading(Object obj, Vector3 hitPoint, Vector3 normal, Vector3 lightPos)
        {
            Vector3 ka = Vector3.Zero;
            Vector3 kd = Vector3.Zero;
            Vector3 ks = Vector3.Zero;
            float specPower = 0;

            if (obj is Plane)
            {
                ka = new Vector3(0.2f, 0.2f, 0.2f);
                kd = new Vector3(1.0f, 1.0f, 1.0f);
                ks = Vector3.Zero;
                specPower = 0;
            }
            else if (obj is Sphere sphere)
            {
                if (sphere.Center == new Vector3(-4, 0, -7))
                {
                    ka = new Vector3(0.2f, 0.0f, 0.0f);
                    kd = new Vector3(1.0f, 0.0f, 0.0f);
                    ks = Vector3.Zero;
                    specPower = 0;
                }
                else if (sphere.Center == new Vector3(0, 0, -7))
                {
                    ka = new Vector3(0.0f, 0.2f, 0.0f);
                    kd = new Vector3(0.0f, 0.5f, 0.0f);
                    ks = new Vector3(0.5f, 0.5f, 0.5f);
                    specPower = 32;
                }
                else if (sphere.Center == new Vector3(4, 0, -7))
                {
                    ka = new Vector3(0.0f, 0.0f, 0.2f);
                    kd = new Vector3(0.0f, 0.0f, 1.0f);
                    ks = Vector3.Zero;
                    specPower = 0;
                }
            }
            else
            {
                throw new Exception("Invalid object type");
            }

            Vector3 lightDir = (lightPos - hitPoint).Normalized();

            float diffuseTerm = Math.Max(Vector3.Dot(lightDir, normal), 0);
            Vector3 diffuse = kd * diffuseTerm;

            Vector3 viewDir = (-hitPoint).Normalized();
            Vector3 reflectDir = 2 * Vector3.Dot(normal, lightDir) * normal - lightDir;
            float specularTerm = (float)Math.Pow(Math.Max(Vector3.Dot(viewDir, reflectDir), 0), specPower);
            Vector3 specular = ks * specularTerm;


            Vector3 color = ka + diffuse + specular;


            color.X = (float)Math.Pow(color.X, 1.0 / 2.2);
            color.Y = (float)Math.Pow(color.Y, 1.0 / 2.2);
            color.Z = (float)Math.Pow(color.Z, 1.0 / 2.2);

            return color;
        }
    }

    abstract class Object
    {
        public abstract bool Intersect(Vector3 eyePoint, Vector3 rayDir, out float t);
        public abstract Vector3 GetNormal(Vector3 point);
    }

    class Plane : Object
    {
        public Vector3 Position;
        public Vector3 Normal;

        public Plane(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal.Normalized();
        }

        public override bool Intersect(Vector3 eyePoint, Vector3 rayDir, out float t)
        {
            float denom = Vector3.Dot(rayDir, Normal);
            if (Math.Abs(denom) > 1e-6f)
            {
                Vector3 p0l0 = Position - eyePoint;
                t = Vector3.Dot(p0l0, Normal) / denom;
                return (t > 0);
            }

            t = 0;
            return false;
        }

        public override Vector3 GetNormal(Vector3 point)
        {
            return Normal;
        }
    }

    class Sphere : Object
    {
        public Vector3 Center;
        public float Radius;

        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public override bool Intersect(Vector3 eyePoint, Vector3 rayDir, out float t)
        {
            Vector3 p = eyePoint;
            Vector3 d = rayDir;
            Vector3 m = p - Center;
            
            float p_d = Vector3.Dot(m, d);
            float p_p = Vector3.Dot(m, m);


            float discriminant = p_d * p_d - (p_p - Radius * Radius);

            if (discriminant < 0)
            {
                t = 0;
                return false;
            }
            else
            {
                float sqrt_discriminant = (float)Math.Sqrt(discriminant);
                float t1 = (-p_d - sqrt_discriminant);
                float t2 = (-p_d + sqrt_discriminant);

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                if (t1 < 0)
                {
                    t1 = t2;
                    if (t1 < 0)
                    {
                        t = 0;
                        return false;
                    }
                }
               
                t = t1;
                return true;
            }
        }


        public override Vector3 GetNormal(Vector3 point)
        {
            return (point - Center).Normalized();
        }
    }
}

