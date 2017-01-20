using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    static class pParameters
    {
        internal static byte addressSize = 32;

        internal static byte base64AddressSize = 44;

        internal static int statsBufferSize = 10;

        internal static int propagation = 1;

        internal static byte requestHeaderParamsSize = 4; 

        internal static byte requestHeaderSize = (byte)(requestHeaderParamsSize + 6); //6 = sizeof(ip:port);

        internal static byte ipv4Addresssize = 4;

        internal static int packetSize = addressSize * 2029;

        internal static int postSize = (addressSize * 2029) - (requestHeaderSize + sizeof(int) + addressSize + 1); //sizeof(int) = content length; addressize = targetsize; 1 = binary

        internal static int packetHeaderSize = 21; //1 byte (data type) + 4 byte (offset) + 16 byte (hash)

        #region semantic search

        internal const byte semanticSearchSteps = 2;

        internal static byte semanticSearchReferences = 3;

        #endregion

        #region files

        internal static string localTempDir = "temp";

        internal static string localPacketsDir = "packets";

        internal static string localPacketsFile = "Packet.bin";

        internal static string peersPath = "Peer.bin";

        internal static string fileQueuePath = "FileQueue.bin";

        #endregion

        internal static int maxDataAddress = 400;

        internal static int max_upload_kb = 100;

        internal static int max_download_kb = 100;

        public static int time_out = 4000;

        public static int peers_interval = 10000;
        
        internal static int response_timeout = 2000;

        internal static int postTupleTimeout = 100;

        internal static int cacheActiveTimeoutInterval = 100;

        

        public static int send_localdata_interval = 1000;

        public static int GetPeerCountReturn = 10;

        public static int PacketsMaintenanceQueueSize = 1000;

        public static int PacketsMaxItems = 1000;

        public static int MetaPacketsMaintenanceQueueSize = 1000;

        public static int MetaPacketsMaxItems = 1000;

        public static int PeerMaintenanceQueueSize = 1000;

        public static int PeerMaxItems = 1000;

        public static int MaxDelayedWriteQueue = 20;

    }
}
