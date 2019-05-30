using System.Collections.Generic;

namespace Landmarks.Models
{
    public class Landmark
    {
        public string Name { get; set; }
        public float Confidence { get; set; }
        public string Description { get; set; }
    }

    public class CustomVisionAPIResult
    {
        public IList<Prediction> Predictions { get; set; }
    }

    public class VisionAPIResult
    {
        public IList<Landmark> Landmarks { get; set; }
    }

    public class Prediction
    {
        public float Probability { get; set; }
        public string TagName { get; set; }
    }
}
