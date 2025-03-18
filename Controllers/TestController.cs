using Microsoft.AspNetCore.Mvc;

[Route("api/test")]
[ApiController]
public class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("🏓 Pong! L'API fonctionne.");
    }

    [HttpGet("data")]
    public IActionResult GetTestData()
    {
        var testData = new
        {
            Message = "API is working",
            Timestamp = DateTime.Now
        };
        return Ok(testData);
    }
}