﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DiabloIIIHotkeys.Commands;
using DiabloIIIHotkeys.Serialization;
using DiabloIIIHotkeys.ViewModels;

namespace DiabloIIIHotkeys
{
    internal class HotkeyManager
    {
        private static Lazy<HotkeyManager> _Instance = new Lazy<HotkeyManager>(() => new HotkeyManager());

        public static HotkeyManager Instance
        {
            get { return _Instance.Value; }
        }

        private const int _TimerDueTime = 5000;
        private const string _DefaultKey1 = "1";
        private const string _DefaultKey2 = "2";
        private const string _DefaultKey3 = "3";
        private const string _DefaultKey4 = "4";
        private const string _DefaultToggleRunningMacroKey = "A";
        private const uint _DefaultToggleRunningMacroModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;

        private IntPtr _MainWindowHandle;
        private HwndSource _MainWindowHwndSource;
        private Timer _Timer;

        private static readonly List<string> _ProcessNames = new List<string>() { "Diablo III64", "Diablo III" };
        private static readonly IDictionary<int, HotkeyDefinition> _HotkeyDefinitions = new Dictionary<int, HotkeyDefinition>();

        public event EventHandler<EventArgs> HotkeysRegisteredChanged;

        public bool AreHotkeysRegistered { get; set; }

        private HotkeyManager()
        {
            Preferences preferences = null;
            var key1Value = String.Empty;
            var key2Value = String.Empty;
            var key3Value = String.Empty;
            var key4Value = String.Empty;

            if (File.Exists(Utils.Instance.PreferencesFilename))
            {
                try
                {
                    preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Utils.Instance.PreferencesFilename));
                }
                catch
                {
                }
            }

            if (preferences != null)
            {
                if (preferences.SkillKeybindOverrides != null)
                {
                    if (!String.IsNullOrEmpty(preferences.SkillKeybindOverrides.One?.Key))
                    {
                        if (preferences.SkillKeybindOverrides.One.UseNumPad && int.TryParse(preferences.SkillKeybindOverrides.One.Key, out int value))
                        {
                            key1Value = $"{{NumPad{preferences.SkillKeybindOverrides.One.Key}}}";
                        }
                        else
                        {
                            key1Value = preferences.SkillKeybindOverrides.One.Key;
                        }
                    }

                    if (!String.IsNullOrEmpty(preferences.SkillKeybindOverrides.Two?.Key))
                    {
                        if (preferences.SkillKeybindOverrides.Two.UseNumPad && int.TryParse(preferences.SkillKeybindOverrides.Two.Key, out int value))
                        {
                            key2Value = $"{{NumPad{preferences.SkillKeybindOverrides.Two.Key}}}";
                        }
                        else
                        {
                            key2Value = preferences.SkillKeybindOverrides.Two.Key;
                        }
                    }

                    if (!String.IsNullOrEmpty(preferences.SkillKeybindOverrides.Three?.Key))
                    {
                        if (preferences.SkillKeybindOverrides.Three.UseNumPad && int.TryParse(preferences.SkillKeybindOverrides.Three.Key, out int value))
                        {
                            key3Value = $"{{NumPad{preferences.SkillKeybindOverrides.Three.Key}}}";
                        }
                        else
                        {
                            key3Value = preferences.SkillKeybindOverrides.Three.Key;
                        }
                    }

                    if (!String.IsNullOrEmpty(preferences.SkillKeybindOverrides.Four?.Key))
                    {
                        if (preferences.SkillKeybindOverrides.Four.UseNumPad && int.TryParse(preferences.SkillKeybindOverrides.Four.Key, out int value))
                        {
                            key4Value = $"{{NumPad{preferences.SkillKeybindOverrides.Four.Key}}}";
                        }
                        else
                        {
                            key4Value = preferences.SkillKeybindOverrides.Four.Key;
                        }
                    }
                }
            }

            var toggleRunningMacroKey = _DefaultToggleRunningMacroKey;
            var toggleRunningMacroModifiers = _DefaultToggleRunningMacroModifiers;

            if (!String.IsNullOrEmpty(preferences?.ToggleProfileMacro?.Key))
            {
                toggleRunningMacroKey = preferences.ToggleProfileMacro.Key;
                toggleRunningMacroModifiers = NativeMethods.MOD_NONE;

                if (preferences.ToggleProfileMacro.CtrlModifier)
                {
                    toggleRunningMacroModifiers |= NativeMethods.MOD_CONTROL;
                }

                if (preferences.ToggleProfileMacro.AltModifier)
                {
                    toggleRunningMacroModifiers |= NativeMethods.MOD_ALT;
                }

                if (preferences.ToggleProfileMacro.ShiftModifier)
                {
                    toggleRunningMacroModifiers |= NativeMethods.MOD_SHIFT;
                }
            }

