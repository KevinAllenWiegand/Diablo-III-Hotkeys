using System;
using System.Diagnostics;
using System.IO;

namespace DiabloIIIHotkeys
{
    internal class LogfileTraceListener : TraceListener
    {
        private string _LogfileLocation = Utils.Instance.LogfileFilename;

        public override void Write(string message)
        {
            try
            {
                File.AppendAllText(_LogfileLocation, message);
            }
            catch
            {
            }
        }

        public override void WriteLine(string message)
        {
            try
            {
                File.AppendAllText(_LogfileLocation, $"{message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
