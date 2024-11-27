using SearchPRBot.AspNetCore;
using WorksPad.Assistant.Bot;
using WorksPad.Assistant.Bot.Protocol.BotServer;
using SearchPRBot.Lib;
using SearchPRBot.Lib.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", 
        optional: false,
        reloadOnChange: true);
IConfiguration config = builder.Configuration;
ConfigurationParser cp = new ConfigurationParser(config);
MyChatBotConfiguration BotConfig = cp.Get<MyChatBotConfiguration>();
ServiceCollection serviceCollection = new ServiceCollection();
serviceCollection.AddLogging();
ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
//ILogger logger = serviceProvider.GetService<ILogger<ChatBot>>();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();
DefaultValues defValues = cp.Get<DefaultValues>();
APIConnector connector = new APIConnector(cp.Get<APIConfig>());
ChatBot chatBot = new ChatBot(connector, builder.Environment.IsDevelopment(), defValues);
HttpClientHandler httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
{
    return true;
};
ChatBotCommunicator chatBotCommunicator = new ChatBotCommunicator(BotConfig, chatBot, httpClientHandler);

builder.Services.AddSingleton(chatBotCommunicator);
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    var kestrelSection = context.Configuration.GetSection("Kestrel");
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.Lifetime.ApplicationStarted.Register(async () => await _ReactivateBotAsync(chatBotCommunicator, BotConfig.ChatBotUrl));
app.Lifetime.ApplicationStopping.Register(async () => await _DeactivateBotAsync(chatBotCommunicator));

async Task _DeactivateBotAsync(ChatBotCommunicator chatBotCommunicator)
{
    Log.Information("Bot deactivation has been requested. Creating new RequestDeactivateBotModel...");
    var requestDeactivateBotModel = new RequestDeactivateBotModel();
    Log.Information("RequestDeactivateBotModel created. Deactivation has been started...");
    await chatBotCommunicator.DeactivateBotAsync(requestDeactivateBotModel);
    Log.Information("Chatbot was deactivated...");
}

async Task _ReactivateBotAsync(ChatBotCommunicator chatBotCommunicator, string chatBotUrl)
{
    Log.Information("Bot reactivation has been requested");
    int chatBotOrderIndex = 0;
    Log.Information("Starting creation List of BotCommand");

    var chatBotCommandList = new List<RequestActivateBotModel.BotCommand>()
    {
        new RequestActivateBotModel.BotCommand(
            ChatBotCommand.project_registration_search,
            "Поиск регистрации проекта",
            "Команда позволяет осуществить поиск регистрации проекта",
            chatBotOrderIndex++)
    };
    
    Log.Information("List of BotCommand created. Creating new RequestActivateBotModel");
    var requestActivateBotModel = new RequestActivateBotModel(
                                        chatBotUrl,
                                        true,
                                        chatBotCommandList
                                        );
    Log.Information("RequestActivateBotModel was created. Starting async reactivation");
    await chatBotCommunicator.ReactivateBotAsync(requestActivateBotModel);
    Log.Information("Reactivation completed");
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
