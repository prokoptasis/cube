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
        private Microsoft.DirectX.Direct3D.Font _font = null;

        private void CreateFont()
        {
            try
            {
                FontDescription fd = new FontDescription();
                fd.Height = 12;
                fd.FaceName = "SYSTEM";
                this._font = new Microsoft.DirectX.Direct3D.Font(this._device, fd);
            }
            catch (DirectXException ex)
            {
                throw ex;
            }
        }
    }
}
