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
    public partial class CubePuzz : IDisposable
    {
        private CubeForm    _form   = null;
        private Device      _device = null;

        private void CreateDevice(CubeForm topLevelForm)
        {
            PresentParameters pp = new PresentParameters();
            pp.BackBufferFormat = Format.Unknown;
            pp.Windowed = true;
            pp.SwapEffect = SwapEffect.Discard;
            pp.EnableAutoDepthStencil = true;
            pp.AutoDepthStencilFormat = DepthFormat.D16;
            pp.PresentationInterval = PresentInterval.Immediate;

            try
            {
                this.CreateDevice(topLevelForm, pp);
            }
            catch (DirectXException ex)
            {
                throw ex;
            }
        }

        private void CreateDevice(CubeForm topLevelForm, PresentParameters presentationParameters)
        {
            try
            {
                this._device = new Device
                (
                    0,
                    DeviceType.Hardware,
                    topLevelForm.Handle,
                    CreateFlags.HardwareVertexProcessing,
                    presentationParameters
                );
            }
            catch (DirectXException ex1)
            {
                Debug.WriteLine(ex1.ToString());

                try
                {
                    this._device = new Device
                    (
                        0,
                        DeviceType.
                        Hardware,
                        topLevelForm.Handle,
                        CreateFlags.SoftwareVertexProcessing,
                        presentationParameters
                    );
                }
                catch (DirectXException ex2)
                {
                    Debug.WriteLine(ex2.ToString());

                    try
                    {
                        this._device = new Device
                        (
                            0,
                            DeviceType.Reference,
                            topLevelForm.Handle,
                            CreateFlags.SoftwareVertexProcessing,
                            presentationParameters
                        );
                    }
                    catch (DirectXException ex3)
                    {
                        throw ex3;
                    }
                }
            }
        }
    }
}
