using System.Collections.Generic;

namespace Landmarks
{
    public class Landmark
    {
        public string Name { get; set; }
        public float Confidence { get; set; }
        public string Description { get; set; }
    }

    public class Result
    {
        public IList<Prediction> Predictions { get; set; }
    }

    public class Prediction
    {
        public float Probability { get; set; }
        public string TagName { get; set; }
    }
}
