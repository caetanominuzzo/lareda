using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace library
{
    public class Metapacket
    {
        internal DateTime LastAccess = DateTime.MinValue;

        internal byte[] Address;

        internal byte[] TargetAddress;

        internal byte[] LinkAddress;

        internal long IdAddress;

        internal long IdTargetAddress;

        internal long IdLinkAddress;

        internal byte[] Hash;

        internal DateTime Creation = DateTime.MinValue;

        internal MetaPacketType Type;


        internal string Base64Hash
        {
            get { return Utils.ToBase64String(Hash); }
            set { Hash = Utils.AddressFromBase64String(value); }
        }

        internal string Base64LinkAddress
        {
            get { return Utils.ToBase64String(LinkAddress); }
            set { LinkAddress = Utils.AddressFromBase64String(value); }
        }


        internal string Base64Address
        {
            get { return Utils.ToBase64String(Address); }
            set { Address = Utils.AddressFromBase64String(value); }
        }

        public string Base64TargetAddress
        {
            get { return Utils.ToBase64String(TargetAddress); }
        }

        public string SimpleLinkAddress
        {
            get { return Utils.ToSimpleAddress(LinkAddress); }
        }

        public string SimpleAddress
        {
            get { return Utils.ToSimpleAddress(Address); }
        }

        public string SimpleTargetAddress
        {
            get { return Utils.ToSimpleAddress(TargetAddress); }
        }

        byte[] _marker = null;

        internal byte[] Marker
        {
            get { return _marker; }
            set
            {
                _marker = value;

                Id_Marker = BitConverter.ToInt64(value, 0);

            }
        }

        internal long Id_Marker;

        internal static Cache<byte[]> DistancesItems = new Cache<byte[]>(60 * 1000000);

        internal Metapacket(
            DateTime creation,
            byte[] targetAddress = null,
            byte[] linkAddress = null,
            byte[] hashContent = null,
            MetaPacketType type = MetaPacketType.Link,
            byte[] address = null)
        {
            //todo: né
            if (!DistancesItems.Any())
            {
                DistancesItems.Add(VirtualAttributes.MIME_TYPE_DIRECTORY);

                DistancesItems.Add(VirtualAttributes.Track);

                DistancesItems.Add(VirtualAttributes.CONTEUDO);

                DistancesItems.Add(VirtualAttributes.CONCEITO);

                DistancesItems.Add(VirtualAttributes.AUTHOR);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_TEXT_THUMB);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_TEXT);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_VIDEO_STREAM);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_AUDIO_STREAM);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_TEXT_STREAM);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_IMAGE);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_IMAGE_THUMB);

                DistancesItems.Add(VirtualAttributes.ROOT_TYPE);

                DistancesItems.Add(VirtualAttributes.ORDER);

                DistancesItems.Add(VirtualAttributes.MIME_TYPE_DOWNLOAD);

                DistancesItems.Add(VirtualAttributes.ROOT_TYPE);

                DistancesItems.Add(VirtualAttributes.ROOT_SEQUENCE);

                DistancesItems.Add(VirtualAttributes.ROOT_STREAM);
            }

            Creation = creation;

            Address = address ?? Utils.GetAddress();

            this.TargetAddress = targetAddress ?? Address;

            this.Hash = hashContent;

            this.LinkAddress = linkAddress;

            this.IdAddress = BitConverter.ToInt64(this.Address, 0);

            this.IdLinkAddress = BitConverter.ToInt64(this.LinkAddress, 0);

            this.IdTargetAddress = BitConverter.ToInt64(this.TargetAddress, 0);

            this.Type = type;

            if (Hash != null && !Addresses.Equals(Hash, Addresses.zero))
            {
                Marker = VirtualAttributes.CONTEUDO;
            }
            else

            if (this.LinkAddress != null)
            {
                if (DistancesItems.Any(x => Addresses.Equals(x.CachedValue, LinkAddress)))
                    Marker = LinkAddress;
            }
        }

        static public Metapacket Create(
            byte[] targetAddress = null,
            byte[] linkAddress = null,
            byte[] hashContent = null,
            MetaPacketType type = MetaPacketType.Link,
            byte[] address = null)
        {
            Metapacket result = new Metapacket(DateTime.UtcNow, targetAddress, linkAddress, hashContent, type, address);

            MetaPackets.Add(result, Client.LocalPeer);

            return result;
        }

        internal string ToString()
        {
            return string.Format("{7}\r\nAddress: {0}\r\nTarget: {1}\r\nLink: {2}\r\nHash: {3} [{6}]\r\nCreation: {4}\r\nLastAccess: {5}\r\n",
                Utils.ToSimpleAddress(Address),
                Utils.ToSimpleAddress(TargetAddress),
                Utils.ToSimpleAddress(LinkAddress),
                Hash == null || Addresses.Equals(Hash, Addresses.zero) ? "" : "CONTEUDO",
                Creation,
                LastAccess,
                Hash == null ? "null" : Hash.Length.ToString(),
                Type == MetaPacketType.Hash? "hash" : "link");
        }
    }
}
