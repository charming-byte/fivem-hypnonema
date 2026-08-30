namespace Hypnonema.Server
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using CitizenFX.Core;
    using CitizenFX.Core.Native;

    using Hypnonema.Server.Communications;
    using Hypnonema.Server.Managers;
    using Hypnonema.Server.Utils;
    using Hypnonema.Shared;
    using Hypnonema.Shared.Models;

    using LiteDB;

    using Logger = Hypnonema.Server.Utils.Logger;

    public class BaseServer : BaseScript
    {
        public static bool IsLoggingEnabled = true;

        public ScreenPlaybackManager PlaybackManager = new ScreenPlaybackManager();

        public ScheduleManager ScheduleManager;

        public ScreenStorageManager StorageManager = new ScreenStorageManager();

        private string connectionString = "Filename=hypnonema.db";

        private LiteDatabase database;

        private NetworkMethod<int> getMaxActiveScaleforms;

        private int maxActiveScaleforms = 10;

        private LiteCollection<Schedule> scheduleCollection;

        private LiteCollection<Screen> screenCollection;

        public BaseServer()
        {
            Self = this;

            this.Tick += this.OnFirstTick;
        }

        public static BaseServer Self { get; private set; }

        public void AddEvent(string eventName, Delegate action)
        {
            this.EventHandlers[eventName] += action;
        }

        public void AddExport(string name, Delegate action)
        {
            this.Exports.Add(name, action);
        }

        public void RemoveEvent(string eventName, Delegate action)
        {
            this.EventHandlers[eventName] -= action;
        }

        /// <summary>
        ///     CalculateMaxActiveScaleforms is used to specify how many scaleforms can be active at the same time.
        ///     The limit of max. active scaleforms is limited by number of *.gfx files in resource's stream directory.
        /// </summary>
        private void CalculateMaxActiveScaleforms()
        {
            var resourcePath = API.GetResourcePath(API.GetCurrentResourceName());

            var streamDirectory = Path.Combine(resourcePath, "stream");

            if (Directory.Exists(streamDirectory))
            {
                var regExp = new Regex(@"hypnonema_texture_renderer\d+\.+gfx");

                this.maxActiveScaleforms = Directory.GetFiles(streamDirectory, "*.gfx")
                    .Where(path => regExp.IsMatch(path)).ToList().Count;

                Logger.Debug($"maxActiveScaleforms: {this.maxActiveScaleforms}");
            }
            else
            {
                Logger.Error($"Failed to read stream directory. Path: {streamDirectory}");
            }
        }

        private async Task OnFirstTick()
        {
            this.Tick -= this.OnFirstTick;

            this.ReadConfiguration();

            this.OpenDatabase();

            // Create Example Screen if Database is empty
            this.PopulateDatabaseIfEmpty();

            this.CalculateMaxActiveScaleforms();

            this.PlaybackManager.Initialize(this.screenCollection);
            this.StorageManager.Initialize(this.screenCollection);
            this.ScheduleManager = await ScheduleManager.Create(this.scheduleCollection);

            await this.ScheduleManager.Start();

            // existing screens mostly wont have this property set, because it was introduced recently, so
            // its set to a default value if the property should be 0
            foreach (var screen in this.screenCollection.FindAll())
            {
                if (screen.MaxRenderDistance != 0) continue;

                screen.MaxRenderDistance = 400;
                this.screenCollection.Update(screen);
            }

            this.getMaxActiveScaleforms = new NetworkMethod<int>(
                Events.GetMaxActiveScaleforms,
                this.OnGetMaxActiveScaleforms);

            await UpdateChecker.CheckForUpdate();
            await Delay(0);
        }

        private void OnGetMaxActiveScaleforms(Player p, int unused)
        {
            this.getMaxActiveScaleforms.Invoke(p, this.maxActiveScaleforms);
        }

        private void OpenDatabase()
        {
            // database fluent mapping
            BsonMapper.Global.Entity<Schedule>().Id(x => x.Id);

            BsonMapper.Global.Entity<Schedule>().DbRef(x => x.Screen, "screens");

            try
            {
                this.database = new LiteDatabase(this.connectionString);

                this.screenCollection = this.database.GetCollection<Screen>("screens");
                this.screenCollection.EnsureIndex(s => s.Name, true);

                this.scheduleCollection = this.database.GetCollection<Schedule>("schedules");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to open database. error: {e.Message}");
                throw;
            }
        }

        private void PopulateDatabaseIfEmpty()
        {
            if (this.screenCollection.Count() >= 1) return;

            var exampleScreen = Screen.CreateExampleScreen();

            try
            {
                this.screenCollection.Insert(exampleScreen);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create example screen: {e.Message}");
                throw;
            }

            Logger.Verbose($"Created example screen: \"{exampleScreen.Name}\".");
        }

        private void ReadConfiguration()
        {
            var resourceName = API.GetCurrentResourceName();

            this.connectionString = ConfigReader.GetConfigKeyValue(
                resourceName,
                "hypnonema_db_connString",
                0,
                this.connectionString);

            IsLoggingEnabled = ConfigReader.GetConfigKeyValue(
                resourceName,
                "hypnonema_logging_enabled",
                0,
                IsLoggingEnabled);
        }
    }
}