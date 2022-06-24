using System.Linq;

namespace MyLab.PrometheusAgent.Tools
{
    class ServiceLabelExcludeLogic
    {
        private readonly string[] _whiteList;

        private static readonly string[] ExactlyServiceLabels = new[]
        {
            "maintainer"
        };

        private static readonly string[] ServiceLabelsStartWith = new[]
        {
            "com.docker.compose.",
            "desktop.docker."
        };
        
        public ServiceLabelExcludeLogic()
        {
            
        }
        
        public ServiceLabelExcludeLogic(string[] whiteList)
        {
            _whiteList = whiteList;
        }

        public bool ShouldExcludeLabel(string labelName)
        {
            if(_whiteList != null && _whiteList.Contains(labelName))
                return false;

            if (ExactlyServiceLabels.Contains(labelName))
                return true;

            if (ServiceLabelsStartWith.Any(labelName.StartsWith))
                return true;

            return false;
        }
    }
}
