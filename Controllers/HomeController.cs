using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureCognitiveServicesDemo.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Rest;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace AzureCognitiveServicesDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IOptions<AzureCognitiveServicesConfig> _config;
        private readonly Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials _credentials;

        public HomeController(ILogger<HomeController> logger, IOptions<AzureCognitiveServicesConfig> config)
        {
            _logger = logger;
            _config = config;
            _credentials = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(_config.Value.Face.ApiKey);
        }

        public IActionResult Index()
        {
            return View(new HomeViewModel());
        }
        public IActionResult Sentiment()
        {
            return View("Index", new HomeViewModel());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// HTTP Post Action for when an image file is uploaded
        /// </summary>
        /// <param name="files">The files.</param>
        /// <returns></returns>
        [HttpPost("Index")]
        public async Task<IActionResult> Index(List<IFormFile> files)
        {

            var file = files.FirstOrDefault();
            if (file.Length > 0)
            {
                using var stream = new MemoryStream(new byte[file.Length]);
                await file.CopyToAsync(stream);
                var image = stream.ToArray();
                var faces = await DetectFaces(image);
                var objects = await DetectObjects(image);

                var model = new HomeViewModel
                {
                    Faces = faces,
                    FileName = file.FileName,
                    Image = image,
                    Objects = objects
                };

                return View(model);
            }
            return View(new HomeViewModel());
        }

        private async Task<IList<DetectedObject>> DetectObjects(byte[] image)
        {
            using var ms = new MemoryStream(image);
            var visionClient = new ComputerVisionClient(_credentials)
            {
                Endpoint = _config.Value.Face.BaseUri
            };

            var visionResult = await visionClient.DetectObjectsInStreamAsync(ms);
            return visionResult.Objects;
        }

        private async Task<IList<DetectedFace>> DetectFaces(byte[] image)
        {
            using var ms = new MemoryStream(image);
            var faceClient = new FaceClient(_credentials)
            {
                Endpoint = _config.Value.Face.BaseUri
            };

            var result = await faceClient.Face.DetectWithStreamAsync(ms, true, true, new[] {
                        FaceAttributeType.Age,
                        FaceAttributeType.Emotion,
                        FaceAttributeType.FacialHair,
                        FaceAttributeType.Gender,
                        FaceAttributeType.Glasses,
                        FaceAttributeType.Hair,
                        FaceAttributeType.Smile }).ConfigureAwait(false);
            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost("Sentiment")]
        public async Task<IActionResult> Sentiment(string text)
        {
            var textAnalysis = new TextAnalyticsClient(new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(_config.Value.Face.ApiKey))
            {
                Endpoint = _config.Value.Face.BaseUri
            };

            var results = await textAnalysis.SentimentAsync(text, "en", true);
                       
            var model = new HomeViewModel
            {
                Text = text,
                Sentiment = results
            };

            return View("Index", model);

        }
    }
}
