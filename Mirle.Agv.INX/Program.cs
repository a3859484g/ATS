//#define SIMULATE 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.View;


namespace Mirle.Agv
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {
#if (SIMULATE)
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            InitialForm initialForm = new InitialForm();
            Application.Run(initialForm);
#else
            bool isFirstOpen;
            Mutex mutex = new Mutex(false, Application.ProductName, out isFirstOpen);

            if (isFirstOpen)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //Application.Run(new Form1());

                InitialForm initialForm = new InitialForm();
                Application.Run(initialForm);
            }
            else
            {
                MessageBox.Show("重複開啟!");
            }
            mutex.Dispose();
#endif
        }
    }
}
