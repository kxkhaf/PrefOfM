using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EmailService.Services.Implementations
{
    public class EmailTemplateLoader(IWebHostEnvironment env)
    {
        public async Task<string> LoadTemplateAsync(
            string templatePath, 
            Dictionary<string, string> placeholders)
        {
            var fullPath = Path.Combine(env.ContentRootPath, templatePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Email template not found at {fullPath}");
            }

            var templateContent = await File.ReadAllTextAsync(fullPath);

            foreach (var (key, value) in placeholders)
            {
                templateContent = templateContent.Replace($"{{{key}}}", value);
            }

            return templateContent;
        }
    }
}