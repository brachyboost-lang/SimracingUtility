using Microsoft.AspNetCore.Mvc;

namespace SimracingUtility.Controllers
{
    // Stellt den lokalen LMU-Agent (separates Projekt) zum Download bereit.
    // Die ausgelieferte Datei wird vom Publish-Skript des Agents nach
    // wwwroot/downloads/ gelegt (siehe LMU_Agent/publish.ps1).
    public class DownloadController : Controller
    {
        private const string AgentFileName = "LMU.Agent.Service.zip";

        private readonly IWebHostEnvironment _env;

        public DownloadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            ViewBag.AgentAvailable = System.IO.File.Exists(GetAgentPath());
            return View();
        }

        public IActionResult Agent()
        {
            var path = GetAgentPath();
            if (!System.IO.File.Exists(path))
            {
                TempData["DownloadMessage"] =
                    "Der LMU-Agent steht aktuell nicht zum Download bereit. " +
                    "Bitte das Artefakt mit LMU_Agent/publish.ps1 erzeugen.";
                return RedirectToAction(nameof(Index));
            }

            var stream = System.IO.File.OpenRead(path);
            return File(stream, "application/zip", AgentFileName);
        }

        private string GetAgentPath()
            => Path.Combine(_env.WebRootPath, "downloads", AgentFileName);
    }
}
