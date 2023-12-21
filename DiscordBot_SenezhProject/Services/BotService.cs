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
        public async Task<ResponseService> AddWhiteListAsync(string steamId)
        {
            try
            {
                bool isNewSteamId = true;
                string guid = GenerateGuidBySteamId(steamId);
                StreamReader sr = new(StaticData.pathOfWhiteList.Replace(" ", ""));

                var lines = await sr.ReadLineAsync();
                if (lines != null  && lines.Contains(guid))
                {
                    isNewSteamId = false;
                }
                
                sr.Close();

                if (isNewSteamId)
                {
                    StreamWriter sw = new(StaticData.pathOfWhiteList, true);

                    await sw.WriteLineAsync(guid + "\n");

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
                    Message = "Ошибка, не могу прочитать или записать в whitelist данные, пример пути - C:/(path)/WhiteList.txt"
                };
            }
        }

        private string GenerateGuidBySteamId(string steamId)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(steamId);
            string guid = "";

            using(MD5 mD5 = MD5.Create())
            {
                byte[] hashBytes = mD5.ComputeHash(byteArray);

                guid = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
            }

            return guid;
        }
    }
}
