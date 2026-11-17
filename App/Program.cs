using System;
using System.Windows.Forms;

namespace HuffPressStudio
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Theme.Initialize();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnUnhandledException;

            string problem = CheckEngine();
            if (problem != null)
            {
                MessageBox.Show(problem, "HuffPress Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm());
        }

        // Makes sure the C++ DLL can be loaded before any window is shown.
        private static string CheckEngine()
        {
            try
            {
                HuffmanService.EngineVersion();
                return null;
            }
            catch (DllNotFoundException)
            {
                return "huffman.dll was not found next to the program.\n\n"
                     + "Run build.bat - it compiles the C++ engine first, then the C# app.";
            }
            catch (BadImageFormatException)
            {
                return "huffman.dll was built for a different architecture.\n\n"
                     + "Build it as x64 (use the \"x64 Native Tools Command Prompt\") and try again.";
            }
        }

        private static void OnUnhandledException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("Something unexpected happened:\n\n" + e.Exception.Message,
                            "HuffPress Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
