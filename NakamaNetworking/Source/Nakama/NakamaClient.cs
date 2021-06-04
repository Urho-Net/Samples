using Urho;
using System;
using System.Threading.Tasks;
using Nakama;


namespace NakamaNetworking
{
    public class NakamaClient
    {
        public string Scheme = "http";
        public string Host = "192.168.1.110";
        public int Port = 7350;

        public string ServerKey = "defaultkey";

        public IClient Client = null;

        public ISession Session = null;

        public ISocket Socket = null;

        private const string SessionPrefName = "nakama.session";
        private const string DeviceIdentifierPrefName = "nakama.deviceUniqueIdentifier";

        public string currentMatchmakingTicket = String.Empty;

        public string matchID = String.Empty;

        public NakamaClient()
        {
            LogSharp.LogLevel = LogSharpLevel.Debug;
        }
        public bool IsConnectedToNakamaServer()
        {
            return (Socket != null && Socket.IsConnected == true);
        }

        /// <summary>
        /// Connects to the Nakama server using device authentication and opens socket for realtime communication.
        /// </summary>
        public async Task Connect()
        {

            try
            {
                // Connect to the Nakama server.
                Client = new Nakama.Client(Scheme, Host, Port, ServerKey);

                // Attempt to restore an existing user session.
                var authToken = PlayerPrefs.GetString(SessionPrefName);
                if (!string.IsNullOrEmpty(authToken))
                {
                    var session = Nakama.Session.Restore(authToken);
                    if (!session.IsExpired)
                    {
                        Session = session;
                    }
                }

                // If we weren't able to restore an existing session, authenticate to create a new user session.
                if (Session == null)
                {
                    string deviceId;

                    // If we've already stored a device identifier in PlayerPrefs then use that.
                    if (PlayerPrefs.HasKey(DeviceIdentifierPrefName))
                    {
                        deviceId = PlayerPrefs.GetString(DeviceIdentifierPrefName);
                    }
                    else
                    {

                        deviceId = System.Guid.NewGuid().ToString();
                        // Store the device identifier to ensure we use the same one each time from now on.
                        PlayerPrefs.SetString(DeviceIdentifierPrefName, deviceId);
                    }

                    // Use Nakama Device authentication to create a new session using the device identifier.
                    Session = await Client.AuthenticateDeviceAsync(deviceId);

                    // Store the auth token that comes back so that we can restore the session later if necessary.
                    PlayerPrefs.SetString(SessionPrefName, Session.AuthToken);
                }

                // Open a new Socket for realtime communication.
                Socket = Nakama.Socket.From(Client);
                await Socket.ConnectAsync(Session, true);

                LogSharp.Debug(Session.ToString());
                LogSharp.Debug(Socket.ToString());
            }
            catch (Exception e)
            {
                LogSharp.Error(e.ToString());
            }
        }


        /// <summary>
        /// Starts looking for a match with a given number of minimum players.
        /// </summary>
        public async Task FindMatch(int minPlayers = 2)
        {
            if (IsConnectedToNakamaServer() == false) return;

            if (currentMatchmakingTicket == String.Empty)
            {
                var matchMakingTicket = await Socket.AddMatchmakerAsync("*", minPlayers, minPlayers);
                currentMatchmakingTicket = matchMakingTicket.Ticket;
            }
        }

      

   

        // Perhaps just call this directly from the Socket since it's public already?
        /// <summary>
        /// Cancels the current matchmaking request.
        /// </summary>
        public async Task CancelMatchmaking()
        {
            await Socket.RemoveMatchmakerAsync(currentMatchmakingTicket);
        }

    }

}