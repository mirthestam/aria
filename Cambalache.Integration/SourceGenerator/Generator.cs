using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cambalache.Integration.SourceGenerator;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        ScanForUIFiles(context);
    }

    private void ScanForUIFiles(IncrementalGeneratorInitializationContext context)
    {
        var uiFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".ui", StringComparison.OrdinalIgnoreCase))
            .Select((file, _) => file.Path)
            .Collect();
        
        context.RegisterSourceOutput(uiFiles, (spc, files) =>
        {
            var projectData = new CambalacheProjectData
            {
                UIFiles = files.ToList()
            };

            CambalacheProject.Generate(spc, projectData);
        });        
    }
}

internal class CambalacheProjectData
{
    public List<string> UIFiles { get; set; } = new();
}

internal static class CambalacheProject
{
    public static void Generate(SourceProductionContext context, CambalacheProjectData projectData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version='1.0' encoding='UTF-8' standalone='no'?>");
        sb.AppendLine("<!DOCTYPE cambalache-project SYSTEM \"cambalache-project.dtd\">");
        sb.AppendLine("<!-- Created with Cambalache Integration Source Generator -->");
        sb.AppendLine("<cambalache-project version=\"0.96.0\" target_tk=\"gtk-4.0\">");

        foreach (var file in projectData.UIFiles)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CAMB001",
                title: "Cambalache Log",
                messageFormat: "Gevonden UI bestand: {0}",
                category: "CambalacheGenerator",
                DiagnosticSeverity.Warning, // Gebruik Warning zodat het opvalt
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, file));
            
            // file is hier dus een path bijv. foo.ui
            
            var sourceText = File.ReadAllText(file);
            var content = sourceText?.ToString() ?? string.Empty;
            
            var templateClass = ExtractTemplateClass(content);
            var sha256 = ComputeSha256(content);
            var fileName = "../" + Path.GetFileName(file); 

            sb.AppendLine($"  <ui template-class=\"{templateClass}\" filename=\"{fileName}\" sha256=\"{sha256}\"/>");
        }

        sb.AppendLine("</cambalache-project>");
        
        var xmlOutput = sb.ToString();
        
        Console.WriteLine(xmlOutput);
    }

    private static string ExtractTemplateClass(string xmlContent)
    {
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var template = doc.Descendants("template").FirstOrDefault();
            return template?.Attribute("class")?.Value ?? "UnknownClass";
        }
        catch { return "UnknownClass"; }
    }

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
