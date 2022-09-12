using static Network_Protocol_Library.Network_Settings;

namespace Network_Protocol_Library
{
    public class Meta_Data_Response
    {
        public Supported_Type supported_Type = Supported_Type.meta_data_response;

        // the time in milisenconds data will be sent from the server to the client. In mi
        public float Server_network_frequency;
        // time limit that must elapse before a connection is considered disconnected. In seconds.
        public float Timeout_limit;
        // the maximum size of a single UDP Packet Payload will be. In Bytes.
        public float Max_payload_size;
        // the maximum number of entrys in the clients received data list.
        public int max_sent_data_record_length;
        // the maximum number of entrys in the clients sent data list.
        public int max_received_data_record_length;
        // the send rate of the stil alive signal.
        public float still_alive_frequency;
        // the asigned UID of the client.
        public string UID;
    }
}