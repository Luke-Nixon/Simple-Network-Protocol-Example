# Simple Network Protocol Example 

created for a proof of concept for a university course.
 
This project contains a simple custom data serialiser and network protocol that comunicates using UDP sockets. 
The protocol enables multiple clients to connect to a single server and can be identified by a name. The server then comunicates the client information and name back to each client.

The protocl library, client and server program can then be tested using the testing program to measure the quality and speed of the protocl.

The project contains four seperate subfolders that contain the source code for different applications and librarys.

- Network Protocol Library.
This folder contains all of the source code for the other programs including the testing program, the example client and the example server program.
This library is the core of the project and features a basic custom seriliser to convert simple user generated classes into data that can be sent over the network using UDP sockets.The library is designed to be expandable and facilitate new custom serialisable objects that can be transmitted over the network using the instructions in the serialiser.

- Example server.
Folder contains the code for the server application. This server listens for incoming connections on the specified port once run. Once connected to a client, the server will relay information to each client.

- Example client.
Folder contains the code for the client application. The client connects to the server at the specified IP address on the given port. Once connected the client will be given information from the server using the protocol. 

- testing program.
The testing program performs various tests on the protocol by creating an instance of a client and server. The program can then perform various tests such as jitter, ping, packet loss and serilisation speed tests.
