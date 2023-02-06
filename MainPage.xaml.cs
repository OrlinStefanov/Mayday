using Newtonsoft.Json;

using System.Globalization;
using System.Net;

namespace Glasses;

public partial class MainPage : ContentPage
{
    private ISpeechToText speechToText;
    private CancellationTokenSource tokenSource = new CancellationTokenSource();

    public Command ListenCommand { get; set; }
    public Command ListenCancelCommand { get; set; }
    public string RecognitionText { get; set; }

    IEnumerable<Locale> locales;
    private CancellationTokenSource _cancelTokenSource;

    private bool isworking = true;

    struct Days
    {
        public string tempMin;
        public string tempMax;
        public string temp;
    }

    struct CurrentConditions
    {
        public string temp;
    }

    class TempWeather
    {
        public string resolvedAddress;
        public string timezone;
        public Days[] days;
    }

    public MainPage(ISpeechToText speechToText)
	{
		InitializeComponent();

        this.speechToText = speechToText;

        ListenCommand = new Command(Listen);
        ListenCancelCommand = new Command(ListenCancel);
        BindingContext = this;        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        List<string> pickers = new List<string>();

        locales = await TextToSpeech.GetLocalesAsync();

        foreach (var locale in locales)
        {
            Languages.Items.Add(locale.Name);

            pickers.Add(locale.Name);
        }

        Languages.SelectedItem = pickers.LastOrDefault();

        Listen();
    }

    //my functions-------------------------------------------------------------------------------------------------
    private async void Listen()
    {
        var isauth = await speechToText.RequestPermissions();

        if (isauth)
        {
            try
            {
                RecognitionText = await speechToText.Listen(CultureInfo.GetCultureInfo("en-us"), new Progress<string>(partialText =>
                {
                    RecognitionText = partialText;
                    OnPropertyChanged(nameof(RecognitionText));
                }), tokenSource.Token);

                ListenJ();
            }
            catch (Exception ex)
            {
                Listen();
            }
        }
        else
        {
            await DisplayAlert("Premmision Error", "No microphone access", "OK");
            Listen();
        }
    }

    private void ListenCancel()
    {
        tokenSource?.Cancel();
        Listen();
    }

    private async void ListenJ()
    {
        if (Languages.SelectedIndex > 0)
        {
            await SecureStorage.SetAsync("lang", Languages.SelectedIndex.ToString());

            if (isworking == true)
            {
                switch (RecognitionText)
                {
                    case "hello":
                        Speak("Hello sur");
                        break;

                    case "how are you":
                        Speak("I'm fine what about you");
                        break;

                    case "what is the time":
                    case "what's the time":
                        DateTime time = DateTime.Now;
                        Speak("The time is" + time.TimeOfDay.ToString());
                        break;

                    case "open YouTube":
                        Uri uri = new Uri("https://www.youtube.com/");
                        await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);

                        Speak("opening YouTube");
                        break;

                    case "call Chris":
                        if (PhoneDialer.Default.IsSupported)
                        {
                            PhoneDialer.Default.Open("089-495-7416");
                        }

                        Speak("Calling Chris");
                        break;

                    case "what is your name":
                    case "what's your name":
                        Speak("My name is Mayday");
                        break;

                    case "what is my name":
                    case "what's my name":
                    case "do you know my name":
                        Speak("Your name is" + await SecureStorage.GetAsync("username"));
                        break;

                    case "what is the weather":
                    case "what's the weather":
                    case "what is the temperature":
                    case "what's the temperature":
                        GetWeatherAsync();
                        break;
                    case "what is Chris":
                    case "what's Chris":
                        Speak("Kris is black nigga");
                        break;

                    default:
                        Speak(RecognitionText);
                        break;

                    case "disconnect":
                        Speak("disconnecting");
                        isworking = false;
                        break;
                }
            } else
            {
                switch (RecognitionText)
                {
                    case "connect":
                        Speak("Connecting");
                        isworking = true;
                        break;

                    default:
                        Speak(RecognitionText);
                        break;
                }
            }
            
            if (RecognitionText == null)
            {
                Listen();
            } else
            {
                RecognitionText = null;
            }

            if (isworking)
            {
                con.Text = "connected";
                con.TextColor = Colors.Lime;

            } else
            {
                con.Text = "disconnected";
                con.TextColor = Colors.Red;
            }
        }
    }

    private void Speak(string text)
    {
        TextToSpeech.SpeakAsync(text, new SpeechOptions
        {
            Locale = locales.Single(l => l.Name == Languages.SelectedItem.ToString())
        });

        Listen();
    }

    private async void GetWeatherAsync()
    {
        string dateNow = DateTime.Now.ToString("yyyy-MM-dd");

        GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

        _cancelTokenSource = new CancellationTokenSource();

        Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

        string myLocation = location.Latitude + "," + location.Longitude;

        string url = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/" + myLocation + "/" + dateNow + "/?key=NCFPB2ZRJSPV346976GL8JQAX&include=current&unitGroup=metric&elements=tempmin,tempmax,temp,windspeed";


        string json = new WebClient().DownloadString(url);

        dynamic t = JsonConvert.DeserializeObject(json);


        TempWeather tempNow = new TempWeather();

        tempNow = JsonConvert.DeserializeObject<TempWeather>(json);

        Speak("The temperature is" + (string)(t.currentConditions.temp + "°C"));

        label1.Text = t.currentConditions.temp + "°C";
    }
}

