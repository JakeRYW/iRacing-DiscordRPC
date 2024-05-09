using DiscordRPC;
using GameReaderCommon;
using IRacingReader;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace iRacing.DiscordRichPresence
{
    [PluginDescription("Adds Discord Rich Presence support to iRacing")]
    [PluginAuthor("JakeRYW")]
    [PluginName("iRacing - Discord Rich Presence")]
    public class DiscordRichPresence : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public DiscordRichPresenceSettings Settings;

        private DataSampleEx irData;

        private string sessionType;

        private string trackDisplayName;

        private string carName;

        private DateTime timeJoinedSession;

        private bool timeSet = false;
        private bool presenceActive = false;

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.discordmenuicon);

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "iRacing Discord RPC";

        /// <summary>
        /// The discord client
        /// </summary>
        private static DiscordRpcClient client;

        private static RichPresence presence = new RichPresence()
        {
            Details = "iRacing",
            State = "iRacing",
            Assets = new Assets()
            {
                LargeImageKey = "iracing-logo",
                LargeImageText = "iRacing"
            }
        };

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // Define the value of our property (declared in init)
            if (data.GameRunning && data.GameName == "IRacing" && data.OldData != null && data.NewData != null)
            {
                if (!timeSet)
                {
                    timeJoinedSession = DateTime.UtcNow;
                    timeSet = true;
                }
                if (data?.NewData?.GetRawDataObject() is DataSampleEx)
                {
                    irData = data.NewData.GetRawDataObject() as DataSampleEx;
                }

                trackDisplayName = irData.SessionData.WeekendInfo.TrackDisplayName;
                sessionType = data.NewData.SessionTypeName;
                carName = data.NewData.CarModel;

                if (sessionType == "Offline Testing")
                {
                    presence.State = "Driving the " + carName;
                    presence.Details = sessionType + " - " + trackDisplayName;
                    presence.Timestamps = new Timestamps()
                    {
                        Start = timeJoinedSession
                    };
                }
                else if (sessionType == "Practice")
                {
                    int position = data.NewData.Position;

                    presence.State = "Driving the " + carName;

                    if (position > 0)
                    {
                        presence.Details = "P" + position + " - Practicing at " + trackDisplayName;
                    }
                    else
                    {
                        presence.Details = "Practicing at " + trackDisplayName;

                    }
                }
                else if (sessionType == "Open Qualify" || sessionType == "Closed Qualify")
                {
                    int position = data.NewData.Position;

                    presence.State = "Driving the " + carName;

                    if (position > 0)
                    {
                        presence.Details = "P" + position + " - Qualifying at " + trackDisplayName;
                    }
                    else
                    {
                        presence.Details = "Qualifying at " + trackDisplayName;

                    }
                }
                else if (sessionType == "Race")
                {
                    int position = data.NewData.Position;

                    presence.State = "Driving the " + carName;

                    if (position > 0)
                    {
                        presence.Details = "P" + position + " - Racing at " + trackDisplayName;
                    }
                    else
                    {
                        presence.Details = "Racing at " + trackDisplayName;

                    }
                }

                if (data.NewData.Flag_Blue == 1)
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = "iracing-logo",
                        LargeImageText = "iRacing",
                        SmallImageKey = "flag-blue",
                        SmallImageText = "Blue Flag"
                    };
                }
                else if (data.NewData.Flag_White == 1)
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = "iracing-logo",
                        LargeImageText = "iRacing",
                        SmallImageKey = "flag-white",
                        SmallImageText = "White Flag"
                    };
                }
                else if (data.NewData.Flag_Orange == 1)
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = "iracing-logo",
                        LargeImageText = "iRacing",
                        SmallImageKey = "flag-meatball",
                        SmallImageText = "Meatball"
                    };
                }
                else if (data.NewData.Flag_Checkered == 1)
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = "iracing-logo",
                        LargeImageText = "iRacing",
                        SmallImageKey = "flag-checker",
                        SmallImageText = "Checkered Flag"
                    };
                }
                else
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = "iracing-logo",
                        LargeImageText = "iRacing",
                    };
                }


                client.SetPresence(presence);
                presenceActive = true;

            }
            else
            {
                if(presenceActive)
                {
                    disposeDiscordRichPresence();
                    presenceActive = false;
                }
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
            timeSet = false;
            disposeDiscordRichPresence();
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting iRacing Discord Rich Presence plugin");

            client = new DiscordRpcClient("1091142873740742787");

            initializeDiscordRichPresence();

            // Load settings
            Settings = this.ReadCommonSettings("GeneralSettings", () => new DiscordRichPresenceSettings());

            // Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
            this.AttachDelegate("CurrentDateTime", () => DateTime.Now);

        }

        /// <summary>
        /// Initializes the Discord Rich Presence client to be used.
        /// </summary>
        private void initializeDiscordRichPresence()
        {
            client.Initialize();
        }

        /// <summary>
        /// Clears and disposes the Discord Rich Presence.
        /// </summary>
        private void disposeDiscordRichPresence()
        {
            client.ClearPresence();
            client.Dispose();
        }
    }
}