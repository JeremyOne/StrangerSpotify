using System;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;


namespace StrangerSpotify
{
    class Program
    {
        static SpotifyLocalAPI Spotify;
        static string BaseUrl;
        static int TriggerMod;

        static bool SpotifyIsPlaying, SpotifyIsAdPlaying;
        static Track SpotifyCurrentTrack = null;
        static Track SpotifyLastTrack = null;
        static int SpotifyTrackNumber = 0;

        static void Main(string[] args) {

            BaseUrl = Properties.Settings.Default.TriggerBaseUrl;
            TriggerMod = Properties.Settings.Default.TriggerMod;

            try {
                Spotify = new SpotifyLocalAPI();

                if (!SpotifyLocalAPI.IsSpotifyRunning()) {
                    SpotifyLocalAPI.RunSpotify();
                    Console.WriteLine("Waiting for Spotify Local");
                    System.Threading.Thread.Sleep(3000);
                }

                if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning()) {
                    SpotifyLocalAPI.RunSpotifyWebHelper();
                    Console.WriteLine("Waiting for Spotify Web");
                    System.Threading.Thread.Sleep(3000);
                }

                while (!Spotify.Connect()) {
                    Console.WriteLine("Waiting for Spotify Connect");
                    System.Threading.Thread.Sleep(3000);
                }

                Register();

                Console.WriteLine("Press 'q' to quit.");
                while (Console.ReadKey().KeyChar != 'q') {
                    //wait forever for 'q'
                }
                
            } catch (Exception ex) {

                Console.WriteLine(ex.Message.ToString());
                Console.WriteLine("Press any key to continue...");
                Console.Read();
            }

        }
        
        public static void Register() {
            UpdateTrack();

            //register the events we want to listen for
            Spotify.OnTrackChange += Spotify_OnTrackChange;
            Spotify.OnPlayStateChange += Spotify_OnPlayStateChange;

            Spotify.ListenForEvents = true;
        }

        static void Spotify_OnPlayStateChange(object sender, PlayStateEventArgs e) {
            if (SpotifyIsAdPlaying) {
                return;
            }

            SpotifyIsPlaying = e.Playing;

            if (SpotifyCurrentTrack != null) {
                if (SpotifyIsPlaying) {
                    if (SpotifyLastTrack == null) { 
                        UpdateTrack();
                    }
                }
            }
        }

        static void Spotify_OnTrackChange(object sender, TrackChangeEventArgs e) {
            if (SpotifyIsAdPlaying) {
                return;
            }

            UpdateTrack();
        }

        /// <summary>
        /// Read track info from Spotify API and update local variables
        /// </summary>
        private static void UpdateTrack() {
            if (Spotify == null) {
                return;
            }

            StatusResponse status = Spotify.GetStatus();

            //check if an ad is playing
            if (status == null || status.Track == null) {
                SpotifyIsAdPlaying = true;
                return;
            } else {
                SpotifyIsAdPlaying = false;
            }

            SpotifyCurrentTrack = status.Track;

            if ((!status.NextEnabled && !status.PrevEnabled) ||
                (SpotifyCurrentTrack.AlbumResource == null || SpotifyCurrentTrack.ArtistResource == null)) {
                //invalid
                return;
            }
            
            if (SpotifyCurrentTrack != null) {

                SpotifyTrackNumber++;
                if (SpotifyTrackNumber % TriggerMod == 0) {
                    //Launch a new async task
                    new Task(TriggerAction).Start();
                }

                Console.WriteLine(String.Format("Spotify is playing: {0}: {1} - {2}", SpotifyTrackNumber, SpotifyCurrentTrack.ArtistResource.Name, SpotifyCurrentTrack.TrackResource.Name));
                
                SpotifyLastTrack = SpotifyCurrentTrack;
            }
        }

        static void TriggerAction() {
            Console.WriteLine("Triggering events...");

            var wc = new System.Net.WebClient();

            wc.DownloadData(BaseUrl + "/off");
            System.Threading.Thread.Sleep(5000);
            wc.DownloadData(BaseUrl + "/toggle");
            System.Threading.Thread.Sleep(1000);
            wc.DownloadData(BaseUrl + "/toggle");
            wc.DownloadData(BaseUrl + "/flicker?count=200");
            wc.DownloadData(BaseUrl + "/off");
            System.Threading.Thread.Sleep(2000);
            wc.DownloadData(BaseUrl + "/on");
            wc.DownloadData(BaseUrl + "/on");
            wc.DownloadData(BaseUrl + "/on");

            Console.WriteLine("Done, waiting for next trigger");
        }
        
    }
}
