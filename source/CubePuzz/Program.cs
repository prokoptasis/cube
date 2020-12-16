using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace Cube
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //CubeForm cubefrm = new CubeForm();

            using (CubeForm cubefrm = new CubeForm())
            //CubePuzz cubepuzz = new CubePuzz();

            using (CubePuzz cubepuzz = new CubePuzz())
            {
                if (cubepuzz.InitializeApplication(cubefrm))
                {
                    cubefrm.Show();

                    while (cubefrm.Created)
                    {
                        cubepuzz.MainLoop();
                        Thread.Sleep(1);
                        Application.DoEvents();
                    }
                }
                else
                {
                }
            }

        }
    }
}