            _HotkeyDefinitions.Add(0, new HotkeyDefinition(toggleRunningMacroModifiers, NativeMethods.GetVirtualKeyForValue(toggleRunningMacroKey), ProfileKeyActionParameters.ToggleRunningMacroProfileParameters));

            ProfileKeyManager.Instance.SetKeybinds(new Dictionary<int, string>()
            {
                { 1, key1Value ?? _DefaultKey1 },
                { 2, key2Value ?? _DefaultKey2 },
                { 3, key3Value ?? _DefaultKey3 },
                { 4, key4Value ?? _DefaultKey4 },
            });
        }

        public void Start()
        {
            _MainWindowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            _MainWindowHwndSource = HwndSource.FromHwnd(_MainWindowHandle);
            _MainWindowHwndSource.AddHook(HwndHook);

            _Timer = new Timer(TimerCallback, null, _TimerDueTime, Timeout.Infinite);
        }

        private void TimerCallback(object state)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var foundProcess = false;

                    foreach (var processName in _ProcessNames)
                    {
                        try
                        {
                            foundProcess = Process.GetProcessesByName(processName).FirstOrDefault() != null;

                            if (foundProcess)
                            {
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }

                    if (foundProcess)
                    {
                        if (!AreHotkeysRegistered)
                        {
                            Logger.Instance.Log("Registering hotkeys.");
                            AreHotkeysRegistered = true;
                            OnHotkeysRegisteredChanged();

                            foreach (var kvp in _HotkeyDefinitions)
                            {
                                if (kvp.Value == null)
                                {
                                    continue;
                                }

                                NativeMethods.RegisterHotKey(_MainWindowHandle, kvp.Key, kvp.Value.HotkeyModifiers, kvp.Value.HotkeyKey);
                            }
                        }
                    }
                    else
                    {
                        UnregisterHotkeysIfNecessary();
                        ((ILogHandler)Application.Current.MainWindow.DataContext)?.ClearLog();
                    }

                    _Timer.Change(_TimerDueTime, Timeout.Infinite);
                });
            }
            catch
            {
            }
        }

        public void Stop()
        {
            _Timer.Change(Timeout.Infinite, Timeout.Infinite);
            _MainWindowHwndSource.RemoveHook(HwndHook);
            UnregisterHotkeysIfNecessary();
        }

        private void UnregisterHotkeysIfNecessary()
        {
            if (!AreHotkeysRegistered)
            {
                return;
            }

            Logger.Instance.Log("Unregistering hotkeys.");

            foreach (var kvp in _HotkeyDefinitions)
            {
                if (kvp.Value == null)
                {
                    continue;
                }

                NativeMethods.UnregisterHotKey(_MainWindowHandle, kvp.Key);
            }

            AreHotkeysRegistered = false;
            OnHotkeysRegisteredChanged();
            ((ICommand)new PerformProfileKeyActionCommand()).Execute(ProfileKeyActionParameters.StopActionParameters);
        }

        private void OnHotkeysRegisteredChanged()
        {
            var handler = HotkeysRegisteredChanged;

            handler?.Invoke(this, EventArgs.Empty);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case NativeMethods.WM_HOTKEY:
                    var hotkeyId = wParam.ToInt32();

                    Logger.Instance.Log($"Received Hotkey ID {hotkeyId}.");

                    if (_HotkeyDefinitions.TryGetValue(hotkeyId, out HotkeyDefinition hotkeyDefinition))
                    {
                        handled = true;

                        Task.Run(() =>
                        {
                            Logger.Instance.Log($"Executing handler for Hotkey ID {hotkeyId}.");

                            var start = DateTime.Now;

                            ((ICommand)new PerformProfileKeyActionCommand()).Execute(hotkeyDefinition.ActionParameters);

                            var milliseconds = DateTime.Now.Subtract(start).TotalMilliseconds;

                            Logger.Instance.Log($"Handler for Hotkey ID {hotkeyId} completed in {milliseconds}ms.");
                        });
                    }

                    break;
            }

            return IntPtr.Zero;
        }
    }
}
