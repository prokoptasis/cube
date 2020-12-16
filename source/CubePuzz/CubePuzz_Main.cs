using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;

namespace Cube
{
    public partial class CubePuzz : IDisposable
    {
        private ArrayList _clist = new ArrayList();
        private Texture _background = null;
        private Sprite sprite = null;

        public Vector3 ViewVector = new Vector3(0.0f,0.0f,0.0f);

        private CustomVertex.PositionTextured[] Vertices = null;
        private int selface = 0;

        public void CreateCube(Device device, string cubename, Vector3 vs, Vector3 ve)
        {
            CubeObject cube = new CubeObject(device,cubename,vs,ve);
            this._clist.Add(cube);
        }

        public bool InitializeApplication(CubeForm topLevelForm)
        {
            this._form = topLevelForm;
            this.CreateInputEvent(topLevelForm);

            try
            {
                this.CreateDevice(topLevelForm);
                this.CreateFont();

            }
            catch (DirectXException ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            this.SettingCamera();

            // front-top
            CreateCube(this._device, "cube020", new Vector3(-3.1f, 3.1f, -1.1f), new Vector3(-1.1f, 1.1f, -3.1f));
            CreateCube(this._device, "cube120", new Vector3(-1.0f, 3.1f, -1.1f), new Vector3(1.0f, 1.1f, -3.1f));
            CreateCube(this._device, "cube220", new Vector3(1.1f, 3.1f, -1.1f), new Vector3(3.1f, 1.1f, -3.1f));
            // front-middle                  "
            CreateCube(this._device, "cube010", new Vector3(-3.1f, 1.0f, -1.1f), new Vector3(-1.1f, -1.0f, -3.1f));
            CreateCube(this._device, "cube110", new Vector3(-1.0f, 1.0f, -1.1f), new Vector3(1.0f, -1.0f, -3.1f));
            CreateCube(this._device, "cube210", new Vector3(1.1f, 1.0f, -1.1f), new Vector3(3.1f, -1.0f, -3.1f));
            // front-low                     "
            CreateCube(this._device, "cube000", new Vector3(-3.1f, -1.1f, -1.1f), new Vector3(-1.1f, -3.1f, -3.1f));
            CreateCube(this._device, "cube100", new Vector3(-1.0f, -1.1f, -1.1f), new Vector3(1.0f, -3.1f, -3.1f));
            CreateCube(this._device, "cube200", new Vector3(1.1f, -1.1f, -1.1f), new Vector3(3.1f, -3.1f, -3.1f));
            // midldle-top                   "
            CreateCube(this._device, "cube021", new Vector3(-3.1f, 3.1f, 1.0f), new Vector3(-1.1f, 1.1f, -1.0f));
            CreateCube(this._device, "cube121", new Vector3(-1.0f, 3.1f, 1.0f), new Vector3(1.0f, 1.1f, -1.0f));
            CreateCube(this._device, "cube221", new Vector3(1.1f, 3.1f, 1.0f), new Vector3(3.1f, 1.1f, -1.0f));
            // middle-middle                 "
            CreateCube(this._device, "cube011", new Vector3(-3.1f, 1.0f, 1.0f), new Vector3(-1.1f, -1.0f, -1.0f));
            CreateCube(this._device, "cube111", new Vector3(-1.0f, 1.0f, 1.0f), new Vector3(1.0f, -1.0f, -1.0f));
            CreateCube(this._device, "cube211", new Vector3(1.1f, 1.0f, 1.0f), new Vector3(3.1f, -1.0f, -1.0f));
            // middle-low                    "
            CreateCube(this._device, "cube001", new Vector3(-3.1f, -1.1f, 1.0f), new Vector3(-1.1f, -3.1f, -1.0f));
            CreateCube(this._device, "cube101", new Vector3(-1.0f, -1.1f, 1.0f), new Vector3(1.0f, -3.1f, -1.0f));
            CreateCube(this._device, "cube201", new Vector3(1.1f, -1.1f, 1.0f), new Vector3(3.1f, -3.1f, -1.0f));
            // low-top                       "
            CreateCube(this._device, "cube002", new Vector3(-3.1f, -1.1f, 3.1f), new Vector3(-1.1f, -3.1f, 1.1f));
            CreateCube(this._device, "cube102", new Vector3(-1.0f, -1.1f, 3.1f), new Vector3(1.0f, -3.1f, 1.1f));
            CreateCube(this._device, "cube202", new Vector3(1.1f, -1.1f, 3.1f), new Vector3(3.1f, -3.1f, 1.1f));
            // low-middle                    "
            CreateCube(this._device, "cube012", new Vector3(-3.1f, 1.0f, 3.1f), new Vector3(-1.1f, -1.0f, 1.1f));
            CreateCube(this._device, "cube112", new Vector3(-1.0f, 1.0f, 3.1f), new Vector3(1.0f, -1.0f, 1.1f));
            CreateCube(this._device, "cube212", new Vector3(1.1f, 1.0f, 3.1f), new Vector3(3.1f, -1.0f, 1.1f));
            // low-low                       "
            CreateCube(this._device, "cube022", new Vector3(-3.1f, 3.1f, 3.1f), new Vector3(-1.1f, 1.1f, 1.1f));
            CreateCube(this._device, "cube122", new Vector3(-1.0f, 3.1f, 3.1f), new Vector3(1.0f, 1.1f, 1.1f));
            CreateCube(this._device, "cube222", new Vector3(1.1f, 3.1f, 3.1f), new Vector3(3.1f, 1.1f, 1.1f));

            this._device.RenderState.Lighting = false;

            unchecked
            {
                this._background = TextureLoader.FromFile(_device, "background.bmp", 640, 480, 1, Usage.None, Format.A8R8G8B8, Pool.Managed, Filter.None, Filter.None, (int)0xFF000000);
            }

            sprite = new Sprite(_device);

            return true;
        }

        public void MainLoop()
        {
            this.SettingCamera();

            this._device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.DarkBlue.ToArgb(), 1.0f, 0);

            sprite.Begin(SpriteFlags.AlphaBlend);
            sprite.Draw(_background, Vector3.Empty, new Vector3(1, 1, 1), Color.White.ToArgb());
            sprite.End();

            this._device.BeginScene();           


            CheckRotateMove();

            foreach (CubeObject cube in this._clist)
            {
                cube.Render(this._device, this._x, this._y, this._z, this._scale, this._selected, this._zoomfactor);
            }

            this._font.DrawText(null, "Draw Cubes", 23, 25, Color.Black);
            this._font.DrawText(null, "Mouse X/Y:" + this._oldMousePoint.X.ToString() + "   /   " + this._oldMousePoint.Y.ToString(), 23, 35, Color.Black);
            this._font.DrawText(null, "θ/φ：" + this._lensPosTheta + "   /   " + this._lensPosPhi, 23, 45, Color.Black);
            this._font.DrawText(null, "X/Y/Z：" + this._x + "=" + Geometry.DegreeToRadian(this._x) + "   /   " + this._y + "=" + Geometry.DegreeToRadian(this._y) + "   /   " + this._z + "=" + Geometry.DegreeToRadian(this._z), 23, 55, Color.Black);
            this._font.DrawText(null, "확대：" + this._scale.X, 23, 65, Color.Black);
            this._font.DrawText(null, "선택：" + this._selected, 23, 75, Color.Black);
            this._font.DrawText(null, "방향：" + this._mouseway, 23, 85, Color.Black);
            this._font.DrawText(null, "벡터：" + this.ViewVector.X, 23, 95, Color.Black);

            Vertices = new CustomVertex.PositionTextured[9];

            foreach (CubeObject cube in _clist)
            {
                if (cube.select)
                    Vertices = cube.intersectedVertices;
            }

            Vector3 vtr1 = new Vector3(Vertices[0].X, Vertices[0].Y, Vertices[0].Z);
            Vector3 vtr2 = new Vector3(Vertices[1].X, Vertices[1].Y, Vertices[1].Z);
            Vector3 vtr3 = new Vector3(Vertices[2].X, Vertices[2].Y, Vertices[2].Z);

            Viewport vp = _device.Viewport;
            Matrix vw = _device.GetTransform(TransformType.View);
            Matrix pj = _device.GetTransform(TransformType.Projection);
            Matrix wd = _device.GetTransform(TransformType.World);
            Matrix vi = _device.GetTransform(TransformType.View); vi.Invert();
            ViewVector.TransformCoordinate(wd*pj*vi);

            CubeObject cubex = GetSelectedCube();

            Matrix mat = new Matrix();

            if (cubex != null)
            {
                vtr1.TransformNormal(cubex.mat); Math.Round(vtr1.X, 5); Math.Round(vtr1.Y, 5); Math.Round(vtr1.Z, 5);
                vtr2.TransformNormal(cubex.mat); Math.Round(vtr2.X, 5); Math.Round(vtr2.Y, 5); Math.Round(vtr2.Z, 5);
                vtr3.TransformNormal(cubex.mat); Math.Round(vtr3.X, 5); Math.Round(vtr3.Y, 5); Math.Round(vtr3.Z, 5);
            }

            this._font.DrawText(null, "벌트：   " + vtr1.X + "    / " + vtr1.Y + "    / " + vtr1.Z, 23, 105, Color.Black);
            this._font.DrawText(null, "벌트：   " + vtr2.X + "    / " + vtr2.Y + "    / " + vtr2.Z, 23, 115, Color.Black);
            this._font.DrawText(null, "벌트：   " + vtr3.X + "    / " + vtr3.Y + "    / " + vtr3.Z, 23, 125, Color.Black);
            this._font.DrawText(null, "벌트：   " + this.selface, 23, 135, Color.Black);

            Vector3 pos = Vector3.Empty;

            foreach (CubeObject cube in _clist)
            {
                if (cube.select)
                     pos = cube.pos;
            }


            this._font.DrawText(null, "벌트：   " + pos.X + "    /    " + pos.Y + "    /    " + pos.Z, 23, 145, Color.Black);


            this._device.EndScene();
            this._device.Present();

            this.initctlvar();
        }

        public void Dispose()
        {
            if (this._font != null)
            {
                this._font.Dispose();
            }
            if (this._device != null)
            {
                this._device.Dispose();
            }
            if (this.sprite != null)
            {
                this.sprite.Dispose();
            }
            if (this._background != null)
            {
                this._background.Dispose();
            }
        }
    }
}
