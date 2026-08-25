
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public static class SerializationHelper
    {
        public static T DeserializeXml<T>(this string toDeserialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

        public static string SerializeXml<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        public static T DeserializeJson<T>(this string toDeserialize)
        {
            return JsonUtility.FromJson<T>(toDeserialize);
        }

        public static string SerializeJson<T>(this T toSerialize)
        {
            return JsonUtility.ToJson(toSerialize);
        }
    }
}