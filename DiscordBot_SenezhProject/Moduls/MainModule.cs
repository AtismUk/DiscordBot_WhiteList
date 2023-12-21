using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot_WhiteList.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_WhiteList.Moduls
{
    public class MainModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly BotService _service;

        public MainModule(BotService botService)
        {
            _service = botService;
        }


        [SlashCommand("whitelist", "Registration in project")]
        public async Task RegistrationCommand()
        {
            await RespondWithModalAsync<RegistModal>("registration_modal");
        }

        [SlashCommand("config", "Settings of bot")]
        public async Task ConfigCommand(IRole new_role, string path_of_whitelist)
        {
            if (Context.User is SocketGuildUser userGuild)
            {
                if (userGuild.Roles.Any(x => x.Permissions.BanMembers))
                {
                    StaticData.newRole = new_role.Id;
                    StaticData.pathOfWhiteList = path_of_whitelist;
                    StaticData.isConfigured = true;
                    await RespondAsync("Данные сохранены");
                }

            }

        }

        [ModalInteraction("registration_modal")]
        public async Task HandlerRegistModal(RegistModal modal)
        {


            var embedBuilder = CreateFormEmdeb("Заявка на регистрацию", Color.LightOrange, modal.Nick, modal.steamId, modal.web);

            var acceptButton = new ButtonBuilder()
            {
                Label = "Принять",
                CustomId = "accept_button",
                Style = ButtonStyle.Success,
            };

            var cancelButton = new ButtonBuilder()
            {
                Label = "Отклонить",
                CustomId = "cancel_button",
                Style = ButtonStyle.Danger
            };

            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton(acceptButton);
            componentBuilder.WithButton(cancelButton);

            await RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
        }


        [ComponentInteraction("accept_button")]
        public async Task HandlerButtonAccept()
        {
            if (Context.User is SocketGuildUser userGuild)
            {
                if (userGuild.Roles.Any(x => x.Permissions.BanMembers))
                {
                    var interaction = (IComponentInteraction)Context.Interaction;
                    var message = interaction.Message;

                    var embed = message.Embeds.First();
                    var nameField = embed.Fields.First(x => x.Name == "Позывной").Value;
                    var steamId = embed.Fields.First(x => x.Name == "Steam Id").Value;
                    var steamWeb = embed.Fields.First(x => x.Name == "Ссылка на Steam").Value;

                    var embedBuilder = CreateFormEmdeb("Принят", Color.Green, nameField, steamId, steamWeb);

                    var componentBuilder = new ComponentBuilder();

                    var role = Context.Guild.Roles.FirstOrDefault(x => x.Id == StaticData.newRole);
                    if (role != null)
                    {
                        var user = (IGuildUser)Context.User;
                        await user.AddRoleAsync(role);
                    }

                    var res = await _service.AddWhiteListAsync(steamId);

                    if (!res.IsValid)
                    {
                        await Context.User.SendMessageAsync(res.Message);
                    }
                    else
                    {
                        await message.ModifyAsync(props =>
                        {
                            props.Embed = embedBuilder.Build();
                            props.Components = componentBuilder.Build();
                        });
                        if (res.Message != null)
                        {
                            await Context.User.SendMessageAsync(res.Message);
                        }
                        IGuildUser guildUser = (IGuildUser)Context.User;
                        await guildUser.ModifyAsync(props =>
                        {
                            props.Nickname = nameField;
                        });
                    }
                }
            }
        }

        [ComponentInteraction("cancel_button")]
        public async Task HandlerButtonCancel()
        {
            if (Context.User is SocketGuildUser userGuild)
            {
                if (userGuild.Roles.Any(x => x.Permissions.BanMembers))
                {
                    await RespondWithModalAsync<CancelModal>("cancel_modal");
                }
            }
        }

        [ModalInteraction("cancel_modal")]
        public async Task HandlerCancelModal(CancelModal modal)
        {
            var interaction = (IModalInteraction)Context.Interaction;
            var message = interaction.Message;

            var embed = message.Embeds.First();
            var nameField = embed.Fields.First(x => x.Name == "Позывной").Value;
            var steamId = embed.Fields.First(x => x.Name == "Steam Id").Value;
            var steamWeb = embed.Fields.First(x => x.Name == "Ссылка на Steam").Value;

            var embedBuilder = CreateFormEmdeb("Отклонено", Color.Red, nameField, steamId, steamWeb, modal.Text);
            var componentBuilder = new ComponentBuilder();

            await message.ModifyAsync(props =>
            {
                props.Embed = embedBuilder.Build();
                props.Components = componentBuilder.Build();
            });

            await RespondAsync();
        }

        private EmbedBuilder CreateFormEmdeb(string titel, Color color, string name, string steamId, string steamWeb, string errorMessage = null)
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = titel,
            }
           .AddField("Позывной", name)
           .AddField("Steam Id", steamId)
           .AddField("Ссылка на Steam", steamWeb)
           .WithColor(color)
           .WithCurrentTimestamp();

            if (errorMessage != null)
            {
                embedBuilder.AddField("Причина", errorMessage);
            }

            return embedBuilder;
        }
    }



    public class CancelModal : IModal
    {
        public string Title => "Причина отмены";
        [InputLabel("Причина")]
        [ModalTextInput("text_input", Discord.TextInputStyle.Short, placeholder: "причина", maxLength: 70)]
        public string Text { get; set; }

    }


    public class RegistModal : IModal
    {
        public string Title => "Форма регистрации";

        [InputLabel("Позывной")]
        [ModalTextInput("nick_input", Discord.TextInputStyle.Short, placeholder: "Позывной", maxLength: 70)]
        public string Nick { get; set; }

        [InputLabel("Steam Id")]
        [ModalTextInput("steamid_input", Discord.TextInputStyle.Short, placeholder: "Steam Id", maxLength: 70)]
        public string steamId { get; set; }

        [InputLabel("Ссылка на профиль")]
        [ModalTextInput("web_input", Discord.TextInputStyle.Short, placeholder: "Ссылка", maxLength: 70)]
        public string web { get; set; }
    }
}
