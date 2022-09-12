using Network_Protocol_Library;
using Network_Protocol_Library.Supported_Network_types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ProtoBuf;


namespace Test_Serializer
{
    class tests
    {

        public void Serialiser_speed_test()
        {

            // serialiser

            // Test 1

            int num_of_tests = 10000;
            int test_1_size;
            // results array
            double[] times_1 = new double[num_of_tests];

            // TEST 1A
            // serialisation size
            {
                Network_Serializer ser = new Network_Serializer();
                Connection_Request con = new Connection_Request();
                con.display_name = "test";
                List<byte> bytes = new List<byte>(ser.serialize_Object(con));

                test_1_size = bytes.Count();
            }


            // Test 1B
            {

                // repeat the serialisation test 10,000 times
                for (int i = 1; i < num_of_tests; i++)
                {

                    // Developed protocol test.
                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start(); // start the stopwatch

                    // create a connection request and serialise it
                    Network_Serializer ser1 = new Network_Serializer();
                    Connection_Request con1 = new Connection_Request();
                    con1.display_name = "test";
                    List<byte> bytes1 = new List<byte>(ser1.serialize_Object(con1));

                    // deserialise it
                    ser1.Deserialize_bytes(bytes1.ToArray());

                    // stop the stopwatch
                    stopwatch.Stop();

                    // print the time
                    Console.WriteLine("Elapsed serialisation time: " + stopwatch.Elapsed.TotalMilliseconds + "ms");
                    times_1[i] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }


            // c# serialiser

            // Test 2

            // results array
            double[] times_2 = new double[num_of_tests];
            long test_2_size;
            // TEST 2A
            // c# serialisation size
            {
                default_serialiser_test connection_request = new default_serialiser_test();
                connection_request.display_name = "test";
                MemoryStream stream = new MemoryStream();
                stream.Seek(0, SeekOrigin.Begin);
                new BinaryFormatter().Serialize(stream, connection_request);

                test_2_size = stream.Length;
            }
            // TEST 2B
            // c# in built serialiser
            {




                // serialisation speed

                for (int i = 1; i < num_of_tests; i++)
                {
                    // Developed protocol test.
                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start(); // start the stopwatch

                    // create a connection request and serialise it
                    default_serialiser_test connection_request = new default_serialiser_test();
                    connection_request.display_name = "test";

                    // serialise
                    MemoryStream stream = new MemoryStream();
                    stream.Seek(0, SeekOrigin.Begin);
                    new BinaryFormatter().Serialize(stream, connection_request);
                    // deserialise
                    stream.Seek(0, SeekOrigin.Begin);
                    new BinaryFormatter().Deserialize(stream);

                    // stop the stopwatch
                    stopwatch.Stop();

                    // print the time
                    Console.WriteLine("Elapsed serialisation time: " + stopwatch.Elapsed.TotalMilliseconds + "ms");
                    times_2[i] = stopwatch.Elapsed.TotalMilliseconds;

                }

            }


            // JSON


            // Test 3

            // results array
            double[] times_3 = new double[num_of_tests];
            long test_3_size;
            // TEST 3A
            // JSON serialisation size
            {
                JSON_serialiser_test json_serialiser_test = new JSON_serialiser_test();
                json_serialiser_test.display_name = "test";

                string jsonString = JsonConvert.SerializeObject(json_serialiser_test, Formatting.Indented);

                byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

                test_3_size = bytes.Length;


            }
            // TEST 3B
            // JSON in built serialiser
            {
                for (int i = 1; i < num_of_tests; i++)
                {
                    // Developed protocol test.
                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start(); // start the stopwatch

                    JSON_serialiser_test json_serialiser_test = new JSON_serialiser_test();
                    json_serialiser_test.display_name = "test";

                    // serialise
                    string jsonString = JsonConvert.SerializeObject(json_serialiser_test, Formatting.Indented);

                    byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

                    // deserialise
                    jsonString = Encoding.ASCII.GetString(bytes);
                    JsonConvert.DeserializeObject<JSON_serialiser_test>(jsonString);

                    stopwatch.Stop();

                    Console.WriteLine("Elapsed serialisation time: " + stopwatch.Elapsed.TotalMilliseconds + "ms");
                    times_3[i] = stopwatch.Elapsed.TotalMilliseconds;

                }
            }


            // Protobuff

            // Test 4

            // results array
            double[] times_4 = new double[num_of_tests];
            long test_4_size;
            // TEST 4A
            // Protobuff serialisation size
            {
                protobuff_serialiser protobuff_serialiser_test = new protobuff_serialiser();
                protobuff_serialiser_test.display_name = "test";

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Serializer.Serialize(memoryStream, protobuff_serialiser_test);
                    byte[] bytes = memoryStream.ToArray();
                    test_4_size = bytes.Length;
                }
            }
            // TEST 4B
            // protobuff
            {
                for (int i = 1; i < num_of_tests; i++)
                {
                    // protobuff protocol test.
                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start(); // start the stopwatch

                    // serialise

                    protobuff_serialiser protobuff_serialiser_test = new protobuff_serialiser();
                    protobuff_serialiser_test.display_name = "test";
                    MemoryStream memoryStream = new MemoryStream();
                    Serializer.Serialize(memoryStream, protobuff_serialiser_test);
                    byte[] temp = memoryStream.ToArray();

                    // deserialise

                    memoryStream = new MemoryStream(temp);
                    protobuff_serialiser protobuff_serialiser_test_deserialised = Serializer.Deserialize<protobuff_serialiser>(memoryStream);

                    stopwatch.Stop();

                    Console.WriteLine("Elapsed serialisation time: " + stopwatch.Elapsed.TotalMilliseconds + "ms");
                    times_4[i] = stopwatch.Elapsed.TotalMilliseconds;

                }
            }

            // print results


            Console.WriteLine("\n Test Complete. \n" +
                              "The total average time for serialising the connection_request class " + num_of_tests + " times is.." +
                              "\n" + Queryable.Average(times_1.AsQueryable()).ToString() + "ms"
                             );

            Console.WriteLine("\n The total average time for serialising the connection_request class using the c# serialiser " + num_of_tests + " times is.." +
                              "\n" + Queryable.Average(times_2.AsQueryable()).ToString() + "ms"
                             );

            Console.WriteLine("\n The total average time for serialising the connection_request class using the JSON serialiser " + num_of_tests + " times is.." +
                              "\n" + Queryable.Average(times_3.AsQueryable()).ToString() + "ms"
                              );

            Console.WriteLine("\n The total average time for serialising the connection_request class using the Protobuff serialiser " + num_of_tests + " times is.." +
                              "\n" + Queryable.Average(times_4.AsQueryable()).ToString() + "ms"
    );


            Console.WriteLine("\n Serilisation size for serialiser = " + test_1_size.ToString());
            Console.WriteLine(" Serilisation size for c# serialiser = " + test_2_size.ToString());
            Console.WriteLine(" Serilisation size for JSON serialiser = " + test_3_size.ToString());
            Console.WriteLine(" Serilisation size for Protobuff serialiser = " + test_4_size.ToString());
        }

