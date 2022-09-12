using Network_Protocol_Library;
using System;
using System.Threading;

namespace Test_Client
{
    /// <summary>
    /// Class used to test the functionality of the network protocol library. 
    /// Creates a Network Protocol Library.Client instance.
    /// </summary>
    class Test_Client
    {
        static void Main(string[] args)
        {
            Network_Settings.still_alive_frequency = 0.1f;

            // ask the user to specify an ip address for the client.
            Console.WriteLine("Enter the IP Address of the Server to connect to. Or press enter to connect to a server hosted on the local machine.");
            
            string ip = Console.ReadLine();
            
            
            // check if the user specified an IP address
            if (!ip.Equals(""))
            {
                Network_Settings.Server_IP = ip;
            }

            
            Client cli = new Client(false);


            Console.WriteLine("press any key followed by enter, to close the client.");

            Console.ReadLine();
            
            cli.Send_Disconect_Notice();
            // wait for the clients data que to be fully consumed and the disconect notice sent.
            //Thread.Sleep(1000);
            cli.Close_client();

        }
    }
}
