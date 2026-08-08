using System.Net;

namespace WaslX.Infrastructure.Email;

public static class EmailBodyBuilder
{
    public static string GenerateEmailBody(string template, Dictionary<string, string> templateModel)
    {
        var templatePath = $"{Directory.GetCurrentDirectory()}/Templates/{template}.html";
        var streamReader = new StreamReader(templatePath);
        var body = streamReader.ReadToEnd();
        streamReader.Close();

        // Values are substituted into raw HTML, so anything user-controlled (e.g. FullName)
        // must be HTML-encoded first — otherwise a name like "<img src=x onerror=...>" gets
        // injected verbatim into the email.
        foreach (var item in templateModel)
            body = body.Replace(item.Key, WebUtility.HtmlEncode(item.Value));

        return body;
    }
}
