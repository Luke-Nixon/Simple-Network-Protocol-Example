using Network_Protocol_Library;
using Network_Protocol_Library.Supported_Network_types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Test_Serializer
{
    public class Test_Serializer
    {
        static void Main(string[] args)
        {
           tests tests = new tests();

            Console.WriteLine("Welcome to the test program.");
            Console.WriteLine("Enter one of the following commands to run a test..");
            Console.WriteLine(" '1' - test avg serialiser speed ");
            Console.WriteLine(" '2' - test avg protocol end to end delay ");
            Console.WriteLine(" '3' - test protocol jitter");
            Console.WriteLine(" '4' - test packet loss");
            // packet loss test
            // serialisation speeds for other protocols
            string cmd = Console.ReadLine();

            // run serialiser speed test
            if (cmd.Equals("1"))
            {
                tests.Serialiser_speed_test();
            }
            else if(cmd.Equals("2"))
            {
                tests.protocol_end_to_end_delay();
            }
            else if (cmd.Equals("3"))
            {
                tests.end_to_end_jitter_test();
            }
            else if (cmd.Equals("4"))
            {
                tests.packet_loss_test();
            }

        }



        
    }

}

