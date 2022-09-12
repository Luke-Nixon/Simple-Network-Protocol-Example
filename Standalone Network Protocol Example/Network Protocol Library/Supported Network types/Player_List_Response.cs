using System.Collections.Generic;
using static Network_Protocol_Library.Network_Settings;

namespace Network_Protocol_Library
{
    public class Player_List_Response
    {
        public Supported_Type supported_Type = Supported_Type.player_list_response;
        public List<Player> player_list = new List<Player>();
    }
}