using Steamworks;
using Newtonsoft.Json;
using System.Threading;

namespace SteamUGCKVPViewer
{
    class SteamUGCKVPViewer
    {
        static AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
        private CallResult<SteamUGCQueryCompleted_t> queryCallback;
        PublishedFileId_t[]? subscribedItemIDs;
        UGCQueryHandle_t query;
        Thread callbackThread;
        private static bool steamInited = false;
        static void Main(string[] args)
        {
            SteamUGCKVPViewer viewer = new SteamUGCKVPViewer();
            viewer.GetAllIDs();
            stopWaitHandle.WaitOne();
            steamInited = false;
            Thread.Sleep(2000);
            SteamAPI.Shutdown();
            Console.ReadLine();
        }

        public SteamUGCKVPViewer()
        {
            Console.WriteLine($"Steam running: {SteamAPI.IsSteamRunning()}");

            if (!SteamAPI.Init())
            {
                Console.WriteLine("Could not connect to Steam Client, make sure its running");
                Environment.Exit(1);
            }
            steamInited = true;
            callbackThread = new Thread(new ThreadStart(GetCallbacks));
            callbackThread.Start();
            this.queryCallback = CallResult<SteamUGCQueryCompleted_t>.Create(OnSteamUGCQueryCompleted);

        }

        public void GetAllIDs()
        {
            subscribedItemIDs = new PublishedFileId_t[(int)SteamUGC.GetNumSubscribedItems()];
            SteamUGC.GetSubscribedItems(subscribedItemIDs, (uint)subscribedItemIDs.Length);
            if (subscribedItemIDs.Length == 0)
            {
                Console.WriteLine("No subscribed mods found! Subscribe to mods you want to check the metadata of (and make sure steam_appid.txt has the correct steamapp id");
                stopWaitHandle.Set();
                return;
            }

            query = SteamUGC.CreateQueryUGCDetailsRequest(subscribedItemIDs, (uint)subscribedItemIDs.Length);
            SteamUGC.SetReturnKeyValueTags(query, true);
            SteamUGC.SetReturnOnlyIDs(query, true);

            var handle = SteamUGC.SendQueryUGCRequest(query);
            queryCallback.Set(handle);
        }
        private void OnSteamUGCQueryCompleted(SteamUGCQueryCompleted_t pCallback, bool bIOFailure)
        {

            if (pCallback.m_eResult != EResult.k_EResultOK)
            {
                Console.WriteLine($"Error: {pCallback.m_eResult}");
                stopWaitHandle.Set();
                return;
            }

            /*
            TODO: Look into why pCallback.m_unTotalMatchingResults exists (pagination?)
            It could mean we cannot use resultIndex to get the ID from subscribedItemIDs like we currently do below
            */

            var handle = pCallback.m_handle;
            Dictionary<Steamworks.PublishedFileId_t, Dictionary<string, List<string>>> mods = new Dictionary<Steamworks.PublishedFileId_t, Dictionary<string, List<string>>>();

            for (uint resultIndex = 0; resultIndex < pCallback.m_unNumResultsReturned; resultIndex++)
            {
                var itemID = subscribedItemIDs[resultIndex];
                mods.Add(subscribedItemIDs[resultIndex], null);
                var numerOfKeyValueTags = SteamUGC.GetQueryUGCNumKeyValueTags(handle, resultIndex);

                var keyValueTags = new Dictionary<string, List<string>>();
                mods[itemID] = keyValueTags;
                for (uint keyValueTagIndex = 0; keyValueTagIndex < numerOfKeyValueTags; keyValueTagIndex++)
                {
                    string key;
                    string value;

                    // Given the info at the top of the file 255 as the size for both key and value should work?!
                    if (!SteamUGC.GetQueryUGCKeyValueTag(handle, resultIndex, keyValueTagIndex, out key, 255, out value, 255))
                    {
                        continue;
                    }

                    List<string> values;
                    if (!keyValueTags.TryGetValue(key, out values))
                    {
                        values = new List<string>();

                        keyValueTags[key] = values;
                    }

                    values.Add(value);
                }

            }
            Console.WriteLine(JsonConvert.SerializeObject(mods));
            SteamUGC.ReleaseQueryUGCRequest(query);
            stopWaitHandle.Set();

        }

        private void GetCallbacks()
        {
            while (steamInited)
            {
                Thread.Sleep(1000);
                SteamAPI.RunCallbacks();
            }
        }

    }
}


