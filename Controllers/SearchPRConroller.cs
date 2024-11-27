using Microsoft.AspNetCore.Mvc;
using WorksPad.Assistant.Bot;
using System.Net;

namespace TestBot.Controllers;

[Route("searchprojregbot")]

public class TestBotController : ControllerBase
{
    private readonly ChatBotCommunicator _communicator;

    public TestBotController(ChatBotCommunicator communicator)
    {
        _communicator = communicator;
    }

    [HttpPost]
    public async Task PostAsync()
    {
        await _communicator.HandleApiRequestAsync(HttpContext);
    }
    [HttpGet]
    public IActionResult GetAsync()
    {
        return Ok(new {Max = 10});
    }
}
