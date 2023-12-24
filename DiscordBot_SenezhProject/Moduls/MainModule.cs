using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot_WhiteList.Models;
using DiscordBot_WhiteList.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

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
        public async Task ConfigCommand(IRole new_role, IChannel admin_channel, string path_of_whitelist)
        {
            if (Context.User is SocketGuildUser userGuild)
            {
                if (userGuild.Roles.Any(x => x.Permissions.BanMembers))
                {
                    StaticData.newRole = new_role.Id;
                    StaticData.pathOfWhiteList = path_of_whitelist;
                    StaticData.channelId = admin_channel.Id;
                    await RespondAsync("Данные сохранены");
                }

            }

        }

        [ModalInteraction("registration_modal")]
        public async Task HandlerRegistModal(RegistModal modal)
        {

            Dictionary<string, string> fields = new();
            fields.Add("Пользователь", Context.User.Mention);
            fields.Add("Позывной", modal.Nick);
            fields.Add("Steam Id", modal.steamId);
            fields.Add("Ссылка на профиль Steam", modal.web);
            var embed = CreateEmbed(new("Заявка на регистрацию", fields, Color.LightOrange, new EmbedAuthorBuilder()
            {
                Name = Context.User.GlobalName,
                IconUrl = Context.User.GetAvatarUrl()
            }));


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

            await RespondAsync(embed: embed.Build(), components: componentBuilder.Build());
        }


        [ComponentInteraction("accept_button")]
        public async Task HandlerButtonAccept()
        {
            if (Context.User is SocketGuildUser userGuild)
            {
                if (userGuild.Roles.Any(x => x.Permissions.BanMembers) || userGuild.Roles.Any(x => x.Id == StaticData.requiredRoleId))
                {
                    var interaction = (IComponentInteraction)Context.Interaction;
                    var message = interaction.Message;

                    var embed = message.Embeds.First();
                    var nameField = embed.Fields.First(x => x.Name == "Позывной").Value;
                    var steamId = embed.Fields.First(x => x.Name == "Steam Id").Value;
                    var steamWeb = embed.Fields.First(x => x.Name == "Ссылка на профиль Steam").Value;
                    var user = embed.Fields.First(x => x.Name == "Пользователь").Value;


                    Dictionary<string, string> fields = new();
                    fields.Add("Пользователь", user);
                    fields.Add("Позывной", nameField);
                    fields.Add("Steam Id", steamId);
                    fields.Add("Ссылка на профиль Steam", steamWeb);
                    var embedBuilder = CreateEmbed(new("Одобрено", fields, Color.Green, new EmbedAuthorBuilder()
                    {
                        Name = message.Author.GlobalName,
                        IconUrl = message.Author.GetAvatarUrl(),
                    }));

                    var componentBuilder = new ComponentBuilder();

                    var role = Context.Guild.Roles.FirstOrDefault(x => x.Id == StaticData.newRole);
                    if (role != null && !userGuild.Roles.Any(x => x.Id == StaticData.newRole))
                    {
                        var userGuid = (IGuildUser)Context.User;
                        await userGuid.AddRoleAsync(role);
                    }

                    ulong id = 0;
                    ulong.TryParse(user.Replace("@", "").Replace("<", "").Replace(">", ""), out id);
                    var res = await _service.AddWhiteListAsync(steamId, nameField, id);

                    if (!res.IsValid)
                    {
                        await Context.User.SendMessageAsync(res.Message);
                    }
                    else
                    {

                        var channel = Context.Guild.GetTextChannel(StaticData.channelId);
                        if (channel != null)
                        {
                            var embedNotify = NotifyEmbed(steamId, user, Context.Interaction.User.Mention);
                            await channel.SendMessageAsync(embed: embedNotify.Build(), messageReference: message.Reference);
                        }
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
            var steamWeb = embed.Fields.First(x => x.Name == "Ссылка на профиль Steam").Value;
            var userMentionId = embed.Fields.First(x => x.Name == "Пользователь").Value;

            Dictionary<string, string> fields = new();
            fields.Add("Пользователь", userMentionId);
            fields.Add("Позывной", nameField);
            fields.Add("Steam Id", steamId);
            fields.Add("Ссылка на профиль Steam", steamWeb);
            fields.Add("Причина", modal.Text);
            var embedBuild = CreateEmbed(new("Отклонено", fields, Color.Red, new EmbedAuthorBuilder()
            {
                Name = message.Author.GlobalName,
                IconUrl = message.Author.GetAvatarUrl()
            }));
            var componentBuilder = new ComponentBuilder();

            await message.ModifyAsync(props =>
            {
                props.Embed = embedBuild.Build();
                props.Components = componentBuilder.Build();
            });


            var user = Context.Guild.Users.FirstOrDefault(x => x.Id == ulong.Parse(userMentionId.Replace("@", "").Replace("<", "").Replace(">", "")));
            if (user != null)
            {
                await user.SendMessageAsync("Ваша заявка отклонена, исправьте замечание и отправьте заявку снова " + message.GetJumpUrl());
            }

            var channel = Context.Guild.GetTextChannel(StaticData.channelId);
            if (channel != null)
            {
                var embedNotify = NotifyEmbed(steamId, userMentionId, Context.Interaction.User.Mention, modal.Text);
                await channel.SendMessageAsync(embed: embedNotify.Build(), messageReference: message.Reference);
            }

            await RespondAsync();
        }

        private EmbedBuilder CreateEmbed(CreatorEmbedModel creatorEmbed)
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = creatorEmbed.Titel,
            }
            .WithColor(creatorEmbed.Color)
            .WithCurrentTimestamp();
            if (creatorEmbed.Author != null)
            {
                embedBuilder.Author = creatorEmbed.Author;
            }

            foreach (var field in creatorEmbed.Fields)
            {
                embedBuilder.AddField(field.Key, field.Value);
            }

            return embedBuilder;

        }

        private EmbedBuilder NotifyEmbed(string steamId, string userMentionId, string moderMentionId, string reason = null)
        {
            string text = reason != null ? "Заявка отклонена" : "Заявка принята";
            var embedBuilder = new EmbedBuilder()
            {
                Title = "Уведомление о заявки\n" +
                text
            }
            .AddField("Проверяющий", moderMentionId)
            .AddField("Пользователь", userMentionId)
            .AddField("Steam Id", steamId)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();
            if (reason != null)
            {
                embedBuilder.WithColor(Color.Red)
                .AddField("Причина", reason);
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
