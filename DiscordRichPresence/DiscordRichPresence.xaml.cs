using iRacing.DiscordRichPresence;
using SimHub.Plugins;
using System.Windows;
using System.Windows.Controls;

namespace iRacing.DiscordRichPresence
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl
    {
        public DiscordRichPresence Plugin { get; }

        public SettingsControlDemo()
        {
            InitializeComponent();
        }

        public SettingsControlDemo(DiscordRichPresence plugin) : this()
        {
            this.Plugin = plugin;
        }
    }
}
