using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Test_Serializer
{
    [ProtoContract]
    class protobuff_serialiser
    {
        [ProtoMember(1)]
        public string display_name;
    }
}
