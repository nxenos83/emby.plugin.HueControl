using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;
using Emby.HueControl.API;
using Emby.HueControl;
//using System.Net.Http;

namespace Emby.WebhHueControl.API
{

    [Route("/FindBridges", "GET", Summary = "Add Bridge")]
    public class FindBridges : IReturn
    {

    }

    [Route("/AddBridge", "GET", Summary = "Add Bridge")]
    public class AddBridge : IReturn
    {
        [ApiMember(Name = "BridgeIP", Description = "Bridge IP Address", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string BridgeIP { get; set; }
    }

    [Route("/TestBridgeConnection", "GET", Summary = "Add Bridge")]
    public class TestBridge : IReturn
    {
        [ApiMember(Name = "BridgeIP", Description = "Bridge IP Address", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string BridgeIP { get; set; }
    }

    [Route("/GetLightGroups", "GET")]
    public class GetLightGroups : IReturn
    {
        [ApiMember(Name = "BridgeIP", Description = "Bridge IP Address", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string BridgeIP { get; set; }
    }

    [Route("/GetLights", "GET")]
    public class GetLights : IReturn
    {
         //[ApiMember(Name = "BirdgeIP", Description = "Bridge IP Address", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        // public string BirdgeIP { get; set; }
    }

    [Route("/CreateLightGroup", "GET", Summary = "Add Bridge")]
    public class CreateLightGroup : IReturn
    {
        // [ApiMember(Name = "BirdgeIP", Description = "Bridge IP Address", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        // public string BirdgeIP { get; set; }
    }

    class ServerApiEndpoints : IService

    {

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public ServerApiEndpoints(ILogManager logManager, IHttpClient httpClient, IJsonSerializer jsonSerializer)

        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;



        }

        public string Get(FindBridges request)
        {
            var response =_httpClient.Get(new HttpRequestOptions(){
                LogErrors = true,
                Url = "http://meethue.com/api/nupnp"
            });
            //var response = _httpClient.Get("http://www.meethue.com/api/nupnp", new System.Threading.CancellationToken());
            response.Wait();

            StreamReader reader = new StreamReader(response.Result);
            string r = reader.ReadToEnd();
            return r;
        }

        public string Get(AddBridge request)
        {
            
            var response = _httpClient.Post(new HttpRequestOptions()
            {
                Url = "http://" + request.BridgeIP + "/api",
                //For Emby Server 3.5
                //RequestContent = "{\"devicetype\":\"emby_server#emby\"}",
                //Fix for Emby Server version 3.6
                RequestContent = "{\"devicetype\":\"embey_server#emby\"}".AsMemory(),
                RequestContentType= "text/json"
            });
            response.Wait();

            StreamReader reader = new StreamReader(response.Result.Content);
            string r = reader.ReadToEnd();
            r= r.Trim(new Char[] { ' ', '[', ']' });
            HueResponse a = _jsonSerializer.DeserializeFromString<HueResponse>(r);
          
            
            if (a.success != null)
            {
                var config = Plugin.Instance.Configuration;
                config.bridge.HueIP = request.BridgeIP;
                config.bridge.API = a.success.username;
                Plugin.Instance.SaveConfiguration();
                Plugin.Instance.UpdateConfiguration(config);
                Plugin.Instance.SaveConfiguration();
                
                return "success";
                
            }
            else if (a.error != null)
            {
                return a.error.description;
            }
            else
            {
                return "unknown error";
            }
            
        }

        public string Get(TestBridge request)
        {
            

            _logger.Debug("Hue Connection Test for IP:" +  request.BridgeIP);
            _logger.Debug("Using API:" + Plugin.Instance.Configuration.bridge.API);


            //if (request.BirdgeIP != null && Plugin.Instance.Configuration.bridge.API != null)
            //{
            return TestHue(request.BridgeIP, Plugin.Instance.Configuration.bridge.API);
            //}

            //return "false";
        }

        public string TestHue(string IP, string API)
        {
            //var r = _httpClient.Get("http://" + IP + "/api/" + API, new System.Threading.CancellationToken());
            var r = _httpClient.Get(new HttpRequestOptions(){
                LogErrors = true,
                Url = "http://" + IP + "/api/" + API
            });
            r.Wait();
            HueResponse a = _jsonSerializer.DeserializeFromStream<HueResponse>(r.Result);

            if (a.error != null)
            {
                return a.error.description;
            }
            else
            {
                return "success";
            }
        }

        public string Get(GetLightGroups request)
        {
            //Save IP to Config
            var config = Plugin.Instance.Configuration;
            config.bridge.HueIP = request.BridgeIP;
            Plugin.Instance.UpdateConfiguration(config);
            Plugin.Instance.SaveConfiguration();

            var IP = request.BridgeIP;
            var API = Plugin.Instance.Configuration.bridge.API;
            string URL = "http://" + IP + "/api/" + API + "/groups";

            _logger.Debug("Getting light groups from " + URL);

            //var response = _httpClient.Get("http://" + IP + "/api/" + API + "/groups", new System.Threading.CancellationToken());
            var response = _httpClient.Get(new HttpRequestOptions(){
                LogErrorResponseBody = true,
                Url = "http://" + IP + "/api/" + API + "/groups"
            });
            response.Wait();
            
            StreamReader reader = new StreamReader(response.Result);
            string r = reader.ReadToEnd();
            return r;
        }
    }
 }
