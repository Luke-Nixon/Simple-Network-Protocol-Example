using System;
using System.Collections.Generic;
using System.Text;

namespace Test_Serializer
{
    // this class simulates the network protocols connection request class. but can be serialised with the default c# serialiser
    // used in the serialisation test.

    [Serializable]
    class default_serialiser_test
    {
        public string display_name;
    }
}
