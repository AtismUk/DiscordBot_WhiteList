using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot_WhiteList.Handler;
using DiscordBot_WhiteList.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using System;

namespace DiscordBot_WhiteList
{
    internal class Program
    {

        private static DiscordSocketClient _client;
        private InteractionService _commands;
        static async Task Main(string[] args) => await new Program().RunAsync();

        async Task RunAsync()
        {
            using (var service = ConfigureService())
            {
                _client = service.GetRequiredService<DiscordSocketClient>();
                _commands = service.GetRequiredService<InteractionService>();

                _client.Log += Log;
                _client.Ready += async () =>
                {
                    await _commands.RegisterCommandsGloballyAsync();
                };

                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("AppSettings.json")
                    .Build();

                await _client.LoginAsync(TokenType.Bot, configuration["Discord:Token"]);
                await _client.StartAsync();

                await service.GetRequiredService<CommandHandler>().InitializeAsync();

                await Task.Delay(-1);
            }
        }

        private ServiceProvider ConfigureService()
        {

            return new ServiceCollection()
                .AddSingleton<BotService>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .AddScoped<BotService>()
                .BuildServiceProvider();
        }

        static private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}