using System;
using System.Collections.Generic;
using System.Text;
using static Network_Protocol_Library.Network_Settings;

namespace Network_Protocol_Library.Supported_Network_types
{
    public class Still_Alive_Response
    {
        public Supported_Type supported_Type = Supported_Type.still_alive_response;
        // used for packet loss testing
        public int unique_indentifier = new Random().Next(0, 9999999);

    }
}
