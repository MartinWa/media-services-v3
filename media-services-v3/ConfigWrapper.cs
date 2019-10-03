using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace media_services_v3
{
    public class ConfigWrapper
    {
        private readonly IConfiguration _config;

        public ConfigWrapper(IConfiguration config)
        {
            _config = config;
        }

        public string SubscriptionId
        {
            get { return _config["SubscriptionId"]; }
        }

        public string ResourceGroup
        {
            get { return _config["ResourceGroup"]; }
        }

        public string AccountName
        {
            get { return _config["AccountName"]; }
        }

        public string AadTenantId
        {
            get { return _config["AadTenantId"]; }
        }

        public string AadClientId
        {
            get { return _config["AadClientId"]; }
        }

        public string AadSecret
        {
            get { return _config["AadSecret"]; }
        }

        public Uri ArmAadAudience
        {
            get { return new Uri(_config["ArmAadAudience"]); }
        }

        public Uri AadEndpoint
        {
            get { return new Uri(_config["AadEndpoint"]); }
        }

        public Uri ArmEndpoint
        {
            get { return new Uri(_config["ArmEndpoint"]); }
        }

        public string Region
        {
            get { return _config["Region"]; }
        }

        public string StorageConnectionString
        {
            get { return _config["StorageConnectionString"]; }
        }

        public string MediaServicesTransform
        {
            get { return _config["MediaServicesTransform"]; }
        }

        public List<string> SupportedVideoTypes
        {
            get
            {
                return new List<string> {
                                          ".3gp",
                                          ".3g2",
                                          ".3gp2",
                                          ".asf",
                                          ".avi",
                                          ".dv",
                                          ".m2ts",
                                          ".m2v",
                                          ".m4a",
                                          ".mod",
                                          ".mov",
                                          ".mp4",
                                          ".mpeg",
                                          ".mpg",
                                          ".mts",
                                          ".ts",
                                          ".wmv" };
            }
        }

    }
}
