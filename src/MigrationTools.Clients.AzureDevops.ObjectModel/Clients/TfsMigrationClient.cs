﻿using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Services.Common;
using MigrationTools.Configuration;
using Serilog;

namespace MigrationTools.Clients
{
    public class TfsMigrationClient : IMigrationClient
    {
        private TfsTeamProjectConfig _config;
        private NetworkCredential _credentials;
        private IWorkItemMigrationClient _workItemClient;
        private ITestPlanMigrationClient _testPlanClient;

        private readonly IServiceProvider _Services;
        private readonly ITelemetryLogger _Telemetry;

        public TfsTeamProjectConfig TfsConfig
        {
            get
            {
                return _config;
            }
        }

        public IMigrationClientConfig Config
        {
            get
            {
                return _config;
            }
        }

        public IWorkItemMigrationClient WorkItems
        {
            get
            {
                return _workItemClient;
            }
        }

        public ITestPlanMigrationClient TestPlans
        {
            get
            {
                return _testPlanClient;
            }
        }

        // if you add Migration Engine in here you will have to fix the infinate loop
        public TfsMigrationClient(ITestPlanMigrationClient testPlanClient, IWorkItemMigrationClient workItemClient, IServiceProvider services, ITelemetryLogger telemetry)
        {
            _testPlanClient = testPlanClient;
            _workItemClient = workItemClient;
            _Services = services;
            _Telemetry = telemetry;
        }

        public void Configure(IMigrationClientConfig config, NetworkCredential credentials = null)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (!(config is TfsTeamProjectConfig))
            {
                throw new ArgumentOutOfRangeException(string.Format("{0} needs to be of type {1}", nameof(config), nameof(TfsTeamProjectConfig)));
            }

            _config = (TfsTeamProjectConfig)config;
            _credentials = credentials;
            EnsureCollection();
            _workItemClient.Configure(this);
            _testPlanClient.Configure(this);
        }

        private TfsTeamProjectCollection _collection;

        [Obsolete]
        public object InternalCollection
        {
            get
            {
                return _collection;
            }
        }

        private void EnsureCollection()
        {
            if (_collection == null)
            {
                _Telemetry.TrackEvent("TeamProjectContext.EnsureCollection",
                    new Dictionary<string, string> {
                          { "Name", TfsConfig.Project},
                          { "Target Project", TfsConfig.Project},
                          { "Target Collection",TfsConfig.Collection.ToString() },
                           { "ReflectedWorkItemID Field Name",TfsConfig.ReflectedWorkItemIDFieldName }
                    }, null);
                _collection = GetDependantTfsCollection(_credentials);
            }
        }

        private TfsTeamProjectCollection GetDependantTfsCollection(NetworkCredential credentials)
        {
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            TfsTeamProjectCollection y;
            try
            {
                if (credentials == null)
                {
                    y = new TfsTeamProjectCollection(TfsConfig.Collection);
                }
                else
                {
                    y = new TfsTeamProjectCollection(TfsConfig.Collection, new VssCredentials(new Microsoft.VisualStudio.Services.Common.WindowsCredential(credentials)));
                }
                Log.Debug("MigrationClient: Connectng to {CollectionUrl} ", TfsConfig.Collection.ToString());
                Log.Debug("MigrationClient: validating security for {@AuthorizedIdentity} ", y.AuthorizedIdentity);
                y.EnsureAuthenticated();
                timer.Stop();
                Log.Information("MigrationClient: Access granted ");
                _Telemetry.TrackDependency(new DependencyTelemetry("TfsObjectModel", TfsConfig.Collection.ToString(), "GetWorkItem", null, startTime, timer.Elapsed, "200", true));
            }
            catch (Exception ex)
            {
                timer.Stop();
                _Telemetry.TrackDependency(new DependencyTelemetry("TfsObjectModel", TfsConfig.Collection.ToString(), "GetWorkItem", null, startTime, timer.Elapsed, "500", false));
                _Telemetry.TrackException(ex,
                       new Dictionary<string, string> {
                            { "CollectionUrl", TfsConfig.Collection.ToString() },
                            { "TeamProjectName",  TfsConfig.Project}
                       },
                       new Dictionary<string, double> {
                            { "Time",timer.ElapsedMilliseconds }
                       });
                Log.Error(ex, "Unable to configure store");
                throw;
            }
            return y;
        }

        public T GetService<T>()
        {
            EnsureCollection();
            return _collection.GetService<T>();
        }
    }
}