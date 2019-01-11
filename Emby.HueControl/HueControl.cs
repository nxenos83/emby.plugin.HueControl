using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emby.HueControl.Configuration;
//using System.Runtime.Serialization.Json;
using System.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Net;
using System.Net.Http;
using MediaBrowser.Controller.Notifications;
using System.Threading;
using MediaBrowser.Controller.Entities;

namespace Emby.HueControl
{

    public class HueControl : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClient _httpClient;

        private List<PauseControl> pauseControl = new List<PauseControl>();
        public class PauseControl
        {
            public string deviceId { get; set; }
            public bool wasPaused { get; set; }
        }

        public PauseControl getPauseControl(string deviceId)
        {
            var c = pauseControl.Where(x => x.deviceId == deviceId).FirstOrDefault();
            if (c == null)
            {
                c = new PauseControl() { deviceId = deviceId };
                pauseControl.Add(c);
            }
            return c;
        }

        public static HueControl Instance { get; private set; }

        public string Name
        {
            get { return "HueControl"; }
        }

        public HueControl(ISessionManager sessionManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            _logger = logManager.GetLogger(Plugin.Instance.Name);
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _userDataManager = userDataManager;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;

            Instance = this;
        }

        public void Dispose()
        {
            //Unbind events
            _sessionManager.PlaybackStart -= PlaybackStart;
            _sessionManager.PlaybackStopped -= PlaybackStopped;
            _sessionManager.PlaybackProgress -= PlaybackProgress;

            //_libraryManager.ItemAdded -= ItemAdded;
        }

        public void Run()
        {
            _sessionManager.PlaybackStart += PlaybackStart;
            _sessionManager.PlaybackStopped += PlaybackStopped;
            _sessionManager.PlaybackProgress += PlaybackProgress;

            //_libraryManager.ItemAdded += ItemAdded;
        }
        
        private void PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            //we are only interested in video
            if (e.Item.MediaType != "Video") { return; }

            _logger.Debug("e.IsPaused: " + e.IsPaused.ToString());

            //var iType = _libraryManager.GetContentType(e.Item);

            var pauseControl = getPauseControl(e.DeviceId);
            _logger.Debug("pauseControl.wasPaused" + pauseControl.wasPaused.ToString());

            if (e.IsPaused & pauseControl.wasPaused == false)
            {
                _logger.Debug("Playback Paused event");
                _logger.Debug(_jsonSerializer.SerializeToString(e));
                pauseControl.wasPaused = true;

                var DeviceOptions = Plugin.Instance.Configuration.Options.Where(i => i.embyDeviceID == e.DeviceId && i.Enabled);

                if (DeviceOptions.Count() > 0)
                {
                    var a = DeviceOptions.First();
                    //send dim-up command
                    sendDimUp(DeviceOptions.First());
                }
            }
            else if (e.IsPaused == false & pauseControl.wasPaused)
            {
                _logger.Debug("Playback Resume event");
                _logger.Debug(_jsonSerializer.SerializeToString(e));

                getPauseControl(e.DeviceId).wasPaused = false;

                var DeviceOptions = Plugin.Instance.Configuration.Options.Where(i => i.embyDeviceID == e.DeviceId && i.Enabled);

                if (DeviceOptions.Count() > 0)
                {
                    var a = DeviceOptions.First();
                    //send dim-down command
                    sendDim(DeviceOptions.First());
                }
            }

        }
        private void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {

            //we are only interested in video
            if (e.Item.MediaType != "Video") { return; }

            _logger.Debug("Playback Start event");
            _logger.Debug(_jsonSerializer.SerializeToString(e));

            getPauseControl(e.DeviceId).wasPaused = false;

            var DeviceOptions = Plugin.Instance.Configuration.Options.Where(i => i.embyDeviceID == e.DeviceId && i.Enabled);

            if (DeviceOptions.Count() > 0)
            {
                var a = DeviceOptions.First();
                //send dim-down command
                sendDim(DeviceOptions.First());
            }
            return;
                        
        }
        private void PlaybackStopped(object sender, PlaybackProgressEventArgs e)
        {
            //we are only interested in video
            if (e.Item.MediaType != "Video") { return; }

            _logger.Debug("Playback Stop event");
            getPauseControl(e.DeviceId).wasPaused = false;

            var DeviceOptions = Plugin.Instance.Configuration.Options.Where(i => i.embyDeviceID == e.DeviceId);

            if (DeviceOptions.Count() > 0)
            {
                var a = DeviceOptions.First();
                //send dim-up command
                sendDimUp(DeviceOptions.First());
            }
            return;
        }

        public async void sendDim(PluginConfiguration.HueControl o)
        {
            //using transitiontime sets the brightness to 1
            //opening a ticket with Phillips Hue for workaround

            var bridgOptions = Plugin.Instance.Configuration.bridge;
            string data = "{\"on\": false }";  //, \"transitiontime\": " + o.OnPlay_TransitionTime.ToString() + "}";
            _logger.Debug(data);
            string url = "http://" + bridgOptions.HueIP + "/api/" + bridgOptions.API + "/groups/" + o.LightGroupNumber + "/action" ;
            _logger.Debug(url);

            try { 
            await _httpClient.SendAsync(new HttpRequestOptions
            {
                Url = url,
                //Fix for Emby Server 3.6
                RequestContent = data.AsMemory()
                //RequestContent = data
            }, "PUT");
            }
            catch (Exception e)
            {
                _logger.Debug(e.ToString());
            }

            return;
        }

        public async void sendDimUp(PluginConfiguration.HueControl o)
        {
            var bridgOptions = Plugin.Instance.Configuration.bridge;
            string data = "{\"on\": true, \"transitiontime\": " + o.OnStop_TransitionTime.ToString() + "}";
            _logger.Debug(data);
            string url = "http://" + bridgOptions.HueIP + "/api/" + bridgOptions.API + "/groups/" + o.LightGroupNumber + "/action";
            _logger.Debug(url);


            try
            {
                await _httpClient.SendAsync(new HttpRequestOptions
                {
                    Url = url,
                    //Fix for Emby Server 3.6
                    RequestContent = data.AsMemory()
                    //RequestContent = data
                }, "PUT");
            }catch (Exception e)
            {
                _logger.Debug(e.ToString());
            }
            return;
        }
    }
}