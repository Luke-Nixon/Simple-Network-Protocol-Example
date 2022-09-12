using Network_Protocol_Library.Supported_Network_types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Network_Protocol_Library
{
    /// <summary>
    /// Provides a means of storing  and acsessing configuration settings for the Network Protocol library.
    /// </summary>
    public class Network_Settings
    {

        // server and client side settings

        /// <summary>
        /// enum representation of all supported types in "Network_Protocol_Library.Supported_Network_types"
        /// used when deserializing to lookup the supported_type in the supported_type_map. 
        /// 
        /// All serializable classes require a public supported_Type field.
        /// </summary>
        public enum Supported_Type
        {
            Connection_request,
            still_alive_response,
            disconect_notice,
            meta_data_response,
            player,
            player_list_response,  
        }

        /// <summary>
        /// Map of all supported types in "Network_Protocol_Library.Supported_Network_types"
        /// used when deserializing to lookup the supported_type. 
        /// 
        /// All serializable classes require their type to be included in this map.
        /// </summary>
        public static Dictionary<Supported_Type, Type> supported_type_map = new Dictionary<Supported_Type, Type>
        {
            { Supported_Type.Connection_request, typeof(Connection_Request) },
            { Supported_Type.still_alive_response, typeof(Still_Alive_Response)},
            { Supported_Type.disconect_notice, typeof(Disconect_Notice) },
            { Supported_Type.meta_data_response, typeof(Meta_Data_Response) },
            { Supported_Type.player, typeof(Player) },
            { Supported_Type.player_list_response, typeof(Player_List_Response) },

        };


        /// <summary>
        /// Sets the maximum number of entrys in the clients received data list.
        /// </summary>
        public static int max_received_data_record_length { get; set; } = 100;

        /// <summary>
        /// Sets the maximum number of entrys in the clients sent data list.
        /// </summary>
        public static int max_sent_data_record_length { get; set; } = 100;

        /// <summary>
        /// The rate in secconds that a still alive response will be send over the network.
        /// </summary>
        public static float still_alive_frequency { get; set; } = 2f;

        /// <summary>
        /// The time in milisenconds data will be sent over the network. Measured In miliseconds.
        /// </summary>
        public static float network_frequency { get; set; } = 10f;

        /// <summary>
        /// Time limit that must elapse before a connection is disconnected. Measured in seconds.
        /// </summary>        
        public static float Timeout_limit { get; set; } = 10f;

        /// <summary>
        /// The maximum supported size a UDP packets payload will be. Measured in bytes.
        /// Subsequent data added to UDP packet will be added to the next packet in the que.
        /// This is used to avoid split packets and staying under MTU.
        /// </summary>
        public static float Max_payload_size { get; set; } = 500;


        // server side settings

       /// <summary>
       /// The public port the server will use.
       /// Note: this port must be port forwarded and not in use by other services on the network.
       /// </summary>
        public static int Server_port { get; set; } = 1337;

        // client settings

        /// <summary>
        /// The IP address the client will atempt to connect to.
        /// </summary>
        public static string Server_IP {get ; set ;} = "127.0.0.1"; 
        
    }
}
