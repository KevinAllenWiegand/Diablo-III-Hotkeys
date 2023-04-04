using System;
using System.IO;

namespace DiabloIIIHotkeys
{
    internal class Utils
    {
        private static string _BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaPuffer Enterprises\\Diablo III Hotkeys");

        private string _ProfilesFilename;
        public string ProfilesFilename
        {
            get
            {
                if (_ProfilesFilename == null)
                {
                    _ProfilesFilename = $"{_BasePath}\\Profiles.json";
                }

                return _ProfilesFilename;
            }
        }

        private string _PreferenesFilename;
        public string PreferencesFilename
        {
            get
            {
                if (_PreferenesFilename == null)
                {
                    _PreferenesFilename = $"{_BasePath}\\Preferences.json";
                }

                return _PreferenesFilename;
            }
        }

        private string _LogfileFilename;
        public string LogfileFilename
        {
            get
            {
                if (_LogfileFilename == null)
                {
                    _LogfileFilename = $"{_BasePath}\\Log.txt";
                }

                return _LogfileFilename;
            }
        }

        private static Lazy<Utils> _Instance = new Lazy<Utils>(() => new Utils());
        public static Utils Instance
        {
            get { return _Instance.Value; }
        }
    }
}
