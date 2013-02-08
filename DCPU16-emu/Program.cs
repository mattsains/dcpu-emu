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
            string[] program=
            {
                "set b,1",
                "set a,2",
                "set pc,end",
                "add b,a",
                ":end",
                "",
                "add a,b"
            };


            foreach (ushort opcode in compile.opcodeify(program))
                Debug.Print("{0:x}",opcode);
            //</testing code>
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
