
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

        public static byte[] MIME_TYPE_TEXT_THUMB = Utils.GetAddress();

        public static byte[] Culture = Utils.GetAddress();

        public static byte[] VERSAO = Utils.GetAddress();

        public static byte[] PT_BR = Utils.GetAddress();

        public static byte[] EN_US = Utils.GetAddress();

        public static byte[] ORDER = Utils.GetAddress();




        public static long Id_CONCEITO;

        public static long Id_CONTEUDO;


        public static long Id_MIME_TYPE_VIDEO_STREAM;

        public static long Id_MIME_TYPE_AUDIO_STREAM;


        public static long Id_MIME_TYPE_TEXT_STREAM;

        public static long Id_MIME_TYPE_WEB;

        public static long Id_MIME_TYPE_TEXT;

        public static long Id_MIME_TYPE_TEXT_THUMB;


        public static long Id_MIME_TYPE_DOWNLOAD;

        public static long Id_MIME_TYPE_IMAGE_THUMB;

        public static long Id_ORDER;

        public static long Id_ROOT_TYPE;




        public static byte[] AUTHOR = Utils.GetAddress();

        public static byte[] MIME_TYPE_IMAGE_THUMB = Utils.GetAddress();



        public static byte[] MIME_TYPE_DIRECTORY = Utils.GetAddress();

        public static byte[] MIME_TYPE_DOWNLOAD = Utils.GetAddress();

        public static byte[] ROOT_TYPE = Utils.GetAddress();

        public static byte[] ROOT_STREAM = Utils.GetAddress();

        public static byte[] ROOT_SEQUENCE = Utils.GetAddress();

        public static byte[] ROOT_APP = Utils.GetAddress();

        public static byte[] ROOT_IMAGE = Utils.GetAddress();

        public static byte[] ROOT_POST = Utils.GetAddress();

        public static byte[] Uploader = Utils.GetAddress();

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

        public static byte[] MIME_TYPE_IMAGE = Utils.GetAddress();

        public static byte[] MIME_TYPE_VIDEO_STREAM = Utils.GetAddress();

        public static byte[] MIME_TYPE_AUDIO_STREAM = Utils.GetAddress();

        public static byte[] MIME_TYPE_TEXT_STREAM = Utils.GetAddress();

        public static byte[] MIME_TYPE_WEB = Utils.GetAddress();

        public static byte[] MIME_TYPE_TEXT = Utils.GetAddress();

        public static byte[] CONTEUDO = Utils.GetAddress();
       
        public static byte[] Nome = Utils.GetAddress();

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

        public static byte[] IsVirtualAttribute(string AttributeName)
        {
            foreach (var t in fields ?? Fields)
            {
                if(t.Name == AttributeName)
                {
                    var v = (byte[])t.GetValue(null);

                    return v;
                }
            }

            return null;
        }

        public static void BootStrap()
        {
            Id_CONCEITO = BitConverter.ToInt64(CONCEITO, 0);

            Id_CONTEUDO = BitConverter.ToInt64(CONTEUDO, 0);


            Id_MIME_TYPE_VIDEO_STREAM = BitConverter.ToInt64(MIME_TYPE_VIDEO_STREAM, 0);

            Id_MIME_TYPE_AUDIO_STREAM = BitConverter.ToInt64(MIME_TYPE_AUDIO_STREAM, 0);


            Id_MIME_TYPE_TEXT_STREAM = BitConverter.ToInt64(MIME_TYPE_TEXT_STREAM, 0);

            Id_MIME_TYPE_WEB = BitConverter.ToInt64(MIME_TYPE_WEB, 0);

            Id_MIME_TYPE_TEXT = BitConverter.ToInt64(MIME_TYPE_TEXT, 0);

            Id_MIME_TYPE_TEXT_THUMB = BitConverter.ToInt64(MIME_TYPE_TEXT_THUMB, 0);


            Id_MIME_TYPE_DOWNLOAD = BitConverter.ToInt64(MIME_TYPE_DOWNLOAD, 0);

            Id_MIME_TYPE_IMAGE_THUMB = BitConverter.ToInt64(MIME_TYPE_IMAGE_THUMB, 0);

            Id_ORDER = BitConverter.ToInt64(ORDER, 0);

            Id_ROOT_TYPE = BitConverter.ToInt64(ROOT_TYPE, 0);

            foreach (var field in Fields)
            {
                var value = (byte[])field.GetValue(null);

                Client.Post(field.Name, value);

                Metapacket.Create(value, VirtualAttributes.CONCEITO);

                //if (field.Name == "MIME_TYPE_IMAGE_THUMB")
                //    return;
            }


            Client.Post("Ingles", VirtualAttributes.EN_US);

            Client.Post("Portugues", VirtualAttributes.PT_BR);

            Utils.StopInternalAddressCount(); 

        }

    }
}

