namespace ESSample.API
{
    using API.Domain.Policies;
    using API.Infrastructure;
    using Application.Meetup.Commands.CreateMeetup;
    using Core;
    using ESSample.Domain.MeetupAggregate.Policies;
    using ESSample.Infrastructure;
    using MediatR;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OpenApi.Models;
    using System.Reflection;
    //using Microsoft.AspNetCore.Builder;

    public class Startup
    {
        public IHostingEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMediatR(typeof(CreateMeetupCommand).GetTypeInfo().Assembly);

            var domainEventMapper = new DomainEventMapper();
            domainEventMapper.Map();

            var documentStoreInitializer = new DocumentStoreInitializer(
                url: Configuration.GetValue<string>("RavenDB:Url"),
                databaseName: Configuration.GetValue<string>("RavenDB:DatabaseName"));

            var documentStore = documentStoreInitializer.GetDocumentStore();

            var elasticClientInitializer = new ElasticClientInitializer(
                uri: Configuration.GetValue<string>("ElasticSearch:Uri"));

            var elasticClient = elasticClientInitializer.GetElasticClientAsync().GetAwaiter().GetResult();

            var eventStoreConnectionInitializer = new EventStoreConnectionInitializer(
               connectionString: Configuration.GetValue<string>("EventStore:ConnectionString"),
               connectionName: Configuration.GetValue<string>("EventStore:ConnectionName"));

            var eventStoreConnection = eventStoreConnectionInitializer.GetEventStoreConnectionAsync().GetAwaiter().GetResult();

            services.AddSingleton(documentStore);
            services.AddSingleton(elasticClient);
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton<IDomainEventMapper>(domainEventMapper);
            services.AddSingleton<IMeetupPolicy, MeetupPolicy>();
            services.AddSingleton<IAggregateRepository, AggregateRepository>();

            services.AddMvc();
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CQRS_MediatR", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CQRS_MediatR v1"));
            }
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
