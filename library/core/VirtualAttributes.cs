using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public static class VirtualAttributes
    {
        public static byte[] CONCEITO = Utils.GetAddress();

        public static byte[] AUTHOR = Utils.GetAddress();

        public static byte[] MIME_TYPE_TEXT_THUMB = Utils.GetAddress();

        public static byte[] MIME_TYPE_IMAGE_THUMB = Utils.GetAddress();

        public static byte[] MIME_TYPE_DIRECTORY = Utils.GetAddress();

        public static byte[] MIME_TYPE_DOWNLOAD = Utils.GetAddress();

        public static byte[] ROOT_STREAM = Utils.GetAddress();

        public static byte[] ROOT_IMAGE = Utils.GetAddress();

        public static byte[] ROOT_POST = Utils.GetAddress();

        public static byte[] Uploader = Utils.GetAddress();

        public static byte[] Culture = Utils.GetAddress();

        public static byte[] FileExtension = Utils.GetAddress();

        public static byte[] FileName = Utils.GetAddress();

        public static byte[] Title = Utils.GetAddress();

        public static byte[] Artist = Utils.GetAddress();

        

        public static byte[] Comment = Utils.GetAddress();

        public static byte[] Genre = Utils.GetAddress();

        public static byte[] Track = Utils.GetAddress();

        public static byte[] Episode = Utils.GetAddress();

        public static byte[] Season = Utils.GetAddress();

        public static byte[] Show = Utils.GetAddress();

        public static byte[] Lyrics = Utils.GetAddress();

        public static byte[] Grouping = Utils.GetAddress();

        public static byte[] BeatsPerMinute = Utils.GetAddress();

        public static byte[] Disc = Utils.GetAddress();

        public static byte[] Duration = Utils.GetAddress();

        public static byte[] Description = Utils.GetAddress();

        public static byte[] AudioBitrate = Utils.GetAddress();

        public static byte[] AudioSampleRate = Utils.GetAddress();

        public static byte[] BitsPerSample = Utils.GetAddress();

        public static byte[] AudioChannels = Utils.GetAddress();

        public static byte[] Quality = Utils.GetAddress();

        public static byte[] Album = Utils.GetAddress();

        public static byte[] MIMEType = Utils.GetAddress();

        public static byte[] Height = Utils.GetAddress();

        public static byte[] Width = Utils.GetAddress();

        public static byte[] Resolution = Utils.GetAddress();


        public static byte[] Year = Utils.GetAddress();

        public static byte[] VideoCodec = Utils.GetAddress();

        public static byte[] AudioCodec = Utils.GetAddress();






        //todo: gerar numeros, talvez mudar de lugar
        

        

        public static byte[] MIME_TYPE_IMAGE = Utils.GetAddress();

        public static byte[] MIME_TYPE_VIDEO_STREAM = Utils.GetAddress();

        public static byte[] MIME_TYPE_AUDIO_STREAM = Utils.GetAddress();

        public static byte[] MIME_TYPE_TEXT_STREAM = Utils.GetAddress();

        public static byte[] MIME_TYPE_WEB = Utils.GetAddress();

        public static byte[] MIME_TYPE_TEXT = Utils.GetAddress();
        public static byte[] CONTEUDO = Utils.GetAddress();
       
        public static byte[] Nome = Utils.GetAddress();
        
        public static byte[] VERSAO = Utils.GetAddress();


        public static byte[] DATA = Utils.GetAddress();
        
        

        public static int Count = Utils.AddressCount;

        static FieldInfo[] fields = null;

        static FieldInfo[] Fields
        {
            get
            {
                if (fields == null)
                    fields = typeof(VirtualAttributes).GetFields().Where(x => x.FieldType == typeof(byte[])).ToArray();

                return fields;
            }
        }

        public static string PropertyIndex(int index)
        {
            if (index > Count)
                return string.Empty;

            if (index == 0)
                return string.Empty;

            return Fields[index - 1].Name;
        }

        public static bool IsVirtualAttribute(byte[] address)
        {
            foreach(var t in fields ?? Fields)
            {
                var v = (byte[])t.GetValue(null);

                if (Addresses.Equals(v, address))
                    return true;
            }

            return false;
        }

        public static void BootStrap()
        {
            foreach(var field in Fields)
            {
                var value = (byte[])field.GetValue(null);

                
                Client.Post(title: field.Name, conceptAddress: value);

                Metapacket.Create(value, VirtualAttributes.CONCEITO);

                if (field.Name == "MIME_TYPE_IMAGE_THUMB")
                    return;
            }
                
        }

    }
}

