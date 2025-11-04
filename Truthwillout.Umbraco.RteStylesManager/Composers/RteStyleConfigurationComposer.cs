using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Truthwillout.Composers
{
    public class RteStyleConfigurationComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, RteStyleConfigurationHandler>();
        }
    }

    public class RteStyleConfigurationHandler : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RteStyleConfigurationHandler(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        public Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
        {
            var configPath = Path.Combine(_webHostEnvironment.ContentRootPath, "rte-style-formats.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var jsonContent = File.ReadAllText(configPath);
                    var obj = JsonSerializer.Deserialize<object>(jsonContent);
                    var jsonString = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });

                    // Try to update configuration if it's a ConfigurationManager
                    if (_configuration is ConfigurationManager configManager)
                    {
                        configManager["Umbraco:CMS:RichTextEditor:CustomConfig:style_formats"] = jsonString;
                    }
                    else if (_configuration is IConfigurationRoot configRoot)
                    {
                        configRoot["Umbraco:CMS:RichTextEditor:CustomConfig:style_formats"] = jsonString;
                    }
                }
                catch (Exception ex)
                {
                    // Log error if needed
                    Console.WriteLine($"Error loading RTE style configuration: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }
}