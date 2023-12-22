using Discord;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_WhiteList.Services
{
    public class BotService
    {
        public async Task<ResponseService> AddWhiteListAsync(string steamId, string nick, ulong userId)
        {
            try
            {
                bool isNewSteamId = true;
                string guid = GenerateGuidBySteamId(ulong.Parse(steamId.Replace(" ", "")));
                StreamReader sr = new(StaticData.pathOfWhiteList.Replace(" ", ""));

                var lines = await sr.ReadLineAsync();
                if (lines != null && lines.Contains(guid))
                {
                    isNewSteamId = false;
                }

                sr.Close();

                if (isNewSteamId)
                {
                    StreamWriter sw = new(StaticData.pathOfWhiteList, true);

                    await sw.WriteLineAsync(guid + " " + $"({nick} | {userId})" + "\n");

                    sw.Close();

                    return new()
                    {
                        IsValid = true
                    };
                }

                return new()
                {
                    IsValid = true,
                    Message = "SteamId, который был отправлен уже был записан в whiteList"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Message = "Ошибка, не могу прочитать или записать в whitelist данные, пример пути - C:/(path)/WhiteList.txt\nИли SteamId не является строкой"
                };
            }
        }


        private string GenerateGuidBySteamId(ulong steamId)
        {


            byte[] temp = new byte[8];

            for (int i = 0; i < 8; i++)
            {
                temp[i] = (byte)(steamId & 0xFF);
                steamId >>= 8;
            }


            byte[] data = new byte[temp.Length + 2];
            data[0] = (byte)'B';
            data[1] = (byte)'E';
            Array.Copy(temp, 0, data, 2, temp.Length);

            MD5 md5 = MD5.Create();

            byte[] hash = md5.ComputeHash(data);

            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
