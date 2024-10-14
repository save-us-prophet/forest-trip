using System.Diagnostics;
using System.Windows;

using Tesseract;

namespace ShareInvest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
#if DEBUG
        using (var img = Pix.LoadFromFile("E:\\Images\\captchaImg.do"))
        {
            using (var ocr = new TesseractEngine(@"./tessdata", "kor", EngineMode.TesseractOnly))
            {
                var page = ocr.Process(img);

                var captchaText = page.GetText().Trim();

                Debug.WriteLine(captchaText);
            }
        }
#endif
        base.OnStartup(e);
    }
}