using System;
using Newtonsoft.Json.Linq;
using WorksPad.Assistant.Bot;
using WorksPad.Assistant.Bot.Protocol;
using WorksPad.Assistant.Bot.Protocol.BotServer;
using WorksPad.Assistant.Bot.Protocol.ServerBot;
using WorksPad.Assistant.ExternalService.Http;
using SearchPRBot.Lib.AdaptiveCard;
using SearchPRBot.Lib.Configuration;
using System.Text.RegularExpressions;
using Serilog;

namespace SearchPRBot.Lib
{
    public class ChatBot : IServerRequestHandler
    {
        public JObject dataFind = new JObject();
        public APIConnector connector;
        private bool devEnv;
        private DefaultValues defValues;
        public ChatBot( APIConnector _connector, bool _devEnv, DefaultValues _defValues)
        {
            this.connector = _connector;
            this.devEnv = _devEnv;
            this.defValues = _defValues;
        }
        public static string ExecuteShortName(string str)
        {
            string pattern = @"(\bЛицензия\b\s+|\bСредства\b\s+разработки\b\s+|\bБессрочная\b\s+лицензия\b\s+|\bСертификат\b\s+)(.*?)(\bRuPost\b\s+\bStandard\b\s+\bCAL\b|\bRuPost\b\s+\bEnterprise\b\s+\bCAL\b|\bWorksPad\b\s+\bMDM\b\s+\bx1CAL\b|\bWorksPad\b\s+\bMDM\b\s+\bx1CAL\b\s+\bUpdate\b|\bWorksPad\b\s+\bMDM\b\s+\bx1DEV\b|\bWorksPad\b\s+\bMDM\b\s+\bx1DEV\b\s+\bUpdate\b|\bWorksPad\b\s+\bEMM\b|\bWorksPad\b\s+\bEMM\b\s+\bUpdate\b|\bWorksPad\b\s+\bCore\b\s+\bEMM\b|\bWorksPad\b\s+\bCore\b\s+\bEMM\b\s+\bUpdate\b|\bWorksPad\b|\bWorksPad\b\s+\bFile\b|\bWorksPad\b\s+\bCore\b|\bWorksPad\b\s+\bAssistant\b|\bWorksPad\b\s+\bUpdate\b|\bWorksPad\b\s+\bCore\b\s+\bUpdate\b|\bWorksPad\b\s+\bAssistant\b\s+\bUpdate\b|\bWorksPad\b\s+\bFile\b\s+\bUpdate\b|\bAstra\b\s+\bLinux\b\s+\bSpecial\b\s+\bEdition\b|\bAstra\b\s+\bLinux\b\s+\bCommon\b\s+\bEdition\b|\bAL-1701\b|\bAL-1702\b|\bAL-1703\b|\bAL-1704\b|\bAL-1705\b|\bAL-1707\b|\bAL-1722\b|\bAL-1724VR\b|\bAP-2101\b|\bBI-601\b|\bBR-301\b|\bBR-3201\b|\bDC-601\b|\bRB-201\b|\bRP-2001SE\b|\bVM-601\b|\bБрест\b|\bALD\b\s+\bPro\b|\bAstra\b\s+\bAutomation\b|\bAstra.Disk\b|\bNodus\b|\bACM\b)";
            Match match = Regex.Match(str, pattern);
            string match1 = match.Groups[1].Value + " " + match.Groups[3].Value;
            return match1;
        }
        public async Task<ResponseResult> ButtonClickedAsync(ChatBotCommunicator communicator, RequestButtonClickedModel requestModel, CancellationToken cancellationToken)
        {
            RequestUpdateMessageModel messageForClient;
            try
            {
                Log.Information("Button clicked");
                var dataContent = JObject.Parse(requestModel.CallbackData.ToString());
                var cmd = dataContent.GetValue("cmd")!.ToString();
                Log.Information($"data content: {dataContent}");
                Log.Information($"cmd: {cmd}");
                var attachment = AdaptiveCardHelper.CreateUpdateAttachment("ProcessingTemplate.json");
                messageForClient = new RequestUpdateMessageModel(
                    requestModel.Channel,
                    requestModel.Conversation.Id,
                    requestModel.MessageId,
                    MessageTextFormat.Plain,
                    text: null,
                    attachmentList: new List<Attachment> { attachment });
                switch (cmd)
                {
                    case "findCompanies":
                        messageForClient = await ProcessFindCompaniesRequest(requestModel, dataContent);
                        break;
                    case "findRegistrations":
                        messageForClient = await ProcessFindRegistrationRequest(requestModel, dataContent);
                        break;
                    case "ShowRegistrationInfo":
                        messageForClient = await ProcessShowRegistrationInfoRequest(requestModel, dataContent);
                        break;
                    case "StartNewSearch":
                        messageForClient = await ProcessNewSearchRequest(requestModel);
                        break;
                    case "BackPage":
                        messageForClient = await ProcessBackPageRequest(requestModel, dataFind);
                        break;
                    default:
                        messageForClient = new RequestUpdateMessageModel(
                            requestModel.Channel,
                            requestModel.Conversation.Id,
                            requestModel.MessageId,
                            MessageTextFormat.Plain,
                            "Не удалось обработать команду.");
                        break;
                }
            }
            catch
            {
                messageForClient = new RequestUpdateMessageModel(
                    requestModel.Channel,
                    requestModel.Conversation.Id,
                    requestModel.MessageId,
                    MessageTextFormat.Plain,
                    "Не удалось обработать команду.");
            }
            if (messageForClient != null)
            {
                if (requestModel.Channel == Channel.WorksPadAssistant)
                {
                    await communicator.UpdateMessageAsync(messageForClient, cancellationToken);
                }
                else
                {
                    await communicator.SendMessageAsync(new RequestSendMessageModel(
                        messageForClient.Channel,
                        messageForClient.ConversationId,
                        messageForClient.TextFormat,
                        messageForClient.Text,
                        messageForClient.ButtonList,
                        messageForClient.AttachmentLayout,
                        messageForClient.AttachmentList),
                        cancellationToken);
                }
            }
            return ResponseResult.Ok();
        }

