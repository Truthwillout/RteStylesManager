using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Truthwillout.Controllers
{
    [PluginController("RteStyleManager")]
    public class RteStyleApiController : UmbracoAuthorizedApiController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RteStyleApiController> _logger;

        public RteStyleApiController(
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ILogger<RteStyleApiController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetStyleConfig()
        {
            try
            {
                var configPath = Path.Combine(_webHostEnvironment.ContentRootPath, "rte-style-formats.json");
                
                if (!System.IO.File.Exists(configPath))
                {
                    return NotFound(new { message = "Configuration file not found" });
                }

                var jsonContent = System.IO.File.ReadAllText(configPath);
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var config = JsonSerializer.Deserialize<List<StyleCategory>>(jsonContent, options);

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading style configuration");
                return StatusCode(500, new { message = "Error reading configuration", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveStyleConfig([FromBody] List<StyleCategory> config)
        {
            try
            {
                if (config == null)
                {
                    return BadRequest(new { message = "Invalid configuration data" });
                }

                var configPath = Path.Combine(_webHostEnvironment.ContentRootPath, "rte-style-formats.json");
                var stylesPath = Path.Combine(_webHostEnvironment.WebRootPath, "css", "styles.css");
                var rtfStylesPath = Path.Combine(_webHostEnvironment.WebRootPath, "css", "rtfstyles.css");

                // Serialize and save JSON
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var jsonContent = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(configPath, jsonContent);

                // Generate CSS
                var css = GenerateCss(config);
                System.IO.File.WriteAllText(stylesPath, css);
                System.IO.File.WriteAllText(rtfStylesPath, css);

                // Update in-memory configuration
                var obj = JsonSerializer.Deserialize<object>(jsonContent);
                var jsonString = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
                _configuration["Umbraco:CMS:RichTextEditor:CustomConfig:style_formats"] = jsonString;

                return Ok(new { message = "Configuration saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving style configuration");
                return StatusCode(500, new { message = "Error saving configuration", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCss()
        {
            try
            {
                var stylesPath = Path.Combine(_webHostEnvironment.WebRootPath, "css", "styles.css");
                
                if (!System.IO.File.Exists(stylesPath))
                {
                    return NotFound(new { message = "CSS file not found" });
                }

                var css = System.IO.File.ReadAllText(stylesPath);
                return Ok(new { css });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSS");
                return StatusCode(500, new { message = "Error reading CSS", error = ex.Message });
            }
        }

        private string GenerateCss(List<StyleCategory> config)
        {
            var css = new System.Text.StringBuilder();
            var processedClasses = new HashSet<string>();

            foreach (var category in config)
            {
                if (category.Items == null) continue;

                foreach (var item in category.Items)
                {
                    if (string.IsNullOrEmpty(item.Classes)) continue;

                    var className = item.Classes;
                    var block = item.Block ?? "p";
                    
                    // Create a unique key for this combination
                    var key = $"{block}.{className}";
                    
                    // Skip if we've already processed this class
                    if (processedClasses.Contains(key)) continue;
                    processedClasses.Add(key);

                    // Use the Color property if available, otherwise extract from title
                    var color = !string.IsNullOrEmpty(item.Color) 
                        ? ConvertToRgb(item.Color) 
                        : ExtractColorFromItem(item);
                    
                    if (!string.IsNullOrEmpty(color))
                    {
                        if (block != "p" && block != "span" && block != "div")
                        {
                            css.AppendLine($"{block}.{className} {{");
                        }
                        else
                        {
                            css.AppendLine($".{className} {{");
                        }
                        css.AppendLine($"    color: {color};");
                        css.AppendLine("}");
                    }
                }
            }

            return css.ToString();
        }

        private string ConvertToRgb(string color)
        {
            if (string.IsNullOrEmpty(color)) return string.Empty;

            // If already in rgb format, return as is
            if (color.StartsWith("rgb(")) return color;

            // Convert hex to RGB
            if (color.StartsWith("#"))
            {
                try
                {
                    var hex = color.TrimStart('#');
                    
                    // Handle 3-digit hex
                    if (hex.Length == 3)
                    {
                        hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                    }

                    if (hex.Length == 6)
                    {
                        var r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        var g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        var b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        return $"rgb({r}, {g}, {b})";
                    }
                }
                catch
                {
                    // If conversion fails, return empty
                    return string.Empty;
                }
            }

            return color; // Return as-is if it's a named color or other format
        }

        private string ExtractColorFromItem(StyleItem item)
        {
            if (string.IsNullOrEmpty(item.Title)) return string.Empty;

            var title = item.Title.ToLower();

            // Try to extract RGB color from title
            if (title.Contains("red"))
                return "rgb(255, 0, 0)";
            if (title.Contains("blue"))
                return "rgb(0, 0, 255)";
            if (title.Contains("green"))
                return "rgb(0, 128, 0)";
            if (title.Contains("yellow"))
                return "rgb(255, 255, 0)";
            if (title.Contains("orange"))
                return "rgb(255, 165, 0)";
            if (title.Contains("purple"))
                return "rgb(128, 0, 128)";
            if (title.Contains("black"))
                return "rgb(0, 0, 0)";
            if (title.Contains("white"))
                return "rgb(255, 255, 255)";
            if (title.Contains("gray") || title.Contains("grey"))
                return "rgb(128, 128, 128)";

            return string.Empty;
        }
    }

    public class StyleCategory
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("items")]
        public List<StyleItem>? Items { get; set; }
    }

    public class StyleItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("block")]
        public string Block { get; set; } = string.Empty;
        
        [JsonPropertyName("classes")]
        public string? Classes { get; set; }
        
        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }
}
