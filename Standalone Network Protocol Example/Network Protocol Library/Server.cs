using Network_Protocol_Library.Supported_Network_types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network_Protocol_Library
{

    public delegate void Server_Incoming_Data_Delegate(object obj, object uid);
    public delegate void Server_Send_Data_Delegate(object obj, object uid);

    /// <summary>
    /// Creates a UDP Server to be used in a game server
    /// </summary>
    public class Server
    {
        /// <summary>
        /// dictionary of all connected clients indexed by UID.
        /// </summary>
        public Dictionary<string, Connected_Client> Connected_clients { get; set; } = new Dictionary<string, Connected_Client>();

        /// <summary>
        /// Dictionary of all players. Indexed by UID
        /// 
        /// Similar to connected_clients except it does not contain any reference to a clients IP address.
        /// Used to send player information without comprimising client ip address privacy.
        /// </summary>
        public Dictionary<string, Player> players = new Dictionary<string, Player>();

        // used to get the UID of the client from the IP address.
        private Dictionary<IPEndPoint, string> Id_map { get; set; } = new Dictionary<IPEndPoint, string>();

        public const int SIO_UDP_CONNRESET = -1744830452;

        /// <summary>
        /// Event that is called when an incoming connection request is sent to the server by the client.
        /// </summary>
        public event Server_Incoming_Data_Delegate On_Incoming_Data;

        /// <summary>
        /// Event that is called when data is sent from the server.
        /// </summary>
        public event Server_Send_Data_Delegate On_Send_Data;

        private UdpClient UDP_server { get; set; }
        // stop watch used to check for connections that have timed out
        private Stopwatch stopwatch = new Stopwatch();
        // bool used to suppress the output of still alive responses to the console.
        private bool Suppress_still_alive_notifications;

        // lock object used to lock the "Send_qued_data" function.
        private object lock_object = new object();


        Timer qued_data_timer;
        Timer still_alive_timer;
        Timer check_dead_clients_timer;


        /// <summary>
        /// Constructor for the server class.
        /// Starts the UDP server on the established port.
        /// Begins listening for connections.
        /// </summary>
        public Server(bool Suppress_still_alive_notifications)
        {
            this.On_Send_Data += new Server_Send_Data_Delegate(Handle_On_Send_Data);
            // subscribe the "Handle_Incoming_Connection_Requests" method to the "On_Incoming_Data" event.
            this.On_Incoming_Data += new Server_Incoming_Data_Delegate(Handle_Incoming_Disconect_notice);
            this.On_Incoming_Data += new Server_Incoming_Data_Delegate(Handle_Incoming_Still_Alive_Responses);

            Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Starting UDP Server on Port:" + Network_Settings.Server_port);

            this.UDP_server = new UdpClient(Network_Settings.Server_port);

            // set the connection to ignore ICMP messages
            this.UDP_server.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);

            this.UDP_server.BeginReceive(new AsyncCallback(Incoming_UDP), null); // begin listening for incoming UDP data. when it arives, use the callback "Incoming_UDP".

            // start the send data timer
            TimerCallback send_data_callback = Send_qued_data;
            qued_data_timer = new Timer(send_data_callback, "Send_qued_data timer", TimeSpan.FromMilliseconds(Network_Settings.network_frequency), TimeSpan.FromMilliseconds(Network_Settings.network_frequency));

            // start the still alive timer
            TimerCallback send_still_alive_callback = Send_Still_Alive_Request;
            still_alive_timer = new Timer(send_still_alive_callback, "send_still_alive timer", TimeSpan.FromSeconds(Network_Settings.still_alive_frequency), TimeSpan.FromSeconds(Network_Settings.still_alive_frequency));

            // start the stopwatch used to measure timed out connections
            stopwatch.Start();

            // start the check dead connections timer
            TimerCallback check_dead_clients_callback = Dead_client_check;
            check_dead_clients_timer = new Timer(check_dead_clients_callback, "check for dead clients timer", TimeSpan.FromSeconds(Network_Settings.Timeout_limit / 3), TimeSpan.FromSeconds(Network_Settings.Timeout_limit / 3));

            this.Suppress_still_alive_notifications = Suppress_still_alive_notifications;
        }

        private void Handle_On_Send_Data(object obj, object uid)
        {


        }

        // sends a still alive request to each connected client on the server.
        private void Send_Still_Alive_Request(object state)
        {
            if (!this.Suppress_still_alive_notifications)
            {
                Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Sending Still Alive response to all connected clients.");
            }
            this.Enque_data(new Still_Alive_Response());
        }

        // checks for clients that have stopped sending data outside the Network_Settings.Timeout_limit
        private void Dead_client_check(object state)
        {
            if (this.Connected_clients.Count == 0)
            {
                return;
            }

            // iterate over each connected client
            foreach (Connected_Client client in this.Connected_clients.Values)
            {
                if (this.stopwatch.Elapsed.TotalSeconds > (client.last_received_data_timestamp + Network_Settings.Timeout_limit))
                {
                    Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Client: " + client.display_name + " UID: " + client.uid + " lost connection to the server");
                    Remove_client(client.uid);
                }
            }
        }

        private void Remove_client(string uid)
        {
            Connected_Client cli = Get_Client_From_UID(uid);

            this.Id_map.Remove(cli.ip);
            this.Connected_clients.Remove(uid);
            this.players.Remove(uid);

            Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Client: " + cli.uid + "Has been removed from the register.");

            // send the updated player list to all clients
            Player_List_Response player_List_response = new Player_List_Response();
            player_List_response.player_list = this.players.Values.ToList();

            Enque_data(player_List_response);
            Console.WriteLine("enqued player list");

        }


        private void Incoming_UDP(IAsyncResult res) // called by the "server.BeginReceive" function when incoming UDP data arives.
        {
            this.UDP_server.BeginReceive(new AsyncCallback(Incoming_UDP), null); // receive the next packet

            IPEndPoint client_ip = new IPEndPoint(IPAddress.Any, 1337); // the incoming clients IP and port

            byte[] received_bytes = this.UDP_server.EndReceive(res, ref client_ip); // the received data from this specific client

            List<object> received_data = new List<object>();

            try
            {
                // as we are attempting to deserialize data from clients that have the potential to send corrupted data
                // when deserializing this data from bytes to objects, it will be wrapped in a try and catch.

                (received_data, _) = new Network_Serializer().Deserialize_bytes(received_bytes);
            }
            catch (Exception e)
            {
                throw new Exception("Error when deserializing data" + e.ToString());
            }

            // if a connection request is received
            // process the connection request and exit further processing.
            if (received_data[0] is Connection_Request cr)
            {
                Handle_Incoming_Connection_Requests(cr, client_ip);

                // once the new client has been established. set the initial timestamp for the client, 
                // so that it does not get timed out before it can send a still alive response.

                string uid = this.Id_map[client_ip];
                Connected_Client temp_client = this.Get_Client_From_UID(uid);
                temp_client.last_received_data_timestamp = stopwatch.Elapsed.TotalSeconds;
                return;
            }


            // if data has been sent to the server, but the client is not recognised.
            // (potentially by a disconect notice being sent out of order.)
            // or a client being removed from the server but still trying to send data on the client
            if (!Id_map.ContainsKey(client_ip))
            {
                throw new Exception("valid data arived from a client, but the client was not registerd." + client_ip.ToString());


            }

            // update the clients last_received_data_timestamp.

            Connected_Client client = Get_Client_From_UID(Id_map[client_ip]);
            client.last_received_data_timestamp = stopwatch.Elapsed.TotalSeconds;

            // update the clients received data record.

            client.received_data_record.Add(received_bytes);
            this.confine_client_received_record(client);



            // iterate over each deserialized object
            foreach (object obj in received_data)
            {
                // trigger the On_Incoming_Data event with the object and the UID of the client as arguments.
                // NOTE: this event should be called last, in this function.
                // If for example, the "update last received data timestamp" code came after this, it will result in errors where the code can not find a client that has been removed.
                On_Incoming_Data(obj, Id_map[client_ip]);
            }
        }

        // sends qued data to each client.
        // called at the rate of Network_Settings.Network_Frequency.
        private void Send_qued_data(object state)
        {
            lock (this.lock_object)
            {

                foreach (Connected_Client client in this.Connected_clients.Values)
                {
                    // if there is no data to send for this client
                    if (client.to_send_data_que.Count == 0)
                    {
                        // go onto the next client
                        continue;
                    }

                    // create a new serializer and byte lis to start serializing objects with.
                    Network_Serializer serializer = new Network_Serializer();
                    List<byte> byte_list = new List<byte>();

                    // while the serializer has not exceeded the max payload size
                    while (byte_list.Count < Network_Settings.Max_payload_size & client.to_send_data_que.Count > 0)
                    {
                        // add objects to the serializer and serialize them from the clients data que.
                        byte_list.AddRange(serializer.serialize_Object(client.to_send_data_que[0]));

                        // now that the object has been removed and is ready to be sent add it to the  "On_Send_Data event".
                        this.On_Send_Data(client.to_send_data_que[0], client.uid);

                        // remove the object from the clients data que.
                        client.to_send_data_que.RemoveAt(0);
                    }

                    // send the data to the client.
                    UDP_server.Send(byte_list.ToArray(), byte_list.Count, client.ip);
                    // add the sent data to the clients "sent_data_list".
                    client.sent_data_record.Add(byte_list.ToArray());
                    this.confine_client_sent_record(client);

                    

                }
            }
        }

        // confines the clients received record from exceeding the allocated maximum defined by Network_Settings.max_received_data_record_length
        private void confine_client_received_record(Connected_Client client)
        {
            if (client.received_data_record.Count > Network_Settings.max_received_data_record_length)
            {
                client.received_data_record.RemoveAt(Network_Settings.max_received_data_record_length);
            }
        }
        // confines the clients sent record from exceeding the allocated maximum defined by Network_Settings.max_sent_data_record_length
        private void confine_client_sent_record(Connected_Client client)
        {
            if (client.sent_data_record.Count > Network_Settings.max_sent_data_record_length)
            {
                client.sent_data_record.RemoveAt(Network_Settings.max_sent_data_record_length);
            }
        }

        // enque data (List of data) to specific client via UID
        public void Enque_data(List<object> data, string uid)
        {
            Connected_Client client = Get_Client_From_UID(uid);

            foreach (object obj in data)
            {
                client.to_send_data_que.Add(obj);
            }
        }

        // enque data to specific client via UID
        public void Enque_data(object data, string uid)
        {
            Connected_Client client = Get_Client_From_UID(uid);

            client.to_send_data_que.Add(data);
        }

        // enque data to specific client
        public void Enque_data(object data, Connected_Client client)
        {
            client.to_send_data_que.Add(data);
        }

        // enque data to all clients
        public void Enque_data(object data)
        {
            // do not attempt to que data if there are no clients
            if (this.Connected_clients.Count == 0)
            {
                return;
            }

            // iterate over the connected clients dictionary
            foreach (Connected_Client client in this.Connected_clients.Values)
            {
                client.to_send_data_que.Add(data);
            }
        }

        private Connected_Client Get_Client_From_UID(string uid)
        {
            if (Connected_clients.TryGetValue(uid, out var connected_client))
            {
                return connected_client;
            }
            else
            {
                throw new Exception("no client exsists with the given UID:" + uid.ToString());
            }
        }

        // called when new data arives
        // checks if the data is from an unrecognised client, and add its to the connected_clients dictionary.
        private readonly object balanceLock = new object();
        private void Handle_Incoming_Connection_Requests(Connection_Request cr, IPEndPoint ip)
        {
            // 
            lock(balanceLock) {

                // check if the client is already registerd
                if (Id_map.ContainsKey(ip))
                {
                    Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "A client is attempting to send a connection request that is already registerd." + ip.ToString() + " UID:" + Id_map[ip]);
                    return;
                }

                // create a new client and add it to the list of known clients
                Connected_Client client = new Connected_Client(ip, cr.display_name);


                Connected_clients.Add(client.uid, client);
                Id_map.Add(ip, client.uid);

                // add a new entry into the player list
                Player player = new Player();
                player.display_name = client.display_name;
                player.UID = client.uid;
                players.Add(player.UID, player);

                Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "New client '" + client.display_name + "' joined. UID:'" + client.uid + "'");

                // send back the meta data to this client.

                Meta_Data_Response meta = new Meta_Data_Response();

                // populate the Meta_Data_Response with the network settings.
                meta.Max_payload_size = Network_Settings.Max_payload_size;
                meta.max_received_data_record_length = Network_Settings.max_received_data_record_length;
                meta.max_sent_data_record_length = Network_Settings.max_sent_data_record_length;
                meta.still_alive_frequency = Network_Settings.still_alive_frequency;
                meta.Server_network_frequency = Network_Settings.network_frequency;
                meta.Timeout_limit = Network_Settings.Timeout_limit;
                meta.UID = client.uid;

                // send the meta data to the new client
                Enque_data(meta, client);

                // send the updated player list to all clients
                Player_List_Response player_List_response = new Player_List_Response();
                player_List_response.player_list = this.players.Values.ToList();
                Enque_data(player_List_response);
            }
        }

        private void Handle_Incoming_Still_Alive_Responses(object obj, object uid)
        {
            if (obj is Still_Alive_Response & !this.Suppress_still_alive_notifications)
            {
                Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "New still alive response from client:" + uid.ToString());
            }
        }

        private void Handle_Incoming_Disconect_notice(object obj, object uid)
        {
            if (obj is Disconect_Notice)
            {
                string auid = (string)uid;

                if (Connected_clients.ContainsKey(auid))
                {
                    this.Remove_client(auid);

                    Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Client: '" + auid.ToString() + "' disconected from the server.");
                }
                else
                {
                    Console.WriteLine("SERVER: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "WARNING: a disconect notice arrived for a client that is not known. " + auid.ToString());
                }

            }
        }


    }
}