        public void packet_loss_test()
        {
            Network_Settings.still_alive_frequency = 0.1f;
            int max_objects = 300;


            // start a new server
            Server ser = new Server(false);

            // start a new client
            Client cli = new Client(false);


            ser.On_Send_Data += packet_loss_test_server_recrod;
            cli.On_Incoming_Data += packet_loss_test_client_record;


            Console.WriteLine("This test will measure packet loss between the server and client \n" +
                              "The test will take aproximatley:" + (Network_Settings.still_alive_frequency * max_objects).ToString() + " secconds"
                              );


            // wait for the test to complete
            while (server_data_record.Count() != max_objects)
            {
                // do nothing while the test completes. (server / client will continue to run as it is running in another thread.
            }

            // once the test has completed, go through the client data record and find an instance of the unique identifier inside each server record.
            // every time a unique identifier is not found in a list, this means packet loss has occoured.

            Console.WriteLine("\n Test complete. Searching for missing packets \n");

            // unsubscribe from the event.
            ser.On_Send_Data -= packet_loss_test_server_recrod;
            cli.On_Incoming_Data -= packet_loss_test_client_record;

            int found_packets = 0;

            cli = null;
            ser = null;

            List<int> client_data_record2 = new List<int>(client_data_record);
            List<int> server_data_record2 = new List<int>(server_data_record);


            foreach (int id in client_data_record2)
            {

                foreach (int id2 in server_data_record2)
                {

                    if (id.Equals(id2))
                    {
                        found_packets += 1;
                        Console.WriteLine("found packet: " + found_packets.ToString() + "of " + max_objects.ToString());
                    }

                }


            }

            Console.WriteLine("Test Complete!  Total packet loss was " + (max_objects - found_packets).ToString() + " of " + max_objects.ToString());
        }



