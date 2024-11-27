using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WorksPad.Assistant.Bot.Protocol.BotServer;

namespace SearchPRBot.Lib.AdaptiveCard
{
    public class AdaptiveCardHelper
    {
        public static Attachment CreateUpdateAttachment(string template_name, JObject dataContent = null)
        {
            var weekTemplateFilePath = Path.Combine("Lib/AdaptiveCard/Templates", template_name);
            var template = File.ReadAllText(weekTemplateFilePath);
            var cardContentAsString = template.ToString();
            JObject cardContent = new JObject();
            switch (template_name)
            {
                case "ProcessingTemplate.json": 
                    cardContent = CreateProcessingAttachment(cardContentAsString);
                    break;
                case "SearchProjectRegistration.json": 
                    cardContent = CreateFoundRegistrationAttachment(cardContentAsString, dataContent);
                    break;
                case "RegistrationInfoTemplate.json":
                    cardContent = CreateRegInfoAttachment(cardContentAsString, dataContent);
                    break;
                default:
                    //Create Template for "template not found"
                    break;
            }
            
            return Attachment.CreateAdaptiveCardAttachment(cardContent);
        }

        private static JObject CreateRegInfoAttachment(string cardContentAsString, JObject dataContent)
        {
            JObject cardContent;
            cardContentAsString = cardContentAsString.Replace("{Contragent}", dataContent["RegInfo"]["CompanyName"].ToString());
            cardContentAsString = cardContentAsString.Replace("{Status}", dataContent["RegInfo"]["Status"].ToString());
            cardContentAsString = cardContentAsString.Replace("{Chance}", dataContent["RegInfo"]["Chance"].ToString());
            cardContentAsString = cardContentAsString.Replace("{Sum}", dataContent["RegInfo"]["Sum"].ToString());
            cardContentAsString = cardContentAsString.Replace("{Manager}", dataContent["RegInfo"]["Manager"].ToString());
            cardContentAsString = cardContentAsString.Replace("{PayDay}", dataContent["RegInfo"]["PayDay"].ToString());
            cardContentAsString = cardContentAsString.Replace("{RegNumber}", dataContent["RegInfo"]["RegNumber"].ToString());
            cardContentAsString = cardContentAsString.Replace("{CompanyName}", dataContent["RegInfo"]["CompanyName"].ToString());
            cardContentAsString = cardContentAsString.Replace("{Svetofor}", dataContent["RegInfo"]["Svetofor"].ToString());

            cardContent = JObject.Parse(cardContentAsString);
            JArray regproducts = new JArray();
            foreach (var regproduct in dataContent["RegInfo"]["Products"])
            {
                JObject regproductContainer = new JObject();
                regproductContainer["type"] = "Container";
                JArray items = new JArray();
                JObject block = new JObject();
                block["type"] = "TextBlock";
                block["wrap"] = true;
                block["text"] = regproduct;
                items.Add(block);
                regproductContainer["items"] = items;
                regproducts.Add(regproductContainer);
            }
            cardContent["body"][4]["columns"][1]["items"] = regproducts;
            return cardContent;
        }

