using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

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
            StreamReader sr = new StreamReader("dcputest.asm");
            string p = sr.ReadToEnd();
            sr.Close();

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
