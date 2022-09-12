using System.Collections.Generic;
using System.Net;

namespace Network_Protocol_Library
{
    public class Connected_Client
    {
        /// <summary>
        /// the Unique ID of the client
        /// </summary>
        public string uid { get; set; }

        /// <summary>
        /// the ip+port of the client
        /// </summary>
        public IPEndPoint ip { get; set; }

        /// <summary>
        /// the display name of the client
        /// </summary>
        public string display_name { get; set; }

        /// <summary>
        /// a list of all objects that will be sent to the client when "send_data" function.
        /// </summary>
        public List<object> to_send_data_que { get; set; } = new List<object>();

        /// <summary>
        /// a list of all data that has been sent to the client
        /// </summary>
        public List<byte[]> sent_data_record { get; set; } = new List<byte[]>();

        /// <summary>
        /// a list of all data that has been received from the client.
        /// </summary>
        public List<byte[]> received_data_record { get; set; } = new List<byte[]>();

        /// <summary>
        /// A timestamp that contains the elapsed time in miliseconds since the last data was received from the client.
        /// Measured in miliseconds.
        /// </summary>
        public double last_received_data_timestamp;

        /// <summary>
        /// Constructor for the Connected_Client class.
        /// </summary>
        /// <param name="client_ip"> the IP address + port of the client</param>
        /// <param name="display_name"> the name of the client</param>
        public Connected_Client(IPEndPoint client_ip, string display_name)
        {
            this.ip = client_ip;
            this.uid = (client_ip.GetHashCode() + new System.Random().Next(1, 1000)).ToString();
            this.display_name = display_name;
        }
    }
}