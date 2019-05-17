using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Landmarks
{
    public static class LandmarkFinder
    {
        private static readonly HttpClient client = new HttpClient() { };
        private static readonly string customVisionURL = "https://japaneast.api.cognitive.microsoft.com/customvision/v3.0/Prediction/76f0d893-7cda-4625-90ec-8470bd025024/classify/iterations/TestIteration/image";

        static LandmarkFinder()
        {
            client.DefaultRequestHeaders.Add("Prediction-Key", "c7b31743df8a4b599690ec34bf7dd568");
        }


        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }


        public static async Task<Landmark> AnalyzeImage(Stream stream)
        {
            Landmark landmark;
            var imageArray = ReadFully(stream);
            var response = await client.PostAsync(customVisionURL, new ByteArrayContent(imageArray));
            var replyBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Result>(replyBody);

            // It should always return a prediction... 
            // But one exception is when size of the pic is larger than 4MB it returns bad data
            // This check is to say that no landmark was found but should be changed to display error about size?
            // Played with photo options below to reduce the size of the image but keeping this just in case
            if (result.Predictions == null || result.Predictions[0] == null)
            {
                landmark = new Landmark()
                {
                    Name = "No landmark found",
                    Confidence = 0
                };
                return landmark;
            }
            landmark = new Landmark()
            {
                Name = result.Predictions[0].TagName,
                Confidence = result.Predictions[0].Probability
            };
            return landmark;
        }
    }
}
