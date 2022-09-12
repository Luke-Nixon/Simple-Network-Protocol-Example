﻿using Network_Protocol_Library.Supported_Network_types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Network_Protocol_Library
{

    public delegate void Client_Incoming_Data_Delegate(object obj);

    /// <summary>
    /// Creates a UDP Client to be used in a game server
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Event that is called when incoming data arvies from the server.
        /// </summary>
        public event Client_Incoming_Data_Delegate On_Incoming_Data;

        /// <summary>
        /// the Unique ID asigned to this client by the server.
        /// </summary>
        public string UID { get; set; } = "";

        /// <summary>
        /// the que of data that is to be gradually sent to the server when the "Send_data_timer" is triggerd.
        /// </summary>
        public List<object> To_send_data_que { get; set; } = new List<object>();

        /// <summary>
        /// Gets the current connection status of the client.
        /// </summary>
        public bool Is_Connected { get; set; } = false;

        /// <summary>
        /// Contains a list of all connected players.
        /// Is updated by the server when a new player joins.
        /// </summary>
        public List<Player> players = new List<Player>();


        // the UDPClient used to create a UDP socket 
        private UdpClient UDP_client;
        // the Server IP and port to connect to
        private IPEndPoint server_IP = new IPEndPoint(IPAddress.Parse(Network_Settings.Server_IP), Network_Settings.Server_port);
        // lock object used to lock the "Send_qued_data" function.
        private object lock_object = new object();
        // Timer that is used to repeatedly attempt to connect to the server.
        private Timer Attempt_connection_timer;
        // Timer that runs at the Network_Settings.network_frequency to deque data from the "To_send_data_que"
        private Timer Send_data_timer;
        // Timer that runs at the Network_Settings.still_alive_frequency to notify the server that the connection is still alive.
        private Timer Send_still_alive_timer;
        // Timer that runs at the Network_Settings.Timeout_limit rate to check if the connection is unresponsive.
        private Timer time_out_check;
        // recorded timestamp of when the last data arrived from the server.
        private Stopwatch timeout_stopWatch = new Stopwatch();
        // used to suppress the console output generated by incoming/outgoing still alive responses.
        private bool Suppress_Still_alive_notifications;

        // received data record
        public List<byte[]> Received_data_record { get; set; } = new List<byte[]>();
        // sent data recrod
        public List<byte[]> Sent_data_record { get; set; } = new List<byte[]>();

        public Client(bool Suppress_Still_alive_responses)
        {
            // setup a new socket to the given server

            Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Starting UDP_Client. Connecting to:" + Network_Settings.Server_IP + " On Port:" + Network_Settings.Server_port);

            this.UDP_client = new UdpClient();

            // set the connection to ignore ICMP messages
            const int SIO_UDP_CONNRESET = -1744830452;
            UDP_client.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            UDP_client.Connect(new IPEndPoint(IPAddress.Parse(Network_Settings.Server_IP), Network_Settings.Server_port));
            UDP_client.BeginReceive(new AsyncCallback(Incoming_UDP), null);


            // subscribe the "Handle_Incoming_X" funtions to the "On_Incoming_Data" event.
            this.On_Incoming_Data += new Client_Incoming_Data_Delegate(Handle_Incoming_Meta_Data);
            this.On_Incoming_Data += new Client_Incoming_Data_Delegate(Handle_Incoming_Still_Alive_Responses);
            this.On_Incoming_Data += new Client_Incoming_Data_Delegate(Handle_Incoming_Player_List);

            // start the Send_qued_data timer.
            TimerCallback send_data_callback = Send_qued_data;
            this.Send_data_timer = new Timer(send_data_callback, "Send_qued_data timer", TimeSpan.FromMilliseconds(Network_Settings.network_frequency), TimeSpan.FromMilliseconds(Network_Settings.network_frequency));

            // start the Attempt_Connection timer
            TimerCallback Attempt_connection_callback = Attempt_connection;
            this.Attempt_connection_timer = new Timer(Attempt_connection_callback, "Attempt_connection timer", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            this.Suppress_Still_alive_notifications = Suppress_Still_alive_responses;
        }

        private void check_timeout(object state)
        {
            if (this.timeout_stopWatch.Elapsed.TotalSeconds > Network_Settings.Timeout_limit)
            {
                // inform the user that the connection appears to have timed out.
                Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "No incoming Still Alive signal for " + Network_Settings.Timeout_limit.ToString() + "\n Disconecting from server.");

                // stop the timeout timer and stopwatch from running as the connection will be closed.
                this.timeout_stopWatch.Stop();
                this.time_out_check.Dispose();

                // send a disconcect notice to the server incase the server is still running but unable to send data due to a connection issue.
                this.Send_Disconect_Notice();
                // close the connection
                this.Close_client();

            }
        }


        // creates a new connection request and Enques the request to send to the server.
        private void Attempt_connection(object state)
        {
            Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Sending Connection Request");
            // create the connection request.
            Connection_Request cr = new Connection_Request();
            cr.display_name = "test";

            Enque_data(cr);
        }

        /// <summary>
        /// Enques Data to send to the UDP server at the rate asigned by the server from Network_Settings.Network_Frequencey.
        /// </summary>
        /// <param name="obj"> The data to send to the server. Must be of type "Supported_Network_Type"</param>
        public void Enque_data(object obj)
        {
            this.To_send_data_que.Add(obj);
        }

        // consumes and serialises data in the data que and sends it to the server.
        private void Send_qued_data(object state)
        {
            // as the timer firing this event is multi threaded, this section of code needs to be locked to stop multiple threads from removing objects from the this.To_send_data_que 
            lock (lock_object)
            {
                //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                //Console.WriteLine("sending Qued Data.. Que count:" + this.To_send_data_que.Count);

                // only attempt to send data if there is data to send.
                if (this.To_send_data_que.Count > 0)
                {

                    // create a new serializer and byte lis to start serializing objects with.
                    Network_Serializer serializer = new Network_Serializer();
                    List<byte> byte_list = new List<byte>();

                    // while the serializer has not exceeded the max payload size
                    while (byte_list.Count < Network_Settings.Max_payload_size & this.To_send_data_que.Count > 0)
                    {
                        // add objects to the serializer and serialize them from the clients data que.
                        byte_list.AddRange(serializer.serialize_Object(this.To_send_data_que[0]));
                        this.To_send_data_que.RemoveAt(0);
                    }

                    // send the data to the client.
                    UDP_client.Send(byte_list.ToArray(), byte_list.Count);

                    this.Sent_data_record.Add(byte_list.ToArray());
                    this.confine_sent_record();

                }
            }
        }

        // Handles the raw UDP data and deserialises the data. Then invokes the On_Incoming_Data_event
        private void Incoming_UDP(IAsyncResult res)
        {
            try
            {
                byte[] bytes = UDP_client.EndReceive(res, ref server_IP);
                this.UDP_client.BeginReceive(new AsyncCallback(Incoming_UDP), null);

                List<object> received_objects = new List<object>();

                Network_Serializer derserializer = new Network_Serializer();

                (received_objects, _) = derserializer.Deserialize_bytes(bytes);

                foreach (object obj in received_objects)
                {
                    On_Incoming_Data(obj);
                }

                // add the received bytes to the received data record.
                this.Received_data_record.Add(bytes);
                this.confine_received_record();

                // update the clients timeout stopwatch
                this.timeout_stopWatch.Restart();
            }
            catch (ObjectDisposedException e)
            {
                // this exception is caused by closing the socket and the async end receive is called 
                // https://stackoverflow.com/questions/1921611/c-how-do-i-terminate-a-socket-before-socket-beginreceive-calls-back
            }
        }

        // process the incmoing Meta_Data response from the server when a connection is established.
        private void Handle_Incoming_Meta_Data(object obj)
        {
            if (obj is Meta_Data_Response meta)
            {
                Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Incoming meta data from server: " + obj);

                // asign the settings and UID received from the server
                Network_Settings.max_received_data_record_length = meta.max_received_data_record_length;
                Network_Settings.max_sent_data_record_length = meta.max_sent_data_record_length;
                Network_Settings.Max_payload_size = meta.Max_payload_size;
                Network_Settings.network_frequency = meta.Server_network_frequency;
                Network_Settings.Timeout_limit = meta.Timeout_limit;
                Network_Settings.still_alive_frequency = meta.still_alive_frequency;

                this.UID = meta.UID;

                // stop attempt connection requests as a connection has been astablished.
                this.Attempt_connection_timer.Dispose();
                // re-adjust the send rate based on the new network frequency.
                this.Send_data_timer.Change(TimeSpan.FromMilliseconds(Network_Settings.network_frequency), TimeSpan.FromMilliseconds(Network_Settings.network_frequency));

                //begin sending still alive signals.
                this.Send_still_alive_timer = new Timer(Send_Still_Alive_Request, "Attempt_connection timer", TimeSpan.FromSeconds(Network_Settings.still_alive_frequency), TimeSpan.FromSeconds(Network_Settings.still_alive_frequency));

                // Asert that the client is now connected to the server
                this.Is_Connected = true;

                // run the check timeout timer
                TimerCallback Check_timeout_callback = check_timeout;
                this.time_out_check = new Timer(Check_timeout_callback, "check timeout timer", TimeSpan.FromSeconds(Network_Settings.Timeout_limit), TimeSpan.FromSeconds(Network_Settings.Timeout_limit));
            }
        }

        // Process incoming still alive responses from the server.
        private void Handle_Incoming_Still_Alive_Responses(object obj)
        {
            if (obj is Still_Alive_Response & !this.Suppress_Still_alive_notifications)
            {
                Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Still Alive Response Received from the server.");
            }
        }

        // Process incoming player data from the server
        private void Handle_Incoming_Player_List(object obj)
        {
            if (obj is Player_List_Response player_List)
            {
                List<Player> updated_players = player_List.player_list;

                // if players already exsist in the list. then any new or removed players must be new or leaving players.
                if (this.players.Count != 0)
                {

                    // check for new players
                    foreach (Player player in updated_players)
                    {
                        // if the players list does not contain this player
                        //  announce a new player has joined
                        if (!this.players.Contains(player))
                        {
                            Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "New player has connected to the server.. UID: " + player.UID + " , Name: " + player.display_name);
                        }
                    }

                    // check for disconected players
                    foreach (Player player in this.players)
                    {
                        // if the incoming updated players list does not contain this player.
                        // announce a player has disconected
                        if (!updated_players.Contains(player))
                        {
                            Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Player Has disconected from the server.. UID: " + player.UID + " , Name: " + player.display_name);
                        }
                    }

                }
                else
                // if no other players exsist in the players list. it is because this client has just connected to the server.
                // therefore print to the console a different message for the user.
                {
                    Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "There are " + updated_players.Count().ToString() + " players already connected to the server (including you) \n The UID and Names are..");

                    foreach (Player player in updated_players)
                    {
                        Console.WriteLine("UID: " + player.UID + ", Name: " + player.display_name);
                    }
                }
                // set the player list with the updated players
                this.players = player_List.player_list;
            }
        }

        // Enque a new still alive response to the server.
        private void Send_Still_Alive_Request(object state)
        {

            if (!this.Suppress_Still_alive_notifications)
            {
                Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Sending Still Alive response to the server");
            }
            this.Enque_data(new Still_Alive_Response());

        }



        /// <summary>
        /// Informs the server that this client is disconecting.
        /// jumps the que and sends the disconect message instantly.
        /// </summary>
        public void Send_Disconect_Notice()
        {
            // stop further attempts to connect to the server
            if (this.Attempt_connection_timer != null)
            {
                this.Attempt_connection_timer.Dispose();
            }

            if (this.Is_Connected)
            {
                // stop the still alive signal
                this.Send_still_alive_timer.Dispose();
                // stop any further enqued data to be sent.
                this.Send_data_timer.Dispose();

                // send the Disconect_Notice to the server. immedietly without queing.
                Disconect_Notice dn = new Disconect_Notice();
                Network_Serializer serializer = new Network_Serializer();

                byte[] bytes = serializer.serialize_Object(dn).ToArray();
                this.UDP_client.Send(bytes, bytes.Length);

            }
        }

        /// <summary>
        /// Closes the UDP_Socket and ends the connection.
        /// </summary>
        public void Close_client()
        {
            if (!this.Is_Connected)
            {
                Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Client connection is already disconected");
                return;
            }

            Console.WriteLine("Client: [" + DateTime.Now.ToString("hh.mm.ss.ffffff") + "] " + "Closing Connection.");


            // close the UDP_socket.

            this.UDP_client.Client.Shutdown(SocketShutdown.Both);
            this.UDP_client.Close();
            this.UDP_client.Dispose();
            this.UDP_client = null;

            this.Is_Connected = false;
        }

        // confines the clients received record from exceeding the allocated maximum defined by Network_Settings.max_received_data_record_length
        private void confine_received_record()
        {
            if (this.Received_data_record.Count > Network_Settings.max_received_data_record_length)
            {
                this.Received_data_record.RemoveAt(Network_Settings.max_received_data_record_length);
            }
        }
        // confines the clients sent record from exceeding the allocated maximum defined by Network_Settings.max_sent_data_record_length
        private void confine_sent_record()
        {
            if (this.Sent_data_record.Count > Network_Settings.max_sent_data_record_length)
            {
                this.Sent_data_record.RemoveAt(Network_Settings.max_sent_data_record_length);
            }
        }

    }
}