        private async Task<RequestUpdateMessageModel> ProcessBackPageRequest(RequestButtonClickedModel requestModel, JObject dataContent)
        {
            Attachment attachment;
            attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);
            return new RequestUpdateMessageModel(
                            requestModel.Channel,
                            requestModel.Conversation.Id,
                            requestModel.MessageId,
                            MessageTextFormat.Plain,
                            text: null,
                            attachmentList: new List<Attachment> { attachment });
        }

        private async Task<RequestUpdateMessageModel> ProcessNewSearchRequest(RequestButtonClickedModel requestModel)
        {
            Attachment attachment;
            attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json");
            return new RequestUpdateMessageModel(
                            requestModel.Channel,
                            requestModel.Conversation.Id,
                            requestModel.MessageId,
                            MessageTextFormat.Plain,
                            text: null,
                            attachmentList: new List<Attachment> { attachment });
        }

        private async Task<RequestUpdateMessageModel> ProcessFindCompaniesRequest(RequestButtonClickedModel requestModel, JObject dataContent)
        {
            Attachment attachment;
            dataContent.Property("ChoiseSetCompanies")?.Remove();
            Log.Information($"Employee {requestModel.UserCredentials.Username} requested for company list");
            if (dataContent["InputCompany"] == null || String.IsNullOrEmpty(dataContent["InputCompany"]!.ToString().Trim()))
            {
                dataContent["Warning"] = "Указано пустое значение. Пожалуйста, проверьте что поле ввода наименования компании не пустое и повторите поиск.";
                attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);
            }
            else {
                string searchoption = "";
                dataContent["InputCompany"] = Regex.Replace(dataContent["InputCompany"]!.ToString().Trim(), @"\s+", " ");
                if (Regex.IsMatch(dataContent["InputCompany"].ToString(), @"^\d{5}|\d{10}|\d{12}$")) { searchoption = defValues.regInfoDefaults.companyINNIql;}
                else { searchoption = defValues.regInfoDefaults.companyNameIql;}
                Log.Information($"Employee {requestModel.UserCredentials.Username} input data is {dataContent["InputCompany"]!.ToString()}");
                dataContent.Property("ChoiseSetCompanies")?.Remove();
                dataContent.Property("ChoiseSetRegistrations")?.Remove();
                object queries = new {
                objectSchemaId = defValues.regInfoDefaults.companySchemaId,
                iql = string.Format(searchoption!, dataContent["InputCompany"]!.ToString()),
                resultPerPage = 5000
                };
                    var resp = await connector.GetData(defValues.regInfoDefaults.appendPath!,requestModel.UserCredentials, queries);
                    var items = ((IDictionary<String, Object>)resp)["objectEntries"];
                    var companies = (List<object>)items;

                if (companies.Count() == 0 || companies.Count() >= 50)
                {
                    Log.Information($"Employee {requestModel.UserCredentials.Username} input wrong data or receive too many result");
                    dataContent["Warning"] = "К сожалению, мы не смогли найти организации, соответствующие вашему запросу. Или по Вашему запросу найдено слишком много записей. Пожалуйста, проверьте правильность написания названия организации/ИНН или уточните и повторите поиск.";
                    attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);
                } 
                else 
                {
                    JArray jCompanies = new JArray();
                    foreach (dynamic company in companies)
                    {
                        JObject jCompany = new JObject();
                        string name = company.attributes[1].objectAttributeValues[0].value;
                        string id = company.objectKey;
                        string manager = "Менеджер не закреплен";
                        int attributesCount = company.attributes.Count;
                        for (int i=0; i < attributesCount; i++)
                        {
                            if (company.attributes[i].objectTypeAttributeId == 4131) {
                                manager = company.attributes[i].objectAttributeValues[0].displayValue;
                            }
                        }
                        jCompany.Add("title", name);
                        jCompany.Add("value", id);
                        jCompany.Add("manager", manager);
                        jCompanies.Add(jCompany);
                    }
                    JProperty prop = new JProperty("FoundCompanies", jCompanies);
                    dataContent.Add(prop);
                    attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);
                }
            }
            return new RequestUpdateMessageModel(
                            requestModel.Channel,
                            requestModel.Conversation.Id,
                            requestModel.MessageId,
                            MessageTextFormat.Plain,
                            text: null,
                            attachmentList: new List<Attachment> { attachment });

        }

        private async Task<RequestUpdateMessageModel> ProcessFindRegistrationRequest(RequestButtonClickedModel requestModel, JObject dataContent)
        {
            Attachment attachment;
            Log.Information("ProcessFindRegistrationRequest");
            if (dataContent["ChoiseSetCompanies"] == null || String.IsNullOrEmpty(dataContent["ChoiseSetCompanies"]!.ToString().Trim()))
            {
                dataContent["Warning"] = "Выберите компанию из списка";
                attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);
            }
            else {
                JToken foundNameCompany = dataContent["FoundCompanies"].FirstOrDefault(obj => (string)obj["value"] == dataContent["ChoiseSetCompanies"]!.ToString());
                Log.Information($"Employee {requestModel.UserCredentials.Username} input data is {foundNameCompany["title"]} to find registration list");
                object queries = new {
                objectSchemaId = defValues.regInfoDefaults.companySchemaId,
                iql = string.Format(defValues.regInfoDefaults.companyRegIql!, foundNameCompany["title"].ToString().Replace("\"", "")),
                resultPerPage = 5000
                };
                Log.Information($"Starting searching the registration list");
                dataContent.Property("ChoiseSetRegistrations")?.Remove();
                var resp = await connector.GetData(
                    defValues.regInfoDefaults.appendPath!,
                    requestModel.UserCredentials, queries);
                var items = ((IDictionary<String, Object>)resp)["objectEntries"];
                var companies = (List<object>)items;
                    object querieProducts = new {
                            objectSchemaId = defValues.regInfoDefaults.companySchemaId,
                            iql = string.Format(defValues.regInfoDefaults.productTypeId!, defValues.regInfoDefaults.productSchemaId),
                            resultPerPage = 5000
                            };
                            var respProducts = await connector.GetData(
                                    defValues.regInfoDefaults.appendPath!,
                                    requestModel.UserCredentials, querieProducts);
                            var itemProducts = ((IDictionary<String, Object>)respProducts)["objectEntries"];
                            var products = (List<object>)itemProducts;
                            JArray jProducts = new JArray();
                            foreach (dynamic article in products)
                            {
                                JObject jProduct = new JObject();
                                string labelProducts = article.attributes[4].objectAttributeValues[0].displayValue;
                                string idProducts = article.name;
                                jProduct.Add("title", labelProducts);
                                jProduct.Add("value", idProducts);
                                jProducts.Add(jProduct);
                            }
                            JProperty prod = new JProperty("FoundProducts", jProducts);
                            dataContent.Add(prod);

                    JArray jCompanies = new JArray();
                    foreach (dynamic company in companies)
                    {
                        JObject jCompany = new JObject();
                        string name = company.attributes[1].objectAttributeValues[0].value;
                        string id = company.objectKey;
                        JToken foundManager = dataContent["FoundCompanies"].FirstOrDefault(obj => (string)obj["value"] == dataContent["ChoiseSetCompanies"]!.ToString());
                        string manager = foundManager["manager"].ToString();
                        string status = company.attributes[6].objectAttributeValues[0].value;
                        JArray listProducts = new JArray();
                        JArray shortNameProducts = new JArray();
                        JArray fullNameProducts = new JArray();
                        listProducts.Add(company.attributes[9].objectAttributeValues[0].displayValue);
                        int i = 1;
                        bool errorOccurred = false;
                        while (!errorOccurred)
                        {
                            try
                            {
                                listProducts.Add(company.attributes[9].objectAttributeValues[i].displayValue);
                                i++;
                            }
                            catch (Exception ex)
                            {
                                errorOccurred = true;
                            }
                        }

                        if (listProducts.Count() == 1) {
                            JToken foundIdProduct = dataContent["FoundProducts"].FirstOrDefault(obj => (string)obj["value"] == listProducts[0].ToString());
                            if (foundIdProduct != null)
                            {
                                // Если элемент найден, извлекаем нужное значение
                                string labelProducts = (string)foundIdProduct["title"];
                            }
                            string shortname = ExecuteShortName(foundIdProduct["title"].ToString());
                            string fullname = foundIdProduct["title"].ToString();
                            shortNameProducts.Add(shortname);
                            fullNameProducts.Add(fullname);
                        }
                        else {
                            for (i=0; i < listProducts.Count(); i++)
                            {
                                JToken foundIdProduct = dataContent["FoundProducts"].FirstOrDefault(obj => (string)obj["value"] == listProducts[i].ToString());
                                if (foundIdProduct != null)
                                {
                                    // Если элемент найден, извлекаем нужное значение
                                    string labelProducts = (string)foundIdProduct["title"];
                                }
                                // Проверяем наличие дубликата
                                string shortname = ExecuteShortName(foundIdProduct["title"].ToString());
                                string fullname = foundIdProduct["title"].ToString();
                                bool containsElement = shortNameProducts.Any(item => item.ToString() == shortname);
                                // Если дубликата нет, добавляем его
                                if (!containsElement)
                                {
                                    shortNameProducts.Add(shortname);
                                }
                                fullNameProducts.Add(fullname);
                            }
                        }
                        string resultNameProducts = shortNameProducts.Select(item => item.ToString()).Aggregate((i, j) => i + ", " + j);
                        string resultListProducts = listProducts.Select(item => item.ToString()).Aggregate((i, j) => i + ", " + j);
                        string resultFullNameProducts = fullNameProducts.Select(item => item.ToString()).Aggregate((i, j) => i + "; " + j);
                        jCompany.Add("title", resultNameProducts + " " + "(" + status + ")" + " " + name);
                        jCompany.Add("value", id);
                        jCompany.Add("manager", manager);
                        jCompany.Add("status", status);
                        jCompany.Add("product", listProducts);
                        jCompany.Add("fullnameproduct", resultFullNameProducts);
                        jCompanies.Add(jCompany);
                    }
                    JProperty prop = new JProperty("FoundRegistrations", jCompanies);
                    dataContent.Add(prop);
                    attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);
                    //JObject dataFind = new JObject();
                    dataFind = dataContent;
            }
            Log.Information($"Task completed");
            return new RequestUpdateMessageModel(
                            requestModel.Channel,
                            requestModel.Conversation.Id,
                            requestModel.MessageId,
                            MessageTextFormat.Plain,
                            text: null,
                            attachmentList: new List<Attachment> { attachment });
        }

        private async Task<RequestUpdateMessageModel> ProcessShowRegistrationInfoRequest(RequestButtonClickedModel requestModel, JObject dataContent)
        {
            Attachment attachment;
            Log.Information("ShowRegistrationInfo");
            if (dataContent["ChoiseSetRegistrations"] == null)
            {
                dataContent["RegWarning"] = "Выберите регистрацию";
                attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json", dataContent);    
            }
            else 
            {
                Log.Information($"Employee {requestModel.UserCredentials.Username} requested registration info for {dataContent["ChoiseSetRegistrations"]!.ToString()}");
                object queries = new {
                    objectSchemaId = defValues.regInfoDefaults.companySchemaId,
                    iql = string.Format(defValues.regInfoDefaults.companyIdIql!, dataContent["ChoiseSetRegistrations"]!.ToString()),
                    resultPerPage = 5000
                    };
                var resp = await connector.GetData(
                    defValues.regInfoDefaults.appendPath!,
                    requestModel.UserCredentials, queries);
                var entries = ((IDictionary<string, object>)resp)["objectEntries"];
                var item = ((List<object>)entries)[0];
                string companyName = ((dynamic)item).attributes[5].objectAttributeValues[0].value;
                companyName = Regex.Replace(companyName, "\"", "");
                string payday = "Undefined";
                JToken foundManager = dataContent["FoundRegistrations"].FirstOrDefault(obj => (string)obj["value"] == dataContent["ChoiseSetRegistrations"]!.ToString());
                string manager = foundManager["manager"].ToString();
                try
                {
                    payday = ((dynamic)item).attributes[11].objectAttributeValues[0].value;
                }
                catch (Exception exc)
                {
                    Log.Information("Pay day is not set");
                }
                if (string.Equals(payday, "Undefined"))
                {
                    payday = "Дата оплаты не установлена";
                }

                string status = ((dynamic)item).attributes[6].objectAttributeValues[0].value;
                string chance = ((dynamic)item).attributes[8].objectAttributeValues[0].value;
                string sum = ((dynamic)item).attributes[10].objectAttributeValues[0].value;
                string registrationmumber = ((dynamic)item).attributes[1].objectAttributeValues[0].value;
                string svetofor = ((dynamic)item).attributes[7].objectAttributeValues[0].value;
                JObject jRegInfo = new JObject();
                jRegInfo["CompanyName"] = companyName;
                jRegInfo["Manager"] = manager;
                jRegInfo["PayDay"] = payday;
                jRegInfo["Status"] = status;
                jRegInfo["Chance"] = chance;
                jRegInfo["Sum"] = Regex.Replace(sum, @"\B(?=(\d{3})+(?!\d))", " ");
                jRegInfo["RegNumber"] = registrationmumber;
                jRegInfo["Svetofor"] = svetofor;
                JToken foundIdProduct = dataContent["FoundRegistrations"].FirstOrDefault(obj => (string)obj["value"] == dataContent["ChoiseSetRegistrations"]!.ToString());
                JArray jProducts = new JArray();
                    string[] arrProducts = foundIdProduct["fullnameproduct"].ToString().Split(';');
                    foreach (string product in arrProducts)
                    {
                        jProducts.Add(product);
                    }
                jRegInfo.Add("Products", jProducts);
                dataContent.Add("RegInfo", jRegInfo);
                attachment = AdaptiveCardHelper.CreateUpdateAttachment("RegistrationInfoTemplate.json", dataContent);
            }
            return new RequestUpdateMessageModel(
                            requestModel.Channel,
                            requestModel.Conversation.Id,
                            requestModel.MessageId,
                            MessageTextFormat.Plain,
                            text: null,
                            attachmentList: new List<Attachment> { attachment });
        }

        public async Task<ResponseResult> ConversationEndedAsync(ChatBotCommunicator communicator, RequestConversationEndedModel requestModel, CancellationToken cancellationToken)
        {
            Log.Information($"Conversation {requestModel.ConversationId} ended");
            return ResponseResult.Ok();
        }

        public Task<ResponseResult> ConversationStartedAsync(ChatBotCommunicator communicator, RequestConversationStartedModel requestModel, CancellationToken cancellationToken)
        {
            string members = String.Join(", ", requestModel.Members.Select(x => x.DisplayName).ToArray());
            Log.Information($"Start conversation. Conversation id - {requestModel.ConversationId}. Members are: {members}");
            return Task.FromResult(ResponseResult.Ok());
        }

        public Task<ResponseResult> DownloadAttachmentAsync(ChatBotCommunicator communicator, RequestDownloadAttachmentModel requestModel, CancellationToken cancellationToken)
        {
            return Task.FromResult(ResponseResult.Ok());
        }

        public Task<ResponseResult> MessagesDeliveredAsync(ChatBotCommunicator communicator, RequestMessagesDeliveredModel requestModel, CancellationToken cancellationToken)
        {
            Log.Information($"Message delivered to {requestModel.Sender}");
            return Task.FromResult(ResponseResult.Ok());
        }

        public Task<ResponseResult> MessageSeenAsync(ChatBotCommunicator communicator, RequestMessageSeenModel requestModel, CancellationToken cancellationToken)
        {
            Log.Information($"Conversation id - {requestModel.Conversation.Id}. Message id {requestModel.LastMessageId} seen by {requestModel.Conversation.Title},");
            return Task.FromResult(ResponseResult.Ok());
        }

        public async Task<ResponseResult> ReceiveMessageAsync(ChatBotCommunicator communicator, RequestReceiveMessageModel requestModel, IEnumerable<HttpFormData> attachmentFormDataList, CancellationToken cancellationToken)
        {
            Log.Information($"Conversation id - {requestModel.Conversation.Id}. Recieved message {requestModel.MessageId} from User - {requestModel.Sender.DisplayName}, User Id {requestModel.Sender.UserId}");
            var messageForClient = await TryProcessTextAsync(requestModel);
            await communicator.SendMessageAsync(messageForClient, cancellationToken);
            return ResponseResult.Ok();
        }

        private async Task<RequestSendMessageModel> TryProcessTextAsync(RequestReceiveMessageModel requestModel)
        {

            string? text = requestModel.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string? commandCode = null;
            string? textAfterCommand = null;

            if (!(requestModel.TryGetCommandCode(out commandCode, out textAfterCommand)))
            {
                return new RequestSendMessageModel(
                                    requestModel.Channel,
                                    requestModel.Conversation.Id,
                                    MessageTextFormat.Plain,
                                    "Автоматическое определение команды из контекста сообщения не предусмотрено.\nПожалуйста воспользуйтесь меню команд."

                );
            }
            else
            {
                Attachment attachment;
                switch (commandCode)
                {
                    case ChatBotCommand.project_registration_search:
                        attachment = AdaptiveCardHelper.CreateUpdateAttachment("SearchProjectRegistration.json");
                        return new RequestSendMessageModel(
                                    requestModel.Channel,
                                    requestModel.Conversation.Id,
                                    MessageTextFormat.Plain,
                                    text: null,
                                    attachmentList: new List<Attachment> { attachment });
                    default:
                    Log.Warning("Incoming cmd not listed in cmd menu");
                        return new RequestSendMessageModel(
                                    requestModel.Channel,
                                    requestModel.Conversation.Id,
                                    MessageTextFormat.Plain,
                                    "Не удалось обработать команду.");
                }
            }
        }

        public Task<ResponseResult> VerifyUserCredentialsAsync(ChatBotCommunicator communicator, RequestVerifyUserCredentialsModel requestModel, CancellationToken cancellationToken)
        {
            return Task.FromResult(ResponseResult.Ok());
        }
    }
}
