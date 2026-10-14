using Grimoire.Botting;
using Grimoire.Botting.Commands.Misc;
using Newtonsoft.Json;
using System;

internal static class LegacyGbotCompatibilitySmoke
{
    private static int Main()
    {
        const string legacyBot = @"{
  '$type': 'Grimoire.Botting.Configuration, Grimoire, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null',
  'Commands': {
    '$type': 'System.Collections.Generic.List`1[[Grimoire.Botting.IBotCommand, Grimoire, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
    '$values': [
      {
        '$type': 'Grimoire.Botting.Commands.Misc.CmdDelay, Grimoire, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null',
        'Delay': 321
      }
    ]
  },
  'Author': 'Legacy bot author',
  'Description': 'Compatibility fixture with older assembly metadata',
  'BotDelay': 1000
}";

        try
        {
            Configuration configuration = JsonConvert.DeserializeObject<Configuration>(
                legacyBot,
                BotJson.CreateSettings(DefaultValueHandling.Include));

            if (configuration == null)
                return Fail("The legacy bot deserialized to null.");
            if (configuration.Commands == null || configuration.Commands.Count != 1)
                return Fail("The legacy command list was not restored.");
            CmdDelay delay = configuration.Commands[0] as CmdDelay;
            if (delay == null || delay.Delay != 321)
                return Fail("The legacy command type or values changed.");
            if (configuration.Skills == null || configuration.Quests == null || configuration.Drops == null)
                return Fail("Fields absent from an older GBot did not keep their safe defaults.");

            string roundTrip = JsonConvert.SerializeObject(configuration, BotJson.CreateSettings());
            Configuration reloaded = JsonConvert.DeserializeObject<Configuration>(
                roundTrip,
                BotJson.CreateSettings());
            if (reloaded == null || reloaded.Commands == null || reloaded.Commands.Count != 1)
                return Fail("A legacy bot did not survive a current-format round trip.");

            Console.WriteLine("Legacy GBot compatibility passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }
}
