using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static Network_Protocol_Library.Network_Settings;

namespace Network_Protocol_Library
{
    /// <summary>
    /// Serializes and de-serializes supported network types to bytes and from bytes.
    /// 
    /// Supported types can be found in "\Network Protocol Library\Supported Network types".
    /// Each type must also have been added to "\Network Protocol Library\Network_Settings.cs"
    /// 
    /// </summary>
    public class Network_Serializer
    {


        // Converts objects sent to this function to bytes. Bytes are then returned as a list. 
        public List<byte> serialize_Object(object obj) 
        {
            Type obj_type = obj.GetType(); // the type of the current object
            FieldInfo supported_Type_field = obj_type.GetField("supported_Type"); // get the FieldInfo of the serializable_type. so that it can be put in position zero in the list

            List<FieldInfo> fields = new List<FieldInfo>(obj_type.GetFields()); // get all fields of this object and store them in a list.
            fields.Remove(supported_Type_field); // remove the supported_Type enum from the list, so that it can be put into position zero after sorting.

            // order the list of fields by its "Name"
            fields.OrderBy(x => ((dynamic)x).Name);
            // insert the supported_Type_field into position zero.
            fields.Insert(0, supported_Type_field);

            // begin serialization now that the order of the data has been set.
            
            // the byte_list to be returned, made from a serialized supported_type object.
            List<byte> byte_list = new List<byte>();

            // iterate over each field
            foreach (FieldInfo field in fields)
            {
                dynamic value = field.GetValue(obj); // get the value stored in that field 

                // use the apropriete serialize function for the type of data.
                // add its contents to the byte_list
                switch (value)
                {
                    case Supported_Type t:
                        byte_list.AddRange(this.Serialize_supported_type(t));
                        break;
                    case int i:
                        byte_list.AddRange(this.Serialize_int(i));
                        break;
                    case string s:
                        byte_list.AddRange(this.Serialize_string(s));
                        break;
                    case float f:
                        byte_list.AddRange(this.serialize_float(f));
                        break;
                    case IList l: // takes lists of Network_Protocol_Library.Supported_Network_types (e.g List<connection_Request> not List<string>)

                        List<object> cast_list = new List<object>(value);

                        byte_list.AddRange(this.serialize_list(cast_list));
                        break;

                    default:
                        throw new Exception("ERROR: unsupported data type when serializing: " + field.Name + "in Class" + obj_type.ToString());
                }
            }

            // after serializing the object, return the byte list.
            return byte_list;
        }

        // Converts a list of objects to bytes. Only objects of a supported network type are supported.
        private List<byte> serialize_list(List<object> l)
        {
            List<byte> serialized_list = new List<byte>();

            // in a serialized list, the size of the list is serialized first.
            serialized_list.AddRange(Serialize_int(l.Count));

            // iterate over the list, and serialize each object
            foreach(object obj in l)
            {
                serialized_list.AddRange(serialize_Object(obj));
            }

            return serialized_list;
        }

        // Serializes the given float to a byte list.
        private List<byte> serialize_float(float f)
        {
            return new List<byte>(BitConverter.GetBytes(f));
        }

        // Serializes the given int to a byte list.
        private List<byte> Serialize_int(int i)
        {
            return new List<byte>(BitConverter.GetBytes(i));
        }

        // Serializes the supported_type enum to a byte list.
        private List<byte> Serialize_supported_type(Supported_Type i)
        {
            return new List<byte>(BitConverter.GetBytes((int)i));
        }

        // Serializes a string of any length to a byte list. 
        // Inserts a null byte to the signify the end of the string.
        private List<byte> Serialize_string(string s)
        {
            List<byte> byte_list = new List<byte>(Encoding.ASCII.GetBytes(s));
            byte_list.Add(new byte()); // add the null byte onto the end of the string, so the de-serialization process knows where a string ends

            return byte_list;
        }



