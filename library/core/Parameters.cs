using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public static class pParameters
    {
        public static byte addressSize = 32;

        public static byte base64AddressSize = 44;

        public static int statsBufferSize = 10;

        public static int propagation = 1;

        public static byte requestHeaderParamsSize = 4; 

        public static byte requestHeaderSize = (byte)(requestHeaderParamsSize + 6); //6 = sizeof(ip:port);

        public static byte ipv4Addresssize = 4;

        public static int packetSize = addressSize * 2029;

        public static int postSize = (addressSize * 2029) - (requestHeaderSize + sizeof(int) + addressSize + 1); //sizeof(int) = content length; addressize = targetsize; 1 = binary

        public static int packetHeaderSize = 21; //1 byte (data type) + 4 byte (offset) + 16 byte (hash)

        #region semantic search

        public const byte semanticSearchSteps = 2;

        public static byte semanticSearchReferences = 3;

        #endregion

        #region files

        public static string localTempDir = "temp";

        public static string localPacketsDir = "packets";

        public static string localPacketsFile = "Packet.bin";

        public static string peersPath = "Peer.bin";

        public static string fileQueuePath = "FileQueue.bin";

        #endregion

        public static int maxDataAddress = 400;

        public static int max_upload_kb = 100;

        public static int max_download_kb = 100;

        public static int time_out = 4000;

        public static int restart_requesting_packets_from_coda_timeout = 0;

        public static int peers_interval = 10000;
        
        public static int response_timeout = 2000;

        public static int postTupleTimeout = 100;

        public static int cacheActiveTimeoutInterval = 100;

        

        public static int send_localdata_interval = 1000;

        public static int GetPeerCountReturn = 10;

        public static int PacketsMaintenanceQueueSize = 1000;

        public static int PacketsMaxItems = 1000;

        public static int MetaPacketsMaintenanceQueueSize = 1000;

        public static int MetaPacketsMaxItems = 1000;

        public static int PeerMaintenanceQueueSize = 1000;

        public static int PeerMaxItems = 1000;

        public static int MaxDelayedWriteQueue = 20;

        public static int QueueWebserverStreamMaxDistance = 1024 * 1024 * 3;

        public static int READFILE_RETRY_COUNT = 3;


        public static int WebServer_FileDownloadTimeout = 1000;
    }
}
