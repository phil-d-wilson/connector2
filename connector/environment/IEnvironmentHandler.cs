using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace connector
{
    public interface IEnvironmentHandler
    {
        public IDictionary GetEnvironmentVariables();

        public string GetEnvironmentVariable(string key);

        public void SetEnvironmentVariable(string key, string value);
    }
}
