# UrlFinderBot

UrlFinderBot is a Discord bot that finds URLs in a .NET module.

## Screenshot
![image](https://github.com/qzxtu/UrlFinderBot/assets/69091361/4b232d60-dba0-4c03-b2d8-0f24468bcfcb)

## Description

This Discord bot allows you to upload a .exe file and it will scan the file to find URLs within the .NET module. It uses the dnlib library to analyze the module and extract URLs from strings. The bot provides a response with the URLs found, if any. Please note that the bot includes safety checks to ensure the provided file is safe to analyze.

## Prerequisites

- .NET Framework 4.7.2 or higher
- DSharpPlus NuGet package
- dnlib NuGet package

## Installation

1. Clone the repository.
2. Replace `"Your Token Here"` in the `Program.cs` file with your Discord bot token.
3. Build the project to restore NuGet packages and compile the code.
4. Run the bot.

## Usage

1. Invite the bot to your Discord server.
2. Use the `/urlfinder` command to initiate the URL finding process.
3. Provide a valid .exe file as an attachment when prompted by the bot.
4. The bot will scan the file and display any URLs found within the .NET module.

## Author

This project was created by [qzxtu](https://github.com/qzxtu).

## License

This project is licensed under the Apache License 2.0. See the [LICENSE](LICENSE) file for more information.
