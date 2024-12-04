using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CustomVisionApiClassification.Complete
{
    public class PredictionConsole
    {
        // Frank Lafontaine Peralta 2023-0675
        static string Endpoint = "https://customvisionweb894.cognitiveservices.azure.com/";
        static string PredictionKey = "CATpEVgmymw50i1Hqa2DunWH17jFtZt4vabqM6iHzavUIqqI2kM8JQQJ99AKACYeBjFXJ3w3AAAJACOGXoZw";

        // Custom Vision project details
        static string ProjectId = "c99728c3-fe2a-48f1-999d-abe619116830";
        static string PublishedName = "IA FastFood1";

        // Paths for test images and prediction output
        static string TestImageFolder = @"C:\Users\frank\Desktop\transformar\Test";
        static string PredictionImageFolder = @"C:\Users\frank\Desktop\transformar\Train";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Custom Vision API - Classification - Prediction");

            // Ensure folders exist
            if (!Directory.Exists(TestImageFolder))
            {
                Console.WriteLine($"La carpeta de prueba no existe: {TestImageFolder}");
                return;
            }

            if (!Directory.Exists(PredictionImageFolder))
            {
                Directory.CreateDirectory(PredictionImageFolder);
            }

            // Process images and output predictions
            var predictionResults = await PredictTestImageFolderAsync();

            // Display the results
            foreach (var result in predictionResults)
            {
                Console.WriteLine($"{result.Key}: {(result.Value * 100):F2}%");
            }
        }

        public static async Task<Dictionary<string, double>> PredictTestImageFolderAsync()
        {
            var results = new Dictionary<string, double>();
            int overallPredicted = 0;
            int overallCorrect = 0;

            // Iterate through test folders (one per tag)
            var testImageFolders = Directory.EnumerateDirectories(TestImageFolder);

            foreach (var testFolder in testImageFolders)
            {
                string testTagName = Path.GetFileName(testFolder);
                Console.WriteLine($"Procesando imágenes para etiqueta: {testTagName}");

                // Ensure prediction folder exists
                string predictionsFolder = Path.Combine(PredictionImageFolder, testTagName);
                if (!Directory.Exists(predictionsFolder))
                {
                    Directory.CreateDirectory(predictionsFolder);
                }

                // Process images in the current folder
                var testImages = Directory.GetFiles(testFolder);
                int tagPredicted = 0;
                int tagCorrect = 0;

                foreach (var testImage in testImages)
                {
                    string testImageFileName = Path.GetFileName(testImage);

                    // Get predictions
                    var predictions = await GetImagePredictionsAsync(testImage);
                    if (predictions?.Predictions == null || !predictions.Predictions.Any())
                    {
                        Console.WriteLine($"No se pudo obtener predicción para: {testImageFileName}");
                        continue;
                    }

                    // Get top prediction
                    var topPrediction = predictions.Predictions.OrderByDescending(q => q.Probability).FirstOrDefault();

                    // Update statistics
                    tagPredicted++;
                    overallPredicted++;

                    bool isCorrect = testTagName == topPrediction.TagName;
                    if (isCorrect)
                    {
                        tagCorrect++;
                        overallCorrect++;
                    }

                    // Display prediction result
                    Console.ForegroundColor = isCorrect ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"{testImageFileName} predicho como {topPrediction.TagName} ({topPrediction.Probability:P2})");
                    Console.ResetColor();

                    // Copy image to corresponding folder
                    string destinationPath = Path.Combine(predictionsFolder, $"{testTagName}_{testImageFileName}");
                    if (!File.Exists(destinationPath)) // Avoid overwriting files
                    {
                        File.Copy(testImage, destinationPath);
                    }
                }

                // Calculate accuracy for this tag
                results.Add(testTagName, (double)tagCorrect / tagPredicted);
            }

            // Add overall accuracy
            results.Add("Overall", (double)overallCorrect / overallPredicted);
            return results;
        }

        public static async Task<ImagePrediction> GetImagePredictionsAsync(string imageFile)
        {
            try
            {
                // Create prediction client using proper credentials
                var predictionClient = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(PredictionKey))
                {
                    Endpoint = Endpoint
                };

                // Ensure file exists
                if (!File.Exists(imageFile))
                {
                    Console.WriteLine($"El archivo no existe: {imageFile}");
                    return null;
                }

                // Get predictions
                using (var imageStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read))
                {
                    return await predictionClient.ClassifyImageAsync(
                        new Guid(ProjectId), PublishedName, imageStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar {imageFile}: {ex.Message}");
                return null;
            }
        }
    }
}
