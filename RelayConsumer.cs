using SignalWire.Relay;
using SignalWire.Relay.Calling;
using Xunit;

namespace fetcher_tester;

public class RelayConsumer : Consumer
{
    protected override void Setup()
    {
        var relayContext = AppConfig.GetConfigValue("relay_context");
        if (string.IsNullOrWhiteSpace(relayContext))
            throw new NullReferenceException("relay_context must be provided in configuration");

        Host = AppConfig.GetConfigValue("relay_host");
        Contexts = new List<string>() { relayContext };
        Project = AppConfig.GetConfigValue("test_project_id");
        Token = AppConfig.GetConfigValue("test_api_token");
    }

    protected override void OnIncomingCall(Call call)
    {
        Console.WriteLine("New call received on relay context! Answering now.");
        AnswerResult resultAnswer = call.Answer();

        if (!resultAnswer.Successful)
        {
            Stop();
            return;
        }

        RecordAction actionRecord = call.RecordAsync(new CallRecord
        {
            Audio = new CallRecord.AudioParams
            {
                Direction = CallRecord.AudioParams.AudioDirection.both,
                InitialTimeout = 5,
                EndSilenceTimeout = 5,
            }
        });

        call.PlayTTS("Welcome to SignalWire! This call is being recorded for quality assurance purposes.");

        while (!actionRecord.Completed)
            Thread.Sleep(1000);

        Console.WriteLine("The recording was {0}", actionRecord.Result.Successful ? "Successful" : "Unsuccessful");
        if (actionRecord.Result.Successful)
        {
            Assert.NotNull(actionRecord.Result.Duration);
            Console.WriteLine("You can find the {0} duration recording at {1}", TimeSpan.FromSeconds(actionRecord.Result.Duration.Value).ToString(), actionRecord.Result.Url);
        }

        call.Hangup();
        Stop();
    }
}
