using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

// TODO: if BitConverter.IsLittleEndian -> need to do a array.Reverse().
// TODO: add a version
namespace WebSocketSharpController.Serializer
{
    /// <summary>
    /// Serialize only public properties.
    /// </summary>
    public class BinarySerializer : IDisposable
    {
        private enum TypeId
        {
            Bool = 0,
            Byte,
            SByte,
            Char,
            Decimal,
            Double,
            Float,
            Int,
            UInt,
            Long,
            ULong,
            Short,
            UShort,
            String,

            // complex types
            Object,
            Array,
            Enum,
            List,
            Dictionary,
        }

        /// <summary>
        /// Serialize the object in the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        public void Serialize(Stream stream, object value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            WriteValue(stream, value);
        }

        

        /// <summary>
        /// Deserialize an object using the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public object Deserialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return ReadValue(stream);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private void WriteValue(Stream stream, object obj, bool skipHeader = false)
        {
            var type = obj.GetType();
            if (type == typeof(bool))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Bool);
                stream.Write(BitConverter.GetBytes((bool)obj), 0, 0);
            }
            else if (type == typeof(byte))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Byte);
                stream.WriteByte((byte)obj);
            }
            else if (type == typeof(sbyte))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.SByte);
                stream.WriteByte((byte)obj);
            }
            else if (type == typeof(char))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Char);
                stream.Write(BitConverter.GetBytes((char)obj), 0, 0);
            }
            else if (type == typeof(decimal))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Decimal);

                var bits = decimal.GetBits((decimal)obj);
                var bytes = new List<byte>();
                foreach (var i in bits)
                    bytes.AddRange(BitConverter.GetBytes(i));
                stream.Write(bytes.ToArray(), 0, 0);
            }
            else if (type == typeof(double))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Double);
                stream.Write(BitConverter.GetBytes((double)obj), 0, 0);
            }
            else if (type == typeof(float))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Float);
                stream.Write(BitConverter.GetBytes((float)obj), 0, 0);
            }
            else if (type == typeof(int))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Int);
                stream.Write(BitConverter.GetBytes((int)obj), 0, 0);
            }
            else if (type == typeof(uint))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.UInt);
                stream.Write(BitConverter.GetBytes((uint)obj), 0, 0);
            }
            else if (type == typeof(long))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Long);
                stream.Write(BitConverter.GetBytes((long)obj), 0, 0);
            }
            else if (type == typeof(ulong))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.ULong);
                stream.Write(BitConverter.GetBytes((ulong)obj), 0, 0);
            }
            else if (type == typeof(short))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.Short);
                stream.Write(BitConverter.GetBytes((short)obj), 0, 0);
            }
            else if (type == typeof(ushort))
            {
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.UShort);
                stream.Write(BitConverter.GetBytes((ushort)obj), 0, 0);
            }
            else if (type == typeof(string))
            {
                var str = (string)obj;
                if (!skipHeader)
                    stream.WriteByte((byte)TypeId.String);
                var bytes = Encoding.UTF8.GetBytes(str);
                stream.Write(BitConverter.GetBytes(bytes.Length), 0, 0);
                stream.Write(bytes, 0, 0);
            }
            else
            {
                if (type.IsEnum)
                {
                    if (!skipHeader)
                        stream.WriteByte((byte)TypeId.Enum);
                    // TODO: using integer, but we should do a sizeof since we can set it in the declaration: enum MyType : long {...}
                    stream.Write(BitConverter.GetBytes((int)obj), 0, 0);
                }
                else if (type.IsArray)
                {
                    stream.WriteByte((byte)TypeId.Array);
                    if (type.GetElementType() != null)
                    {
                        if (type.GetElementType().IsPrimitive)
                        {
                            var array = (Array)obj;
                            for (var i = 0; i < array.Length; i++)
                                WriteValue(stream, array.GetValue(i), (i > 0));
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("An array should always have an Element type.");
                    }
                }
                else if (typeof(IList).IsAssignableFrom(type))
                {
                    stream.WriteByte((byte)TypeId.List);
                    // TODO: we have a list.
                    throw new NotImplementedException();
                }
                else if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    stream.WriteByte((byte)TypeId.Dictionary);
                    // TODO: we have a dict -> key/value.
                    throw new NotImplementedException();
                }
                else
                {
                    if (!skipHeader)
                        stream.WriteByte((byte)TypeId.Object);

                    // object: [nameHashed, propCount, properties]
                    var valueType = obj.GetType();
                    var classHash = MessageControllerFactory.GetClassHash(valueType);
                    WriteValue(stream, classHash, true);
                    var properties = valueType.GetProperties();

                    byte count = 0; // should be unlikely that we have more than 256 properties
                    foreach (var propertyInfo in properties)
                        if (propertyInfo.GetValue(obj) != null) // skip if value is null.
                            count++;
                    WriteValue(stream, count, true);

                    foreach (var propertyInfo in properties)
                    {
                        var value = propertyInfo.GetValue(obj);
                        if (value == null) // skip if value is null.
                            continue;

                        var hashValue = MessageControllerFactory.GetPropertyHash(propertyInfo);
                        WriteValue(stream, hashValue, true);
                        WriteValue(stream, value);
                    }
                }
            }
        }

        private object ReadValue(Stream stream, TypeId typeId)
        {
            var buffer = new byte[1024];
            if (typeId == TypeId.Bool)
            {
                stream.Read(buffer, 0, sizeof(bool));
                return BitConverter.ToBoolean(buffer, 0);
            }
            if (typeId == TypeId.Byte)
            {
                stream.Read(buffer, 0, sizeof(byte));
                return (byte)BitConverter.ToChar(buffer, 0);
            }
            if (typeId == TypeId.SByte)
            {
                stream.Read(buffer, 0, sizeof(sbyte));
                return (sbyte)BitConverter.ToChar(buffer, 0);
            }
            if (typeId == TypeId.Char)
            {
                stream.Read(buffer, 0, sizeof(char));
                return BitConverter.ToChar(buffer, 0);
            }
            if (typeId == TypeId.Decimal)
            {
                throw new NotImplementedException();
            }
            if (typeId == TypeId.Double)
            {
                stream.Read(buffer, 0, sizeof(double));
                return BitConverter.ToDouble(buffer, 0);
            }
            if (typeId == TypeId.Float)
            {
                stream.Read(buffer, 0, sizeof(float));
                return BitConverter.ToSingle(buffer, 0);
            }
            if (typeId == TypeId.Int)
            {
                stream.Read(buffer, 0, sizeof(int));
                return BitConverter.ToInt32(buffer, 0);
            }
            if (typeId == TypeId.UInt)
            {
                stream.Read(buffer, 0, sizeof(uint));
                return BitConverter.ToUInt32(buffer, 0);
            }
            if (typeId == TypeId.Long)
            {
                stream.Read(buffer, 0, sizeof(long));
                return BitConverter.ToInt64(buffer, 0);
            }
            if (typeId == TypeId.ULong)
            {
                stream.Read(buffer, 0, sizeof(ulong));
                return BitConverter.ToUInt64(buffer, 0);
            }
            if (typeId == TypeId.Short)
            {
                stream.Read(buffer, 0, sizeof(short));
                return BitConverter.ToInt16(buffer, 0);
            }
            if (typeId == TypeId.UShort)
            {
                stream.Read(buffer, 0, sizeof(ushort));
                return BitConverter.ToUInt16(buffer, 0);
            }
            if (typeId == TypeId.String)
            {
                stream.Read(buffer, 0, sizeof(int));
                var bufferLength = BitConverter.ToInt32(buffer, 0);

                var tmpBuffer = buffer;
                if (bufferLength >= buffer.Length)
                    tmpBuffer = new byte[bufferLength]; // TODO: if string too long
                
                stream.Read(tmpBuffer, 0, bufferLength);
                return Encoding.UTF8.GetString(tmpBuffer, 0, bufferLength);
            }
            if (typeId == TypeId.Enum)
            {
                stream.Read(buffer, 0, sizeof(int));
                return BitConverter.ToInt32(buffer, 0);
            }
            if (typeId == TypeId.Array)
            {
                var elementTypeId = (TypeId)stream.ReadByte();
                var count = (int)ReadValue(stream, TypeId.Int);
                var array = new ArrayList();
                for (var i = 0; i < count; i++)
                    array.Add(ReadValue(stream, elementTypeId));
                return array;
            }
            if (typeId == TypeId.Object)
            {
                var classHash = (uint)ReadValue(stream, TypeId.UInt);
                var classHashItem = MessageControllerFactory.FindClassByHash(classHash);
                var propCount = (byte)ReadValue(stream, TypeId.Byte);
                if (classHashItem == null) // TODO: could just read the object and ignore it...
                {
                    // just skip the object
                    for (var i = 0; i < propCount; i++)
                    {
                        ReadValue(stream, TypeId.UShort);
                        ReadValue(stream);
                    }

                    return null;
                }
                else
                {
                    var obj = System.Activator.CreateInstance(classHashItem.ClassType);
                    for (var i = 0; i < propCount; i++)
                    {
                        var propertyHash = (ushort)ReadValue(stream, TypeId.UShort);
                        var value = ReadValue(stream);
                        var propertyInfo = classHashItem.FindByHash(propertyHash);
                        if (propertyInfo != null) // if property isn't there, just skip it.
                            propertyInfo.SetValue(obj, value);
                    }

                    return obj;
                }
            }

            throw new NotImplementedException();
        }

        private object ReadValue(Stream stream)
        {
            var typeId = (TypeId)stream.ReadByte();
            return ReadValue(stream, typeId);
        }
    }
}
