using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

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

            using (var client = new WebClient())
            {
                var fileBytes = await client.DownloadDataTaskAsync(exeFile.Url);

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
                                if (Regex.IsMatch(str,
                                        @"(?<=\()\b(https?://|www\.)[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|](?=\))|(?<=(?<wrap>[=~|_#]))\b(https?://|www\.)[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|](?=\k<wrap>)|\b(https?://|www\.)[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|]"))
                                {
                                    messageBuilder.AppendLine(str);
                                    urlFound = true;
                                }
                            }
                    }
                }

                if (!urlFound)
                    messageBuilder.AppendLine("No URLs found");

                embedBuilder.AddField("URLFound: ", "```" + messageBuilder.ToString() + "```");
            }

            builder.AddEmbed(embedBuilder.Build());
        }
        catch (BadImageFormatException e)
        {
            var messageBuilder = new StringBuilder();

            if (e.ToString().Contains(".NET data directory RVA is 0"))
                messageBuilder.AppendLine("The application is not made with .NET or is obfuscated");
            else
                messageBuilder.AppendLine("Error");

            builder.WithContent(messageBuilder.ToString());
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
    }

    private bool IsFileSafe(byte[] fileBytes)
    {
        byte[] exeSignature = { 0x4D, 0x5A };
        return fileBytes.Length >= 2 && fileBytes[0] == exeSignature[0] && fileBytes[1] == exeSignature[1];
    }
}

public class Program
{
    public static DiscordClient discord;

    public static async Task Main(string[] args)
    {
        discord = new DiscordClient(new DiscordConfiguration
        {
            Token = "Your Token Here",
            TokenType = TokenType.Bot
        });
        var slash = discord.UseSlashCommands();
        await slash.RefreshCommands();
        slash.RegisterCommands(typeof(UrlFinderCommand).GetTypeInfo().Assembly);
        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}