        /// <summary>
        /// Converts a serialized byte array to a list of objects. 
        /// </summary>
        /// <param name="bytes"> the bytes to be deserialized. </param>
        /// <param name="count"> optional paramater. Used to specify a maximum number of objects to deserialize. Used when deseirlizing a list.</param>
        /// <param name="index"> Optional paramater. Used to specify the starting index of the deserilizer. Used when deserializing lists.</param>
        /// <returns>Returns a tuple, containing a list of objects deserialized and an int representing the index where deserialization ended.</returns>
        public (List<object>, int) Deserialize_bytes(byte[] bytes, int count = 0, int index = 0)
        {
            List<object> objects = new List<object>();
            
            int deserialize_index = index; // if no argument is past to index (i.e when a list is being desierialized) the deselization will start at the beginning of the byte array.

            

            while (deserialize_index < bytes.Length)
            {
                // if the optinonal paramater count is used, then only deserialize if the function has not met the count threshold. (used for deserializing lists)
                if (count > 0 & objects.Count.Equals(count))
                {
                    return (objects, deserialize_index);
                }


                // deserialize the supported type (always the first item to deserialize of a class in .supported network types
                Supported_Type supported_type;
                (supported_type, deserialize_index) = Deserialize_supported_type(bytes, deserialize_index);


                // now that the supported type has been calculated. create an instance of this type, and start populating its class atributes.

                Type type = Network_Settings.supported_type_map[supported_type];
                // create an instance of the class type
                dynamic instance = Activator.CreateInstance(type);

                
                FieldInfo supported_Type_field = type.GetField("supported_Type"); // get the FieldInfo of the serializable_type. so that it can be put in position zero in the list

                List<FieldInfo> fields = new List<FieldInfo>(type.GetFields()); // get all fields of this object and store them in a list.
                fields.Remove(supported_Type_field); // remove the supported_Type enum from the list, so that it can be put into position zero after sorting.

                // order the list of fields by its "Name"
                fields.OrderBy(x => ((dynamic)x).Name);
                // insert the supported_Type_field into position zero.
                fields.Insert(0, supported_Type_field);

                // begin de-serialization now that the order of the data has been set.

                // iterate over each field
                foreach (FieldInfo field in fields)
                {
                    Type field_type = field.FieldType;

                    switch (field_type) 
                    {
                        case Type deserialize_type when deserialize_type == typeof(Supported_Type): // Supported_Type
                            // dont need to deserialize the supported type as it is already known.

                            break;
                        
                        case Type deserialize_type when deserialize_type == typeof(int):

                            int i;

                            (i, deserialize_index) = Deserialize_int(bytes, deserialize_index); // int

                            field.SetValue(instance, i);

                            break;
                        
                        case Type deserialize_type when deserialize_type == typeof(string): // string

                            string s;
                            
                            (s,deserialize_index) = Deserialize_string(bytes, deserialize_index);
                            
                            field.SetValue(instance, s);

                            break;

                        case Type deserialize_type when deserialize_type == typeof(float): // float

                            float f;

                            (f, deserialize_index) = Deserialize_float(bytes, deserialize_index);

                            field.SetValue(instance, f);

                            break;

                        case Type deserialize_type when deserialize_type.GetGenericTypeDefinition() == typeof(List<>) && deserialize_type.IsGenericType: // list

                            List<object> obj_list = new List<object>();
                            (obj_list, deserialize_index) = deserialize_list(bytes, deserialize_index);



                            Type list_type = field.FieldType;
                            dynamic correct_typed_list = Activator.CreateInstance(list_type);

                            foreach(object temp in obj_list)
                            {
                                list_type.GetMethod("Add").Invoke(correct_typed_list, new[] { temp });
                            }


                            field.SetValue(instance, correct_typed_list);

                            break;
                    }

                }

                objects.Add(instance); // once all fields have been deserialized from the above loop. add to the "deserialized_objects" list.

            }

            return (objects,deserialize_index); // once the end of the byte array is reached. return all objects found and the deserialize_index
        }

        // Deserializes a list.
        private (List<object> l, int deserialize_index) deserialize_list(byte[] bytes, int deserialize_index)
        {
            // get the size of the list
            int count;
            (count, deserialize_index) = Deserialize_int(bytes, deserialize_index);
            // after getting the size, the deserialize_index will now be on the first item of the list.

            List<object> list = new List<object>();

            // recursivley deserialize objects using the Deserialize_bytes function, using its optional paramaters to establish the length of the list
            // and the start position of the deserialization.
            (list ,deserialize_index) = Deserialize_bytes(bytes, count, deserialize_index);

            return (list, deserialize_index);
        }

        // Deserializes a float.
        private (float f, int deserialize_index) Deserialize_float(byte[] bytes, int deserialize_index)
        {
            float f;
            f = BitConverter.ToSingle(bytes, deserialize_index);
            return (f, deserialize_index += 4);
        }

        // Deserializes the supported type enum.
        private (Supported_Type, int) Deserialize_supported_type(byte[] bytes, int deserialize_index)
        {
            Supported_Type type = (Supported_Type)BitConverter.ToInt32(bytes, deserialize_index);
            
            return (type, deserialize_index += 4);
        }

        // Deserlizes an int.
        private (int i, int deserialize_index) Deserialize_int(byte[] bytes, int index)
        {
            int deserialized_int = BitConverter.ToInt32(bytes, index);

            return ( deserialized_int, index+=4);
        }

        // Deserializes a string.
        private (string, int) Deserialize_string(byte[] bytes, int index)
        {
            int end_index = Find_String_End_Position(index, bytes);
            string s = Encoding.UTF8.GetString(bytes, index, end_index - index); // get the string from the bytes, using the index and end_index.

            return (s, end_index + 1);
        }

        // used to find the string end position. 
        // Note: only works if the serialized string uses an empty byte to reprisent the strings end position.
        private int Find_String_End_Position(int start_index, byte[] bytes) 
        {
            for (int i = start_index; i < bytes.Length; i++)
            {
                if (bytes[i] == new byte())
                {
                    return i;
                }
            }
            throw new Exception("could not find null byte");
        }
    }
}
