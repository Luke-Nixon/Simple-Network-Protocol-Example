using System;
using System.Collections.Generic;
using System.Text;
using static Network_Protocol_Library.Network_Settings;

namespace Network_Protocol_Library.Supported_Network_types
{
    public class Connection_Request
    {
        public Supported_Type supported_Type = Supported_Type.Connection_request;
        public string display_name;
    }
}
