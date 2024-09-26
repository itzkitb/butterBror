using butterBror.Utils;
using TwitchLib.Client.Events;

namespace butterBror
{
    public partial class Commands
    {
        static void pizza(OnChatCommandReceivedArgs e, string lang)
        {
            if (CommandUtil.IsNotOnCooldown(5, 1, "Pizza", e.Command.ChatMessage.UserId, e.Command.ChatMessage.RoomId))
            {
                string[] peepoHuyUsers = { "763415747" };
                string[] buy = { "buy", "купить", "bought", "b", "к" };
                string[] pizzas = { TranslationManager.GetTranslation(lang, "pizzaNapoletana", ""), TranslationManager.GetTranslation(lang, "pizzaCalzone", ""), TranslationManager.GetTranslation(lang, "pizzaRomana", ""), TranslationManager.GetTranslation(lang, "pizzaPepperoni", ""), TranslationManager.GetTranslation(lang, "pizzaMeat", ""), TranslationManager.GetTranslation(lang, "pizzaVillage", ""), TranslationManager.GetTranslation(lang, "pizzaCheese", ""), TranslationManager.GetTranslation(lang, "pizzaMargarita", ""), TranslationManager.GetTranslation(lang, "pizzaHawaiian", ""), TranslationManager.GetTranslation(lang, "pizzaAssorted", ""), TranslationManager.GetTranslation(lang, "pizzaMarine", "") };
                int[] pizzaCosts = { 10, 15, 10, 20, 25, 15, 15, 10, 20, 25, 15 };
                if (e.Command.ArgumentsAsList.Count < 1)
                {
                    string pizzasAsString = "";
                    for (int i = 0; i < pizzas.Count(); i++)
                    {
                        pizzasAsString += pizzas[i] + $" ({pizzaCosts[i]} coins), ";
                    }
                    if (peepoHuyUsers.Contains(e.Command.ChatMessage.UserId))
                    {
                        ChatUtil.SendMsgReply(e.Command.ChatMessage.Channel, e.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "huicaMenu", "").Replace("%menu%", (pizzasAsString + "\n").Replace(", \n", "")), e.Command.ChatMessage.Id, lang, true);
                    }
                    else
                    {
                        ChatUtil.SendMsgReply(e.Command.ChatMessage.Channel, e.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "pizzaMenu", "").Replace("%menu%", (pizzasAsString + "\n").Replace(", \n", "")), e.Command.ChatMessage.Id, lang, true);
                    }
                }
                else if (e.Command.ArgumentsAsList.Count >= 2)
                {
                    if (buy.Contains(e.Command.ArgumentsAsList.ElementAt(1).ToLower()))
                    {

                    }
                }
            }
        }
    }
}