        List<int> client_data_record = new List<int>();
        private void packet_loss_test_client_record(object obj)
        {

            if (obj is Still_Alive_Response sar)
            {
                client_data_record.Add(sar.unique_indentifier);
                Console.WriteLine(sar.unique_indentifier);
            }

        }

        List<int> server_data_record = new List<int>();
        private void packet_loss_test_server_recrod(object obj, object uid)
        {
            if (obj is Still_Alive_Response sar)
            {
                server_data_record.Add(sar.unique_indentifier);
                Console.WriteLine(sar.unique_indentifier);
            }
        }


        Stopwatch e2e_stopwatch = new Stopwatch();
        List<double> end_to_end_times = new List<double>();
        List<double> end_to_end_jitter = new List<double>();
        public void protocol_end_to_end_delay()
        {
            int num_of_tests = 100;
            Network_Settings.still_alive_frequency = 1f;

            // start a new server
            Server ser = new Server(false);

            // start a new client
            Client cli = new Client(false);


            ser.On_Send_Data += end_to_end_server_time_stamp;
            cli.On_Incoming_Data += end_to_end_client_time_stamp;


            Console.WriteLine("\n This test will measure the end to end delay between the server and client \n" +
                              "The test will take aproximatley:" + (Network_Settings.still_alive_frequency * num_of_tests).ToString() + " secconds"
                              );


            // wait for the test to complete
            while (end_to_end_times.Count != num_of_tests)
            {
                // do nothing while the test completes. (server / client will continue to run as it is running in another thread.
            }
            // calculate the highest, lowest and average end to end delay.
            Console.WriteLine("END OF TEST!");
            Console.WriteLine("Highest end to end delay was: " + this.end_to_end_times.Max().ToString() + "ms");
            Console.WriteLine("Lowest end to end delay was: " + this.end_to_end_times.Min().ToString() + "ms");
            Console.WriteLine("Average end to end delay was: " + this.end_to_end_times.Average().ToString() + "ms");



        }



        private void end_to_end_server_time_stamp(object obj, object uid)
        {
            // when an object is sent from the server, reset the stopwatch and start it.
            e2e_stopwatch = new Stopwatch();
            e2e_stopwatch.Start();
        }

        private void end_to_end_client_time_stamp(object obj)
        {
            // when the object arives on the client, stop the stopwatch and measure the time.
            e2e_stopwatch.Stop();
            end_to_end_times.Add(e2e_stopwatch.Elapsed.TotalMilliseconds);

            // add the difference in latency from the previous result to the jitter list.
            end_to_end_jitter.Add(Math.Abs(e2e_stopwatch.Elapsed.TotalMilliseconds - end_to_end_jitter.LastOrDefault()));
        }



        public void end_to_end_jitter_test()
        {
            int max_packets = 100;

            // increase the frequency of the packets to be sent every 0.1 seconds.
            Network_Settings.still_alive_frequency = 1f;

            // start a new server
            Server ser = new Server(false);
            GC.KeepAlive(ser);
            // start a new client
            Client cli = new Client(true);
            GC.KeepAlive(cli);

            ser.On_Send_Data += end_to_end_server_time_stamp;
            cli.On_Incoming_Data += end_to_end_client_time_stamp;


            Console.WriteLine("\n This test will measure the end to end delay between the server and client \n" +
                              "The test will take aproximatley:" + (Network_Settings.still_alive_frequency * max_packets).ToString() + " secconds"
                              );


            // wait for the test to complete
            while (end_to_end_times.Count != max_packets)
            {
                // do nothing while the test completes. (server / client will continue to run as it is running in another thread.
            }
            // calculate the highest, lowest and average end to end delay.
            Console.WriteLine("END OF TEST!");
            Console.WriteLine("Maximum protocol jitter was: " + this.end_to_end_times.Max().ToString() + "ms");

            Console.WriteLine("Minimum protocol jitter was: " + this.end_to_end_jitter.Min().ToString() + "ms");

            Console.WriteLine("Average end to end delay was: " + this.end_to_end_times.Average().ToString() + "ms");




            cli.Send_Disconect_Notice();
            cli.Close_client();
            cli = null;
        }

    }
}
