﻿using GridDomain.Node.Configuration.Persistence;

namespace GridDomain.Tests.Common
{
    public class AutoTestLocalDbConfiguration : IDbConfiguration
    {
        public string ReadModelConnectionString
            => @"Data Source=(local);Initial Catalog=AutoTestRead;Integrated Security = true";

        public string LogsConnectionString
            => @"Data Source=(local);Initial Catalog=AutoTestLogs;Integrated Security = true";
    }
}