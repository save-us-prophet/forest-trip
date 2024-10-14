using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using ShareInvest.Models;

using System.IO;
using System.Net.Http;

using Tesseract;

namespace ShareInvest;

class ReservationService : IDisposable
{
    public void Dispose()
    {
        driver.Dispose();

        chromeService.Dispose();
    }

    internal ReservationService(string url, string[]? args = null, int commandTimeout = 0x100)
    {
        if (args != null && args.Length > 0)
        {
            foreach (var arg in args) this.args.Enqueue(arg);
        }
        var chromeOption = new ChromeOptions
        {

        };
        chromeOption.AddArguments(this.args);

        driver = new ChromeDriver(chromeService, chromeOption, TimeSpan.FromSeconds(commandTimeout));

        if (this.args.Count < 4)
        {
            driver.Manage().Window.Maximize();
        }
        driver.Navigate().GoToUrl(url);
    }

    internal async Task EnterInfomationAsync(Reservation rm, int commandTimeout = 0x100)
    {
        bool clickComboBox(string prefix, string? suffix = null, string? matchWord = null)
        {
            foreach (var e in driver.FindElements(By.XPath(prefix)))
            {
                e.Click();

                if (string.IsNullOrEmpty(suffix))
                {
                    return true;
                }

                foreach (var li in new WebDriverWait(driver, TimeSpan.FromSeconds(commandTimeout * .25)).Until(e => e.FindElements(By.XPath(suffix))))
                {
                    var a = li.FindElement(By.TagName("a"));

                    if (a.Text.Trim().Equals(matchWord))
                    {
                        a.Click();

                        return true;
                    }
                }
            }
            return false;
        }

        if (clickComboBox("//*[@id=\"srch_frm\"]/div[1]/div[1]", matchWord: rm.Region, suffix: "//*[@id=\"srch_region\"]/ul/li"))
        {
            if (clickComboBox("//*[@id=\"srch_frm\"]/div[2]/div[1]", matchWord: rm.ForestRetreat, suffix: "//*[@id=\"srch_rcfcl\"]/ul/li"))
            {
                if (clickComboBox("//*[@id=\"srch_frm\"]/div[3]"))
                {
                    var calendar = driver.FindElement(By.Id("forestCalPicker"));

                    calendar.FindElement(By.XPath($"//a[@name='{rm.StartDate.ToString("d").Replace('-', '/')}({GetDayOfWeek(rm.StartDate.DayOfWeek)})']")).Click();
                    calendar.FindElement(By.XPath($"//a[@name='{rm.EndDate.ToString("d").Replace('-', '/')}({GetDayOfWeek(rm.EndDate.DayOfWeek)})']")).Click();

                    foreach (var a in calendar.FindElements(By.TagName("a")))
                    {
                        var tag = a.TagName;

                        if ("확인".Equals(a.Text))
                        {
                            a.Click();

                            break;
                        }
                    }

                    while (int.TryParse(driver.FindElement(By.Id("stng_nofpr")).Text, out int cost) && cost != rm.NumberOfPeople)
                    {
                        foreach (var a in driver.FindElement(By.XPath("//*[@id=\"srch_frm\"]/div[4]/div[2]")).FindElements(By.TagName("a")))
                        {
                            if (cost > rm.NumberOfPeople && "minus".Equals(a.GetAttribute("class")))
                            {
                                a.Click();

                                break;
                            }

                            if (cost < rm.NumberOfPeople && "plus".Equals(a.GetAttribute("class")))
                            {
                                a.Click();

                                break;
                            }
                        }
                    }

                    foreach (var btn in driver.FindElement(By.XPath("//*[@id=\"srch_frm\"]/div[5]")).FindElements(By.TagName("button")))
                    {
                        if ("조회하기".Equals(btn.GetAttribute("title")))
                        {
                            btn.Click();

                            await Task.Delay(0x200);

                            foreach (var li in new WebDriverWait(driver, TimeSpan.FromSeconds(commandTimeout)).Until(e => e.FindElement(By.XPath("//*[@id=\"infoWrap\"]/fieldset/div/div[3]/ul"))).FindElements(By.TagName("li")))
                            {
                                foreach (var a in li.FindElements(By.TagName("a")))
                                {
                                    if ("kakao_Login".Equals(a.GetAttribute("class")))
                                    {
                                        a?.Click();

                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(rm.CabinName) is false)
                    {
                        await Task.Delay(0x200);

                        try
                        {
                            _ = driver.FindElement(By.Id("searchResultEmpty"));
                        }
                        catch (NoSuchElementException)
                        {
                            if (await ChooseCabinAsync(rm.CabinName))
                            {
                                await OpticalCharacterRecognitionAsync();
                                await Reserve();
                            }
                        }
                    }
                }
            }
        }
    }

    async Task OpticalCharacterRecognitionAsync(int commandTimeout = 0x400)
    {
        var dw = new WebDriverWait(driver, TimeSpan.FromMilliseconds(commandTimeout));

        await Task.Delay(0x200);

        using (var client = new HttpClient())
        {
            var res = await client.GetAsync(new Uri(dw.Until(e => e.FindElement(By.Id("captchaImg"))).GetAttribute("src")));

            if (res.IsSuccessStatusCode)
            {
                using (var ms = new MemoryStream())
                {
                    await res.Content.CopyToAsync(ms);

                    ms.Position = 0;

                    using (var img = Pix.LoadFromMemory(ms.ToArray()))
                    {
                        using (var ocr = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractOnly))
                        {
                            if (ocr.SetVariable("tessedit_char_whitelist", "0123456789"))
                            {
                                var page = ocr.Process(img);

                                var captchaText = page.GetText().Trim();

                                dw.Until(e => e.FindElement(By.Id("atmtcRsrvtPrvntChrct"))).SendKeys(captchaText);
                            }
                        }
                    }
                }
            }
        }
    }

    async Task Reserve(int commandTimeout = 0x400)
    {
        var dw = new WebDriverWait(driver, TimeSpan.FromMilliseconds(commandTimeout));

        await Task.Delay(0x200);

        dw.Until(e => e.FindElement(By.Id("arr_01")).FindElement(By.XPath(".."))).Click();

        await Task.Delay(0x100);

        dw.Until(e => e.FindElement(By.Id("btnRsrvt"))).Click();

        await Task.Delay(0x200);

        dw.Until(e => e.SwitchTo().Alert()).Accept();
    }

    async Task<bool> ChooseCabinAsync(string name, int commandTimeout = 0x400)
    {
        await Task.Delay(0x200);

        var dw = new WebDriverWait(driver, TimeSpan.FromMilliseconds(commandTimeout));

        foreach (var label in dw.Until(e => e.FindElement(By.Id("cmpgr")).FindElements(By.ClassName("chackbox_all"))))
        {
            label.Click();

            await Task.Delay(0x200);
        }
        var cabins = dw.Until(e => e.FindElement(By.Id("gsrm")));

        foreach (var label in cabins.FindElements(By.ClassName("chackbox_all")))
        {
            label.Click();

            await Task.Delay(0x200);
        }

        foreach (var label in cabins.FindElements(By.TagName("label")))
        {
            if (name.Contains(label.Text))
            {
                label.Click();

                break;
            }
        }
        await Task.Delay(0x200);

        foreach (var div in driver.FindElements(By.ClassName("goods_list_area")))
        {
            foreach (var cl in div.FindElements(By.TagName("div")))
            {
                if (cl.GetAttribute("class").StartsWith("communication"))
                {
                    foreach (var e in cl.FindElements(By.TagName("div")))
                    {
                        if (e.GetAttribute("class").StartsWith("list") is false)
                        {
                            continue;
                        }

                        foreach (var a in e.FindElements(By.TagName("a")))
                        {
                            if ("item".Equals(a.GetAttribute("class")) && a.Text.Contains(name))
                            {
                                a.Click();

                                return true;
                            }
                        }
                    }
                    break;
                }
            }
        }
        return false;
    }

    static string GetDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Sunday => "일",
        DayOfWeek.Monday => "월",
        DayOfWeek.Tuesday => "화",
        DayOfWeek.Wednesday => "수",
        DayOfWeek.Thursday => "목",
        DayOfWeek.Friday => "금",
        DayOfWeek.Saturday or _ => "토"
    };

    readonly Queue<string> args = new(["--window-size=1920,1080", Properties.Resources.USERAGENT, string.Concat(Properties.Resources.USERDATA, Path.Combine(Environment.CurrentDirectory, Properties.Resources.USERPATH))]);

    readonly ChromeDriver driver;

    readonly ChromeDriverService chromeService = ChromeDriverService.CreateDefaultService();
}