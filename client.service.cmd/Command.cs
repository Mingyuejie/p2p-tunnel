using System.Runtime.InteropServices;

namespace client.service.cmd
{
    public class Command
    {
        public Command()
        {

            System.Diagnostics.Process proc = new System.Diagnostics.Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc.StartInfo.FileName = "cmd.exe";
            }
            else
            {
                proc.StartInfo.FileName = "/bin/bash";
            }
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Verb = "runas";
            proc.StandardInput.AutoFlush = true;
            proc.Start();

            proc.StandardInput.WriteLine("");
            proc.StandardInput.WriteLine("exit");

            proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();


            proc.WaitForExit();
            proc.Close();
            proc.Dispose();
        }
    }
}
