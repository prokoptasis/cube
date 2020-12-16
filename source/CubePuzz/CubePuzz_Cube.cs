using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Cube
{
    public class CubeObject
    {
        // Base
        public string   cubename;
        public bool     select;
        public bool     rselect;

        // Cube Matrix
        public Matrix mat;

        // Postion
        public Vector3 pos = Vector3.Empty;
        public Vector3 org = Vector3.Empty;
        
        // Scale
        public Vector3 scl = Vector3.Empty;

        // Angle
        public float cx = 0.0f;
        public float cy = 0.0f;
        public float cz = 0.0f;

        // private
        private int LENG = 1;
        private VertexBuffer vertexBuff = null;
        private VertexBuffer selectBuff = null;
        private VertexBuffer normalBuff = null;
        private VertexBuffer texCoordBuffer = null;
        private Texture      texture    = null;

        public float distance = 0.0f;

        private Mesh scannerMesh = null;

        public CustomVertex.PositionTextured[] intersectedVertices = null;
        public bool isPicking = false;

        CustomVertex.PositionTextured[] verticex = new CustomVertex.PositionTextured[24];

        Vector2[] cubeTexCoords =
        {
            new Vector2( 0.75f, 0.00f), new Vector2( 0.75f, 0.50f), new Vector2( 1.00f, 0.00f), new Vector2( 1.00f, 0.50f),
            new Vector2( 0.25f, 0.00f), new Vector2( 0.50f, 0.00f), new Vector2( 0.25f, 0.50f), new Vector2( 0.50f, 0.50f),
            new Vector2( 0.00f, 0.00f), new Vector2( 0.00f, 0.50f), new Vector2( 0.25f, 0.00f), new Vector2( 0.25f, 0.50f),
            new Vector2( 0.25f, 0.50f), new Vector2( 0.50f, 0.50f), new Vector2( 0.25f, 1.00f), new Vector2( 0.50f, 1.00f),
            new Vector2( 0.00f, 0.50f), new Vector2( 0.00f, 1.00f), new Vector2( 0.25f, 0.50f), new Vector2( 0.25f, 1.00f),
            new Vector2( 0.50f, 0.00f), new Vector2( 0.50f, 0.50f), new Vector2( 0.75f, 0.00f), new Vector2( 0.75f, 0.50f)
        };

        int[] selectcolor =  
        {
            Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),
            Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),
            Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),
            Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),
            Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),
            Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb(),Color.DarkGray.ToArgb()
        };

        int[] normalcolor =  
        {
            Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),
            Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),
            Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),
            Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),
            Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),
            Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb(),Color.White.ToArgb()
        };

        short[] indices = 
        { 
            0,1,2,1,3,2,            // Back Face 
            4,5,6,5,7,6,            // Front Face 
            8,9,10,9,11,10,         // Top Face 
            12,13,14,13,15,14,      // Bottom Face 
            16,17,18,17,19,18,      // Left Face 
            20,21,22,21,23,22       // Right Face 
        };

        public CubeObject(Device device, string cname, Vector3 vs, Vector3 ve)
        {
            cubename = cname;

            mat = new Matrix();
            select = false;
            
            mat = Matrix.Identity;

            org.TransformCoordinate(mat);
            pos.TransformCoordinate(mat);

            scl = new Vector3(1.0f, 1.0f, 1.0f);

            org = Vector3.TransformCoordinate(new Vector3(0, 0, 0), mat);
            pos = Vector3.TransformCoordinate(new Vector3(0, 0, 0), mat);

            CreateBuff(device);
            CreateCubePolygon(device, vs, ve);
        }

        ~CubeObject()
        {
            if (this.vertexBuff != null)
                this.vertexBuff.Dispose();
        }
       
        public void CreateBuff(Device device)
        {
            vertexBuff = new VertexBuffer(typeof(CustomVertex.PositionTextured), 24, device, Usage.None, VertexFormats.Position, Pool.Managed);
            selectBuff = new VertexBuffer(typeof(int), 24, device, Usage.Dynamic | Usage.WriteOnly, VertexFormats.Diffuse, Pool.Default);
            normalBuff = new VertexBuffer(typeof(int), 24, device, Usage.Dynamic | Usage.WriteOnly, VertexFormats.Diffuse, Pool.Default);
            texCoordBuffer = new VertexBuffer(typeof(Vector2), 24, device, Usage.Dynamic | Usage.WriteOnly, VertexFormats.Texture1, Pool.Default);
            scannerMesh = new Mesh(72, 24, MeshFlags.Managed, CustomVertex.PositionTextured.Format, device);
        }

        private void CreateCubePolygon(Device device, Vector3 vs, Vector3 ve)
        {
            CustomVertex.PositionTextured[] vertices = new CustomVertex.PositionTextured[24];

            // back
            vertices[0] = new CustomVertex.PositionTextured(vs.X, vs.Y, ve.Z, 0.75f, 0.00f);
            vertices[1] = new CustomVertex.PositionTextured(ve.X, vs.Y, ve.Z, 0.75f, 0.50f);
            vertices[2] = new CustomVertex.PositionTextured(vs.X, ve.Y, ve.Z, 1.00f, 0.00f);
            vertices[3] = new CustomVertex.PositionTextured(ve.X, ve.Y, ve.Z, 1.00f, 0.50f);

            // front
            vertices[4] = new CustomVertex.PositionTextured(vs.X, vs.Y, vs.Z, 0.25f, 0.00f);
            vertices[5] = new CustomVertex.PositionTextured(vs.X, ve.Y, vs.Z, 0.50f, 0.00f);
            vertices[6] = new CustomVertex.PositionTextured(ve.X, vs.Y, vs.Z, 0.25f, 0.50f);
            vertices[7] = new CustomVertex.PositionTextured(ve.X, ve.Y, vs.Z, 0.50f, 0.50f);

            // top
            vertices[8] = new CustomVertex.PositionTextured(vs.X, vs.Y, vs.Z, 0.00f, 0.00f);
            vertices[9] = new CustomVertex.PositionTextured(ve.X, vs.Y, vs.Z, 0.00f, 0.50f);
            vertices[10] = new CustomVertex.PositionTextured(vs.X, vs.Y, ve.Z, 0.25f, 0.00f);
            vertices[11] = new CustomVertex.PositionTextured(ve.X, vs.Y, ve.Z, 0.25f, 0.50f);

            // bottom
            vertices[12] = new CustomVertex.PositionTextured(vs.X, ve.Y, vs.Z, 0.25f, 0.50f);
            vertices[13] = new CustomVertex.PositionTextured(vs.X, ve.Y, ve.Z, 0.50f, 0.50f);
            vertices[14] = new CustomVertex.PositionTextured(ve.X, ve.Y, vs.Z, 0.25f, 1.00f);
            vertices[15] = new CustomVertex.PositionTextured(ve.X, ve.Y, ve.Z, 0.50f, 1.00f);

            // left
            vertices[16] = new CustomVertex.PositionTextured(ve.X, vs.Y, ve.Z, 0.00f, 0.50f);
            vertices[17] = new CustomVertex.PositionTextured(ve.X, vs.Y, vs.Z, 0.00f, 1.00f);
            vertices[18] = new CustomVertex.PositionTextured(ve.X, ve.Y, ve.Z, 0.25f, 0.50f);
            vertices[19] = new CustomVertex.PositionTextured(ve.X, ve.Y, vs.Z, 0.25f, 1.00f);

            // right
            vertices[20] = new CustomVertex.PositionTextured(vs.X, vs.Y, ve.Z, 0.50f, 0.00f);
            vertices[21] = new CustomVertex.PositionTextured(vs.X, ve.Y, ve.Z, 0.50f, 0.50f);
            vertices[22] = new CustomVertex.PositionTextured(vs.X, vs.Y, vs.Z, 0.75f, 0.00f);
            vertices[23] = new CustomVertex.PositionTextured(vs.X, ve.Y, vs.Z, 0.75f, 0.50f);

            verticex = vertices;

            using (GraphicsStream data = vertexBuff.Lock(0, 0, LockFlags.None)) { data.Write(vertices); vertexBuff.Unlock(); }
            using (GraphicsStream data = selectBuff.Lock(0, 0, LockFlags.None)) { data.Write(selectcolor); selectBuff.Unlock(); }
            using (GraphicsStream data = normalBuff.Lock(0, 0, LockFlags.None)) { data.Write(normalcolor); normalBuff.Unlock(); }
            using (GraphicsStream data = texCoordBuffer.Lock(0, 0, LockFlags.None)) { data.Write(cubeTexCoords); texCoordBuffer.Unlock(); }

            // Mesh
            using (VertexBuffer vb = scannerMesh.VertexBuffer) { GraphicsStream data = vb.Lock(0, 0, LockFlags.None); data.Write(vertices); vb.Unlock(); }
            using (IndexBuffer ib = scannerMesh.IndexBuffer) { ib.SetData(indices, 0, LockFlags.None); }

            // Set Texture
            this.texture = TextureLoader.FromFile(device, "texture.jpg");

            device.SamplerState[0].MinFilter = TextureFilter.Linear;
            device.SamplerState[0].MagFilter = TextureFilter.Linear;

            VertexElement[] elements = new VertexElement[]
            {
                //		        Stream  Offset        Type                    Method                 Usage                      Usage Index
                new VertexElement( 0,     0,  DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position,          0),
                new VertexElement( 1,     0,  DeclarationType.Color,  DeclarationMethod.Default, DeclarationUsage.Color,             0),
                new VertexElement( 2,     0,  DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),

                VertexElement.VertexDeclarationEnd 
            };

            device.VertexDeclaration = new VertexDeclaration(device, elements);


            // Set Postion
            org = pos = new Vector3(ve.X - LENG, vs.Y - LENG, vs.Z - LENG);            
        }

        public void Render(Device device, float x, float y, float z, Vector3 sc, int selected, float zoomfactor)
        {
            SelectCube(selected);
            CubeTransform(device, x, y, z, sc, zoomfactor);

            device.SetStreamSource(0, vertexBuff, 0);

            if (select && intersectedVertices != null)
            {
                device.SetStreamSource(1, selectBuff, 0);
            }
            else
            {
                device.SetStreamSource(1, normalBuff, 0);
            }

            device.SetStreamSource(2, texCoordBuffer, 0);

            device.SetTexture(0, texture);


            //// Mesh DrawSubset
            //if (this.select && intersectedVertices != null)
            //{
            //    device.RenderState.FillMode = FillMode.Point;

            //    for (int i = 0; i < scannerMesh.NumberAttributes; i++)
            //    {
            //        scannerMesh.DrawSubset(i);
            //    }

            //    device.RenderState.FillMode = FillMode.WireFrame;
            //    device.DrawUserPrimitives(PrimitiveType.TriangleList, 1, intersectedVertices);
            //}
            //else
            //{
            //    device.RenderState.FillMode = FillMode.Solid;

            //    for (int i = 0; i < scannerMesh.NumberAttributes; i++)
            //    {
            //        scannerMesh.DrawSubset(i);
            //    }
            //}

            // Draw Cube
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 4, 2);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 8, 2);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 12, 2);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 16, 2);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 20, 2);
        }

        public void DoPicking(Device device, int mouseX, int mouseY)
        {
            Viewport vp = device.Viewport;
            Matrix vw = device.GetTransform(TransformType.View);
            Matrix pj = device.GetTransform(TransformType.Projection);
            Matrix wd = device.GetTransform(TransformType.World);

            if (mouseX < 0) mouseX = 0;
            if (mouseY < 0) mouseY = 0;
            if (mouseX > vp.Width) mouseX = (short)vp.Width;
            if (mouseY > vp.Height) mouseY = (short)vp.Height;

            Vector3 near = new Vector3(mouseX, mouseY,0.0f);
            Vector3 far = new Vector3(mouseX, mouseY,1.0f);

            near.Unproject(vp,pj,vw, mat);
            far.Unproject(vp, pj, vw, mat);

            IntersectInformation closestIntersection;

            bool intersects = scannerMesh.Intersect(near,far,out closestIntersection);

            this.select = intersects;
            this.distance = closestIntersection.Dist;
            
            if (intersects && this.select)
            {
                HighlightIntersectedFace(closestIntersection);
            }
        }

        public void InitDist()
        {
            this.distance = 0.0f;
        }

        private void HighlightIntersectedFace(IntersectInformation ii)
        {
            short[] intersectedIndices = new short[3];
            short[] indices = (short[])scannerMesh.LockIndexBuffer(typeof(short), LockFlags.ReadOnly, scannerMesh.NumberFaces * 3);
            Array.Copy(indices, ii.FaceIndex * 3, intersectedIndices, 0, 3);
            scannerMesh.UnlockIndexBuffer();

            CustomVertex.PositionTextured[] tempIntersectedVertices = new CustomVertex.PositionTextured[3];
            CustomVertex.PositionTextured[] meshVertices = (CustomVertex.PositionTextured[])scannerMesh.LockVertexBuffer(typeof(CustomVertex.PositionTextured), LockFlags.ReadOnly, scannerMesh.NumberVertices);
            tempIntersectedVertices[0] = meshVertices[intersectedIndices[0]];
            tempIntersectedVertices[1] = meshVertices[intersectedIndices[1]];
            tempIntersectedVertices[2] = meshVertices[intersectedIndices[2]];
            scannerMesh.UnlockVertexBuffer();

            this.intersectedVertices = tempIntersectedVertices;
        }
        
        private void CubeTransform(Device device, float x, float y, float z, Vector3 scale, float zoomfactor)
        {
            if (scale != new Vector3(1.0f, 1.0f, 1.0f))
            {
                if (scale.X < 1.0f)
                {
                    scl.X /= zoomfactor;
                    scl.Y /= zoomfactor;
                    scl.Z /= zoomfactor;
                }
                else
                {
                    scl *= zoomfactor;                    
                }

                mat *= Matrix.Scaling(scale);
            }

            if (rselect)
            {
                if (x != 0) { cx = cx + x; mat *= Matrix.RotationX(Geometry.DegreeToRadian(x)); }
                if (y != 0) { cy = cy + y; mat *= Matrix.RotationY(Geometry.DegreeToRadian(y)); }
                if (z != 0) { cz = cz + z; mat *= Matrix.RotationZ(Geometry.DegreeToRadian(z)); }
            }

            pos = Vector3.TransformCoordinate(org, mat);
            pos.X = (float)Math.Round((float)pos.X, 1);
            pos.Y = (float)Math.Round((float)pos.Y, 1);
            pos.Z = (float)Math.Round((float)pos.Z, 1);

            device.SetTransform(TransformType.World, mat);
        }


        private void SelectCube(int selected)
        {
            switch (selected)
            {
                case 1:
                    if (pos.Y == FindCubeMaxXYZ(scl.Y)) { rselect = true; } else { rselect = false; }
                    break;
                case 2:
                    if (pos.Y == 0.0f) { rselect = true; } else { rselect = false; }
                    break;
                case 3:
                    if (pos.Y == -(FindCubeMaxXYZ(scl.Y))) { rselect = true; } else { rselect = false; }
                    break;
                case 4:
                    if (pos.X == -(FindCubeMaxXYZ(scl.X))) { rselect = true; } else { rselect = false; }
                    break;
                case 5:
                    if (pos.X == 0.0f) { rselect = true; } else { rselect = false; }
                    break;
                case 6:
                    if (pos.X == FindCubeMaxXYZ(scl.X)) { rselect = true; } else { rselect = false; }
                    break;
                case 7:
                    if (pos.Z == -(FindCubeMaxXYZ(scl.Z))) { rselect = true; } else { rselect = false; }
                    break;
                case 8:
                    if (pos.Z == 0.0f) { rselect = true; } else { rselect = false; }
                    break;
                case 9:
                    if (pos.Z == FindCubeMaxXYZ(scl.Z)) { rselect = true; } else { rselect = false; }
                    break;
                //case 0:
                //    select = false;
                //    break;
            }
        }

        private float FindCubeMaxXYZ(float zoomfactor)
        {
            return (float)Math.Round((LENG + 1.1f) * zoomfactor, 1);
        }
    }
}