        private static JObject CreateFoundRegistrationAttachment(string cardContentAsString, JObject dataContent)
        {
            JObject cardContent;
            if (dataContent != null)
            {
                cardContent = JObject.Parse(cardContentAsString);
                if (dataContent["InputCompany"] != null )
                {
                    cardContent["body"][1]["columns"][1]["items"][0]["value"] = dataContent["InputCompany"].ToString();
                }
                if(dataContent["Warning"] != null)
                {
                    cardContent["body"][2]["items"][0]["text"] = dataContent["Warning"]?.ToString();
                    cardContent["body"][2]["isVisible"] = true;
                }
                if (dataContent["FoundCompanies"] != null)
                {
                    cardContent["body"][3]["columns"][0]["items"][0]["isVisible"] = true;
                    cardContent["body"][3]["columns"][1]["items"][0]["text"] = $"Найдено организаций: {dataContent["FoundCompanies"].Count()}";
                    cardContent["body"][3]["columns"][1]["items"][0]["isVisible"] = true;
                    cardContent["body"][3]["columns"][1]["items"][1]["isVisible"] = true;
                    cardContent["body"][3]["columns"][1]["items"][1]["choices"] = dataContent["FoundCompanies"];
                    if (dataContent["FoundCompanies"].Count() == 1) {cardContent["body"][3]["columns"][1]["items"][1]["value"] = dataContent["FoundCompanies"][0]["value"];}
                    cardContent["body"][3]["columns"][1]["items"][2]["isVisible"] = true;
                    cardContent["body"][3]["columns"][1]["items"][2]["actions"][0]["data"]["FoundCompanies"] = dataContent["FoundCompanies"];
                    cardContent["body"][4]["items"][0]["text"] = "Внимание! Поиск регистрации потребует времени";
                    cardContent["body"][4]["isVisible"] = true;
                }
                if (dataContent["ChoiseSetCompanies"] != null)
                {
                    cardContent["body"][3]["columns"][1]["items"][1]["value"] = dataContent["ChoiseSetCompanies"].ToString();
                }
                if (dataContent["ChoiseSetCompanies"] != null)
                {
                    cardContent["body"][4]["isVisible"] = false;
                    cardContent["body"][5]["columns"][0]["items"][0]["isVisible"] = true;
                    cardContent["body"][5]["columns"][1]["items"][0]["isVisible"] = true;
                    JToken foundNameCompany = dataContent["FoundCompanies"].FirstOrDefault(obj => (string)obj["value"] == dataContent["ChoiseSetCompanies"]!.ToString());
                    if (dataContent["FoundRegistrations"].Count() == 0)
                    {
                        cardContent["body"][5]["columns"][1]["items"][0]["text"] = $"Регистраций для {foundNameCompany["title"]} не найдено.";
                    }
                    else
                    {
                        cardContent["body"][5]["columns"][1]["items"][0]["text"] = $"Найдено регистраций: {dataContent["FoundRegistrations"].Count()}";
                        cardContent["body"][5]["columns"][1]["items"][1]["isVisible"] = true;
                        var statusOrder = new Dictionary<string, int>
                        {
                            {"В работе", 1},
                            {"Смена партнера", 2},
                            {"Успешно завершен", 3},
                            {"Неуспешно завершен", 4}
                        };
                        var sortedJArray = new JArray(dataContent["FoundRegistrations"].OrderBy(obj => statusOrder[obj["status"].ToString()]));

                        cardContent["body"][5]["columns"][1]["items"][1]["choices"] = sortedJArray;
                        cardContent["body"][5]["columns"][1]["items"][1]["value"] = sortedJArray[0]["value"];
                        cardContent["body"][5]["columns"][1]["items"][2]["isVisible"] = true;
                        cardContent["body"][5]["columns"][1]["items"][2]["actions"][0]["data"]["FoundRegistrations"] = dataContent["FoundRegistrations"];
                    }
                }
                if (dataContent["ChoiseSetRegistrations"] != null)
                {
                    cardContent["body"][6]["value"] = dataContent["ChoiseSetRegistrations"].ToString();
                }
            }
            else {cardContent = JObject.Parse(cardContentAsString);}
            return cardContent;
        }

        private static JObject CreateProcessingAttachment(string cardContentAsString)
        {
            var cardContent = JObject.Parse(cardContentAsString);
            return cardContent;
        }

        internal static Attachment CreateFailureAttachment(Exception exc)
        {
            var weekTemplateFilePath = Path.Combine("Lib/AdaptiveCard/Templates", "FailureTemplate.json");
            var template = File.ReadAllText(weekTemplateFilePath);
            var cardContentAsString = template.ToString();
            string title = (exc.Message.Contains("Электронная услуга не может быть применена")) ? "Ограничение" : "Ошибка";
            cardContentAsString = cardContentAsString.Replace("{ExceptionTitle}", title);
            cardContentAsString = cardContentAsString.Replace("{ExceptionMessage}", exc.Message);
            var cardContent = JObject.Parse(cardContentAsString);
            return Attachment.CreateAdaptiveCardAttachment(cardContent);
        }
    }
}
