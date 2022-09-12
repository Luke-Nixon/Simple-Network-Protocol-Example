using Network_Protocol_Library;
using System;

namespace Test_Server
{

    /// <summary>
    /// Class used to test the functionality of the network protocol library. 
    /// Creates a Network Protocol Library.Server instance.
    /// </summary>
    class Test_Server
    {
        static void Main(string[] args)
        {

            Network_Settings.still_alive_frequency = 0.1f;

            Server meme = new Server(false);

            Console.WriteLine("press any key followed by enter, to close the server.");

            Console.ReadLine();
        }
    }
}
