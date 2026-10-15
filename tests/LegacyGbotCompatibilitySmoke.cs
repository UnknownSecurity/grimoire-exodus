using Grimoire.Botting;
using Grimoire.Botting.Commands.Misc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

internal static class LegacyGbotCompatibilitySmoke
{
    private static int Main(string[] args)
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

            if (args.Length > 0 && Directory.Exists(args[0]))
            {
                string[] botFiles = Directory.GetFiles(args[0], "*.gbot", SearchOption.AllDirectories);
                int failed = 0;
                foreach (string botFile in botFiles)
                {
                    try
                    {
                        if (new FileInfo(botFile).Length > 32L * 1024L * 1024L)
                            throw new InvalidDataException("File exceeds the 32 MB safety limit.");

                        Configuration bot = JsonConvert.DeserializeObject<Configuration>(
                            File.ReadAllText(botFile),
                            BotJson.CreateSettings());
                        if (bot == null || bot.Commands == null)
                            throw new InvalidDataException("File did not contain a valid bot configuration.");
                        if (bot.Commands.Any(command => command == null))
                            throw new InvalidDataException("File contains a null bot command.");
                        if (bot.Skills == null || bot.Quests == null || bot.Boosts == null
                            || bot.Drops == null || bot.Items == null || bot.NotifyUponDrop == null)
                        {
                            throw new InvalidDataException("File contains a null runtime collection.");
                        }

                        string serialized = JsonConvert.SerializeObject(bot, BotJson.CreateSettings());
                        Configuration roundTripped = JsonConvert.DeserializeObject<Configuration>(
                            serialized,
                            BotJson.CreateSettings());
                        if (roundTripped == null || roundTripped.Commands == null
                            || roundTripped.Commands.Count != bot.Commands.Count)
                        {
                            throw new InvalidDataException("Command list changed during a current-format round trip.");
                        }
                        for (int commandIndex = 0; commandIndex < bot.Commands.Count; commandIndex++)
                        {
                            IBotCommand roundTrippedCommand = roundTripped.Commands[commandIndex];
                            if (roundTrippedCommand == null
                                || roundTrippedCommand.GetType() != bot.Commands[commandIndex].GetType())
                            {
                                throw new InvalidDataException(
                                    "Command type or order changed at index " + commandIndex + ".");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        Console.Error.WriteLine(Path.GetFileName(botFile) + ": " + exception.GetBaseException().Message);
                    }
                }

                if (failed > 0)
                    return Fail(failed + " of " + botFiles.Length + " GBot files failed compatibility validation.");
                Console.WriteLine("Validated " + botFiles.Length + " GBot files from " + args[0] + ".");
            }

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
