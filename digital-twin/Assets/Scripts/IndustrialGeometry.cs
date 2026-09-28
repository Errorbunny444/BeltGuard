using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeltGuard
{
    /// <summary>Millimetre-scale hard-surface parts with bevels and real cable cross-sections.</summary>
    public static class IndustrialGeometry
    {
        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

        public static Material Finish(string name, Color color, float metallic = 0f, float smoothness = .35f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;
            return m;
        }

        public static Transform Group(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }

        public static GameObject Part(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
        {
            var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(parent, false); g.transform.localPosition = position;
            g.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            return g;
        }

        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, float bevel = .4f)
        {
            string key = "box" + size.ToString("F3") + bevel.ToString("F3");
            if (!Meshes.TryGetValue(key, out Mesh mesh))
            {
                float r = Mathf.Clamp(bevel, .00001f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .45f);
                Vector3 h = size * .5f, inner = h - Vector3.one * r;
                var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
                Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                foreach (Vector3 n in faces)
                {
                    Vector3 u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.up;
                    Vector3 v = Vector3.Cross(n, u);
                    float hu = Vector3.Dot(h, Abs(u)), hv = Vector3.Dot(h, Abs(v));
                    float[] us = { -hu, -hu + r*.293f, -hu + r, hu-r, hu-r*.293f, hu };
                    float[] vs = { -hv, -hv + r*.293f, -hv+r, hv-r, hv-r*.293f, hv };
                    int start = vertices.Count;
                    for (int j = 0; j < 6; j++) for (int i = 0; i < 6; i++)
                    {
                        Vector3 p = Vector3.Scale(n, h) + u*us[i] + v*vs[j];
                        Vector3 c = new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                        Vector3 normal = (p-c).normalized;
                        vertices.Add(c+normal*r); normals.Add(normal); uv.Add(new Vector2((us[i]+hu)/(2*hu),(vs[j]+hv)/(2*hv)));
                    }
                    for (int j=0;j<5;j++) for(int i=0;i<5;i++)
                    {
                        int a=start+j*6+i; triangles.AddRange(new[]{a,a+1,a+7,a,a+7,a+6});
                    }
                }
                mesh = new Mesh { name = "Bevelled " + size };
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
                Meshes[key]=mesh;
            }
            return Part(name,parent,position,mesh,material);
        }

        /// <summary>Revolved profile in radius/axial-Z coordinates; supports hollow lens barrels.</summary>
        public static GameObject Lathe(string name, Transform parent, Vector3 position, Vector2[] profile, Material material, int segments=64)
        {
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            for(int j=0;j<profile.Length;j++) for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                vertices.Add(new Vector3(Mathf.Cos(a)*profile[j].x,Mathf.Sin(a)*profile[j].x,profile[j].y));
                uv.Add(new Vector2((float)i/segments,(float)j/(profile.Length-1)));
            }
            for(int j=0;j<profile.Length-1;j++) for(int i=0;i<segments;i++)
            {
                int a=j*(segments+1)+i; int b=a+segments+1;
                triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
            }
            var mesh=new Mesh {name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return Part(name,parent,position,mesh,material);
        }

        public static GameObject Cylinder(string name,Transform parent,Vector3 position,float radius,float length,Material material,int segments=48)
        {
            return Lathe(name,parent,position,new[]{new Vector2(0,0),new Vector2(radius,0),new Vector2(radius,length),new Vector2(0,length)},material,segments);
        }

        public static GameObject Ring(string name,Transform parent,Vector3 position,float outer,float inner,float length,Material material)
        {
            return Lathe(name,parent,position,new[]{new Vector2(inner,0),new Vector2(outer-.2f,0),new Vector2(outer,.2f),new Vector2(outer,length-.2f),new Vector2(outer-.2f,length),new Vector2(inner,length),new Vector2(inner,0)},material);
        }

        public static void Bolt(Transform parent,Vector3 position,Material steel,float radius=1.45f)
        {
            Ring("Socket head M3",parent,position,radius,radius*.46f,1.25f,steel);
        }

        public static void Cable(string name,Transform parent,Vector3[] control,float radius,Material material)
        {
            var points=new List<Vector3>();
            for(int i=0;i<control.Length-1;i++) for(int s=0;s<10;s++)
            {
                float t=s/10f;Vector3 a=control[Mathf.Max(0,i-1)],b=control[i],c=control[i+1],d=control[Mathf.Min(control.Length-1,i+2)];
                points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
            points.Add(control[control.Length-1]);
            var vertices=new List<Vector3>();var triangles=new List<int>();var normals=new List<Vector3>();
            for(int j=0;j<points.Count;j++)
            {
                Vector3 tangent=(points[Mathf.Min(j+1,points.Count-1)]-points[Mathf.Max(j-1,0)]).normalized;
                Vector3 u=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.95f?Vector3.right:Vector3.up).normalized,v=Vector3.Cross(tangent,u);
                for(int i=0;i<10;i++){float a=i*Mathf.PI/5;Vector3 n=u*Mathf.Cos(a)+v*Mathf.Sin(a);vertices.Add(points[j]+n*radius);normals.Add(n);}
            }
            for(int j=0;j<points.Count-1;j++)for(int i=0;i<10;i++)
            {int a=j*10+i,b=j*10+(i+1)%10;triangles.AddRange(new[]{a,b,a+10,b,b+10,a+10});}
            var mesh=new Mesh {name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();Part(name,parent,Vector3.zero,mesh,material);
        }

        public static void Label(string text,Transform parent,Vector3 position,float height,Color color,Vector3 facing)
        {
            var t=Group("Label - "+text,parent,position);t.localRotation=Quaternion.LookRotation(-facing,Vector3.up);
            var label=t.gameObject.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=height*.1f;
            label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=color;
        }

        private static Vector3 Abs(Vector3 a)=>new Vector3(Mathf.Abs(a.x),Mathf.Abs(a.y),Mathf.Abs(a.z));
    }
}


