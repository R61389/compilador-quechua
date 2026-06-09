using System;
using System.Windows.Forms;
using CompiladorQuechua.Forms;

namespace CompiladorQuechua
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
