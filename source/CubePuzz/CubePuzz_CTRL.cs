using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Cube
{
    public partial class CubePuzz : IDisposable
    {
        private bool[] _keys = new bool[256];
        private Point _oldMousePoint = Point.Empty;
        private Point _startMousePoint = Point.Empty;

        private float MAXDEGREE = 90.0f;
        private float MOVDEGREE = 1.0f;
        private int CONFIRMEDDELTA = 50;
        private int DIAGONALDELTA = 60;

        // Lens Setting
        private float _lensPosRadius = 12.0f;
        private float _lensPosTheta = 270.0f;
        private float _lensPosPhi   = 0.0f;

        // Rotation Setting
        private float _x = 0.0f;
        private float _y = 0.0f;
        private float _z = 0.0f;

        private float _tx = 0.0f;
        private float _ty = 0.0f;
        private float _tz = 0.0f;

        // Scale
        private Vector3 _scale = new Vector3(1.0f, 1.0f, 1.0f);
        private float _zoomfactor = 1.05f;
        
        // Rotation Selection
        private int _selected = 0;
        private int _viewhigh = 0;
        private int _viewface = 0;
        private int _selectedface = 0;
        public string _mouseway = null;

        private void CreateInputEvent(CubeForm topLevelForm)
        {
            topLevelForm.KeyDown += new KeyEventHandler(this.form_KeyDown);
            topLevelForm.KeyUp += new KeyEventHandler(this.form_KeyUp);
            topLevelForm.MouseMove += new MouseEventHandler(this.form_MouseMove);
            topLevelForm.MouseWheel += new MouseEventHandler(this.form_MouseWheel);
            topLevelForm.MouseDown += new MouseEventHandler(this.form_MouseDown);
            topLevelForm.MouseUp += new MouseEventHandler(this.form_MouseUp);
        }

        // Dellme
        private Vector3 GetWorldMousePosition(Point spoint)
        {
            Viewport vp = _device.Viewport;
            Matrix vw = _device.GetTransform(TransformType.View);
            Matrix pj = _device.GetTransform(TransformType.Projection);
            Matrix wd = _device.GetTransform(TransformType.World);

            if (spoint.X < 0) spoint.X = 0;
            if (spoint.Y < 0) spoint.Y = 0;
            if (spoint.X > vp.Width) spoint.X = (short)vp.Width;
            if (spoint.Y > vp.Height) spoint.Y = (short)vp.Height;

            Vector3 near = new Vector3(spoint.X, spoint.Y, 0.0f);

            near.Unproject(vp,pj,vw, wd);
            
            return near;
        }

        private void form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this._startMousePoint = e.Location;

                foreach (CubeObject cube in _clist)
                {
                    cube.rselect = false;
                    cube.select = false;
                    cube.distance = 0.0f;
                }
                // DoPicking
                foreach (CubeObject cube in _clist)
                {
                    cube.DoPicking(_device, e.X, e.Y);
                }

                foreach (CubeObject cube in _clist)
                {
                    // If Closer Object Exists
                    if (cube.select && IsNearDistExist(cube))
                    {
                        cube.select = false;
                        cube.distance = 0.0f;
                    }
                }
            }
        }

        private void SelectViewSide()
        {
            float the = 270.0f;

            if (this._lensPosTheta > 0)
                the = (this._lensPosTheta + 360) % 360;
            else
                the = 360 - (Math.Abs(this._lensPosTheta)) % 360;

            float phi = this._lensPosPhi;

            _viewhigh = _viewface = 0;

            if (phi > 45) { _viewhigh = 2; }                        //Top Down
            else if (phi < -45) { _viewhigh = 3; }                  //Bottop Up
            else if (phi <= 45 && phi >= -45) { _viewhigh = 1; }    //Middle

            if (the >= 45 && the < 135) { _viewface = 4; }          // Back
            else if (the >= 135 && the < 225) { _viewface = 3; }    // Left
            else if (the >= 225 && the < 315) { _viewface = 1; }   // Front
            else if (the >= 315 || the < 45) { _viewface = 2; }    // Right
        }

        private void GetSelectedFace(CubeObject cube)
        {
            if (cube == null)
                return;

            this._selectedface = 0;

            Vector3 vtr1 = new Vector3(cube.intersectedVertices[0].X, cube.intersectedVertices[0].Y, cube.intersectedVertices[0].Z);
            Vector3 vtr2 = new Vector3(cube.intersectedVertices[1].X, cube.intersectedVertices[1].Y, cube.intersectedVertices[1].Z);
            Vector3 vtr3 = new Vector3(cube.intersectedVertices[2].X, cube.intersectedVertices[2].Y, cube.intersectedVertices[2].Z);

            vtr1.TransformNormal(cube.mat);
            vtr2.TransformNormal(cube.mat);
            vtr3.TransformNormal(cube.mat);

            Math.Round(vtr1.X, 5); Math.Round(vtr1.Y, 5); Math.Round(vtr1.Z, 5);
            Math.Round(vtr2.X, 5); Math.Round(vtr2.Y, 5); Math.Round(vtr2.Z, 5);
            Math.Round(vtr3.X, 5); Math.Round(vtr3.Y, 5); Math.Round(vtr3.Z, 5);

            if (vtr1.Z < 0 && vtr2.Z < 0 && vtr3.Z < 0 && Math.Abs(vtr1.Z - vtr2.Z) < 1 && Math.Abs(vtr2.Z - vtr3.Z) < 1 && Math.Abs(vtr2.Z - vtr3.Z) < 1)
                this._selectedface = 1;
            else if (vtr1.X > 0 && vtr2.X > 0 && vtr3.X > 0 && Math.Abs(vtr1.X - vtr2.X) < 1 && Math.Abs(vtr2.X - vtr3.X) < 1 && Math.Abs(vtr2.X - vtr3.X) < 1)
                this._selectedface = 2;
            else if (vtr1.X < 0 && vtr2.X < 0 && vtr3.X < 0 && Math.Abs(vtr1.X - vtr2.X) < 1 && Math.Abs(vtr2.X - vtr3.X) < 1 && Math.Abs(vtr2.X - vtr3.X) < 1)
                this._selectedface = 3;
            else if (vtr1.Z > 0 && vtr2.Z > 0 && vtr3.Z > 0 && Math.Abs(vtr1.Z - vtr2.Z) < 1 && Math.Abs(vtr2.Z - vtr3.Z) < 1 && Math.Abs(vtr2.Z - vtr3.Z) < 1)
                this._selectedface = 4;
            else if (vtr1.Y > 0 && vtr2.Y > 0 && vtr3.Y > 0 && Math.Abs(vtr1.Y - vtr2.Y) < 1 && Math.Abs(vtr2.Y - vtr3.Y) < 1 && Math.Abs(vtr2.Y - vtr3.Y) < 1)
                this._selectedface = 5;
            else if (vtr1.Y < 0 && vtr2.Y < 0 && vtr3.Y < 0 && Math.Abs(vtr1.Y - vtr2.Y) < 1 && Math.Abs(vtr2.Y - vtr3.Y) < 1 && Math.Abs(vtr2.Y - vtr3.Y) < 1)
                this._selectedface = 6;

            this.selface = this._selectedface;

        }

        private void RotateCubeWay(Vector3 cubepos,string Axis)
        {
            switch (Axis)
            {
                case "X" :
                    if (cubepos.X < 0) { this._selected = 4; }
                    else if (cubepos.X == 0) { this._selected = 5; }
                    else if (cubepos.X > 0) { this._selected = 6; }
                    break;
                case "Y":
                    if (cubepos.Y < 0) { this._selected = 3; }
                    else if (cubepos.Y == 0) { this._selected = 2; }
                    else if (cubepos.Y > 0) { this._selected = 1; }
                    break;
                case "Z":
                    if (cubepos.Z < 0) { this._selected = 7;  }
                    else if (cubepos.Z == 0) { this._selected = 8; }
                    else if (cubepos.Z > 0) { this._selected = 9; }
                    break;
            }
        }

        private void SelectRotateAxis(CubeObject cube)
        {
            float the = 270.0f;

            if (this._lensPosTheta < 0)
                the = (this._lensPosTheta + 360) % 360;
            else
                the = 360 - (Math.Abs(this._lensPosTheta)) % 360;
            float phi = Math.Abs(this._lensPosPhi);

            Vector3 cubepos = GetCubeObjPos();

            if (this._viewhigh == 1)
            {
                if(this._viewface == 1)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                        if (the < 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                        else if (the < 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                    }
                }
                else if (this._viewface == 2)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 0 && the < 45)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR" ) { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                        else if (the <= 360)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 0 && the < 45)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                        else if (the <= 360)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                    }
                }
                else if (this._viewface == 3)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the > 180)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                        else if (the <= 180)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                        else if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                    }
                }
                if (this._viewface == 4)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                        if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                        else if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                    }
                }
            }

            if (this._viewhigh == 2)
            {
                if (this._viewface == 1)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                        if (the < 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                    }
                }
                else if (this._viewface == 2)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 0 && the < 45)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                        else if (the <= 360)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                    }
                }
                else if (this._viewface == 3)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the > 180)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                        else if (the <= 180)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                    }
                }
                if (this._viewface == 4)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                        if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                    }
                }
            }



            if (this._viewhigh == 3)
            {
                if (this._viewface == 1)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                        if (the < 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                        else if (the < 270)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                    }
                }
                else if (this._viewface == 2)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 0 && the < 45)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                        else if (the <= 360)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 0 && the < 45)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                        else if (the <= 360)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                    }
                }
                else if (this._viewface == 3)
                {
                    if (this._selectedface == 1)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the > 180)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                        else if (the <= 180)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                        else if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        }
                    }
                }
                if (this._viewface == 4)
                {
                    if (this._selectedface == 4)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 2)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 3)
                    {
                        if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Y"); RotateYP(); }
                        else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Y"); RotateYM(); }
                    }
                    else if (this._selectedface == 5)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                        if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                        }
                    }
                    else if (this._selectedface == 6)
                    {
                        if (the >= 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DLL") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "ULL" || this._mouseway == "UUL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                        else if (the < 90)
                        {
                            if (this._mouseway == "UP" || this._mouseway == "UUL" || this._mouseway == "UUR") { RotateCubeWay(cubepos, "X"); RotateXM(); }
                            else if (this._mouseway == "DN" || this._mouseway == "DDL" || this._mouseway == "DDR") { RotateCubeWay(cubepos, "X"); RotateXP(); }
                            else if (this._mouseway == "LT" || this._mouseway == "DLL" || this._mouseway == "ULL") { RotateCubeWay(cubepos, "Z"); RotateZP(); }
                            else if (this._mouseway == "RT" || this._mouseway == "DRR" || this._mouseway == "URR") { RotateCubeWay(cubepos, "Z"); RotateZM(); }
                        }
                    }
                }
            }

            // Mid-high & Red 
            if (this._viewhigh == 1 && this._viewface == 2 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 2 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 2 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 2 && this._mouseway == "RT")
            {
            }


            // Mid-high & Blue 
            if (this._viewhigh == 1 && this._viewface == 3 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 3 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 3 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 3 && this._mouseway == "RT")
            {
            }

            // Mid-high & Yellow
            if (this._viewhigh == 1 && this._viewface == 4 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 4 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 4 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 1 && this._viewface == 4 && this._mouseway == "RT")
            {
            }

            // Up-Down & Green 
            if (this._viewhigh == 2 && this._viewface == 1 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 1 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 1 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 1 && this._mouseway == "RT")
            {
            }

            // Up-Down & Red 
            if (this._viewhigh == 2 && this._viewface == 2 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 2 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 2 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 2 && this._mouseway == "RT")
            {
            }


            // Up-Down & Blue 
            if (this._viewhigh == 2 && this._viewface == 3 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 3 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 3 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 3 && this._mouseway == "RT")
            {
            }

            // Up-Down & Yellow
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "RT")
            {
            }

            // Bottom-Up & Green
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "RT")
            {
            }

            // Bottom-Up & Red
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "RT")
            {
            }

            // Bottom-Up & Blue
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "RT")
            {
            }

            // Bottom-Up & Yellow
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "UP")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "DN")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "LT")
            {
            }
            if (this._viewhigh == 2 && this._viewface == 4 && this._mouseway == "RT")
            {
            }
        }

        private void form_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point endMousePoint = new Point(e.X,e.Y);

                Point MouseDelta = new Point(_startMousePoint.X - endMousePoint.X, _startMousePoint.Y - endMousePoint.Y);

                if (Math.Abs(MouseDelta.X) > Math.Abs(MouseDelta.Y) && Math.Abs(MouseDelta.X) > CONFIRMEDDELTA && Math.Abs(MouseDelta.X) - Math.Abs(MouseDelta.Y) > DIAGONALDELTA)
                    if (MouseDelta.X > 0) _mouseway = "LT";
                    else _mouseway = "RT";
                else if (Math.Abs(MouseDelta.Y) > Math.Abs(MouseDelta.X) && Math.Abs(MouseDelta.Y) > CONFIRMEDDELTA && Math.Abs(MouseDelta.Y) - Math.Abs(MouseDelta.X) > DIAGONALDELTA)
                    if (MouseDelta.Y > 0) _mouseway = "UP";
                    else _mouseway = "DN";
                else if (MouseDelta.X > 0 && MouseDelta.Y > 0 && Math.Abs(MouseDelta.Y) > Math.Abs(MouseDelta.X)) _mouseway = "UUL";
                else if (MouseDelta.X > 0 && MouseDelta.Y > 0 && Math.Abs(MouseDelta.Y) < Math.Abs(MouseDelta.X)) _mouseway = "ULL";
                else if (MouseDelta.X < 0 && MouseDelta.Y > 0 && Math.Abs(MouseDelta.Y) > Math.Abs(MouseDelta.X)) _mouseway = "UUR";
                else if (MouseDelta.X < 0 && MouseDelta.Y > 0 && Math.Abs(MouseDelta.Y) < Math.Abs(MouseDelta.X)) _mouseway = "URR";
                else if (MouseDelta.X > 0 && MouseDelta.Y < 0 && Math.Abs(MouseDelta.Y) > Math.Abs(MouseDelta.X)) _mouseway = "DDL";
                else if (MouseDelta.X > 0 && MouseDelta.Y < 0 && Math.Abs(MouseDelta.Y) < Math.Abs(MouseDelta.X)) _mouseway = "DLL";
                else if (MouseDelta.X < 0 && MouseDelta.Y < 0 && Math.Abs(MouseDelta.Y) > Math.Abs(MouseDelta.X)) _mouseway = "DDR";
                else if (MouseDelta.X < 0 && MouseDelta.Y < 0 && Math.Abs(MouseDelta.Y) < Math.Abs(MouseDelta.X)) _mouseway = "DRR";
                else
                    _mouseway = "What?";

                Vector3 cubepos = GetCubeObjPos();
                this.ViewVector = GetCubeObjPos();

                //Viewport vp = _device.Viewport;
                //Matrix vw = _device.GetTransform(TransformType.View);
                //Matrix pj = _device.GetTransform(TransformType.Projection);
                //Matrix wd = _device.GetTransform(TransformType.World);
                //Matrix vi = _device.GetTransform(TransformType.View); vi.Invert();
                // ViewVector.TransformCoordinate(wd * vi);

                //Front
                float the = (this._lensPosTheta+360) % 360;
                float phi = this._lensPosPhi;

                CubeObject cube = GetSelectedCube();

                SelectViewSide();

                if (cube != null)
                {
                    GetSelectedFace(cube);
                    SelectRotateAxis(cube);
                }

                /*
                if (phi > 45)
                {
                    if (the >= 45 && the < 135)
                    {
                        if (_mouseway == "RT") 
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;

                            RotateZP();
                        }
                        else if (_mouseway == "LT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;
                            RotateZM();
                        }
                        else if (_mouseway == "DN")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;

                            RotateXP();
                        }
                        else if (_mouseway == "UP")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;

                            RotateXM();
                        }
                    }
                    else if (the >= 135 && the < 225)
                    {
                        if (_mouseway == "DN")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "UP")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "LT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "RT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                    }
                    else if (the >= 225 && the < 315)
                    {
                        if (_mouseway == "LT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "RT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "UP")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "DN")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                    }
                    else if (the >= 315 || the < 45)
                    {
                        if (_mouseway == "UP")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "DN")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "RT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "LT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                    }
                }

                if (phi < -45)
                {
                    if (the >= 45 && the < 135)
                    {
                        if (_mouseway == "LT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "RT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "DN")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "UP")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                    }
                    else if (the >= 135 && the < 225)
                    {
                        if (_mouseway == "DN")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "UP")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "RT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "LT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                    }
                    else if (the >= 225 && the < 315)
                    {
                        if (_mouseway == "RT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "LT")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "UP")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "DN")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;

                            
                            RotateXM();
                        }
                    }
                    else if (the >= 315 || the < 45)
                    {
                        if (_mouseway == "UP")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "DN")
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                        else if (_mouseway == "LT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "RT")
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                    }
                }

                if (phi <= 45 && phi >= -45)
                {
                    if (the >= 225 || the < 45)
                    {
                        if (_mouseway == "LT") // (cubepos.Z < 0 && the < 315 && _mouseway == "LT")
                        {
                            // 123 중 하나
                            if (cubepos.Y > 0.0f)
                                this._selected = 1;
                            if (cubepos.Y == 0.0f)
                                this._selected = 2;
                            if (cubepos.Y < 0.0f)
                                this._selected = 3;


                            RotateYP();
                        }
                        else if (_mouseway == "RT") //(cubepos.Z < 0 && the < 315 && _mouseway == "RT")
                        {
                            // 123 중 하나
                            if (cubepos.Y > 0.0f)
                                this._selected = 1;
                            if (cubepos.Y == 0.0f)
                                this._selected = 2;
                            if (cubepos.Y < 0.0f)
                                this._selected = 3;


                            RotateYM();
                        }
                        else if (_mouseway == "UP" && cubepos.Z < 0 && the <= 315 && the >= 225)
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "DN" && cubepos.Z < 0 && the <= 315 && the >= 225)
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                        else if (_mouseway == "UP" && cubepos.X>0 && (the > 315 || the < 45))
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "DN" && cubepos.X > 0 && (the > 315 || the < 45))
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                    }

                    if (the < 225)
                    {
                        if (_mouseway == "LT") // (cubepos.Z < 0 && the < 315 && _mouseway == "LT")
                        {
                            // 123 중 하나
                            if (cubepos.Y > 0.0f)
                                this._selected = 1;
                            if (cubepos.Y == 0.0f)
                                this._selected = 2;
                            if (cubepos.Y < 0.0f)
                                this._selected = 3;


                            RotateYP();
                        }
                        else if (_mouseway == "RT") //(cubepos.Z < 0 && the < 315 && _mouseway == "RT")
                        {
                            // 123 중 하나
                            if (cubepos.Y > 0.0f)
                                this._selected = 1;
                            if (cubepos.Y == 0.0f)
                                this._selected = 2;
                            if (cubepos.Y < 0.0f)
                                this._selected = 3;


                            RotateYM();
                        }
                        else if (_mouseway == "DN" && cubepos.Z > 0 && the <= 135 && the > 45)
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXP();
                        }
                        else if (_mouseway == "UP" && cubepos.Z > 0 && the <= 135 && the > 45)
                        {
                            // 456 중 하나
                            if (cubepos.X > 0.0f)
                                this._selected = 6;
                            if (cubepos.X == 0.0f)
                                this._selected = 5;
                            if (cubepos.X < 0.0f)
                                this._selected = 4;


                            RotateXM();
                        }
                        else if (_mouseway == "DN" && cubepos.X < 0 && the > 135 && the < 225)
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZP();
                        }
                        else if (_mouseway == "UP" && cubepos.X < 0 && the > 135 && the < 225)
                        {
                            // 789 중 하나
                            if (cubepos.Z > 0.0f)
                                this._selected = 9;
                            if (cubepos.Z == 0.0f)
                                this._selected = 8;
                            if (cubepos.Z < 0.0f)
                                this._selected = 7;


                            RotateZM();
                        }
                    }
                }
*/

                
            }
        }

        private Vector3 GetCubeObjPos()
        {
            Vector3 v3 = Vector3.Empty;

            foreach (CubeObject cube in _clist)
            {
                if (cube.select)
                {
                    v3 = cube.pos;
                }
            }

            return v3;
        }

        private CubeObject GetSelectedCube()
        {
            foreach (CubeObject cube in _clist)
            {
                if (cube.select)
                    return cube;
            }

            return null;
        }

        private void  form_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0) { this._scale *= _zoomfactor; }
            else if (e.Delta < 0) { this._scale.X /= _zoomfactor; this._scale.Y /= _zoomfactor; this._scale.Z /= _zoomfactor; }
        }

        private void initctlvar()
        {
            //this._x = 0.0f; this._y = 0.0f; this._z = 0.0f;
            this._scale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        private void CheckRotateMove()
        {
            if (Math.Abs(_tx) > 0.0f) { _ty = _tz = _y = _z = 0.0f; _tx = _tx - _x; }
            else if (Math.Abs(_tx) == 0) { _tx = 0.0f; _x = 0.0f; }

            if (Math.Abs(_ty) > 0.0f) { _tx = _tz = _x = _z = 0.0f; _ty = _ty - _y; }
            else if (Math.Abs(_ty) == 0) { _ty = 0.0f; _y = 0.0f; }
            
            if (Math.Abs(_tz) > 0.0f) { _ty = _tx = _y = _x = 0.0f; _tz = _tz - _z; }
            else if (Math.Abs(_tz) == 0) { _tz = 0.0f; _z = 0.0f; }

        }

        private void RotateYP()
        {
            if (_tx == 0.0f && _ty == 0.0f && _tz == 0.0f)
            {
                _ty = MAXDEGREE;
                _y = MOVDEGREE;
            }
            else if (_ty > 0.0f)
            {
                _y = MOVDEGREE;
            }
            else if (Math.Abs(_ty) == 0)
            {
                _ty = 0.0f; _y = 0.0f;
            }
        }
        private void RotateYM()
        {
            if (_tx == 0.0f && _ty == 0.0f && _tz == 0.0f)
            {
                _ty = -MAXDEGREE;
                _y = -MOVDEGREE;
            }
            else if (_ty > 0.0f)
            {
                _y = -MOVDEGREE;
            }
            else if (Math.Abs(_ty) == 0)
            {
                _ty = 0.0f; _y = 0.0f;
            }
        }
        private void RotateXP()
        {
            if (_tx == 0.0f && _ty == 0.0f && _tz == 0.0f)
            {
                _tx = MAXDEGREE;
                _x = MOVDEGREE;
            }
            else if (_tx > 0.0f)
            {
                _x = MOVDEGREE;
            }
            else if (Math.Abs(_tx) == 0)
            {
                _tx = 0.0f; _x = 0.0f;
            }
        }
        private void RotateXM()
        {

            if (_tx == 0.0f && _ty == 0.0f && _tz == 0.0f)
            {
                _tx = -MAXDEGREE;
                _x = -MOVDEGREE;
            }
            else if (_tx > 0.0f)
            {
                _x = -MOVDEGREE;
            }
            else if (Math.Abs(_tx) == 0)
            {
                _tx = 0.0f; _x = 0.0f;
            }
        }
        private void RotateZP()
        {
            if (_tx == 0.0f && _ty == 0.0f && _tz == 0.0f)
            {
                _tz = MAXDEGREE;
                _z = MOVDEGREE;
            }
            else if (_tz > 0.0f)
            {
                _z = MOVDEGREE;
            }
            else if (Math.Abs(_tz) == 0)
            {
                _tz = 0.0f;
                _z = 0.0f;
            }
        }
        private void RotateZM()
        {
            if (_tx == 0.0f && _ty == 0.0f && _tz == 0.0f)
            {
                _tz = -MAXDEGREE;
                _z = -MOVDEGREE;
            }
            else if (_tz > 0.0f)
            {
                _z = -MOVDEGREE;
            }
            else if (Math.Abs(_tz) == 0)
            {
                _tz = 0.0f;
                _z = 0.0f;
            }
        }

        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyCode < this._keys.Length)
            {
                this._keys[(int)e.KeyCode] = true;
            }

            if (this._keys[(int)Keys.D1]) { this._selected = 1; initctlvar(); }
            if (this._keys[(int)Keys.D2]) { this._selected = 2; initctlvar(); }
            if (this._keys[(int)Keys.D3]) { this._selected = 3; initctlvar(); }
            if (this._keys[(int)Keys.D4]) { this._selected = 4; initctlvar(); }
            if (this._keys[(int)Keys.D5]) { this._selected = 5; initctlvar(); }
            if (this._keys[(int)Keys.D6]) { this._selected = 6; initctlvar(); }
            if (this._keys[(int)Keys.D7]) { this._selected = 7; initctlvar(); }
            if (this._keys[(int)Keys.D8]) { this._selected = 8; initctlvar(); }
            if (this._keys[(int)Keys.D9]) { this._selected = 9; initctlvar(); }
            if (this._keys[(int)Keys.D0]) { this._selected = 0; initctlvar(); }

            if (this._keys[(int)Keys.Escape])
            {
                this._form.Close();
            }

            if (this._keys[(int)Keys.W])
            {
                if (this._selected == 4 || this._selected == 5 || this._selected == 6) 
                {
                    RotateXP();
                }
            }

            if (this._keys[(int)Keys.S])
            {
                if (this._selected == 4 || this._selected == 5 || this._selected == 6)
                {
                    RotateYM();
                }
            }
            
            if (this._keys[(int)Keys.A])
            {
                if (this._selected == 1 || this._selected == 2 || this._selected == 3)
                {
                    RotateYP();
                }
            }

            if (this._keys[(int)Keys.D])
            {
                if (this._selected == 1 || this._selected == 2 || this._selected == 3)
                {
                    RotateYM();
                }
            }

            if (this._keys[(int)Keys.Q])
            {
                if (this._selected == 7 || this._selected == 8 || this._selected == 9)
                {
                    RotateZP();
                }
            }

            if (this._keys[(int)Keys.E])
            {
                if (this._selected == 7 || this._selected == 8 || this._selected == 9)
                {
                    RotateZM();
                }
            }
        }

        private void form_KeyUp(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyCode < this._keys.Length)
            {
                this._keys[(int)e.KeyCode] = false;
            }
        }

        private void form_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this._lensPosTheta -= e.Location.X - this._oldMousePoint.X;
                this._lensPosPhi += e.Location.Y - this._oldMousePoint.Y;

                if (this._lensPosPhi >= 90.0f)
                {
                    this._lensPosPhi = 89.9999f;
                }
                else if (this._lensPosPhi <= -90.0f)
                {
                    this._lensPosPhi = -89.9999f;
                }
            }

            this._oldMousePoint = e.Location;
        }

        private bool IsNearDistExist(CubeObject scube)
        {
            foreach (CubeObject cube in _clist)
            {
                if (cube.cubename != scube.cubename && cube.select == true && Math.Abs(scube.distance) >= Math.Abs(cube.distance))
                {
                    return true;
                }
            }

            return false;
        }

        // Camera
        private void SettingCamera()
        {
            float radius = this._lensPosRadius;
            float theta = Geometry.DegreeToRadian(this._lensPosTheta);
            float phi = Geometry.DegreeToRadian(this._lensPosPhi);

            Vector3 lensPosition = new Vector3 ( (float)(radius * Math.Cos(theta) * Math.Cos(phi)), (float)(radius * Math.Sin(phi)), (float)(radius * Math.Sin(theta) * Math.Cos(phi)) );

            this._device.Transform.View = Matrix.LookAtLH ( lensPosition, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f) );
            this._device.Transform.Projection = Matrix.PerspectiveFovLH ( Geometry.DegreeToRadian(60.0f),(float)this._device.Viewport.Width / (float)this._device.Viewport.Height,1.0f,100.0f );
        }
    }
}

