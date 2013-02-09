using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace DCPU16_emu
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //testing code
            string p = @"set a,5
set b,6
:loop
add b, 1
set pc,[loop]
set pc,loop
";
            string[] program = p.Split(new char[] { '\n', '\r' },StringSplitOptions.RemoveEmptyEntries);


            foreach (ushort opcode in compile.opcodeify(program))
                Debug.Print("{0:x}",opcode);
            //</testing code>
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
