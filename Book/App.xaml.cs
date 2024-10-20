using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;

namespace ShareInvest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
#if DEBUG
        _ = Task.Run(async () =>
        {
            using (var client = new HttpClient())
            {
                var res = await client.GetAsync(new Uri("https://www.foresttrip.go.kr/rep/cm/captchaImg.do"));

                if (res.IsSuccessStatusCode)
                {
                    using (var ms = new MemoryStream())
                    {
                        await res.Content.CopyToAsync(ms);

                        ms.Position = 0;

                        using (var apiClient = new HttpClient())
                        {
                            using (var content = new MultipartFormDataContent())
                            {
                                var imageContent = new ByteArrayContent(ms.ToArray());

                                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

                                content.Add(imageContent, "file", "captcha.png");

                                var response = await apiClient.PostAsync("http://localhost:15409", content);

                                if (response.IsSuccessStatusCode)
                                {
                                    Debug.WriteLine(await response.Content.ReadAsStringAsync());

                                    return;
                                }
                                Debug.WriteLine(response.StatusCode);
                            }
                        }
                    }
                }
            }
        });
#endif
        base.OnStartup(e);
    }
}