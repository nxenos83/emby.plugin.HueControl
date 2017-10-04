using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.HueControl.API
{
    class HueBridge
    {
    }

    public class findBridgeResutls
    {
        public bridge[] bridges {get;set;}
    }

    public class bridge
    {
        public string ID { get; set; }
        public string IP { get; set; }
    }


    public class HueResponse
    {
        public Success success { get; set; }
        public Error error { get; set; }
    }

    public class Error
    {
        public int type { get; set; }
        public string address { get; set; }
        public string description { get; set; }
    }

    public class Success
    {
        public string username { get; set; }
    }
}
