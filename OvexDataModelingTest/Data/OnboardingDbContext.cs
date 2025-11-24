using Microsoft.EntityFrameworkCore;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace OvexDataModelingTest.Data
{
    public class OnboardingDbContext : DbContext
    {
        public OnboardingDbContext(DbContextOptions<OnboardingDbContext> options) : base(options) { }

        // Config Schema
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<WorkflowRoutingRule> WorkflowRoutingRules { get; set; }
        public DbSet<Phase> Phases { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<FieldDefinition> FieldDefinitions { get; set; }
        public DbSet<Step_Field> StepFields { get; set; }
        public DbSet<Rule> Rules { get; set; }
        public DbSet<InstanceActionStrategy> InstanceActionStrategies { get; set; }

        // App Schema
        public DbSet<User> Users { get; set; }
        public DbSet<CustomerData> CustomerData { get; set; }
        public DbSet<Prospect> Prospects { get; set; }
        public DbSet<ProspectData> ProspectData { get; set; }
    }
}
