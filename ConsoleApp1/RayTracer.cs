using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace RayTracer
{
    class Program
    {
        static void Main(string[] args)
        {
            // 창 생성 및 OpenGL 초기화
            GameWindow window = new GameWindow(512, 512, GraphicsMode.Default, "Ray Tracer");
            window.Load += Window_Load;
            window.RenderFrame += Window_RenderFrame;
            window.Run();
        }

        private static void Window_Load(object sender, EventArgs e)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        }

        private static void Window_RenderFrame(object sender, FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // 카메라 설정
            Vector3 eyePoint = new Vector3(0, 0, 0);
            Vector3 u = new Vector3(1, 0, 0);
            Vector3 v = new Vector3(0, 1, 0);
            Vector3 w = new Vector3(0, 0, 1);
            float l = -0.1f, r = 0.1f, b = -0.1f, t = 0.1f, d = 0.1f;

            // 장면 객체 정의
            Plane plane = new Plane(new Vector3(0, -2, 0), new Vector3(0, 1, 0));
            Sphere sphere1 = new Sphere(new Vector3(-4, 0, -7), 1);
            Sphere sphere2 = new Sphere(new Vector3(0, 0, -7), 2);
            Sphere sphere3 = new Sphere(new Vector3(4, 0, -7), 1);

            // 광원 설정
            Vector3 lightPos = new Vector3(-4, 4, -3);

            // 각 픽셀에 대한 레이 트레이싱 수행
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    // 픽셀 중심을 통과하는 광선 계산
                    Vector3 rayDir = CalculateRayDirection(x, y, 512, 512, l, r, b, t, d, u, v, w);

                    // 광선과 객체의 교점 계산
                    float minDist = float.MaxValue;
                    Object closestObj = null;
                    if (plane.Intersect(eyePoint, rayDir, out float t))
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
                        Vector3 normal = closestObj.Normal(hitPoint);
                        Vector3 color = PhongShading(closestObj, hitPoint, normal, lightPos);
                        GL.Color3(color);
                    }
                    else
                    {
                        GL.Color3(0.0f, 0.0f, 0.0f); // 배경색
                    }

                    GL.Vertex2(x / 256.0f - 1.0f, 1.0f - y / 256.0f);
                }
            }

            window.SwapBuffers();
        }

        private static Vector3 CalculateRayDirection(int x, int y, int width, int height, float l, float r, float b, float t, float d, Vector3 u, Vector3 v, Vector3 w)
        {
            float px = (x + 0.5f) / width;
            float py = (y + 0.5f) / height;
            Vector3 rayDir = u * (l + (r - l) * px) + v * (b + (t - b) * py) - w * d;
            return rayDir.Normalized();
        }

        private static Vector3 PhongShading(Object obj, Vector3 hitPoint, Vector3 normal, Vector3 lightPos)
        {
            Vector3 ka, kd, ks;
            float specPower;

            // 물체별 재질 파라미터 설정
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

            // 광원 벡터 계산
            Vector3 lightDir = (lightPos - hitPoint).Normalized();

            // Diffuse 성분 계산
            float diffuseTerm = Math.Max(Vector3.Dot(lightDir, normal), 0);
            Vector3 diffuse = kd * diffuseTerm;

            // Specular 성분 계산
            Vector3 viewDir = (-hitPoint).Normalized();
            Vector3 reflectDir = 2 * Vector3.Dot(normal, lightDir) * normal - lightDir;
            float specularTerm = (float)Math.Pow(Math.Max(Vector3.Dot(viewDir, reflectDir), 0), specPower);
            Vector3 specular = ks * specularTerm;

            // 최종 색상 계산
            Vector3 color = ka + diffuse + specular;

            // Gamma 보정
            color.X = (float)Math.Pow(color.X, 1.0 / 2.2);
            color.Y = (float)Math.Pow(color.Y, 1.0 / 2.2);
            color.Z = (float)Math.Pow(color.Z, 1.0 / 2.2);

            return color;
        }
    }

    abstract class Object
    {
        public abstract bool Intersect(Vector3 eyePoint, Vector3 rayDir, out float t);
        public abstract Vector3 Normal(Vector3 point);
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

        public override Vector3 Normal(Vector3 point)
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
            Vector3 ec = eyePoint - Center;
            float a = Vector3.Dot(rayDir, rayDir);
            float b = 2 * Vector3.Dot(ec, rayDir);
            float c = Vector3.Dot(ec, ec) - Radius * Radius;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {
                t = 0;
                return false;
            }
            else
            {
                float e = (float)Math.Sqrt(discriminant);
                float denom = 2 * a;
                t = (-b - e) / denom;
                if (t > 0)
                {
                    return true;
                }

                t = (-b + e) / denom;
                return (t > 0);
            }
        }

        public override Vector3 Normal(Vector3 point)
        {
            return (point - Center).Normalized();
        }
    }
}