using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireDemo
{
    public class Service : IService
    {
        private readonly ILogger<Service> _logger;

        public Service(ILogger<Service> logger)
        {
            this._logger = logger;
        }
        public void Log(string value)
        {
            _logger.LogWarning($"{value}：{DateTime.Now}");
        }
    }
}
