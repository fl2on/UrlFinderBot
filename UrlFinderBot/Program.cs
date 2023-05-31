using dnlib.DotNet;
using dnlib.DotNet.Emit;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Reflection;
using System.Text;
using static System.Text.RegularExpressions.Regex;

namespace UrlFinderBot;

public class UrlFinderCommand : ApplicationCommandModule
{
    [SlashCommand("urlfinder", "Finds URLs in a .NET module")]
    public async Task UrlFinderCommandAsync(InteractionContext ctx,
        [Option("exe-file", "Executable file (.exe)")] DiscordAttachment exeFile)
    {
        var builder = new DiscordInteractionResponseBuilder().AsEphemeral();

        // Check if the provided file is a valid .exe file
        if (!exeFile.FileName.EndsWith(".exe"))
        {
            builder.WithContent("Please provide a valid .exe file.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
            return;
        }

        try
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "URL Finder",
                Color = DiscordColor.Blue
            };

            using (var client = new HttpClient())
            {
                var fileBytes = await client.GetByteArrayAsync(exeFile.Url);

                // Perform additional security checks on the file
                var isSafe = IsFileSafe(fileBytes);
                if (!isSafe)
                {
                    builder.WithContent(
                        "The provided file contains potentially malicious content. Aborting operation.");
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                    return;
                }

                var module = ModuleDefMD.Load(fileBytes);

                var messageBuilder = new StringBuilder();

                messageBuilder.AppendLine();
                embedBuilder.WithDescription($"Module Name: {module.Name}");
                messageBuilder.AppendLine();

                var urlFound = false;

                foreach (var type in module.Types)
                {
                    if (type.IsGlobalModuleType || type.Name == "Resources" || type.Name == "Settings")
                        continue;

                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody)
                            continue;

                        method.Body.KeepOldMaxStack = true;

                        foreach (var op in method.Body.Instructions)
                            if (op.OpCode == OpCodes.Ldstr)
                            {
                                var str = op.Operand.ToString();
                                if (str != null && !IsMatch(str,
                                        @"(?<=\()\b(https?://|www\.)[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|](?=\))|(?<=(?<wrap>[=~|_#]))\b(https?://|www\.)[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|](?=\k<wrap>)|\b(https?://|www\.)[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|]"))
                                    continue;
                                messageBuilder.AppendLine(str);
                                urlFound = true;
                            }
                    }
                }

                if (!urlFound)
                    messageBuilder.AppendLine("No URLs found");

                embedBuilder.AddField("URLFound: ", "```" + messageBuilder + "```");
            }

            builder.AddEmbed(embedBuilder.Build());
        }
        catch (BadImageFormatException e)
        {
            var messageBuilder = new StringBuilder();

            messageBuilder.AppendLine(e.ToString().Contains(".NET data directory RVA is 0")
                ? "The application is not made with .NET or is obfuscated"
                : "Error");

            builder.WithContent(messageBuilder.ToString());
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
    }

    private static bool IsFileSafe(IReadOnlyList<byte> fileBytes)
    {
        var exeSignature = "MZ"u8.ToArray();
        return fileBytes.Count >= 2 && fileBytes[0] == exeSignature[0] && fileBytes[1] == exeSignature[1];
    }
}

public abstract class Program
{
    private static DiscordClient? _discord;

    public static async Task Main()
    {
        _discord = new DiscordClient(new DiscordConfiguration
        {
            Token = "ODAzMzU5Mjk4NzgwNTI4Njkx.GSNHDb.DMWzmn89exFcTzcYkA40q7pFjKERw39a_IBx-E",
            TokenType = TokenType.Bot
        });
        var slash = _discord.UseSlashCommands();
        await slash.RefreshCommands();
        slash.RegisterCommands(typeof(UrlFinderCommand).GetTypeInfo().Assembly);
        await _discord.ConnectAsync();
        await Task.Delay(-1);
    }
}
